using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RadiatorStockAPI.DTOs.Radiators;
using RadiatorStockAPI.Services;

namespace RadiatorStockAPI.Controllers
{
    [ApiController]
    [Route("api/v1/radiators")]
    [Produces("application/json")]
    [Authorize] // All endpoints require authentication
    public class RadiatorsController : ControllerBase
    {
        private readonly IRadiatorService _radiatorService;

        public RadiatorsController(IRadiatorService radiatorService)
        {
            _radiatorService = radiatorService;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<RadiatorListDto>>> GetAllRadiators()
        {
            var radiators = await _radiatorService.GetAllRadiatorsAsync();
            return Ok(radiators);
        }

        [HttpGet("{id:guid}")]
        public async Task<ActionResult<RadiatorResponseDto>> GetRadiator(Guid id)
        {
            var radiator = await _radiatorService.GetRadiatorByIdAsync(id);
            if (radiator == null)
                return NotFound(new { message = $"Radiator with ID {id} not found." });

            return Ok(radiator);
        }

        [HttpPost]
        [Authorize(Roles = "Admin,Staff")] // Admin or Staff can create
        public async Task<ActionResult<RadiatorResponseDto>> CreateRadiator([FromBody] CreateRadiatorDto dto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var created = await _radiatorService.CreateRadiatorAsync(dto);
            if (created == null)
                return Conflict(new { message = $"A radiator with code '{dto.Code}' already exists." });

            return CreatedAtAction(nameof(GetRadiator), new { id = created.Id }, created);
        }

        [HttpPut("{id:guid}")]
        [Authorize(Roles = "Admin,Staff")] // Admin or Staff can update
        public async Task<ActionResult<RadiatorResponseDto>> UpdateRadiator(Guid id, [FromBody] UpdateRadiatorDto dto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            if (!await _radiatorService.RadiatorExistsAsync(id))
                return NotFound(new { message = $"Radiator with ID {id} not found." });

            var updated = await _radiatorService.UpdateRadiatorAsync(id, dto);
            return Ok(updated);
        }

        [HttpDelete("{id:guid}")]
        [Authorize(Roles = "Admin")] // Only Admin can delete
        public async Task<IActionResult> DeleteRadiator(Guid id)
        {
            if (!await _radiatorService.RadiatorExistsAsync(id))
                return NotFound(new { message = $"Radiator with ID {id} not found." });

            var deleted = await _radiatorService.DeleteRadiatorAsync(id);
            if (!deleted)
                return BadRequest(new { message = "Failed to delete radiator." });

            return NoContent();
        }
        [HttpPost("test-s3")]
        [Authorize(Roles = "Admin,Staff")]
        public async Task<IActionResult> TestS3Upload(IFormFile file)
        {
            try
            {
                var url = await _radiatorService.TestS3Async(file);
                return Ok(new { success = true, url });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, error = ex.Message });
            }
        }
        [HttpPost("create-with-image")]
        [Authorize(Roles = "Admin,Staff")]
        [Consumes("multipart/form-data")]
        public async Task<ActionResult<RadiatorResponseDto>> CreateRadiatorWithImage([FromForm] CreateRadiatorWithImageDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            try
            {
                var created = await _radiatorService.CreateRadiatorWithImageAsync(dto);

                if (created == null)
                    return Conflict(new { message = $"A radiator with code '{dto.Code}' already exists." });

                return CreatedAtAction(nameof(GetRadiator), new { id = created.Id }, created);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }
        [HttpPost("{id:guid}/images")]
        [Authorize(Roles = "Admin,Staff")]
        [Consumes("multipart/form-data")]
        public async Task<ActionResult<RadiatorImageDto>> UploadRadiatorImage(Guid id, [FromForm] UploadRadiatorImageDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            try
            {
                var result = await _radiatorService.AddImageToRadiatorAsync(id, dto);

                if (result == null)
                    return NotFound(new { message = $"Radiator with ID {id} not found." });

                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpGet("{id:guid}/images")]
        public async Task<ActionResult<List<RadiatorImageDto>>> GetRadiatorImages(Guid id)
        {
            if (!await _radiatorService.RadiatorExistsAsync(id))
                return NotFound(new { message = $"Radiator with ID {id} not found." });

            var images = await _radiatorService.GetRadiatorImagesAsync(id);
            return Ok(images);
        }

    }
}
