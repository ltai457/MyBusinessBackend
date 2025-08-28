// Controllers/RadiatorsController.cs
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RadiatorStockAPI.DTOs;
using RadiatorStockAPI.Services;
using RadiatorStockAPI.Models;

namespace RadiatorStockAPI.Controllers
{
    [ApiController]
    [Route("api/v1/radiators")]
    [Produces("application/json")]
    [Authorize] // Require authentication for all actions
    public class RadiatorsController : ControllerBase
    {
        private readonly IRadiatorService _radiatorService;

        public RadiatorsController(IRadiatorService radiatorService)
        {
            _radiatorService = radiatorService;
        }

        /// <summary>
        /// Create a new radiator with initial stock = 0 in all warehouses (Admin only)
        /// </summary>
        [HttpPost]
        [Authorize(Roles = nameof(UserRole.Admin))] // Admin only
        [ProducesResponseType(typeof(RadiatorResponseDto), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<ActionResult<RadiatorResponseDto>> CreateRadiator([FromBody] CreateRadiatorDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            // Check if code already exists
            if (await _radiatorService.CodeExistsAsync(dto.Code))
            {
                return Conflict(new { message = $"Radiator with code '{dto.Code}' already exists." });
            }

            var radiator = await _radiatorService.CreateRadiatorAsync(dto);
            if (radiator == null)
                return BadRequest(new { message = "Failed to create radiator." });

            return CreatedAtAction(nameof(GetRadiator), new { id = radiator.Id }, radiator);
        }

        /// <summary>
        /// Get all radiators with their stock levels per warehouse
        /// </summary>
        [HttpGet]
        [ProducesResponseType(typeof(IEnumerable<RadiatorListDto>), StatusCodes.Status200OK)]
        public async Task<ActionResult<IEnumerable<RadiatorListDto>>> GetAllRadiators()
        {
            var radiators = await _radiatorService.GetAllRadiatorsAsync();
            return Ok(radiators);
        }

        /// <summary>
        /// Get a specific radiator by ID with stock levels
        /// </summary>
        [HttpGet("{id:guid}")]
        [ProducesResponseType(typeof(RadiatorResponseDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<RadiatorResponseDto>> GetRadiator(Guid id)
        {
            var radiator = await _radiatorService.GetRadiatorByIdAsync(id);
            if (radiator == null)
                return NotFound(new { message = $"Radiator with ID {id} not found." });

            return Ok(radiator);
        }

        /// <summary>
        /// Update radiator details (not stock - use stock endpoints for that)
        /// </summary>
        [HttpPut("{id:guid}")]
        [ProducesResponseType(typeof(RadiatorResponseDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<RadiatorResponseDto>> UpdateRadiator(Guid id, [FromBody] UpdateRadiatorDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            if (!await _radiatorService.RadiatorExistsAsync(id))
                return NotFound(new { message = $"Radiator with ID {id} not found." });

            var updatedRadiator = await _radiatorService.UpdateRadiatorAsync(id, dto);
            if (updatedRadiator == null)
                return BadRequest(new { message = "Failed to update radiator." });

            return Ok(updatedRadiator);
        }

        /// <summary>
        /// Delete a radiator (cascades to delete stock levels) - Admin only
        /// </summary>
        [HttpDelete("{id:guid}")]
        [Authorize(Roles = nameof(UserRole.Admin))] // Admin only
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> DeleteRadiator(Guid id)
        {
            if (!await _radiatorService.RadiatorExistsAsync(id))
                return NotFound(new { message = $"Radiator with ID {id} not found." });

            var deleted = await _radiatorService.DeleteRadiatorAsync(id);
            if (!deleted)
                return BadRequest(new { message = "Failed to delete radiator." });

            return NoContent();
        }
    }
}