using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RadiatorStockAPI.DTOs;
using RadiatorStockAPI.Services;

namespace RadiatorStockAPI.Controllers
{
    [ApiController]
    [Route("api/v1")]
    [Produces("application/json")]
    [Authorize]
    public class StockController : ControllerBase
    {
        private readonly IStockService _stockService;
        private readonly IRadiatorService _radiatorService;

        public StockController(IStockService stockService, IRadiatorService radiatorService)
        {
            _stockService = stockService;
            _radiatorService = radiatorService;
        }

        // Existing endpoints for individual radiator stock management
        [HttpGet("radiators/{radiatorId:guid}/stock")]
        public async Task<ActionResult<StockResponseDto>> GetRadiatorStock(Guid radiatorId)
        {
            if (!await _radiatorService.RadiatorExistsAsync(radiatorId))
                return NotFound(new { message = $"Radiator with ID {radiatorId} not found." });

            var stock = await _stockService.GetRadiatorStockAsync(radiatorId);
            if (stock == null)
                return NotFound(new { message = $"Stock not found for radiator ID {radiatorId}." });

            return Ok(stock);
        }

        [HttpPost("radiators/{radiatorId:guid}/stock")]
        [Authorize(Roles = "Admin,Staff")]
        public async Task<IActionResult> UpdateStock(Guid radiatorId, [FromBody] UpdateStockDto dto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            if (!await _radiatorService.RadiatorExistsAsync(radiatorId))
                return NotFound(new { message = $"Radiator with ID {radiatorId} not found." });

            var updated = await _stockService.UpdateStockAsync(radiatorId, dto);
            if (!updated)
                return BadRequest(new { message = $"Failed to update stock. Check warehouse code '{dto.WarehouseCode}'." });

            return Ok(new
            {
                message = $"Stock updated successfully for warehouse {dto.WarehouseCode}.",
                radiatorId,
                dto.WarehouseCode,
                dto.Quantity
            });
        }

        // New enhanced endpoints for stock management

        [HttpGet("stock/summary")]
        public async Task<ActionResult<StockSummaryDto>> GetStockSummary()
        {
            var summary = await _stockService.GetStockSummaryAsync();
            return Ok(summary);
        }

        [HttpGet("stock/all-radiators")]
        public async Task<ActionResult<IEnumerable<RadiatorWithStockDto>>> GetAllRadiatorsWithStock(
            [FromQuery] string? search = null,
            [FromQuery] bool lowStockOnly = false,
            [FromQuery] string? warehouseCode = null)
        {
            var radiators = await _stockService.GetAllRadiatorsWithStockAsync(search, lowStockOnly, warehouseCode);
            return Ok(radiators);
        }

        [HttpGet("stock/low-stock")]
        public async Task<ActionResult<IEnumerable<LowStockItemDto>>> GetLowStockItems(
            [FromQuery] int threshold = 5)
        {
            var lowStockItems = await _stockService.GetLowStockItemsAsync(threshold);
            return Ok(lowStockItems);
        }

        [HttpGet("stock/out-of-stock")]
        public async Task<ActionResult<IEnumerable<OutOfStockItemDto>>> GetOutOfStockItems()
        {
            var outOfStockItems = await _stockService.GetOutOfStockItemsAsync();
            return Ok(outOfStockItems);
        }

        [HttpPost("stock/bulk-update")]
        [Authorize(Roles = "Admin,Staff")]
        public async Task<ActionResult<BulkUpdateResultDto>> BulkUpdateStock([FromBody] BulkUpdateStockDto dto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var result = await _stockService.BulkUpdateStockAsync(dto);

            if (result.HasErrors)
            {
                return BadRequest(new
                {
                    message = "Some stock updates failed",
                    result.SuccessCount,
                    result.ErrorCount,
                    result.Errors
                });
            }

            return Ok(result);
        }

        [HttpGet("stock/warehouse/{warehouseCode}")]
        public async Task<ActionResult<WarehouseStockDto>> GetWarehouseStock(string warehouseCode)
        {
            var warehouseStock = await _stockService.GetWarehouseStockAsync(warehouseCode);
            if (warehouseStock == null)
                return NotFound(new { message = $"Warehouse with code '{warehouseCode}' not found." });

            return Ok(warehouseStock);
        }

        [HttpGet("stock/history/{radiatorId:guid}")]
        public async Task<ActionResult<IEnumerable<StockHistoryDto>>> GetStockHistory(
            Guid radiatorId,
            [FromQuery] DateTime? fromDate = null,
            [FromQuery] DateTime? toDate = null,
            [FromQuery] string? warehouseCode = null)
        {
            if (!await _radiatorService.RadiatorExistsAsync(radiatorId))
                return NotFound(new { message = $"Radiator with ID {radiatorId} not found." });

            var history = await _stockService.GetStockHistoryAsync(radiatorId, fromDate, toDate, warehouseCode);
            return Ok(history);
        }

        [HttpPost("stock/adjust")]
        [Authorize(Roles = "Admin,Staff")]
        public async Task<IActionResult> AdjustStock([FromBody] StockAdjustmentDto dto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var result = await _stockService.AdjustStockAsync(dto);
            if (!result.Success)
                return BadRequest(new { message = result.Error });

            return Ok(new
            {
                message = "Stock adjustment completed successfully",
                result.RadiatorId,
                result.WarehouseCode,
                result.OldQuantity,
                result.NewQuantity,
                result.AdjustmentReason
            });
        }


        [HttpGet("stock/movements")]
        public async Task<ActionResult<IEnumerable<StockMovementDto>>> GetStockMovements(
     [FromQuery] Guid? radiatorId = null,
     [FromQuery] string? warehouseCode = null,
     [FromQuery] DateTime? fromDate = null,
     [FromQuery] DateTime? toDate = null,
     [FromQuery] string? movementType = null,
     [FromQuery] int? limit = 100)
        {
            try
            {
                var movements = await _stockService.GetStockMovementsAsync(
                    radiatorId,
                    warehouseCode,
                    fromDate,
                    toDate,
                    movementType,
                    limit
                );

                return Ok(movements);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error retrieving stock movements", error = ex.Message });
            }
        }
    }
}