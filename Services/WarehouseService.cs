using Microsoft.EntityFrameworkCore;
using RadiatorStockAPI.Data;
using RadiatorStockAPI.DTOs;
using RadiatorStockAPI.Models;

namespace RadiatorStockAPI.Services
{
    public class WarehouseService : IWarehouseService
    {
        private readonly RadiatorDbContext _context;

        public WarehouseService(RadiatorDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<WarehouseDto>> GetAllWarehousesAsync()
        {
            var warehouses = await _context.Warehouses
                .OrderBy(w => w.Code)
                .ToListAsync();

            return warehouses.Select(w => new WarehouseDto
            {
                Id = w.Id,
                Code = w.Code,
                Name = w.Name
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
    }
}