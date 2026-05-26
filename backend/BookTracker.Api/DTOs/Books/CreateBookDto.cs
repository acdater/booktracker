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
    [Range(1, int.MaxValue, ErrorMessage = "TotalPages must be a positive integer.")]
    public int TotalPages { get; set; }

    [Required]
    public string Genre { get; set; } = string.Empty;

    public string? CoverImageUrl { get; set; }
}
