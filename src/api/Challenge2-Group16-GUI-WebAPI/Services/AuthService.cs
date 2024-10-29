// AuthService.cs
using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Configuration;
using System.Collections.Concurrent;
using System.Collections.Generic;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Challenge2_Group16_GUI_WebAPI.Data;
using Challenge2_Group16_GUI_WebAPI.Models;
using Challenge2_Group16_GUI_WebAPI.Models.Auth;
using Microsoft.EntityFrameworkCore;

namespace Challenge2_Group16_GUI_WebAPI.Services
{
    public class AuthService
    {
        private readonly UserManager<AppUser> _userManager;
        private readonly ApplicationDbContext _dbContext;
        private readonly IConfiguration _configuration;

        private static readonly ConcurrentDictionary<string, AuthorizationCode> AuthorizationCodes = new ConcurrentDictionary<string, AuthorizationCode>();

        public AuthService(
            UserManager<AppUser> userManager,
            ApplicationDbContext dbContext,
            IConfiguration configuration)
        {
            _userManager = userManager;
            _dbContext = dbContext;
            _configuration = configuration;
        }

        public async Task<string> GenerateAuthorizationCodeAsync(string clientId, string redirectUri, string scope, string codeChallenge, string codeChallengeMethod, AppUser user)
        {
            var code = GenerateRandomString(32);
            var authorizationCode = new AuthorizationCode
            {
                Code = code,
                ClientId = clientId,
                RedirectUri = redirectUri,
                Scope = scope,
                CodeChallenge = codeChallenge,
                CodeChallengeMethod = codeChallengeMethod,
                UserId = user.Id,
                ExpiresAt = DateTime.UtcNow.AddMinutes(5)
            };
            AuthorizationCodes[code] = authorizationCode;
            return code;
        }

        public async Task<TokenResponse> ExchangeCodeForTokenAsync(TokenRequest request)
        {
            if (!AuthorizationCodes.TryGetValue(request.Code, out var authCode))
            {
                return null;
            }

            if (authCode.ExpiresAt < DateTime.UtcNow)
            {
                AuthorizationCodes.TryRemove(request.Code, out _);
                return null;
            }

            if (authCode.ClientId != request.ClientId || authCode.RedirectUri != request.RedirectUri)
            {
                return null;
            }

            if (!ValidateCodeVerifier(request.CodeVerifier, authCode.CodeChallenge, authCode.CodeChallengeMethod))
            {
                return null;
            }

            var user = await _userManager.FindByIdAsync(authCode.UserId);
            if (user == null)
            {
                return null;
            }

            var accessToken = await GenerateAccessTokenAsync(user);
            var refreshToken = GenerateRandomString(32);

            RefreshToken token = new RefreshToken
            {
                Token = refreshToken,
                ExpiresAt = DateTime.UtcNow.AddDays(7),
                UserId = user.Id,
                IsRevoked = false,
                User = user,
            };

            _dbContext.RefreshTokens.Add(token);
            await _dbContext.SaveChangesAsync();

            AuthorizationCodes.TryRemove(request.Code, out _);

            return new TokenResponse
            {
                AccessToken = accessToken,
                TokenType = "Bearer",
                ExpiresIn = 3600,
                RefreshToken = refreshToken
            };
        }

        public async Task<TokenResponse> RefreshAccessToken(RefreshRequest request)
        {
            var refreshRelatedToken = await _dbContext.RefreshTokens.Include(x => x.User)
                .Where(rt => rt.Token == request.RefreshToken && !rt.IsExpired && !rt.IsRevoked)
                .FirstOrDefaultAsync();
            if (refreshRelatedToken == null)
            {
                return null;
            }

            var user = refreshRelatedToken.User;
            if (user == null)
            {
                return null;
            }

            if (refreshRelatedToken.IsUsed)
            {
                _dbContext.Entry(user).Reference(u => u.RefreshTokens).Load();
                user.RefreshTokens.ForEach(rt => rt.IsRevoked = true);
                return null;
            }

            var accessToken = await GenerateAccessTokenAsync(user);
            var refreshToken = GenerateRandomString(32);

            RefreshToken token = new RefreshToken
            {
                Token = refreshToken,
                ExpiresAt = DateTime.UtcNow.AddDays(7),
                UserId = user.Id,
                IsRevoked = false,
                User = user,
            };

            refreshRelatedToken.IsUsed = true;
            _dbContext.RefreshTokens.Add(token);
            await _dbContext.SaveChangesAsync();

            return new TokenResponse
            {
                AccessToken = accessToken,
                TokenType = "Bearer",
                ExpiresIn = 3600,
                RefreshToken = refreshToken
            };
        }

        public async Task SignOutFromAllDevices(AppUser User)
        {
            _dbContext.Entry(User).Reference(u => u.RefreshTokens).Load();
            User.RefreshTokens.ForEach(rt => rt.IsRevoked = true);
            await _dbContext.SaveChangesAsync();
        }

        public async Task<bool> SignOut(AppUser User, string accessToken)
        {
            // validate the access token claims by comparing to the user, if accessToken is valid, add access token to access token blacklist
            // refresh tokens stay

            var tokenHandler = new JwtSecurityTokenHandler();
            var token = tokenHandler.ReadJwtToken(accessToken);
            var userName = token.Claims.First(claim => claim.Type == JwtRegisteredClaimNames.Sub).Value;
            var userId = token.Claims.First(claim => claim.Type == ClaimTypes.NameIdentifier).Value;

            if (userId != User.Id || userName != User.UserName)
            {
                return false;
            }

            var tokenBlacklist = new AccessTokenBlacklistEntry
            {
                AccessToken = accessToken
            };

            _dbContext.AccessTokenBlacklist.Add(tokenBlacklist);
            await _dbContext.SaveChangesAsync();
            return true;
        }

        private bool ValidateCodeVerifier(string codeVerifier, string codeChallenge, string codeChallengeMethod)
        {
            if (codeChallengeMethod == "S256")
            {
                using (var sha256 = SHA256.Create())
                {
                    var hash = sha256.ComputeHash(Encoding.UTF8.GetBytes(codeVerifier));
                    var codeVerifierHashed = Base64UrlEncode(hash);
                    return codeVerifierHashed == codeChallenge;
                }
            }
            else if (codeChallengeMethod == "plain")
            {
                return codeVerifier == codeChallenge;
            }
            else
            {
                return false;
            }
        }

        private async Task<string> GenerateAccessTokenAsync(AppUser user)
        {
            var userClaims = await _userManager.GetClaimsAsync(user);
            var roles = await _userManager.GetRolesAsync(user);

            var claims = new List<Claim>
            {
                new Claim(JwtRegisteredClaimNames.Sub, user.UserName),
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            };

            claims.AddRange(userClaims);
            claims.AddRange(roles.Select(role => new Claim(ClaimTypes.Role, role)));

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["JwtSettings:Secret"]));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer: _configuration["JwtSettings:Issuer"],
                audience: _configuration["JwtSettings:Audience"],
                claims: claims,
                expires: DateTime.UtcNow.AddHours(1),
                signingCredentials: creds);

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        private string GenerateRandomString(int length)
        {
            var randomNumber = new byte[length];
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(randomNumber);
            }
            return Base64UrlEncode(randomNumber);
        }

        private string Base64UrlEncode(byte[] input)
        {
            var output = Convert.ToBase64String(input);
            output = output.Replace("+", "-").Replace("/", "_").Replace("=", "");
            return output;
        }

        private static byte[] HexStringToByteArray(string hex)
        {
            if (hex.Length % 2 != 0)
                throw new ArgumentException("Hex string must have an even length.");

            byte[] bytes = new byte[hex.Length / 2];
            for (int i = 0; i < hex.Length; i += 2)
            {
                bytes[i / 2] = Convert.ToByte(hex.Substring(i, 2), 16);
            }

            return bytes;
        }
    }
}
