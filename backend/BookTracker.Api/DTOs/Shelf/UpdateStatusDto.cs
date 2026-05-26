using System.ComponentModel.DataAnnotations;

namespace BookTracker.Api.DTOs.Shelf;

public class UpdateStatusDto
{
    [Required] public string Status { get; set; } = string.Empty;
}
