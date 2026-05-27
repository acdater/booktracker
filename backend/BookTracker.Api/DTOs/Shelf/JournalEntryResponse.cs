namespace BookTracker.Api.DTOs.Shelf;

public class JournalEntryResponse
{
    public int ReadingNumber { get; set; }
    public string ActionType { get; set; } = "";
    public string? OldValue { get; set; }
    public string? NewValue { get; set; }
    public DateTime Timestamp { get; set; }
}
