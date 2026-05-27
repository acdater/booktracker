# Story 4.1 — Stats Strip Endpoint

**Status:** review  
**Epic:** 4 — Reading Analytics  
**Story ID:** 4.1  

---

## User Story

As an **authenticated reader**,  
I want the Stats Strip to always show my current reading totals,  
So that I get an at-a-glance overview of my reading life on every Shelf visit.

---

## Acceptance Criteria

- `GET /api/stats/strip` returns HTTP 200 with `StatsStripResponse`: `{ totalBooks, finishedCount, startedCount, pagesThisMonth }`
- `totalBooks` = COUNT of ALL `UserBook` records for the user (all `readingNumbers`, all statuses)
- `finishedCount` = COUNT of `UserBook` records where `Status = Finished` for the user
- `startedCount` = COUNT of `UserBook` records where `Status = Started` for the user
- `pagesThisMonth` = SUM of `(newValue − oldValue)` for all `PageUpdate` `BookActions` where `Timestamp` falls within the **current calendar month** (NOT rolling 30 days) and the delta is positive (negative deltas ignored)
- All four values computed from `BookAction` / `UserBook` queries at request time — **no counter fields** on `User` or `UserBook` used (FR-23 hard contract)
- `IStatsService` / `StatsService` and `StatsController` are created; `StatsController` delegates to service
- Endpoint is protected (`[Authorize]`); unauthenticated requests return 401

---

## Tasks

- [x] Create `backend/BookTracker.Api/DTOs/Stats/StatsStripResponse.cs`
- [x] Add `CountAllAsync` and `CountByStatusAsync` to `IUserBookRepository` + implement in `UserBookRepository`
- [x] Add `GetPageUpdatesInMonthAsync` to `IBookActionRepository` + implement in `BookActionRepository`
- [x] Create `backend/BookTracker.Api/Services/Interfaces/IStatsService.cs`
- [x] Create `backend/BookTracker.Api/Services/StatsService.cs`
- [x] Create `backend/BookTracker.Api/Controllers/StatsController.cs`
- [x] Register `IStatsService` / `StatsService` in `Program.cs`
- [x] Run `dotnet test` — all 46 existing tests still pass (no regressions)

---

## Dev Notes

### Architecture rules (MUST follow)

| Rule | Detail |
|------|--------|
| **No concrete injection** | `StatsService` constructor takes `IUserBookRepository` and `IBookActionRepository` — never inject `AppDbContext` directly |
| **One class per file** | File name matches class name exactly |
| **Interface pairing** | `StatsService` paired with `IStatsService`; both must exist |
| **Controller stays thin** | Extract `userId` from claims, call service, return `Ok(result)`. No business logic in controller |
| **Route** | `StatsController` route = `"api/stats"`, action = `[HttpGet("strip")]` |
| **FR-23 hard contract** | All stats computed from `BookAction`/`UserBook` queries at request time — never read from counter columns |
| **Calendar month** | `pagesThisMonth` uses current calendar month (`year == now.Year && month == now.Month`), NOT rolling 30 days |
| **Positive deltas only** | Page deltas where `newValue < oldValue` are discarded (e.g. if someone manually decreased pages, don't subtract from total) |

---

### File 1 — `backend/BookTracker.Api/DTOs/Stats/StatsStripResponse.cs` (NEW)

```csharp
namespace BookTracker.Api.DTOs.Stats;

public class StatsStripResponse
{
    public int TotalBooks { get; set; }
    public int FinishedCount { get; set; }
    public int StartedCount { get; set; }
    public int PagesThisMonth { get; set; }
}
```

> **Note:** The `DTOs/Stats/` folder already has a `.gitkeep` — just add the file directly.

---

### File 2 — `IUserBookRepository.cs` (UPDATE)

Add two new methods to the interface (existing methods must NOT be removed):

```csharp
Task<int> CountAllAsync(int userId);
Task<int> CountByStatusAsync(int userId, ReadingStatus status);
```

Full updated interface (reference only — preserve all existing methods):
```csharp
using BookTracker.Api.Models;
using BookTracker.Api.Models.Enums;

namespace BookTracker.Api.Repositories.Interfaces;

public interface IUserBookRepository
{
    Task<List<UserBook>> GetShelfAsync(int userId);
    Task<UserBook?> GetByIdAsync(int id);
    Task<UserBook> CreateAsync(UserBook ub);
    Task<UserBook> CreateWithActionAsync(UserBook ub, BookAction action);
    Task<UserBook> UpdateAsync(UserBook ub);
    Task<UserBook> UpdateWithActionAsync(UserBook ub, BookAction action);
    Task<UserBook> UpdateWithActionsAsync(UserBook ub, IReadOnlyList<BookAction> actions);
    Task<int> GetMaxReadingNumberAsync(int userId, int bookId);
    Task<Dictionary<int, int>> GetReaderCountsAsync(IEnumerable<int> bookIds);
    Task<int> CountAllAsync(int userId);
    Task<int> CountByStatusAsync(int userId, ReadingStatus status);
}
```

---

### File 3 — `UserBookRepository.cs` (UPDATE)

Add implementations at the end of the class (before the closing `}`):

```csharp
public async Task<int> CountAllAsync(int userId) =>
    await _db.UserBooks
        .Where(ub => ub.UserId == userId)
        .CountAsync();

public async Task<int> CountByStatusAsync(int userId, ReadingStatus status) =>
    await _db.UserBooks
        .Where(ub => ub.UserId == userId && ub.Status == status)
        .CountAsync();
```

> **Note:** `ReadingStatus` is already used in `UserBookRepository.cs` via `GetShelfAsync` and data context — no new using needed. Confirm `using BookTracker.Api.Models.Enums;` is present; if not, add it.

---

### File 4 — `IBookActionRepository.cs` (UPDATE)

Add one new method (preserve all three existing methods):

```csharp
Task<List<BookAction>> GetPageUpdatesInMonthAsync(int userId, int year, int month);
```

Full updated interface:
```csharp
using BookTracker.Api.Models;

namespace BookTracker.Api.Repositories.Interfaces;

public interface IBookActionRepository
{
    Task AddAsync(BookAction action);
    Task<List<BookAction>> GetByUserAndBookAsync(int userId, int userBookId);
    Task<List<BookAction>> GetJournalAsync(int userId, int bookId);
    Task<List<BookAction>> GetPageUpdatesInMonthAsync(int userId, int year, int month);
}
```

---

### File 5 — `BookActionRepository.cs` (UPDATE)

Add implementation at end of class. The query must:
- Filter by `userId`
- Filter `ActionType == ActionType.PageUpdate` (using `ActionType` enum, not string)
- Filter `Timestamp.Year == year && Timestamp.Month == month`
- Return list (enumerated in service layer to compute sum)

```csharp
public async Task<List<BookAction>> GetPageUpdatesInMonthAsync(int userId, int year, int month) =>
    await _db.BookActions
        .Where(a => a.UserId == userId
            && a.ActionType == ActionType.PageUpdate
            && a.Timestamp.Year == year
            && a.Timestamp.Month == month)
        .ToListAsync();
```

> **Note:** `ActionType` enum is in `BookTracker.Api.Models.Enums`. Check `BookActionRepository.cs` already uses it; if `using BookTracker.Api.Models.Enums;` is missing, add it.

---

### File 6 — `Services/Interfaces/IStatsService.cs` (NEW)

```csharp
using BookTracker.Api.DTOs.Stats;

namespace BookTracker.Api.Services.Interfaces;

public interface IStatsService
{
    Task<StatsStripResponse> GetStripAsync(int userId);
}
```

---

### File 7 — `Services/StatsService.cs` (NEW)

```csharp
using BookTracker.Api.DTOs.Stats;
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

        var pagesThisMonth = pageActions.Sum(a =>
        {
            if (int.TryParse(a.NewValue, out var nv) && int.TryParse(a.OldValue, out var ov))
                return Math.Max(0, nv - ov);
            return 0;
        });

        return new StatsStripResponse
        {
            TotalBooks = totalBooks,
            FinishedCount = finishedCount,
            StartedCount = startedCount,
            PagesThisMonth = pagesThisMonth
        };
    }
}
```

---

### File 8 — `Controllers/StatsController.cs` (NEW)

Follow the exact same thin-controller pattern as `ShelfController.cs`:

```csharp
using System.Security.Claims;
using BookTracker.Api.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BookTracker.Api.Controllers;

[ApiController]
[Route("api/stats")]
[Authorize]
public class StatsController(IStatsService statsService) : ControllerBase
{
    [HttpGet("strip")]
    public async Task<IActionResult> GetStrip()
    {
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var result = await statsService.GetStripAsync(userId);
        return Ok(result);
    }
}
```

---

### File 9 — `Program.cs` (UPDATE)

Replace the `TODO` comment with the real registration:

```csharp
// TODO Story 4.1: Register IStatsService / StatsService
```
→ replace with:
```csharp
builder.Services.AddScoped<IStatsService, StatsService>();
```

Also add `using BookTracker.Api.Services.Interfaces;` / `using BookTracker.Api.Services;` if not already present (they already are, since `IShelfService` and `ShelfService` are registered).

---

### Current UserBookRepository.cs — current state (for reference)

```
GetShelfAsync, GetByIdAsync, CreateAsync, UpdateAsync, CreateWithActionAsync,
UpdateWithActionAsync, UpdateWithActionsAsync, GetMaxReadingNumberAsync, GetReaderCountsAsync
```
All must be preserved. Only ADD `CountAllAsync` and `CountByStatusAsync`.

### Current BookActionRepository.cs — current state (for reference)
```
AddAsync, GetByUserAndBookAsync, GetJournalAsync
```
All must be preserved. Only ADD `GetPageUpdatesInMonthAsync`.

---

## Architecture Constraints to Respect

| Constraint | Detail |
|-----------|--------|
| **No concrete DI** | Constructor parameters are interfaces only |
| **Primary keys** | `Id` (int, auto-increment) — no changes to models needed |
| **Status stored as string** | `ReadingStatus` stored as string in DB (EF conversion configured in `AppDbContext`); enum comparison in LINQ works correctly |
| **ActionType stored as string** | `ActionType.PageUpdate` stored as `"PageUpdate"` in DB; EF conversion configured — LINQ filter on enum value works |
| **CalendarMonth boundary** | `Timestamp.Year == year && Timestamp.Month == month` — correct for calendar month, not rolling window |
| **No migration needed** | No model or DB schema changes; all new code is service/controller/DTO layer only |
| **Existing 46 tests must pass** | All mock setups in `ShelfServiceTests` target `IUserBookRepository` and `IBookActionRepository` — adding methods to interfaces does NOT break existing mocks (Moq doesn't require all interface methods to be set up) |

---

## Testing Notes (Story 4.1)

- **No new test class required in Story 4.1** — `StatsServiceTests.cs` is created in Story 4.2
- **Must run `dotnet test`** to confirm all 46 existing tests still pass after adding new interface methods
- Story 4.2 will add tests that cover: `pagesThisMonth` calendar month boundary, `unfinishedGenre` threshold, and period-bucketed counts

---

## Definition of Done

- [x] `dotnet test` — all 46 existing tests pass (0 failures, 0 new tests required)
- [x] `dotnet build` succeeds with 0 errors
- [x] `GET /api/stats/strip` returns 200 with correct shape when authenticated
- [x] `GET /api/stats/strip` returns 401 when not authenticated
- [x] `totalBooks`, `finishedCount`, `startedCount` match actual UserBook counts
- [x] `pagesThisMonth` counts only PageUpdate actions in current calendar month with positive delta
- [x] `IStatsService`, `StatsService`, `StatsController` all exist and are wired in `Program.cs`
- [x] No regressions to Shelf or Auth endpoints

---

## Dev Agent Record

### File List
- `backend/BookTracker.Api/DTOs/Stats/StatsStripResponse.cs` — NEW DTO
- `backend/BookTracker.Api/Repositories/Interfaces/IUserBookRepository.cs` — added `CountAllAsync`, `CountByStatusAsync`; added `using BookTracker.Api.Models.Enums`
- `backend/BookTracker.Api/Repositories/UserBookRepository.cs` — implemented `CountAllAsync`, `CountByStatusAsync`; added `using BookTracker.Api.Models.Enums`
- `backend/BookTracker.Api/Repositories/Interfaces/IBookActionRepository.cs` — added `GetPageUpdatesInMonthAsync`
- `backend/BookTracker.Api/Repositories/BookActionRepository.cs` — implemented `GetPageUpdatesInMonthAsync`; added `using BookTracker.Api.Models.Enums`
- `backend/BookTracker.Api/Services/Interfaces/IStatsService.cs` — NEW interface
- `backend/BookTracker.Api/Services/StatsService.cs` — NEW service; computes all 4 strip values
- `backend/BookTracker.Api/Controllers/StatsController.cs` — NEW controller; thin: extracts userId, calls service
- `backend/BookTracker.Api/Program.cs` — replaced TODO comment with `AddScoped<IStatsService, StatsService>()`

### Completion Notes
- All 8 tasks complete. `dotnet test`: 46/46 passed, 0 regressions.
- `StatsService` injects `IUserBookRepository` and `IBookActionRepository` — no concrete DI, per architecture rules.
- `pagesThisMonth` filters by `ActionType.PageUpdate`, calendar month boundary (`Year == now.Year && Month == now.Month`), and `Math.Max(0, nv - ov)` discards negative deltas — satisfies FR-23 hard contract.
- Adding two new methods to `IUserBookRepository` and one to `IBookActionRepository` did not break any existing Moq mock setups (Moq does not require all interface members to be configured).

### Change Log
- 2026-05-27: Implemented Story 4.1 — Stats Strip Endpoint (StatsStripResponse DTO, CountAllAsync/CountByStatusAsync on UserBookRepository, GetPageUpdatesInMonthAsync on BookActionRepository, IStatsService/StatsService, StatsController, Program.cs DI registration)
