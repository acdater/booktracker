# Epic 1: Project Foundation & User Authentication

Users can register, log in, and the app runs locally from a single clone. A developer can clone the repo and run both backend and frontend with two environment values.

### Story 1.1: Backend Project Scaffold

As a **developer**,
I want the .NET backend scaffolded with the correct structure, all required packages, and `Program.cs` fully wired,
So that all subsequent backend stories have a working, runnable foundation to build on.

**Acceptance Criteria:**

**Given** .NET 10 SDK is installed
**When** `dotnet new webapi --use-controllers -n BookTracker.Api -o backend` is run
**Then** the project exists at `backend/BookTracker.Api/` with `Controllers/`, `Services/Interfaces/`, `Repositories/Interfaces/`, `Models/Enums/`, `DTOs/Auth/`, `DTOs/Books/`, `DTOs/Shelf/`, `DTOs/Stats/`, `Data/Migrations/`, `Middleware/` folders created
**And** NuGet packages added: `Npgsql.EntityFrameworkCore.PostgreSQL`, `Microsoft.EntityFrameworkCore.Design`, `Microsoft.AspNetCore.Authentication.JwtBearer`, `BCrypt.Net-Next`, `Swashbuckle.AspNetCore`
**And** `Program.cs` registers: global camelCase JSON (`JsonNamingPolicy.CamelCase`), `ExceptionHandlingMiddleware` (maps all unhandled exceptions to `{ "error": "...", "code": "..." }`), Swagger UI at `/swagger` in Development only, permissive CORS (`AllowAnyOrigin`) for local dev
**And** `appsettings.json` documents required keys: `ConnectionStrings__Default`, `JWT__Secret`, `JWT__ExpiryHours` (value: 24)
**And** `appsettings.Development.json` documents `dotnet user-secrets` key names: `ConnectionStrings__Default` and `JWT__Secret`
**And** xUnit test project exists at `backend/BookTracker.Tests/` referencing `BookTracker.Api`
**And** `dotnet run` starts the app on `https://localhost:5001` without errors (no DB connection required yet)

---

### Story 1.2: Database Setup & User Domain Model

As a **developer**,
I want the EF Core `AppDbContext`, `User` entity, and initial database migration created,
So that the authentication schema is in version control and can be applied with a single command.

**Acceptance Criteria:**

**Given** Story 1.1 is complete and a PostgreSQL connection string is set via `dotnet user-secrets set "ConnectionStrings__Default" "..."`
**When** `dotnet ef migrations add InitialCreate` is run followed by `dotnet ef database update`
**Then** `User` entity exists at `Models/User.cs` with fields: `Id` (int, PK, auto-increment), `Email` (varchar, unique index `IX_Users_Email`), `PasswordHash` (varchar), `FirstName` (varchar), `LastName` (varchar), `DateOfBirth` (DateTime UTC)
**And** `AppDbContext` inherits `DbContext`, exposes `DbSet<User> Users`, and configures the email unique index in `OnModelCreating`
**And** `IUserRepository` interface at `Repositories/Interfaces/IUserRepository.cs` declares `GetByEmailAsync(string email)` and `CreateAsync(User user)`
**And** `UserRepository` implements `IUserRepository`, injecting `AppDbContext`; both registered in `Program.cs` DI
**And** migration file is generated in `Data/Migrations/` and the `Users` table appears in PostgreSQL with PascalCase column names after `dotnet ef database update`

---

### Story 1.3: User Registration Endpoint

As a **visitor**,
I want to register a new account with my email, password, first name, last name, and date of birth,
So that I have a personal BookTracker account.

**Acceptance Criteria:**

**Given** the backend is running and database is initialised
**When** `POST /api/auth/register` is called with valid `{ email, password, firstName, lastName, dateOfBirth }`
**Then** a `User` record is created with a bcrypt password hash (cost factor ≥ 12) and stored in the database
**And** response is HTTP 201 with `{ userId, email, firstName, token }` where `token` is a valid JWT bearer token
**And** plaintext password is never stored, logged, or returned in any response

**Given** the email address is already registered
**When** `POST /api/auth/register` with the same email
**Then** returns HTTP 409 with `{ "error": "Email is already registered.", "code": "EMAIL_EXISTS" }`

**Given** any required field is missing or email format is invalid
**When** `POST /api/auth/register`
**Then** returns HTTP 400 with `{ "error": "...", "code": "VALIDATION_ERROR" }`

**And** `RegisterDto` at `DTOs/Auth/RegisterDto.cs` uses `[Required]` and `[EmailAddress]` Data Annotations
**And** `IAuthService` / `AuthService` exist; `AuthController` delegates all logic to `AuthService`; no business logic in the controller

---

### Story 1.4: User Login & JWT Authentication

As a **registered user**,
I want to log in with my email and password and receive a JWT bearer token,
So that I can make authenticated API requests.

**Acceptance Criteria:**

**Given** a registered User exists
**When** `POST /api/auth/login` with `{ email, password }`
**Then** returns HTTP 200 with `{ userId, email, firstName, token }` — token valid for `JWT__ExpiryHours` hours (default 24) with JWT payload `{ userId, exp }` only (no other claims)

**Given** incorrect email or password
**When** `POST /api/auth/login`
**Then** returns HTTP 401 with `{ "error": "Invalid credentials.", "code": "INVALID_CREDENTIALS" }` — does not distinguish which field was wrong

**Given** a request to any protected endpoint without a token
**When** the request arrives
**Then** JWT bearer middleware returns HTTP 401 before the controller action is reached

**Given** an expired JWT token is used on a protected endpoint
**Then** returns HTTP 401

**And** `userId` is extractable in controllers via `User.FindFirstValue(ClaimTypes.NameIdentifier)`
**And** `JWT__ExpiryHours` is read from `appsettings.json` in `Program.cs` — never hardcoded
**And** `ExceptionHandlingMiddleware` is verified to return `{ error, code }` for all unhandled exceptions

---

### Story 1.5: Frontend Scaffold & Design System

As a **developer**,
I want the React frontend scaffolded with Tailwind v4 design tokens, Radix UI, React Router, and the Vite proxy configured,
So that all subsequent stories can build components on a consistent visual and architectural foundation.

**Acceptance Criteria:**

**Given** Node.js is installed
**When** scaffold and install commands run (`npm create vite@latest frontend -- --template react-ts`, then `npm install tailwindcss @tailwindcss/vite @radix-ui/react-dialog @radix-ui/react-visually-hidden react-router`)
**Then** `frontend/` exists with React 19 + TypeScript (strict mode) + Vite 6

**And** `src/index.css` contains `@import "tailwindcss"` and `@theme {}` block with all 13 color tokens: `warm-bg` (#FAF6F0), `warm-surface` (#FFFFFF), `warm-surface-alt` (#F3EEE7), `warm-border` (#E2D9CE), `accent` (#6B7555), `accent-hover` (#556044), `accent-subtle` (#EBF0E6), `text-primary` (#1C1A18), `text-secondary` (#6B6259), `text-disabled` (#ADA49A), `error` (#A84040), `error-bg` (#FDF0EF), `celebration` (#C4874A)
**And** `@theme {}` also defines: font-family system stack (`-apple-system, BlinkMacSystemFont, 'Segoe UI', sans-serif`), border-radius tokens (card: 12px, button: 8px, input: 8px, popup: 16px), box-shadow tokens (card-rest, card-hover, popup)
**And** `vite.config.ts` includes `@tailwindcss/vite` plugin and proxy `/api → https://localhost:5001` with `secure: false`
**And** `src/api/client.ts` exports `fetchJson<T>(url, options)` — injects `Authorization: Bearer <token>` from localStorage; throws `ApiError({ message, code })` on non-2xx responses
**And** `src/types/index.ts` exports shared TypeScript interfaces: `Book`, `UserBook`, `BookAction`, `User`, `AuthResponse`, `StatsStripData`, `StatsPageData`
**And** `npm run dev` starts without errors on `localhost:5173`; `/api` requests proxy to `localhost:5001`

---

### Story 1.6: Authentication Pages & Client-Side Routing

As a **visitor**,
I want to register and log in through a clean UI, with my session persisted across page refreshes,
So that I stay authenticated without re-entering credentials on every visit.

**Acceptance Criteria:**

**Given** the frontend is running and backend is available
**When** a visitor fills the Register form (email, password, firstName, lastName, dateOfBirth) and submits
**Then** `POST /api/auth/register` is called; on success, JWT and userId stored in `AuthContext` (localStorage) and user redirected to `/shelf`
**And** on 409 (duplicate email), inline banner shows "An account with this email already exists"
**And** on 400 (validation), inline banner shows the error message

**When** a registered user fills the Login form (email, password) and submits
**Then** `POST /api/auth/login` called; on success, JWT stored + user redirected to `/shelf`
**And** on 401, inline banner shows "Invalid email or password"

**And** form validation fires on `blur` (not `onChange`): all required fields, email format — friendly inline messages per field
**And** `AuthContext` (`src/context/AuthContext.tsx`) stores `{ token, userId, firstName }`, initialises from `localStorage` on app load, exposes `login(response)` and `logout()` actions
**And** `useAuth` hook (`src/hooks/useAuth.ts`) provides access to `AuthContext`
**And** `<RequireAuth>` (`src/components/RequireAuth/RequireAuth.tsx`) redirects unauthenticated users to `/login`
**And** React Router v7 routes configured in `App.tsx`: `/login → LoginPage`, `/register → RegisterPage`, `/shelf → <RequireAuth><ShelfPage/>`, `/stats → <RequireAuth><StatsPage/>`
**And** `App.tsx` wraps all routes in `AuthContext` provider and renders `NavBar` (basic — full styling in Epic 2)
**And** on page refresh, a valid token in localStorage restores the session without redirecting to `/login`
