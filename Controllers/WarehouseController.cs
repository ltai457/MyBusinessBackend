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
        /// Get warehouse by ID
        /// </summary>
        [HttpGet("{id:guid}")]
        [ProducesResponseType(typeof(WarehouseDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<WarehouseDto>> GetWarehouseById(Guid id)
        {
            var warehouse = await _warehouseService.GetWarehouseByIdAsync(id);
            if (warehouse == null)
                return NotFound(new { message = $"Warehouse with ID '{id}' not found." });

            return Ok(warehouse);
        }

        /// <summary>
        /// Get warehouse by code
        /// </summary>
        [HttpGet("code/{code}")]
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
                Name = warehouse.Name,
                Location = warehouse.Location,
                Address = warehouse.Address,
                Phone = warehouse.Phone,
                Email = warehouse.Email
            });
        }

        /// <summary>
        /// Create a new warehouse
        /// </summary>
        [HttpPost]
        [ProducesResponseType(typeof(WarehouseDto), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public async Task<ActionResult<WarehouseDto>> CreateWarehouse([FromBody] CreateWarehouseDto dto)
        {
            try
            {
                if (!ModelState.IsValid)
                    return BadRequest(ModelState);

                // Check if warehouse code already exists
                var existingWarehouse = await _warehouseService.GetWarehouseByCodeAsync(dto.Code);
                if (existingWarehouse != null)
                {
                    return Conflict(new { message = $"Warehouse with code '{dto.Code}' already exists." });
                }

                var warehouse = await _warehouseService.CreateWarehouseAsync(dto);
                
                return CreatedAtAction(
                    nameof(GetWarehouseById), 
                    new { id = warehouse.Id }, 
                    warehouse
                );
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        /// <summary>
        /// Update an existing warehouse
        /// </summary>
        [HttpPut("{id:guid}")]
        [ProducesResponseType(typeof(WarehouseDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public async Task<ActionResult<WarehouseDto>> UpdateWarehouse(Guid id, [FromBody] UpdateWarehouseDto dto)
        {
            try
            {
                if (!ModelState.IsValid)
                    return BadRequest(ModelState);

                var existingWarehouse = await _warehouseService.GetWarehouseByIdAsync(id);
                if (existingWarehouse == null)
                    return NotFound(new { message = $"Warehouse with ID '{id}' not found." });

                // Check if the new code conflicts with another warehouse
                if (dto.Code.ToUpper() != existingWarehouse.Code.ToUpper())
                {
                    var warehouseWithSameCode = await _warehouseService.GetWarehouseByCodeAsync(dto.Code);
                    if (warehouseWithSameCode != null)
                    {
                        return Conflict(new { message = $"Warehouse with code '{dto.Code}' already exists." });
                    }
                }

                var updatedWarehouse = await _warehouseService.UpdateWarehouseAsync(id, dto);
                return Ok(updatedWarehouse);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        /// <summary>
        /// Delete a warehouse
        /// </summary>
        [HttpDelete("{id:guid}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public async Task<ActionResult> DeleteWarehouse(Guid id)
        {
            try
            {
                var warehouse = await _warehouseService.GetWarehouseByIdAsync(id);
                if (warehouse == null)
                    return NotFound(new { message = $"Warehouse with ID '{id}' not found." });

                // Check if warehouse has stock levels (optional - you might want to prevent deletion)
                var hasStock = await _warehouseService.HasStockLevelsAsync(id);
                if (hasStock)
                {
                    return Conflict(new { message = "Cannot delete warehouse that has stock levels. Please move or remove stock first." });
                }

                await _warehouseService.DeleteWarehouseAsync(id);
                return NoContent();
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        /// <summary>
        /// Validate warehouse code availability
        /// </summary>
        [HttpGet("validate-code/{code}")]
        [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
        public async Task<ActionResult> ValidateWarehouseCode(string code)
        {
            var existingWarehouse = await _warehouseService.GetWarehouseByCodeAsync(code);
            
            return Ok(new 
            { 
                available = existingWarehouse == null,
                message = existingWarehouse == null 
                    ? "Code is available" 
                    : $"Code '{code}' is already in use"
            });
        }
    }
}