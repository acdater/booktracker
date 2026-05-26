# Implementation Patterns & Consistency Rules

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
