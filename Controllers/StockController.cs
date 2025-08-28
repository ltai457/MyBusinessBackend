// Controllers/StockController.cs
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RadiatorStockAPI.DTOs;
using RadiatorStockAPI.Services;

namespace RadiatorStockAPI.Controllers
{
    [ApiController]
    [Route("api/v1/radiators/{radiatorId:guid}/stock")]
    [Produces("application/json")]
    [Authorize] // Require authentication for all stock operations
    public class StockController : ControllerBase
    {
        private readonly IStockService _stockService;
        private readonly IRadiatorService _radiatorService;

        public StockController(IStockService stockService, IRadiatorService radiatorService)
        {
            _stockService = stockService;
            _radiatorService = radiatorService;
        }

        /// <summary>
        /// Get stock levels for a specific radiator across all warehouses
        /// </summary>
        [HttpGet]
        [ProducesResponseType(typeof(StockResponseDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<StockResponseDto>> GetRadiatorStock(Guid radiatorId)
        {
            if (!await _radiatorService.RadiatorExistsAsync(radiatorId))
                return NotFound(new { message = $"Radiator with ID {radiatorId} not found." });

            var stock = await _stockService.GetRadiatorStockAsync(radiatorId);
            if (stock == null)
                return NotFound(new { message = $"Stock not found for radiator ID {radiatorId}." });

            return Ok(stock);
        }

        /// <summary>
        /// Update stock quantity for a specific warehouse
        /// </summary>
        [HttpPost]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> UpdateStock(Guid radiatorId, [FromBody] UpdateStockDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            if (!await _radiatorService.RadiatorExistsAsync(radiatorId))
                return NotFound(new { message = $"Radiator with ID {radiatorId} not found." });

            var updated = await _stockService.UpdateStockAsync(radiatorId, dto);
            if (!updated)
                return BadRequest(new { message = $"Failed to update stock. Check warehouse code '{dto.WarehouseCode}'." });

            return Ok(new { 
                message = $"Stock updated successfully for warehouse {dto.WarehouseCode}.",
                radiatorId = radiatorId,
                warehouseCode = dto.WarehouseCode,
                quantity = dto.Quantity
            });
        }
    }
}