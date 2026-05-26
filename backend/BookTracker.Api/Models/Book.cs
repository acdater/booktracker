namespace BookTracker.Api.Models;

public class Book
{
    public int Id { get; set; }
    public string ISBN { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Author { get; set; } = string.Empty;
    public int TotalPages { get; set; }
    public string Genre { get; set; } = string.Empty;
    public string? CoverImageUrl { get; set; }
}
