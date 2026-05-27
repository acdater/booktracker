namespace BookTracker.Api.DTOs.Stats;

public class StatsPageResponse
{
    public ByStatusCounts ByStatus { get; set; } = new();
    public PeriodCounts BooksCompleted { get; set; } = new();
    public PeriodCounts PagesRead { get; set; } = new();
    public string? UnfinishedGenre { get; set; }
}
