using System.ComponentModel.DataAnnotations;

namespace RadiatorStockAPI.DTOs.Auth;

public class RefreshTokenRequestDto
{
    [Required]
    public string RefreshToken { get; set; } = string.Empty;
}
