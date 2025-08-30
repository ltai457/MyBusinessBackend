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
        private readonly IWarehouseService _warehouseService;

        public RadiatorService(
            RadiatorDbContext context,
            IStockService stockService,
            IWarehouseService warehouseService)
        {
            _context = context;
            _stockService = stockService;
            _warehouseService = warehouseService;
        }

        // -----------------------------
        // Helpers (pure mapping)
        // -----------------------------
        private static Dictionary<string, int> BuildStockDictInMemory(IEnumerable<StockLevel> stockLevels)
        {
            // Group/sum just in case duplicates exist, and ignore null warehouses defensively
            return stockLevels
                .Where(sl => sl.Warehouse != null && !string.IsNullOrWhiteSpace(sl.Warehouse.Code))
                .GroupBy(sl => sl.Warehouse.Code!)
                .ToDictionary(g => g.Key, g => g.Sum(x => x.Quantity));
        }

        private static RadiatorListDto ToListDto(Radiator r)
        {
            return new RadiatorListDto
            {
                Id = r.Id,
                Brand = r.Brand,
                Code = r.Code,
                Name = r.Name,
                Year = r.Year,
                RetailPrice = r.RetailPrice,
                TradePrice = r.TradePrice,
                Stock = BuildStockDictInMemory(r.StockLevels)
            };
        }

        private static RadiatorResponseDto ToResponseDto(Radiator r)
        {
            return new RadiatorResponseDto
            {
                Id = r.Id,
                Brand = r.Brand,
                Code = r.Code,
                Name = r.Name,
                Year = r.Year,
                RetailPrice = r.RetailPrice,
                TradePrice = r.TradePrice,
                CostPrice = r.CostPrice,
                IsPriceOverridable = r.IsPriceOverridable,
                MaxDiscountPercent = r.MaxDiscountPercent,
                Stock = BuildStockDictInMemory(r.StockLevels)
            };
        }

        // -----------------------------
        // CRUD
        // -----------------------------
        public async Task<RadiatorResponseDto?> CreateRadiatorAsync(CreateRadiatorDto dto)
        {
            // prevent duplicate code
            if (await CodeExistsAsync(dto.Code))
                return null;

            var now = DateTime.UtcNow;

            var radiator = new Radiator
            {
                Id = Guid.NewGuid(),
                Brand = dto.Brand,
                Code = dto.Code,
                Name = dto.Name,
                Year = dto.Year,

                RetailPrice = dto.RetailPrice,
                TradePrice  = dto.TradePrice,
                CostPrice   = dto.CostPrice,

                IsPriceOverridable = dto.IsPriceOverridable,
                MaxDiscountPercent = dto.MaxDiscountPercent,

                CreatedAt = now,
                UpdatedAt = now
            };

            _context.Radiators.Add(radiator);

            // Initialize stock for all warehouses as 0 (or change if you want different behavior)
            var warehouses = await _warehouseService.GetAllWarehousesAsync();
            foreach (var wh in warehouses)
            {
                _context.StockLevels.Add(new StockLevel
                {
                    Id          = Guid.NewGuid(),
                    RadiatorId  = radiator.Id,
                    WarehouseId = wh.Id,
                    Quantity    = 0,
                    CreatedAt   = now,
                    UpdatedAt   = now
                });
            }

            await _context.SaveChangesAsync();

            // Load with stock + warehouses, then map
            var created = await _context.Radiators
                .AsNoTracking()
                .Include(r => r.StockLevels).ThenInclude(sl => sl.Warehouse)
                .FirstAsync(r => r.Id == radiator.Id);

            return ToResponseDto(created);
        }

        public async Task<List<RadiatorListDto>> GetAllRadiatorsAsync()
        {
            // 1) Run the SQL and materialize first
            var entities = await _context.Radiators
                .AsNoTracking()
                .Include(r => r.StockLevels)
                .ThenInclude(sl => sl.Warehouse)
                .OrderBy(r => r.Brand)
                .ThenBy(r => r.Name)
                .ToListAsync();

            // 2) Build the dictionary on the client (C#)
            var list = entities.Select(r => new RadiatorListDto
            {
                Id = r.Id,
                Brand = r.Brand,
                Code = r.Code,
                Name = r.Name,
                Year = r.Year,

                RetailPrice        = r.RetailPrice,
                TradePrice         = r.TradePrice,
                IsPriceOverridable = r.IsPriceOverridable,
                MaxDiscountPercent = r.MaxDiscountPercent,

                // Safe & robust: ignore null warehouses and combine duplicate codes
                Stock = r.StockLevels
                    .Where(sl => sl.Warehouse != null && !string.IsNullOrWhiteSpace(sl.Warehouse.Code))
                    .GroupBy(sl => sl.Warehouse.Code)
                    .ToDictionary(g => g.Key, g => g.Sum(x => x.Quantity))
            }).ToList();

            return list;
        }

        public async Task<RadiatorResponseDto?> GetRadiatorByIdAsync(Guid id)
        {
            var entity = await _context.Radiators
                .AsNoTracking()
                .Include(r => r.StockLevels).ThenInclude(sl => sl.Warehouse)
                .FirstOrDefaultAsync(r => r.Id == id);

            return entity is null ? null : ToResponseDto(entity);
        }

        public async Task<RadiatorResponseDto?> UpdateRadiatorAsync(Guid id, UpdateRadiatorDto dto)
        {
            var entity = await _context.Radiators
                .Include(r => r.StockLevels).ThenInclude(sl => sl.Warehouse)
                .FirstOrDefaultAsync(r => r.Id == id);

            if (entity is null) return null;

            // If code changes, enforce uniqueness (excluding current id)
            if (!string.IsNullOrWhiteSpace(dto.Code) &&
                !dto.Code.Equals(entity.Code, StringComparison.OrdinalIgnoreCase) &&
                await CodeExistsAsync(dto.Code, excludeId: id))
            {
                return null; // caller can translate this to 409 Conflict
            }

            // Apply updates
            entity.Brand = dto.Brand ?? entity.Brand;
            entity.Code  = dto.Code  ?? entity.Code;
            entity.Name  = dto.Name  ?? entity.Name;
            if (dto.Year.HasValue) entity.Year = dto.Year.Value;

            if (dto.RetailPrice.HasValue) entity.RetailPrice = dto.RetailPrice.Value;
            if (dto.TradePrice.HasValue)  entity.TradePrice  = dto.TradePrice.Value;
            if (dto.CostPrice.HasValue)   entity.CostPrice   = dto.CostPrice.Value;

            if (dto.IsPriceOverridable.HasValue) entity.IsPriceOverridable = dto.IsPriceOverridable.Value;
            if (dto.MaxDiscountPercent.HasValue) entity.MaxDiscountPercent = dto.MaxDiscountPercent.Value;

            entity.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            // Project fresh snapshot
            var updated = await _context.Radiators
                .AsNoTracking()
                .Include(r => r.StockLevels).ThenInclude(sl => sl.Warehouse)
                .FirstAsync(r => r.Id == entity.Id);

            return ToResponseDto(updated);
        }

        public async Task<bool> DeleteRadiatorAsync(Guid id)
        {
            var entity = await _context.Radiators.FirstOrDefaultAsync(r => r.Id == id);
            if (entity is null) return false;

            _context.Radiators.Remove(entity);
            await _context.SaveChangesAsync();
            return true;
        }

        // -----------------------------
        // Existence / Uniqueness
        // -----------------------------
        public Task<bool> RadiatorExistsAsync(Guid id)
            => _context.Radiators.AsNoTracking().AnyAsync(r => r.Id == id);

        public async Task<bool> CodeExistsAsync(string code, Guid? excludeId = null)
        {
            var query = _context.Radiators.AsNoTracking().Where(r => r.Code == code);
            if (excludeId.HasValue) query = query.Where(r => r.Id != excludeId.Value);
            return await query.AnyAsync();
        }

        // -----------------------------
        // Bulk price updates
        // -----------------------------
        public async Task<List<RadiatorListDto>> UpdateMultiplePricesAsync(List<UpdateRadiatorPriceDto> updates)
        {
            if (updates == null || updates.Count == 0)
                return new List<RadiatorListDto>();

            var ids = updates.Select(u => u.Id).Distinct().ToList();
            var now = DateTime.UtcNow;

            var entities = await _context.Radiators
                .Include(r => r.StockLevels).ThenInclude(sl => sl.Warehouse)
                .Where(r => ids.Contains(r.Id))
                .ToListAsync();

            // apply changes
            foreach (var r in entities)
            {
                var change = updates.FirstOrDefault(u => u.Id == r.Id);
                if (change == null) continue;

                if (change.RetailPrice.HasValue) r.RetailPrice = change.RetailPrice.Value;
                if (change.TradePrice.HasValue)  r.TradePrice  = change.TradePrice.Value;
                if (change.CostPrice.HasValue)   r.CostPrice   = change.CostPrice.Value;

                r.UpdatedAt = now;
            }

            await _context.SaveChangesAsync();

            // Map in memory (NO ToDictionary inside EF query)
            // If you want to re-load to ensure no stale navigation: do another Include + ToListAsync() here.
            return entities
                .OrderBy(r => r.Brand).ThenBy(r => r.Name)
                .Select(ToListDto)
                .ToList();
        }
    }
}
