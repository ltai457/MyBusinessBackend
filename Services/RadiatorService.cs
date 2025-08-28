// Services/RadiatorService.cs
using Microsoft.EntityFrameworkCore;
using RadiatorStockAPI.Data;
using RadiatorStockAPI.DTOs;
using RadiatorStockAPI.Models;

namespace RadiatorStockAPI.Services
{
    public class RadiatorService : IRadiatorService
    {
        private readonly RadiatorDbContext _context;
        private readonly IStockService _stockService;

        public RadiatorService(RadiatorDbContext context, IStockService stockService)
        {
            _context = context;
            _stockService = stockService;
        }

        public async Task<RadiatorResponseDto?> CreateRadiatorAsync(CreateRadiatorDto dto)
        {
            // Check if code already exists
            if (await CodeExistsAsync(dto.Code))
            {
                return null; // Code already exists
            }

            var radiator = new Radiator
            {
                Id = Guid.NewGuid(),
                Brand = dto.Brand,
                Code = dto.Code,
                Name = dto.Name,
                Year = dto.Year,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.Radiators.Add(radiator);

            // Initialize stock levels for all warehouses
            var warehouses = await _context.Warehouses.ToListAsync();
            foreach (var warehouse in warehouses)
            {
                var stockLevel = new StockLevel
                {
                    Id = Guid.NewGuid(),
                    RadiatorId = radiator.Id,
                    WarehouseId = warehouse.Id,
                    Quantity = 0,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };
                _context.StockLevels.Add(stockLevel);
            }

            await _context.SaveChangesAsync();

            // Get stock dictionary for response
            var stock = await _stockService.GetStockDictionaryAsync(radiator.Id);

            return new RadiatorResponseDto
            {
                Id = radiator.Id,
                Brand = radiator.Brand,
                Code = radiator.Code,
                Name = radiator.Name,
                Year = radiator.Year,
                CreatedAt = radiator.CreatedAt,
                UpdatedAt = radiator.UpdatedAt,
                Stock = stock
            };
        }

        public async Task<IEnumerable<RadiatorListDto>> GetAllRadiatorsAsync()
        {
            var radiators = await _context.Radiators
                .Include(r => r.StockLevels)
                    .ThenInclude(sl => sl.Warehouse)
                .OrderBy(r => r.Brand)
                .ThenBy(r => r.Name)
                .ToListAsync();

            var result = new List<RadiatorListDto>();
            
            foreach (var radiator in radiators)
            {
                var stock = radiator.StockLevels.ToDictionary(
                    sl => sl.Warehouse.Code,
                    sl => sl.Quantity
                );

                result.Add(new RadiatorListDto
                {
                    Id = radiator.Id,
                    Brand = radiator.Brand,
                    Code = radiator.Code,
                    Name = radiator.Name,
                    Year = radiator.Year,
                    Stock = stock
                });
            }

            return result;
        }

        public async Task<RadiatorResponseDto?> GetRadiatorByIdAsync(Guid id)
        {
            var radiator = await _context.Radiators
                .Include(r => r.StockLevels)
                    .ThenInclude(sl => sl.Warehouse)
                .FirstOrDefaultAsync(r => r.Id == id);

            if (radiator == null)
                return null;

            var stock = radiator.StockLevels.ToDictionary(
                sl => sl.Warehouse.Code,
                sl => sl.Quantity
            );

            return new RadiatorResponseDto
            {
                Id = radiator.Id,
                Brand = radiator.Brand,
                Code = radiator.Code,
                Name = radiator.Name,
                Year = radiator.Year,
                CreatedAt = radiator.CreatedAt,
                UpdatedAt = radiator.UpdatedAt,
                Stock = stock
            };
        }

        public async Task<RadiatorResponseDto?> UpdateRadiatorAsync(Guid id, UpdateRadiatorDto dto)
        {
            var radiator = await _context.Radiators.FindAsync(id);
            if (radiator == null)
                return null;

            radiator.Brand = dto.Brand;
            radiator.Name = dto.Name;
            radiator.Year = dto.Year;
            radiator.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            // Get updated radiator with stock
            return await GetRadiatorByIdAsync(id);
        }

        public async Task<bool> DeleteRadiatorAsync(Guid id)
        {
            var radiator = await _context.Radiators.FindAsync(id);
            if (radiator == null)
                return false;

            _context.Radiators.Remove(radiator);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> RadiatorExistsAsync(Guid id)
        {
            return await _context.Radiators.AnyAsync(r => r.Id == id);
        }

        public async Task<bool> CodeExistsAsync(string code, Guid? excludeId = null)
        {
            var query = _context.Radiators.Where(r => r.Code == code);
            if (excludeId.HasValue)
                query = query.Where(r => r.Id != excludeId.Value);
                
            return await query.AnyAsync();
        }
    }
}