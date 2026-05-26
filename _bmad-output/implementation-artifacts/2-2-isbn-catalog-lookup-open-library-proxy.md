# Story 2.2: ISBN Catalog Lookup & Open Library Proxy

Status: review

<!-- Note: Validation is optional. Run validate-create-story for quality check before dev-story. -->

## Story

As a **developer**,
I want `GET /api/books/{isbn}` to check the shared catalog and fall back to Open Library,
so that book metadata can be prefilled when a reader adds a new book.

## Acceptance Criteria

1. `GET /api/books/{isbn}` called with an ISBN that exists in the Catalog returns HTTP 200 with `BookResponse` immediately — no Open Library call is made
2. `GET /api/books/{isbn}` called with an ISBN not in the Catalog and Open Library returns a match → `BookService` calls Open Library via `IHttpClientFactory` named client with 3-second timeout, maps `title`, `author`, `totalPages`, `coverImageUrl` to `BookResponse`, and returns HTTP 200; `genre` is `null` — never prefilled from Open Library
3. When Open Library is unreachable, times out (> 3 seconds), or returns no match → returns HTTP 200 with `null` body (no error — frontend shows empty manual entry form)
4. ISBN lookup strips leading/trailing whitespace from the input; `X` and `x` in ISBN-10 check digit positions are treated identically (case-insensitive)
5. `IBookService` / `BookService` exist with method `LookupISBNAsync(string isbn): Task<BookResponse?>`; `BooksController` at `Controllers/BooksController.cs` delegates to service; no business logic in controller
6. `IHttpClientFactory` named client `"OpenLibrary"` registered in `Program.cs` with `BaseAddress = https://openlibrary.org` and `Timeout = TimeSpan.FromSeconds(3)`
7. `BookResponse` DTO at `DTOs/Books/BookResponse.cs` exposes: `id` (int), `isbn` (string), `title` (string), `author` (string), `totalPages` (int), `genre` (string?), `coverImageUrl` (string?)
8. `GET /api/books/{isbn}` endpoint requires JWT authentication (`[Authorize]`)
9. All existing backend tests (7/7) still pass

## Tasks / Subtasks

- [x] Task 1: Create `BookResponse` DTO (AC: 7)
  - [x] Create `DTOs/Books/BookResponse.cs` with properties matching the spec
  - [x] `Genre` and `CoverImageUrl` are `string?` (nullable)
  - [x] Namespace: `BookTracker.Api.DTOs.Books`

- [x] Task 2: Create `IBookService` and `BookService` (AC: 2, 3, 4, 5)
  - [x] Create `Services/Interfaces/IBookService.cs` declaring `Task<BookResponse?> LookupISBNAsync(string isbn)`
  - [x] Create `Services/BookService.cs` implementing `IBookService`
  - [x] Constructor injects `IBookRepository` and `IHttpClientFactory`
  - [x] `LookupISBNAsync` logic:
    1. Normalise ISBN: `isbn = isbn.Trim().ToUpperInvariant()` (handles whitespace + `x` → `X`)
    2. Check catalog via `IBookRepository.GetByISBNAsync(normalised)` → if found, map to `BookResponse` and return
    3. Otherwise, call Open Library via named `HttpClient`; catch ALL exceptions (timeout, network, parse) and return `null`
    4. If Open Library response is empty object or missing the key → return `null`
    5. Map Open Library response → `BookResponse`; `Genre = null` always

- [x] Task 3: Create `BooksController` (AC: 1, 5, 8)
  - [x] Create `Controllers/BooksController.cs`
  - [x] `[ApiController]`, `[Route("api/books")]`, `[Authorize]`
  - [x] `GET /{isbn}` action delegates to `IBookService.LookupISBNAsync(isbn)`
  - [x] Returns `Ok(result)` whether result is a `BookResponse` or `null`
  - [x] No try/catch — exceptions bubble to `ExceptionHandlingMiddleware`

- [x] Task 4: Register dependencies in `Program.cs` (AC: 6)
  - [x] Add `builder.Services.AddHttpClient("OpenLibrary", client => { client.BaseAddress = new Uri("https://openlibrary.org"); client.Timeout = TimeSpan.FromSeconds(3); });`
  - [x] Add `builder.Services.AddScoped<IBookService, BookService>();`
  - [x] Remove `// TODO Story 2.2` comment from `Program.cs`

- [x] Task 5: Write unit tests for `BookService` (AC: 1, 2, 3, 4)
  - [x] Test: catalog hit → returns `BookResponse` immediately, no HTTP call made
  - [x] Test: catalog miss + Open Library match → returns mapped `BookResponse` with `Genre = null`
  - [x] Test: catalog miss + Open Library empty response (`{}`) → returns `null`
  - [x] Test: catalog miss + Open Library throws `HttpRequestException` → returns `null`
  - [x] Test: catalog miss + Open Library throws `TaskCanceledException` (timeout) → returns `null`
  - [x] Test: ISBN with leading/trailing whitespace is trimmed before lookup
  - [x] Test: ISBN with lowercase `x` check digit is uppercased before lookup
  - [x] Use `Moq` for mocking `IBookRepository` and `IHttpClientFactory`

- [x] Task 6: Verify all tests pass (AC: 9)
  - [x] Run `dotnet test backend/BookTracker.Tests`
  - [x] All new + existing tests pass

## Dev Notes

### Open Library API — Exact Endpoint & Response Shape

**Endpoint:**
```
GET https://openlibrary.org/api/books?bibkeys=ISBN:{isbn}&format=json&jscmd=data
```

**Success response** (ISBN found):
```json
{
  "ISBN:9780140328721": {
    "title": "Fantastic Mr. Fox",
    "authors": [{ "name": "Roald Dahl" }],
    "number_of_pages": 96,
    "cover": {
      "small": "https://covers.openlibrary.org/b/id/8739161-S.jpg",
      "medium": "https://covers.openlibrary.org/b/id/8739161-M.jpg",
      "large": "https://covers.openlibrary.org/b/id/8739161-L.jpg"
    }
  }
}
```

**Not found response** (empty object):
```json
{}
```

**Field mapping to `BookResponse`:**
| Open Library field | `BookResponse` field | Notes |
|---|---|---|
| `title` | `Title` | Direct string |
| `authors[0].name` | `Author` | First author only; `""` if array is empty |
| `number_of_pages` | `TotalPages` | `0` if missing |
| `cover.medium` | `CoverImageUrl` | `null` if `cover` is absent or `medium` missing |
| (none) | `Genre` | Always `null` — never prefilled from Open Library |

**Key to read from response:** `$"ISBN:{isbn}"` where `isbn` is the normalized (trimmed, uppercased) value.

### BookService Implementation Pattern

```csharp
public async Task<BookResponse?> LookupISBNAsync(string isbn)
{
    isbn = isbn.Trim().ToUpperInvariant();

    // 1. Catalog hit
    var existing = await _bookRepository.GetByISBNAsync(isbn);
    if (existing is not null)
        return MapToResponse(existing);

    // 2. Open Library fallback
    try
    {
        var client = _httpClientFactory.CreateClient("OpenLibrary");
        var url = $"/api/books?bibkeys=ISBN:{isbn}&format=json&jscmd=data";
        var response = await client.GetFromJsonAsync<Dictionary<string, JsonElement>>(url);

        var key = $"ISBN:{isbn}";
        if (response is null || !response.TryGetValue(key, out var bookData))
            return null;

        return MapOpenLibraryResponse(isbn, bookData);
    }
    catch
    {
        return null;  // timeout, network error, parse error — all return null
    }
}
```

**`MapOpenLibraryResponse` helper:**
```csharp
private static BookResponse MapOpenLibraryResponse(string isbn, JsonElement data)
{
    var title = data.TryGetProperty("title", out var t) ? t.GetString() ?? "" : "";

    var author = "";
    if (data.TryGetProperty("authors", out var authors) && authors.GetArrayLength() > 0)
        author = authors[0].TryGetProperty("name", out var n) ? n.GetString() ?? "" : "";

    var totalPages = 0;
    if (data.TryGetProperty("number_of_pages", out var pages))
        totalPages = pages.GetInt32();

    string? coverImageUrl = null;
    if (data.TryGetProperty("cover", out var cover) &&
        cover.TryGetProperty("medium", out var medium))
        coverImageUrl = medium.GetString();

    return new BookResponse
    {
        Id = 0,       // not in catalog yet — 0 signals "not persisted"
        ISBN = isbn,
        Title = title,
        Author = author,
        TotalPages = totalPages,
        Genre = null,
        CoverImageUrl = coverImageUrl
    };
}
```

**`MapToResponse` from catalog `Book`:**
```csharp
private static BookResponse MapToResponse(Book book) => new()
{
    Id = book.Id,
    ISBN = book.ISBN,
    Title = book.Title,
    Author = book.Author,
    TotalPages = book.TotalPages,
    Genre = book.Genre,
    CoverImageUrl = book.CoverImageUrl
};
```

### ⚠️ CRITICAL: `GetFromJsonAsync` Needs `System.Net.Http.Json`

`GetFromJsonAsync<T>` is an extension method in `System.Net.Http.Json`. It is included in .NET 10 BCL — no extra NuGet package needed. Add `using System.Net.Http.Json;` to `BookService.cs`.

### ⚠️ CRITICAL: `catch` must be bare `catch` (or `catch (Exception)`)

The architecture requires returning `null` for **all** Open Library failures — network errors, timeouts, JSON parse errors. Use a bare `catch` or `catch (Exception)` to capture everything. Do NOT re-throw; do NOT filter by exception type.

### ⚠️ CRITICAL: `[Authorize]` on BooksController

`GET /api/books/{isbn}` requires a valid JWT (FR-3 — all endpoints except `/api/auth/register` and `/api/auth/login` require auth). Apply `[Authorize]` at the controller class level.

### ⚠️ CRITICAL: IHttpClientFactory named client timeout

Setting `client.Timeout` on the `HttpClient` via `AddHttpClient` factory sets the default per-request timeout. With `TimeSpan.FromSeconds(3)`, any Open Library request exceeding 3 seconds throws `TaskCanceledException` — which is caught by the bare `catch` and returns `null`. This satisfies AR-11.

### Architecture: Controller Pattern (from `AuthController.cs`)

```csharp
[ApiController]
[Route("api/books")]
[Authorize]
public class BooksController(IBookService bookService) : ControllerBase
{
    [HttpGet("{isbn}")]
    public async Task<IActionResult> LookupByISBN(string isbn)
    {
        var result = await bookService.LookupISBNAsync(isbn);
        return Ok(result);  // Ok(null) serialises as HTTP 200 with null body
    }
}
```

**Note:** `Ok(null)` in ASP.NET Core returns HTTP 200 with `null` body — this is the correct behaviour per AC-3 (frontend shows empty manual entry form).

### Architecture: DI Registration in `Program.cs`

Add in the `// Services` block (after existing registrations):

```csharp
builder.Services.AddHttpClient("OpenLibrary", client =>
{
    client.BaseAddress = new Uri("https://openlibrary.org");
    client.Timeout = TimeSpan.FromSeconds(3);
});

builder.Services.AddScoped<IBookService, BookService>();
```

`AddHttpClient` is already available — `Microsoft.Extensions.Http` is included transitively. No new NuGet package required.

### Unit Testing Pattern (from existing `BookTracker.Tests`)

Check the test project to see existing patterns. Use `Moq` for mocking:

```csharp
// Mock IHttpClientFactory returning a handler that returns specific JSON
var handlerMock = new Mock<HttpMessageHandler>();
handlerMock.Protected()
    .Setup<Task<HttpResponseMessage>>("SendAsync",
        ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
    .ReturnsAsync(new HttpResponseMessage
    {
        StatusCode = HttpStatusCode.OK,
        Content = new StringContent(openLibraryJson, Encoding.UTF8, "application/json")
    });

var httpClient = new HttpClient(handlerMock.Object)
{
    BaseAddress = new Uri("https://openlibrary.org"),
    Timeout = TimeSpan.FromSeconds(3)
};

var factoryMock = new Mock<IHttpClientFactory>();
factoryMock.Setup(f => f.CreateClient("OpenLibrary")).Returns(httpClient);
```

Check if `Moq` is already in `BookTracker.Tests.csproj`. If not, it will need to be added — but check first before adding to avoid unnecessary changes.

### Files Touched from Story 2.1

- `Program.cs` — add `AddHttpClient` + `AddScoped<IBookService>` registrations; remove `TODO Story 2.2` comment
- `Repositories/Interfaces/IBookRepository.cs` — already created in 2.1; DO NOT modify
- `Repositories/BookRepository.cs` — already created in 2.1; DO NOT modify

### No New EF Migration Required

Story 2.2 introduces no new database tables or columns. The `Books` table and `UQ_Books_ISBN` constraint already exist from Story 2.1 migration. Do NOT run `dotnet ef migrations add`.

### Testing Requirements (AR-13)

xUnit tests for `BookService` logic (service layer). The 7 test cases in Task 5 cover all significant branching paths. No controller tests required.

## Dev Agent Record

### Agent Model Used

claude-sonnet-4.6

### Debug Log References

### Completion Notes List

- All 6 tasks completed. 7 new BookServiceTests added; total 14/14 tests pass.
- BookResponse DTO, IBookService/BookService, BooksController all created.
- Program.cs updated: AddHttpClient("OpenLibrary") with 3s timeout + AddScoped<IBookService>.
- bare catch in BookService covers all Open Library failures (network, timeout, JSON parse).
- ISBN normalization (trim + ToUpperInvariant) tested explicitly.

### File List

- `backend/BookTracker.Api/DTOs/Books/BookResponse.cs` (new)
- `backend/BookTracker.Api/Services/Interfaces/IBookService.cs` (new)
- `backend/BookTracker.Api/Services/BookService.cs` (new)
- `backend/BookTracker.Api/Controllers/BooksController.cs` (new)
- `backend/BookTracker.Api/Program.cs` (modified — AddHttpClient + AddScoped<IBookService>, removed TODO 2.2)
- `backend/BookTracker.Tests/Services/BookServiceTests.cs` (new — 7 tests)
