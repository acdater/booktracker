# Story 2.4: Add to Shelf & Shelf Display Endpoints

Status: review

<!-- Note: Validation is optional. Run validate-create-story for quality check before dev-story. -->

## Story

As an **authenticated reader**,
I want to add a catalogued book to my shelf and retrieve my full shelf,
so that I can track my personal reading list and see the most recently active book first.

## Acceptance Criteria

1. `POST /api/shelf` with `{ bookId }` from an authenticated user creates a `UserBook` with `Status = Resting`, `CurrentPages = 0`, `ReadingNumber = 1`, `LastActivityAt = DateTime.UtcNow` and returns **HTTP 201** with `UserBookResponse`
2. `GET /api/shelf` returns **HTTP 200** with an array of `UserBookResponse` ordered by `LastActivityAt DESC`
3. `GET /api/shelf` returns only the **most-recent `UserBook` per Book** — the one with the highest `ReadingNumber` for each userId+bookId pair
4. Each `UserBookResponse` exposes: `id` (int), `book` (full `BookResponse`), `status` (string), `currentPages` (int), `readingNumber` (int), `startedAt` (DateTime?, nullable), `finishedAt` (DateTime?, nullable), `lastActivityAt` (DateTime), `readerCount` (int)
5. `readerCount` = `COUNT(DISTINCT UserId)` across **all** `UserBooks` for that `BookId` (not just the requester's)
6. Nullable fields (`startedAt`, `finishedAt`, `coverImageUrl` inside `book`) serialize as JSON `null` — never omitted
7. `POST /api/shelf` and `GET /api/shelf` both require JWT authentication (`[Authorize]`)
8. `AddToShelfDto` at `DTOs/Shelf/AddToShelfDto.cs`; `UserBookResponse` at `DTOs/Shelf/UserBookResponse.cs`
9. `IShelfService` gains `AddToShelfAsync(int userId, int bookId)` and `GetShelfAsync(int userId)`; `ShelfController` at `Controllers/ShelfController.cs` delegates to service; no business logic in controller
10. All existing backend tests (27/27) still pass

## Tasks / Subtasks

- [x] Task 1: Create DTOs (AC: 4, 6, 8)
  - [x] Create `DTOs/Shelf/AddToShelfDto.cs` — `[Required] int BookId`
  - [x] Create `DTOs/Shelf/UserBookResponse.cs` — all fields from AC 4; `status` as `string`; `book` as `BookResponse`; nullable fields typed `DateTime?` / `string?`

- [x] Task 2: Add `GetReaderCountsAsync` to `IUserBookRepository` / `UserBookRepository` (AC: 5)
  - [x] Add `Task<Dictionary<int, int>> GetReaderCountsAsync(IEnumerable<int> bookIds)` to `IUserBookRepository`
  - [x] Implement in `UserBookRepository`: group all `UserBooks` where `BookId` is in `bookIds`, count `COUNT(DISTINCT UserId)` per BookId
  - [x] Returns `Dictionary<int, int>` keyed by BookId — missing entries mean 0 readers (handle gracefully in service)

- [x] Task 3: Implement `IShelfService` and `ShelfService` (AC: 1, 2, 3, 5)
  - [x] Add `Task<UserBookResponse> AddToShelfAsync(int userId, int bookId)` to `IShelfService`
  - [x] Add `Task<List<UserBookResponse>> GetShelfAsync(int userId)` to `IShelfService`
  - [x] `AddToShelfAsync` implementation:
    1. Call `_bookRepository.GetByIdAsync(bookId)` — if null throw `ApiException(404, "Book not found.", "NOT_FOUND")` — **NOTE: add `GetByIdAsync(int id)` to `IBookRepository` and `BookRepository`**
    2. Build `UserBook`: `UserId=userId, BookId=bookId, Status=ReadingStatus.Resting, CurrentPages=0, ReadingNumber=1, LastActivityAt=DateTime.UtcNow`
    3. Call `_userBookRepository.CreateAsync(ub)` — returns saved entity with `.Book` loaded
    4. Get `readerCount`: call `_userBookRepository.GetReaderCountsAsync([bookId])`, index by bookId
    5. Map to `UserBookResponse` and return
  - [x] `GetShelfAsync` implementation:
    1. Call `_userBookRepository.GetShelfAsync(userId)` — returns all UserBooks for user (with `.Book` included, ordered by `LastActivityAt DESC`)
    2. Filter to most-recent per Book: group by `BookId`, take the one with highest `ReadingNumber`, re-order by `LastActivityAt DESC`
    3. Collect all unique BookIds from the filtered list
    4. Call `_userBookRepository.GetReaderCountsAsync(bookIds)` — one batch query
    5. Map each to `UserBookResponse` using reader counts
  - [x] Private `MapToResponse(UserBook ub, int readerCount): UserBookResponse` helper in `ShelfService`

- [x] Task 4: Create `ShelfController` (AC: 1, 2, 7, 9)
  - [x] Create `Controllers/ShelfController.cs`
  - [x] `[ApiController]`, `[Route("api/shelf")]`, `[Authorize]`
  - [x] Extract `userId`: `int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!)`
  - [x] `POST /` action → `AddToShelfAsync(userId, dto.BookId)` → `StatusCode(201, result)`
  - [x] `GET /` action → `GetShelfAsync(userId)` → `Ok(result)`
  - [x] No try/catch in controller

- [x] Task 5: Write unit tests for `ShelfService` (AC: 1, 2, 3, 5)
  - [x] Test: `AddToShelfAsync` — creates UserBook with correct initial values, returns UserBookResponse with readerCount
  - [x] Test: `GetShelfAsync` — returns only highest-ReadingNumber UserBook per Book
  - [x] Test: `GetShelfAsync` — multiple books ordered by `LastActivityAt DESC`
  - [x] Test: `GetShelfAsync` — empty shelf returns empty list
  - [x] Mock `IUserBookRepository` and `IBookRepository`

- [x] Task 6: Verify all tests pass (AC: 10)
  - [x] Run `dotnet test backend/BookTracker.Tests`
  - [x] All new + existing tests pass

## Dev Notes

### ⚠️ CRITICAL: `IBookRepository` Needs `GetByIdAsync`

`AddToShelfAsync` needs to verify the book exists before creating a UserBook. `IBookRepository` currently only has `GetByISBNAsync` and `CreateAsync`. You **must** add:

```csharp
// IBookRepository.cs — ADD:
Task<Book?> GetByIdAsync(int id);

// BookRepository.cs — ADD:
public async Task<Book?> GetByIdAsync(int id) =>
    await _db.Books.FindAsync(id);
```

### ⚠️ CRITICAL: `status` in `UserBookResponse` Must Be a String

`ReadingStatus` is a C# enum. The global JSON serializer does NOT convert enums to strings by default — they'd serialize as integers (`0`, `1`, `2`, `3`). In `UserBookResponse`, declare `Status` as `string` and set it with `ub.Status.ToString()` in the mapper. This avoids any serializer configuration changes.

```csharp
public string Status { get; set; } = string.Empty;  // set as ub.Status.ToString()
```

### ⚠️ CRITICAL: `readerCount` Requires Batch Query, Not N+1

After filtering shelf to latest-per-book, collect all BookIds and call `GetReaderCountsAsync(bookIds)` **once** — do NOT call it per-book in a loop.

### ⚠️ CRITICAL: `GetShelfAsync` Most-Recent-Per-Book Filter

The repository returns ALL UserBooks for the user. The service must filter:

```csharp
var latest = userBooks
    .GroupBy(ub => ub.BookId)
    .Select(g => g.OrderByDescending(ub => ub.ReadingNumber).First())
    .OrderByDescending(ub => ub.LastActivityAt)
    .ToList();
```

This in-memory grouping is correct for the demo scale (max 100 UserBooks per user per NFR-2).

### `GetReaderCountsAsync` Implementation Pattern

```csharp
public async Task<Dictionary<int, int>> GetReaderCountsAsync(IEnumerable<int> bookIds)
{
    var ids = bookIds.ToList();
    return await _db.UserBooks
        .Where(ub => ids.Contains(ub.BookId))
        .GroupBy(ub => ub.BookId)
        .Select(g => new { BookId = g.Key, Count = g.Select(ub => ub.UserId).Distinct().Count() })
        .ToDictionaryAsync(x => x.BookId, x => x.Count);
}
```

**Note:** EF Core translates `g.Select(ub => ub.UserId).Distinct().Count()` correctly to `COUNT(DISTINCT UserId)` in SQL.

### `AddToShelfAsync` Full Implementation Pattern

```csharp
public async Task<UserBookResponse> AddToShelfAsync(int userId, int bookId)
{
    var book = await _bookRepository.GetByIdAsync(bookId)
        ?? throw new ApiException(404, "Book not found.", "NOT_FOUND");

    var ub = new UserBook
    {
        UserId = userId,
        BookId = bookId,
        Status = ReadingStatus.Resting,
        CurrentPages = 0,
        ReadingNumber = 1,
        LastActivityAt = DateTime.UtcNow
    };

    ub = await _userBookRepository.CreateAsync(ub);
    ub.Book = book;  // attach nav property (CreateAsync doesn't re-load it)

    var counts = await _userBookRepository.GetReaderCountsAsync([bookId]);
    return MapToResponse(ub, counts.GetValueOrDefault(bookId, 0));
}
```

**Important:** `UserBookRepository.CreateAsync` calls `SaveChangesAsync()` and returns the saved entity, but does not eagerly load `.Book`. You must re-attach `ub.Book = book` manually.

### `GetShelfAsync` Full Implementation Pattern

```csharp
public async Task<List<UserBookResponse>> GetShelfAsync(int userId)
{
    var all = await _userBookRepository.GetShelfAsync(userId);  // includes Book

    var latest = all
        .GroupBy(ub => ub.BookId)
        .Select(g => g.OrderByDescending(ub => ub.ReadingNumber).First())
        .OrderByDescending(ub => ub.LastActivityAt)
        .ToList();

    if (latest.Count == 0) return [];

    var bookIds = latest.Select(ub => ub.BookId).ToList();
    var counts = await _userBookRepository.GetReaderCountsAsync(bookIds);

    return latest.Select(ub => MapToResponse(ub, counts.GetValueOrDefault(ub.BookId, 0))).ToList();
}
```

### `MapToResponse` Helper

```csharp
private static UserBookResponse MapToResponse(UserBook ub, int readerCount) => new()
{
    Id = ub.Id,
    Book = new BookResponse
    {
        Id = ub.Book.Id,
        ISBN = ub.Book.ISBN,
        Title = ub.Book.Title,
        Author = ub.Book.Author,
        TotalPages = ub.Book.TotalPages,
        Genre = ub.Book.Genre,
        CoverImageUrl = ub.Book.CoverImageUrl
    },
    Status = ub.Status.ToString(),
    CurrentPages = ub.CurrentPages,
    ReadingNumber = ub.ReadingNumber,
    StartedAt = ub.StartedAt,
    FinishedAt = ub.FinishedAt,
    LastActivityAt = ub.LastActivityAt,
    ReaderCount = readerCount
};
```

### `ShelfController` Pattern

```csharp
using System.Security.Claims;
using BookTracker.Api.DTOs.Shelf;
using BookTracker.Api.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/shelf")]
[Authorize]
public class ShelfController(IShelfService shelfService) : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> AddToShelf([FromBody] AddToShelfDto dto)
    {
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var result = await shelfService.AddToShelfAsync(userId, dto.BookId);
        return StatusCode(201, result);
    }

    [HttpGet]
    public async Task<IActionResult> GetShelf()
    {
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var result = await shelfService.GetShelfAsync(userId);
        return Ok(result);
    }
}
```

### `UserBookResponse` DTO Exact Shape

```csharp
using BookTracker.Api.DTOs.Books;

namespace BookTracker.Api.DTOs.Shelf;

public class UserBookResponse
{
    public int Id { get; set; }
    public BookResponse Book { get; set; } = null!;
    public string Status { get; set; } = string.Empty;
    public int CurrentPages { get; set; }
    public int ReadingNumber { get; set; }
    public DateTime? StartedAt { get; set; }
    public DateTime? FinishedAt { get; set; }
    public DateTime LastActivityAt { get; set; }
    public int ReaderCount { get; set; }
}
```

### `AddToShelfDto` Exact Shape

```csharp
using System.ComponentModel.DataAnnotations;

namespace BookTracker.Api.DTOs.Shelf;

public class AddToShelfDto
{
    [Required]
    public int BookId { get; set; }
}
```

### No New EF Migration Required

Story 2.4 introduces no new database tables or columns. The `UserBooks` table exists from Story 2.1 migration. Do **NOT** run `dotnet ef migrations add`.

### Files to Modify (Existing)

- `Repositories/Interfaces/IUserBookRepository.cs` — add `GetReaderCountsAsync`
- `Repositories/UserBookRepository.cs` — implement `GetReaderCountsAsync`
- `Repositories/Interfaces/IBookRepository.cs` — add `GetByIdAsync`
- `Repositories/BookRepository.cs` — implement `GetByIdAsync`
- `Services/Interfaces/IShelfService.cs` — add `AddToShelfAsync` + `GetShelfAsync`
- `Services/ShelfService.cs` — full implementation

### Files to Create (New)

- `DTOs/Shelf/AddToShelfDto.cs`
- `DTOs/Shelf/UserBookResponse.cs`
- `Controllers/ShelfController.cs`
- `Tests/Services/ShelfServiceTests.cs`

### ShelfService Constructor Injection

`ShelfService` already has stub constructor injecting `IUserBookRepository` and `IBookRepository`. Do **NOT** change the constructor signature — just add the implementation to the existing methods.

```csharp
// Already in ShelfService:
public class ShelfService(IUserBookRepository userBookRepository, IBookRepository bookRepository) : IShelfService
{
    private readonly IUserBookRepository _userBookRepository = userBookRepository;
    private readonly IBookRepository _bookRepository = bookRepository;
    // Implementations added in Story 2.4  ← replace this comment
}
```

### Testing Pattern (from existing `BookServiceTests.cs`)

```csharp
var repoMock = new Mock<IUserBookRepository>();
var bookRepoMock = new Mock<IBookRepository>();
var sut = new ShelfService(repoMock.Object, bookRepoMock.Object);
```

For `GetReaderCountsAsync`, return a pre-built dictionary:
```csharp
repoMock.Setup(r => r.GetReaderCountsAsync(It.IsAny<IEnumerable<int>>()))
    .ReturnsAsync(new Dictionary<int, int> { [42] = 3 });
```

### `using` Directives Needed in `ShelfService.cs`

```csharp
using BookTracker.Api.DTOs.Books;
using BookTracker.Api.DTOs.Shelf;
using BookTracker.Api.Exceptions;
using BookTracker.Api.Models;
using BookTracker.Api.Models.Enums;
using BookTracker.Api.Repositories.Interfaces;
using BookTracker.Api.Services.Interfaces;
```

### AR-6 Reminder

`LastActivityAt` (UTC datetime) is set by `ShelfService` on every mutation. For `AddToShelfAsync`, set `LastActivityAt = DateTime.UtcNow`. Story 2.4 only creates UserBooks; mutations (status changes, page updates) come in Stories 3.1 and 3.2 where `LastActivityAt` is also updated.

## Dev Agent Record

### Agent Model Used

claude-sonnet-4.6

### Debug Log References

### Completion Notes List

- All 6 tasks complete. 7 new tests added; 34/34 pass total.
- Added `GetByIdAsync(int id)` to `IBookRepository` + `BookRepository` (prerequisite for AddToShelfAsync book existence check)
- Added `GetReaderCountsAsync(IEnumerable<int> bookIds)` to `IUserBookRepository` + `UserBookRepository` — uses EF COUNT(DISTINCT UserId) grouping
- `IShelfService` now has `AddToShelfAsync` + `GetShelfAsync`; `ShelfService` fully implemented with `MapToResponse` helper
- `ShelfController`: `POST /api/shelf` → 201, `GET /api/shelf` → 200, both `[Authorize]`
- Status field serialized as string via `ub.Status.ToString()` — no serializer changes needed
- Most-recent-per-book logic is in-memory LINQ grouping (appropriate for demo scale)

### File List

- `backend/BookTracker.Api/DTOs/Shelf/AddToShelfDto.cs` (new)
- `backend/BookTracker.Api/DTOs/Shelf/UserBookResponse.cs` (new)
- `backend/BookTracker.Api/Controllers/ShelfController.cs` (new)
- `backend/BookTracker.Tests/Services/ShelfServiceTests.cs` (new)
- `backend/BookTracker.Api/Repositories/Interfaces/IBookRepository.cs` (modified — added GetByIdAsync)
- `backend/BookTracker.Api/Repositories/BookRepository.cs` (modified — added GetByIdAsync)
- `backend/BookTracker.Api/Repositories/Interfaces/IUserBookRepository.cs` (modified — added GetReaderCountsAsync)
- `backend/BookTracker.Api/Repositories/UserBookRepository.cs` (modified — added GetReaderCountsAsync)
- `backend/BookTracker.Api/Services/Interfaces/IShelfService.cs` (modified — added AddToShelfAsync + GetShelfAsync)
- `backend/BookTracker.Api/Services/ShelfService.cs` (modified — full implementation)
