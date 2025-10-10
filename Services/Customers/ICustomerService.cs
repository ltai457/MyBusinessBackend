using RadiatorStockAPI.DTOs.Customers;
using RadiatorStockAPI.DTOs.Sales;

namespace RadiatorStockAPI.Services.Customers;

public interface ICustomerService
{
    Task<CustomerResponseDto?> CreateCustomerAsync(CreateCustomerDto dto);
    Task<IEnumerable<CustomerListDto>> GetAllCustomersAsync();
    Task<CustomerResponseDto?> GetCustomerByIdAsync(Guid id);
    Task<CustomerResponseDto?> UpdateCustomerAsync(Guid id, UpdateCustomerDto dto);
    Task<bool> DeleteCustomerAsync(Guid id);
    Task<bool> CustomerExistsAsync(Guid id);
    Task<IEnumerable<SaleListDto>> GetCustomerSalesHistoryAsync(Guid customerId);
}
