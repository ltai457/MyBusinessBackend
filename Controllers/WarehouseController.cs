// Controllers/WarehousesController.cs
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RadiatorStockAPI.DTOs;
using RadiatorStockAPI.Services;

namespace RadiatorStockAPI.Controllers
{
    [ApiController]
    [Route("api/v1/warehouses")]
    [Produces("application/json")]
    [Authorize] // Require authentication
    public class WarehousesController : ControllerBase
    {
        private readonly IWarehouseService _warehouseService;

        public WarehousesController(IWarehouseService warehouseService)
        {
            _warehouseService = warehouseService;
        }

        /// <summary>
        /// Get all warehouses
        /// </summary>
        [HttpGet]
        [ProducesResponseType(typeof(IEnumerable<WarehouseDto>), StatusCodes.Status200OK)]
        public async Task<ActionResult<IEnumerable<WarehouseDto>>> GetAllWarehouses()
        {
            var warehouses = await _warehouseService.GetAllWarehousesAsync();
            return Ok(warehouses);
        }

        /// <summary>
        /// Get warehouse by code
        /// </summary>
        [HttpGet("{code}")]
        [ProducesResponseType(typeof(WarehouseDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<WarehouseDto>> GetWarehouseByCode(string code)
        {
            var warehouse = await _warehouseService.GetWarehouseByCodeAsync(code);
            if (warehouse == null)
                return NotFound(new { message = $"Warehouse with code '{code}' not found." });

            return Ok(new WarehouseDto
            {
                Id = warehouse.Id,
                Code = warehouse.Code,
                Name = warehouse.Name
            });
        }
    }
}