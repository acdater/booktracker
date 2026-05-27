namespace BookTracker.Api.DTOs.Stats;

public class ByStatusCounts
{
    public int Total { get; set; }
    public int Resting { get; set; }
    public int Started { get; set; }
    public int Finished { get; set; }
    public int Abandoned { get; set; }
}
