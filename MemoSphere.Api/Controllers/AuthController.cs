using Microsoft.AspNetCore.Mvc;
using Core.Interfaces.Services;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;

namespace MemoSphere.Api.Controllers
{
    [AllowAnonymous]
    public class AuthController : ApiControllerBase
    {
        private readonly IAuthService _authService;

        public AuthController(IAuthService _authService)
        {
            this._authService = _authService;
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterRequest request)
        {
            if (request == null || string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password))
            {
                return BadRequest("Az email és a jelszó megadása kötelező.");
            }

            var isSuccess = await _authService.SignUpAsync(request.Email, request.Password);

            if (!isSuccess)
            {
                return BadRequest("A regisztráció sikertelen. Lehet, hogy az email cím már foglalt, vagy a jelszó nem elég biztonságos.");
            }

            return Ok(new { Message = "Sikeres regisztráció! Most már bejelentkezhetsz." });
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequest request)
        {
            if (request == null || string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password))
            {
                return BadRequest("Az email és a jelszó megadása kötelező.");
            }

            var token = await _authService.SignInAsync(request.Email, request.Password);

            if (string.IsNullOrEmpty(token))
            {
                return Unauthorized(new { Message = "Hibás email cím vagy jelszó!" });
            }

            return Ok(new LoginResponse
            {
                Token = token,
                Email = request.Email
            });
        }
    }

    public class RegisterRequest
    {
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
    }

    public class LoginRequest
    {
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
    }

    public class LoginResponse
    {
        public string Token { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
    }
}