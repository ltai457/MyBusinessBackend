using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using BCrypt.Net;
using RadiatorStockAPI.Data;
using RadiatorStockAPI.DTOs.Auth;
using RadiatorStockAPI.DTOs.Users;
using RadiatorStockAPI.Models;
using RadiatorStockAPI.Services.Users;

namespace RadiatorStockAPI.Services.Auth;

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

    public async Task<AuthResponseDto?> LoginAsync(LoginRequestDto loginRequest)
    {
        try
        {
            _logger.LogInformation("Login attempt for user: {Username}", loginRequest.Username);

            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Username == loginRequest.Username && u.IsActive);

            if (user == null)
            {
                _logger.LogWarning("Login failed - user not found: {Username}", loginRequest.Username);
                return null;
            }

            if (!BCrypt.Net.BCrypt.Verify(loginRequest.Password, user.PasswordHash))
            {
                _logger.LogWarning("Login failed - invalid password for user: {Username}", loginRequest.Username);
                return null;
            }

            user.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            var userDto = await _userService.GetUserDtoAsync(user);
            var accessToken = GenerateJwtToken(userDto);
            var refreshToken = GenerateRefreshToken();

            await CleanupOldRefreshTokens(user.Id);

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

            _logger.LogInformation("Login successful for user: {Username}", loginRequest.Username);

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
            _logger.LogError(ex, "Error during login for user: {Username}", loginRequest.Username);
            return null;
        }
    }

    public async Task<AuthResponseDto?> RegisterAsync(RegisterRequestDto registerRequest)
    {
        try
        {
            _logger.LogInformation("Registration attempt for user: {Username}", registerRequest.Username);

            if (await _userService.UsernameExistsAsync(registerRequest.Username))
            {
                _logger.LogWarning("Registration failed - username exists: {Username}", registerRequest.Username);
                return null;
            }

            if (await _userService.EmailExistsAsync(registerRequest.Email))
            {
                _logger.LogWarning("Registration failed - email exists: {Email}", registerRequest.Email);
                return null;
            }

            var createUserDto = new CreateUserDto
            {
                Username = registerRequest.Username,
                Email = registerRequest.Email,
                Password = registerRequest.Password,
                Role = registerRequest.Role
            };

            var userDto = await _userService.CreateUserAsync(createUserDto);
            if (userDto == null)
            {
                _logger.LogError("Failed to create user: {Username}", registerRequest.Username);
                return null;
            }

            var accessToken = GenerateJwtToken(userDto);
            var refreshToken = GenerateRefreshToken();

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

            _logger.LogInformation("Registration successful for user: {Username}", registerRequest.Username);

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
            _logger.LogError(ex, "Error during registration for user: {Username}", registerRequest.Username);
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

            tokenEntity.IsRevoked = true;

            var userDto = await _userService.GetUserDtoAsync(tokenEntity.User);
            var newAccessToken = GenerateJwtToken(userDto);
            var newRefreshToken = GenerateRefreshToken();

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

    public async Task<bool> ChangePasswordAsync(Guid userId, ChangePasswordRequestDto changePasswordRequest)
    {
        try
        {
            var user = await _context.Users.FindAsync(userId);
            if (user == null)
            {
                _logger.LogWarning("Password change failed - user not found: {UserId}", userId);
                return false;
            }

            if (!BCrypt.Net.BCrypt.Verify(changePasswordRequest.CurrentPassword, user.PasswordHash))
            {
                _logger.LogWarning("Password change failed - invalid current password for user: {UserId}", userId);
                return false;
            }

            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(changePasswordRequest.NewPassword);
            user.UpdatedAt = DateTime.UtcNow;

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
            }, out SecurityToken _);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Invalid token");
            return false;
        }
    }

    public string GenerateJwtToken(UserDto user)
    {
        var key = Encoding.UTF8.GetBytes(
            _configuration["JWT:Secret"] ??
            throw new InvalidOperationException("JWT Secret not configured")
        );

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Name, user.Username),
            new(ClaimTypes.Email, user.Email),
            new(ClaimTypes.Role, user.Role.ToString())
        };

        if (!string.IsNullOrWhiteSpace(user.FirstName) || !string.IsNullOrWhiteSpace(user.LastName))
        {
            claims.Add(new Claim("fullname", $"{user.FirstName} {user.LastName}".Trim()));
        }

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Expires = DateTime.UtcNow.AddMinutes(
                _configuration.GetValue<int>("JWT:AccessTokenExpirationMinutes", 15)
            ),
            Issuer = _configuration["JWT:Issuer"],
            Audience = _configuration["JWT:Audience"],
            SigningCredentials = new SigningCredentials(
                new SymmetricSecurityKey(key),
                SecurityAlgorithms.HmacSha256Signature)
        };

        var tokenHandler = new JwtSecurityTokenHandler();
        var token = tokenHandler.CreateToken(tokenDescriptor);
        return tokenHandler.WriteToken(token);
    }

    public string GenerateRefreshToken()
    {
        var randomNumber = new byte[32];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(randomNumber);
        return Convert.ToBase64String(randomNumber);
    }

    private async Task CleanupOldRefreshTokens(Guid userId)
    {
        var tokens = await _context.RefreshTokens
            .Where(rt => rt.UserId == userId)
            .OrderByDescending(rt => rt.CreatedAt)
            .Skip(5)
            .ToListAsync();

        if (tokens.Count > 0)
        {
            _context.RefreshTokens.RemoveRange(tokens);
            await _context.SaveChangesAsync();
        }
    }
}
