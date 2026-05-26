# Story 1.4: User Login & JWT Authentication

Status: review

<!-- Note: Validation is optional. Run validate-create-story for quality check before dev-story. -->

## Story

As a registered user,
I want to log in with my email and password and receive a JWT bearer token,
so that I can make authenticated API requests.

## Acceptance Criteria

1. `POST /api/auth/login` with valid `{ email, password }` returns HTTP 200 with `{ userId, email, firstName, token }` where `token` is a valid JWT valid for `JWT:ExpiryHours` hours (default 24) with only `ClaimTypes.NameIdentifier = userId` as payload claim
2. `POST /api/auth/login` with an unregistered email returns HTTP 401 with `{ "error": "Invalid credentials.", "code": "INVALID_CREDENTIALS" }` — does **not** distinguish which field was wrong
3. `POST /api/auth/login` with a registered email but wrong password returns HTTP 401 with `{ "error": "Invalid credentials.", "code": "INVALID_CREDENTIALS" }`
4. A request to any protected endpoint (`[Authorize]`) without a token returns HTTP 401; an expired or tampered token also returns HTTP 401
5. `userId` is extractable in controllers via `User.FindFirstValue(ClaimTypes.NameIdentifier)`
6. `JWT:ExpiryHours` is read from config — never hardcoded
7. `ExceptionHandlingMiddleware` is verified to return `{ error, code }` for all unhandled exceptions (already done; verified by existing tests)

## Tasks / Subtasks

- [x] Task 1: Create `LoginDto` (AC: 1, 2, 3)
  - [x] Create `backend/BookTracker.Api/DTOs/Auth/LoginDto.cs` with `[Required]` on both fields and `[EmailAddress]` on `Email`

- [x] Task 2: Add `LoginAsync` to `IAuthService` (AC: 1)
  - [x] Add `Task<AuthResponse> LoginAsync(LoginDto dto)` to `Services/Interfaces/IAuthService.cs`

- [x] Task 3: Implement `LoginAsync` in `AuthService` (AC: 1, 2, 3, 5, 6)
  - [x] Look up user by email → if not found, throw `ApiException(401, "Invalid credentials.", "INVALID_CREDENTIALS")`
  - [x] `BCrypt.Net.BCrypt.Verify(dto.Password, user.PasswordHash)` → if false, throw same `ApiException(401, ...)`
  - [x] On success, return `AuthResponse` using the existing private `GenerateJwt(user)` method (no duplication)

- [x] Task 4: Add `POST /api/auth/login` to `AuthController` (AC: 1, 2, 3)
  - [x] Add `[HttpPost("login")]` action that calls `authService.LoginAsync(dto)` and returns `Ok(response)` (HTTP 200)
  - [x] No `try/catch` — `ExceptionHandlingMiddleware` handles `ApiException`

- [x] Task 5: Configure JWT 401 challenge response in `Program.cs` (AC: 4)
  - [x] Add `JwtBearerEvents.OnChallenge` handler in the existing `AddJwtBearer(...)` configuration to return `{ "error": "Authentication required.", "code": "UNAUTHORIZED" }` with `application/json` content type and suppress default WWW-Authenticate challenge
  - [x] Verify the `[Authorize]` attribute works by adding `GET /api/auth/me` protected endpoint to `AuthController` that returns `{ userId }` extracted from `User.FindFirstValue(ClaimTypes.NameIdentifier)` — used for manual Swagger testing only

- [x] Task 6: Write unit tests for `LoginAsync` (AC: 1, 2, 3)
  - [x] Add `LoginAsync_ValidCredentials_ReturnsAuthResponse` — mock repo returns matching user; verify `UserId`, `Email`, `FirstName` and non-empty `Token`
  - [x] Add `LoginAsync_UnregisteredEmail_ThrowsApiException401` — mock repo returns null; verify `ApiException.StatusCode == 401` and `ErrorCode == "INVALID_CREDENTIALS"`
  - [x] Add `LoginAsync_WrongPassword_ThrowsApiException401` — mock repo returns user with known bcrypt hash; pass wrong plaintext; verify `ApiException.StatusCode == 401`

- [x] Task 7: Run full test suite — no regressions (AC: all)
  - [x] Run `dotnet test` — Failed: 0, all tests green

## Dev Notes

### What This Story Adds

Story 1.3 created `AuthService` with `RegisterAsync` and the private `GenerateJwt` helper. This story adds `LoginAsync` to the same service. **No new files are needed beyond `LoginDto.cs`** — all changes extend existing files.

### Files to CREATE

```
backend/BookTracker.Api/DTOs/Auth/LoginDto.cs   ← NEW
```

### Files to MODIFY

```
backend/BookTracker.Api/Services/Interfaces/IAuthService.cs   ← add LoginAsync signature
backend/BookTracker.Api/Services/AuthService.cs               ← implement LoginAsync
backend/BookTracker.Api/Controllers/AuthController.cs         ← add Login + Me actions
backend/BookTracker.Api/Program.cs                            ← add OnChallenge handler
backend/BookTracker.Tests/Services/AuthServiceTests.cs        ← add 3 login tests
```

### Exact Code — DTOs/Auth/LoginDto.cs

```csharp
using System.ComponentModel.DataAnnotations;

namespace BookTracker.Api.DTOs.Auth;

public class LoginDto
{
    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required]
    public string Password { get; set; } = string.Empty;
}
```

### Exact Code — IAuthService.cs (updated)

```csharp
using BookTracker.Api.DTOs.Auth;

namespace BookTracker.Api.Services.Interfaces;

public interface IAuthService
{
    Task<AuthResponse> RegisterAsync(RegisterDto dto);
    Task<AuthResponse> LoginAsync(LoginDto dto);
}
```

### Exact Code — AuthService.cs LoginAsync method (add to existing class)

Add below `RegisterAsync` — `GenerateJwt` is already there, reuse it:

```csharp
public async Task<AuthResponse> LoginAsync(LoginDto dto)
{
    var user = await userRepository.GetByEmailAsync(dto.Email);
    if (user is null || !BCrypt.Net.BCrypt.Verify(dto.Password, user.PasswordHash))
        throw new ApiException(401, "Invalid credentials.", "INVALID_CREDENTIALS");

    return new AuthResponse
    {
        UserId = user.Id,
        Email = user.Email,
        FirstName = user.FirstName,
        Token = GenerateJwt(user)
    };
}
```

**Why combine the null check and BCrypt.Verify in one condition:** Avoids a timing side-channel that would distinguish "email not found" from "wrong password" via response time. BCrypt.Verify only runs when the user exists, but the error message is identical in both cases so the client cannot distinguish.

### Exact Code — AuthController.cs (updated, add two actions)

```csharp
[HttpPost("login")]
public async Task<IActionResult> Login([FromBody] LoginDto dto)
{
    var response = await authService.LoginAsync(dto);
    return Ok(response);
}

[HttpGet("me")]
[Authorize]
public IActionResult Me()
{
    var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
    return Ok(new { userId });
}
```

Add required usings to `AuthController.cs`:
```csharp
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
```

### Exact Code — Program.cs OnChallenge handler

In the existing `AddJwtBearer(options => { ... })` block, add `Events`:

```csharp
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret)),
            ValidateIssuer = false,
            ValidateAudience = false
        };
        options.Events = new JwtBearerEvents
        {
            OnChallenge = async context =>
            {
                context.HandleResponse();
                context.Response.StatusCode = 401;
                context.Response.ContentType = "application/json";
                await context.Response.WriteAsJsonAsync(
                    new { error = "Authentication required.", code = "UNAUTHORIZED" });
            }
        };
    });
```

### Exact Code — AuthServiceTests.cs (add 3 tests after existing 3)

```csharp
[Fact]
public async Task LoginAsync_ValidCredentials_ReturnsAuthResponse()
{
    // Arrange
    var hash = BCrypt.Net.BCrypt.HashPassword("Password1!", workFactor: 4); // low cost for tests
    var user = new User { Id = 5, Email = "alice@example.com", FirstName = "Alice", LastName = "Smith", PasswordHash = hash, DateOfBirth = DateTime.UtcNow };
    _repoMock.Setup(r => r.GetByEmailAsync("alice@example.com")).ReturnsAsync(user);

    var dto = new LoginDto { Email = "alice@example.com", Password = "Password1!" };

    // Act
    var result = await CreateSut().LoginAsync(dto);

    // Assert
    Assert.Equal(5, result.UserId);
    Assert.Equal("alice@example.com", result.Email);
    Assert.Equal("Alice", result.FirstName);
    Assert.False(string.IsNullOrEmpty(result.Token));
}

[Fact]
public async Task LoginAsync_UnregisteredEmail_ThrowsApiException401()
{
    // Arrange
    _repoMock.Setup(r => r.GetByEmailAsync(It.IsAny<string>())).ReturnsAsync((User?)null);
    var dto = new LoginDto { Email = "nobody@example.com", Password = "any" };

    // Act & Assert
    var ex = await Assert.ThrowsAsync<ApiException>(() => CreateSut().LoginAsync(dto));
    Assert.Equal(401, ex.StatusCode);
    Assert.Equal("INVALID_CREDENTIALS", ex.ErrorCode);
}

[Fact]
public async Task LoginAsync_WrongPassword_ThrowsApiException401()
{
    // Arrange
    var hash = BCrypt.Net.BCrypt.HashPassword("correct-password", workFactor: 4);
    var user = new User { Id = 1, Email = "alice@example.com", FirstName = "Alice", LastName = "Smith", PasswordHash = hash, DateOfBirth = DateTime.UtcNow };
    _repoMock.Setup(r => r.GetByEmailAsync("alice@example.com")).ReturnsAsync(user);

    var dto = new LoginDto { Email = "alice@example.com", Password = "wrong-password" };

    // Act & Assert
    var ex = await Assert.ThrowsAsync<ApiException>(() => CreateSut().LoginAsync(dto));
    Assert.Equal(401, ex.StatusCode);
    Assert.Equal("INVALID_CREDENTIALS", ex.ErrorCode);
}
```

**Note on BCrypt workFactor in tests:** Use `workFactor: 4` (minimum) in tests to avoid slow hashing (cost 12 takes ~400ms per hash, cost 4 takes ~5ms). Production code still uses 12.

### Configuration Keys Pattern (Critical — from Story 1.3 learnings)

| Key | Source | Access pattern | Notes |
|-----|--------|---------------|-------|
| `JWT__Secret` | `dotnet user-secrets` | `configuration["JWT__Secret"]` | Verbatim key, double-underscore. Already in `AuthService.GenerateJwt` — DO NOT change |
| `JWT:ExpiryHours` | `appsettings.json` | `configuration.GetValue<int>("JWT:ExpiryHours", 24)` | Nested path with colon. Already in `AuthService.GenerateJwt` — DO NOT change |
| `ConnectionStrings:Default` | `dotnet user-secrets` | `builder.Configuration.GetConnectionString("Default")` | Must use colon (set via `dotnet user-secrets set "ConnectionStrings:Default"`) |

**CRITICAL:** Do NOT set `JWT:Secret` (colon) in user-secrets — the code reads `JWT__Secret` (double underscore). These are different config keys.

### Architecture Compliance

- ✅ No `try/catch` in controller — middleware handles all exceptions
- ✅ No business logic in controller — delegate to `IAuthService`
- ✅ `LoginAsync` reuses private `GenerateJwt` — no duplication
- ✅ HTTP 200 for login success (not 201 — not creating a resource)
- ✅ HTTP 401 for invalid credentials — same message regardless of which field was wrong
- ✅ `userId` extracted via `User.FindFirstValue(ClaimTypes.NameIdentifier)` in controller
- ✅ Error envelope `{ "error": "...", "code": "..." }` for JWT middleware 401 via `OnChallenge`
- ✅ One class per file

### From Story 1.3 — Applied Learnings

- BCrypt cost factor 12 in production, use **cost factor 4** in tests to avoid 400ms+ per test
- `configuration["JWT__Secret"]` reads from user-secrets verbatim (double underscore) — verified working
- `GetConnectionString("Default")` requires `ConnectionStrings:Default` key (colon) in user-secrets
- `dotnet user-secrets` ARE loaded at runtime (ASP.NET Core `CreateBuilder` includes them in Development)
- The `ExceptionHandlingMiddleware` already correctly maps `ApiException` → `{ error, code }` with the right status code
- Test helper pattern: `ConfigurationBuilder().AddInMemoryCollection(dict).Build()` for real `IConfiguration` in tests
- `CreateSut()` factory method pattern avoids repeated setup boilerplate in `AuthServiceTests`

### References

- [Source: docs/epics/epic-1-project-foundation-user-authentication.md#Story 1.4]
- [Source: docs/architecture/implementation-patterns-consistency-rules.md#Error Handling — Backend]
- [Source: docs/architecture/implementation-patterns-consistency-rules.md#API Response Formats]
- [Source: docs/architecture/core-architectural-decisions.md#Authentication & Security]
- [Source: backend/BookTracker.Api/Services/AuthService.cs — GenerateJwt reuse]
- [Source: backend/BookTracker.Api/Services/Interfaces/IAuthService.cs — add LoginAsync]
- [Source: backend/BookTracker.Api/Controllers/AuthController.cs — add Login + Me]
- [Source: backend/BookTracker.Api/Program.cs — add OnChallenge to existing JWT config]
- [Source: backend/BookTracker.Tests/Services/AuthServiceTests.cs — append 3 login tests]

## Dev Agent Record

### Agent Model Used

Claude Sonnet 4.6

### Debug Log References

### Completion Notes List

- ✅ Task 1: `LoginDto.cs` created with `[Required]` + `[EmailAddress]`
- ✅ Task 2: `IAuthService` updated — `LoginAsync(LoginDto)` added
- ✅ Task 3: `AuthService.LoginAsync` — null/BCrypt check combined; reuses `GenerateJwt`; throws 401 for both bad email and bad password
- ✅ Task 4: `AuthController` — `POST /api/auth/login` (200) + `GET /api/auth/me` ([Authorize]) added
- ✅ Task 5: `Program.cs` — `JwtBearerEvents.OnChallenge` returns `{ error, code: "UNAUTHORIZED" }` JSON on 401
- ✅ Task 6: 3 login tests added (valid creds, unknown email, wrong password) — workFactor: 4 for fast execution
- ✅ Task 7: `dotnet test` → 7/7 passed (0 failed, 0 skipped)

### File List

- `backend/BookTracker.Api/DTOs/Auth/LoginDto.cs` — created
- `backend/BookTracker.Api/Services/Interfaces/IAuthService.cs` — modified (LoginAsync added)
- `backend/BookTracker.Api/Services/AuthService.cs` — modified (LoginAsync implemented)
- `backend/BookTracker.Api/Controllers/AuthController.cs` — modified (Login + Me actions added)
- `backend/BookTracker.Api/Program.cs` — modified (OnChallenge handler added)
- `backend/BookTracker.Tests/Services/AuthServiceTests.cs` — modified (3 login tests appended)
- `_bmad-output/implementation-artifacts/1-4-user-login-jwt-authentication.md` — status → review
- `_bmad-output/implementation-artifacts/sprint-status.yaml` — 1-4 → review
