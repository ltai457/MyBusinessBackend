// Services/WarehouseService.cs - UPDATE YOUR EXISTING FILE
using Microsoft.EntityFrameworkCore;
using RadiatorStockAPI.Data;
using RadiatorStockAPI.DTOs.Warehouses;
using RadiatorStockAPI.Models;

namespace RadiatorStockAPI.Services.Warehouses
{
    public class WarehouseService : IWarehouseService
    {
        private readonly RadiatorDbContext _context;

        public WarehouseService(RadiatorDbContext context)
        {
            _context = context;
        }

        // EXISTING METHODS - KEEP THESE
        public async Task<IEnumerable<WarehouseDto>> GetAllWarehousesAsync()
        {
            var warehouses = await _context.Warehouses
                .OrderBy(w => w.Code)
                .ToListAsync();

            return warehouses.Select(w => new WarehouseDto
            {
                Id = w.Id,
                Code = w.Code,
                Name = w.Name,
                Location = w.Location,
                Address = w.Address,
                Phone = w.Phone,
                Email = w.Email,
                CreatedAt = w.CreatedAt,
                UpdatedAt = w.UpdatedAt
            });
        }

        public async Task<Warehouse?> GetWarehouseByCodeAsync(string code)
        {
            return await _context.Warehouses
                .FirstOrDefaultAsync(w => w.Code == code);
        }

        public async Task<bool> WarehouseExistsAsync(string code)
        {
            return await _context.Warehouses
                .AnyAsync(w => w.Code == code);
        }

        // ADD THESE NEW METHODS:
        public async Task<WarehouseDto?> GetWarehouseByIdAsync(Guid id)
        {
            var warehouse = await _context.Warehouses
                .FirstOrDefaultAsync(w => w.Id == id);

            if (warehouse == null)
                return null;

            return new WarehouseDto
            {
                Id = warehouse.Id,
                Code = warehouse.Code,
                Name = warehouse.Name,
                Location = warehouse.Location,
                Address = warehouse.Address,
                Phone = warehouse.Phone,
                Email = warehouse.Email,
                CreatedAt = warehouse.CreatedAt,
                UpdatedAt = warehouse.UpdatedAt
            };
        }

        public async Task<WarehouseDto> CreateWarehouseAsync(CreateWarehouseDto dto)
        {
            var warehouse = new Warehouse
            {
                Id = Guid.NewGuid(),
                Code = dto.Code.ToUpper(),
                Name = dto.Name,
                Location = dto.Location,
                Address = dto.Address,
                Phone = dto.Phone,
                Email = dto.Email,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.Warehouses.Add(warehouse);
            await _context.SaveChangesAsync();

            return new WarehouseDto
            {
                Id = warehouse.Id,
                Code = warehouse.Code,
                Name = warehouse.Name,
                Location = warehouse.Location,
                Address = warehouse.Address,
                Phone = warehouse.Phone,
                Email = warehouse.Email,
                CreatedAt = warehouse.CreatedAt,
                UpdatedAt = warehouse.UpdatedAt
            };
        }

        public async Task<WarehouseDto> UpdateWarehouseAsync(Guid id, UpdateWarehouseDto dto)
        {
            var warehouse = await _context.Warehouses
                .FirstOrDefaultAsync(w => w.Id == id);

            if (warehouse == null)
                throw new ArgumentException($"Warehouse with ID {id} not found");

            warehouse.Code = dto.Code.ToUpper();
            warehouse.Name = dto.Name;
            warehouse.Location = dto.Location;
            warehouse.Address = dto.Address;
            warehouse.Phone = dto.Phone;
            warehouse.Email = dto.Email;
            warehouse.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return new WarehouseDto
            {
                Id = warehouse.Id,
                Code = warehouse.Code,
                Name = warehouse.Name,
                Location = warehouse.Location,
                Address = warehouse.Address,
                Phone = warehouse.Phone,
                Email = warehouse.Email,
                CreatedAt = warehouse.CreatedAt,
                UpdatedAt = warehouse.UpdatedAt
            };
        }

        public async Task<bool> DeleteWarehouseAsync(Guid id)
        {
            var warehouse = await _context.Warehouses
                .FirstOrDefaultAsync(w => w.Id == id);

            if (warehouse == null)
                return false;

            _context.Warehouses.Remove(warehouse);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> HasStockLevelsAsync(Guid warehouseId)
        {
            return await _context.StockLevels
                .AnyAsync(sl => sl.WarehouseId == warehouseId && sl.Quantity > 0);
        }
    }
}
