# Story 2.1: Book & UserBook Domain Models

Status: review

<!-- Note: Validation is optional. Run validate-create-story for quality check before dev-story. -->

## Story

As a **developer**,
I want the `Book`, `UserBook`, and `ReadingStatus` types with their repositories and migration created,
so that catalog and shelf data can be stored and queried in all subsequent Epic 2 stories.

## Acceptance Criteria

1. `Book` entity at `Models/Book.cs` with fields: `Id` (int, PK, auto-increment), `ISBN` (varchar), `Title` (varchar), `Author` (varchar), `TotalPages` (int), `Genre` (varchar), `CoverImageUrl` (varchar, nullable)
2. `UserBook` entity at `Models/UserBook.cs` with fields: `Id` (int, PK), `UserId` (int, FK → Users), `BookId` (int, FK → Books), `Status` (`ReadingStatus` enum stored as string), `CurrentPages` (int, default 0), `ReadingNumber` (int, default 1), `StartedAt` (DateTime?, nullable), `FinishedAt` (DateTime?, nullable), `LastActivityAt` (DateTime UTC)
3. `ReadingStatus` enum at `Models/Enums/ReadingStatus.cs`: `Resting`, `Started`, `Finished`, `Abandoned`
4. `AppDbContext` gains `DbSet<Book> Books` and `DbSet<UserBook> UserBooks`; `UQ_Books_ISBN` unique constraint on `Books.ISBN` configured in `OnModelCreating`; FK relationships configured
5. `IBookRepository` / `BookRepository` expose: `GetByISBNAsync(string isbn)`, `CreateAsync(Book book)`
6. `IUserBookRepository` / `UserBookRepository` expose: `GetShelfAsync(int userId)`, `GetByIdAsync(int id)`, `CreateAsync(UserBook ub)`, `UpdateAsync(UserBook ub)`, `GetMaxReadingNumberAsync(int userId, int bookId)`
7. `IShelfService` stub at `Services/Interfaces/IShelfService.cs` and `ShelfService` stub at `Services/ShelfService.cs` exist (empty methods — real implementations added in Story 2.4); all new interfaces and implementations registered in `Program.cs` DI
8. `dotnet ef migrations add BookAndShelfModels` followed by `dotnet ef database update` applies cleanly; `Books` and `UserBooks` tables appear in PostgreSQL with PascalCase column names
9. All existing backend tests (7/7) still pass after changes

## Tasks / Subtasks

- [x] Task 1: Create `ReadingStatus` enum (AC: 3)
  - [x] Create `Models/Enums/ReadingStatus.cs` with values: `Resting`, `Started`, `Finished`, `Abandoned`
  - [x] Namespace: `BookTracker.Api.Models.Enums`

- [x] Task 2: Create `Book` entity (AC: 1)
  - [x] Create `Models/Book.cs` with all required properties
  - [x] `CoverImageUrl` must be nullable (`string?`) — never omit from JSON response (returns `null`)
  - [x] Namespace: `BookTracker.Api.Models`

- [x] Task 3: Create `UserBook` entity (AC: 2)
  - [x] Create `Models/UserBook.cs` with all required properties
  - [x] `Status` property typed as `ReadingStatus` (the enum)
  - [x] `StartedAt` and `FinishedAt` as `DateTime?` (nullable)
  - [x] `LastActivityAt` as `DateTime` (UTC, non-nullable)
  - [x] Navigation properties: `User User`, `Book Book` (EF navigation — not required for DB, but follows project pattern)
  - [x] Namespace: `BookTracker.Api.Models`

- [x] Task 4: Update `AppDbContext` (AC: 4)
  - [x] Add `DbSet<Book> Books => Set<Book>();` and `DbSet<UserBook> UserBooks => Set<UserBook>();`
  - [x] In `OnModelCreating`, configure `UQ_Books_ISBN` unique constraint on `Books.ISBN`
  - [x] Configure `UserBook.Status` stored as string: `entity.Property(u => u.Status).HasConversion<string>()`
  - [x] Configure FK relationships for `UserBook` → `User` and `UserBook` → `Book`

- [x] Task 5: Create `IBookRepository` and `BookRepository` (AC: 5)
  - [x] Create `Repositories/Interfaces/IBookRepository.cs` with methods: `Task<Book?> GetByISBNAsync(string isbn)`, `Task<Book> CreateAsync(Book book)`
  - [x] Create `Repositories/BookRepository.cs` implementing `IBookRepository`, injecting `AppDbContext`
  - [x] `GetByISBNAsync` uses `FirstOrDefaultAsync` — returns `null` if not found
  - [x] `CreateAsync` adds entity, calls `SaveChangesAsync`, returns saved entity with generated `Id`
  - [x] Namespace: `BookTracker.Api.Repositories` / `BookTracker.Api.Repositories.Interfaces`

- [x] Task 6: Create `IUserBookRepository` and `UserBookRepository` (AC: 6)
  - [x] Create `Repositories/Interfaces/IUserBookRepository.cs` with methods:
    - `Task<List<UserBook>> GetShelfAsync(int userId)` — returns all UserBooks for user, ordered by `LastActivityAt DESC`
    - `Task<UserBook?> GetByIdAsync(int id)` — returns null if not found
    - `Task<UserBook> CreateAsync(UserBook ub)`
    - `Task<UserBook> UpdateAsync(UserBook ub)`
    - `Task<int> GetMaxReadingNumberAsync(int userId, int bookId)` — returns 0 if none exist
  - [x] Create `Repositories/UserBookRepository.cs` implementing `IUserBookRepository`
  - [x] `GetShelfAsync` must `Include(ub => ub.Book)` to eagerly load Book data (needed for shelf display)
  - [x] `GetByIdAsync` must also `Include(ub => ub.Book)` for the same reason
  - [x] `UpdateAsync` calls `SaveChangesAsync`, returns updated entity
  - [x] `GetMaxReadingNumberAsync`: `await _db.UserBooks.Where(ub => ub.UserId == userId && ub.BookId == bookId).MaxAsync(ub => (int?)ub.ReadingNumber) ?? 0`

- [x] Task 7: Create `IShelfService` and `ShelfService` stubs (AC: 7)
  - [x] Create `Services/Interfaces/IShelfService.cs` as an empty interface stub (methods added in Story 2.4)
  - [x] Create `Services/ShelfService.cs` as a stub class implementing `IShelfService`, injecting `IUserBookRepository` and `IBookRepository`
  - [x] Do NOT implement any real logic — just the class skeleton

- [x] Task 8: Register new dependencies in `Program.cs` (AC: 7)
  - [x] Register `IBookRepository` / `BookRepository` as scoped
  - [x] Register `IUserBookRepository` / `UserBookRepository` as scoped
  - [x] Register `IShelfService` / `ShelfService` as scoped
  - [x] Remove the `// TODO Story 2.1` comment from Program.cs

- [x] Task 9: Create and apply EF migration (AC: 8)
  - [x] Run `dotnet ef migrations add BookAndShelfModels --project backend/BookTracker.Api`
  - [x] Verify migration file in `Data/Migrations/` looks correct (Books table, UserBooks table, UQ constraint, FK relations, Status stored as string)
  - [x] Run `dotnet ef database update --project backend/BookTracker.Api`
  - [x] Confirm `Books` and `UserBooks` tables exist in PostgreSQL

- [x] Task 10: Verify all existing tests pass (AC: 9)
  - [x] Run `dotnet test backend/BookTracker.Tests`
  - [x] All 7 tests pass with no regressions

## Dev Notes

### ⚠️ CRITICAL: AppDbContext Constructor Pattern (Primary Constructor)

The existing `AppDbContext` uses C# **primary constructor** syntax:

```csharp
public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
```

**Keep this pattern** — do NOT change to a traditional constructor. New `DbSet` properties and `OnModelCreating` entries go into the existing class, not a new one.

### ⚠️ CRITICAL: Enum Stored as String

`UserBook.Status` MUST be stored as a string in PostgreSQL (not an integer). Configure this in `OnModelCreating`:

```csharp
modelBuilder.Entity<UserBook>(entity =>
{
    entity.Property(u => u.Status)
          .HasConversion<string>();
});
```

This means the `Status` column stores `"Resting"`, `"Started"`, `"Finished"`, `"Abandoned"` — matching the PascalCase API response spec.

### ⚠️ CRITICAL: UQ_Books_ISBN — Unique Constraint Name

The constraint must be named exactly `UQ_Books_ISBN` as specified in AR-10. Configure in `OnModelCreating`:

```csharp
modelBuilder.Entity<Book>(entity =>
{
    entity.HasIndex(b => b.ISBN)
          .HasDatabaseName("UQ_Books_ISBN")
          .IsUnique();
});
```

This exact name is referenced in Story 2.3 where `BookService` catches `DbUpdateException` on duplicate ISBN.

### ⚠️ CRITICAL: No Parameter Properties (TypeScript lesson carries over to C#)

The existing backend uses traditional constructor patterns for entities. **Use auto-properties**, not primary constructors for entities. Example:

```csharp
public class Book
{
    public int Id { get; set; }
    public string ISBN { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    // ...
}
```

### Architecture: Repository Pattern

All repositories follow the same pattern established by `UserRepository.cs`:
- Interface in `Repositories/Interfaces/` declares the contract
- Implementation in `Repositories/` injects `AppDbContext` via constructor
- Primary constructor syntax accepted for repositories (matches existing `UserRepository`)
- Return `Task<T>` / `Task<T?>` for all async methods

### Architecture: EF Core Eager Loading

`GetShelfAsync` and `GetByIdAsync` in `UserBookRepository` **MUST** use `.Include(ub => ub.Book)`. Without this, the `Book` navigation property will be `null` when shelf data is returned, causing null reference errors in Story 2.4+.

### Architecture: `LastActivityAt` — AR-6 Requirement

`LastActivityAt` is a required field specified in AR-6:
> `UserBook` model requires `LastActivityAt` (UTC datetime) field set by `ShelfService` on every mutation. `GET /api/shelf` orders by `LastActivityAt DESC`.

The `UserBookRepository.GetShelfAsync` orders by `LastActivityAt DESC` to satisfy this. In Story 2.1, the field exists in the model — `ShelfService` populates it from Story 2.4 onward.

### Architecture: IShelfService Stub

Story 2.1 only creates the **stub** for `IShelfService` / `ShelfService`. Do NOT implement `AddToShelfAsync`, `GetShelfAsync`, etc. yet — those come in Story 2.4. The stub exists only to satisfy DI registration and allow compilation.

```csharp
// Services/Interfaces/IShelfService.cs
namespace BookTracker.Api.Services.Interfaces;

public interface IShelfService
{
    // Methods added in Story 2.4
}

// Services/ShelfService.cs
namespace BookTracker.Api.Services;

public class ShelfService(IUserBookRepository userBookRepository, IBookRepository bookRepository) : IShelfService
{
    private readonly IUserBookRepository _userBookRepository = userBookRepository;
    private readonly IBookRepository _bookRepository = bookRepository;
    // Implementations added in Story 2.4
}
```

### Exact Entity Code

**`Models/Enums/ReadingStatus.cs`**:
```csharp
namespace BookTracker.Api.Models.Enums;

public enum ReadingStatus
{
    Resting,
    Started,
    Finished,
    Abandoned
}
```

**`Models/Book.cs`**:
```csharp
namespace BookTracker.Api.Models;

public class Book
{
    public int Id { get; set; }
    public string ISBN { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Author { get; set; } = string.Empty;
    public int TotalPages { get; set; }
    public string Genre { get; set; } = string.Empty;
    public string? CoverImageUrl { get; set; }
}
```

**`Models/UserBook.cs`**:
```csharp
using BookTracker.Api.Models.Enums;

namespace BookTracker.Api.Models;

public class UserBook
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public int BookId { get; set; }
    public ReadingStatus Status { get; set; } = ReadingStatus.Resting;
    public int CurrentPages { get; set; } = 0;
    public int ReadingNumber { get; set; } = 1;
    public DateTime? StartedAt { get; set; }
    public DateTime? FinishedAt { get; set; }
    public DateTime LastActivityAt { get; set; }

    // Navigation properties
    public User User { get; set; } = null!;
    public Book Book { get; set; } = null!;
}
```

**Updated `AppDbContext.OnModelCreating` additions**:
```csharp
modelBuilder.Entity<Book>(entity =>
{
    entity.HasIndex(b => b.ISBN)
          .HasDatabaseName("UQ_Books_ISBN")
          .IsUnique();
});

modelBuilder.Entity<UserBook>(entity =>
{
    entity.Property(u => u.Status).HasConversion<string>();

    entity.HasOne(u => u.User)
          .WithMany()
          .HasForeignKey(u => u.UserId)
          .OnDelete(DeleteBehavior.Cascade);

    entity.HasOne(u => u.Book)
          .WithMany()
          .HasForeignKey(u => u.BookId)
          .OnDelete(DeleteBehavior.Cascade);
});
```

### Existing Files to NOT Break

- `Data/AppDbContext.cs` — add to it, never replace it. Primary constructor stays.
- `Program.cs` — add DI registrations in the `// Repositories` and `// Services` blocks; remove the `// TODO Story 2.1` comment
- `Data/Migrations/` — do NOT manually edit migration files; let `dotnet ef` generate them
- All existing tests: `AuthService` tests must continue to pass — they do not depend on Book/UserBook

### Database Verification After Migration

After `dotnet ef database update`, verify in pgAdmin or psql:
```sql
\d "Books"       -- should show ISBN with unique constraint
\d "UserBooks"   -- should show Status as varchar, LastActivityAt as timestamptz
\d+ "UserBooks"  -- FK constraints to Users and Books
```

### Testing Requirements

Per AR-13: xUnit tests are for **service layer only**. Story 2.1 adds no service layer logic (only the stub), so **no new tests are required**. Run the existing 7 tests to confirm no regressions.

### Project Context

- **Backend project**: `backend/BookTracker.Api/`
- **Tests project**: `backend/BookTracker.Tests/`
- **Run commands from repo root** (or adjust `--project` flag)
- **EF tools**: `dotnet ef` must be installed (`dotnet tool install --global dotnet-ef`)
- **Proxy note**: AR-12 specifies `/api → https://localhost:5001`; Story 1.5 implementation used `http://localhost:5000` — use whichever profile the developer is running

## Dev Agent Record

### Agent Model Used

claude-sonnet-4.6

### Debug Log References

### Completion Notes List

All 10 tasks completed. Migration `BookAndShelfModels` applied successfully — `Books` and `UserBooks` tables created in PostgreSQL with `UQ_Books_ISBN` unique constraint and FK cascade deletes. `Status` stored as varchar string. All 7 existing tests pass with no regressions. No new service-layer tests required (stub only per AR-13).

### File List

- backend/BookTracker.Api/Models/Enums/ReadingStatus.cs (new)
- backend/BookTracker.Api/Models/Book.cs (new)
- backend/BookTracker.Api/Models/UserBook.cs (new)
- backend/BookTracker.Api/Data/AppDbContext.cs (modified)
- backend/BookTracker.Api/Repositories/Interfaces/IBookRepository.cs (new)
- backend/BookTracker.Api/Repositories/Interfaces/IUserBookRepository.cs (new)
- backend/BookTracker.Api/Repositories/BookRepository.cs (new)
- backend/BookTracker.Api/Repositories/UserBookRepository.cs (new)
- backend/BookTracker.Api/Services/Interfaces/IShelfService.cs (new)
- backend/BookTracker.Api/Services/ShelfService.cs (new)
- backend/BookTracker.Api/Program.cs (modified)
- backend/BookTracker.Api/Data/Migrations/20260526125336_BookAndShelfModels.cs (generated)
- backend/BookTracker.Api/Data/Migrations/20260526125336_BookAndShelfModels.Designer.cs (generated)
- backend/BookTracker.Api/Data/Migrations/AppDbContextModelSnapshot.cs (modified)
