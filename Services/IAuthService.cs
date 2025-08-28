using RadiatorStockAPI.DTOs;

namespace RadiatorStockAPI.Services
{
    public interface IAuthService
    {
        Task<AuthResponseDto?> LoginAsync(LoginDto loginDto);
        Task<AuthResponseDto?> RegisterAsync(RegisterDto registerDto);
        Task<AuthResponseDto?> RefreshTokenAsync(string refreshToken);
        Task<bool> RevokeTokenAsync(string refreshToken);
        Task<bool> ChangePasswordAsync(Guid userId, ChangePasswordDto changePasswordDto);
        Task<bool> ValidateTokenAsync(string token);
        string GenerateJwtToken(UserDto user);
        string GenerateRefreshToken();
    }
}