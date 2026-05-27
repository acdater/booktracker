namespace BookTracker.Api.DTOs.Stats;

public class StatsStripResponse
{
    public int TotalBooks { get; set; }
    public int FinishedCount { get; set; }
    public int StartedCount { get; set; }
    public int PagesThisMonth { get; set; }
}
