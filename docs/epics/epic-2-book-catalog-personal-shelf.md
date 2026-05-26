# Epic 2: Book Catalog & Personal Shelf

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
