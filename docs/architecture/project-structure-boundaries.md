# Project Structure & Boundaries

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
