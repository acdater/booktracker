# Story 1.2: Database Setup & User Domain Model

Status: ready-for-dev

## Story

As a developer,
I want the EF Core `AppDbContext`, `User` entity, and initial database migration created,
so that the authentication schema is in version control and can be applied with a single command.

## Acceptance Criteria

1. `User` entity exists at `Models/User.cs` with fields: `Id` (int, PK, auto-increment), `Email` (varchar), `PasswordHash` (varchar), `FirstName` (varchar), `LastName` (varchar), `DateOfBirth` (DateTime UTC)
2. `AppDbContext` inherits `DbContext`, exposes `DbSet<User> Users`, and configures a unique index named `IX_Users_Email` on `User.Email` in `OnModelCreating`
3. `IUserRepository` interface at `Repositories/Interfaces/IUserRepository.cs` declares `GetByEmailAsync(string email)` returning `Task<User?>` and `CreateAsync(User user)` returning `Task<User>`
4. `UserRepository` at `Repositories/UserRepository.cs` implements `IUserRepository` using primary constructor injection of `AppDbContext`
5. `AppDbContext` and `IUserRepository`/`UserRepository` are registered in `Program.cs` DI (replacing the two TODO Story 1.2 comments)
6. `dotnet ef migrations add InitialCreate` generates a migration file in `Data/Migrations/` and `dotnet ef database update` creates the `Users` table in PostgreSQL with PascalCase columns (`Id`, `Email`, `PasswordHash`, `FirstName`, `LastName`, `DateOfBirth`)
7. `dotnet test` still passes (existing sample test must remain green; no regressions)

## Tasks / Subtasks

- [ ] Task 1: Create User entity (AC: 1)
  - [ ] Create `backend/BookTracker.Api/Models/User.cs`
  - [ ] Properties: `Id` (int), `Email` (string), `PasswordHash` (string), `FirstName` (string), `LastName` (string), `DateOfBirth` (DateTime)
  - [ ] Remove `Models/.gitkeep` — file now has real content

- [ ] Task 2: Create AppDbContext (AC: 2)
  - [ ] Create `backend/BookTracker.Api/Data/AppDbContext.cs`
  - [ ] Inherit `DbContext`; use primary constructor `(DbContextOptions<AppDbContext> options)`
  - [ ] Expose `DbSet<User> Users => Set<User>()`
  - [ ] Override `OnModelCreating` — configure `IX_Users_Email` unique index on `User.Email`

- [ ] Task 3: Create IUserRepository interface (AC: 3)
  - [ ] Create `backend/BookTracker.Api/Repositories/Interfaces/IUserRepository.cs`
  - [ ] Declare `Task<User?> GetByEmailAsync(string email)`
  - [ ] Declare `Task<User> CreateAsync(User user)`
  - [ ] Remove `Repositories/Interfaces/.gitkeep`

- [ ] Task 4: Create UserRepository implementation (AC: 4)
  - [ ] Create `backend/BookTracker.Api/Repositories/UserRepository.cs`
  - [ ] Primary constructor: inject `AppDbContext context`
  - [ ] `GetByEmailAsync`: `context.Users.FirstOrDefaultAsync(u => u.Email == email)`
  - [ ] `CreateAsync`: `context.Users.Add(user)` → `await context.SaveChangesAsync()` → return user

- [ ] Task 5: Wire DI in Program.cs (AC: 5)
  - [ ] Add `using Microsoft.EntityFrameworkCore;` to Program.cs imports
  - [ ] Add `using BookTracker.Api.Data;` and `using BookTracker.Api.Repositories;` and `using BookTracker.Api.Repositories.Interfaces;`
  - [ ] Replace `// TODO Story 1.2: Register AppDbContext` with: `builder.Services.AddDbContext<AppDbContext>(options => options.UseNpgsql(builder.Configuration.GetConnectionString("Default")));`
  - [ ] Replace `// TODO Story 1.2: Register IUserRepository / UserRepository` with: `builder.Services.AddScoped<IUserRepository, UserRepository>();`

- [ ] Task 6: Generate and apply EF Core migration (AC: 6)
  - [ ] Confirm `dotnet user-secrets set "ConnectionStrings__Default" "..."` is set (prerequisite)
  - [ ] From `backend/BookTracker.Api/`: run `dotnet ef migrations add InitialCreate`
  - [ ] Verify migration file appears in `Data/Migrations/` (two files: `*_InitialCreate.cs` + snapshot)
  - [ ] Run `dotnet ef database update` — confirm `Users` table created in PostgreSQL
  - [ ] Verify columns are PascalCase: `Id`, `Email`, `PasswordHash`, `FirstName`, `LastName`, `DateOfBirth`

- [ ] Task 7: Verify no regressions (AC: 7)
  - [ ] Run `dotnet test` from repo root — must pass (1 sample test green)
  - [ ] Run `dotnet build` — 0 errors, 0 warnings about missing types

## Dev Notes

### Prerequisites

Story 1.2 requires a live PostgreSQL connection to run the migration. Before Task 6:

```powershell
cd backend/BookTracker.Api
dotnet user-secrets set "ConnectionStrings__Default" "Host=localhost;Database=booktracker;Username=postgres;Password=yourpassword"
```

The app still starts without a connection string set (fallback is `null` for EF Core — it will throw only when `AddDbContext` tries to use the connection, not at startup if no request hits the DB).

### Exact Code — User.cs

```csharp
namespace BookTracker.Api.Models;

public class User
{
    public int Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public DateTime DateOfBirth { get; set; }
}
```

**No `[Column]` or `[Table]` attributes** — EF Core default naming produces PascalCase table/column names, which is exactly what the architecture requires. Do NOT add `[JsonPropertyName]` or custom serialization attributes — camelCase is handled globally in Program.cs.

### Exact Code — AppDbContext.cs

```csharp
using Microsoft.EntityFrameworkCore;
using BookTracker.Api.Models;

namespace BookTracker.Api.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<User> Users => Set<User>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<User>(entity =>
        {
            entity.HasIndex(u => u.Email)
                  .HasDatabaseName("IX_Users_Email")
                  .IsUnique();
        });
    }
}
```

**Why `=> Set<User>()`?** The `Set<User>()` expression-bodied property is idiomatic EF Core 8+ — avoids the nullable warning from `DbSet<User> Users { get; set; }` without `= null!`.

### Exact Code — IUserRepository.cs

```csharp
using BookTracker.Api.Models;

namespace BookTracker.Api.Repositories.Interfaces;

public interface IUserRepository
{
    Task<User?> GetByEmailAsync(string email);
    Task<User> CreateAsync(User user);
}
```

### Exact Code — UserRepository.cs

```csharp
using BookTracker.Api.Data;
using BookTracker.Api.Models;
using BookTracker.Api.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace BookTracker.Api.Repositories;

public class UserRepository(AppDbContext context) : IUserRepository
{
    public async Task<User?> GetByEmailAsync(string email)
    {
        return await context.Users
            .FirstOrDefaultAsync(u => u.Email == email);
    }

    public async Task<User> CreateAsync(User user)
    {
        context.Users.Add(user);
        await context.SaveChangesAsync();
        return user;
    }
}
```

**Do NOT inject `ILogger` here** — repositories have no logging responsibility; let EF Core's built-in SQL logging handle diagnostics.

### Program.cs Changes — Exact Diff

Replace these two TODO lines (lines 38–39 in current Program.cs):

```csharp
// TODO Story 1.2: Register AppDbContext
// TODO Story 1.2: Register IUserRepository / UserRepository
```

With:

```csharp
// Database
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("Default")));

// Repositories
builder.Services.AddScoped<IUserRepository, UserRepository>();
```

Add these `using` directives at the top of Program.cs (after existing usings):

```csharp
using BookTracker.Api.Data;
using BookTracker.Api.Repositories;
using BookTracker.Api.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;
```

### EF Core Migration Notes

- Run `dotnet ef migrations add InitialCreate` from `backend/BookTracker.Api/` directory (not repo root, not test project)
- `Microsoft.EntityFrameworkCore.Design` is already installed (Story 1.1)
- The migration will create **two files**: `Data/Migrations/{timestamp}_InitialCreate.cs` and `Data/Migrations/AppDbContextModelSnapshot.cs`
- **Never hand-edit migration files** — if you need to change the schema, add a new migration
- Column names in generated migration will be `Id`, `Email`, `PasswordHash`, `FirstName`, `LastName`, `DateOfBirth` (PascalCase — correct per architecture)
- `DateOfBirth` will be mapped to PostgreSQL `timestamp with time zone` by Npgsql (UTC storage — correct per architecture)

### .gitkeep Removal

When Task 1 creates `Models/User.cs`, the `.gitkeep` in `Models/` should be removed (folder is no longer empty). Similarly, when Task 3 creates `IUserRepository.cs`, remove `Repositories/Interfaces/.gitkeep`. Git will track the real files.

`Repositories/.gitkeep` should also be removed once `UserRepository.cs` is created there.

### Architecture Compliance Checklist

- ✅ One class per file — `User.cs`, `AppDbContext.cs`, `IUserRepository.cs`, `UserRepository.cs`
- ✅ Interface prefix `I` — `IUserRepository`
- ✅ PascalCase EF Core column names (no `NamingConventions` package, no `snake_case` override)
- ✅ Unique index named `IX_Users_Email` (matches `IX_{Table}_{Column}` convention)
- ✅ No `DbContext` injection in controllers — only via `UserRepository`
- ✅ No business logic in repository — just EF Core CRUD
- ✅ No raw EF Core LINQ outside repositories

### Testing Notes

No **new** tests are required for Story 1.2. The architecture specifies unit tests for the **service layer** only. `UserRepository` is infrastructure code tested via integration (Story 1.3+ service tests will cover the repository indirectly). The existing `UnitTest1.cs` sample test must remain green.

Run after completion:
```powershell
dotnet test backend/BookTracker.Tests/BookTracker.Tests.csproj
# Expected: Passed! - Failed: 0, Passed: 1
```

### From Story 1.1 — Applied Learnings

- `.NET 10 uses `.slnx` solution format** — already present at repo root, no changes needed
- **Primary constructor syntax** is used consistently throughout this project (see `ExceptionHandlingMiddleware` pattern)
- **Connection string key name** in `builder.Configuration.GetConnectionString("Default")` maps to `"ConnectionStrings:Default"` in appsettings / `"ConnectionStrings__Default"` in user-secrets — both forms work

### Project Structure Notes

Files being created (all new):
- `backend/BookTracker.Api/Models/User.cs` — NEW
- `backend/BookTracker.Api/Data/AppDbContext.cs` — NEW
- `backend/BookTracker.Api/Repositories/Interfaces/IUserRepository.cs` — NEW
- `backend/BookTracker.Api/Repositories/UserRepository.cs` — NEW
- `backend/BookTracker.Api/Data/Migrations/*_InitialCreate.cs` — generated by `dotnet ef`
- `backend/BookTracker.Api/Data/Migrations/AppDbContextModelSnapshot.cs` — generated by `dotnet ef`

Files being modified:
- `backend/BookTracker.Api/Program.cs` — add usings + replace 2 TODO comments with real DI registrations

Files being deleted (replaced by real files):
- `backend/BookTracker.Api/Models/.gitkeep`
- `backend/BookTracker.Api/Repositories/Interfaces/.gitkeep`
- `backend/BookTracker.Api/Repositories/.gitkeep` (if present in Repositories/ root)

### References

- [Source: docs/epics/epic-1-project-foundation-user-authentication.md#Story 1.2]
- [Source: docs/architecture/implementation-patterns-consistency-rules.md#Database Naming Conventions]
- [Source: docs/architecture/implementation-patterns-consistency-rules.md#Backend Project Structure]
- [Source: docs/architecture/implementation-patterns-consistency-rules.md#Code Naming Conventions]
- [Source: docs/architecture/core-architectural-decisions.md#Data Architecture]
- [Source: docs/architecture/project-structure-boundaries.md#Complete Project Directory Structure]
- [Source: backend/BookTracker.Api/Program.cs — current state with TODO comments]

## Dev Agent Record

### Agent Model Used

{{agent_model_name_version}}

### Debug Log References

### Completion Notes List

### File List
