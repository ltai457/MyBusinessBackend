// Data/SeedData.cs
using Microsoft.EntityFrameworkCore;
using RadiatorStockAPI.Models;

namespace RadiatorStockAPI.Data
{
    public static class SeedData
    {
        public static async Task Initialize(RadiatorDbContext context)
        {
            // Seed Warehouses
            if (!await context.Warehouses.AnyAsync())
            {
                var warehouses = new[]
                {
                    new Warehouse 
                    { 
                        Id = Guid.NewGuid(),
                        Code = "WH_AKL", 
                        Name = "Auckland Main Warehouse",
                        CreatedAt = DateTime.UtcNow
                    },
                    new Warehouse 
                    { 
                        Id = Guid.NewGuid(),
                        Code = "WH_CHC", 
                        Name = "Christchurch Warehouse",
                        CreatedAt = DateTime.UtcNow
                    },
                    new Warehouse 
                    { 
                        Id = Guid.NewGuid(),
                        Code = "WH_WLG", 
                        Name = "Wellington Warehouse",
                        CreatedAt = DateTime.UtcNow
                    }
                };
                
                context.Warehouses.AddRange(warehouses);
                await context.SaveChangesAsync();
                Console.WriteLine("✅ Warehouses seeded successfully");
            }
            
            // Seed Default Users
            if (!await context.Users.AnyAsync())
            {
                var users = new[]
                {
                    new User
                    {
                        Id = Guid.NewGuid(),
                        Username = "admin",
                        Email = "admin@radiatorstock.com",
                        PasswordHash = BCrypt.Net.BCrypt.HashPassword("Admin123!"),
                        Role = UserRole.Admin,
                        IsActive = true,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    },
                    new User
                    {
                        Id = Guid.NewGuid(),
                        Username = "staff1",
                        Email = "staff1@radiatorstock.com",
                        PasswordHash = BCrypt.Net.BCrypt.HashPassword("Staff123!"),
                        Role = UserRole.Staff,
                        IsActive = true,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    }
                };
                
                context.Users.AddRange(users);
                await context.SaveChangesAsync();
                Console.WriteLine("✅ Default users seeded successfully");
                Console.WriteLine("   👑 Admin: username='admin', password='Admin123!'");
                Console.WriteLine("   👤 Staff: username='staff1', password='Staff123!'");
            }
        }
    }
}