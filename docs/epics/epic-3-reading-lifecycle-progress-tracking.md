# Epic 3: Reading Lifecycle & Progress Tracking

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
