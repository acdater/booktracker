using System.ComponentModel.DataAnnotations;

namespace BookTracker.Api.DTOs.Shelf;

public class UpdatePagesDto
{
    [Required] public int Pages { get; set; }
}
