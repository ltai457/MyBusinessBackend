// Services/UserService.cs
// REPLACE YOUR EXISTING FILE with this updated version

using Microsoft.EntityFrameworkCore;
using BCrypt.Net;
using RadiatorStockAPI.Data;
using RadiatorStockAPI.DTOs.Users;
using RadiatorStockAPI.Models;

namespace RadiatorStockAPI.Services
{
    public class UserService : IUserService
    {
        private readonly RadiatorDbContext _context;

        public UserService(RadiatorDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<UserDto>> GetAllUsersAsync()
        {
            var users = await _context.Users
                .OrderBy(u => u.Username)
                .ToListAsync();

            return users.Select(u => new UserDto
            {
                Id = u.Id,
                Username = u.Username,
                Email = u.Email,
                FirstName = u.FirstName,  // NEW
                LastName = u.LastName,    // NEW
                Role = u.Role,
                IsActive = u.IsActive,
                CreatedAt = u.CreatedAt,
                UpdatedAt = u.UpdatedAt
            });
        }

        public async Task<UserDto?> GetUserByIdAsync(Guid id)
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null)
                return null;

            return await GetUserDtoAsync(user);
        }

        public async Task<UserDto?> CreateUserAsync(CreateUserDto dto)
        {
            // Check if username or email already exists
            if (await UsernameExistsAsync(dto.Username) || await EmailExistsAsync(dto.Email))
                return null;

            var user = new User
            {
                Id = Guid.NewGuid(),
                Username = dto.Username,
                Email = dto.Email,
                FirstName = dto.FirstName,  // NEW
                LastName = dto.LastName,    // NEW
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password),
                Role = dto.Role,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            return await GetUserDtoAsync(user);
        }

        public async Task<UserDto?> UpdateUserAsync(Guid id, UpdateUserDto dto)
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null)
                return null;

            // Check if username or email conflicts with other users
            if (await UsernameExistsAsync(dto.Username, id) || await EmailExistsAsync(dto.Email, id))
                return null;

            user.Username = dto.Username;
            user.Email = dto.Email;
            user.FirstName = dto.FirstName;  // NEW
            user.LastName = dto.LastName;    // NEW
            user.Role = dto.Role;
            user.IsActive = dto.IsActive;
            user.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return await GetUserDtoAsync(user);
        }

        public async Task<bool> DeleteUserAsync(Guid id)
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null)
                return false;

            _context.Users.Remove(user);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> UserExistsAsync(Guid id)
        {
            return await _context.Users.AnyAsync(u => u.Id == id);
        }

        public async Task<bool> UsernameExistsAsync(string username, Guid? excludeId = null)
        {
            var query = _context.Users.Where(u => u.Username == username);
            if (excludeId.HasValue)
                query = query.Where(u => u.Id != excludeId.Value);
                
            return await query.AnyAsync();
        }

        public async Task<bool> EmailExistsAsync(string email, Guid? excludeId = null)
        {
            var query = _context.Users.Where(u => u.Email == email);
            if (excludeId.HasValue)
                query = query.Where(u => u.Id != excludeId.Value);
                
            return await query.AnyAsync();
        }

        public async Task<UserDto> GetUserDtoAsync(User user)
        {
            return new UserDto
            {
                Id = user.Id,
                Username = user.Username,
                Email = user.Email,
                FirstName = user.FirstName,  // NEW
                LastName = user.LastName,    // NEW
                Role = user.Role,
                IsActive = user.IsActive,
                CreatedAt = user.CreatedAt,
                UpdatedAt = user.UpdatedAt
            };
        }
    }
}
