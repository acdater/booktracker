using System.ComponentModel.DataAnnotations;

namespace BookTracker.Api.DTOs.Shelf;

public class UpdatePagesDto
{
    [Required]
    [Range(0, 10000, ErrorMessage = "Pages must be between 0 and 10,000.")]
    public int Pages { get; set; }
}
