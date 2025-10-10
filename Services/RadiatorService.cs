// Services/RadiatorService.cs
using Microsoft.EntityFrameworkCore;
using RadiatorStockAPI.Data;
using RadiatorStockAPI.DTOs.Radiators;
using RadiatorStockAPI.Models;

namespace RadiatorStockAPI.Services
{
    public class RadiatorService : IRadiatorService
    {
        private readonly RadiatorDbContext _context;
        private readonly IStockService _stockService;
        private readonly IWarehouseService _warehouseService;
        private readonly IS3Service _s3Service;

        public RadiatorService(
            RadiatorDbContext context,
            IStockService stockService,
            IWarehouseService warehouseService,
            IS3Service s3Service)
        {
            _context = context;
            _stockService = stockService;
            _warehouseService = warehouseService;
            _s3Service = s3Service;
        }

        // -----------------------------
        // Helpers (pure mapping)
        // -----------------------------
        private static Dictionary<string, int> BuildStockDictInMemory(IEnumerable<StockLevel> stockLevels)
        {
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
                IsPriceOverridable = r.IsPriceOverridable,
                MaxDiscountPercent = r.MaxDiscountPercent,
                Stock = BuildStockDictInMemory(r.StockLevels),

                // NEW FIELDS ADDED
                ProductType = r.ProductType,
                Dimensions = r.Dimensions,
                Notes = r.Notes,

                // Add image properties
                PrimaryImageUrl = r.Images?.FirstOrDefault(img => img.IsPrimary)?.Url,
                ImageCount = r.Images?.Count ?? 0
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
                Stock = BuildStockDictInMemory(r.StockLevels),

                // NEW FIELDS ADDED
                ProductType = r.ProductType,
                Dimensions = r.Dimensions,
                Notes = r.Notes,

                // Add image properties
                HasImage = r.Images?.Any() ?? false,
                ImageUrl = r.Images?.FirstOrDefault(img => img.IsPrimary)?.Url,
                ImageCount = r.Images?.Count ?? 0,
                CreatedAt = r.CreatedAt,
                UpdatedAt = r.UpdatedAt
            };
        }

        // -----------------------------
        // CRUD
        // -----------------------------
        public async Task<RadiatorResponseDto?> CreateRadiatorAsync(CreateRadiatorDto dto)
        {
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
                TradePrice = dto.TradePrice,
                CostPrice = dto.CostPrice,
                IsPriceOverridable = dto.IsPriceOverridable,
                MaxDiscountPercent = dto.MaxDiscountPercent,
                
                // NEW FIELDS ADDED
                ProductType = dto.ProductType,
                Dimensions = dto.Dimensions,
                Notes = dto.Notes,
                
                CreatedAt = now,
                UpdatedAt = now
            };

            _context.Radiators.Add(radiator);

            // Initialize stock for all warehouses as 0
            var warehouses = await _warehouseService.GetAllWarehousesAsync();
            foreach (var wh in warehouses)
            {
                _context.StockLevels.Add(new StockLevel
                {
                    Id = Guid.NewGuid(),
                    RadiatorId = radiator.Id,
                    WarehouseId = wh.Id,
                    Quantity = 0,
                    CreatedAt = now,
                    UpdatedAt = now
                });
            }

            await _context.SaveChangesAsync();

            // Load with stock, warehouses, and images
            var created = await _context.Radiators
                .AsNoTracking()
                .Include(r => r.StockLevels).ThenInclude(sl => sl.Warehouse)
                .Include(r => r.Images)
                .FirstAsync(r => r.Id == radiator.Id);

            return ToResponseDto(created);
        }

        // NEW: Create radiator with image
        public async Task<RadiatorResponseDto?> CreateRadiatorWithImageAsync(string brand, string code, string name, int year, decimal retailPrice, IFormFile? image)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                // Check for duplicate code first
                if (await CodeExistsAsync(code))
                    return null;

                var now = DateTime.UtcNow;

                // Create radiator entity
                var radiator = new Radiator
                {
                    Id = Guid.NewGuid(),
                    Brand = brand,
                    Code = code,
                    Name = name,
                    Year = year,
                    RetailPrice = retailPrice,
                    IsPriceOverridable = true,
                    MaxDiscountPercent = 20,
                    CreatedAt = now,
                    UpdatedAt = now
                };

                _context.Radiators.Add(radiator);

                // Initialize stock for all warehouses
                var warehouses = await _warehouseService.GetAllWarehousesAsync();
                foreach (var wh in warehouses)
                {
                    _context.StockLevels.Add(new StockLevel
                    {
                        Id = Guid.NewGuid(),
                        RadiatorId = radiator.Id,
                        WarehouseId = wh.Id,
                        Quantity = 0,
                        CreatedAt = now,
                        UpdatedAt = now
                    });
                }

                await _context.SaveChangesAsync();

                // If image provided, upload and save it
                if (image != null)
                {
                    var imageUrl = await _s3Service.UploadImageAsync(image);

                    var radiatorImage = new RadiatorImage
                    {
                        Id = Guid.NewGuid(),
                        RadiatorId = radiator.Id,
                        FileName = image.FileName,
                        S3Key = imageUrl.Split('/').Last(),
                        Url = imageUrl,
                        IsPrimary = true,
                        CreatedAt = now
                    };

                    _context.RadiatorImages.Add(radiatorImage);
                    await _context.SaveChangesAsync();
                }

                await transaction.CommitAsync();
                return await GetRadiatorByIdAsync(radiator.Id);
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        public async Task<List<RadiatorListDto>> GetAllRadiatorsAsync()
        {
            var entities = await _context.Radiators
                .AsNoTracking()
                .Include(r => r.StockLevels).ThenInclude(sl => sl.Warehouse)
                .Include(r => r.Images)
                .OrderBy(r => r.Brand)
                .ThenBy(r => r.Name)
                .ToListAsync();

            return entities.Select(ToListDto).ToList();
        }

        public async Task<RadiatorResponseDto?> GetRadiatorByIdAsync(Guid id)
        {
            var entity = await _context.Radiators
                .AsNoTracking()
                .Include(r => r.StockLevels).ThenInclude(sl => sl.Warehouse)
                .Include(r => r.Images)
                .FirstOrDefaultAsync(r => r.Id == id);

            return entity is null ? null : ToResponseDto(entity);
        }

        public async Task<RadiatorResponseDto?> UpdateRadiatorAsync(Guid id, UpdateRadiatorDto dto)
        {
            var entity = await _context.Radiators
                .Include(r => r.StockLevels).ThenInclude(sl => sl.Warehouse)
                .Include(r => r.Images)
                .FirstOrDefaultAsync(r => r.Id == id);

            if (entity is null) return null;

            // Check for code uniqueness if changing
            if (!string.IsNullOrWhiteSpace(dto.Code) &&
                !dto.Code.Equals(entity.Code, StringComparison.OrdinalIgnoreCase) &&
                await CodeExistsAsync(dto.Code, excludeId: id))
            {
                return null;
            }

            // Apply updates
            entity.Brand = dto.Brand ?? entity.Brand;
            entity.Code = dto.Code ?? entity.Code;
            entity.Name = dto.Name ?? entity.Name;
            if (dto.Year.HasValue) entity.Year = dto.Year.Value;
            if (dto.RetailPrice.HasValue) entity.RetailPrice = dto.RetailPrice.Value;
            if (dto.TradePrice.HasValue) entity.TradePrice = dto.TradePrice.Value;
            if (dto.CostPrice.HasValue) entity.CostPrice = dto.CostPrice.Value;
            if (dto.IsPriceOverridable.HasValue) entity.IsPriceOverridable = dto.IsPriceOverridable.Value;
            if (dto.MaxDiscountPercent.HasValue) entity.MaxDiscountPercent = dto.MaxDiscountPercent.Value;

            // NEW FIELDS ADDED
            entity.ProductType = dto.ProductType ?? entity.ProductType;
            entity.Dimensions = dto.Dimensions ?? entity.Dimensions;
            entity.Notes = dto.Notes ?? entity.Notes;

            entity.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            return ToResponseDto(entity);
        }

        public async Task<bool> DeleteRadiatorAsync(Guid id)
        {
            var entity = await _context.Radiators
                .Include(r => r.Images)
                .FirstOrDefaultAsync(r => r.Id == id);

            if (entity is null) return false;

            // Delete all images from S3 first
            foreach (var image in entity.Images)
            {
                await _s3Service.DeleteImageAsync(image.S3Key);
            }

            _context.Radiators.Remove(entity);
            await _context.SaveChangesAsync();
            return true;
        }

        // -----------------------------
        // Image Management Methods
        // -----------------------------
        public async Task<RadiatorImageDto?> AddImageToRadiatorAsync(Guid radiatorId, UploadRadiatorImageDto dto)
        {
            if (!await RadiatorExistsAsync(radiatorId))
                return null;

            var uploadResult = await _s3Service.UploadImageAsync(dto.Image);

            if (string.IsNullOrEmpty(uploadResult))
                return null;

            var now = DateTime.UtcNow;
            var radiatorImage = new RadiatorImage
            {
                Id = Guid.NewGuid(),
                RadiatorId = radiatorId,
                FileName = dto.Image.FileName,
                S3Key = uploadResult.Split('/').Last(),
                Url = uploadResult,
                IsPrimary = dto.IsPrimary,
                CreatedAt = now
            };

            // If this is set as primary, update other images
            if (dto.IsPrimary)
            {
                await SetAllImagesAsNonPrimary(radiatorId);
            }

            _context.RadiatorImages.Add(radiatorImage);
            await _context.SaveChangesAsync();

            return new RadiatorImageDto
            {
                Id = radiatorImage.Id,
                RadiatorId = radiatorImage.RadiatorId,
                FileName = radiatorImage.FileName,
                S3Key = radiatorImage.S3Key,
                Url = radiatorImage.Url,
                IsPrimary = radiatorImage.IsPrimary,
                CreatedAt = radiatorImage.CreatedAt
            };
        }

        public async Task<List<RadiatorImageDto>> GetRadiatorImagesAsync(Guid radiatorId)
        {
            var images = await _context.RadiatorImages
                .Where(img => img.RadiatorId == radiatorId)
                .OrderByDescending(img => img.IsPrimary)
                .ThenBy(img => img.CreatedAt)
                .ToListAsync();

            return images.Select(img => new RadiatorImageDto
            {
                Id = img.Id,
                RadiatorId = img.RadiatorId,
                FileName = img.FileName,
                S3Key = img.S3Key,
                Url = img.Url,
                IsPrimary = img.IsPrimary,
                CreatedAt = img.CreatedAt
            }).ToList();
        }

        public async Task<bool> DeleteRadiatorImageAsync(Guid radiatorId, Guid imageId)
        {
            var image = await _context.RadiatorImages
                .FirstOrDefaultAsync(img => img.Id == imageId && img.RadiatorId == radiatorId);

            if (image == null)
                return false;

            // Delete from S3
            await _s3Service.DeleteImageAsync(image.S3Key);

            // Delete from database
            _context.RadiatorImages.Remove(image);
            await _context.SaveChangesAsync();

            return true;
        }

        public async Task<bool> SetPrimaryImageAsync(Guid radiatorId, Guid imageId)
        {
            var image = await _context.RadiatorImages
                .FirstOrDefaultAsync(img => img.Id == imageId && img.RadiatorId == radiatorId);

            if (image == null)
                return false;

            // Set all images as non-primary first
            await SetAllImagesAsNonPrimary(radiatorId);

            // Set this image as primary
            image.IsPrimary = true;
            await _context.SaveChangesAsync();
            return true;
        }

        private async Task SetAllImagesAsNonPrimary(Guid radiatorId)
        {
            var images = await _context.RadiatorImages
                .Where(img => img.RadiatorId == radiatorId && img.IsPrimary)
                .ToListAsync();

            foreach (var img in images)
            {
                img.IsPrimary = false;
            }
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
                .Include(r => r.Images)
                .Where(r => ids.Contains(r.Id))
                .ToListAsync();

            foreach (var r in entities)
            {
                var change = updates.FirstOrDefault(u => u.Id == r.Id);
                if (change == null) continue;

                if (change.RetailPrice.HasValue) r.RetailPrice = change.RetailPrice.Value;
                if (change.TradePrice.HasValue) r.TradePrice = change.TradePrice.Value;
                if (change.CostPrice.HasValue) r.CostPrice = change.CostPrice.Value;

                r.UpdatedAt = now;
            }

            await _context.SaveChangesAsync();

            return entities
                .OrderBy(r => r.Brand).ThenBy(r => r.Name)
                .Select(ToListDto)
                .ToList();
        }

        public async Task<RadiatorResponseDto?> CreateRadiatorWithImageAsync(CreateRadiatorWithImageDto dto)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
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
                    TradePrice = dto.TradePrice,
                    CostPrice = dto.CostPrice,
                    IsPriceOverridable = dto.IsPriceOverridable,
                    MaxDiscountPercent = dto.MaxDiscountPercent,

                    // NEW FIELDS ADDED
                    ProductType = dto.ProductType,
                    Dimensions = dto.Dimensions,
                    Notes = dto.Notes,

                    CreatedAt = now,
                    UpdatedAt = now
                };

                _context.Radiators.Add(radiator);

                // Initialize stock
                var warehouses = await _warehouseService.GetAllWarehousesAsync();
                foreach (var wh in warehouses)
                {
                    var qty = 0;
                    if (dto.InitialStock != null && dto.InitialStock.TryGetValue(wh.Code, out var stockQty))
                        qty = stockQty;

                    _context.StockLevels.Add(new StockLevel
                    {
                        Id = Guid.NewGuid(),
                        RadiatorId = radiator.Id,
                        WarehouseId = wh.Id,
                        Quantity = qty,
                        CreatedAt = now,
                        UpdatedAt = now
                    });
                }

                await _context.SaveChangesAsync();

                // Upload image if provided
                if (dto.Image != null)
                {
                    var imageUrl = await _s3Service.UploadImageAsync(dto.Image);

                    var radiatorImage = new RadiatorImage
                    {
                        Id = Guid.NewGuid(),
                        RadiatorId = radiator.Id,
                        FileName = dto.Image.FileName,
                        S3Key = imageUrl.Split('/').Last(),
                        Url = imageUrl,
                        IsPrimary = true,
                        CreatedAt = now
                    };

                    _context.RadiatorImages.Add(radiatorImage);
                    await _context.SaveChangesAsync();
                }

                await transaction.CommitAsync();
                return await GetRadiatorByIdAsync(radiator.Id);
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        // -----------------------------
        // Test method
        // -----------------------------
        public async Task<string> TestS3Async(IFormFile file)
        {
            return await _s3Service.UploadImageAsync(file);
        }
    }
}
