using CoreAPI.Models;
using CoreAPI.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace CoreAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly AuthService _authService;

        public AuthController(AuthService authService)
        {
            _authService = authService;
        }

        [HttpPost("register")]
        public async Task<ActionResult<AuthResponse>> Register([FromBody] UserRegistrationRequest request)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                var response = await _authService.RegisterAsync(request);
                return Ok(response);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception)
            {
                return StatusCode(500, new { message = "An error occurred during registration" });
            }
        }

        [HttpPost("login")]
        public async Task<ActionResult<AuthResponse>> Login([FromBody] UserLoginRequest request)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                var response = await _authService.LoginAsync(request);
                return Ok(response);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception)
            {
                return StatusCode(500, new { message = "An error occurred during login" });
            }
        }

        [HttpPost("refresh")]
        public async Task<ActionResult<AuthResponse>> RefreshToken([FromBody] RefreshTokenRequest request)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                var response = await _authService.RefreshTokenAsync(request.RefreshToken);
                return Ok(response);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception)
            {
                return StatusCode(500, new { message = "An error occurred while refreshing token" });
            }
        }

        [HttpPost("logout")]
        public async Task<ActionResult> Logout([FromBody] RefreshTokenRequest request)
        {
            try
            {
                await _authService.RevokeTokenAsync(request.RefreshToken);
                return Ok(new { message = "Logged out successfully" });
            }
            catch (Exception)
            {
                return StatusCode(500, new { message = "An error occurred during logout" });
            }
        }

        [HttpGet("me")]
        public ActionResult GetCurrentUser()
        {
            var user = User;
            if (!user.Identity?.IsAuthenticated == true)
            {
                return Unauthorized();
            }

            var userInfo = new
            {
                UserId = user.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value,
                Email = user.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value,
                Name = user.FindFirst(System.Security.Claims.ClaimTypes.Name)?.Value,
                Role = user.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value,
                Company = user.FindFirst("Company")?.Value
            };

            return Ok(userInfo);
        }
    }
}
