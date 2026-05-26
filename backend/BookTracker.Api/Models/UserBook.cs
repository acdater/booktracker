using BookTracker.Api.Models.Enums;

namespace BookTracker.Api.Models;

public class UserBook
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public int BookId { get; set; }
    public ReadingStatus Status { get; set; } = ReadingStatus.Resting;
    public int CurrentPages { get; set; } = 0;
    public int ReadingNumber { get; set; } = 1;
    public DateTime? StartedAt { get; set; }
    public DateTime? FinishedAt { get; set; }
    public DateTime LastActivityAt { get; set; }

    // Navigation properties
    public User User { get; set; } = null!;
    public Book Book { get; set; } = null!;
}
