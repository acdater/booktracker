# Story 1.1: Backend Project Scaffold

Status: review

## Story

As a developer,
I want the .NET backend scaffolded with the correct structure, all required packages, and `Program.cs` fully wired,
so that all subsequent backend stories have a working, runnable foundation to build on.

## Acceptance Criteria

1. `dotnet new webapi --use-controllers -n BookTracker.Api -o backend` has been run (the `--use-controllers` flag is REQUIRED — .NET 8+ defaults to Minimal APIs without it)
2. All required folder structure exists under `backend/BookTracker.Api/`: `Controllers/`, `Services/Interfaces/`, `Repositories/Interfaces/`, `Models/Enums/`, `DTOs/Auth/`, `DTOs/Books/`, `DTOs/Shelf/`, `DTOs/Stats/`, `Data/Migrations/`, `Middleware/`
3. NuGet packages added: `Npgsql.EntityFrameworkCore.PostgreSQL`, `Microsoft.EntityFrameworkCore.Design`, `Microsoft.AspNetCore.Authentication.JwtBearer`, `BCrypt.Net-Next`, `Swashbuckle.AspNetCore`
4. `Program.cs` wired with: global camelCase JSON (`JsonNamingPolicy.CamelCase`), `ExceptionHandlingMiddleware` registered before routing, Swagger UI at `/swagger` in Development only, permissive CORS (`AllowAnyOrigin`) for local dev
5. `appsettings.json` contains required config keys: `ConnectionStrings__Default`, `JWT__Secret`, `JWT__ExpiryHours` (value: 24)
6. `appsettings.Development.json` documents the `dotnet user-secrets` key names for `ConnectionStrings__Default` and `JWT__Secret`
7. xUnit test project exists at `backend/BookTracker.Tests/` with a project reference to `BookTracker.Api`
8. `dotnet run` from `backend/BookTracker.Api/` starts successfully on `https://localhost:5001` with no errors (no DB connection required for startup)

## Tasks / Subtasks

- [x] Task 1: Scaffold the .NET backend project (AC: 1, 2)
  - [x] Run `dotnet new webapi --use-controllers -n BookTracker.Api -o backend` from project root
  - [x] Delete generated placeholder files: `WeatherForecast.cs`, `Controllers/WeatherForecastController.cs`
  - [x] Create empty folders: `Services/Interfaces/`, `Repositories/Interfaces/`, `Models/Enums/`, `DTOs/Auth/`, `DTOs/Books/`, `DTOs/Shelf/`, `DTOs/Stats/`, `Data/Migrations/`, `Middleware/`
  - [x] Add `.gitkeep` files in empty folders so they are tracked by git

- [x] Task 2: Add NuGet packages (AC: 3)
  - [x] `dotnet add package Npgsql.EntityFrameworkCore.PostgreSQL`
  - [x] `dotnet add package Microsoft.EntityFrameworkCore.Design`
  - [x] `dotnet add package Microsoft.AspNetCore.Authentication.JwtBearer`
  - [x] `dotnet add package BCrypt.Net-Next`
  - [x] `dotnet add package Swashbuckle.AspNetCore`

- [x] Task 3: Create ExceptionHandlingMiddleware (AC: 4)
  - [x] Create `Middleware/ExceptionHandlingMiddleware.cs`
  - [x] Catch all unhandled exceptions and write `{ "error": "...", "code": "INTERNAL_ERROR" }` JSON response with status 500
  - [x] Log exception via `ILogger<ExceptionHandlingMiddleware>` before responding

- [x] Task 4: Wire Program.cs (AC: 4)
  - [x] Configure global camelCase JSON via `JsonNamingPolicy.CamelCase`
  - [x] Register `ExceptionHandlingMiddleware` in middleware pipeline before `app.UseRouting()`
  - [x] Add Swagger only in `app.Environment.IsDevelopment()`
  - [x] Add CORS with `AllowAnyOrigin`, `AllowAnyMethod`, `AllowAnyHeader` for local dev
  - [x] Add stubs for services/repositories DI (placeholder comments for future stories)
  - [x] Add JWT bearer auth configuration reading `JWT__Secret` and `JWT__ExpiryHours` from config (setup now, used in Story 1.4)

- [x] Task 5: Configure app settings (AC: 5, 6)
  - [x] Update `appsettings.json` with keys: `ConnectionStrings.Default`, `JWT.Secret`, `JWT.ExpiryHours: 24`
  - [x] Update `appsettings.Development.json` with comments documenting `dotnet user-secrets` key names

- [x] Task 6: Create xUnit test project (AC: 7)
  - [x] Run `dotnet new xunit -n BookTracker.Tests -o backend/BookTracker.Tests`
  - [x] Add project reference: `dotnet add backend/BookTracker.Tests reference backend/BookTracker.Api`
  - [x] Create solution file: `dotnet new sln -n BookTracker` and add both projects
  - [x] Verify `dotnet test` runs (0 tests passing is fine at this stage)

- [x] Task 7: Verify startup (AC: 8)
  - [x] Run `dotnet run` from `backend/BookTracker.Api/` — confirm no startup errors
  - [x] Confirm Swagger UI is accessible at `https://localhost:5001/swagger`

## Dev Notes

### CRITICAL: --use-controllers Flag
`dotnet new webapi --use-controllers` is MANDATORY. Without it, .NET 8+ generates a Minimal APIs project (no `Controllers/` folder, no `[ApiController]` pattern). All subsequent stories depend on the controller-based structure.

### Program.cs Wiring — Exact Order

```csharp
var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers()
    .AddJsonOptions(o =>
        o.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase);

builder.Services.AddCors(options =>
    options.AddDefaultPolicy(policy =>
        policy.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader()));

// JWT auth setup (used starting Story 1.4)
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options => {
        var secret = builder.Configuration["JWT__Secret"]!;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret)),
            ValidateIssuer = false,
            ValidateAudience = false
        };
    });
builder.Services.AddAuthorization();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

app.UseMiddleware<ExceptionHandlingMiddleware>(); // ← FIRST in pipeline

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();
```

### ExceptionHandlingMiddleware Pattern

```csharp
public class ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        try { await next(context); }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unhandled exception");
            context.Response.StatusCode = 500;
            context.Response.ContentType = "application/json";
            await context.Response.WriteAsJsonAsync(new { error = "An unexpected error occurred.", code = "INTERNAL_ERROR" });
        }
    }
}
```

### appsettings.json

```json
{
  "ConnectionStrings": {
    "Default": ""
  },
  "JWT": {
    "Secret": "",
    "ExpiryHours": 24
  },
  "Logging": { "LogLevel": { "Default": "Information", "Microsoft.AspNetCore": "Warning" } },
  "AllowedHosts": "*"
}
```

### appsettings.Development.json

```json
{
  "Logging": { "LogLevel": { "Default": "Information", "Microsoft.AspNetCore": "Warning" } },
  "_README": {
    "setup": "Run these commands to configure local secrets:",
    "db":    "dotnet user-secrets set \"ConnectionStrings__Default\" \"Host=localhost;Database=booktracker;Username=postgres;Password=yourpassword\"",
    "jwt":   "dotnet user-secrets set \"JWT__Secret\" \"your-256-bit-secret-here\""
  }
}
```

### Folder Structure to Create

All folders under `backend/BookTracker.Api/`:
```
Controllers/             ← empty (stories 1.3, 1.4, 2.x, 3.x, 4.x populate these)
Services/
  Interfaces/
Repositories/
  Interfaces/
Models/
  Enums/
DTOs/
  Auth/
  Books/
  Shelf/
  Stats/
Data/
  Migrations/            ← EF Core will write here; never hand-edit
Middleware/
```

### Naming Conventions (apply from day 1)
- C# classes: PascalCase; interfaces: `I` prefix + PascalCase
- Private fields: `_camelCase`
- DTOs: `{Action}{Entity}Dto` (e.g. `CreateBookDto`); responses: `{Entity}Response`
- JSON fields globally camelCase — **never** add `[JsonPropertyName]` to override this

### xUnit Test Project
- Location: `backend/BookTracker.Tests/` (sibling to `BookTracker.Api/`)
- Test files mirror source: `Services/AuthServiceTests.cs`, etc. (populated in later stories)
- Include solution file `BookTracker.sln` at repo root for VS/Rider IDE support
- `dotnet test` must pass (0 tests at this stage is fine)

### Technology Versions
- .NET 10 LTS — confirm SDK with `dotnet --version`
- EF Core 10 — comes via `Npgsql.EntityFrameworkCore.PostgreSQL` latest
- `BCrypt.Net-Next` — use this package (not `BCrypt.Net` or `BCryptNet`)
- `Swashbuckle.AspNetCore` — current stable (6.x compatible with .NET 10)

### Project Structure Notes
- Solution root = `BookTracker/` (repo root)
- Two projects: `backend/BookTracker.Api/` and `backend/BookTracker.Tests/`
- Frontend scaffold is Story 1.5 — do not create `frontend/` in this story
- `Data/Migrations/` folder is git-tracked (EF Core writes migration files there in Story 1.2)
- `.gitkeep` in empty folders ensures git tracks them before first real files are added

### References
- [Source: docs/architecture/core-architectural-decisions.md#Infrastructure & Deployment]
- [Source: docs/architecture/project-structure-boundaries.md#Complete Project Directory Structure]
- [Source: docs/architecture/implementation-patterns-consistency-rules.md#Backend Project Structure]
- [Source: docs/architecture/implementation-patterns-consistency-rules.md#Naming Patterns]
- [Source: docs/epics/epic-1-project-foundation-user-authentication.md#Story 1.1]

## Dev Agent Record

### Agent Model Used

Claude Sonnet 4.6

### Debug Log References

- launchSettings.json ports updated from auto-generated (7059/5154) → 5001/5000 to match Vite proxy target
- .NET 10 generates `.slnx` solution format (not `.sln`) — projects added to `BookTracker.slnx`
- `dotnet test` confirmed passing (1 xUnit sample test) at scaffold stage

### Completion Notes List

- AC1: Scaffolded with `--use-controllers` flag. .NET 10.0.300 SDK confirmed.
- AC2: All 10 required folders created with `.gitkeep` (Controllers, Services/Interfaces, Repositories/Interfaces, Models/Enums, DTOs/Auth, DTOs/Books, DTOs/Shelf, DTOs/Stats, Data/Migrations, Middleware)
- AC3: All 5 NuGet packages added (Npgsql.EFCore.PostgreSQL 10.0.1, EFCore.Design 10.0.8, JwtBearer 10.0.8, BCrypt.Net-Next 4.2.0, Swashbuckle.AspNetCore 10.1.7)
- AC4: Program.cs fully wired — camelCase JSON, ExceptionHandlingMiddleware first, Swagger dev-only, CORS permissive, JWT bearer, DI stub comments
- AC5/6: appsettings.json has ConnectionStrings/JWT keys; appsettings.Development.json has `dotnet user-secrets` instructions in `_README` block
- AC7: BookTracker.Tests project created, references BookTracker.Api, added to BookTracker.slnx solution. `dotnet test` passes.
- AC8: App starts on https://localhost:5001 with `--launch-profile https`. No errors. Swagger at /swagger (Development mode).

### File List

- backend/BookTracker.Api/BookTracker.Api.csproj
- backend/BookTracker.Api/Program.cs
- backend/BookTracker.Api/appsettings.json
- backend/BookTracker.Api/appsettings.Development.json
- backend/BookTracker.Api/Properties/launchSettings.json
- backend/BookTracker.Api/Middleware/ExceptionHandlingMiddleware.cs
- backend/BookTracker.Api/Controllers/.gitkeep
- backend/BookTracker.Api/Services/Interfaces/.gitkeep
- backend/BookTracker.Api/Repositories/Interfaces/.gitkeep
- backend/BookTracker.Api/Models/Enums/.gitkeep
- backend/BookTracker.Api/DTOs/Auth/.gitkeep
- backend/BookTracker.Api/DTOs/Books/.gitkeep
- backend/BookTracker.Api/DTOs/Shelf/.gitkeep
- backend/BookTracker.Api/DTOs/Stats/.gitkeep
- backend/BookTracker.Api/Data/Migrations/.gitkeep
- backend/BookTracker.Tests/BookTracker.Tests.csproj
- backend/BookTracker.Tests/UnitTest1.cs
- BookTracker.slnx
