using System.ComponentModel.DataAnnotations;

namespace RadiatorStockAPI.DTOs.Auth;

public class ChangePasswordRequestDto
{
    [Required]
    public string CurrentPassword { get; set; } = string.Empty;

    [Required]
    [StringLength(100, MinimumLength = 6)]
    [RegularExpression(
        @"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[@$!%*?&])[A-Za-z\d@$!%*?&]{6,}$",
        ErrorMessage =
            "Password must contain at least one lowercase letter, one uppercase letter, one digit, and one special character.")]
    public string NewPassword { get; set; } = string.Empty;
}
