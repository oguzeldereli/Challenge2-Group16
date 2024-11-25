// AuthorizationController.cs
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Challenge2_Group16_GUI_WebAPI.Models.Auth;
using Challenge2_Group16_GUI_WebAPI.Models;
using Challenge2_Group16_GUI_WebAPI.Services;
using Azure.Core;
using Microsoft.IdentityModel.Tokens;
using Microsoft.AspNetCore.Authorization;

namespace Challenge2_Group16_GUI_WebAPI.Controllers
{
    [Route("/auth")]
    public class AuthorizationController : Controller
    {
        private readonly AuthService _authService;
        private readonly UserManager<AppUser> _userManager;
        private readonly SignInManager<AppUser> _signInManager;
        private readonly IConfiguration _configuration;

        public AuthorizationController(AuthService authService,
            UserManager<AppUser> userManager,
            SignInManager<AppUser> signInManager,
            IConfiguration configuration)
        {
            _authService = authService;
            _userManager = userManager;
            _signInManager = signInManager;
            _configuration = configuration;
        }

        [HttpGet("authorize")]
        public IActionResult Authorize([FromQuery] AuthorizationRequest request)
        {

            TempData["client_id"] = request.client_id == _configuration["ClientSettings:ClientId"] ? request.client_id : "";
            TempData["redirect_uri"] = request.redirect_uri == _configuration["ClientSettings:RedirectUri"] ? request.redirect_uri : "";
            TempData["response_type"] = request.response_type == _configuration["ClientSettings:ResponseType"] ? request.response_type : "";
            TempData["scope"] = request.scope == _configuration["ClientSettings:Scope"] ? request.scope : "";
            TempData["state"] = request.state;
            TempData["code_challenge"] = request.code_challenge;
            TempData["code_challenge_method"] = request.code_challenge_method == _configuration["ClientSettings:CodeChallengeMethod"] ? request.code_challenge_method : "";

            return RedirectToAction("SignIn");
        }

        [HttpGet("SignIn")]
        public IActionResult SignIn()
        {
            if ((TempData.Peek("client_id")?.ToString()).IsNullOrEmpty() ||
                (TempData.Peek("redirect_uri")?.ToString()).IsNullOrEmpty() ||
                (TempData.Peek("response_type")?.ToString()).IsNullOrEmpty() ||
                (TempData.Peek("scope")?.ToString()).IsNullOrEmpty() ||
                (TempData.Peek("state")?.ToString()).IsNullOrEmpty() ||
                (TempData.Peek("code_challenge")?.ToString()).IsNullOrEmpty() ||
                (TempData.Peek("code_challenge_method")?.ToString()).IsNullOrEmpty())
            {
                return BadRequest(new { error = "missing_authorization_request" });
            }

            return View();
        }

        [HttpPost("SignIn")]
        public async Task<IActionResult> SignIn(string username, string password)
        {
            var result = await _signInManager.PasswordSignInAsync(username, password, false, false);
            if (result.Succeeded)
            {
                var user = await _userManager.FindByNameAsync(username);
                if (user == null)
                {
                    return BadRequest(new { error = "invalid_sign_in" });
                }

                var code = await _authService.GenerateAuthorizationCodeAsync(
                    TempData["client_id"]?.ToString() ?? "",
                    TempData["redirect_uri"]?.ToString() ?? "",
                    TempData["scope"]?.ToString() ?? "",
                    TempData["code_challenge"]?.ToString() ?? "",
                    TempData["code_challenge_method"]?.ToString() ?? "",
                    user);

                var redirectUri = $"{TempData["redirect_uri"]}?code={code}&state={TempData["state"]}";
                return Redirect(redirectUri);
            }

            return BadRequest(new { error = "invalid_sign_in" });
        }

        [HttpGet("is-signed-in")]
        [Authorize]
        public IActionResult IsSignedIn()
        {
            return Ok();
        }

        [HttpPost("oauth/token")]
        public async Task<IActionResult> Token([FromForm] TokenRequest request)
        {
            var tokenResponse = await _authService.ExchangeCodeForTokenAsync(request);
            if (tokenResponse == null)
            {
                return BadRequest(new { error = "invalid_grant" });
            }

            return Ok(tokenResponse);
        }

        [HttpPost("oauth/revoke")]
        [Authorize]
        public async Task<IActionResult> Revoke([FromForm] string accessToken, [FromForm] string refreshToken)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return Unauthorized();
            }

            var result = await _authService.SignOut(user, accessToken, refreshToken);
            if (!result)
            {
                return BadRequest(new { error = "invalid_token" });
            }

            return Ok();
        }

        [HttpPost("oauth/revoke-all")]
        [Authorize]
        public async Task<IActionResult> Revoke()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return Unauthorized();
            }

            var result = await _authService.SignOutFromAllDevices(user);
            if (!result)
            {
                return BadRequest(new { error = "invalid_token" });
            }

            return Ok();
        }

        [HttpPost("oauth/refresh")]
        public async Task<IActionResult> Refresh([FromForm] RefreshRequest refreshRequest)
        {
            if (refreshRequest.grant_type != "refresh_token")
            {
                return BadRequest(new { error = "unsupported_grant_type" });
            }

            if (refreshRequest.scope != _configuration["ClientSettings:Scope"])
            {
                return BadRequest(new { error = "invalid_scope" });
            }

            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return Unauthorized();
            }

            var result = await _authService.RefreshAccessToken(refreshRequest);
            if (result == null)
            {
                return BadRequest(new { error = "invalid_token" });
            }

            return Ok(result);
        }
    }
}
