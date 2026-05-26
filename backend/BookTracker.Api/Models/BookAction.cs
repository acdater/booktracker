using BookTracker.Api.Models.Enums;

namespace BookTracker.Api.Models;

public class BookAction
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public User User { get; set; } = null!;
    public int UserBookId { get; set; }
    public UserBook UserBook { get; set; } = null!;
    public ActionType ActionType { get; set; }
    public string? OldValue { get; set; }
    public string? NewValue { get; set; }
    public DateTime Timestamp { get; set; }
}
