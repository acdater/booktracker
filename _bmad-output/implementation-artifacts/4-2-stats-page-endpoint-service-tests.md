# Story 4.2: Stats Page Endpoint & Service Tests

**Epic:** 4 — Reading Analytics  
**Story ID:** 4.2  
**Story Key:** 4-2-stats-page-endpoint-service-tests  
**Status:** ready-for-dev

---

## User Story

As an **authenticated reader**,  
I want the Stats Page to show my complete reading analytics across six time windows,  
So that I can understand my reading patterns in depth.

---

## Acceptance Criteria

- **AC-1:** `GET /api/stats` (authenticated) returns HTTP 200 with `StatsPageResponse` containing `byStatus`, `booksCompleted`, `pagesRead`, and `unfinishedGenre` fields.
- **AC-2 (FR-19):** `byStatus` = `{ total, resting, started, finished, abandoned }` — count of current `UserBook` records per status.
- **AC-3 (FR-20):** `booksCompleted` = `{ days7, days30, days90, days180, days270, days365 }` — count of `StatusChange` BookActions where `NewValue = "Finished"` and `Timestamp >= now − N days` (rolling windows).
- **AC-4 (FR-21):** `pagesRead` = `{ days7, days30, days90, days180, days270, days365 }` — sum of `max(0, newValue − oldValue)` for `PageUpdate` BookActions where `Timestamp >= now − N days`.
- **AC-5 (FR-22):** `unfinishedGenre`:
  - If user has ≥ 3 `UserBooks` with `Status = Started` across ≥ 2 distinct genres → return genre with highest ratio of `Started / (Finished + Abandoned)`.
  - Otherwise → return `null`.
- **AC-6:** `GET /api/stats` returns HTTP 401 if not authenticated.
- **AC-7 (SM-3):** `StatsServiceTests.cs` in `BookTracker.Tests/Services/` covers:
  - Correct period-bucketed values from mocked repo data.
  - `pagesThisMonth` uses calendar month boundary (not rolling 30 days).
  - `unfinishedGenre` returns correct genre when threshold met; returns `null` below threshold.
  - Repository is called each time (no caching).

---

## Tasks

- [ ] **Task 1: Add new DTOs**
  - Create `backend/BookTracker.Api/DTOs/Stats/PeriodCounts.cs` — `{ Days7, Days30, Days90, Days180, Days270, Days365 }` (all `int`).
  - Create `backend/BookTracker.Api/DTOs/Stats/ByStatusCounts.cs` — `{ Total, Resting, Started, Finished, Abandoned }` (all `int`).
  - Create `backend/BookTracker.Api/DTOs/Stats/StatsPageResponse.cs` — `{ ByStatus: ByStatusCounts, BooksCompleted: PeriodCounts, PagesRead: PeriodCounts, UnfinishedGenre: string? }`.

- [ ] **Task 2: Add new repository methods**
  - Add `Task<List<BookAction>> GetStatusChangesCompletedSinceAsync(int userId, DateTime since)` to `IBookActionRepository` and implement in `BookActionRepository` (filter: `ActionType == StatusChange && NewValue == "Finished" && Timestamp >= since`).
  - Add `Task<List<BookAction>> GetPageUpdatesSinceAsync(int userId, DateTime since)` to `IBookActionRepository` and implement in `BookActionRepository` (filter: `ActionType == PageUpdate && Timestamp >= since`).

- [ ] **Task 3: Extend `IStatsService` and implement `GetPageAsync`**
  - Add `Task<StatsPageResponse> GetPageAsync(int userId)` to `IStatsService`.
  - Implement in `StatsService`:
    - `ByStatus`: call `CountAllAsync` + four `CountByStatusAsync` calls (Resting, Started, Finished, Abandoned).
    - `BooksCompleted`: call `GetStatusChangesCompletedSinceAsync(userId, now.AddDays(-365))`; filter in-memory for each window.
    - `PagesRead`: call `GetPageUpdatesSinceAsync(userId, now.AddDays(-365))`; filter by window and sum `Math.Max(0, nv - ov)` in-memory.
    - `UnfinishedGenre`: call `_userBookRepository.GetShelfAsync(userId)` (returns all UserBooks with Book nav); apply FR-22 logic (see Dev Notes).

- [ ] **Task 4: Add `GET /api/stats` endpoint to `StatsController`**
  - Add `[HttpGet]` action `GetStats()` to `StatsController` — extracts userId from claims, calls `statsService.GetPageAsync(userId)`, returns `Ok(result)`.

- [ ] **Task 5: Write `StatsServiceTests.cs`**
  - Create `backend/BookTracker.Tests/Services/StatsServiceTests.cs`.
  - Use Moq pattern (same as `ShelfServiceTests.cs`): mock `IUserBookRepository` + `IBookActionRepository`.
  - Test cases (see Dev Notes for full code):
    - `GetStripAsync_PagesThisMonth_CountsCalendarMonthOnly` — action in current month counted; previous-month action not counted by `GetPageUpdatesInMonthAsync` (calendar month, not rolling).
    - `GetStripAsync_NegativePageDelta_NotCounted` — backward page movement not added to `PagesThisMonth`.
    - `GetPageAsync_BooksCompleted_CountsCorrectPerWindow` — 6 completions at different ages fall into correct window buckets.
    - `GetPageAsync_PagesRead_SumsPositiveDeltasPerWindow` — rolling window sums, negative delta skipped.
    - `GetPageAsync_UnfinishedGenre_ReturnsTopGenreWhenThresholdMet` — ≥3 started across ≥2 genres, returns genre with highest Started/(Finished+Abandoned) ratio.
    - `GetPageAsync_UnfinishedGenre_ReturnsNullWhenBelowThreshold` — only 2 started books → null.
    - `GetPageAsync_UnfinishedGenre_ReturnsNullWhenStartedAcrossOnlyOneGenre` — 3 started in same genre → null.
    - `GetStripAsync_NoCaching_RepositoryCalledEachTime` — two calls → repo called twice.

- [ ] **Task 6: Run `dotnet test` — all tests pass (≥ 8 new tests + 46 existing = ≥ 54 total, 0 failures)**

---

## Dev Notes

### Architecture Constraints (MUST follow)
- **No direct `AppDbContext` injection in services.** `StatsService` already uses `IUserBookRepository` + `IBookActionRepository`. Keep this pattern.
- **No caching.** All values computed fresh each call. No `static` fields, no `MemoryCache`.
- **FR-23 hard contract:** All stats computed from `BookAction` queries (plus `UserBook` status counts). No counter columns.
- **Test pattern: Moq.** `BookTracker.Tests` uses Moq + xUnit (see `ShelfServiceTests.cs`). Do NOT use EF in-memory DB.
- **Enum stored as strings in DB.** `ReadingStatus` and `ActionType` are stored as their string names. In LINQ filter, use `a.NewValue == "Finished"` (string literal), not `ReadingStatus.Finished.ToString()`.

---

### Existing Code to Understand

**`StatsService.cs`** (already exists — add `GetPageAsync` to it):
```csharp
// Already has: GetStripAsync(int userId)
// Inject: IUserBookRepository + IBookActionRepository (primary ctor params)
```

**`IStatsService.cs`** (already exists — add one method):
```csharp
Task<StatsPageResponse> GetPageAsync(int userId);  // ADD this
```

**`StatsController.cs`** (already exists — add one action):
```csharp
[HttpGet]  // maps to GET /api/stats
public async Task<IActionResult> GetStats() { ... }
```

**`IUserBookRepository.GetShelfAsync`** returns `List<UserBook>` with `Book` nav prop loaded. All rows for the user (all reading numbers). Safe to reuse for genre analysis.

**`BookActionRepository` pattern** (see `GetPageUpdatesInMonthAsync` for style):
```csharp
public async Task<List<BookAction>> GetStatusChangesCompletedSinceAsync(int userId, DateTime since) =>
    await _db.BookActions
        .Where(a => a.UserId == userId
            && a.ActionType == ActionType.StatusChange
            && a.NewValue == "Finished"
            && a.Timestamp >= since)
        .ToListAsync();

public async Task<List<BookAction>> GetPageUpdatesSinceAsync(int userId, DateTime since) =>
    await _db.BookActions
        .Where(a => a.UserId == userId
            && a.ActionType == ActionType.PageUpdate
            && a.Timestamp >= since)
        .ToListAsync();
```

---

### Complete `GetPageAsync` Implementation

```csharp
public async Task<StatsPageResponse> GetPageAsync(int userId)
{
    var now = DateTime.UtcNow;
    var since365 = now.AddDays(-365);

    // ByStatus — 5 repo calls (reuse existing methods)
    var total    = await _userBookRepository.CountAllAsync(userId);
    var resting  = await _userBookRepository.CountByStatusAsync(userId, ReadingStatus.Resting);
    var started  = await _userBookRepository.CountByStatusAsync(userId, ReadingStatus.Started);
    var finished = await _userBookRepository.CountByStatusAsync(userId, ReadingStatus.Finished);
    var abandoned= await _userBookRepository.CountByStatusAsync(userId, ReadingStatus.Abandoned);

    // BooksCompleted — one DB call, 6 in-memory filters
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
            Total = total, Resting = resting, Started = started,
            Finished = finished, Abandoned = abandoned
        },
        BooksCompleted = booksCompleted,
        PagesRead = pagesRead,
        UnfinishedGenre = unfinishedGenre
    };
}

private static int SumPositiveDeltas(List<BookAction> actions, DateTime since) =>
    actions
        .Where(a => a.Timestamp >= since)
        .Sum(a =>
        {
            if (int.TryParse(a.NewValue, out var nv) && int.TryParse(a.OldValue, out var ov))
                return Math.Max(0, nv - ov);
            return 0;
        });

private static string? ComputeUnfinishedGenre(List<UserBook> userBooks)
{
    // FR-22: ≥3 Started across ≥2 distinct genres → genre with highest Started/(Finished+Abandoned) ratio
    var startedBooks = userBooks.Where(ub => ub.Status == ReadingStatus.Started).ToList();
    var startedGenres = startedBooks.Select(ub => ub.Book.Genre).Distinct().ToList();

    if (startedBooks.Count < 3 || startedGenres.Count < 2)
        return null;

    // For each genre: count Started and (Finished+Abandoned)
    // Ratio = Started / (Finished+Abandoned); if denominator is 0, treat as infinity
    return startedGenres
        .OrderByDescending(genre =>
        {
            var genreStarted  = startedBooks.Count(ub => ub.Book.Genre == genre);
            var genreDone     = userBooks.Count(ub =>
                ub.Book.Genre == genre &&
                (ub.Status == ReadingStatus.Finished || ub.Status == ReadingStatus.Abandoned));
            return genreDone == 0 ? double.MaxValue : (double)genreStarted / genreDone;
        })
        .First();
}
```

---

### Complete `StatsServiceTests.cs`

```csharp
using BookTracker.Api.Models;
using BookTracker.Api.Models.Enums;
using BookTracker.Api.Repositories.Interfaces;
using BookTracker.Api.Services;
using Moq;

namespace BookTracker.Tests.Services;

public class StatsServiceTests
{
    private readonly Mock<IUserBookRepository>   _userBookRepoMock  = new();
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
        Book   = new Book { Genre = genre, ISBN = $"978-{id}", Title = $"Book{id}", Author = "A", TotalPages = 200 }
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
        // GetPageUpdatesInMonthAsync is called with current year+month (calendar, not rolling 30d)
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
        // Verify it used calendar month (not rolling 30d) — called with year/month, not a since date
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
                MakePageUpdate(now, 100, 50),  // backward: -50 → ignored
                MakePageUpdate(now, 50, 80)    // forward:  +30 → counted
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

        // 6 completions — one in each time window
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
                MakePageUpdate(now.AddDays(-5),  0,   100),  // +100, within all windows
                MakePageUpdate(now.AddDays(-20), 100, 50),   // -50 delta → skipped
                MakePageUpdate(now.AddDays(-60), 200, 250)   // +50, within 90d+
            ]);

        var sut    = CreateSut();
        var result = await sut.GetPageAsync(1);

        Assert.Equal(100, result.PagesRead.Days7);
        Assert.Equal(100, result.PagesRead.Days30); // negative delta not counted
        Assert.Equal(150, result.PagesRead.Days90); // 100 + 50
        Assert.Equal(150, result.PagesRead.Days180);
        Assert.Equal(150, result.PagesRead.Days270);
        Assert.Equal(150, result.PagesRead.Days365);
    }

    // ── GetPageAsync — UnfinishedGenre ────────────────────────────────────────

    [Fact]
    public async Task GetPageAsync_UnfinishedGenre_ReturnsTopGenreWhenThresholdMet()
    {
        SetupEmptyPageMocks();

        // Fantasy: 2 Started, 1 Finished → ratio = 2/1 = 2.0
        // Sci-Fi:  2 Started, 0 Finished → ratio = infinity
        // Total started = 4, genres = 2 → threshold met → Sci-Fi wins
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

        // Only 2 Started — below threshold of 3
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
```

---

### DTO JSON Shape Reference (for AC validation)

```json
{
  "byStatus": {
    "total": 12,
    "resting": 5,
    "started": 3,
    "finished": 3,
    "abandoned": 1
  },
  "booksCompleted": {
    "days7": 1,
    "days30": 2,
    "days90": 4,
    "days180": 6,
    "days270": 7,
    "days365": 10
  },
  "pagesRead": {
    "days7": 120,
    "days30": 380,
    "days90": 950,
    "days180": 1800,
    "days270": 2400,
    "days365": 3100
  },
  "unfinishedGenre": "Fantasy"
}
```

ASP.NET default JSON serialization: PascalCase → camelCase via `JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase` (already configured globally in `Program.cs`).

---

### Files to Create / Modify

| File | Action |
|------|--------|
| `backend/BookTracker.Api/DTOs/Stats/PeriodCounts.cs` | CREATE |
| `backend/BookTracker.Api/DTOs/Stats/ByStatusCounts.cs` | CREATE |
| `backend/BookTracker.Api/DTOs/Stats/StatsPageResponse.cs` | CREATE |
| `backend/BookTracker.Api/Repositories/Interfaces/IBookActionRepository.cs` | UPDATE — add 2 methods |
| `backend/BookTracker.Api/Repositories/BookActionRepository.cs` | UPDATE — implement 2 methods |
| `backend/BookTracker.Api/Services/Interfaces/IStatsService.cs` | UPDATE — add `GetPageAsync` |
| `backend/BookTracker.Api/Services/StatsService.cs` | UPDATE — implement `GetPageAsync` + helpers |
| `backend/BookTracker.Api/Controllers/StatsController.cs` | UPDATE — add `GetStats()` action |
| `backend/BookTracker.Tests/Services/StatsServiceTests.cs` | CREATE |

**Do NOT modify:** `Program.cs`, `UserBookRepository.cs`, `IUserBookRepository.cs`, `AppDbContext.cs`, any migration files, or any frontend file.

---

## Dev Agent Record

### Implementation Plan
_(to be filled by dev agent)_

### Debug Log
_(to be filled by dev agent)_

### Completion Notes
_(to be filled by dev agent)_

### Change Log
_(to be filled by dev agent)_
