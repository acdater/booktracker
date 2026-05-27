using BookTracker.Api.Models;
using BookTracker.Api.Models.Enums;
using BookTracker.Api.Repositories.Interfaces;
using BookTracker.Api.Services;
using Moq;

namespace BookTracker.Tests.Services;

public class StatsServiceTests
{
    private readonly Mock<IUserBookRepository>   _userBookRepoMock   = new();
    private readonly Mock<IBookActionRepository> _bookActionRepoMock = new();

    private StatsService CreateSut() => new(_userBookRepoMock.Object, _bookActionRepoMock.Object);

    // ── Helpers ──────────────────────────────────────────────────────────────

    private static BookAction MakePageUpdate(DateTime ts, int oldVal, int newVal) => new()
    {
        ActionType = ActionType.PageUpdate,
        OldValue   = oldVal.ToString(),
        NewValue   = newVal.ToString(),
        Timestamp  = ts
    };

    private static BookAction MakeCompletion(DateTime ts) => new()
    {
        ActionType = ActionType.StatusChange,
        OldValue   = "Started",
        NewValue   = "Finished",
        Timestamp  = ts
    };

    private static UserBook MakeUserBook(int id, ReadingStatus status, string genre) => new()
    {
        Id     = id,
        Status = status,
        Book   = new Book { Id = id, Genre = genre, ISBN = $"978-{id}", Title = $"Book{id}", Author = "A", TotalPages = 200 }
    };

    private void SetupDefaultStrip()
    {
        var now = DateTime.UtcNow;
        _userBookRepoMock.Setup(r => r.CountAllAsync(1)).ReturnsAsync(5);
        _userBookRepoMock.Setup(r => r.CountByStatusAsync(1, ReadingStatus.Finished)).ReturnsAsync(2);
        _userBookRepoMock.Setup(r => r.CountByStatusAsync(1, ReadingStatus.Started)).ReturnsAsync(1);
        _bookActionRepoMock.Setup(r => r.GetPageUpdatesInMonthAsync(1, now.Year, now.Month)).ReturnsAsync([]);
    }

    private void SetupEmptyPageMocks()
    {
        _userBookRepoMock.Setup(r => r.CountAllAsync(It.IsAny<int>())).ReturnsAsync(0);
        _userBookRepoMock.Setup(r => r.CountByStatusAsync(It.IsAny<int>(), It.IsAny<ReadingStatus>())).ReturnsAsync(0);
        _userBookRepoMock.Setup(r => r.GetShelfAsync(It.IsAny<int>())).ReturnsAsync([]);
        _bookActionRepoMock.Setup(r => r.GetStatusChangesCompletedSinceAsync(It.IsAny<int>(), It.IsAny<DateTime>())).ReturnsAsync([]);
        _bookActionRepoMock.Setup(r => r.GetPageUpdatesSinceAsync(It.IsAny<int>(), It.IsAny<DateTime>())).ReturnsAsync([]);
    }

    // ── GetStripAsync ─────────────────────────────────────────────────────────

    [Fact]
    public async Task GetStripAsync_PagesThisMonth_CountsCalendarMonthBoundary()
    {
        // GetPageUpdatesInMonthAsync is called with current year+month (calendar, not rolling 30 days)
        var now = DateTime.UtcNow;
        _userBookRepoMock.Setup(r => r.CountAllAsync(1)).ReturnsAsync(3);
        _userBookRepoMock.Setup(r => r.CountByStatusAsync(1, ReadingStatus.Finished)).ReturnsAsync(1);
        _userBookRepoMock.Setup(r => r.CountByStatusAsync(1, ReadingStatus.Started)).ReturnsAsync(1);
        _bookActionRepoMock
            .Setup(r => r.GetPageUpdatesInMonthAsync(1, now.Year, now.Month))
            .ReturnsAsync([MakePageUpdate(DateTime.UtcNow, 0, 75)]);

        var sut    = CreateSut();
        var result = await sut.GetStripAsync(1);

        Assert.Equal(75, result.PagesThisMonth);
        // Verify calendar month used (year+month params, not a since date)
        _bookActionRepoMock.Verify(r => r.GetPageUpdatesInMonthAsync(1, now.Year, now.Month), Times.Once);
    }

    [Fact]
    public async Task GetStripAsync_NegativePageDelta_NotCounted()
    {
        var now = DateTime.UtcNow;
        _userBookRepoMock.Setup(r => r.CountAllAsync(1)).ReturnsAsync(1);
        _userBookRepoMock.Setup(r => r.CountByStatusAsync(1, It.IsAny<ReadingStatus>())).ReturnsAsync(0);
        _bookActionRepoMock
            .Setup(r => r.GetPageUpdatesInMonthAsync(1, now.Year, now.Month))
            .ReturnsAsync([
                MakePageUpdate(now, 100, 50), // backward: -50 → ignored
                MakePageUpdate(now, 50,  80)  // forward:  +30 → counted
            ]);

        var sut    = CreateSut();
        var result = await sut.GetStripAsync(1);

        Assert.Equal(30, result.PagesThisMonth);
    }

    // ── GetPageAsync — BooksCompleted ─────────────────────────────────────────

    [Fact]
    public async Task GetPageAsync_BooksCompleted_CountsCorrectPerRollingWindow()
    {
        var now = DateTime.UtcNow;
        SetupEmptyPageMocks();

        // One completion inside each rolling window
        _bookActionRepoMock
            .Setup(r => r.GetStatusChangesCompletedSinceAsync(1, It.IsAny<DateTime>()))
            .ReturnsAsync([
                MakeCompletion(now.AddDays(-5)),   // within 7d
                MakeCompletion(now.AddDays(-20)),  // within 30d
                MakeCompletion(now.AddDays(-60)),  // within 90d
                MakeCompletion(now.AddDays(-120)), // within 180d
                MakeCompletion(now.AddDays(-200)), // within 270d
                MakeCompletion(now.AddDays(-300))  // within 365d
            ]);

        var sut    = CreateSut();
        var result = await sut.GetPageAsync(1);

        Assert.Equal(1, result.BooksCompleted.Days7);
        Assert.Equal(2, result.BooksCompleted.Days30);
        Assert.Equal(3, result.BooksCompleted.Days90);
        Assert.Equal(4, result.BooksCompleted.Days180);
        Assert.Equal(5, result.BooksCompleted.Days270);
        Assert.Equal(6, result.BooksCompleted.Days365);
    }

    // ── GetPageAsync — PagesRead ──────────────────────────────────────────────

    [Fact]
    public async Task GetPageAsync_PagesRead_SumsPositiveDeltasPerRollingWindow()
    {
        var now = DateTime.UtcNow;
        SetupEmptyPageMocks();

        _bookActionRepoMock
            .Setup(r => r.GetPageUpdatesSinceAsync(1, It.IsAny<DateTime>()))
            .ReturnsAsync([
                MakePageUpdate(now.AddDays(-5),  0,   100), // +100, within all windows
                MakePageUpdate(now.AddDays(-20), 100, 50),  // -50 delta → skipped (negative)
                MakePageUpdate(now.AddDays(-60), 200, 250)  // +50, within 90d+
            ]);

        var sut    = CreateSut();
        var result = await sut.GetPageAsync(1);

        Assert.Equal(100, result.PagesRead.Days7);
        Assert.Equal(100, result.PagesRead.Days30);  // negative delta not counted
        Assert.Equal(150, result.PagesRead.Days90);  // 100 + 50
        Assert.Equal(150, result.PagesRead.Days180);
        Assert.Equal(150, result.PagesRead.Days270);
        Assert.Equal(150, result.PagesRead.Days365);
    }

    // ── GetPageAsync — UnfinishedGenre ────────────────────────────────────────

    [Fact]
    public async Task GetPageAsync_UnfinishedGenre_ReturnsTopGenreWhenThresholdMet()
    {
        SetupEmptyPageMocks();

        // Fantasy: 2 Started, 1 Finished → ratio = 2.0
        // Sci-Fi:  2 Started, 0 Finished → ratio = infinity
        // Total started = 4, genres = 2 → threshold met → Sci-Fi wins (higher ratio)
        _userBookRepoMock.Setup(r => r.GetShelfAsync(1)).ReturnsAsync([
            MakeUserBook(1, ReadingStatus.Started,  "Fantasy"),
            MakeUserBook(2, ReadingStatus.Started,  "Fantasy"),
            MakeUserBook(3, ReadingStatus.Finished, "Fantasy"),
            MakeUserBook(4, ReadingStatus.Started,  "Sci-Fi"),
            MakeUserBook(5, ReadingStatus.Started,  "Sci-Fi")
        ]);

        var sut    = CreateSut();
        var result = await sut.GetPageAsync(1);

        Assert.Equal("Sci-Fi", result.UnfinishedGenre);
    }

    [Fact]
    public async Task GetPageAsync_UnfinishedGenre_ReturnsNullWhenFewerThanThreeStarted()
    {
        SetupEmptyPageMocks();

        // Only 2 Started books — below threshold of 3
        _userBookRepoMock.Setup(r => r.GetShelfAsync(1)).ReturnsAsync([
            MakeUserBook(1, ReadingStatus.Started, "Fantasy"),
            MakeUserBook(2, ReadingStatus.Started, "Sci-Fi")
        ]);

        var sut    = CreateSut();
        var result = await sut.GetPageAsync(1);

        Assert.Null(result.UnfinishedGenre);
    }

    [Fact]
    public async Task GetPageAsync_UnfinishedGenre_ReturnsNullWhenStartedInFewerThanTwoGenres()
    {
        SetupEmptyPageMocks();

        // 3 Started but all in same genre — only 1 distinct genre, below threshold of 2
        _userBookRepoMock.Setup(r => r.GetShelfAsync(1)).ReturnsAsync([
            MakeUserBook(1, ReadingStatus.Started, "Fantasy"),
            MakeUserBook(2, ReadingStatus.Started, "Fantasy"),
            MakeUserBook(3, ReadingStatus.Started, "Fantasy")
        ]);

        var sut    = CreateSut();
        var result = await sut.GetPageAsync(1);

        Assert.Null(result.UnfinishedGenre);
    }

    // ── No caching ────────────────────────────────────────────────────────────

    [Fact]
    public async Task GetStripAsync_NoCaching_RepositoryCalledOnEveryCall()
    {
        SetupDefaultStrip();

        var sut = CreateSut();
        await sut.GetStripAsync(1);
        await sut.GetStripAsync(1);

        _userBookRepoMock.Verify(r => r.CountAllAsync(1), Times.Exactly(2));
    }
}
