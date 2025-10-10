using System;
using RadiatorStockAPI.DTOs.Users;
using RadiatorStockAPI.Models;

namespace RadiatorStockAPI.Services.Users;

public interface IUserService
{
    Task<IEnumerable<UserDto>> GetAllUsersAsync();
    Task<UserDto?> GetUserByIdAsync(Guid id);
    Task<UserDto?> CreateUserAsync(CreateUserDto dto);
    Task<UserDto?> UpdateUserAsync(Guid id, UpdateUserDto dto);
    Task<bool> DeleteUserAsync(Guid id);
    Task<bool> UserExistsAsync(Guid id);
    Task<bool> UsernameExistsAsync(string username, Guid? excludeId = null);
    Task<bool> EmailExistsAsync(string email, Guid? excludeId = null);
    Task<UserDto> GetUserDtoAsync(User user);
}
