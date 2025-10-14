using Microsoft.EntityFrameworkCore;
using RadiatorStockAPI.Data;
using RadiatorStockAPI.DTOs.Stock;
using RadiatorStockAPI.DTOs.Sales;
using RadiatorStockAPI.DTOs.Customers;
using RadiatorStockAPI.DTOs.Users;
using RadiatorStockAPI.DTOs.Warehouses;
using RadiatorStockAPI.DTOs.Radiators;
using RadiatorStockAPI.Models;
using RadiatorStockAPI.Services.Customers;
using RadiatorStockAPI.Services.Stock;
using RadiatorStockAPI.Services.Users;

namespace RadiatorStockAPI.Services.Sales
{
    public class SalesService : ISalesService
    {
        private readonly RadiatorDbContext _context;
        private readonly IStockService _stockService;
        private readonly IUserService _userService;
        private readonly ICustomerService _customerService;

        public SalesService(RadiatorDbContext context, IStockService stockService,
            IUserService userService, ICustomerService customerService)
        {
            _context = context;
            _stockService = stockService;
            _userService = userService;
            _customerService = customerService;
        }

        public async Task<SaleResponseDto?> CreateSaleAsync(CreateSaleDto dto, Guid userId)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                // Validate customer exists
                if (!await _customerService.CustomerExistsAsync(dto.CustomerId))
                    return null;

                // Validate stock availability for all items
                foreach (var item in dto.Items)
                {
                    var stockDict = await _stockService.GetStockDictionaryAsync(item.RadiatorId);
                    var warehouse = await _context.Warehouses.FindAsync(item.WarehouseId);

                    if (warehouse == null || !stockDict.ContainsKey(warehouse.Code) ||
                        stockDict[warehouse.Code] < item.Quantity)
                    {
                        return null; // Insufficient stock
                    }
                }

                var sale = new Sale
                {
                    Id = Guid.NewGuid(),
                    CustomerId = dto.CustomerId,
                    UserId = userId,
                    SaleNumber = GenerateSaleNumber(),
                    PaymentMethod = dto.PaymentMethod,
                    Status = SaleStatus.Completed,
                    Notes = dto.Notes,
                    SaleDate = DateTime.UtcNow,
                    CreatedAt = DateTime.UtcNow
                };

                decimal subTotal = 0;
                var saleItems = new List<SaleItem>();

                foreach (var itemDto in dto.Items)
                {
                    var totalPrice = itemDto.UnitPrice * itemDto.Quantity;
                    subTotal += totalPrice;

                    var saleItem = new SaleItem
                    {
                        Id = Guid.NewGuid(),
                        SaleId = sale.Id,
                        RadiatorId = itemDto.RadiatorId,
                        WarehouseId = itemDto.WarehouseId,
                        Quantity = itemDto.Quantity,
                        UnitPrice = itemDto.UnitPrice,
                        TotalPrice = totalPrice,
                        CreatedAt = DateTime.UtcNow
                    };
                    saleItems.Add(saleItem);

                    // Update stock levels
                    // Update stock levels
                    var warehouse = await _context.Warehouses.FindAsync(itemDto.WarehouseId);
                    var stockLevel = await _context.StockLevels
                        .FirstOrDefaultAsync(sl =>
                            sl.RadiatorId == itemDto.RadiatorId &&
                            sl.WarehouseId == itemDto.WarehouseId);

                    if (stockLevel != null)
                    {
                        int oldQuantity = stockLevel.Quantity;
                        stockLevel.Quantity -= itemDto.Quantity;
                        stockLevel.UpdatedAt = DateTime.UtcNow;

                        // Log stock movement
                        var stockHistory = new StockHistory
                        {
                            Id = Guid.NewGuid(),
                            RadiatorId = itemDto.RadiatorId,
                            WarehouseId = itemDto.WarehouseId,
                            OldQuantity = oldQuantity,
                            NewQuantity = stockLevel.Quantity,
                            QuantityChange = -itemDto.Quantity,
                            MovementType = "OUTGOING",
                            ChangeType = "Sale",
                            SaleId = sale.Id,
                            CreatedAt = DateTime.UtcNow
                        };
                        _context.StockHistories.Add(stockHistory);
                    }
                }

                // Calculate tax (15% GST for New Zealand)
                var taxAmount = subTotal * 0.15m;
                var totalAmount = subTotal + taxAmount;

                sale.SubTotal = subTotal;
                sale.TaxAmount = taxAmount;
                sale.TotalAmount = totalAmount;

                _context.Sales.Add(sale);
                _context.SaleItems.AddRange(saleItems);

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                return await GetSaleByIdAsync(sale.Id);
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        public async Task<IEnumerable<SaleListDto>> GetAllSalesAsync()
        {
            var sales = await _context.Sales
                .Include(s => s.Customer)
                .Include(s => s.ProcessedBy)
                .Include(s => s.SaleItems)
                .OrderByDescending(s => s.SaleDate)
                .ToListAsync();

            return sales.Select(s => new SaleListDto
            {
                Id = s.Id,
                SaleNumber = s.SaleNumber,
                CustomerName = $"{s.Customer.FirstName} {s.Customer.LastName}",
                ProcessedByName = s.ProcessedBy.Username,
                TotalAmount = s.TotalAmount,
                PaymentMethod = s.PaymentMethod,
                Status = s.Status,
                SaleDate = s.SaleDate,
                ItemCount = s.SaleItems.Count
            });
        }

        public async Task<SaleResponseDto?> GetSaleByIdAsync(Guid id)
        {
            var sale = await _context.Sales
                .Include(s => s.Customer)
                .Include(s => s.ProcessedBy)
                .Include(s => s.SaleItems)
                    .ThenInclude(si => si.Radiator)
                .Include(s => s.SaleItems)
                    .ThenInclude(si => si.Warehouse)
                .FirstOrDefaultAsync(s => s.Id == id);

            if (sale == null)
                return null;

            var userDto = await _userService.GetUserDtoAsync(sale.ProcessedBy);
            var customerDto = new CustomerListDto
            {
                Id = sale.Customer.Id,
                FirstName = sale.Customer.FirstName,
                LastName = sale.Customer.LastName,
                Email = sale.Customer.Email,
                Phone = sale.Customer.Phone,
                Company = sale.Customer.Company,
                IsActive = sale.Customer.IsActive
            };

            return new SaleResponseDto
            {
                Id = sale.Id,
                SaleNumber = sale.SaleNumber,
                Customer = customerDto,
                ProcessedBy = userDto!,
                SubTotal = sale.SubTotal,
                TaxAmount = sale.TaxAmount,
                TotalAmount = sale.TotalAmount,
                PaymentMethod = sale.PaymentMethod,
                Status = sale.Status,
                Notes = sale.Notes,
                SaleDate = sale.SaleDate,
                CreatedAt = sale.CreatedAt,
                Items = sale.SaleItems.Select(si => new SaleItemResponseDto
                {
                    Id = si.Id,
                    Radiator = new RadiatorListDto
                    {
                        Id = si.Radiator.Id,
                        Brand = si.Radiator.Brand,
                        Code = si.Radiator.Code,
                        Name = si.Radiator.Name,
                        Year = si.Radiator.Year,
                        Stock = new Dictionary<string, int>() // Empty for now
                    },
                    Warehouse = new WarehouseDto
                    {
                        Id = si.Warehouse.Id,
                        Code = si.Warehouse.Code,
                        Name = si.Warehouse.Name
                    },
                    Quantity = si.Quantity,
                    UnitPrice = si.UnitPrice,
                    TotalPrice = si.TotalPrice
                }).ToList()
            };
        }

        public async Task<ReceiptDto?> GetReceiptAsync(Guid saleId)
        {
            var sale = await GetSaleByIdAsync(saleId);
            if (sale == null)
                return null;

            return new ReceiptDto
            {
                Sale = sale,
                CompanyName = "RadiatorStock NZ",
                CompanyAddress = "123 Main Street, Auckland, New Zealand",
                CompanyPhone = "+64 9 123 4567",
                CompanyEmail = "sales@radiatorstock.co.nz"
            };
        }

        public async Task<bool> CancelSaleAsync(Guid id)
        {
            var sale = await _context.Sales.FindAsync(id);
            if (sale == null || sale.Status != SaleStatus.Completed)
                return false;

            sale.Status = SaleStatus.Cancelled;
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<SaleResponseDto?> RefundSaleAsync(Guid id)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                var sale = await _context.Sales
                    .Include(s => s.SaleItems)
                        .ThenInclude(si => si.Warehouse)
                    .FirstOrDefaultAsync(s => s.Id == id);

                if (sale == null || sale.Status != SaleStatus.Completed)
                    return null;

                // Restore stock levels
                foreach (var item in sale.SaleItems)
                {
                    var currentStock = await _stockService.GetStockDictionaryAsync(item.RadiatorId);
                    var newQuantity = currentStock[item.Warehouse.Code] + item.Quantity;

                    await _stockService.UpdateStockAsync(item.RadiatorId, new UpdateStockDto
                    {
                        WarehouseCode = item.Warehouse.Code,
                        Quantity = newQuantity
                    });
                }

                sale.Status = SaleStatus.Refunded;
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                return await GetSaleByIdAsync(id);
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        public async Task<IEnumerable<SaleListDto>> GetSalesByDateRangeAsync(DateTime fromDate, DateTime toDate)
        {
            var sales = await _context.Sales
                .Include(s => s.Customer)
                .Include(s => s.ProcessedBy)
                .Include(s => s.SaleItems)
                .Where(s => s.SaleDate >= fromDate && s.SaleDate <= toDate)
                .OrderByDescending(s => s.SaleDate)
                .ToListAsync();

            return sales.Select(s => new SaleListDto
            {
                Id = s.Id,
                SaleNumber = s.SaleNumber,
                CustomerName = $"{s.Customer.FirstName} {s.Customer.LastName}",
                ProcessedByName = s.ProcessedBy.Username,
                TotalAmount = s.TotalAmount,
                PaymentMethod = s.PaymentMethod,
                Status = s.Status,
                SaleDate = s.SaleDate,
                ItemCount = s.SaleItems.Count
            });
        }

        public async Task<bool> SaleExistsAsync(Guid id)
        {
            return await _context.Sales.AnyAsync(s => s.Id == id);
        }

        public string GenerateSaleNumber()
        {
            var timestamp = DateTime.UtcNow.ToString("yyyyMMddHHmmss");
            var random = new Random().Next(100, 999);
            return $"RS{timestamp}{random}";
        }

        public async Task<InvoiceResponseDto?> GenerateInvoiceAsync(GenerateInvoiceRequestDto dto, Guid userId)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                var userDto = await _userService.GetUserByIdAsync(userId);
                if (userDto == null)
                {
                    return null;
                }

                if (dto.Items.Count == 0)
                {
                    return null;
                }

                var productItems = dto.Items.Where(i => i.RadiatorId.HasValue).ToList();

                var radiatorIds = productItems
                    .Select(i => i.RadiatorId!.Value)
                    .Distinct()
                    .ToList();

                var warehouseIds = productItems
                    .Select(i => i.WarehouseId)
                    .Where(id => id.HasValue)
                    .Select(id => id!.Value)
                    .Distinct()
                    .ToList();

                var radiators = radiatorIds.Count > 0
                    ? await _context.Radiators
                        .Where(r => radiatorIds.Contains(r.Id))
                        .ToDictionaryAsync(r => r.Id)
                    : new Dictionary<Guid, Radiator>();

                var warehouses = warehouseIds.Count > 0
                    ? await _context.Warehouses
                        .Where(w => warehouseIds.Contains(w.Id))
                        .ToDictionaryAsync(w => w.Id)
                    : new Dictionary<Guid, Warehouse>();

                if (radiators.Count != radiatorIds.Count || warehouses.Count != warehouseIds.Count)
                {
                    return null;
                }

                // Calculate totals
                decimal subTotal = 0m;
                var taxRate = dto.TaxRate;

                // Create Invoice entity
                var invoice = new Invoice
                {
                    Id = Guid.NewGuid(),
                    InvoiceNumber = GenerateInvoiceNumber(),
                    UserId = userId,
                    CustomerFullName = dto.Customer.FullName,
                    CustomerEmail = dto.Customer.Email,
                    CustomerPhone = dto.Customer.Phone,
                    CustomerCompany = dto.Customer.Company,
                    CustomerAddress = dto.Customer.Address,
                    PaymentMethod = dto.PaymentMethod,
                    Notes = dto.Notes,
                    TaxRate = taxRate,
                    Status = InvoiceStatus.Issued,
                    IssueDate = DateTime.UtcNow,
                    CreatedAt = DateTime.UtcNow
                };

                var invoiceItems = new List<InvoiceItem>();
                var invoiceItemDtos = new List<InvoiceItemDto>();

                foreach (var item in dto.Items)
                {
                    if (item.RadiatorId.HasValue)
                    {
                        if (!item.WarehouseId.HasValue)
                        {
                            return null;
                        }

                        var radiator = radiators[item.RadiatorId.Value];
                        var warehouse = warehouses[item.WarehouseId.Value];

                        var unitPrice = item.UnitPrice ?? radiator.RetailPrice;
                        var totalPrice = unitPrice * item.Quantity;
                        subTotal += totalPrice;

                        var invoiceItem = new InvoiceItem
                        {
                            Id = Guid.NewGuid(),
                            InvoiceId = invoice.Id,
                            RadiatorId = radiator.Id,
                            WarehouseId = warehouse.Id,
                            Description = item.Description?.Trim() ?? radiator.Name,
                            RadiatorCode = radiator.Code,
                            RadiatorName = radiator.Name,
                            Brand = radiator.Brand,
                            WarehouseCode = warehouse.Code,
                            WarehouseName = warehouse.Name,
                            IsCustomItem = false,
                            Quantity = item.Quantity,
                            UnitPrice = unitPrice,
                            TotalPrice = totalPrice,
                            CreatedAt = DateTime.UtcNow
                        };

                        invoiceItems.Add(invoiceItem);

                        invoiceItemDtos.Add(new InvoiceItemDto
                        {
                            RadiatorId = radiator.Id,
                            RadiatorCode = radiator.Code,
                            RadiatorName = radiator.Name,
                            Brand = radiator.Brand,
                            WarehouseId = warehouse.Id,
                            WarehouseCode = warehouse.Code,
                            WarehouseName = warehouse.Name,
                            Description = item.Description?.Trim() ?? radiator.Name,
                            IsCustomItem = false,
                            Quantity = item.Quantity,
                            UnitPrice = unitPrice,
                            TotalPrice = totalPrice
                        });
                    }
                    else
                    {
                        if (string.IsNullOrWhiteSpace(item.Description))
                        {
                            return null;
                        }

                        if (!item.UnitPrice.HasValue)
                        {
                            return null;
                        }

                        var unitPrice = item.UnitPrice.Value;
                        var totalPrice = unitPrice * item.Quantity;
                        subTotal += totalPrice;

                        var invoiceItem = new InvoiceItem
                        {
                            Id = Guid.NewGuid(),
                            InvoiceId = invoice.Id,
                            RadiatorId = null,
                            WarehouseId = null,
                            Description = item.Description!.Trim(),
                            RadiatorCode = null,
                            RadiatorName = null,
                            Brand = null,
                            WarehouseCode = null,
                            WarehouseName = null,
                            IsCustomItem = true,
                            Quantity = item.Quantity,
                            UnitPrice = unitPrice,
                            TotalPrice = totalPrice,
                            CreatedAt = DateTime.UtcNow
                        };

                        invoiceItems.Add(invoiceItem);

                        invoiceItemDtos.Add(new InvoiceItemDto
                        {
                            RadiatorId = null,
                            RadiatorCode = string.Empty,
                            RadiatorName = string.Empty,
                            Brand = string.Empty,
                            WarehouseId = null,
                            WarehouseCode = string.Empty,
                            WarehouseName = string.Empty,
                            Description = item.Description!.Trim(),
                            IsCustomItem = true,
                            Quantity = item.Quantity,
                            UnitPrice = unitPrice,
                            TotalPrice = totalPrice
                        });
                    }
                }

                var taxAmount = Math.Round(subTotal * taxRate, 2, MidpointRounding.AwayFromZero);
                var totalAmount = subTotal + taxAmount;

                invoice.SubTotal = subTotal;
                invoice.TaxAmount = taxAmount;
                invoice.TotalAmount = totalAmount;

                // Save to database
                _context.Invoices.Add(invoice);
                _context.InvoiceItems.AddRange(invoiceItems);
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                var response = new InvoiceResponseDto
                {
                    InvoiceNumber = invoice.InvoiceNumber,
                    IssueDate = invoice.IssueDate,
                    Customer = new InvoiceCustomerDto
                    {
                        FullName = dto.Customer.FullName,
                        Email = dto.Customer.Email,
                        Phone = dto.Customer.Phone,
                        Company = dto.Customer.Company,
                        Address = dto.Customer.Address
                    },
                    ProcessedBy = userDto,
                    PaymentMethod = dto.PaymentMethod,
                    Notes = dto.Notes,
                    TaxRate = taxRate,
                    SubTotal = subTotal,
                    TaxAmount = taxAmount,
                    TotalAmount = totalAmount,
                    Items = invoiceItemDtos
                };

                return response;
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        private static string GenerateInvoiceNumber()
        {
            var timestamp = DateTime.UtcNow.ToString("yyyyMMddHHmmss");
            var random = new Random().Next(100, 999);
            return $"INV{timestamp}{random}";
        }

        public async Task<IEnumerable<InvoiceResponseDto>> GetAllInvoicesAsync()
        {
            var invoices = await _context.Invoices
                .Include(i => i.CreatedBy)
                .Include(i => i.InvoiceItems)
                    .ThenInclude(ii => ii.Radiator)
                .Include(i => i.InvoiceItems)
                    .ThenInclude(ii => ii.Warehouse)
                .OrderByDescending(i => i.IssueDate)
                .ToListAsync();

            return invoices.Select(MapInvoiceToDto);
        }

        public async Task<InvoiceResponseDto?> GetInvoiceByIdAsync(Guid id)
        {
            var invoice = await _context.Invoices
                .Include(i => i.CreatedBy)
                .Include(i => i.InvoiceItems)
                    .ThenInclude(ii => ii.Radiator)
                .Include(i => i.InvoiceItems)
                    .ThenInclude(ii => ii.Warehouse)
                .FirstOrDefaultAsync(i => i.Id == id);

            if (invoice == null)
                return null;

            return MapInvoiceToDto(invoice);
        }

        public async Task<InvoiceResponseDto?> GetInvoiceByNumberAsync(string invoiceNumber)
        {
            var invoice = await _context.Invoices
                .Include(i => i.CreatedBy)
                .Include(i => i.InvoiceItems)
                    .ThenInclude(ii => ii.Radiator)
                .Include(i => i.InvoiceItems)
                    .ThenInclude(ii => ii.Warehouse)
                .FirstOrDefaultAsync(i => i.InvoiceNumber == invoiceNumber);

            if (invoice == null)
                return null;

            return MapInvoiceToDto(invoice);
        }

        private InvoiceResponseDto MapInvoiceToDto(Invoice invoice)
        {
            var userDto = _userService.GetUserDtoAsync(invoice.CreatedBy).Result;

            return new InvoiceResponseDto
            {
                InvoiceNumber = invoice.InvoiceNumber,
                IssueDate = invoice.IssueDate,
                Customer = new InvoiceCustomerDto
                {
                    FullName = invoice.CustomerFullName,
                    Email = invoice.CustomerEmail,
                    Phone = invoice.CustomerPhone,
                    Company = invoice.CustomerCompany,
                    Address = invoice.CustomerAddress
                },
                ProcessedBy = userDto!,
                PaymentMethod = invoice.PaymentMethod,
                Notes = invoice.Notes,
                TaxRate = invoice.TaxRate,
                SubTotal = invoice.SubTotal,
                TaxAmount = invoice.TaxAmount,
                TotalAmount = invoice.TotalAmount,
                Items = invoice.InvoiceItems.Select(ii => new InvoiceItemDto
                {
                    RadiatorId = ii.RadiatorId,
                    RadiatorCode = ii.RadiatorCode ?? string.Empty,
                    RadiatorName = ii.RadiatorName ?? string.Empty,
                    Brand = ii.Brand ?? string.Empty,
                    WarehouseId = ii.WarehouseId,
                    WarehouseCode = ii.WarehouseCode ?? string.Empty,
                    WarehouseName = ii.WarehouseName ?? string.Empty,
                    Description = ii.Description,
                    IsCustomItem = ii.IsCustomItem,
                    Quantity = ii.Quantity,
                    UnitPrice = ii.UnitPrice,
                    TotalPrice = ii.TotalPrice
                }).ToList()
            };
        }
    }
}
