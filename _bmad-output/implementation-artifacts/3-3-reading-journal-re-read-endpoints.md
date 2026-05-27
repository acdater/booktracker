# Story 3.3: Reading Journal & Re-read Endpoints

Status: review

## Story

As a **reader**,
I want to view the full event history for a book and start a new reading of a finished/abandoned book,
So that my reading memoir is preserved and each re-read is independent.

## Acceptance Criteria

1. `GET /api/shelf/{userBookId}/journal` returns HTTP 200 with an array of `JournalEntryResponse` for ALL `BookActions` across **all** `UserBooks` for this User + Book pair (all readingNumbers), ordered by `Timestamp DESC`
2. Each `JournalEntryResponse` includes: `readingNumber` (int), `actionType` (human-readable string: "Status Change" or "Page Update"), `oldValue` (string), `newValue` (string), `timestamp` (ISO 8601 UTC)
3. Journal endpoint is **read-only** — no create, update, or delete endpoints for `BookAction` exist in this story
4. `POST /api/shelf/{userBookId}/reread` is valid **only** when `UserBook.Status == Finished` or `Abandoned`; returns HTTP 201 with the new `UserBookResponse`
5. `RereadAsync` creates a new `UserBook`: `Status = Resting`, `CurrentPages = 0`, `ReadingNumber = GetMaxReadingNumberAsync(userId, bookId) + 1`, `StartedAt = null`, `FinishedAt = null`, `LastActivityAt = DateTime.UtcNow`
6. The prior `UserBook` and **all** its `BookActions` are completely untouched by `RereadAsync`
7. `POST /api/shelf/{userBookId}/reread` on a `Resting` or `Started` UserBook returns HTTP 400 `{ "error": "Read Again is only available for Finished or Abandoned books.", "code": "INVALID_STATE" }`
8. Ownership mismatch on either endpoint returns HTTP 403 `{ "code": "FORBIDDEN" }`
9. `IBookActionRepository` gains `GetJournalAsync(int userId, int bookId)` — returns `List<BookAction>` with `UserBook` navigation loaded (for `ReadingNumber`), filtered by `userId` + `UserBook.BookId`, ordered by `Timestamp DESC`
10. Unit tests in `ShelfServiceTests.cs` cover: journal returns entries across readingNumbers; journal ownership throws; reread creates correct new UserBook; reread on Resting/Started throws; reread ownership throws

## Tasks / Subtasks

- [x] Task 1: Add `GetJournalAsync` to `IBookActionRepository` and implement (AC: 1, 2, 9)
  - [x] Add to `backend/BookTracker.Api/Repositories/Interfaces/IBookActionRepository.cs`:
    ```csharp
    Task<List<BookAction>> GetJournalAsync(int userId, int bookId);
    ```
  - [x] Implement in `backend/BookTracker.Api/Repositories/BookActionRepository.cs`:
    ```csharp
    public async Task<List<BookAction>> GetJournalAsync(int userId, int bookId) =>
        await _db.BookActions
            .Include(a => a.UserBook)
            .Where(a => a.UserId == userId && a.UserBook.BookId == bookId)
            .OrderByDescending(a => a.Timestamp)
            .ToListAsync();
    ```
  - [x] Note: `BookAction.UserBook` nav property already configured in `AppDbContext.OnModelCreating` — no migration needed

- [x] Task 2: Create `JournalEntryResponse` DTO (AC: 2)
  - [x] Create `backend/BookTracker.Api/DTOs/Shelf/JournalEntryResponse.cs`:

- [x] Task 3: Inject `IBookActionRepository` into `ShelfService` and update `IShelfService` (AC: 1, 4)
  - [x] Add to `backend/BookTracker.Api/Services/Interfaces/IShelfService.cs`
  - [x] Update `ShelfService` constructor to inject `IBookActionRepository`
  - [x] `IBookActionRepository` is already registered in `Program.cs` (from Story 3.1) — **no change to Program.cs needed**

- [x] Task 4: Implement `GetJournalAsync` in `ShelfService` (AC: 1, 2, 3, 8)
  - [x] Implemented in `backend/BookTracker.Api/Services/ShelfService.cs`

- [x] Task 5: Implement `RereadAsync` in `ShelfService` (AC: 4, 5, 6, 7, 8)
  - [x] Implemented in `backend/BookTracker.Api/Services/ShelfService.cs`

- [x] Task 6: Add endpoints to `ShelfController` (AC: 1, 4)
  - [x] Added `GET {userBookId}/journal` and `POST {userBookId}/reread` to `backend/BookTracker.Api/Controllers/ShelfController.cs`

- [x] Task 7: Update `ShelfServiceTests.cs` — add mock, update `CreateSut()`, add tests (AC: 10)
  - [x] Added `_bookActionRepoMock` field; updated `CreateSut()` to pass 3 args
  - [x] All existing tests remain valid ✅
  - [x] Added 5 new tests (GetJournal valid + ownership, Reread valid + Started + ownership)

- [x] Task 8: Build and test verification (AC: all)
  - [x] Run `dotnet build` from `backend/BookTracker.Api/` — zero errors ✅
  - [x] Run `dotnet test` from `backend/BookTracker.Tests/` — 46 tests pass (0 failures) ✅
  - [ ] Add to `backend/BookTracker.Api/Repositories/Interfaces/IBookActionRepository.cs`:
    ```csharp
    Task<List<BookAction>> GetJournalAsync(int userId, int bookId);
    ```
  - [ ] Implement in `backend/BookTracker.Api/Repositories/BookActionRepository.cs`:
    ```csharp
    public async Task<List<BookAction>> GetJournalAsync(int userId, int bookId) =>
        await _db.BookActions
            .Include(a => a.UserBook)
            .Where(a => a.UserId == userId && a.UserBook.BookId == bookId)
            .OrderByDescending(a => a.Timestamp)
            .ToListAsync();
    ```
  - [ ] Note: `BookAction.UserBook` nav property already configured in `AppDbContext.OnModelCreating` — no migration needed

- [ ] Task 2: Create `JournalEntryResponse` DTO (AC: 2)
  - [ ] Create `backend/BookTracker.Api/DTOs/Shelf/JournalEntryResponse.cs`:
    ```csharp
    namespace BookTracker.Api.DTOs.Shelf;

    public class JournalEntryResponse
    {
        public int ReadingNumber { get; set; }
        public string ActionType { get; set; } = "";
        public string? OldValue { get; set; }
        public string? NewValue { get; set; }
        public DateTime Timestamp { get; set; }
    }
    ```

- [ ] Task 3: Inject `IBookActionRepository` into `ShelfService` and update `IShelfService` (AC: 1, 4)
  - [ ] Add to `backend/BookTracker.Api/Services/Interfaces/IShelfService.cs`:
    ```csharp
    Task<List<JournalEntryResponse>> GetJournalAsync(int userId, int userBookId);
    Task<UserBookResponse> RereadAsync(int userId, int userBookId);
    ```
  - [ ] Update `ShelfService` constructor to inject `IBookActionRepository`:
    ```csharp
    public class ShelfService(
        IUserBookRepository userBookRepository,
        IBookRepository bookRepository,
        IBookActionRepository bookActionRepository) : IShelfService
    ```
  - [ ] Add private readonly field: `private readonly IBookActionRepository _bookActionRepository = bookActionRepository;`
  - [ ] `IBookActionRepository` is already registered in `Program.cs` (from Story 3.1) — **no change to Program.cs needed**

- [ ] Task 4: Implement `GetJournalAsync` in `ShelfService` (AC: 1, 2, 3, 8)
  - [ ] Add `GetJournalAsync` to `ShelfService`:
    ```csharp
    public async Task<List<JournalEntryResponse>> GetJournalAsync(int userId, int userBookId)
    {
        var ub = await _userBookRepository.GetByIdAsync(userBookId)
            ?? throw new ApiException(404, "UserBook not found.", "NOT_FOUND");

        if (ub.UserId != userId)
            throw new ApiException(403, "Access denied.", "FORBIDDEN");

        var actions = await _bookActionRepository.GetJournalAsync(userId, ub.BookId);

        return actions.Select(a => new JournalEntryResponse
        {
            ReadingNumber = a.UserBook.ReadingNumber,
            ActionType = a.ActionType == Models.Enums.ActionType.StatusChange
                ? "Status Change"
                : "Page Update",
            OldValue = a.OldValue,
            NewValue = a.NewValue,
            Timestamp = a.Timestamp
        }).ToList();
    }
    ```

- [ ] Task 5: Implement `RereadAsync` in `ShelfService` (AC: 4, 5, 6, 7, 8)
  - [ ] Add `RereadAsync` to `ShelfService`:
    ```csharp
    public async Task<UserBookResponse> RereadAsync(int userId, int userBookId)
    {
        var ub = await _userBookRepository.GetByIdAsync(userBookId)
            ?? throw new ApiException(404, "UserBook not found.", "NOT_FOUND");

        if (ub.UserId != userId)
            throw new ApiException(403, "Access denied.", "FORBIDDEN");

        if (ub.Status != ReadingStatus.Finished && ub.Status != ReadingStatus.Abandoned)
            throw new ApiException(400, "Read Again is only available for Finished or Abandoned books.", "INVALID_STATE");

        var maxReadingNumber = await _userBookRepository.GetMaxReadingNumberAsync(userId, ub.BookId);

        var newUb = new UserBook
        {
            UserId = userId,
            BookId = ub.BookId,
            Status = ReadingStatus.Resting,
            CurrentPages = 0,
            ReadingNumber = maxReadingNumber + 1,
            StartedAt = null,
            FinishedAt = null,
            LastActivityAt = DateTime.UtcNow
        };

        newUb = await _userBookRepository.CreateAsync(newUb);
        newUb.Book = ub.Book;  // ub.Book is loaded by GetByIdAsync via .Include

        var counts = await _userBookRepository.GetReaderCountsAsync([newUb.BookId]);
        return MapToResponse(newUb, counts.GetValueOrDefault(newUb.BookId, 0));
    }
    ```

- [ ] Task 6: Add endpoints to `ShelfController` (AC: 1, 4)
  - [ ] Add to `backend/BookTracker.Api/Controllers/ShelfController.cs`:
    ```csharp
    [HttpGet("{userBookId}/journal")]
    public async Task<IActionResult> GetJournal(int userBookId)
    {
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var result = await shelfService.GetJournalAsync(userId, userBookId);
        return Ok(result);
    }

    [HttpPost("{userBookId}/reread")]
    public async Task<IActionResult> Reread(int userBookId)
    {
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var result = await shelfService.RereadAsync(userId, userBookId);
        return StatusCode(201, result);
    }
    ```

- [ ] Task 7: Update `ShelfServiceTests.cs` — add mock, update `CreateSut()`, add tests (AC: 10)
  - [ ] Add `_bookActionRepoMock` field and update `CreateSut()`:
    ```csharp
    private readonly Mock<IBookActionRepository> _bookActionRepoMock = new();

    private ShelfService CreateSut() => new(
        _userBookRepoMock.Object,
        _bookRepoMock.Object,
        _bookActionRepoMock.Object);
    ```
  - [ ] All existing tests remain valid — new mock parameter doesn't affect them since it's never called by existing methods
  - [ ] Add 5 tests — see **Dev Notes** for exact patterns

- [ ] Task 8: Build and test verification (AC: all)
  - [ ] Run `dotnet build` from `backend/BookTracker.Api/` — zero errors
  - [ ] Run `dotnet test` from `backend/BookTracker.Tests/` — all tests pass (existing 41 + 5 new = 46)

## Dev Notes

### Journal Query Strategy

The journal must show ALL `BookActions` across ALL readingNumbers for a User+Book pair. `BookAction` has a navigation property `BookAction.UserBook` (with `ReadingNumber`) configured as a FK relationship in `AppDbContext`. Use EF `.Include(a => a.UserBook)` + `.Where(a => a.UserBook.BookId == bookId)` to filter across all readingNumbers in a single query — no N+1 issue.

**Do NOT use the existing `GetByUserAndBookAsync(userId, userBookId)` for the journal** — that method filters by a single `userBookId` (UserBook PK), not `bookId` (Book PK). They are different parameters.

### ActionType Human-Readable Labels

The `ActionType` enum values must be converted to human-readable labels:
- `ActionType.StatusChange` → `"Status Change"`
- `ActionType.PageUpdate` → `"Page Update"`

Do this mapping in `ShelfService.GetJournalAsync` using a simple ternary — no switch needed since there are only two values.

### `ShelfService` Constructor Change

Adding `IBookActionRepository` as the third constructor parameter. `IBookActionRepository` is already registered in `Program.cs` DI (registered in Story 3.1). **No change to `Program.cs` needed.**

However, `ShelfServiceTests.CreateSut()` must be updated to pass the new mock. All 41 existing tests will continue to pass — the new dependency is only called in the two new methods, and existing tests don't exercise them.

### `RereadAsync` — `GetByIdAsync` Loads Book

`UserBookRepository.GetByIdAsync` uses `.Include(ub => ub.Book)` — so `ub.Book` is available after fetch. After `CreateAsync(newUb)`, set `newUb.Book = ub.Book` (same Book entity) to enable `MapToResponse` without a second book lookup.

### `GetMaxReadingNumberAsync` Already Exists

`IUserBookRepository.GetMaxReadingNumberAsync(int userId, int bookId)` was added in Story 2.1 and is implemented in `UserBookRepository`. It takes `bookId` (the Book PK), not `userBookId`. Use `ub.BookId` (from the loaded `UserBook`) when calling it.

### State Machine After Story 3.3

| Status | How set |
|--------|---------|
| `Resting` | AddToShelf (initial), **RereadAsync (new UserBook)** |
| `Started` | `PATCH /api/shelf/{id}/status` with `"Started"` |
| `Abandoned` | `PATCH /api/shelf/{id}/status` with `"Abandoned"` |
| `Finished` | Auto-set by `UpdatePagesAsync` when pages == totalPages |

`UpdateStatusAsync.validTransitions` does NOT need to change for Story 3.3 — re-read is a NEW UserBook creation, not a status transition on the existing one.

### No EF Migration Required

`BookAction.UserBook` nav property and the FK are already configured in `AppDbContext.OnModelCreating` from Story 3.1. The `GetJournalAsync` query uses `.Include(a => a.UserBook)` which only reads — no schema change needed.

### Existing Files to Modify

| File | Change |
|------|--------|
| `Repositories/Interfaces/IBookActionRepository.cs` | Add `GetJournalAsync(int userId, int bookId)` |
| `Repositories/BookActionRepository.cs` | Implement `GetJournalAsync` |
| `Services/Interfaces/IShelfService.cs` | Add `GetJournalAsync` and `RereadAsync` |
| `Services/ShelfService.cs` | Inject `IBookActionRepository`, implement both methods |
| `Controllers/ShelfController.cs` | Add GET `journal` and POST `reread` endpoints |
| `Tests/Services/ShelfServiceTests.cs` | Add `_bookActionRepoMock`, update `CreateSut()`, add 5 tests |

### New Files to Create

| File | Purpose |
|------|---------|
| `DTOs/Shelf/JournalEntryResponse.cs` | Response DTO for journal entries |

### Test Patterns for Task 7

All tests use the shared `_userBookRepoMock`, `_bookRepoMock`, `_bookActionRepoMock` fields and the updated `CreateSut()`.

**Test 1 — Journal returns entries ordered Timestamp DESC across readingNumbers:**
```csharp
[Fact]
public async Task GetJournalAsync_ValidRequest_ReturnsJournalEntriesAcrossReadingNumbers()
{
    var book = MakeBook(10);
    var ub = MakeUserBook(1, 7, book);

    _userBookRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(ub);

    var ub2 = MakeUserBook(2, 7, book, readingNumber: 2);
    var actions = new List<BookAction>
    {
        new() { Id = 1, UserId = 7, UserBookId = 2, UserBook = ub2,
                ActionType = ActionType.StatusChange, OldValue = "Resting", NewValue = "Started",
                Timestamp = DateTime.UtcNow },
        new() { Id = 2, UserId = 7, UserBookId = 1, UserBook = ub,
                ActionType = ActionType.PageUpdate, OldValue = "0", NewValue = "100",
                Timestamp = DateTime.UtcNow.AddDays(-1) }
    };

    _bookActionRepoMock.Setup(r => r.GetJournalAsync(7, book.Id)).ReturnsAsync(actions);

    var sut = CreateSut();
    var result = await sut.GetJournalAsync(7, 1);

    Assert.Equal(2, result.Count);
    Assert.Equal(2, result[0].ReadingNumber);        // ub2.ReadingNumber = 2
    Assert.Equal("Status Change", result[0].ActionType);
    Assert.Equal(1, result[1].ReadingNumber);        // ub.ReadingNumber = 1
    Assert.Equal("Page Update", result[1].ActionType);
}
```

**Test 2 — Journal ownership check:**
```csharp
[Fact]
public async Task GetJournalAsync_WrongOwner_Throws403()
{
    var book = MakeBook(10);
    var ub = MakeUserBook(1, 99, book);  // owned by userId=99

    _userBookRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(ub);

    var sut = CreateSut();
    var ex = await Assert.ThrowsAsync<ApiException>(() => sut.GetJournalAsync(7, 1));

    Assert.Equal(403, ex.StatusCode);
}
```

**Test 3 — Reread creates correct new UserBook:**
```csharp
[Fact]
public async Task RereadAsync_FinishedBook_CreatesNewUserBookWithCorrectValues()
{
    var book = MakeBook(10);
    var ub = MakeUserBook(1, 7, book);
    ub.Status = ReadingStatus.Finished;

    _userBookRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(ub);
    _userBookRepoMock.Setup(r => r.GetMaxReadingNumberAsync(7, book.Id)).ReturnsAsync(1);

    UserBook? captured = null;
    _userBookRepoMock.Setup(r => r.CreateAsync(It.IsAny<UserBook>()))
        .Callback<UserBook>(u => captured = u)
        .ReturnsAsync((UserBook u) => u);
    _userBookRepoMock.Setup(r => r.GetReaderCountsAsync(It.IsAny<IEnumerable<int>>()))
        .ReturnsAsync(new Dictionary<int, int> { [book.Id] = 1 });

    var sut = CreateSut();
    var result = await sut.RereadAsync(7, 1);

    Assert.NotNull(captured);
    Assert.Equal(ReadingStatus.Resting, captured.Status);
    Assert.Equal(0, captured.CurrentPages);
    Assert.Equal(2, captured.ReadingNumber);  // MAX(1) + 1
    Assert.Null(captured.StartedAt);
    Assert.Null(captured.FinishedAt);
    Assert.Equal("Resting", result.Status);
}
```

**Test 4 — Reread on Started book throws 400:**
```csharp
[Fact]
public async Task RereadAsync_StartedBook_Throws400InvalidState()
{
    var book = MakeBook(10);
    var ub = MakeUserBook(1, 7, book);
    ub.Status = ReadingStatus.Started;

    _userBookRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(ub);

    var sut = CreateSut();
    var ex = await Assert.ThrowsAsync<ApiException>(() => sut.RereadAsync(7, 1));

    Assert.Equal(400, ex.StatusCode);
    Assert.Equal("INVALID_STATE", ex.ErrorCode);
}
```

**Test 5 — Reread ownership check:**
```csharp
[Fact]
public async Task RereadAsync_WrongOwner_Throws403()
{
    var book = MakeBook(10);
    var ub = MakeUserBook(1, 99, book);  // owned by userId=99
    ub.Status = ReadingStatus.Finished;

    _userBookRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(ub);

    var sut = CreateSut();
    var ex = await Assert.ThrowsAsync<ApiException>(() => sut.RereadAsync(7, 1));

    Assert.Equal(403, ex.StatusCode);
}
```

### Error Codes Reference

| Condition | HTTP | Code |
|-----------|------|------|
| UserBook not found | 404 | `NOT_FOUND` |
| Wrong user | 403 | `FORBIDDEN` |
| Status not Finished/Abandoned (reread) | 400 | `INVALID_STATE` |

### ApiException Pattern

```csharp
throw new ApiException(statusCode, message, code);
// Handled by ExceptionHandlingMiddleware → { "error": "...", "code": "..." }
```

### From Previous Stories

- `ShelfService` constructor currently: `(IUserBookRepository, IBookRepository)` — becomes `(IUserBookRepository, IBookRepository, IBookActionRepository)`
- `GetByIdAsync` uses `.Include(ub => ub.Book)` — `ub.Book.TotalPages` and `ub.BookId` are always accessible
- `GetMaxReadingNumberAsync(int userId, int bookId)` is on `IUserBookRepository` — takes `bookId` (Book PK), not `userBookId`
- `CreateAsync` on `IUserBookRepository` returns the created entity
- All error handling via `ApiException` — no try/catch in controllers
- `IBookActionRepository` already DI-registered in `Program.cs` from Story 3.1

---

## Dev Agent Record

### Agent Model Used

claude-sonnet-4.6

### Debug Log References

### Completion Notes List
Story 3.3 implemented fully in a single session. All 8 tasks completed.

- Added `GetJournalAsync(int userId, int bookId)` to `IBookActionRepository` / `BookActionRepository` — queries across all UserBooks for a User+Book pair using EF `.Include(a => a.UserBook)` navigation, ordered Timestamp DESC
- Created `JournalEntryResponse` DTO with `ReadingNumber`, `ActionType` (human label), `OldValue`, `NewValue`, `Timestamp`
- Updated `ShelfService` constructor: added `IBookActionRepository` as 3rd parameter (already DI-registered from Story 3.1 — no Program.cs change needed)
- Implemented `GetJournalAsync` in `ShelfService`: ownership check, delegates to `IBookActionRepository.GetJournalAsync`, maps `ActionType` enum to human labels
- Implemented `RereadAsync` in `ShelfService`: ownership + state guard (Finished/Abandoned only), calls `GetMaxReadingNumberAsync` for ReadingNumber+1, creates new UserBook via `CreateAsync`
- Added `GET /api/shelf/{userBookId}/journal` and `POST /api/shelf/{userBookId}/reread` endpoints to `ShelfController`
- Updated `CreateSut()` in `ShelfServiceTests.cs` to pass 3rd mock; added 5 tests; all 46 tests pass (0 regressions)

### File List
**New files:**
- `backend/BookTracker.Api/DTOs/Shelf/JournalEntryResponse.cs`

**Modified files:**
- `backend/BookTracker.Api/Repositories/Interfaces/IBookActionRepository.cs`
- `backend/BookTracker.Api/Repositories/BookActionRepository.cs`
- `backend/BookTracker.Api/Services/Interfaces/IShelfService.cs`
- `backend/BookTracker.Api/Services/ShelfService.cs`
- `backend/BookTracker.Api/Controllers/ShelfController.cs`
- `backend/BookTracker.Tests/Services/ShelfServiceTests.cs`

### Change Log
- Added `GetJournalAsync` to `IBookActionRepository` / `BookActionRepository` for cross-readingNumber journal queries (2026-05-27)
- Created `JournalEntryResponse` DTO (2026-05-27)
- Added `GetJournalAsync` and `RereadAsync` to `IShelfService` / `ShelfService`; injected `IBookActionRepository` (2026-05-27)
- Added `GET /api/shelf/{id}/journal` and `POST /api/shelf/{id}/reread` endpoints (2026-05-27)
- Added 5 unit tests; updated `CreateSut()` for new constructor parameter (2026-05-27)
