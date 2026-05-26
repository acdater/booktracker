# Core Architectural Decisions

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
