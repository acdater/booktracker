# Story 3.1: BookAction Domain Model & Status Transition Endpoint

Status: review

## Story

As an **authenticated reader**,
I want to change the reading status of a book on my shelf (Start or Abandon),
So that my shelf accurately reflects the current state of each reading attempt, with every status change logged as an auditable BookAction.

## Acceptance Criteria

1. A `BookAction` entity exists in the domain with fields: `Id`, `UserId` (FK→Users), `UserBookId` (FK→UserBooks), `ActionType` (stored as string), `OldValue` (string, nullable), `NewValue` (string, nullable), `Timestamp` (DateTime UTC)
2. An `ActionType` enum exists at `Models/Enums/ActionType.cs` with values `StatusChange` and `PageUpdate`
3. Two composite indexes exist on `BookActions`: `IX_BookActions_UserId_Timestamp` and `IX_BookActions_UserId_UserBookId`
4. `PATCH /api/shelf/{userBookId}/status` accepts `{ "status": "Started" }` or `{ "status": "Abandoned" }` and returns the updated `UserBookResponse` (200)
5. Valid transitions for this story: `Resting → Started` and `Started → Abandoned` only — any other requested transition returns 400 with `{ "error": "...", "code": "INVALID_TRANSITION" }`
6. Requesting a status change on a UserBook that belongs to another user returns 403 with `{ "code": "FORBIDDEN" }`
7. Requesting a status change on a non-existent UserBook returns 404 with `{ "code": "NOT_FOUND" }`
8. When a status transition succeeds: `UserBook.Status` is updated; `StartedAt` is set (UTC) on →Started; `FinishedAt` is set (UTC) on →Abandoned; `LastActivityAt` is set to `UtcNow` on every transition; a `BookAction` row (type=`StatusChange`) is persisted — all in a **single `SaveChangesAsync()` call** (AR-9)
9. `IUserBookRepository` gains a new method `UpdateWithActionAsync(UserBook ub, BookAction action)` that stages both entity changes and saves atomically
10. An EF migration `BookActionModel` is generated and applied
11. Unit tests in `ShelfServiceTests.cs` cover: valid Resting→Started transition, invalid transition throws, ownership mismatch throws

## Tasks / Subtasks

- [x] Task 1: Create `ActionType` enum and `BookAction` entity (AC: 1, 2)
  - [x] Create `backend/BookTracker.Api/Models/Enums/ActionType.cs`:
    ```csharp
    namespace BookTracker.Api.Models.Enums;
    public enum ActionType { StatusChange, PageUpdate }
    ```
  - [x] Create `backend/BookTracker.Api/Models/BookAction.cs`:
    ```csharp
    using BookTracker.Api.Models.Enums;
    namespace BookTracker.Api.Models;
    public class BookAction
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public User User { get; set; } = null!;
        public int UserBookId { get; set; }
        public UserBook UserBook { get; set; } = null!;
        public ActionType ActionType { get; set; }
        public string? OldValue { get; set; }
        public string? NewValue { get; set; }
        public DateTime Timestamp { get; set; }
    }
    ```

- [x] Task 2: Update `AppDbContext` with BookActions configuration (AC: 1, 3, 10)
  - [x] Add `public DbSet<BookAction> BookActions => Set<BookAction>();` to `AppDbContext`
  - [x] In `OnModelCreating`, add BookAction entity configuration:
    ```csharp
    modelBuilder.Entity<BookAction>(entity =>
    {
        entity.Property(a => a.ActionType).HasConversion<string>();

        entity.HasOne(a => a.User)
              .WithMany()
              .HasForeignKey(a => a.UserId)
              .OnDelete(DeleteBehavior.Cascade);

        entity.HasOne(a => a.UserBook)
              .WithMany()
              .HasForeignKey(a => a.UserBookId)
              .OnDelete(DeleteBehavior.Cascade);

        entity.HasIndex(a => new { a.UserId, a.Timestamp })
              .HasDatabaseName("IX_BookActions_UserId_Timestamp");

        entity.HasIndex(a => new { a.UserId, a.UserBookId })
              .HasDatabaseName("IX_BookActions_UserId_UserBookId");
    });
    ```

- [x] Task 3: Create `IBookActionRepository` and `BookActionRepository` (AC: 1)
  - [x] Create `backend/BookTracker.Api/Repositories/Interfaces/IBookActionRepository.cs`:
    ```csharp
    using BookTracker.Api.Models;
    namespace BookTracker.Api.Repositories.Interfaces;
    public interface IBookActionRepository
    {
        Task AddAsync(BookAction action);
        Task<List<BookAction>> GetByUserAndBookAsync(int userId, int userBookId);
    }
    ```
  - [x] Create `backend/BookTracker.Api/Repositories/BookActionRepository.cs`:
    ```csharp
    using BookTracker.Api.Data;
    using BookTracker.Api.Models;
    using BookTracker.Api.Repositories.Interfaces;
    using Microsoft.EntityFrameworkCore;
    namespace BookTracker.Api.Repositories;
    public class BookActionRepository(AppDbContext db) : IBookActionRepository
    {
        private readonly AppDbContext _db = db;
        public async Task AddAsync(BookAction action)
        {
            _db.BookActions.Add(action);
            await _db.SaveChangesAsync();
        }
        public async Task<List<BookAction>> GetByUserAndBookAsync(int userId, int userBookId) =>
            await _db.BookActions
                .Where(a => a.UserId == userId && a.UserBookId == userBookId)
                .OrderBy(a => a.Timestamp)
                .ToListAsync();
    }
    ```
  - [x] Register in `Program.cs` alongside other repositories:
    ```csharp
    builder.Services.AddScoped<IBookActionRepository, BookActionRepository>();
    ```

- [x] Task 4: Add `UpdateWithActionAsync` to `IUserBookRepository` (AC: 8, 9)
  - [x] Add to `IUserBookRepository.cs`:
    ```csharp
    Task<UserBook> UpdateWithActionAsync(UserBook ub, BookAction action);
    ```
  - [x] Implement in `UserBookRepository.cs`:
    ```csharp
    public async Task<UserBook> UpdateWithActionAsync(UserBook ub, BookAction action)
    {
        _db.UserBooks.Update(ub);
        _db.BookActions.Add(action);
        await _db.SaveChangesAsync();
        return ub;
    }
    ```
  - [x] Note: This method stages BOTH the UserBook update AND the BookAction insert in the SAME `SaveChangesAsync()` call — this is the AR-9 atomicity pattern for this project. Both entities are tracked by the same scoped `AppDbContext` instance.

- [x] Task 5: Create `UpdateStatusDto` (AC: 4)
  - [x] Create `backend/BookTracker.Api/DTOs/Shelf/UpdateStatusDto.cs`:
    ```csharp
    using System.ComponentModel.DataAnnotations;
    namespace BookTracker.Api.DTOs.Shelf;
    public class UpdateStatusDto
    {
        [Required] public string Status { get; set; } = string.Empty;
    }
    ```

- [x] Task 6: Add `UpdateStatusAsync` to `IShelfService` and implement in `ShelfService` (AC: 4, 5, 6, 7, 8)
  - [x] Add to `IShelfService.cs`:
    ```csharp
    Task<UserBookResponse> UpdateStatusAsync(int userId, int userBookId, string status);
    ```
  - [x] Implement `UpdateStatusAsync` in `ShelfService.cs`:
    ```csharp
    public async Task<UserBookResponse> UpdateStatusAsync(int userId, int userBookId, string status)
    {
        var ub = await _userBookRepository.GetByIdAsync(userBookId)
            ?? throw new ApiException(404, "UserBook not found.", "NOT_FOUND");

        if (ub.UserId != userId)
            throw new ApiException(403, "Access denied.", "FORBIDDEN");

        // Parse and validate requested status
        if (!Enum.TryParse<ReadingStatus>(status, ignoreCase: true, out var newStatus))
            throw new ApiException(400, $"Invalid status: {status}.", "INVALID_TRANSITION");

        // Validate state machine transitions (Story 3.1: only Resting→Started and Started→Abandoned)
        var validTransitions = new Dictionary<ReadingStatus, ReadingStatus>
        {
            [ReadingStatus.Resting] = ReadingStatus.Started,
            [ReadingStatus.Started] = ReadingStatus.Abandoned
        };

        if (!validTransitions.TryGetValue(ub.Status, out var allowedTarget) || allowedTarget != newStatus)
            throw new ApiException(400, $"Cannot transition from {ub.Status} to {newStatus}.", "INVALID_TRANSITION");

        var oldStatus = ub.Status;
        ub.Status = newStatus;
        ub.LastActivityAt = DateTime.UtcNow;

        if (newStatus == ReadingStatus.Started) ub.StartedAt = DateTime.UtcNow;
        if (newStatus == ReadingStatus.Abandoned) ub.FinishedAt = DateTime.UtcNow;

        var action = new BookAction
        {
            UserId = userId,
            UserBookId = userBookId,
            ActionType = ActionType.StatusChange,
            OldValue = oldStatus.ToString(),
            NewValue = newStatus.ToString(),
            Timestamp = DateTime.UtcNow
        };

        ub = await _userBookRepository.UpdateWithActionAsync(ub, action);

        var counts = await _userBookRepository.GetReaderCountsAsync([ub.BookId]);
        return MapToResponse(ub, counts.GetValueOrDefault(ub.BookId, 0));
    }
    ```
  - [x] Add required using at top of ShelfService.cs: `using BookTracker.Api.Models.Enums;`

- [x] Task 7: Add `PATCH /api/shelf/{userBookId}/status` endpoint to `ShelfController` (AC: 4, 5, 6, 7)
  - [x] Add to `ShelfController.cs`:
    ```csharp
    [HttpPatch("{userBookId}/status")]
    public async Task<IActionResult> UpdateStatus(int userBookId, [FromBody] UpdateStatusDto dto)
    {
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var result = await shelfService.UpdateStatusAsync(userId, userBookId, dto.Status);
        return Ok(result);
    }
    ```

- [x] Task 8: Generate and apply EF migration (AC: 10)
  - [x] From `backend/BookTracker.Api/` directory:
    ```bash
    dotnet ef migrations add BookActionModel
    dotnet ef database update
    ```
  - [x] Verify migration file created in `Migrations/` folder

- [x] Task 9: Add unit tests for `UpdateStatusAsync` (AC: 11)
  - [x] In `ShelfServiceTests.cs`, add `Mock<IUserBookRepository>` setup for `UpdateWithActionAsync`:
    - Add to `CreateSut()` fields (no constructor change needed since `ShelfService` still takes only `IUserBookRepository` + `IBookRepository`)
    - See **Dev Notes** for exact test patterns
  - [x] Test: `UpdateStatusAsync_ValidRestingToStarted_UpdatesStatusAndCreatesAction`:
    - Setup: `GetByIdAsync(1)` returns UserBook with Status=Resting, UserId=userId
    - Setup: `UpdateWithActionAsync` captures both args and returns the ub
    - Assert: captured UserBook has Status=Started, StartedAt not null, LastActivityAt updated
    - Assert: captured BookAction has ActionType=StatusChange, OldValue="Resting", NewValue="Started"
    - Assert: `UpdateWithActionAsync` called exactly once (single save = AR-9 satisfied)
  - [x] Test: `UpdateStatusAsync_InvalidTransition_Throws400`:
    - Setup: `GetByIdAsync(1)` returns UserBook with Status=Abandoned, UserId=userId
    - Assert: `UpdateStatusAsync(userId, 1, "Started")` throws `ApiException` with StatusCode=400, ErrorCode="INVALID_TRANSITION"
    - Assert: `UpdateWithActionAsync` never called
  - [x] Test: `UpdateStatusAsync_OwnershipMismatch_Throws403`:
    - Setup: `GetByIdAsync(1)` returns UserBook with UserId=99 (different user)
    - Assert: `UpdateStatusAsync(1, 1, "Started")` throws `ApiException` with StatusCode=403, ErrorCode="FORBIDDEN"

- [x] Task 10: Build and test verification
  - [x] Run `dotnet build` from `backend/` — zero errors ✅
  - [x] Run `dotnet test` from `backend/` — all tests pass (37 total, including 3 new) ✅

## Dev Notes

### AR-9: Atomicity Pattern

**Critical**: Every `UserBook` mutation and its associated `BookAction` MUST be persisted in a **single `SaveChangesAsync()` call**. This is achieved via the new `IUserBookRepository.UpdateWithActionAsync(UserBook, BookAction)` method. Do NOT call `_userBookRepository.UpdateAsync(ub)` then separately call any other save — this would violate AR-9.

The reason this works: EF Core's `AppDbContext` is registered as `Scoped` (one instance per HTTP request). All repositories injected into the same service share the same `AppDbContext` instance. So `UserBookRepository.UpdateWithActionAsync` can stage both `UserBooks.Update(ub)` AND `BookActions.Add(action)` on the same context and commit both atomically in one `SaveChangesAsync()`.

### State Machine Transitions (Story 3.1)

Only two transitions are valid in this story:
```
Resting  →  Started    (user clicks "Start Reading")
Started  →  Abandoned  (user clicks "Abandon")
```

**Future transitions** (do NOT implement in this story, just be aware):
- Started → Finished (auto on page-complete, Story 3.2)
- Finished → Resting (re-read, Story 3.3)
- Abandoned → Resting (re-read, Story 3.3)

The `validTransitions` dictionary in `UpdateStatusAsync` is intentionally kept as a `Dictionary<ReadingStatus, ReadingStatus>` (one-to-one). When Story 3.3 adds multi-target transitions (Finished/Abandoned → Resting), it will need to change to `Dictionary<ReadingStatus, List<ReadingStatus>>`. **Do NOT pre-optimize this in Story 3.1**.

### Timestamp Semantics

- `→ Started`: set `StartedAt = DateTime.UtcNow`, leave `FinishedAt` as is (null at this point)
- `→ Abandoned`: set `FinishedAt = DateTime.UtcNow`, leave `StartedAt` unchanged
- EVERY transition: set `LastActivityAt = DateTime.UtcNow`

### ShelfService Constructor

`ShelfService` does NOT need a new constructor parameter in Story 3.1. The `IBookActionRepository` is created and registered but is not yet needed by `ShelfService` — it will be used in Story 3.3 (journal). The current constructor stays:
```csharp
public class ShelfService(IUserBookRepository userBookRepository, IBookRepository bookRepository) : IShelfService
```

### Existing Files to Modify

| File | Change |
|------|--------|
| `Data/AppDbContext.cs` | Add `DbSet<BookAction>`, entity config + indexes |
| `Repositories/Interfaces/IUserBookRepository.cs` | Add `UpdateWithActionAsync` |
| `Repositories/UserBookRepository.cs` | Implement `UpdateWithActionAsync` |
| `Services/Interfaces/IShelfService.cs` | Add `UpdateStatusAsync` |
| `Services/ShelfService.cs` | Implement `UpdateStatusAsync`, add using |
| `Controllers/ShelfController.cs` | Add PATCH endpoint |
| `Program.cs` | Register `IBookActionRepository` |
| `Tests/Services/ShelfServiceTests.cs` | Add 3 new tests |

### New Files to Create

| File | Purpose |
|------|---------|
| `Models/Enums/ActionType.cs` | ActionType enum |
| `Models/BookAction.cs` | BookAction entity |
| `Repositories/Interfaces/IBookActionRepository.cs` | Repository interface |
| `Repositories/BookActionRepository.cs` | Repository implementation |
| `DTOs/Shelf/UpdateStatusDto.cs` | Request DTO |

### Test Pattern for New Tests

The existing `ShelfServiceTests.cs` already has `_userBookRepoMock` and `CreateSut()`. The new tests mock `UpdateWithActionAsync` and capture the BookAction argument:

```csharp
BookAction? capturedAction = null;
UserBook? capturedUb = null;

_userBookRepoMock
    .Setup(r => r.UpdateWithActionAsync(It.IsAny<UserBook>(), It.IsAny<BookAction>()))
    .Callback<UserBook, BookAction>((ub, a) => { capturedUb = ub; capturedAction = a; })
    .ReturnsAsync((UserBook ub, BookAction _) => ub);
```

Verify atomicity (single call):
```csharp
_userBookRepoMock.Verify(r => r.UpdateWithActionAsync(It.IsAny<UserBook>(), It.IsAny<BookAction>()), Times.Once);
```

### Existing Test Infrastructure

The `MakeUserBook` helper in `ShelfServiceTests.cs` already creates `UserBook` with Status. To create a UserBook with a specific status, look for overloads (or add `status: ReadingStatus.Resting` parameter). The `CreateSut()` method returns a `new ShelfService(_userBookRepoMock.Object, _bookRepoMock.Object)` — this does NOT change in Story 3.1.

### Error Codes Reference

| Condition | HTTP | Code |
|-----------|------|------|
| UserBook not found | 404 | `NOT_FOUND` |
| Different user's UserBook | 403 | `FORBIDDEN` |
| Invalid status string or invalid transition | 400 | `INVALID_TRANSITION` |

All exceptions use `ApiException(statusCode, message, code)` — `ExceptionHandlingMiddleware` converts to `{ "error": "...", "code": "..." }` automatically.

### EF Migration Command

From `backend/BookTracker.Api/` (where the `.csproj` is):
```bash
dotnet ef migrations add BookActionModel
dotnet ef database update
```

If you run from a different directory, add `--project backend/BookTracker.Api --startup-project backend/BookTracker.Api`.

The migration will generate the `BookActions` table with FKs to `Users` and `UserBooks`, cascade deletes, string-stored `ActionType`, and both composite indexes.

---

## Dev Agent Record

### Completion Notes
Story 3.1 implemented fully. All 10 tasks completed in a single session.

- Created `ActionType` enum and `BookAction` domain entity with FKs to Users and UserBooks
- Added `BookActions` DbSet to `AppDbContext` with `HasConversion<string>()` for ActionType and both composite indexes (`IX_BookActions_UserId_Timestamp`, `IX_BookActions_UserId_UserBookId`)
- Created `IBookActionRepository` / `BookActionRepository` (AddAsync, GetByUserAndBookAsync)
- Added `UpdateWithActionAsync(UserBook, BookAction)` to `IUserBookRepository` / `UserBookRepository` — this is the AR-9 atomicity pattern: single `SaveChangesAsync()` saves both entities via the shared scoped `AppDbContext`
- Created `UpdateStatusDto` with `[Required]` attribute
- Implemented `UpdateStatusAsync` in `ShelfService` with full state machine validation (Resting→Started, Started→Abandoned only for this story), 403 ownership check, 404 not-found, timestamps set correctly
- Added `PATCH /api/shelf/{userBookId}/status` endpoint to `ShelfController`
- Registered `IBookActionRepository` in `Program.cs`
- Generated and applied EF migration `BookActionModel` — `BookActions` table + indexes created in PostgreSQL
- Added 3 new unit tests to `ShelfServiceTests.cs` — all 37 tests pass (0 regressions)

### File List
**New files:**
- `backend/BookTracker.Api/Models/Enums/ActionType.cs`
- `backend/BookTracker.Api/Models/BookAction.cs`
- `backend/BookTracker.Api/Repositories/Interfaces/IBookActionRepository.cs`
- `backend/BookTracker.Api/Repositories/BookActionRepository.cs`
- `backend/BookTracker.Api/DTOs/Shelf/UpdateStatusDto.cs`
- `backend/BookTracker.Api/Migrations/20260526143304_BookActionModel.cs` (auto-generated)
- `backend/BookTracker.Api/Migrations/20260526143304_BookActionModel.Designer.cs` (auto-generated)

**Modified files:**
- `backend/BookTracker.Api/Data/AppDbContext.cs`
- `backend/BookTracker.Api/Repositories/Interfaces/IUserBookRepository.cs`
- `backend/BookTracker.Api/Repositories/UserBookRepository.cs`
- `backend/BookTracker.Api/Services/Interfaces/IShelfService.cs`
- `backend/BookTracker.Api/Services/ShelfService.cs`
- `backend/BookTracker.Api/Controllers/ShelfController.cs`
- `backend/BookTracker.Api/Program.cs`
- `backend/BookTracker.Api/Migrations/AppDbContextModelSnapshot.cs` (auto-updated)
- `backend/BookTracker.Tests/Services/ShelfServiceTests.cs`

### Change Log
- Added BookAction domain model, ActionType enum, IBookActionRepository, BookActionRepository (2026-05-26)
- Added UpdateWithActionAsync to IUserBookRepository/UserBookRepository for AR-9 atomicity (2026-05-26)
- Implemented UpdateStatusAsync in ShelfService with state machine (Resting→Started, Started→Abandoned) (2026-05-26)
- Added PATCH /api/shelf/{userBookId}/status endpoint (2026-05-26)
- Applied EF migration BookActionModel — BookActions table + 2 composite indexes in PostgreSQL (2026-05-26)
- Added 3 unit tests for UpdateStatusAsync (valid transition, invalid transition, ownership mismatch) (2026-05-26)
