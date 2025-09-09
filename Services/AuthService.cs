// Services/AuthService.cs - FIXED VERSION
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using BCrypt.Net;
using RadiatorStockAPI.Data;
using RadiatorStockAPI.DTOs;
using RadiatorStockAPI.Models;

namespace RadiatorStockAPI.Services
{
    public class AuthService : IAuthService
    {
        private readonly RadiatorDbContext _context;
        private readonly IConfiguration _configuration;
        private readonly IUserService _userService;
        private readonly ILogger<AuthService> _logger;

        public AuthService(
            RadiatorDbContext context, 
            IConfiguration configuration, 
            IUserService userService,
            ILogger<AuthService> logger)
        {
            _context = context;
            _configuration = configuration;
            _userService = userService;
            _logger = logger;
        }

        public async Task<AuthResponseDto?> LoginAsync(LoginDto loginDto)
        {
            try
            {
                _logger.LogInformation("Login attempt for user: {Username}", loginDto.Username);

                var user = await _context.Users
                    .FirstOrDefaultAsync(u => u.Username == loginDto.Username && u.IsActive);

                if (user == null)
                {
                    _logger.LogWarning("Login failed - user not found: {Username}", loginDto.Username);
                    return null;
                }

                if (!BCrypt.Net.BCrypt.Verify(loginDto.Password, user.PasswordHash))
                {
                    _logger.LogWarning("Login failed - invalid password for user: {Username}", loginDto.Username);
                    return null;
                }

                // Update last login
                user.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();

                var userDto = await _userService.GetUserDtoAsync(user);
                var accessToken = GenerateJwtToken(userDto);
                var refreshToken = GenerateRefreshToken();

                // Clean up old refresh tokens for this user
                await CleanupOldRefreshTokens(user.Id);

                // Save new refresh token
                var refreshTokenEntity = new RefreshToken
                {
                    Id = Guid.NewGuid(),
                    Token = refreshToken,
                    UserId = user.Id,
                    CreatedAt = DateTime.UtcNow,
                    IsRevoked = false
                };

                _context.RefreshTokens.Add(refreshTokenEntity);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Login successful for user: {Username}", loginDto.Username);

                var expirationMinutes = _configuration.GetValue<int>("JWT:AccessTokenExpirationMinutes", 15);
                var expiresAt = DateTime.UtcNow.AddMinutes(expirationMinutes);

                return new AuthResponseDto
                {
                    AccessToken = accessToken,
                    RefreshToken = refreshToken,
                    ExpiresAt = expiresAt,
                    User = userDto
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during login for user: {Username}", loginDto.Username);
                return null;
            }
        }

        public async Task<AuthResponseDto?> RegisterAsync(RegisterDto registerDto)
        {
            try
            {
                _logger.LogInformation("Registration attempt for user: {Username}", registerDto.Username);

                // Check if username exists
                if (await _userService.UsernameExistsAsync(registerDto.Username))
                {
                    _logger.LogWarning("Registration failed - username exists: {Username}", registerDto.Username);
                    return null;
                }

                // Check if email exists
                if (await _userService.EmailExistsAsync(registerDto.Email))
                {
                    _logger.LogWarning("Registration failed - email exists: {Email}", registerDto.Email);
                    return null;
                }

                // Create user
                var createUserDto = new CreateUserDto
                {
                    Username = registerDto.Username,
                    Email = registerDto.Email,
                    Password = registerDto.Password,
                    Role = registerDto.Role
                };

                var userDto = await _userService.CreateUserAsync(createUserDto);
                if (userDto == null)
                {
                    _logger.LogError("Failed to create user: {Username}", registerDto.Username);
                    return null;
                }

                // Generate tokens for immediate login
                var accessToken = GenerateJwtToken(userDto);
                var refreshToken = GenerateRefreshToken();

                // Save refresh token
                var refreshTokenEntity = new RefreshToken
                {
                    Id = Guid.NewGuid(),
                    Token = refreshToken,
                    UserId = userDto.Id,
                    CreatedAt = DateTime.UtcNow,
                    IsRevoked = false
                };

                _context.RefreshTokens.Add(refreshTokenEntity);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Registration successful for user: {Username}", registerDto.Username);

                var expirationMinutes = _configuration.GetValue<int>("JWT:AccessTokenExpirationMinutes", 15);
                var expiresAt = DateTime.UtcNow.AddMinutes(expirationMinutes);

                return new AuthResponseDto
                {
                    AccessToken = accessToken,
                    RefreshToken = refreshToken,
                    ExpiresAt = expiresAt,
                    User = userDto
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during registration for user: {Username}", registerDto.Username);
                return null;
            }
        }

        public async Task<AuthResponseDto?> RefreshTokenAsync(string refreshToken)
        {
            try
            {
                _logger.LogInformation("Token refresh attempt");

                var tokenEntity = await _context.RefreshTokens
                    .Include(rt => rt.User)
                    .FirstOrDefaultAsync(rt => rt.Token == refreshToken);

                if (tokenEntity == null || tokenEntity.IsRevoked || !tokenEntity.User.IsActive)
                {
                    _logger.LogWarning("Token refresh failed - invalid token");
                    return null;
                }

                // Revoke old token
                tokenEntity.IsRevoked = true;

                // Create new tokens
                var userDto = await _userService.GetUserDtoAsync(tokenEntity.User);
                var newAccessToken = GenerateJwtToken(userDto);
                var newRefreshToken = GenerateRefreshToken();

                // Save new refresh token
                var newTokenEntity = new RefreshToken
                {
                    Id = Guid.NewGuid(),
                    Token = newRefreshToken,
                    UserId = tokenEntity.UserId,
                    CreatedAt = DateTime.UtcNow,
                    IsRevoked = false
                };

                _context.RefreshTokens.Add(newTokenEntity);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Token refresh successful for user: {Username}", tokenEntity.User.Username);

                var expirationMinutes = _configuration.GetValue<int>("JWT:AccessTokenExpirationMinutes", 15);
                var expiresAt = DateTime.UtcNow.AddMinutes(expirationMinutes);

                return new AuthResponseDto
                {
                    AccessToken = newAccessToken,
                    RefreshToken = newRefreshToken,
                    ExpiresAt = expiresAt,
                    User = userDto
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during token refresh");
                return null;
            }
        }

        public async Task<bool> RevokeTokenAsync(string refreshToken)
        {
            try
            {
                var tokenEntity = await _context.RefreshTokens
                    .FirstOrDefaultAsync(rt => rt.Token == refreshToken);

                if (tokenEntity == null)
                {
                    return false;
                }

                tokenEntity.IsRevoked = true;
                await _context.SaveChangesAsync();

                _logger.LogInformation("Token revoked successfully");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error revoking token");
                return false;
            }
        }

        public async Task<bool> ChangePasswordAsync(Guid userId, ChangePasswordDto changePasswordDto)
        {
            try
            {
                var user = await _context.Users.FindAsync(userId);
                if (user == null)
                {
                    _logger.LogWarning("Password change failed - user not found: {UserId}", userId);
                    return false;
                }

                if (!BCrypt.Net.BCrypt.Verify(changePasswordDto.CurrentPassword, user.PasswordHash))
                {
                    _logger.LogWarning("Password change failed - invalid current password for user: {UserId}", userId);
                    return false;
                }

                user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(changePasswordDto.NewPassword);
                user.UpdatedAt = DateTime.UtcNow;

                // Revoke all existing refresh tokens to force re-login on all devices
                var userTokens = await _context.RefreshTokens
                    .Where(rt => rt.UserId == userId && !rt.IsRevoked)
                    .ToListAsync();

                foreach (var token in userTokens)
                {
                    token.IsRevoked = true;
                }

                await _context.SaveChangesAsync();

                _logger.LogInformation("Password changed successfully for user: {UserId}", userId);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error changing password for user: {UserId}", userId);
                return false;
            }
        }

        public async Task<bool> ValidateTokenAsync(string token)
        {
            try
            {
                var tokenHandler = new JwtSecurityTokenHandler();
                var key = Encoding.UTF8.GetBytes(
                    _configuration["JWT:Secret"] ?? 
                    throw new InvalidOperationException("JWT Secret not configured")
                );

                tokenHandler.ValidateToken(token, new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(key),
                    ValidateIssuer = true,
                    ValidIssuer = _configuration["JWT:Issuer"],
                    ValidateAudience = true,
                    ValidAudience = _configuration["JWT:Audience"],
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.Zero
                }, out SecurityToken validatedToken);

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Token validation failed");
                return false;
            }
        }

        public string GenerateJwtToken(UserDto user)
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.UTF8.GetBytes(
                _configuration["JWT:Secret"] ?? 
                throw new InvalidOperationException("JWT Secret not configured")
            );

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Name, user.Username),
                new Claim(ClaimTypes.Email, user.Email),
                new Claim(ClaimTypes.Role, user.Role.ToString()),
                new Claim("userId", user.Id.ToString()),
                new Claim("username", user.Username),
                new Claim("role", user.Role.ToString()),
                new Claim("isActive", user.IsActive.ToString())
            };

            var expirationMinutes = _configuration.GetValue<int>("JWT:AccessTokenExpirationMinutes", 15);

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(claims),
                Expires = DateTime.UtcNow.AddMinutes(expirationMinutes),
                Issuer = _configuration["JWT:Issuer"],
                Audience = _configuration["JWT:Audience"],
                SigningCredentials = new SigningCredentials(
                    new SymmetricSecurityKey(key), 
                    SecurityAlgorithms.HmacSha256Signature
                )
            };

            var token = tokenHandler.CreateToken(tokenDescriptor);
            return tokenHandler.WriteToken(token);
        }

        public string GenerateRefreshToken()
        {
            var randomNumber = new byte[64];
            using var rng = RandomNumberGenerator.Create();
            rng.GetBytes(randomNumber);
            return Convert.ToBase64String(randomNumber);
        }

        // Helper method to clean up old refresh tokens
        private async Task CleanupOldRefreshTokens(Guid userId)
        {
            try
            {
                var expiredTokens = await _context.RefreshTokens
                    .Where(rt => rt.UserId == userId && (!rt.IsActive))
                    .ToListAsync();

                if (expiredTokens.Any())
                {
                    _context.RefreshTokens.RemoveRange(expiredTokens);
                    _logger.LogInformation("Cleaned up {Count} expired refresh tokens for user: {UserId}", 
                        expiredTokens.Count, userId);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error cleaning up old refresh tokens for user: {UserId}", userId);
            }
        }

        // Method to clean up all expired tokens (can be called by a background service)
        public async Task CleanupAllExpiredTokensAsync()
        {
            try
            {
                var expiredTokens = await _context.RefreshTokens
                    .Where(rt => !rt.IsActive)
                    .ToListAsync();

                if (expiredTokens.Any())
                {
                    _context.RefreshTokens.RemoveRange(expiredTokens);
                    await _context.SaveChangesAsync();
                    _logger.LogInformation("Cleaned up {Count} expired refresh tokens", expiredTokens.Count);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error cleaning up expired refresh tokens");
            }
        }
    }
}