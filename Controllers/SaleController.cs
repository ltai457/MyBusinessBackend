using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using RadiatorStockAPI.DTOs;
using RadiatorStockAPI.Services;

namespace RadiatorStockAPI.Controllers
{
    [ApiController]
    [Route("api/v1/sales")]
    [Produces("application/json")]
    public class SalesController : ControllerBase
    {
        private readonly ISalesService _salesService;

        public SalesController(ISalesService salesService)
        {
            _salesService = salesService;
        }

        [HttpPost]
        public async Task<ActionResult<SaleResponseDto>> CreateSale([FromBody] CreateSaleDto dto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            // No auth — attempt to parse user id, default to Guid.Empty if not present
            Guid.TryParse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value, out var userGuid);

            var sale = await _salesService.CreateSaleAsync(dto, userGuid);
            if (sale == null)
                return BadRequest(new { message = "Failed to create sale. Check customer ID and stock availability." });

            return CreatedAtAction(nameof(GetSale), new { id = sale.Id }, sale);
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<SaleListDto>>> GetAllSales()
        {
            var sales = await _salesService.GetAllSalesAsync();
            return Ok(sales);
        }

        [HttpGet("{id:guid}")]
        public async Task<ActionResult<SaleResponseDto>> GetSale(Guid id)
        {
            var sale = await _salesService.GetSaleByIdAsync(id);
            if (sale == null)
                return NotFound(new { message = $"Sale with ID {id} not found." });

            return Ok(sale);
        }

        [HttpGet("by-date")]
        public async Task<ActionResult<IEnumerable<SaleListDto>>> GetSalesByDateRange(
            [FromQuery] DateTime fromDate,
            [FromQuery] DateTime toDate)
        {
            if (fromDate > toDate)
                return BadRequest(new { message = "fromDate cannot be greater than toDate." });

            var sales = await _salesService.GetSalesByDateRangeAsync(fromDate, toDate);
            return Ok(sales);
        }

        [HttpGet("{id:guid}/receipt")]
        public async Task<ActionResult<ReceiptDto>> GetReceipt(Guid id)
        {
            var receipt = await _salesService.GetReceiptAsync(id);
            if (receipt == null)
                return NotFound(new { message = $"Sale with ID {id} not found." });

            return Ok(receipt);
        }

        [HttpPost("{id:guid}/cancel")]
        public async Task<IActionResult> CancelSale(Guid id)
        {
            if (!await _salesService.SaleExistsAsync(id))
                return NotFound(new { message = $"Sale with ID {id} not found." });

            var cancelled = await _salesService.CancelSaleAsync(id);
            if (!cancelled)
                return BadRequest(new { message = "Cannot cancel this sale. Only completed sales can be cancelled." });

            return Ok(new { message = "Sale cancelled successfully." });
        }

        [HttpPost("{id:guid}/refund")]
        public async Task<ActionResult<SaleResponseDto>> RefundSale(Guid id)
        {
            if (!await _salesService.SaleExistsAsync(id))
                return NotFound(new { message = $"Sale with ID {id} not found." });

            var refunded = await _salesService.RefundSaleAsync(id);
            if (refunded == null)
                return BadRequest(new { message = "Cannot refund this sale. Only completed sales can be refunded." });

            return Ok(refunded);
        }
    }
}
