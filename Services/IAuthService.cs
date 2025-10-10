using RadiatorStockAPI.DTOs.Auth;
using RadiatorStockAPI.DTOs.Users;

namespace RadiatorStockAPI.Services
{
    public interface IAuthService
    {
        Task<AuthResponseDto?> LoginAsync(LoginRequestDto loginRequest);
        Task<AuthResponseDto?> RegisterAsync(RegisterRequestDto registerRequest);
        Task<AuthResponseDto?> RefreshTokenAsync(string refreshToken);
        Task<bool> RevokeTokenAsync(string refreshToken);
        Task<bool> ChangePasswordAsync(Guid userId, ChangePasswordRequestDto changePasswordRequest);
        Task<bool> ValidateTokenAsync(string token);
        string GenerateJwtToken(UserDto user);
        string GenerateRefreshToken();
    }
}
