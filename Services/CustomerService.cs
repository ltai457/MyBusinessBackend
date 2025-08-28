using Microsoft.EntityFrameworkCore;
using RadiatorStockAPI.Data;
using RadiatorStockAPI.DTOs;
using RadiatorStockAPI.Models;

namespace RadiatorStockAPI.Services
{
    public class CustomerService : ICustomerService
    {
        private readonly RadiatorDbContext _context;

        public CustomerService(RadiatorDbContext context)
        {
            _context = context;
        }

        public async Task<CustomerResponseDto?> CreateCustomerAsync(CreateCustomerDto dto)
        {
            var customer = new Customer
            {
                Id = Guid.NewGuid(),
                FirstName = dto.FirstName,
                LastName = dto.LastName,
                Email = dto.Email,
                Phone = dto.Phone,
                Company = dto.Company,
                Address = dto.Address,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.Customers.Add(customer);
            await _context.SaveChangesAsync();

            return await GetCustomerResponseAsync(customer);
        }

        public async Task<IEnumerable<CustomerListDto>> GetAllCustomersAsync()
        {
            var customers = await _context.Customers
                .Include(c => c.Sales)
                .Where(c => c.IsActive)
                .OrderBy(c => c.LastName)
                .ThenBy(c => c.FirstName)
                .ToListAsync();

            return customers.Select(c => new CustomerListDto
            {
                Id = c.Id,
                FirstName = c.FirstName,
                LastName = c.LastName,
                Email = c.Email,
                Phone = c.Phone,
                Company = c.Company,
                IsActive = c.IsActive,
                TotalPurchases = c.Sales.Count(s => s.Status == SaleStatus.Completed),
                TotalSpent = c.Sales.Where(s => s.Status == SaleStatus.Completed).Sum(s => s.TotalAmount),
                LastPurchaseDate = c.Sales.Where(s => s.Status == SaleStatus.Completed)
                    .OrderByDescending(s => s.SaleDate)
                    .Select(s => s.SaleDate)
                    .FirstOrDefault()
            });
        }

        public async Task<CustomerResponseDto?> GetCustomerByIdAsync(Guid id)
        {
            var customer = await _context.Customers
                .Include(c => c.Sales)
                .FirstOrDefaultAsync(c => c.Id == id);

            if (customer == null)
                return null;

            return await GetCustomerResponseAsync(customer);
        }

        public async Task<CustomerResponseDto?> UpdateCustomerAsync(Guid id, UpdateCustomerDto dto)
        {
            var customer = await _context.Customers.FindAsync(id);
            if (customer == null)
                return null;

            customer.FirstName = dto.FirstName;
            customer.LastName = dto.LastName;
            customer.Email = dto.Email;
            customer.Phone = dto.Phone;
            customer.Company = dto.Company;
            customer.Address = dto.Address;
            customer.IsActive = dto.IsActive;
            customer.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return await GetCustomerResponseAsync(customer);
        }

        public async Task<bool> DeleteCustomerAsync(Guid id)
        {
            var customer = await _context.Customers.FindAsync(id);
            if (customer == null)
                return false;

            // Soft delete - mark as inactive instead of removing
            customer.IsActive = false;
            customer.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> CustomerExistsAsync(Guid id)
        {
            return await _context.Customers.AnyAsync(c => c.Id == id && c.IsActive);
        }

        public async Task<IEnumerable<SaleListDto>> GetCustomerSalesHistoryAsync(Guid customerId)
        {
            var sales = await _context.Sales
                .Include(s => s.Customer)
                .Include(s => s.ProcessedBy)
                .Include(s => s.SaleItems)
                .Where(s => s.CustomerId == customerId)
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

        private async Task<CustomerResponseDto> GetCustomerResponseAsync(Customer customer)
        {
            var completedSales = customer.Sales?.Where(s => s.Status == SaleStatus.Completed) ?? new List<Sale>();

            return new CustomerResponseDto
            {
                Id = customer.Id,
                FirstName = customer.FirstName,
                LastName = customer.LastName,
                Email = customer.Email,
                Phone = customer.Phone,
                Company = customer.Company,
                Address = customer.Address,
                IsActive = customer.IsActive,
                CreatedAt = customer.CreatedAt,
                UpdatedAt = customer.UpdatedAt,
                TotalPurchases = completedSales.Count(),
                TotalSpent = completedSales.Sum(s => s.TotalAmount),
                LastPurchaseDate = completedSales.OrderByDescending(s => s.SaleDate).FirstOrDefault()?.SaleDate
            };
        }
    }
}