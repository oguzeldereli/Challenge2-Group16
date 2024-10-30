// AuthorizationController.cs
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Challenge2_Group16_GUI_WebAPI.Models.Auth;
using Challenge2_Group16_GUI_WebAPI.Models;
using Challenge2_Group16_GUI_WebAPI.Services;
using Azure.Core;
using Microsoft.IdentityModel.Tokens;
using Challenge2_Group16_GUI_WebAPI.Models;

namespace Challenge2_Group16_GUI_WebAPI.Controllers
{
    [Route("/auth")]
    public class AuthorizationController : Controller
    {
        private readonly AuthService _authService;
        private readonly UserManager<AppUser> _userManager;
        private readonly SignInManager<AppUser> _signInManager;

        public AuthorizationController(AuthService authService, UserManager<AppUser> userManager, SignInManager<AppUser> signInManager)
        {
            _authService = authService;
            _userManager = userManager;
            _signInManager = signInManager;
        }

        [HttpGet("authorize")]
        public IActionResult Authorize([FromQuery] AuthorizationRequest request)
        {
            TempData["client_id"] = request.ClientId;
            TempData["redirect_uri"] = request.RedirectUri;
            TempData["response_type"] = request.ResponseType;
            TempData["scope"] = request.Scope;
            TempData["state"] = request.State;
            TempData["code_challenge"] = request.CodeChallenge;
            TempData["code_challenge_method"] = request.CodeChallengeMethod;

            return RedirectToAction("SignIn");
        }

        [HttpGet("SignIn")]
        public IActionResult SignIn()
        {
            if ((TempData.Peek("client_id") as string).IsNullOrEmpty() ||
                (TempData.Peek("redirect_uri") as string).IsNullOrEmpty() ||
                (TempData.Peek("response_type") as string).IsNullOrEmpty() ||
                (TempData.Peek("scope") as string).IsNullOrEmpty() ||
                (TempData.Peek("state") as string).IsNullOrEmpty() ||
                (TempData.Peek("code_challenge") as string).IsNullOrEmpty() ||
                (TempData.Peek("code_challenge_method") as string).IsNullOrEmpty())
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
                var code = await _authService.GenerateAuthorizationCodeAsync(
                    TempData["client_id"].ToString(),
                    TempData["redirect_uri"].ToString(),
                    TempData["scope"].ToString(),
                    TempData["code_challenge"].ToString(),
                    TempData["code_challenge_method"].ToString(),
                    user);

                var redirectUri = $"{TempData["redirect_uri"]}?code={code}&state={TempData["state"]}";
                return Redirect(redirectUri);
            }

            return BadRequest(new { error = "invalid_sign_in" });
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
    }
}
