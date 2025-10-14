using System;
using RadiatorStockAPI.DTOs.Sales;

namespace RadiatorStockAPI.Services.Sales;

public interface ISalesService
{
    Task<SaleResponseDto?> CreateSaleAsync(CreateSaleDto dto, Guid userId);
    Task<IEnumerable<SaleListDto>> GetAllSalesAsync();
    Task<SaleResponseDto?> GetSaleByIdAsync(Guid id);
    Task<ReceiptDto?> GetReceiptAsync(Guid saleId);
    Task<bool> CancelSaleAsync(Guid id);
    Task<SaleResponseDto?> RefundSaleAsync(Guid id);
    Task<IEnumerable<SaleListDto>> GetSalesByDateRangeAsync(DateTime fromDate, DateTime toDate);
    Task<bool> SaleExistsAsync(Guid id);
    string GenerateSaleNumber();
    Task<InvoiceResponseDto?> GenerateInvoiceAsync(GenerateInvoiceRequestDto dto, Guid userId);
    Task<IEnumerable<InvoiceResponseDto>> GetAllInvoicesAsync();
    Task<InvoiceResponseDto?> GetInvoiceByIdAsync(Guid id);
    Task<InvoiceResponseDto?> GetInvoiceByNumberAsync(string invoiceNumber);
}
