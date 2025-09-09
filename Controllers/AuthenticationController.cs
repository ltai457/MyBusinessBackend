using Microsoft.AspNetCore.Authorization; // ADD THIS LINE
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using RadiatorStockAPI.DTOs;
using RadiatorStockAPI.Services;

namespace RadiatorStockAPI.Controllers
{
    [ApiController]
    [Route("api/v1/auth")]
    [Produces("application/json")]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;

        public AuthController(IAuthService authService)
        {
            _authService = authService;
        }

        [HttpPost("login")]
        public async Task<ActionResult<AuthResponseDto>> Login([FromBody] LoginDto loginDto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var response = await _authService.LoginAsync(loginDto);
            if (response == null)
                return Unauthorized(new { message = "Invalid username or password." });

            return Ok(response);
        }

        [HttpPost("register")]
        public async Task<ActionResult<AuthResponseDto>> Register([FromBody] RegisterDto registerDto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var response = await _authService.RegisterAsync(registerDto);
            if (response == null)
                return Conflict(new { message = "Username or email already exists." });

            return Created(string.Empty, response);
        }

        [HttpPost("refresh")]
        public async Task<ActionResult<AuthResponseDto>> RefreshToken([FromBody] RefreshTokenDto refreshTokenDto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var response = await _authService.RefreshTokenAsync(refreshTokenDto.RefreshToken);
            if (response == null)
                return Unauthorized(new { message = "Invalid or expired refresh token." });

            return Ok(response);
        }

        [HttpPost("logout")]
        public async Task<IActionResult> Logout([FromBody] RefreshTokenDto refreshTokenDto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var result = await _authService.RevokeTokenAsync(refreshTokenDto.RefreshToken);
            return Ok(new { message = result ? "Logged out successfully." : "Token already invalid." });
        }

        [HttpPost("change-password")]
        [Authorize] // ADD THIS LINE - only this endpoint needs auth
        public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordDto changePasswordDto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            Guid.TryParse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value, out var userGuid);
            var ok = await _authService.ChangePasswordAsync(userGuid, changePasswordDto);
            if (!ok) return BadRequest(new { message = "Current password is incorrect." });

            return Ok(new { message = "Password changed successfully." });
        }

        [HttpGet("me")]
        [Authorize] // ADD THIS LINE - only this endpoint needs auth
        public IActionResult Me()
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var username = User.FindFirst(ClaimTypes.Name)?.Value;
            var email = User.FindFirst(ClaimTypes.Email)?.Value;
            var role = User.FindFirst(ClaimTypes.Role)?.Value;

            return Ok(new
            {
                id = userId,
                username,
                email,
                role
            });
        }
    }
}