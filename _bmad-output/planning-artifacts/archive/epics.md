---
stepsCompleted: [1, 2, 3, 4]
inputDocuments:
  - _bmad-output/planning-artifacts/prds/prd-BookTracker-2026-05-25/prd.md
  - _bmad-output/planning-artifacts/architecture.md
  - _bmad-output/planning-artifacts/ux-design-specification.md
---

# BookTracker - Epic Breakdown

## Overview

This document provides the complete epic and story breakdown for BookTracker, decomposing the requirements from the PRD, UX Design, and Architecture into implementable stories.

## Requirements Inventory

### Functional Requirements

FR-1: A visitor can register with email, password, firstName, lastName, dateOfBirth; system creates User with bcrypt hash (cost 12) and returns JWT. Returns 409 on duplicate email, 400 on missing/invalid fields.
FR-2: A registered User can authenticate with email and password and receive a JWT bearer token valid for 24 hours. Returns 401 on wrong credentials without distinguishing which field.
FR-3: All API endpoints except /api/auth/register and /api/auth/login require a valid JWT bearer token; requests without or with expired token return 401.
FR-4: An authenticated User submits an ISBN; if a Book with that ISBN exists in the Catalog it is returned immediately (strips whitespace, normalises ISBN-10 check digit casing).
FR-5: If the ISBN is not in the Catalog, the system queries Open Library server-side and prefills title, author, totalPages, coverImageUrl in a confirmation form (genre never prefilled, always user-selected).
FR-6: When Open Library returns no result or is unreachable, the User can manually fill title, author, totalPages, genre to create the Book (all four fields required; totalPages must be positive integer).
FR-7: User selects genre from a predefined dropdown: Fiction, Non-Fiction, Mystery, Science Fiction, Fantasy, Romance, Biography & Memoir, History, Self-Help, Other. Free-text not permitted; genre is required.
FR-8: Each ISBN maps to exactly one Book in the Catalog regardless of how many Users add it; concurrent adds resolve to one entry via unique constraint + catch-and-re-fetch.
FR-9: The Shelf shows all the authenticated User's UserBooks as book cards (most recent UserBook per Book by readingNumber). Each card: cover/placeholder, title, author, status ribbon, Reader Count. Scrollable flat list, reflects state on page load.
FR-10: Each book card displays "👥 N readers" where N = COUNT(DISTINCT userId) across all UserBooks for that Book; reflects new readers within one page refresh.
FR-11: Each book card displays a color-coded ribbon for the UserBook's current Reading Status; four visually distinct colors (one per status: Resting, Started, Finished, Abandoned).
FR-12: When a User adds a Book to their Shelf (creates a UserBook), the initial Reading Status is Resting, currentPages = 0, startedAt = null, finishedAt = null.
FR-13: The action button on each card reflects only valid transitions: Resting → "Start Reading" (sets Started + startedAt); Started → "Abandon" (sets Abandoned + finishedAt); Finished → "Read Again"; Abandoned → "Read Again". No explicit "Mark Finished" button.
FR-14: Every Reading Status transition produces an immutable BookAction (type=StatusChange, oldValue, newValue, timestamp, userId, userBookId). No update or delete endpoint for BookAction.
FR-15: On a Started UserBook, User can update currentPages via a numeric stepper (range [0, totalPages]; invalid values rejected). Creates PageUpdate BookAction. If new value equals totalPages: auto-transitions to Finished, creates StatusChange BookAction. Both BookActions and UserBook mutation in single SaveChangesAsync().
FR-16: User can open the Reading Journal popup for any UserBook showing all BookActions for that User+Book pair across all readingNumbers, ordered timestamp descending. Each entry: readingNumber, actionType label, oldValue, newValue, formatted timestamp. Read-only.
FR-17: "Read Again" on Finished or Abandoned creates a new UserBook (status=Resting, currentPages=0, readingNumber=MAX+1, startedAt/finishedAt=null). Prior UserBook and all its BookActions unchanged.
FR-18: Stats Strip shows four values for the authenticated User: total UserBooks, Finished count, Started count, pages read this calendar month (SUM of positive PageUpdate deltas in current calendar month). Renders on every Shelf load without additional interaction.
FR-19: Stats Page shows count of UserBooks by each Reading Status (Resting, Started, Finished, Abandoned) plus total — all matching current UserBook records for the User.
FR-20: Stats Page shows books completed (StatusChange to Finished) across rolling windows: 7, 30, 90, 180, 270, 365 days from current moment.
FR-21: Stats Page shows pages read (SUM of positive PageUpdate newValue−oldValue deltas) across the same six rolling windows as FR-20.
FR-22: Stats Page shows Unfinished Genre insight (genre with highest ratio of Started to Finished+Abandoned UserBooks across all readingNumbers) when User has ≥ 3 Started UserBooks across ≥ 2 distinct genres; otherwise shows "Not enough data yet".
FR-23: All Stats Page and Stats Strip figures are computed from BookAction queries at request time; no precomputed counters or nightly aggregation jobs. Inserting a BookAction directly reflects in stats on next page load.

### NonFunctional Requirements

NFR-1: Security — passwords stored as bcrypt hashes (cost ≥ 12); plaintext never persisted or returned; JWT payload contains only userId and expiry; all protected endpoints validate that the authenticated userId owns or has rights to the requested resource.
NFR-2: Performance — Stats Page queries complete in < 2 seconds for a User with up to 500 BookAction events; Shelf load completes in < 1 second for a User with up to 100 UserBooks. No background jobs, caches, or materialized views required at demo scale.
NFR-3: Code Navigability — backend folder structure allows any class to be located by type (Controller, Service, Repository) and domain (Auth, Book, UserBook, Stats) within 3 file-tree traversals; every Service and Repository has a paired interface and implementation; no concrete class injected directly.
NFR-4: Local Runnability — application starts from `git clone` plus two environment values (PostgreSQL connection string + JWT secret); README covers explicit steps to configure and run both backend and frontend locally; no cloud account or additional tooling beyond .NET SDK, Node.js, and a local PostgreSQL instance.

### Additional Requirements

- AR-1: Backend scaffold uses `dotnet new webapi --use-controllers -n BookTracker.Api -o backend` — the `--use-controllers` flag is REQUIRED (.NET 8+ defaults to Minimal APIs).
- AR-2: Required NuGet packages: `Npgsql.EntityFrameworkCore.PostgreSQL`, `Microsoft.EntityFrameworkCore.Design`, `Microsoft.AspNetCore.Authentication.JwtBearer`, `BCrypt.Net-Next`, `Swashbuckle.AspNetCore`.
- AR-3: Frontend scaffold uses `npm create vite@latest frontend -- --template react-ts`, then installs `tailwindcss @tailwindcss/vite`, `@radix-ui/react-dialog @radix-ui/react-visually-hidden`, and `react-router`.
- AR-4: Tailwind CSS v4 — design tokens live in a CSS `@theme {}` block in `src/index.css`, NOT in `tailwind.config.js` (which does not exist in v4).
- AR-5: EF Core code-first migrations; PascalCase table/column naming by default (no `EFCore.NamingConventions` package needed).
- AR-6: `UserBook` model requires `LastActivityAt` (UTC datetime) field set by `ShelfService` on every mutation (AddToShelf, UpdateStatus, UpdatePages, Reread). `GET /api/shelf` orders by `LastActivityAt DESC`.
- AR-7: Global camelCase JSON serialization configured in `Program.cs` via `JsonNamingPolicy.CamelCase`; never override per-controller.
- AR-8: Global exception middleware (`ExceptionHandlingMiddleware`) maps all exceptions to `{ "error": "...", "code": "..." }` error envelope; no try/catch in controllers.
- AR-9: BookAction atomicity rule — every `UserBook` mutation and its `BookAction`(s) MUST be written in a single `SaveChangesAsync()` call; no split saves ever.
- AR-10: Three database indexes required: `IX_BookActions_UserId_Timestamp` (stats), `IX_BookActions_UserId_UserBookId` (journal), `UQ_Books_ISBN` (deduplication).
- AR-11: Open Library integration — server-side proxy in `BookService` via `IHttpClientFactory` named client with 3-second timeout; returns `null` on any failure; frontend shows empty manual entry form.
- AR-12: Vite dev proxy `/api` → `https://localhost:5001` with `secure: false` in `vite.config.ts`; eliminates CORS in development entirely.
- AR-13: xUnit unit tests for service layer only (state machine logic, stats query correctness, ownership validation); no E2E tests; manual verification against SM-1–SM-6.
- AR-14: JWT expiry configurable via `JWT__ExpiryHours` in `appsettings.json` (default: 24); not hardcoded in `Program.cs`.
- AR-15: React Router v7 (`npm install react-router`) with 4 routes: `/login`, `/register`, `/shelf`, `/stats`; `<RequireAuth>` wrapper redirects to `/login` if no token in AuthContext.

### UX Design Requirements

UX-DR1: Implement full warm color palette as Tailwind `@theme` tokens in `src/index.css`: `warm-bg` (#FAF6F0), `warm-surface` (#FFFFFF), `warm-surface-alt` (#F3EEE7), `warm-border` (#E2D9CE), `accent` (#6B7555), `accent-hover` (#556044), `accent-subtle` (#EBF0E6), `text-primary` (#1C1A18), `text-secondary` (#6B6259), `text-disabled` (#ADA49A), `error` (#A84040), `error-bg` (#FDF0EF), `celebration` (#C4874A).
UX-DR2: Implement typography system with 5 type roles using system font stack (`-apple-system, BlinkMacSystemFont, 'Segoe UI', sans-serif`), all in `rem`: display (22px/600), title (17px/600), body (15px/400), caption (13px/400), label (12px/500 +0.02em letter-spacing).
UX-DR3: Implement 4 Reading Status color ribbons with warm palette mapping: Resting = muted slate, Started = earthy amber, Finished = soft sage, Abandoned = dusty rose. Four visually distinct colors, all accessible.
UX-DR4: Implement `BookCard` component: cover-first layout (2:3 aspect ratio cover image), full-card tap target with press state, title/author below, thin progress strip on bottom edge (with `aria-label` for screen readers), status ribbon, Reader Count. 12px border radius, `box-shadow: 0 2px 8px rgba(0,0,0,0.08)` at rest / `0 4px 16px rgba(0,0,0,0.12)` on hover. Cover placeholder (warm-toned silhouette) when no `coverImageUrl`.
UX-DR5: Implement `ProgressPopup` component using Radix UI Dialog: slides up on mobile / centres on desktop, shows book title + cover thumbnail, page stepper pre-loaded with current page, closes on successful submit, keeps popup open with inline error message on failure. Focus trapped; Escape dismisses; focus returns to triggering card on close.
UX-DR6: Implement `PageStepper` component: +/− controls + direct numeric input, pre-loaded with `currentPages`, validates range [0, totalPages], "Update" button activates only when value differs from current saved page.
UX-DR7: Implement `CelebrationOverlay` component: fires only on auto-finish (currentPages = totalPages submit); warm amber animation (not full-screen takeover); auto-dismisses after 3 seconds or on tap; no interaction required to proceed.
UX-DR8: Implement `StatsStrip` component: permanently anchored above the book card list on the Shelf; shows 4 values (total UserBooks, Finished count, Started count, pages this calendar month); no user interaction required; renders on every Shelf load.
UX-DR9: Implement `NavBar` component: bottom tabs on mobile (< 640px), top bar on desktop (≥ 640px); links to Shelf and Stats Page; active state styled with accent color.
UX-DR10: Implement `EmptyState` component with two variants: invitation variant (warm encouraging copy + prominent "Add your first book" CTA, feels like invitation not error) and error variant (neutral-red palette, factual copy describing what went wrong + what user can try next).
UX-DR11: Implement responsive grid layout in `ShelfPage`: mobile (< 640px) 1 column, 16px horizontal margin; tablet (640–1024px) 2 columns, 16px gap; desktop (> 1024px) 3 columns, 24px gap, max-width 1200px centred.
UX-DR12: Implement `JournalPopup` component using Radix UI Dialog: read-only timeline of BookActions (newest first), each entry showing readingNumber + action label + old/new values + formatted timestamp; focus trapped; Escape to dismiss.
UX-DR13: Implement accessibility across all interactive components: minimum 44×44px touch targets on all interactive elements; 2px solid `accent` focus rings with 2px offset on keyboard navigation; error states use color AND descriptive text (never color alone); progress strip `aria-label` with numeric page value.
UX-DR14: Implement form validation behavior: validate on blur (not on change or submit); inline field-level error messages with specific, friendly copy (e.g., "ISBN should be 10 or 13 digits"); required fields marked; submit enabled once all required fields have valid values.
UX-DR15: Implement smooth animated card state transition when a book's Reading Status changes (e.g., Started → Finished on ribbon color change) — not an instant DOM swap. Finish state transition is animated.
UX-DR16: Implement `BookForm` (Add Book flow) as a modal overlay on the Shelf: ISBN input step → system lookup → confirmation/edit form with genre dropdown. Genre dropdown constrained to predefined list. Empty form presented immediately when Open Library returns no result (no blocking error).

### FR Coverage Map

```
FR-1  → Epic 1 — User registration + bcrypt + JWT response
FR-2  → Epic 1 — User login + JWT validation
FR-3  → Epic 1 — JWT middleware + protected route enforcement
FR-4  → Epic 2 — ISBN lookup against shared Catalog
FR-5  → Epic 2 — Open Library server-side proxy + prefill form
FR-6  → Epic 2 — Manual Book entry fallback
FR-7  → Epic 2 — Genre dropdown (predefined list)
FR-8  → Epic 2 — ISBN unique constraint + deduplication upsert
FR-9  → Epic 2 — Shelf display (card list, cover, placeholder, last-activity sort)
FR-10 → Epic 2 — Reader Count (COUNT DISTINCT userId)
FR-11 → Epic 2 — Status ribbon (4 distinct colors per status)
FR-12 → Epic 2 — Initial UserBook status = Resting on shelf add
FR-13 → Epic 3 — Context-aware action button (state machine transitions)
FR-14 → Epic 3 — Immutable BookAction on every status transition
FR-15 → Epic 3 — Page progress stepper + auto-finish + dual BookAction
FR-16 → Epic 3 — Reading Journal popup (all BookActions, newest first)
FR-17 → Epic 3 — Read Again (new UserBook, readingNumber MAX+1)
FR-18 → Epic 4 — Stats Strip (4 live totals on every Shelf load)
FR-19 → Epic 4 — By-status UserBook counts
FR-20 → Epic 4 — Period-bucketed completion counts (6 windows)
FR-21 → Epic 4 — Period-bucketed pages read (6 windows)
FR-22 → Epic 4 — Unfinished Genre insight (with threshold check)
FR-23 → Epic 4 — All stats from BookAction queries at request time
```

## Epic List

### Epic 1: Project Foundation & User Authentication
Users can register, log in, and the app runs locally from a single clone. A developer can clone the repo and run both backend and frontend with two environment values.
**FRs covered:** FR-1, FR-2, FR-3

### Epic 2: Book Catalog & Personal Shelf
Readers can add books by ISBN (with Open Library prefill or manual entry), see their full shelf with status ribbons and reader counts, and manage catalog deduplication.
**FRs covered:** FR-4, FR-5, FR-6, FR-7, FR-8, FR-9, FR-10, FR-11, FR-12

### Epic 3: Reading Lifecycle & Progress Tracking
Readers can manage the full reading lifecycle — start, abandon, and auto-finish books; track page progress with the stepper; view their reading journal across all reads; and start re-reading.
**FRs covered:** FR-13, FR-14, FR-15, FR-16, FR-17

### Epic 4: Reading Analytics
Readers can see their reading stats — a persistent Stats Strip summary on the Shelf and a full Stats Page with period-bucketed completions, pages read, and the Unfinished Genre insight. All figures computed from the event log.
**FRs covered:** FR-18, FR-19, FR-20, FR-21, FR-22, FR-23

<!-- Repeat for each epic in epics_list (N = 1, 2, 3...) -->

## Epic 1: Project Foundation & User Authentication

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

## Epic 2: Book Catalog & Personal Shelf

Readers can add books by ISBN (with Open Library prefill or manual entry), see their full shelf with status ribbons and reader counts, and the warm design system is fully in place.

### Story 2.1: Book & UserBook Domain Models

As a **developer**,
I want the `Book`, `UserBook`, and `ReadingStatus` types with their repositories and migration created,
So that catalog and shelf data can be stored and queried in all subsequent Epic 2 stories.

**Acceptance Criteria:**

**Given** Story 1.2 migration is applied
**When** `dotnet ef migrations add BookAndShelfModels` and `dotnet ef database update` are run
**Then** `Book` entity at `Models/Book.cs` has: `Id` (int, PK), `ISBN` (varchar, unique constraint `UQ_Books_ISBN`), `Title` (varchar), `Author` (varchar), `TotalPages` (int), `Genre` (varchar), `CoverImageUrl` (varchar, nullable)
**And** `UserBook` entity at `Models/UserBook.cs` has: `Id` (int, PK), `UserId` (int, FK → Users), `BookId` (int, FK → Books), `Status` (`ReadingStatus` enum stored as string), `CurrentPages` (int, default 0), `ReadingNumber` (int, default 1), `StartedAt` (DateTime?, nullable), `FinishedAt` (DateTime?, nullable), `LastActivityAt` (DateTime UTC)
**And** `ReadingStatus` enum at `Models/Enums/ReadingStatus.cs`: `Resting`, `Started`, `Finished`, `Abandoned`
**And** `AppDbContext` gains `DbSet<Book> Books` and `DbSet<UserBook> UserBooks` with `UQ_Books_ISBN` unique constraint configured in `OnModelCreating`
**And** `IBookRepository` / `BookRepository` expose: `GetByISBNAsync(string isbn)`, `CreateAsync(Book book)`
**And** `IUserBookRepository` / `UserBookRepository` expose: `GetShelfAsync(int userId)`, `GetByIdAsync(int id)`, `CreateAsync(UserBook ub)`, `UpdateAsync(UserBook ub)`, `GetMaxReadingNumberAsync(int userId, int bookId)`
**And** `IShelfService` / `ShelfService` stubs exist (methods added in Story 2.4); all interfaces and implementations registered in `Program.cs` DI
**And** migration applies cleanly; `Books` and `UserBooks` tables appear in PostgreSQL with PascalCase column names

---

### Story 2.2: ISBN Catalog Lookup & Open Library Proxy

As a **developer**,
I want `GET /api/books/{isbn}` to check the shared catalog and fall back to Open Library,
So that book metadata can be prefilled when a reader adds a new book.

**Acceptance Criteria:**

**Given** the backend is running
**When** `GET /api/books/{isbn}` is called with an ISBN that exists in the Catalog
**Then** returns HTTP 200 with the existing `BookResponse` immediately — no Open Library call made

**When** `GET /api/books/{isbn}` is called with an ISBN not in the Catalog and Open Library returns a match
**Then** `BookService` calls Open Library via `IHttpClientFactory` named client with a 3-second timeout, maps the response to `BookResponse` (title, author, totalPages, coverImageUrl), and returns HTTP 200
**And** genre is NOT prefilled — `genre` field in response is `null`

**When** Open Library is unreachable, times out (> 3 seconds), or returns no match
**Then** returns HTTP 200 with `null` body (frontend shows empty manual entry form — no error)

**And** lookup strips leading/trailing whitespace from ISBN; treats uppercase/lowercase `X` identically in ISBN-10 check digits
**And** `IBookService` / `BookService` exist with `LookupISBNAsync(string isbn)`; `BooksController` delegates to service
**And** `IHttpClientFactory` named client "OpenLibrary" registered in `Program.cs` with `BaseAddress = https://openlibrary.org` and `Timeout = TimeSpan.FromSeconds(3)`

---

### Story 2.3: Book Catalog Creation & Deduplication

As an **authenticated user**,
I want to submit book metadata and have it saved to the shared catalog,
So that the book is available for me and other users to add to their shelves.

**Acceptance Criteria:**

**Given** the user is authenticated and the ISBN does not exist in the Catalog
**When** `POST /api/books` with `{ isbn, title, author, totalPages, genre, coverImageUrl? }`
**Then** creates a `Book` record and returns HTTP 201 with the full `BookResponse`

**Given** the same ISBN already exists (submitted by any user, or concurrent race)
**When** `POST /api/books` with a duplicate ISBN
**Then** `BookService` catches `DbUpdateException` (unique constraint violation), re-fetches the existing `Book`, and returns HTTP 200 with the existing `BookResponse` — no error surfaced to the caller

**Given** any required field is missing or `totalPages` is not a positive integer
**When** `POST /api/books`
**Then** returns HTTP 400 with `{ "error": "...", "code": "VALIDATION_ERROR" }`

**Given** genre value is not in the predefined list (Fiction, Non-Fiction, Mystery, Science Fiction, Fantasy, Romance, Biography & Memoir, History, Self-Help, Other)
**When** `POST /api/books`
**Then** returns HTTP 400 — validated in `BookService` against a constants list, not via DB CHECK constraint

---

### Story 2.4: Add to Shelf & Shelf Display Endpoints

As an **authenticated reader**,
I want to add a catalogued book to my shelf and retrieve my full shelf,
So that I can track my personal reading list and see the most recently active book first.

**Acceptance Criteria:**

**Given** the user is authenticated and a `Book` exists in the Catalog
**When** `POST /api/shelf` with `{ bookId }`
**Then** creates a `UserBook` with `Status = Resting`, `CurrentPages = 0`, `ReadingNumber = 1`, `LastActivityAt = DateTime.UtcNow`
**And** returns HTTP 201 with `UserBookResponse`

**Given** the user is authenticated
**When** `GET /api/shelf`
**Then** returns HTTP 200 with array of `UserBookResponse` ordered by `LastActivityAt DESC`
**And** only the most-recent `UserBook` per Book (highest `ReadingNumber` for userId+bookId) is returned
**And** each `UserBookResponse` includes: `id`, `book` (full `BookResponse`), `status`, `currentPages`, `readingNumber`, `startedAt`, `finishedAt`, `lastActivityAt`, `readerCount`
**And** `readerCount` = `COUNT(DISTINCT UserId)` across all `UserBooks` for that `BookId`
**And** nullable fields (`startedAt`, `finishedAt`, `coverImageUrl`) return as `null` — never omitted from the JSON response

---

### Story 2.5: Shelf Layout, NavBar & BookCard Component

As an **authenticated reader**,
I want to see my shelf as a warm, card-based grid with status ribbons and reader counts,
So that I can recognise my books at a glance on any device.

**Acceptance Criteria:**

**Given** user is on `/shelf`
**When** the page loads
**Then** `ShelfPage` calls `shelfApi.getShelf()` and renders a `BookCard` for each `UserBook`
**And** a `StatsStrip` area renders at the top of the page (static placeholder — wired to live data in Epic 4)
**And** empty shelf (zero UserBooks) shows `EmptyState` invitation variant: warm encouraging copy + prominent "Add your first book" button

**And** `BookCard` (`src/components/BookCard/BookCard.tsx`) renders: cover image at 2:3 aspect ratio or warm-toned placeholder silhouette when `coverImageUrl` is null, title (title type scale), author (body type scale), `StatusRibbon`, reader count ("👥 N readers", caption type scale), thin progress strip along card bottom edge with `aria-label="Page X of Y"` for screen readers, full card is the tap target with visible press state
**And** card styles: 12px border radius, `box-shadow: 0 2px 8px rgba(0,0,0,0.08)` at rest / `0 4px 16px rgba(0,0,0,0.12)` on hover, `warm-surface` background
**And** `StatusRibbon` maps status to color: Resting = muted slate (`#8C98A8`), Started = earthy amber (`#C4874A`), Finished = soft sage (`#6B8F71`), Abandoned = dusty rose (`#B07880`)
**And** `NavBar` renders bottom tabs on mobile (< 640px) and top bar on desktop (≥ 640px); active link uses `accent` color; links to `/shelf` and `/stats`
**And** responsive grid: 1 column < 640px (16px horizontal margin), 2 columns 640–1024px (16px gap), 3 columns > 1024px (24px gap, max-width 1200px centred)
**And** all interactive elements have minimum 44×44px touch targets; keyboard focus rings 2px solid `accent` with 2px offset

---

### Story 2.6: Add Book Flow (Frontend)

As an **authenticated reader**,
I want to add a book by ISBN through a modal on the Shelf,
So that I can catalog new books and see them appear immediately on my shelf.

**Acceptance Criteria:**

**Given** user is on `/shelf` and taps "Add Book"
**Then** `BookForm` modal (`src/components/BookForm/BookForm.tsx`) opens using Radix UI Dialog with focus trapped inside

**When** user enters an ISBN and submits the lookup step
**Then** `booksApi.lookupISBN(isbn)` is called (`GET /api/books/{isbn}`); if a book is returned, form pre-fills title, author, totalPages, coverImageUrl (all fields remain editable); genre dropdown stays empty (user must select)

**When** lookup returns `null` (Open Library miss or unreachable)
**Then** an empty editable form is shown immediately with no blocking error — user fills all fields manually

**When** user completes the form and confirms
**Then** `booksApi.createBook(dto)` called (`POST /api/books`), then `shelfApi.addToShelf(bookId)` called (`POST /api/shelf`); modal closes; shelf re-fetches and new Resting card appears

**And** genre is a `<select>` constrained to the 10 predefined genres; free text not permitted; genre is required
**And** form validation fires on `blur`: title and author non-empty, totalPages positive integer, genre selected — friendly inline messages per field
**And** submit button disabled until all required fields pass validation
**And** API errors display as an inline banner inside the modal; modal stays open on error
**And** `booksApi.ts` exports `lookupISBN(isbn)`, `createBook(dto)`; `shelfApi.ts` exports `getShelf()`, `addToShelf(bookId)`

## Epic 3: Reading Lifecycle & Progress Tracking

Readers can manage the full reading lifecycle — start, abandon, and auto-finish books; track page progress; view their reading journal across all reads; and start re-reading.

### Story 3.1: BookAction Domain Model & Status Transition Endpoint

As an **authenticated reader**,
I want to change the reading status of a book on my shelf,
So that my shelf accurately reflects whether I'm resting, reading, finished, or abandoned a book.

**Acceptance Criteria:**

**Given** Story 2.4 migration is applied
**When** `dotnet ef migrations add BookActionModel` and `dotnet ef database update` are run
**Then** `BookAction` entity at `Models/BookAction.cs` has: `Id` (int, PK), `UserId` (int, FK → Users), `UserBookId` (int, FK → UserBooks), `ActionType` (`ActionType` enum stored as string), `OldValue` (varchar), `NewValue` (varchar), `Timestamp` (DateTime UTC)
**And** `ActionType` enum at `Models/Enums/ActionType.cs`: `StatusChange`, `PageUpdate`
**And** composite indexes in `AppDbContext.OnModelCreating`: `IX_BookActions_UserId_Timestamp` and `IX_BookActions_UserId_UserBookId`
**And** `IBookActionRepository` / `BookActionRepository` expose: `AddAsync(BookAction ba)`, `GetByUserAndBookAsync(int userId, int bookId)` — no update or delete methods exist

**Given** `PATCH /api/shelf/{userBookId}/status` with `{ status }` from an authenticated user who owns the UserBook
**When** the requested transition is valid (Resting→Started, Started→Abandoned)
**Then** `ShelfService.UpdateStatusAsync` sets: `UserBook.Status`, appropriate timestamp (`StartedAt` on →Started; `FinishedAt` on →Abandoned), `LastActivityAt = DateTime.UtcNow`, and inserts one `BookAction` (type=StatusChange, oldValue=prior status string, newValue=new status string, timestamp=now) — all in a **single `SaveChangesAsync()` call**
**And** returns HTTP 200 with updated `UserBookResponse`

**Given** the requested transition is invalid (e.g. Resting→Finished, Started→Finished directly)
**Then** returns HTTP 400 with `{ "error": "Invalid status transition.", "code": "INVALID_TRANSITION" }`

**Given** `UserBook.UserId` does not match the authenticated userId
**Then** returns HTTP 403

**And** `ShelfServiceTests.cs` in `BookTracker.Tests` covers: valid transition writes UserBook + BookAction in a single save call; invalid transition throws; ownership mismatch throws

---

### Story 3.2: Page Progress Update & Auto-Finish

As a **reader with a Started book**,
I want to update my current page count and have the book auto-finish when I reach the last page,
So that my progress is recorded accurately and finishing feels automatic.

**Acceptance Criteria:**

**Given** the user is authenticated, owns the `UserBook`, and its `Status = Started`
**When** `PATCH /api/shelf/{userBookId}/pages` with `{ pages }` where value is in `[0, totalPages)`
**Then** in a **single `SaveChangesAsync()` call**: `UserBook.CurrentPages = pages`, `LastActivityAt = DateTime.UtcNow`, and one `PageUpdate` BookAction inserted (oldValue = prior currentPages as string, newValue = new pages as string, timestamp = now)
**And** returns HTTP 200 with updated `UserBookResponse`

**Given** `pages` equals `UserBook.Book.TotalPages`
**When** `PATCH /api/shelf/{userBookId}/pages`
**Then** in a **single `SaveChangesAsync()` call**: `UserBook.CurrentPages = pages`, `UserBook.Status = Finished`, `FinishedAt = DateTime.UtcNow`, `LastActivityAt = DateTime.UtcNow`, one `PageUpdate` BookAction inserted, AND one `StatusChange` BookAction inserted (oldValue="Started", newValue="Finished") — two BookActions, one save call
**And** `UserBookResponse` includes `status = "Finished"` so the frontend knows to trigger the celebration

**Given** `pages` is outside `[0, totalPages]`
**Then** returns HTTP 400 with `{ "error": "Page value exceeds total pages.", "code": "INVALID_PAGE" }`

**Given** `UserBook.Status` is not `Started`
**Then** returns HTTP 400 with `{ "error": "Page progress only allowed on Started books.", "code": "INVALID_STATE" }`

**Given** `UserBook.UserId` does not match the authenticated userId
**Then** returns HTTP 403

**And** `ShelfServiceTests.cs` covers: normal update, auto-finish produces two BookActions in one save call, out-of-range value rejected, non-Started status rejected

---

### Story 3.3: Reading Journal & Re-read Endpoints

As a **reader**,
I want to view the full event history for a book and start a new reading of a finished book,
So that my reading memoir is preserved and each re-read is independent.

**Acceptance Criteria:**

**Given** the user is authenticated and owns the `UserBook`
**When** `GET /api/shelf/{userBookId}/journal`
**Then** returns HTTP 200 with array of `JournalEntryResponse` for all `BookActions` across **all** `UserBooks` for this User + Book pair (all readingNumbers), ordered by `Timestamp DESC`
**And** each entry includes: `readingNumber`, human-readable `actionType` label ("Status Change" / "Page Update"), `oldValue`, `newValue`, `timestamp` (ISO 8601 UTC)
**And** journal is read-only — no create, update, or delete endpoints for `BookAction` exist

**Given** the `UserBook` has `Status = Finished` or `Abandoned`
**When** `POST /api/shelf/{userBookId}/reread`
**Then** creates a new `UserBook`: `Status = Resting`, `CurrentPages = 0`, `ReadingNumber = MAX(readingNumber for userId+bookId) + 1`, `StartedAt = null`, `FinishedAt = null`, `LastActivityAt = DateTime.UtcNow`
**And** the prior `UserBook` and all its `BookActions` are completely untouched
**And** returns HTTP 201 with the new `UserBookResponse`

**Given** `POST /api/shelf/{userBookId}/reread` on a `Resting` or `Started` UserBook
**Then** returns HTTP 400 with `{ "error": "Read Again is only available for Finished or Abandoned books.", "code": "INVALID_STATE" }`

**Given** `UserBook.UserId` does not match the authenticated userId on either endpoint
**Then** returns HTTP 403

---

### Story 3.4: Frontend Context-Aware Action Buttons

As a **reader**,
I want each book card to show only the valid action for its current status,
So that I can start, abandon, and re-read books directly from the shelf.

**Acceptance Criteria:**

**Given** a `BookCard` with `Status = Resting`
**Then** renders one button: "Start Reading" — tapping calls `shelfApi.updateStatus(userBookId, 'Started')`; on success, shelf data refreshes and ribbon animates to Started (earthy amber) via CSS transition

**Given** a `BookCard` with `Status = Started`
**Then** renders one button: "Abandon" — styled with `text-secondary` (subdued, non-punishing); tapping calls `shelfApi.updateStatus(userBookId, 'Abandoned')`; on success, ribbon animates to Abandoned (dusty rose)
**And** no "Mark Finished" button exists — finishing is triggered exclusively via the page stepper

**Given** a `BookCard` with `Status = Finished` or `Abandoned`
**Then** renders one button: "Read Again" — tapping calls `shelfApi.reread(userBookId)`; on success, new Resting card appears at the top of the shelf (sorted by `LastActivityAt DESC`)

**And** all status ribbon color changes use CSS transitions (not instant DOM swaps) — UX-DR15
**And** API errors from action buttons display as inline card-level error messages; card state does not mutate on error
**And** `shelfApi.ts` exports `updateStatus(userBookId, status)` and `reread(userBookId)`

---

### Story 3.5: Progress Popup & Celebration Overlay

As a **reader with a Started book**,
I want to update my page count through a popup stepper and feel rewarded when I finish,
So that logging progress is fast and reaching the last page feels like an achievement.

**Acceptance Criteria:**

**Given** user taps a `BookCard` with `Status = Started`
**Then** `ProgressPopup` opens (Radix UI Dialog): slides up on mobile / centred on desktop; shows book title, cover thumbnail, and `PageStepper` pre-loaded with `currentPages`

**And** `PageStepper` renders +/− buttons and a direct numeric input; validates range `[0, totalPages]`; "Update" button activates only when the displayed value differs from the pre-loaded `currentPages`

**When** user taps "Update"
**Then** `shelfApi.updatePages(userBookId, newPages)` called; on HTTP 200, popup closes; shelf data refreshes; progress strip on the card animates to the new fill position and page count updates in place

**When** the response has `status = "Finished"` (auto-finish triggered)
**Then** popup closes; `CelebrationOverlay` fires — warm amber animation, not full-screen takeover; auto-dismisses after 3 seconds or on tap; book card ribbon transitions to Finished (soft sage) with CSS animation

**When** the API call fails (network or server error)
**Then** popup stays open with an inline error message; user can retry; no local state mutated

**And** `ProgressPopup` traps focus (Radix Dialog); Escape key dismisses; focus returns to the triggering `BookCard` on close
**And** `CelebrationOverlay` requires no user interaction to proceed — app is fully usable after auto-dismiss
**And** `shelfApi.ts` exports `updatePages(userBookId, pages)`

---

### Story 3.6: Reading Journal Popup

As a **reader**,
I want to open the Reading Journal for any book and see my full event history across all readings,
So that I can reflect on my complete reading journey for that book.

**Acceptance Criteria:**

**Given** user taps the "Journal" trigger on any `BookCard` (any status)
**Then** `JournalPopup` opens (Radix UI Dialog); calls `shelfApi.getJournal(userBookId)` (`GET /api/shelf/{userBookId}/journal`); renders the timeline of all `BookActions` across all readingNumbers, ordered newest first

**And** each entry displays: readingNumber label (e.g. "Read #2"), action label ("Status Change" / "Page Update"), `oldValue`, `newValue`, formatted timestamp (e.g. "May 24, 2026 at 3:41 PM")
**And** journal is entirely read-only — no editing or deletion UI of any kind
**And** popup traps focus; Escape dismisses; focus returns to the triggering card on close
**And** loading state shown while fetching; `EmptyState` error variant shown if request fails
**And** `shelfApi.ts` exports `getJournal(userBookId)`

## Epic 4: Reading Analytics

Readers can see their reading stats — a persistent Stats Strip summary on the Shelf and a full Stats Page with period-bucketed completions, pages read, and the Unfinished Genre insight. All figures computed from the BookAction event log at request time.

### Story 4.1: Stats Strip Endpoint

As an **authenticated reader**,
I want the Stats Strip to always show my current reading totals,
So that I get an at-a-glance overview of my reading life on every Shelf visit.

**Acceptance Criteria:**

**Given** the user is authenticated
**When** `GET /api/stats/strip`
**Then** returns HTTP 200 with `StatsStripResponse`: `{ totalBooks, finishedCount, startedCount, pagesThisMonth }`
**And** `totalBooks` = COUNT of all `UserBook` records for the user (all readingNumbers, all statuses)
**And** `finishedCount` = COUNT of `UserBook` records with `Status = Finished` for the user
**And** `startedCount` = COUNT of `UserBook` records with `Status = Started` for the user
**And** `pagesThisMonth` = SUM of `(newValue − oldValue)` for all `PageUpdate` `BookActions` where `Timestamp` falls within the **current calendar month** (not rolling 30 days) and the delta is positive
**And** all four values computed from `BookAction` / `UserBook` queries at request time — no counter fields on `User` or `UserBook` used as source of truth (FR-23 contract)
**And** `IStatsService` / `StatsService` and `StatsController` created; `StatsController` delegates to service

---

### Story 4.2: Stats Page Endpoint & Service Tests

As an **authenticated reader**,
I want the Stats Page to show my complete reading analytics across six time windows,
So that I can understand my reading patterns in depth.

**Acceptance Criteria:**

**Given** the user is authenticated
**When** `GET /api/stats`
**Then** returns HTTP 200 with `StatsPageResponse` containing all of the following, all computed from `BookAction` queries (FR-23 contract):

**By-status counts (FR-19):** `{ total, resting, started, finished, abandoned }` — COUNT of current `UserBook` records per status

**Period-bucketed completions (FR-20):** `booksCompleted: { days7, days30, days90, days180, days270, days365 }` — COUNT of `StatusChange` BookActions where `NewValue = "Finished"` and `Timestamp >= (now − N days)` for each window; windows are **rolling** from current moment

**Period-bucketed pages (FR-21):** `pagesRead: { days7, days30, days90, days180, days270, days365 }` — SUM of `(newValue − oldValue)` for `PageUpdate` BookActions where `Timestamp >= (now − N days)` and delta > 0

**Unfinished Genre insight (FR-22):**
- If user has ≥ 3 `UserBooks` with `Status = Started` across ≥ 2 distinct genres: returns `unfinishedGenre` = genre with the highest ratio of `Started` UserBooks to `(Finished + Abandoned)` UserBooks across all readingNumbers
- Otherwise: returns `unfinishedGenre = null` (frontend shows "Not enough data yet")

**And** `StatsServiceTests.cs` in `BookTracker.Tests` covers (SM-3 validation):
- Inserting known `BookActions` into a test DB and asserting correct period-bucketed values
- `pagesThisMonth` uses calendar month boundary, not rolling 30 days
- `unfinishedGenre` returns correct genre when threshold met; returns `null` below threshold
- Inserting a new `BookAction` is reflected on the next service call (no caching)

---

### Story 4.3: Frontend StatsStrip (Live Data)

As a **reader**,
I want the Stats Strip on the Shelf to show my live reading totals on every visit,
So that I get an honest summary of my reading life without any extra navigation.

**Acceptance Criteria:**

**Given** user is on `/shelf`
**When** the page loads
**Then** `StatsStrip` calls `statsApi.getStrip()` (`GET /api/stats/strip`) and renders four values: total books, finished, started, pages this calendar month
**And** `StatsStrip` is permanently anchored above the book card grid — visible on every Shelf visit without any user interaction
**And** each value is labelled clearly (e.g. "12 books", "4 finished", "2 reading", "318 pages this month")
**And** loading state renders placeholder "—" values while request is in flight; no layout shift
**And** `StatsStrip` uses `warm-surface-alt` background to visually separate it from the card grid below
**And** `statsApi.ts` exports `getStrip()`

---

### Story 4.4: Frontend Stats Page

As a **reader**,
I want to visit the Stats Page and see my full reading analytics in a clear, readable layout,
So that I can understand my reading habits across different time windows.

**Acceptance Criteria:**

**Given** user navigates to `/stats`
**When** the page loads
**Then** `StatsPage` calls `statsApi.getStats()` (`GET /api/stats`) and renders all analytics sections

**By-status counts section (FR-19):** displays total, resting, started, finished, abandoned counts matching live `UserBook` data

**Books completed section (FR-20):** six rows (7 / 30 / 90 / 180 / 270 / 365 days) each showing the count; labelled clearly (e.g. "Last 30 days: 2 books")

**Pages read section (FR-21):** same six rolling windows showing page totals

**Unfinished Genre section (FR-22):** when `unfinishedGenre` is not null, displays genre name with supporting copy (e.g. "You tend to leave [Genre] books unfinished"); when null, displays "Not enough data yet" placeholder

**And** layout adapts responsively: single-column on mobile (< 640px); sections may use 2-column layout on desktop
**And** loading state shown while fetching; `EmptyState` error variant on request failure
**And** `statsApi.ts` exports `getStats()`
