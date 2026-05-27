# Story 3.2: Page Progress Update & Auto-Finish

Status: review

## Story

As a **reader with a Started book**,
I want to update my current page count and have the book auto-finish when I reach the last page,
So that my progress is recorded accurately and finishing feels automatic.

## Acceptance Criteria

1. `PATCH /api/shelf/{userBookId}/pages` accepts `{ "pages": N }` and returns the updated `UserBookResponse` (200)
2. Request is only valid when `UserBook.Status == Started`; any other status returns 400 `{ "error": "Page progress only allowed on Started books.", "code": "INVALID_STATE" }`
3. `pages` must be in range `[0, totalPages]` (inclusive); values outside this range return 400 `{ "error": "Page value exceeds total pages.", "code": "INVALID_PAGE" }`
4. Ownership check: `UserBook.UserId != userId` returns 403 `{ "code": "FORBIDDEN" }`
5. **Normal update** (`pages < totalPages`): In a **single `SaveChangesAsync()`** — `UserBook.CurrentPages = pages`, `LastActivityAt = DateTime.UtcNow`, one `PageUpdate` BookAction inserted (`OldValue = prior CurrentPages as string`, `NewValue = new pages as string`, `Timestamp = UtcNow`)
6. **Auto-finish** (`pages == totalPages`): In a **single `SaveChangesAsync()`** — `UserBook.CurrentPages = pages`, `UserBook.Status = Finished`, `FinishedAt = DateTime.UtcNow`, `LastActivityAt = DateTime.UtcNow`, one `PageUpdate` BookAction AND one `StatusChange` BookAction (`OldValue="Started"`, `NewValue="Finished"`) — two BookActions, one save (AR-9)
7. Auto-finish response includes `status = "Finished"` so the frontend knows to trigger the celebration overlay
8. `IUserBookRepository` gains `UpdateWithActionsAsync(UserBook ub, IReadOnlyList<BookAction> actions)` for multi-BookAction atomic saves; the single-action `UpdateWithActionAsync` from Story 3.1 is reused for normal updates
9. No new EF migration needed — `BookActions` table exists from Story 3.1
10. Unit tests in `ShelfServiceTests.cs` cover: normal page update, auto-finish produces two BookActions, out-of-range rejected, non-Started status rejected

## Tasks / Subtasks

- [x] Task 1: Add `UpdateWithActionsAsync` to `IUserBookRepository` and implement (AC: 6, 8)
  - [x] Add to `backend/BookTracker.Api/Repositories/Interfaces/IUserBookRepository.cs`:
    ```csharp
    Task<UserBook> UpdateWithActionsAsync(UserBook ub, IReadOnlyList<BookAction> actions);
    ```
  - [x] Implement in `backend/BookTracker.Api/Repositories/UserBookRepository.cs`:
    ```csharp
    public async Task<UserBook> UpdateWithActionsAsync(UserBook ub, IReadOnlyList<BookAction> actions)
    {
        _db.UserBooks.Update(ub);
        _db.BookActions.AddRange(actions);
        await _db.SaveChangesAsync();
        return ub;
    }
    ```
  - [x] Note: Uses `AddRange` so any number of BookActions are staged and saved atomically with the UserBook update in a single `SaveChangesAsync()`.

- [x] Task 2: Create `UpdatePagesDto` (AC: 1)
  - [x] Create `backend/BookTracker.Api/DTOs/Shelf/UpdatePagesDto.cs`:

- [x] Task 3: Add `UpdatePagesAsync` to `IShelfService` and implement in `ShelfService` (AC: 2, 3, 4, 5, 6, 7)
  - [x] Add to `backend/BookTracker.Api/Services/Interfaces/IShelfService.cs`
  - [x] Implement `UpdatePagesAsync` in `backend/BookTracker.Api/Services/ShelfService.cs`

- [x] Task 4: Add `PATCH /api/shelf/{userBookId}/pages` endpoint to `ShelfController` (AC: 1)
  - [x] Add to `backend/BookTracker.Api/Controllers/ShelfController.cs`

- [x] Task 5: Add unit tests for `UpdatePagesAsync` (AC: 10)
  - [x] Added 4 tests to `backend/BookTracker.Tests/Services/ShelfServiceTests.cs`
  - [x] Test: `UpdatePagesAsync_NormalUpdate_StoresPageUpdateAction` ✅
  - [x] Test: `UpdatePagesAsync_AutoFinish_StoresTwoBookActionsAndFinishedStatus` ✅
  - [x] Test: `UpdatePagesAsync_PagesExceedTotal_Throws400InvalidPage` ✅
  - [x] Test: `UpdatePagesAsync_StatusNotStarted_Throws400InvalidState` ✅

- [x] Task 6: Build and test verification (AC: all)
  - [x] Run `dotnet build` from `backend/BookTracker.Api/` — zero errors ✅
  - [x] Run `dotnet test` from `backend/BookTracker.Tests/` — 41 tests pass (0 failures) ✅

## Dev Notes

### AR-9: Two-BookAction Atomicity for Auto-Finish

Auto-finish MUST persist both BookActions in a single `SaveChangesAsync()` call. This is why `UpdateWithActionsAsync` uses `AddRange`:

```csharp
_db.BookActions.AddRange(actions);  // stages both PageUpdate + StatusChange
await _db.SaveChangesAsync();        // ONE commit
```

For normal page updates (single BookAction), reuse the existing `UpdateWithActionAsync` — do NOT add an extra list wrapper around it.

### Page Range Logic

- Valid: `pages >= 0 AND pages <= totalPages`
- `pages == totalPages` → **auto-finish** (Status→Finished, two BookActions)
- `pages < totalPages` → **normal update** (one PageUpdate BookAction)
- `pages > totalPages` → **400 INVALID_PAGE** (check happens before auto-finish logic)

Note the epic says range `[0, totalPages)` in one place but `[0, totalPages]` in another — the auto-finish AC (`pages == totalPages` triggers finish) makes it clear the correct range is **[0, totalPages] inclusive**. Use `pages < 0 || pages > ub.Book.TotalPages` as the rejection guard.

### `GetByIdAsync` Already Loads Book

`UserBookRepository.GetByIdAsync` uses `.Include(ub => ub.Book)` — so `ub.Book.TotalPages` is available immediately after the fetch. **Do NOT make a separate book lookup.**

### State Machine After Story 3.2

Valid status values and how they're reached:
| Status | How set |
|--------|---------|
| `Resting` | AddToShelf (initial), Re-read (Story 3.3) |
| `Started` | `PATCH /api/shelf/{id}/status` with `"Started"` |
| `Abandoned` | `PATCH /api/shelf/{id}/status` with `"Abandoned"` |
| `Finished` | Auto-set by `UpdatePagesAsync` when pages == totalPages |

`Finished` is **never** reachable via the `/status` endpoint (Story 3.1's `validTransitions` dict intentionally excludes it).

### Existing Files to Modify

| File | Change |
|------|--------|
| `Repositories/Interfaces/IUserBookRepository.cs` | Add `UpdateWithActionsAsync` method |
| `Repositories/UserBookRepository.cs` | Implement `UpdateWithActionsAsync` |
| `Services/Interfaces/IShelfService.cs` | Add `UpdatePagesAsync` method |
| `Services/ShelfService.cs` | Implement `UpdatePagesAsync` |
| `Controllers/ShelfController.cs` | Add PATCH `{userBookId}/pages` endpoint |
| `Tests/Services/ShelfServiceTests.cs` | Add 4 new tests |

### New Files to Create

| File | Purpose |
|------|---------|
| `DTOs/Shelf/UpdatePagesDto.cs` | Request DTO with `int Pages` |

### Test Patterns for Task 5

All tests use the existing `_userBookRepoMock` and `_bookRepoMock` fields, and `CreateSut()` helper. No changes to `CreateSut()` — ShelfService constructor is unchanged.

**Test 1 — Normal Update:**
```csharp
[Fact]
public async Task UpdatePagesAsync_NormalUpdate_StoresPageUpdateAction()
{
    var book = MakeBook(10);  // TotalPages = 300
    var ub = MakeUserBook(1, 7, book);
    ub.Status = ReadingStatus.Started;
    ub.CurrentPages = 50;

    _userBookRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(ub);

    UserBook? capturedUb = null;
    BookAction? capturedAction = null;
    _userBookRepoMock
        .Setup(r => r.UpdateWithActionAsync(It.IsAny<UserBook>(), It.IsAny<BookAction>()))
        .Callback<UserBook, BookAction>((u, a) => { capturedUb = u; capturedAction = a; })
        .ReturnsAsync((UserBook u, BookAction _) => u);
    _userBookRepoMock.Setup(r => r.GetReaderCountsAsync(It.IsAny<IEnumerable<int>>()))
        .ReturnsAsync(new Dictionary<int, int> { [10] = 1 });

    var sut = CreateSut();
    var result = await sut.UpdatePagesAsync(7, 1, 100);

    Assert.Equal(100, capturedUb!.CurrentPages);
    Assert.Equal(ReadingStatus.Started, capturedUb.Status);
    Assert.Null(capturedUb.FinishedAt);
    Assert.Equal(ActionType.PageUpdate, capturedAction!.ActionType);
    Assert.Equal("50", capturedAction.OldValue);
    Assert.Equal("100", capturedAction.NewValue);
    Assert.Equal("Started", result.Status);
    _userBookRepoMock.Verify(r => r.UpdateWithActionAsync(It.IsAny<UserBook>(), It.IsAny<BookAction>()), Times.Once);
    _userBookRepoMock.Verify(r => r.UpdateWithActionsAsync(It.IsAny<UserBook>(), It.IsAny<IReadOnlyList<BookAction>>()), Times.Never);
}
```

**Test 2 — Auto-Finish (two BookActions, one save):**
```csharp
[Fact]
public async Task UpdatePagesAsync_AutoFinish_StoresTwoBookActionsAndFinishedStatus()
{
    var book = MakeBook(10);  // TotalPages = 300
    var ub = MakeUserBook(1, 7, book);
    ub.Status = ReadingStatus.Started;
    ub.CurrentPages = 280;

    _userBookRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(ub);

    IReadOnlyList<BookAction>? capturedActions = null;
    UserBook? capturedUb = null;
    _userBookRepoMock
        .Setup(r => r.UpdateWithActionsAsync(It.IsAny<UserBook>(), It.IsAny<IReadOnlyList<BookAction>>()))
        .Callback<UserBook, IReadOnlyList<BookAction>>((u, a) => { capturedUb = u; capturedActions = a; })
        .ReturnsAsync((UserBook u, IReadOnlyList<BookAction> _) => u);
    _userBookRepoMock.Setup(r => r.GetReaderCountsAsync(It.IsAny<IEnumerable<int>>()))
        .ReturnsAsync(new Dictionary<int, int> { [10] = 1 });

    var sut = CreateSut();
    var result = await sut.UpdatePagesAsync(7, 1, 300);  // 300 == TotalPages

    Assert.Equal(ReadingStatus.Finished, capturedUb!.Status);
    Assert.NotNull(capturedUb.FinishedAt);
    Assert.Equal(300, capturedUb.CurrentPages);

    Assert.NotNull(capturedActions);
    Assert.Equal(2, capturedActions.Count);
    Assert.Contains(capturedActions, a => a.ActionType == ActionType.PageUpdate && a.NewValue == "300");
    Assert.Contains(capturedActions, a => a.ActionType == ActionType.StatusChange && a.OldValue == "Started" && a.NewValue == "Finished");

    Assert.Equal("Finished", result.Status);
    _userBookRepoMock.Verify(r => r.UpdateWithActionsAsync(It.IsAny<UserBook>(), It.IsAny<IReadOnlyList<BookAction>>()), Times.Once);
    _userBookRepoMock.Verify(r => r.UpdateWithActionAsync(It.IsAny<UserBook>(), It.IsAny<BookAction>()), Times.Never);
}
```

**Test 3 — Pages out of range:**
```csharp
[Fact]
public async Task UpdatePagesAsync_PagesExceedTotal_Throws400InvalidPage()
{
    var book = MakeBook(10);  // TotalPages = 300
    var ub = MakeUserBook(1, 7, book);
    ub.Status = ReadingStatus.Started;

    _userBookRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(ub);

    var sut = CreateSut();
    var ex = await Assert.ThrowsAsync<ApiException>(() => sut.UpdatePagesAsync(7, 1, 999));

    Assert.Equal(400, ex.StatusCode);
    Assert.Equal("INVALID_PAGE", ex.ErrorCode);
}
```

**Test 4 — Wrong status:**
```csharp
[Fact]
public async Task UpdatePagesAsync_StatusNotStarted_Throws400InvalidState()
{
    var book = MakeBook(10);
    var ub = MakeUserBook(1, 7, book);
    ub.Status = ReadingStatus.Resting;  // not Started

    _userBookRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(ub);

    var sut = CreateSut();
    var ex = await Assert.ThrowsAsync<ApiException>(() => sut.UpdatePagesAsync(7, 1, 100));

    Assert.Equal(400, ex.StatusCode);
    Assert.Equal("INVALID_STATE", ex.ErrorCode);
}
```

### Error Codes Reference

| Condition | HTTP | Code |
|-----------|------|------|
| UserBook not found | 404 | `NOT_FOUND` |
| Wrong user | 403 | `FORBIDDEN` |
| Status is not Started | 400 | `INVALID_STATE` |
| pages < 0 or pages > totalPages | 400 | `INVALID_PAGE` |

### From Previous Story (3.1)

- `UpdateWithActionAsync(UserBook, BookAction)` — singular, already on `IUserBookRepository` — **do not replace it**, just add the plural variant
- `ShelfService` constructor: `(IUserBookRepository userBookRepository, IBookRepository bookRepository)` — **unchanged**
- Error pattern: `throw new ApiException(statusCode, message, code)` — auto-handled by `ExceptionHandlingMiddleware`
- `GetByIdAsync` includes `.Include(ub => ub.Book)` — `ub.Book.TotalPages` is always accessible

---

## Dev Agent Record

### Completion Notes
Story 3.2 implemented fully in a single session. All 6 tasks completed.

- Added `UpdateWithActionsAsync(UserBook, IReadOnlyList<BookAction>)` to `IUserBookRepository` / `UserBookRepository` using `AddRange` for multi-BookAction atomic saves (AR-9)
- Created `UpdatePagesDto { int Pages }` with `[Required]`
- Implemented `UpdatePagesAsync` in `ShelfService` with full validation: ownership (403), status guard (400 INVALID_STATE), range guard (400 INVALID_PAGE), normal update path (single PageUpdate BookAction), and auto-finish path (PageUpdate + StatusChange in a single save)
- Added `PATCH /api/shelf/{userBookId}/pages` endpoint to `ShelfController`
- No EF migration needed — `BookActions` table already exists from Story 3.1
- Added 4 unit tests; all 41 tests pass (0 regressions)

### File List
**New files:**
- `backend/BookTracker.Api/DTOs/Shelf/UpdatePagesDto.cs`

**Modified files:**
- `backend/BookTracker.Api/Repositories/Interfaces/IUserBookRepository.cs`
- `backend/BookTracker.Api/Repositories/UserBookRepository.cs`
- `backend/BookTracker.Api/Services/Interfaces/IShelfService.cs`
- `backend/BookTracker.Api/Services/ShelfService.cs`
- `backend/BookTracker.Api/Controllers/ShelfController.cs`
- `backend/BookTracker.Tests/Services/ShelfServiceTests.cs`

### Change Log
- Added `UpdateWithActionsAsync` to `IUserBookRepository` / `UserBookRepository` for multi-BookAction atomic saves (2026-05-27)
- Implemented `UpdatePagesAsync` in `ShelfService` — normal update + auto-finish state machine (2026-05-27)
- Added `PATCH /api/shelf/{userBookId}/pages` endpoint (2026-05-27)
- Added 4 unit tests for `UpdatePagesAsync` (2026-05-27)
