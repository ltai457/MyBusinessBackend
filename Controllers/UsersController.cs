// Controllers/UsersController.cs
// CREATE THIS NEW FILE in your Controllers folder

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RadiatorStockAPI.DTOs.Users;
using RadiatorStockAPI.Services.Users;

namespace RadiatorStockAPI.Controllers
{
    [ApiController]
    [Route("api/v1/users")]
    [Produces("application/json")]
    [Authorize(Roles = "Admin")] // Only admins can access user management
    public class UsersController : ControllerBase
    {
        private readonly IUserService _userService;
        private readonly ILogger<UsersController> _logger;

        public UsersController(IUserService userService, ILogger<UsersController> logger)
        {
            _userService = userService;
            _logger = logger;
        }

        /// <summary>
        /// Get all users (Admin only)
        /// </summary>
        [HttpGet]
        public async Task<ActionResult<IEnumerable<UserDto>>> GetAllUsers()
        {
            try
            {
                var users = await _userService.GetAllUsersAsync();
                return Ok(users);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching all users");
                return StatusCode(500, new { message = "Failed to fetch users" });
            }
        }

        /// <summary>
        /// Get user by ID (Admin only)
        /// </summary>
        [HttpGet("{id:guid}")]
        public async Task<ActionResult<UserDto>> GetUserById(Guid id)
        {
            try
            {
                var user = await _userService.GetUserByIdAsync(id);
                if (user == null)
                    return NotFound(new { message = $"User with ID {id} not found." });

                return Ok(user);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching user {UserId}", id);
                return StatusCode(500, new { message = "Failed to fetch user" });
            }
        }

        /// <summary>
        /// Create new user (Admin only)
        /// </summary>
        [HttpPost]
        public async Task<ActionResult<UserDto>> CreateUser([FromBody] CreateUserDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            try
            {
                // Check if username already exists
                if (await _userService.UsernameExistsAsync(dto.Username))
                {
                    return Conflict(new { message = $"Username '{dto.Username}' already exists." });
                }

                // Check if email already exists
                if (await _userService.EmailExistsAsync(dto.Email))
                {
                    return Conflict(new { message = $"Email '{dto.Email}' already exists." });
                }

                var user = await _userService.CreateUserAsync(dto);
                if (user == null)
                    return BadRequest(new { message = "Failed to create user." });

                _logger.LogInformation("User created successfully: {Username}", dto.Username);
                return CreatedAtAction(nameof(GetUserById), new { id = user.Id }, user);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating user");
                return StatusCode(500, new { message = "Failed to create user" });
            }
        }

        /// <summary>
        /// Update user (Admin only)
        /// </summary>
        [HttpPut("{id:guid}")]
        public async Task<ActionResult<UserDto>> UpdateUser(Guid id, [FromBody] UpdateUserDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            try
            {
                // Check if user exists
                if (!await _userService.UserExistsAsync(id))
                    return NotFound(new { message = $"User with ID {id} not found." });

                // Check if username conflicts with other users
                if (await _userService.UsernameExistsAsync(dto.Username, id))
                {
                    return Conflict(new { message = $"Username '{dto.Username}' is already taken." });
                }

                // Check if email conflicts with other users
                if (await _userService.EmailExistsAsync(dto.Email, id))
                {
                    return Conflict(new { message = $"Email '{dto.Email}' is already taken." });
                }

                var user = await _userService.UpdateUserAsync(id, dto);
                if (user == null)
                    return BadRequest(new { message = "Failed to update user." });

                _logger.LogInformation("User updated successfully: {UserId}", id);
                return Ok(user);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating user {UserId}", id);
                return StatusCode(500, new { message = "Failed to update user" });
            }
        }

        /// <summary>
        /// Delete user (Admin only)
        /// </summary>
        [HttpDelete("{id:guid}")]
        public async Task<IActionResult> DeleteUser(Guid id)
        {
            try
            {
                // Check if user exists
                if (!await _userService.UserExistsAsync(id))
                    return NotFound(new { message = $"User with ID {id} not found." });

                var deleted = await _userService.DeleteUserAsync(id);
                if (!deleted)
                    return BadRequest(new { message = "Failed to delete user." });

                _logger.LogInformation("User deleted successfully: {UserId}", id);
                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting user {UserId}", id);
                return StatusCode(500, new { message = "Failed to delete user" });
            }
        }

        /// <summary>
        /// Check if username exists (Admin only)
        /// </summary>
        [HttpGet("check-username/{username}")]
        public async Task<ActionResult<bool>> CheckUsernameExists(string username)
        {
            try
            {
                var exists = await _userService.UsernameExistsAsync(username);
                return Ok(new { exists });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking username");
                return StatusCode(500, new { message = "Failed to check username" });
            }
        }

        /// <summary>
        /// Check if email exists (Admin only)
        /// </summary>
        [HttpGet("check-email/{email}")]
        public async Task<ActionResult<bool>> CheckEmailExists(string email)
        {
            try
            {
                var exists = await _userService.EmailExistsAsync(email);
                return Ok(new { exists });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking email");
                return StatusCode(500, new { message = "Failed to check email" });
            }
        }
    }
}
