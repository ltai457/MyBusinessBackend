using Microsoft.AspNetCore.Mvc;
using RadiatorStockAPI.DTOs;
using RadiatorStockAPI.Services;

namespace RadiatorStockAPI.Controllers
{
    [ApiController]
    [Route("api/v1/warehouses")]
    [Produces("application/json")]
    public class WarehousesController : ControllerBase
    {
        private readonly IWarehouseService _warehouseService;

        public WarehousesController(IWarehouseService warehouseService)
        {
            _warehouseService = warehouseService;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<WarehouseDto>>> GetAllWarehouses()
        {
            var warehouses = await _warehouseService.GetAllWarehousesAsync();
            return Ok(warehouses);
        }

        [HttpGet("{id:guid}")]
        public async Task<ActionResult<WarehouseDto>> GetWarehouseById(Guid id)
        {
            var warehouse = await _warehouseService.GetWarehouseByIdAsync(id);
            if (warehouse == null)
                return NotFound(new { message = $"Warehouse with ID '{id}' not found." });

            return Ok(warehouse);
        }

        [HttpPost]
        public async Task<ActionResult<WarehouseDto>> CreateWarehouse([FromBody] CreateWarehouseDto dto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var existingWarehouse = await _warehouseService.GetWarehouseByCodeAsync(dto.Code);
            if (existingWarehouse != null)
                return Conflict(new { message = $"Warehouse with code '{dto.Code}' already exists." });

            var warehouse = await _warehouseService.CreateWarehouseAsync(dto);
            return CreatedAtAction(nameof(GetWarehouseById), new { id = warehouse.Id }, warehouse);
        }

        [HttpPut("{id:guid}")]
        public async Task<ActionResult<WarehouseDto>> UpdateWarehouse(Guid id, [FromBody] UpdateWarehouseDto dto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var existingWarehouse = await _warehouseService.GetWarehouseByIdAsync(id);
            if (existingWarehouse == null)
                return NotFound(new { message = $"Warehouse with ID '{id}' not found." });

            var updatedWarehouse = await _warehouseService.UpdateWarehouseAsync(id, dto);
            return Ok(updatedWarehouse);
        }

        [HttpDelete("{id:guid}")]
        public async Task<ActionResult> DeleteWarehouse(Guid id)
        {
            var warehouse = await _warehouseService.GetWarehouseByIdAsync(id);
            if (warehouse == null)
                return NotFound(new { message = $"Warehouse with ID '{id}' not found." });

            await _warehouseService.DeleteWarehouseAsync(id);
            return NoContent();
        }
    }
}
