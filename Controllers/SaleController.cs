// Controllers/SalesController.cs
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using RadiatorStockAPI.DTOs;
using RadiatorStockAPI.Services;
using RadiatorStockAPI.Models;

namespace RadiatorStockAPI.Controllers
{
    [ApiController]
    [Route("api/v1/sales")]
    [Produces("application/json")]
    [Authorize] // Require authentication
    public class SalesController : ControllerBase
    {
        private readonly ISalesService _salesService;

        public SalesController(ISalesService salesService)
        {
            _salesService = salesService;
        }

        /// <summary>
        /// Create a new sale transaction (Both Admin and Staff can process sales)
        /// </summary>
        [HttpPost]
        [ProducesResponseType(typeof(SaleResponseDto), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<ActionResult<SaleResponseDto>> CreateSale([FromBody] CreateSaleDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (userId == null || !Guid.TryParse(userId, out var userGuid))
                return Unauthorized();

            var sale = await _salesService.CreateSaleAsync(dto, userGuid);
            if (sale == null)
                return BadRequest(new { message = "Failed to create sale. Check customer ID and stock availability." });

            return CreatedAtAction(nameof(GetSale), new { id = sale.Id }, sale);
        }

        /// <summary>
        /// Get all sales
        /// </summary>
        [HttpGet]
        [ProducesResponseType(typeof(IEnumerable<SaleListDto>), StatusCodes.Status200OK)]
        public async Task<ActionResult<IEnumerable<SaleListDto>>> GetAllSales()
        {
            var sales = await _salesService.GetAllSalesAsync();
            return Ok(sales);
        }

        /// <summary>
        /// Get sale by ID
        /// </summary>
        [HttpGet("{id:guid}")]
        [ProducesResponseType(typeof(SaleResponseDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<SaleResponseDto>> GetSale(Guid id)
        {
            var sale = await _salesService.GetSaleByIdAsync(id);
            if (sale == null)
                return NotFound(new { message = $"Sale with ID {id} not found." });

            return Ok(sale);
        }

        /// <summary>
        /// Get sales within a date range
        /// </summary>
        [HttpGet("by-date")]
        [ProducesResponseType(typeof(IEnumerable<SaleListDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<IEnumerable<SaleListDto>>> GetSalesByDateRange(
            [FromQuery] DateTime fromDate, 
            [FromQuery] DateTime toDate)
        {
            if (fromDate > toDate)
                return BadRequest(new { message = "fromDate cannot be greater than toDate." });

            var sales = await _salesService.GetSalesByDateRangeAsync(fromDate, toDate);
            return Ok(sales);
        }

        /// <summary>
        /// Generate receipt for a sale
        /// </summary>
        [HttpGet("{id:guid}/receipt")]
        [ProducesResponseType(typeof(ReceiptDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<ReceiptDto>> GetReceipt(Guid id)
        {
            var receipt = await _salesService.GetReceiptAsync(id);
            if (receipt == null)
                return NotFound(new { message = $"Sale with ID {id} not found." });

            return Ok(receipt);
        }

        /// <summary>
        /// Cancel a sale (Admin only)
        /// </summary>
        [HttpPost("{id:guid}/cancel")]
        [Authorize(Roles = nameof(UserRole.Admin))]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> CancelSale(Guid id)
        {
            if (!await _salesService.SaleExistsAsync(id))
                return NotFound(new { message = $"Sale with ID {id} not found." });

            var cancelled = await _salesService.CancelSaleAsync(id);
            if (!cancelled)
                return BadRequest(new { message = "Cannot cancel this sale. Only completed sales can be cancelled." });

            return Ok(new { message = "Sale cancelled successfully." });
        }

        /// <summary>
        /// Refund a sale (Admin only) - Restores stock levels
        /// </summary>
        [HttpPost("{id:guid}/refund")]
        [Authorize(Roles = nameof(UserRole.Admin))]
        [ProducesResponseType(typeof(SaleResponseDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<ActionResult<SaleResponseDto>> RefundSale(Guid id)
        {
            if (!await _salesService.SaleExistsAsync(id))
                return NotFound(new { message = $"Sale with ID {id} not found." });

            var refundedSale = await _salesService.RefundSaleAsync(id);
            if (refundedSale == null)
                return BadRequest(new { message = "Cannot refund this sale. Only completed sales can be refunded." });

            return Ok(refundedSale);
        }
    }
}