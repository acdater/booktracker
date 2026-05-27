using System.ComponentModel.DataAnnotations;

namespace BookTracker.Api.DTOs.Books;

public class CreateBookDto
{
    [Required]
    public string ISBN { get; set; } = string.Empty;

    [Required]
    public string Title { get; set; } = string.Empty;

    [Required]
    public string Author { get; set; } = string.Empty;

    [Required]
    [Range(1, 10000, ErrorMessage = "Total pages must be between 1 and 10,000.")]
    public int TotalPages { get; set; }

    [Required]
    public string Genre { get; set; } = string.Empty;

    public string? CoverImageUrl { get; set; }
}
