# Requirements Inventory

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
