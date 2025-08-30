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
    [Authorize]
    public class RadiatorsController : ControllerBase
    {
        private readonly IRadiatorService _radiatorService;

        public RadiatorsController(IRadiatorService radiatorService)
        {
            _radiatorService = radiatorService;
        }

        /// <summary>Get all radiators with stock summary.</summary>
        [HttpGet]
        [AllowAnonymous] // optional
        [ProducesResponseType(typeof(IEnumerable<RadiatorListDto>), StatusCodes.Status200OK)]
        public async Task<ActionResult<IEnumerable<RadiatorListDto>>> GetAllRadiators()
        {
            var radiators = await _radiatorService.GetAllRadiatorsAsync();
            return Ok(radiators);
        }

        /// <summary>Get a specific radiator by ID with full stock breakdown.</summary>
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

        /// <summary>Create a new radiator (Admin only).</summary>
        [HttpPost]
        [Authorize(Roles = nameof(UserRole.Admin))]
        [ProducesResponseType(typeof(RadiatorResponseDto), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<ActionResult<RadiatorResponseDto>> CreateRadiator([FromBody] CreateRadiatorDto dto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var created = await _radiatorService.CreateRadiatorAsync(dto);
            if (created == null)
                return Conflict(new { message = $"A radiator with code '{dto.Code}' already exists." });

            return CreatedAtAction(nameof(GetRadiator), new { id = created.Id }, created);
        }

        /// <summary>Update radiator details (Admin only).</summary>
        [HttpPut("{id:guid}")]
        [Authorize(Roles = nameof(UserRole.Admin))]
        [ProducesResponseType(typeof(RadiatorResponseDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<ActionResult<RadiatorResponseDto>> UpdateRadiator(Guid id, [FromBody] UpdateRadiatorDto dto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            if (!await _radiatorService.RadiatorExistsAsync(id))
                return NotFound(new { message = $"Radiator with ID {id} not found." });

            var updated = await _radiatorService.UpdateRadiatorAsync(id, dto);
            if (updated == null)
                return BadRequest(new { message = "Failed to update radiator (duplicate code or other validation failed)." });

            return Ok(updated);
        }

        /// <summary>Bulk price update (Admin only).</summary>
        [HttpPut("prices")]
        [Authorize(Roles = nameof(UserRole.Admin))]
        [ProducesResponseType(typeof(IEnumerable<RadiatorListDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> UpdatePrices([FromBody] List<UpdateRadiatorPriceDto> updates)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var result = await _radiatorService.UpdateMultiplePricesAsync(updates ?? new());
            return Ok(result);
        }

        /// <summary>Delete radiator (Admin only).</summary>
        [HttpDelete("{id:guid}")]
        [Authorize(Roles = nameof(UserRole.Admin))]
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
