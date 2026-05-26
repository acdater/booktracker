# Story 2.3: Book Catalog Creation & Deduplication

Status: review

<!-- Note: Validation is optional. Run validate-create-story for quality check before dev-story. -->

## Story

As an **authenticated user**,
I want to submit book metadata and have it saved to the shared catalog,
so that the book is available for me and other users to add to their shelves.

## Acceptance Criteria

1. `POST /api/books` with `{ isbn, title, author, totalPages, genre, coverImageUrl? }` from an authenticated user creates a `Book` record and returns **HTTP 201** with the full `BookResponse`
2. When the same ISBN already exists (duplicate or concurrent race) — `BookService` catches `DbUpdateException` (unique constraint violation on `UQ_Books_ISBN`), re-fetches the existing `Book` via `GetByISBNAsync`, and returns **HTTP 200** with the existing `BookResponse` — no error surfaced to the caller
3. When any required field (`isbn`, `title`, `author`, `totalPages`, `genre`) is missing or `totalPages` is not a positive integer (≥ 1) → returns **HTTP 400** with `{ "error": "...", "code": "VALIDATION_ERROR" }` — handled by existing `ApiBehaviorOptions` in `Program.cs` via Data Annotations on the DTO
4. When `genre` value is not in the predefined list → returns **HTTP 400** with `{ "error": "Genre must be one of: ...", "code": "VALIDATION_ERROR" }` — validated in `BookService` against the `AllowedGenres` constant list, **not** via DB CHECK constraint; throws `ApiException(400, ..., "VALIDATION_ERROR")`
5. `POST /api/books` requires JWT authentication (`[Authorize]` — already on `BooksController` class level)
6. `CreateBookDto` at `DTOs/Books/CreateBookDto.cs` uses Data Annotations for required fields and numeric range; namespace `BookTracker.Api.DTOs.Books`
7. `IBookService` gains method `CreateBookAsync(CreateBookDto dto): Task<(BookResponse Response, bool IsNew)>` — `IsNew=true` means new record (HTTP 201), `IsNew=false` means duplicate found (HTTP 200)
8. All existing backend tests (14/14) still pass

## Tasks / Subtasks

- [x] Task 1: Create `CreateBookDto` (AC: 3, 6)
  - [x] Create `DTOs/Books/CreateBookDto.cs`
  - [x] `[Required]` on: `ISBN`, `Title`, `Author`, `Genre`
  - [x] `TotalPages`: `[Required]`, `[Range(1, int.MaxValue, ErrorMessage = "TotalPages must be a positive integer.")]`
  - [x] `CoverImageUrl` is `string?` (nullable, no `[Required]`)
  - [x] Namespace: `BookTracker.Api.DTOs.Books`

- [x] Task 2: Add `CreateBookAsync` to `IBookService` and `BookService` (AC: 1, 2, 4, 7)
  - [x] Add `Task<(BookResponse Response, bool IsNew)> CreateBookAsync(CreateBookDto dto)` to `IBookService`
  - [x] Implement `CreateBookAsync` in `BookService`:
    1. Validate `dto.Genre` against `AllowedGenres` — throw `ApiException(400, "Genre must be one of: ...", "VALIDATION_ERROR")` if invalid
    2. Build a `Book` entity from `dto` (ISBN must NOT be normalised here — caller-provided value, stored as-is)
    3. Call `_bookRepository.CreateAsync(book)` inside a `try` block
    4. On `DbUpdateException` (any): call `GetByISBNAsync(dto.ISBN)` — guaranteed non-null — return `(MapToResponse(existing!), false)`
    5. On success: return `(MapToResponse(book), true)`
  - [x] Add private `AllowedGenres` constant (`static readonly HashSet<string>`) in `BookService`:
    ```
    "Fiction", "Non-Fiction", "Mystery", "Science Fiction", "Fantasy",
    "Romance", "Biography & Memoir", "History", "Self-Help", "Other"
    ```

- [x] Task 3: Update `BooksController` to add `POST /api/books` (AC: 1, 2, 5)
  - [x] Add `[HttpPost]` action `CreateBook([FromBody] CreateBookDto dto)` to `BooksController`
  - [x] Call `bookService.CreateBookAsync(dto)`
  - [x] `result.IsNew == true` → `return StatusCode(201, result.Response)`
  - [x] `result.IsNew == false` → `return Ok(result.Response)` (HTTP 200)
  - [x] No try/catch — exceptions bubble to `ExceptionHandlingMiddleware`

- [x] Task 4: Write unit tests for `BookService.CreateBookAsync` (AC: 1, 2, 3, 4)
  - [x] Test: valid input → creates book, returns `(Response, IsNew=true)`
  - [x] Test: duplicate ISBN — `CreateAsync` throws `DbUpdateException` → re-fetches, returns `(existingResponse, IsNew=false)`
  - [x] Test: invalid genre → throws `ApiException` with status 400 and code `"VALIDATION_ERROR"`
  - [x] Test: every allowed genre string passes validation
  - [x] Mock `IBookRepository` — `CreateAsync` and `GetByISBNAsync`

- [x] Task 5: Verify all tests pass (AC: 8)
  - [x] Run `dotnet test backend/BookTracker.Tests`
  - [x] All new + existing tests pass (minimum 14+4 = 18)

## Dev Notes

### ⚠️ CRITICAL: No New EF Migration Required

Story 2.3 introduces **no new database tables or columns**. The `Books` table and `UQ_Books_ISBN` constraint already exist from the Story 2.1 migration. Do **NOT** run `dotnet ef migrations add`.

### ⚠️ CRITICAL: Genre Validated in Service, NOT DB

From the epics (FR-7, Story 2.3 AC): genre validation is done programmatically in `BookService` against the `AllowedGenres` set. There is no DB CHECK constraint. This is intentional — allows adding genres later without a migration.

### ⚠️ CRITICAL: `DbUpdateException` Catch Pattern

The deduplication logic (AR from FR-8) catches any `DbUpdateException` — NOT filtered by constraint name. The unique constraint `UQ_Books_ISBN` is the only one on `Books`, so this is safe. After catching, **always** call `GetByISBNAsync(dto.ISBN)` — this is guaranteed to return a non-null `Book` because the constraint violation proves one exists.

```csharp
catch (DbUpdateException)
{
    var existing = await _bookRepository.GetByISBNAsync(dto.ISBN);
    return (MapToResponse(existing!), false);
}
```

### Genre Allowed Values (exact strings — case-sensitive)

```csharp
private static readonly HashSet<string> AllowedGenres = new()
{
    "Fiction", "Non-Fiction", "Mystery", "Science Fiction", "Fantasy",
    "Romance", "Biography & Memoir", "History", "Self-Help", "Other"
};
```

Error message when invalid: `$"Genre must be one of: {string.Join(", ", AllowedGenres)}."`

### `CreateBookDto` Exact Shape

```csharp
using System.ComponentModel.DataAnnotations;

namespace BookTracker.Api.DTOs.Books;

public class CreateBookDto
{
    [Required]
    public string ISBN { get; set; } = string.Empty;

    [Required]
    public string Title { get; set; } = string.Empty;

    [Required]
    public string Author { get; set; } = string.Empty;

    [Required]
    [Range(1, int.MaxValue, ErrorMessage = "TotalPages must be a positive integer.")]
    public int TotalPages { get; set; }

    [Required]
    public string Genre { get; set; } = string.Empty;

    public string? CoverImageUrl { get; set; }
}
```

### `CreateBookAsync` Full Implementation Pattern

```csharp
public async Task<(BookResponse Response, bool IsNew)> CreateBookAsync(CreateBookDto dto)
{
    if (!AllowedGenres.Contains(dto.Genre))
        throw new ApiException(400,
            $"Genre must be one of: {string.Join(", ", AllowedGenres)}.",
            "VALIDATION_ERROR");

    var book = new Book
    {
        ISBN = dto.ISBN,
        Title = dto.Title,
        Author = dto.Author,
        TotalPages = dto.TotalPages,
        Genre = dto.Genre,
        CoverImageUrl = dto.CoverImageUrl
    };

    try
    {
        book = await _bookRepository.CreateAsync(book);
        return (MapToResponse(book), true);
    }
    catch (DbUpdateException)
    {
        var existing = await _bookRepository.GetByISBNAsync(dto.ISBN);
        return (MapToResponse(existing!), false);
    }
}
```

Note: `MapToResponse` is already private in `BookService` from Story 2.2.

### `BooksController` POST Action Pattern

```csharp
[HttpPost]
public async Task<IActionResult> CreateBook([FromBody] CreateBookDto dto)
{
    var result = await bookService.CreateBookAsync(dto);
    return result.IsNew ? StatusCode(201, result.Response) : Ok(result.Response);
}
```

The `[Authorize]` attribute is already applied at the class level on `BooksController` — no need to add it to this action.

### Controller Pattern (from `AuthController.cs`)

Primary constructor injection: `public class BooksController(IBookService bookService) : ControllerBase` — already in place. Just add the new `[HttpPost]` method.

### `MapToResponse` Already Exists in `BookService`

From Story 2.2, `MapToResponse(Book book)` is already a private static method in `BookService`. Use it directly — do NOT redefine it.

### `DbUpdateException` using Directive

Add `using Microsoft.EntityFrameworkCore;` to `BookService.cs` to access `DbUpdateException`.

### Architecture: Service Layer Error Code Pattern

From `ApiException.cs` and `AuthService.cs` — all service-level validation throws:
```csharp
throw new ApiException(statusCode, errorMessage, errorCode);
```
`ExceptionHandlingMiddleware` converts this to `{ "error": "...", "code": "..." }` automatically. No try/catch needed in the controller.

### DTO Validation

`ApiBehaviorOptions` in `Program.cs` already converts `ModelState` failures to `{ "error": "...", "code": "VALIDATION_ERROR" }`. Data Annotations on `CreateBookDto` are sufficient for required-field and range validation.

### Files from Story 2.2 (DO NOT MODIFY except where instructed)

- `DTOs/Books/BookResponse.cs` — already exists, no changes needed
- `Services/Interfaces/IBookService.cs` — **ADD** `CreateBookAsync` signature only
- `Services/BookService.cs` — **ADD** `AllowedGenres` constant + `CreateBookAsync` method + `using Microsoft.EntityFrameworkCore;`
- `Controllers/BooksController.cs` — **ADD** `[HttpPost]` action only
- `Program.cs` — no changes needed (DI already registered in 2.2)

### Unit Testing Pattern (from `BookServiceTests.cs`)

Use `Moq` — already in `BookTracker.Tests.csproj`. Follow the existing `BookServiceTests` helper pattern:

```csharp
var repoMock = new Mock<IBookRepository>();
var factoryMock = new Mock<IHttpClientFactory>();
var sut = new BookService(repoMock.Object, factoryMock.Object);
```

For `DbUpdateException`, construct with default ctor: `new DbUpdateException()`.

### No Postman Collection Update Required

Story 2.3 is a backend-only story. The Postman collection update (adding `POST /api/books`) can be done after this story.

## Dev Agent Record

### Agent Model Used

claude-sonnet-4.6

### Debug Log References

### Completion Notes List

- All 5 tasks complete. 13 new tests (BookCatalogServiceTests) added; total 27/27 pass.
- CreateBookDto with Data Annotations; IBookService/BookService.CreateBookAsync with genre validation + DbUpdateException deduplication pattern.
- BooksController gains POST /api/books returning 201 (new) or 200 (duplicate).
- AllowedGenres HashSet covers all 10 genres; Theory test validates each one.

### File List

- `backend/BookTracker.Api/DTOs/Books/CreateBookDto.cs` (new)
- `backend/BookTracker.Api/Services/Interfaces/IBookService.cs` (modified — added CreateBookAsync)
- `backend/BookTracker.Api/Services/BookService.cs` (modified — AllowedGenres + CreateBookAsync + using Microsoft.EntityFrameworkCore)
- `backend/BookTracker.Api/Controllers/BooksController.cs` (modified — added POST /api/books action)
- `backend/BookTracker.Tests/Services/BookCatalogServiceTests.cs` (new — 13 tests)
