using BookTracker.Api.DTOs.Stats;
using BookTracker.Api.Models;
using BookTracker.Api.Models.Enums;
using BookTracker.Api.Repositories.Interfaces;
using BookTracker.Api.Services.Interfaces;

namespace BookTracker.Api.Services;

public class StatsService(
    IUserBookRepository userBookRepository,
    IBookActionRepository bookActionRepository) : IStatsService
{
    private readonly IUserBookRepository _userBookRepository = userBookRepository;
    private readonly IBookActionRepository _bookActionRepository = bookActionRepository;

    public async Task<StatsStripResponse> GetStripAsync(int userId)
    {
        var totalBooks = await _userBookRepository.CountAllAsync(userId);
        var finishedCount = await _userBookRepository.CountByStatusAsync(userId, ReadingStatus.Finished);
        var startedCount = await _userBookRepository.CountByStatusAsync(userId, ReadingStatus.Started);

        var now = DateTime.UtcNow;
        var pageActions = await _bookActionRepository.GetPageUpdatesInMonthAsync(userId, now.Year, now.Month);

        var pagesThisMonth = 0L;
        foreach (var a in pageActions)
        {
            if (int.TryParse(a.NewValue, out var nv) && int.TryParse(a.OldValue, out var ov))
                pagesThisMonth += Math.Max(0, nv - ov);
        }

        return new StatsStripResponse
        {
            TotalBooks = totalBooks,
            FinishedCount = finishedCount,
            StartedCount = startedCount,
            PagesThisMonth = (int)Math.Min(pagesThisMonth, int.MaxValue)
        };
    }

    public async Task<StatsPageResponse> GetPageAsync(int userId)
    {
        var now = DateTime.UtcNow;
        var since365 = now.AddDays(-365);

        // ByStatus — reuse existing count methods
        var total     = await _userBookRepository.CountAllAsync(userId);
        var resting   = await _userBookRepository.CountByStatusAsync(userId, ReadingStatus.Resting);
        var started   = await _userBookRepository.CountByStatusAsync(userId, ReadingStatus.Started);
        var finished  = await _userBookRepository.CountByStatusAsync(userId, ReadingStatus.Finished);
        var abandoned = await _userBookRepository.CountByStatusAsync(userId, ReadingStatus.Abandoned);

        // BooksCompleted — one DB call, 6 in-memory window filters
        var completions = await _bookActionRepository.GetStatusChangesCompletedSinceAsync(userId, since365);
        var booksCompleted = new PeriodCounts
        {
            Days7   = completions.Count(a => a.Timestamp >= now.AddDays(-7)),
            Days30  = completions.Count(a => a.Timestamp >= now.AddDays(-30)),
            Days90  = completions.Count(a => a.Timestamp >= now.AddDays(-90)),
            Days180 = completions.Count(a => a.Timestamp >= now.AddDays(-180)),
            Days270 = completions.Count(a => a.Timestamp >= now.AddDays(-270)),
            Days365 = completions.Count(a => a.Timestamp >= since365)
        };

        // PagesRead — one DB call, 6 in-memory window sums
        var pageUpdates = await _bookActionRepository.GetPageUpdatesSinceAsync(userId, since365);
        var pagesRead = new PeriodCounts
        {
            Days7   = SumPositiveDeltas(pageUpdates, now.AddDays(-7)),
            Days30  = SumPositiveDeltas(pageUpdates, now.AddDays(-30)),
            Days90  = SumPositiveDeltas(pageUpdates, now.AddDays(-90)),
            Days180 = SumPositiveDeltas(pageUpdates, now.AddDays(-180)),
            Days270 = SumPositiveDeltas(pageUpdates, now.AddDays(-270)),
            Days365 = SumPositiveDeltas(pageUpdates, since365)
        };

        // UnfinishedGenre — FR-22
        var allUserBooks = await _userBookRepository.GetShelfAsync(userId);
        var unfinishedGenre = ComputeUnfinishedGenre(allUserBooks);

        return new StatsPageResponse
        {
            ByStatus = new ByStatusCounts
            {
                Total     = total,
                Resting   = resting,
                Started   = started,
                Finished  = finished,
                Abandoned = abandoned
            },
            BooksCompleted  = booksCompleted,
            PagesRead       = pagesRead,
            UnfinishedGenre = unfinishedGenre
        };
    }

    private static int SumPositiveDeltas(List<BookAction> actions, DateTime since)
    {
        var total = 0L;
        foreach (var a in actions.Where(a => a.Timestamp >= since))
        {
            if (int.TryParse(a.NewValue, out var nv) && int.TryParse(a.OldValue, out var ov))
                total += Math.Max(0, nv - ov);
        }
        return (int)Math.Min(total, int.MaxValue);
    }

    private static string? ComputeUnfinishedGenre(List<UserBook> userBooks)
    {
        var startedBooks  = userBooks.Where(ub => ub.Status == ReadingStatus.Started).ToList();
        var startedGenres = startedBooks.Select(ub => ub.Book.Genre).Distinct().ToList();

        if (startedBooks.Count < 3 || startedGenres.Count < 2)
            return null;

        return startedGenres
            .OrderByDescending(genre =>
            {
                var genreStarted = startedBooks.Count(ub => ub.Book.Genre == genre);
                var genreDone    = userBooks.Count(ub =>
                    ub.Book.Genre == genre &&
                    (ub.Status == ReadingStatus.Finished || ub.Status == ReadingStatus.Abandoned));
                return genreDone == 0 ? double.MaxValue : (double)genreStarted / genreDone;
            })
            .First();
    }
}
