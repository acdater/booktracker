using BookTracker.Api.DTOs.Books;

namespace BookTracker.Api.DTOs.Shelf;

public class UserBookResponse
{
    public int Id { get; set; }
    public BookResponse Book { get; set; } = null!;
    public string Status { get; set; } = string.Empty;
    public int CurrentPages { get; set; }
    public int ReadingNumber { get; set; }
    public DateTime? StartedAt { get; set; }
    public DateTime? FinishedAt { get; set; }
    public DateTime LastActivityAt { get; set; }
    public int ReaderCount { get; set; }
}
