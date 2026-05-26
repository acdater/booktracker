# Epic 4: Reading Analytics

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
