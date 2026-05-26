---
stepsCompleted: [1, 2, 3, 4, 5, 6, 7, 8]
inputDocuments:
  - _bmad-output/planning-artifacts/prds/prd-BookTracker-2026-05-25/prd.md
  - _bmad-output/planning-artifacts/prds/prd-BookTracker-2026-05-25/addendum.md
  - _bmad-output/planning-artifacts/briefs/brief-Agentic AI-2026-05-25/brief.md
  - _bmad-output/planning-artifacts/ux-design-specification.md
workflowType: 'architecture'
lastStep: 8
status: 'complete'
completedAt: '2026-05-26'
project_name: 'BookTracker'
user_name: 'Alexei'
date: '2026-05-26'
---

# Architecture Decision Document

_This document builds collaboratively through step-by-step discovery. Sections are appended as we work through each architectural decision together._

## Project Context Analysis

### Requirements Overview

**Functional Requirements (23 FRs across 8 groups):**

- **Authentication (FR-1–3):** JWT bearer tokens; bcrypt cost 12; registration + login are the only unprotected endpoints. All data operations scoped to authenticated userId.
- **Book Catalog (FR-4–8):** ISBN lookup → shared Catalog dedup → Open Library prefill (server-side proxy) → manual fallback. Genre from predefined list. ISBN unique constraint enforces deduplication with concurrent-safe upsert.
- **Personal Shelf (FR-9–11):** Scrollable flat list of the most-recent UserBook per Book. Status ribbons (4 distinct colors), Reader Count (COUNT DISTINCT), no real-time push.
- **Reading Lifecycle (FR-12–14):** Four-state machine (Resting → Started → Finished/Abandoned; Finished/Abandoned → Resting via Read Again). Every transition produces an immutable BookAction (StatusChange).
- **Page Progress (FR-15–16):** Stepper on Started UserBooks only. Auto-finish when currentPages reaches totalPages (produces both PageUpdate + StatusChange BookActions atomically). Reading Journal is a read-only BookAction timeline.
- **Re-reading (FR-17):** Creates a new UserBook (readingNumber MAX+1); prior record and all its BookActions are immutable and preserved.
- **Stats Strip (FR-18):** Four live totals on every Shelf load — computed from UserBook + BookAction queries, no cached counters.
- **Stats Page (FR-19–23):** By-status counts, 6 rolling-window completion + page totals (7/30/90/180/270/365 days), Unfinished Genre insight. All derived from BookAction at request time — FR-23 is a hard contract.

**Non-Functional Requirements:**

- **Security:** bcrypt cost ≥ 12; JWT payload = {userId, exp} only; all endpoints enforce userId ownership; localStorage token storage is an accepted demo-scope trade-off.
- **Performance:** Stats Page < 2s at 500 BookActions; Shelf load < 1s at 100 UserBooks. No background jobs, caches, or materialized views required at v1 scale.
- **Code Navigability:** Classes locatable by (type × domain) within 3 tree traversals. Every Service and Repository paired with an interface — no concrete injection.
- **Local Runnability:** `git clone` + PostgreSQL connection string + JWT secret. No cloud accounts. README covers full local setup.

**Scale & Complexity:**

- Primary domain: Full-stack web (self-hosted, local-run)
- Complexity level: Low-medium (single-user isolation, demo data scale, no real-time push)
- Estimated architectural components: 4 domain entities, ~8 API resource groups, ~9 frontend components, 1 external integration (Open Library)
- Data volume ceiling: ~500 BookActions per user for NFR targets

### Technical Constraints & Dependencies

- **Stack is pre-decided (from Brief):** .NET MVC backend, React + Vite + TypeScript frontend, PostgreSQL via Entity Framework Core, JWT bearer auth.
- **Open Library API:** Queried server-side (A-2); 3-second timeout suggested; no authentication required for the Books API; manual entry fallback on any failure.
- **Tailwind CSS + custom React components:** No third-party component library. Radix UI primitives allowed for accessibility-critical behaviors (focus trap, ARIA roles) only — zero visual output from Radix.
- **No real-time requirements:** All data reflects state at page load; no WebSockets, SSE, or polling needed.
- **Local-only deployment:** No CDN, no containerization requirement, no environment beyond .NET SDK + Node.js + PostgreSQL in v1.

### Cross-Cutting Concerns Identified

1. **JWT authentication & userId ownership scoping** — every protected endpoint must validate that the authenticated user owns the resource being accessed or mutated.
2. **Immutable event log integrity** — BookAction has no update or delete endpoint. State machine transitions and page updates must be atomic with their BookAction writes.
3. **Reading state machine enforcement** — valid transitions must be enforced in the service layer, not left to controller logic or frontend validation alone.
4. **ISBN deduplication** — concurrent adds of the same ISBN must resolve to one Catalog entry (unique constraint + upsert strategy).
5. **Open Library integration resilience** — timeout handling and fallback to manual entry must be clean and non-blocking; server-side proxy keeps CORS out of the frontend.
6. **Responsive layout strategy** — 375px–1440px; popup interaction model (progress update, Reading Journal) must work on both touch and mouse.

## Starter Template Evaluation

### Primary Technology Domain

Full-stack web application — backend and frontend scaffolded independently, co-located in a single monorepo root.

### Repository Structure Decision

A flat monorepo with two top-level directories:

```
BookTracker/
├── backend/    ← ASP.NET Core Web API project
├── frontend/   ← React + Vite + TypeScript project
└── README.md
```

Rationale: Keeps both halves in one repo (single clone, single README) without requiring a monorepo tool. Simple enough for a demo-scope project; no workspaces or Turborepo needed.

### Backend Starter: ASP.NET Core Web API — .NET 10

**Initialization Command:**

```bash
dotnet new webapi --use-controllers -n BookTracker.Api -o backend
```

`--use-controllers` is required — .NET 8+ defaults to Minimal APIs, which conflicts with the PRD's three-tier controller requirement (§5.3, Addendum §A).

**Required NuGet packages to add after scaffolding:**

```bash
dotnet add package Npgsql.EntityFrameworkCore.PostgreSQL
dotnet add package Microsoft.EntityFrameworkCore.Design
dotnet add package Microsoft.AspNetCore.Authentication.JwtBearer
dotnet add package BCrypt.Net-Next
```

**Architectural Decisions Provided by Starter:**

- **Language & Runtime:** C#, .NET 10 LTS (SDK 10.0.300, released May 2026)
- **Web Framework:** ASP.NET Core MVC with `[ApiController]` + `ControllerBase` — no view rendering, JSON responses only
- **Dependency Injection:** Built-in `IServiceCollection` container; all Services and Repositories registered here
- **Configuration:** `appsettings.json` + environment variable overrides (connection string + JWT secret injected at runtime per §5.4 Local Runnability)
- **Build Tooling:** `dotnet` CLI; `dotnet run` for local dev
- **Project Structure (established by convention):**
  ```
  backend/
  ├── Controllers/
  ├── Services/        ← interfaces + implementations
  ├── Repositories/    ← interfaces + implementations
  ├── Models/          ← domain entities
  ├── DTOs/
  ├── Data/            ← DbContext, migrations
  └── Program.cs
  ```

### Frontend Starter: Vite + React + TypeScript

**Initialization Commands:**

```bash
npm create vite@latest frontend -- --template react-ts
cd frontend
npm install
npm install tailwindcss @tailwindcss/vite
npm install @radix-ui/react-dialog @radix-ui/react-visually-hidden
```

**Architectural Decisions Provided by Starter:**

- **Language:** TypeScript (strict mode)
- **Framework:** React 19 (current at Vite scaffold time)
- **Build Tooling:** Vite 6 — native ESM dev server, Rolldown production build, HMR out of the box
- **Styling:** Tailwind CSS v4 via `@tailwindcss/vite` plugin. **Important difference from UX spec:** Tailwind v4 replaces `tailwind.config.js` token centralization with a CSS `@theme` block in the main stylesheet. All design tokens (colors, shadows, border-radius, font-family) live in `src/index.css` under `@theme { ... }` instead of a JS config file. All UX spec token decisions remain valid — only the location changes.
- **Accessibility primitives:** Radix UI `Dialog` (for ProgressPopup and Journal popup — focus trapping, ARIA roles) + `VisuallyHidden`. Zero visual output from Radix; all styling is custom Tailwind.
- **State Management:** React Context (per Brief/Addendum; confirmed in step 4)
- **Project Structure (established by scaffold):**
  ```
  frontend/
  ├── src/
  │   ├── components/
  │   ├── pages/
  │   ├── context/
  │   ├── hooks/
  │   ├── api/
  │   └── index.css    ← Tailwind @theme tokens live here
  ├── vite.config.ts
  └── package.json
  ```

**Note:** Project initialization should be the first two implementation stories: (1) backend scaffold + packages, (2) frontend scaffold + Tailwind + Radix setup.

## Core Architectural Decisions

### Decision Priority Analysis

**Critical Decisions (Block Implementation):**
- EF Core code-first migrations as the schema management strategy
- ISBN deduplication via unique constraint + catch-and-re-fetch pattern
- BookAction composite indexes for stats query performance
- JWT ownership validation in the service layer
- React Context for global auth + shelf state
- React Router v7 for client-side routing

**Important Decisions (Shape Architecture):**
- Genre stored as `varchar`, validated in service layer (no DB CHECK constraint)
- Consistent JSON error envelope `{ "error": "...", "code": "..." }`
- Native `fetch` wrapped in a thin `api/` module — no Axios
- Vite dev proxy for `/api` → backend (eliminates CORS in dev)
- Swagger/OpenAPI in Development mode only

**Deferred Decisions (Post-MVP):**
- API versioning — not needed at demo scope; no public consumers
- CI/CD pipeline — local run is v1 delivery bar; GitHub Actions is a natural v2 addition
- Refresh token / httpOnly cookie auth hardening — explicitly deferred in PRD §8.2

### Data Architecture

| Decision | Choice | Rationale |
|---|---|---|
| Migration strategy | EF Core code-first migrations (`dotnet ef migrations add`) | Schema changes in version control; `dotnet ef database update` satisfies §5.4 Local Runnability |
| ISBN deduplication | `UNIQUE` constraint on `Books.ISBN` + catch `DbUpdateException` + re-fetch | Simple, correct, no DB-specific upsert syntax; concurrent adds resolve to one record |
| Genre storage | `varchar` column, validated against constant list in service layer | No DB CHECK constraint; keeps migrations simple; validation in domain layer |
| Soft delete | Not used | BookActions are immutable; UserBooks are never deleted — re-reads create new records |
| BookAction indexes | Composite `(userId, timestamp)` + `(userId, userBookId)` | Covers period-bucketed stats and Reading Journal queries; satisfies < 2s NFR at 500 events |

### Authentication & Security

| Decision | Choice | Rationale |
|---|---|---|
| JWT secret management | Environment variable (`JWT__Secret`) + `dotnet user-secrets` for local dev | Never committed; documented in README as required setup step |
| CORS policy | Permissive (`AllowAnyOrigin`) for local dev; irrelevant for v1 (no cloud deployment) | Vite proxy eliminates CORS during development entirely |
| Ownership validation pattern | Service layer receives `userId` from controller (`User.FindFirstValue(ClaimTypes.NameIdentifier)`); validates ownership before any mutation | Keeps controllers thin; ownership logic co-located with domain operations |
| Token storage | `localStorage` | Accepted demo trade-off per PRD A-3; httpOnly cookies deferred post-v1 |

### API & Communication Patterns

**URL Routing Conventions:**

```
POST   /api/auth/register
POST   /api/auth/login
GET    /api/books/{isbn}                      ← catalog lookup
POST   /api/books                             ← create catalog entry
GET    /api/shelf                             ← authenticated user's UserBooks
POST   /api/shelf                             ← add book to shelf (creates UserBook)
PATCH  /api/shelf/{userBookId}/status         ← state machine transition
PATCH  /api/shelf/{userBookId}/pages          ← page progress update
POST   /api/shelf/{userBookId}/reread         ← creates new UserBook
GET    /api/shelf/{userBookId}/journal        ← BookAction history
GET    /api/stats/strip                       ← Stats Strip data
GET    /api/stats                             ← full Stats Page data
```

**Error Response Envelope:**

```json
{ "error": "Human-readable message", "code": "MACHINE_CODE" }
```

All error responses (400, 401, 403, 404, 409, 500) use this shape. Controllers return `Problem()` or custom `ActionResult` wrappers; no naked status codes.

**API Documentation:** Swashbuckle.AspNetCore — Swagger UI at `/swagger` in Development only. Added to NuGet packages.

**API Versioning:** None in v1.

### Frontend Architecture

| Decision | Choice | Rationale |
|---|---|---|
| State management | React Context | Sufficient for auth state + shelf data at this scope; no complex shared mutation patterns; Zustand available as a drop-in if needed |
| Routing | React Router v7 (`npm install react-router`) | Four routes: `/login`, `/register`, `/shelf`, `/stats`; `<RequireAuth>` wrapper checks Context token |
| API client | Native `fetch` wrapped in `src/api/` module | One file per domain (auth, books, shelf, stats); handles Authorization header injection + error parsing; no Axios dependency |
| Form management | Controlled React state + inline validation | Three small forms (Register, Login, Add Book); React Hook Form is overkill |
| Error handling | React Error Boundaries at page level + inline API error display | No global error modal; field validation inline; API errors as banners |

### Infrastructure & Deployment

**Local Development Setup:**

```
Backend:  dotnet run          → https://localhost:5001
Frontend: npm run dev         → http://localhost:5173
          (proxies /api → https://localhost:5001 via Vite config)
```

**Vite proxy configuration (`vite.config.ts`):**

```ts
server: {
  proxy: {
    '/api': {
      target: 'https://localhost:5001',
      secure: false,
    }
  }
}
```

**Environment configuration:**

| Scope | Backend | Frontend |
|---|---|---|
| Local dev | `dotnet user-secrets` (`ConnectionStrings__Default`, `JWT__Secret`) | `.env.local` (gitignored; only needed if proxy is disabled) |
| README required | Documents both values + `dotnet ef database update` step | Documents `npm install` + `npm run dev` |

**Logging:** `ILogger<T>` via ASP.NET Core DI, console sink. No structured logging infrastructure in v1.

**Testing scope (v1):** xUnit unit tests for service layer (state machine logic, stats query correctness, ownership validation). No E2E tests; manual verification against PRD SM-1 through SM-6.

### Decision Impact Analysis

**Implementation Sequence:**
1. Backend scaffold + NuGet packages + `Program.cs` DI wiring
2. Frontend scaffold + Tailwind v4 + Radix + Vite proxy config
3. EF Core DbContext + domain entities + initial migration
4. JWT auth (register, login endpoints + middleware)
5. Book catalog (ISBN lookup, Open Library proxy, deduplication)
6. Shelf + Reading Lifecycle (state machine, BookAction writes, atomic transactions)
7. Page progress + Reading Journal
8. Re-read flow
9. Stats Strip + Stats Page (event-log queries, indexes)
10. Frontend: routing, Context, api/ module, then components

**Cross-Component Dependencies:**
- BookAction writes are always atomic with the mutation that triggers them (single `SaveChanges` call per transaction)
- Stats endpoints depend on BookAction index strategy being in place before performance testing
- Frontend `<RequireAuth>` depends on AuthContext being established before routing is wired
- Vite proxy must be configured before any frontend API calls are made

## Implementation Patterns & Consistency Rules

### Critical Conflict Points Identified

9 areas where AI agents could make inconsistent choices without explicit rules.

### Naming Patterns

#### Database Naming Conventions

EF Core default naming is used — no `EFCore.NamingConventions` package needed.

| Element | Convention | Example |
|---|---|---|
| Table names | PascalCase plural (EF Core default) | `Books`, `Users`, `UserBooks`, `BookActions` |
| Column names | PascalCase (matching C# property names) | `UserId`, `BookId`, `CurrentPages` |
| Primary keys | `Id` (int, auto-increment) | `Id` |
| Foreign keys | `{Entity}Id` | `UserId`, `BookId`, `UserBookId` |
| Indexes | `IX_{Table}_{Columns}` | `IX_BookActions_UserId_Timestamp` |
| Unique constraints | `UQ_{Table}_{Column}` | `UQ_Books_ISBN` |

#### API Naming Conventions

| Element | Convention | Example |
|---|---|---|
| URL segments | kebab-case, plural nouns for resources | `/api/books`, `/api/shelf` |
| Route parameters | camelCase in `{braces}` | `/api/shelf/{userBookId}` |
| Action sub-routes | kebab-case verbs for non-CRUD actions | `/status`, `/pages`, `/reread`, `/journal` |
| Query parameters | camelCase | `?readingNumber=2` |
| JSON response fields | camelCase (configured globally) | `userId`, `currentPages`, `readingNumber` |

ASP.NET Core JSON serialization configured globally in `Program.cs`:
```csharp
builder.Services.AddControllers()
    .AddJsonOptions(o => o.JsonSerializerOptions.PropertyNamingPolicy
        = JsonNamingPolicy.CamelCase);
```

#### Code Naming Conventions

**Backend (C#):**

| Element | Convention | Example |
|---|---|---|
| Classes | PascalCase | `UserBookService`, `BookRepository` |
| Interfaces | `I` prefix + PascalCase | `IUserBookService`, `IBookRepository` |
| Methods | PascalCase | `GetShelfAsync`, `UpdatePagesAsync` |
| Private fields | `_camelCase` | `_bookRepository`, `_logger` |
| DTOs | `{Action}{Entity}Dto` | `CreateBookDto`, `UpdatePagesDto` |
| Response models | `{Entity}Response` | `UserBookResponse`, `StatsStripResponse` |

**Frontend (TypeScript/React):**

| Element | Convention | Example |
|---|---|---|
| Component files | `PascalCase.tsx` | `BookCard.tsx`, `StatsStrip.tsx` |
| Hook files | `use{Name}.ts` | `useAuth.ts`, `useShelf.ts` |
| API module files | `{domain}Api.ts` | `shelfApi.ts`, `statsApi.ts` |
| Utility files | `camelCase.ts` | `dateUtils.ts` |
| Component props types | `{Component}Props` | `BookCardProps` |
| Context types | `{Name}ContextValue` | `AuthContextValue` |

### Structure Patterns

#### Backend Project Structure

```
backend/BookTracker.Api/
├── Controllers/
│   ├── AuthController.cs
│   ├── BooksController.cs
│   ├── ShelfController.cs
│   └── StatsController.cs
├── Services/
│   ├── Interfaces/
│   │   ├── IAuthService.cs
│   │   ├── IBookService.cs
│   │   ├── IShelfService.cs
│   │   └── IStatsService.cs
│   ├── AuthService.cs
│   ├── BookService.cs
│   ├── ShelfService.cs
│   └── StatsService.cs
├── Repositories/
│   ├── Interfaces/
│   │   ├── IBookRepository.cs
│   │   ├── IUserRepository.cs
│   │   ├── IUserBookRepository.cs
│   │   └── IBookActionRepository.cs
│   ├── BookRepository.cs
│   ├── UserRepository.cs
│   ├── UserBookRepository.cs
│   └── BookActionRepository.cs
├── Models/
│   ├── Book.cs
│   ├── User.cs
│   ├── UserBook.cs
│   └── BookAction.cs
├── DTOs/
│   ├── Auth/
│   ├── Books/
│   ├── Shelf/
│   └── Stats/
├── Data/
│   ├── AppDbContext.cs
│   └── Migrations/
└── Program.cs
```

**Rule:** One class per file. File name matches class name exactly.

#### Frontend Project Structure

```
frontend/src/
├── api/               ← one file per domain
│   ├── authApi.ts
│   ├── booksApi.ts
│   ├── shelfApi.ts
│   └── statsApi.ts
├── components/        ← shared/reusable components
│   ├── BookCard/
│   │   ├── BookCard.tsx
│   │   └── BookCard.test.tsx
│   ├── ProgressPopup/
│   ├── StatsStrip/
│   └── ...
├── pages/
│   ├── ShelfPage.tsx
│   ├── StatsPage.tsx
│   ├── LoginPage.tsx
│   └── RegisterPage.tsx
├── context/
│   └── AuthContext.tsx
├── hooks/
│   ├── useAuth.ts
│   └── useShelf.ts
├── types/
│   └── index.ts
├── utils/
│   └── dateUtils.ts
├── App.tsx
└── index.css          ← Tailwind @theme tokens
```

**Rule:** Component folders use PascalCase. All other folders use camelCase. Test files co-located with component (`Component.test.tsx`).

**Backend tests:** Separate `backend/BookTracker.Tests/` project (xUnit), mirroring the source structure.

### Format Patterns

#### API Response Formats

**Success responses — direct object, no wrapper:**
```json
[{ "id": 1, "book": { ... }, "status": "Started", "currentPages": 142 }]
```

**Error responses — always `{ "error": "...", "code": "..." }`:**
```json
{ "error": "Email is already registered.", "code": "EMAIL_EXISTS" }
{ "error": "Page value exceeds total pages.", "code": "INVALID_PAGE" }
```

**HTTP status codes:**

| Situation | Code |
|---|---|
| GET / PATCH success | 200 |
| POST created | 201 |
| Validation failure | 400 |
| Unauthenticated | 401 |
| Resource not owned | 403 |
| Resource not found | 404 |
| Duplicate (ISBN) | 409 |
| Server error | 500 |

#### Data Format Rules

| Data type | Format | Example |
|---|---|---|
| Timestamps | ISO 8601 UTC string | `"2026-05-26T07:18:00Z"` |
| Booleans | `true`/`false` (never 0/1) | `"finished": true` |
| Nullable fields | `null` (never omitted from response) | `"finishedAt": null` |
| Page counts | Integer | `"currentPages": 142` |
| Reading status | PascalCase string | `"status": "Started"` |

### Communication Patterns

#### State Machine Enforcement

**Rule:** All reading status transition logic lives in `ShelfService` only — not in controllers or repositories.

Valid transitions:
```
Resting   → Started    (Start Reading)
Started   → Abandoned  (Abandon)
Started   → Finished   (auto, when currentPages = totalPages)
Finished  → Resting    (Read Again — creates new UserBook)
Abandoned → Resting    (Read Again — creates new UserBook)
```

#### BookAction Atomicity Rule

Every mutation to `UserBook` MUST write its `BookAction`(s) in the **same `SaveChangesAsync()` call**. No split saves.

```csharp
// CORRECT
userBook.Status = ReadingStatus.Started;
userBook.StartedAt = DateTime.UtcNow;
_context.BookActions.Add(new BookAction { ... });
await _context.SaveChangesAsync();  // ← single call

// WRONG — never do this
userBook.Status = ReadingStatus.Started;
await _context.SaveChangesAsync();
_context.BookActions.Add(new BookAction { ... });
await _context.SaveChangesAsync();
```

#### Frontend API Module Pattern

Each `api/` file exports typed async functions. All HTTP calls go through a shared `fetchJson` utility (handles Authorization header + error parsing). **No raw `fetch` calls outside of `src/api/`.**

```typescript
// shape every api function follows
export async function updatePages(userBookId: number, pages: number): Promise<UserBookResponse> {
  return fetchJson(`/api/shelf/${userBookId}/pages`, { method: 'PATCH', body: { pages } });
}
```

#### Loading State Pattern

All data-fetching hooks use the same state shape:

```typescript
interface AsyncState<T> {
  data: T | null;
  loading: boolean;
  error: string | null;
}
```

### Process Patterns

#### Error Handling — Backend

1. Controllers validate input only; do not catch exceptions
2. Services throw typed exceptions (`InvalidOperationException`, `UnauthorizedAccessException`)
3. Global exception middleware in `Program.cs` maps exceptions → error envelope
4. No `try/catch` blocks in controllers

#### Error Handling — Frontend

1. Non-2xx API responses throw `ApiError` with `{ message, code }` fields
2. Page-level React Error Boundaries catch render errors
3. API/form errors displayed inline — no global error modals, no `alert()`

#### Validation Rules

- **Backend:** DTO validation via Data Annotations (`[Required]`, `[Range]`); domain invariant validation (state machine, page bounds) in service layer
- **Frontend:** Validate on `blur`, not on `change`; submit enabled once all required fields have values

### Enforcement Guidelines

**All AI Agents MUST:**

- Follow folder/file structure exactly as defined above
- Use camelCase for all JSON fields (globally configured — do not override per-controller)
- Write `BookAction` and its triggering mutation in a single `SaveChangesAsync()` call
- Validate reading status transitions in `ShelfService` only
- Return `{ "error": "...", "code": "..." }` for all non-2xx responses
- Extract `userId` from JWT in controller, pass to service — never re-query identity in repositories
- Use `null` (not omit) for nullable fields in API responses
- Place no raw `fetch` calls outside of `src/api/`

**Anti-Patterns to Reject:**

- Business/transition logic in controllers
- Direct `DbContext` injection in controllers (use repositories via interfaces)
- `UserBook` mutations without a paired `BookAction` insert
- `snake_case` JSON fields in API responses
- Update or delete endpoints for `BookAction` records

## Project Structure & Boundaries

### Complete Project Directory Structure

```
BookTracker/
├── README.md
├── .gitignore
│
├── backend/
│   ├── BookTracker.Api/
│   │   ├── Controllers/
│   │   │   ├── AuthController.cs          ← FR-1, FR-2, FR-3
│   │   │   ├── BooksController.cs         ← FR-4, FR-5, FR-6, FR-7, FR-8
│   │   │   ├── ShelfController.cs         ← FR-9–FR-17
│   │   │   └── StatsController.cs         ← FR-18–FR-23
│   │   ├── Services/
│   │   │   ├── Interfaces/
│   │   │   │   ├── IAuthService.cs
│   │   │   │   ├── IBookService.cs
│   │   │   │   ├── IShelfService.cs
│   │   │   │   └── IStatsService.cs
│   │   │   ├── AuthService.cs
│   │   │   ├── BookService.cs             ← Open Library proxy lives here
│   │   │   ├── ShelfService.cs            ← state machine + BookAction atomicity
│   │   │   └── StatsService.cs            ← all event-log queries (FR-23 contract)
│   │   ├── Repositories/
│   │   │   ├── Interfaces/
│   │   │   │   ├── IBookRepository.cs
│   │   │   │   ├── IUserRepository.cs
│   │   │   │   ├── IUserBookRepository.cs
│   │   │   │   └── IBookActionRepository.cs
│   │   │   ├── BookRepository.cs
│   │   │   ├── UserRepository.cs
│   │   │   ├── UserBookRepository.cs
│   │   │   └── BookActionRepository.cs    ← read-only; no update/delete methods
│   │   ├── Models/
│   │   │   ├── Book.cs
│   │   │   ├── User.cs
│   │   │   ├── UserBook.cs
│   │   │   ├── BookAction.cs
│   │   │   └── Enums/
│   │   │       ├── ReadingStatus.cs       ← Resting, Started, Finished, Abandoned
│   │   │       └── ActionType.cs          ← StatusChange, PageUpdate
│   │   ├── DTOs/
│   │   │   ├── Auth/
│   │   │   │   ├── RegisterDto.cs
│   │   │   │   ├── LoginDto.cs
│   │   │   │   └── AuthResponse.cs
│   │   │   ├── Books/
│   │   │   │   ├── CreateBookDto.cs
│   │   │   │   └── BookResponse.cs
│   │   │   ├── Shelf/
│   │   │   │   ├── AddToShelfDto.cs
│   │   │   │   ├── UpdateStatusDto.cs
│   │   │   │   ├── UpdatePagesDto.cs
│   │   │   │   ├── UserBookResponse.cs
│   │   │   │   └── JournalEntryResponse.cs
│   │   │   └── Stats/
│   │   │       ├── StatsStripResponse.cs
│   │   │       └── StatsPageResponse.cs
│   │   ├── Data/
│   │   │   ├── AppDbContext.cs
│   │   │   └── Migrations/                ← EF Core generated; never hand-edit
│   │   ├── Middleware/
│   │   │   └── ExceptionHandlingMiddleware.cs  ← maps exceptions → error envelope
│   │   ├── appsettings.json
│   │   ├── appsettings.Development.json   ← documents required secret key names
│   │   └── Program.cs                     ← DI wiring, middleware pipeline, CORS
│   │
│   └── BookTracker.Tests/
│       ├── Services/
│       │   ├── AuthServiceTests.cs
│       │   ├── BookServiceTests.cs
│       │   ├── ShelfServiceTests.cs       ← state machine + atomicity tests
│       │   └── StatsServiceTests.cs       ← event-log query correctness (SM-3)
│       └── BookTracker.Tests.csproj
│
└── frontend/
    ├── src/
    │   ├── api/
    │   │   ├── client.ts                  ← fetchJson utility + ApiError class
    │   │   ├── authApi.ts
    │   │   ├── booksApi.ts
    │   │   ├── shelfApi.ts
    │   │   └── statsApi.ts
    │   ├── components/
    │   │   ├── BookCard/
    │   │   │   ├── BookCard.tsx           ← cover, ribbon, reader count, tap target
    │   │   │   └── BookCard.test.tsx
    │   │   ├── ProgressPopup/
    │   │   │   ├── ProgressPopup.tsx      ← Radix Dialog + PageStepper
    │   │   │   └── ProgressPopup.test.tsx
    │   │   ├── PageStepper/
    │   │   │   └── PageStepper.tsx        ← +/− + direct input; [0, totalPages]
    │   │   ├── JournalPopup/
    │   │   │   └── JournalPopup.tsx       ← Radix Dialog; read-only BookAction list
    │   │   ├── StatsStrip/
    │   │   │   └── StatsStrip.tsx
    │   │   ├── CelebrationOverlay/
    │   │   │   └── CelebrationOverlay.tsx ← auto-finish moment (FR-15)
    │   │   ├── StatusRibbon/
    │   │   │   └── StatusRibbon.tsx       ← color per ReadingStatus
    │   │   ├── EmptyState/
    │   │   │   └── EmptyState.tsx         ← invitation + error variants
    │   │   ├── NavBar/
    │   │   │   └── NavBar.tsx             ← bottom tabs (mobile) / top bar (desktop)
    │   │   └── RequireAuth/
    │   │       └── RequireAuth.tsx        ← redirects to /login if no token
    │   ├── pages/
    │   │   ├── ShelfPage.tsx              ← StatsStrip + card grid + Add Book entry
    │   │   ├── StatsPage.tsx              ← FR-19–FR-22
    │   │   ├── LoginPage.tsx
    │   │   └── RegisterPage.tsx
    │   ├── context/
    │   │   └── AuthContext.tsx            ← token + userId; persists to localStorage
    │   ├── hooks/
    │   │   ├── useAuth.ts
    │   │   └── useShelf.ts
    │   ├── types/
    │   │   └── index.ts                   ← shared TS types (UserBook, Book, Stats…)
    │   ├── utils/
    │   │   └── dateUtils.ts
    │   ├── App.tsx                        ← React Router routes + AuthContext provider
    │   ├── main.tsx
    │   └── index.css                      ← @import "tailwindcss" + @theme tokens
    ├── public/
    │   └── favicon.ico
    ├── vite.config.ts                     ← Tailwind plugin + /api proxy config
    ├── tsconfig.json
    ├── package.json
    └── .env.example
```

### Architectural Boundaries

**API Boundary (Frontend ↔ Backend):**
- All frontend HTTP calls go through `src/api/client.ts` → `fetchJson()`
- Vite proxy routes `/api/*` → `https://localhost:5001` in dev (eliminates CORS)
- JWT bearer token injected by `client.ts` from `AuthContext` on every protected call

**Service Boundary (Controller ↔ Service):**
- Controllers: input validation + `userId` extraction from JWT + delegate to service
- Services: business logic, ownership validation, state machine, Open Library calls
- No `DbContext` in controllers; no business logic in repositories

**Data Boundary (Service ↔ DB):**
- All DB access via repository interfaces — no raw EF Core LINQ outside repositories
- Migrations managed by `dotnet ef` CLI; never edited by hand

**External Integration Boundary (BookService ↔ Open Library):**
- `IHttpClientFactory` named client with 3-second timeout
- On timeout or non-2xx: returns `null`; service caller surfaces empty form (FR-6)
- Open Library response mapped to `BookResponse` DTO before leaving `BookService`

### Requirements to Structure Mapping

| FR Group | Backend | Frontend |
|---|---|---|
| Auth (FR-1–3) | `AuthController`, `AuthService`, `UserRepository`, `DTOs/Auth/` | `LoginPage`, `RegisterPage`, `AuthContext`, `useAuth` |
| Book Catalog (FR-4–8) | `BooksController`, `BookService`, `BookRepository`, `DTOs/Books/` | `booksApi`, Add Book form (in `ShelfPage`) |
| Shelf (FR-9–11) | `ShelfController.GetShelf`, `ShelfService`, `UserBookRepository` | `ShelfPage`, `BookCard`, `StatusRibbon`, `useShelf` |
| Reading Lifecycle (FR-12–14) | `ShelfController.UpdateStatus`, `ShelfService` (state machine) | `BookCard` action button, `shelfApi.updateStatus` |
| Page Progress (FR-15–16) | `ShelfController.UpdatePages` + `.GetJournal`, `ShelfService` (auto-finish) | `ProgressPopup`, `PageStepper`, `JournalPopup`, `CelebrationOverlay` |
| Re-reading (FR-17) | `ShelfController.Reread`, `ShelfService` | `BookCard` "Read Again" button, `shelfApi.reread` |
| Stats Strip (FR-18) | `StatsController.GetStrip`, `StatsService` | `StatsStrip`, `statsApi.getStrip` |
| Stats Page (FR-19–23) | `StatsController.GetStats`, `StatsService` (all event-log queries) | `StatsPage`, `statsApi.getStats` |

### Integration Points

**Data Flow — Page Progress Update (most frequent operation):**
```
User taps BookCard
  → ProgressPopup opens (Radix Dialog)
  → PageStepper pre-loads currentPages
  → User adjusts → taps "Update"
  → shelfApi.updatePages(userBookId, newPages)
  → PATCH /api/shelf/{userBookId}/pages
  → ShelfController → ShelfService.UpdatePagesAsync(userId, userBookId, pages)
  → Validates: Started status, pages in [0, totalPages], user owns UserBook
  → Updates UserBook.CurrentPages + inserts BookAction (PageUpdate)
  → If pages == totalPages: also sets status=Finished + inserts BookAction (StatusChange)
  → Single SaveChangesAsync()
  → Returns updated UserBookResponse
  → If finished: CelebrationOverlay shown; ProgressPopup closes
```

**Data Flow — Stats Page Load:**
```
User navigates to /stats
  → StatsPage mounts → statsApi.getStats()
  → GET /api/stats
  → StatsService runs all queries scoped to userId:
      - By-status counts from UserBooks
      - 6× period-bucketed Finished StatusChange counts
      - 6× period-bucketed PageUpdate SUM(newValue - oldValue)
      - Unfinished Genre ratio (≥3 Started UserBooks across ≥2 genres threshold)
  → Returns StatsPageResponse
```

**Open Library Flow:**
```
User submits ISBN
  → GET /api/books/{isbn}
  → BookService checks Catalog → hit: return immediately
  → miss: IHttpClientFactory → GET openlibrary.org (3s timeout)
    → success: map to BookResponse
    → timeout/miss: return null → frontend shows empty form (FR-6)
```

### Development Workflow

**Running locally:**
```bash
# Terminal 1 — Backend
cd backend/BookTracker.Api
dotnet user-secrets set "ConnectionStrings__Default" "Host=localhost;Database=booktracker;..."
dotnet user-secrets set "JWT__Secret" "your-secret-here"
dotnet ef database update
dotnet run

# Terminal 2 — Frontend
cd frontend
npm install
npm run dev
```

**Adding a migration:**
```bash
cd backend/BookTracker.Api
dotnet ef migrations add {MigrationName}
dotnet ef database update
```

## Architecture Validation Results

### Coherence Validation ✅

**Decision Compatibility:**
All technology versions are mutually compatible: .NET 10 LTS + EF Core 10 + Npgsql (net10.0 compatible); React 19 + Vite 6 + Tailwind CSS v4 + Radix UI. No version conflicts identified. ASP.NET Core built-in DI is the correct container for the three-tier pattern. System.Text.Json camelCase configured globally — no per-controller override risk.

**Pattern Consistency:**
- Database PascalCase → API camelCase (globally configured) → TypeScript camelCase: the naming chain is explicit and covered by a single global setting in `Program.cs`
- BookAction atomicity rule directly enforces the FR-23 event-log contract — decisions are mutually reinforcing
- Tailwind v4 CSS `@theme` deviation from UX spec's `tailwind.config.js` reference is acknowledged in Starter Template section; no functional impact

**Structure Alignment:**
Project structure in Step 5 (Patterns) and Step 6 (Structure) are fully consistent. FR-to-file mapping confirms no orphaned requirements. Integration points (Vite proxy, CORS policy, JWT bearer injection) align with all architectural decisions.

### Requirements Coverage Validation ✅

**Functional Requirements (FR-1–23):** All covered.

| FR Group | Coverage |
|---|---|
| Auth FR-1–3 | `AuthController`, `AuthService`, `UserRepository`, bcrypt, JWT |
| Catalog FR-4–8 | `BooksController`, `BookService` (Open Library proxy), `BookRepository`, unique constraint |
| Shelf FR-9–11 | `ShelfController`, `UserBookRepository`, `StatusRibbon`, Reader Count via COUNT DISTINCT |
| Lifecycle FR-12–14 | State machine in `ShelfService`, `BookAction` writes per transition |
| Progress FR-15–16 | `UpdatePages` endpoint, auto-finish logic, `JournalPopup`, `CelebrationOverlay` |
| Re-reading FR-17 | `Reread` endpoint, new `UserBook` with MAX(readingNumber)+1 |
| Stats Strip FR-18 | `StatsController.GetStrip`, `StatsService` |
| Stats Page FR-19–23 | `StatsService` event-log queries, composite indexes |

**Non-Functional Requirements:**

| NFR | Coverage |
|---|---|
| Security (§5.1) | bcrypt cost 12; JWT {userId, exp} only; ownership validation in service layer |
| Performance (§5.2) | Composite indexes `IX_BookActions_UserId_Timestamp` + `IX_BookActions_UserId_UserBookId` |
| Code Navigability (§5.3) | Three-tier, all interfaces paired, (type × domain) in ≤3 traversals |
| Local Runnability (§5.4) | `dotnet user-secrets` + `dotnet ef database update` + README workflow |

### Implementation Readiness Validation ✅

All critical decisions documented with technology versions. Naming conventions cover database, API, C#, and TypeScript layers. Structure is specific (named files, not placeholders). All 9 conflict points addressed with concrete rules and examples. BookAction atomicity rule includes a correct/incorrect code example.

### Gap Analysis Results

**Critical Gaps:** None.

**Important Gaps:**

1. **Shelf sort key missing from `UserBook` model.** The UX spec requires Shelf sorted by last activity (most recently touched first). The current `UserBook` model has no field to support this.
   **Resolution:** Add `LastActivityAt` (`datetime`, UTC) to `UserBook`. Set by `ShelfService` on: AddToShelf (creation), UpdateStatus, UpdatePages, Reread. `GetShelf` query: `ORDER BY LastActivityAt DESC`. `UserBookResponse` includes `lastActivityAt`.

**Nice-to-Have Gaps:**

1. JWT token expiry (PRD A-4: 24 hours) should be configurable via `appsettings.json` (`JWT__ExpiryHours: 24`) rather than hardcoded in `Program.cs`.
2. Cover image placeholder — when `Book.CoverImageUrl` is null, `BookCard` renders a warm-toned book silhouette (implementation detail; no architecture change needed).

### Validation Issues Addressed

- **Shelf sort key** resolved by adding `LastActivityAt` to `UserBook` model and updating `ShelfService` assignment rules.
- **JWT expiry** resolved by documenting `JWT__ExpiryHours` as a configurable key in `appsettings.json`.

### Architecture Completeness Checklist

**Requirements Analysis**
- [x] Project context thoroughly analyzed
- [x] Scale and complexity assessed
- [x] Technical constraints identified
- [x] Cross-cutting concerns mapped

**Architectural Decisions**
- [x] Critical decisions documented with versions
- [x] Technology stack fully specified
- [x] Integration patterns defined
- [x] Performance considerations addressed

**Implementation Patterns**
- [x] Naming conventions established
- [x] Structure patterns defined
- [x] Communication patterns specified
- [x] Process patterns documented

**Project Structure**
- [x] Complete directory structure defined
- [x] Component boundaries established
- [x] Integration points mapped
- [x] Requirements to structure mapping complete

### Architecture Readiness Assessment

**Overall Status:** READY FOR IMPLEMENTATION

**Confidence Level:** High — 23 FRs fully mapped, all 4 NFRs addressed, no critical gaps, important gap (sort key) resolved within this validation step.

**Key Strengths:**
- Event-log architecture is consistent end-to-end: FR-23 contract → BookAction atomicity rule → composite indexes → StatsService query contract
- Three-tier layering with interface-first DI means each layer is independently testable and replaceable
- Tailwind v4 + custom components gives full visual control without fighting a component library
- Vite proxy eliminates all CORS friction during development

**Areas for Future Enhancement:**
- Refresh token / httpOnly cookie auth hardening (explicitly deferred, PRD §8.2)
- Reading streak / calendar heatmap (event log already holds the data)
- GitHub Actions CI pipeline (natural v2 addition)

### Implementation Handoff

**AI Agent Guidelines:**
- Follow all architectural decisions exactly as documented
- Use implementation patterns consistently across all components
- `ShelfService` owns ALL state machine logic — nothing transitions `UserBook.Status` outside this class
- Every `UserBook` mutation sets `LastActivityAt = DateTime.UtcNow` in the same `SaveChangesAsync()` call as its `BookAction`
- Refer to this document for all architectural questions before making independent decisions

**First Implementation Stories:**
1. `dotnet new webapi --use-controllers -n BookTracker.Api -o backend` + NuGet packages
2. `npm create vite@latest frontend -- --template react-ts` + Tailwind v4 + Radix setup
3. EF Core `AppDbContext` + domain models (including `UserBook.LastActivityAt`) + initial migration
