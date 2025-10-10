using System.ComponentModel.DataAnnotations;
using RadiatorStockAPI.Models;

namespace RadiatorStockAPI.DTOs.Auth;

public class RegisterRequestDto
{
    [Required]
    [StringLength(50, MinimumLength = 3)]
    public string Username { get; set; } = string.Empty;

    [Required]
    [EmailAddress]
    [StringLength(100)]
    public string Email { get; set; } = string.Empty;

    [Required]
    [StringLength(100, MinimumLength = 6)]
    [RegularExpression(
        @"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[@$!%*?&])[A-Za-z\d@$!%*?&]{6,}$",
        ErrorMessage =
            "Password must contain at least one lowercase letter, one uppercase letter, one digit, and one special character.")]
    public string Password { get; set; } = string.Empty;

    [Required]
    public UserRole Role { get; set; }
}
