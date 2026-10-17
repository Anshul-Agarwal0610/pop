using BackendAPI.Interfaces;
using BackendAPI.Models;
using Google.Apis.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BackendAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly IAuthRepository _authRepo;
        private readonly IConfiguration _config;

        public AuthController(IAuthRepository authRepo, IConfiguration config)
        {
            _authRepo = authRepo;
            _config = config;
        }

        // POST /api/auth/register
        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterRequest request)
        {
            try
            {
                var result = await _authRepo.RegisterAsync(request);
                return Ok(result);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return Conflict(new { message = ex.Message });
            }
        }

        // POST /api/auth/login
        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequest request)
        {
            try
            {
                var result = await _authRepo.LoginAsync(request);
                return Ok(result);
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(new { message = ex.Message });
            }
        }

        [HttpPost("business/register")]
        public async Task<IActionResult> RegisterBusiness([FromBody] RegisterBusinessRequest request)
        {
            try { return Ok(await _authRepo.RegisterBusinessAsync(request)); }
            catch (ArgumentException ex) { return BadRequest(new { message = ex.Message }); }
            catch (InvalidOperationException ex) { return Conflict(new { message = ex.Message }); }
        }

        [HttpPost("business/login")]
        public async Task<IActionResult> LoginBusiness([FromBody] LoginRequest request)
        {
            try { return Ok(await _authRepo.LoginBusinessAsync(request)); }
            catch (UnauthorizedAccessException ex) { return Unauthorized(new { message = ex.Message }); }
        }

        // POST /api/auth/google
        // Frontend sends the Google ID token; we validate it server-side.
        [HttpPost("google")]
        public async Task<IActionResult> GoogleLogin([FromBody] GoogleLoginRequest request)
        {
            try
            {
                var audiences = new[]
                {
                    _config["Google:ClientId"],
                    _config["Google:AndroidClientId"],
                    _config["Google:IosClientId"]
                }.Where(value => !string.IsNullOrWhiteSpace(value)).Cast<string>().Distinct().ToArray();
                if (audiences.Length == 0)
                    return StatusCode(503, new { message = "Google sign-in is not configured." });
                var settings = new GoogleJsonWebSignature.ValidationSettings { Audience = audiences };

                var payload = await GoogleJsonWebSignature.ValidateAsync(request.IdToken, settings);

                var result = await _authRepo.GoogleLoginAsync(
                    email:       payload.Email,
                    displayName: payload.Name,
                    avatarUrl:   payload.Picture,
                    acceptTerms: request.AcceptTerms
                );

                return Ok(result);
            }
            catch (InvalidJwtException)
            {
                return Unauthorized(new { message = "Invalid Google token." });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        // GET /api/auth/me  — returns the logged-in user from JWT
        [HttpGet("me")]
        [Authorize]
        public async Task<IActionResult> Me()
        {
            var idClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)
                          ?? User.FindFirst("sub");

            if (idClaim == null || !long.TryParse(idClaim.Value, out var userId))
                return Unauthorized();

            var user = await _authRepo.GetByIdAsync(userId);
            if (user == null) return NotFound();
            return Ok(user);
        }
    }
}
