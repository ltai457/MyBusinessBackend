using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace RadiatorStockAPI.DTOs.Radiators;

public class UploadRadiatorImageDto
{
    [Required]
    public IFormFile Image { get; set; } = null!;

    public bool IsPrimary { get; set; } = false;

    [StringLength(500)]
    public string? Description { get; set; }
}
