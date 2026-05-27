using BookTracker.Api.DTOs.Stats;

namespace BookTracker.Api.Services.Interfaces;

public interface IStatsService
{
    Task<StatsStripResponse> GetStripAsync(int userId);
    Task<StatsPageResponse> GetPageAsync(int userId);
}
