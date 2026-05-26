using System.ComponentModel.DataAnnotations;

namespace BookTracker.Api.DTOs.Shelf;

public class AddToShelfDto
{
    [Required]
    public int BookId { get; set; }
}
