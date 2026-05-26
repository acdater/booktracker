namespace BookTracker.Api.DTOs.Books;

public class BookResponse
{
    public int Id { get; set; }
    public string ISBN { get; set; } = "";
    public string Title { get; set; } = "";
    public string Author { get; set; } = "";
    public int TotalPages { get; set; }
    public string? Genre { get; set; }
    public string? CoverImageUrl { get; set; }
}
