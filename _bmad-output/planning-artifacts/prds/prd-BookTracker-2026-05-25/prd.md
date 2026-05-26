---
title: "BookTracker"
status: final
created: 2026-05-25
updated: 2026-05-25
---

# PRD: BookTracker

## 0. Document Purpose

This PRD is for the PM and downstream workflow owners (UX designer, architect, developer agent), plus any human or AI reviewer picking up the project. §3 Glossary terms are the source of truth, §4 uses stable global FR numbering (FR-1 through FR-23), and assumptions remain tagged inline as `[ASSUMPTION: ...]` and indexed in §11.

The primary input was `brief.md` (BookTracker Product Brief, 2026-05-25). This PRD defines the product requirements needed for UX design, architecture, and story breakdown; implementation depth that belongs in downstream technical artifacts sits in `addendum.md` instead of being duplicated here.

---

## 1. Vision

BookTracker is a self-hosted, full-stack web application for individual readers who want a clean, dependency-free way to catalog books by ISBN, track reading progress through a structured lifecycle, and understand their habits through a stats dashboard built on an event log. It is an answer to two unsatisfying alternatives: heavyweight social platforms with noise, lock-in, and opinionated features, or manual spreadsheets with no UX, no computed stats, and no reading-state logic.

It also serves as a BMad Method reference implementation: every shipped feature must work completely, and the artifact trail from brief to PRD to architecture to code must stay legible to a developer or AI agent with no prior context.

What distinguishes BookTracker is how it works. Every meaningful user action — status change, page update — is recorded as a timestamped BookAction with old and new values. Period stats are queries over this log, not precomputed counters. A shared Catalog de-duplicates book metadata while keeping reading records per-user, which keeps the system simple to extend without structural rework.

---

## 2. Target User

### 2.1 Primary Persona

**The individual reader.** A person who reads regularly and wants a clean, self-hosted tracker without social noise or spreadsheet friction. They need to add books quickly, track progress without ceremony, and see honest stats about what they have started, finished, or abandoned. Success for them: the app is easy to use, their reading state always feels trustworthy, and the Shelf plus stats reflect reality without manual bookkeeping.

### 2.2 Secondary Persona

**The BMad Method developer.** A software developer or architect following the BMad Method for the first time and using BookTracker as the reference application. They run the app locally from a clone, inspect the layering, and use it as evidence that structured product development produces navigable, well-reasoned software. Success for them: the app runs, every feature works, and the code tells a coherent story that a PRD agent, developer agent, or human reviewer can pick up and continue.

### 2.3 Jobs To Be Done

- **Functional:** Add a book I just started without friction; know at a glance what I am reading and how far I have progressed; see honest stats about my reading habits without signing up for a social platform.
- **Contextual:** Track multiple books in different states simultaneously; pick up exactly where I left off when I return after days away.
- **Emotional:** Feel like my reading life is organized — without the noise of Goodreads or the friction of a spreadsheet.
- **Professional (secondary user):** Have a complete, runnable reference implementation that demonstrates structured product development from brief to deployed codebase, navigable by a future AI agent with no prior context.

### 2.4 Non-Users (v1)

Users seeking social reading features, cloud sync, mobile-native apps, multi-user household management, or title/author-based search are not served by v1.

### 2.5 Key User Journeys

- **UJ-1. Alex adds a book by ISBN for the first time.**
  - **Persona + context:** Alex, BMad developer, has just cloned and run the app locally. They want to see the full ISBN → Catalog → Shelf flow working end-to-end.
  - **Entry state:** Authenticated, on the Shelf.
  - **Path:** Taps "Add Book" → enters ISBN → system checks shared Catalog (miss) → system queries Open Library (hit) → confirmation form pre-fills title, author, totalPages, coverImageUrl → Alex selects genre from dropdown → confirms → Book added to Catalog and appears on Shelf with status Resting.
  - **Climax:** Book card appears on the Shelf with correct metadata, Resting ribbon, and "Start Reading" action button.
  - **Resolution:** Alex is back on the Shelf with the new card visible.
  - **Edge case:** Open Library returns nothing (or is unreachable) → Alex sees an empty form and manually fills in title, author, totalPages, and genre before submitting.

- **UJ-2. Sam updates reading progress and checks the Reading Journal.**
  - **Persona + context:** Sam, individual reader, has been reading a Started book and wants to log a session's pages.
  - **Entry state:** Authenticated, on the Shelf.
  - **Path:** Locates book card → adjusts page-progress stepper to new value → submits → opens Reading Journal popup → sees the new PageUpdate event at the top of the newest-first list with a timestamp.
  - **Climax:** Journal entry confirms the update was recorded.
  - **Resolution:** Sam closes the popup and returns to the Shelf.
  - **Edge case:** Sam enters a page value higher than totalPages — system rejects with a validation error and the stepper reverts.

- **UJ-3. Alex finishes a book and starts re-reading it.**
  - **Persona + context:** Alex has a Finished book and wants to verify the re-read flow preserves history.
  - **Entry state:** Authenticated, on the Shelf, viewing a Finished book card.
  - **Path:** Sees "Read Again" action button → clicks → new UserBook record created (readingNumber = 2, status = Resting) → Shelf now shows the book with Resting ribbon.
  - **Climax:** Book card resets to Resting. The prior reading's Journal entries are preserved on the previous UserBook record.
  - **Resolution:** Alex can start the second reading independently of the first.

- **UJ-4. Sam reviews their reading stats for the quarter.**
  - **Persona + context:** Sam wants to know how many books they finished and how many pages they read in the last 90 days.
  - **Entry state:** Authenticated, on the Shelf (Stats Strip visible at the top).
  - **Path:** Glances at Stats Strip for current-month summary → navigates to Stats Page → reads by-status counts → finds the 90-day books-completed row → reads "Your Unfinished Genre" insight.
  - **Climax:** Sam sees their Unfinished Genre and recognizes a pattern in their abandoned reads.
  - **Resolution:** Sam returns to the Shelf. No input was required; the data was already there.

---

## 3. Glossary

- **Book** — A shared catalog entry uniquely identified by ISBN. Holds title, author, totalPages, genre, and coverImageUrl. Created once; available to all Users. No Book is duplicated; a second Add attempt with the same ISBN surfaces the existing record.
- **User** — A registered account identified by email. Holds firstName, lastName, dateOfBirth, and passwordHash. Reading records are isolated per User.
- **UserBook** — A personal reading record linking one User to one Book. Holds status, currentPages, readingNumber, startedAt, and finishedAt. A User may have multiple UserBook records for the same Book (one per reading attempt), each with a distinct readingNumber.
- **Reading Status** — The lifecycle state of a UserBook. Exactly one of: **Resting**, **Started**, **Finished**, **Abandoned**.
- **BookAction** — An immutable event-log entry. Holds userId, userBookId, actionType (StatusChange or PageUpdate), oldValue, newValue, and timestamp. Every Reading Status transition and every page update produces exactly one BookAction.
- **Reading Journal** — All BookActions for this Book, across all readingNumbers for the User+Book pair, surfaced to the user as a popup timeline ordered newest first; each entry shows its readingNumber for context.
- **Shelf** — The authenticated User's personal view of all their UserBooks; the default screen after login.
- **Stats Strip** — A summary bar persistently visible at the top of the Shelf. Shows: total UserBooks, Finished count, Started count, pages read this calendar month.
- **Stats Page** — A dedicated screen showing full reading analytics: counts by Reading Status, books completed per period, pages read per period, and the Unfinished Genre insight.
- **Reader Count** — The number of distinct Users who have at least one UserBook for a given Book. Displayed on each book card as "👥 N readers."
- **Unfinished Genre** — The genre with the highest ratio of Started to (Finished + Abandoned) UserBooks for the current User. Shown on the Stats Page when sufficient data exists.
- **readingNumber** — A positive integer on UserBook tracking which reading attempt this represents, scoped to the (User, Book) pair. First read = 1; each "Read Again" increments by 1.
- **ISBN** — International Standard Book Number; the primary lookup key for Book catalog entries. Both ISBN-10 and ISBN-13 are accepted as entered. [ASSUMPTION: no normalization or cross-format matching between ISBN-10 and ISBN-13 in v1.]

---

## 4. Features

### 4.1 User Authentication

**Description:** Registration and login establish the authenticated state required for every core user journey. On success, the User receives a JWT bearer token and lands on the Shelf. Realizes UJ-1 through UJ-4.

**Functional Requirements:**

#### FR-1: User Registration

A visitor can register by submitting all required fields (email, password, firstName, lastName, dateOfBirth) in a single registration form.

**Consequences (testable):**
- System creates a User record with a bcrypt passwordHash (minimum cost factor 12) and returns a valid JWT bearer token.
- System returns HTTP 409 if the email is already registered.
- System returns HTTP 400 if any required field is missing or if email format is invalid.
- Plaintext password is never persisted or returned in any API response.

#### FR-2: User Login

A registered User can authenticate with email and password and receive a JWT bearer token.

**Consequences (testable):**
- System returns a JWT bearer token valid for [ASSUMPTION: 24 hours] on correct credentials.
- System returns HTTP 401 on incorrect credentials without distinguishing which field was wrong.

#### FR-3: Protected Route Enforcement

All API endpoints except `/api/auth/register` and `/api/auth/login` require a valid JWT bearer token.

**Consequences (testable):**
- Requests without a token return HTTP 401.
- Requests with an expired token return HTTP 401.
- Token payload contains userId; all data operations are scoped to that userId.

---

### 4.2 Book Search and Catalog

**Description:** Users add Books by ISBN. The system checks the shared Catalog first, then uses Open Library to prefill metadata or falls back to manual entry before saving the Book. Realizes UJ-1.

**Functional Requirements:**

#### FR-4: ISBN Lookup Against Shared Catalog

An authenticated User submits an ISBN; if a Book with that ISBN exists in the Catalog, it is returned immediately without querying Open Library.

**Consequences (testable):**
- Response returns the existing Book record.
- Lookup strips leading/trailing whitespace and treats uppercase or lowercase `x` identically in ISBN-10 check digits.

#### FR-5: Open Library Prefill

If the ISBN is not in the Catalog, the system queries Open Library and prefills title, author, totalPages, and coverImageUrl in a confirmation form.

**Consequences (testable):**
- Pre-filled form is presented to the User for review and optional editing.
- Genre is never prefilled; User must always select it explicitly.
- If Open Library returns no match or is unreachable, an empty editable form is presented with no error blocking submission. [Decision: confirmed by Alexei, 2026-05-25 — manual entry when Open Library unavailable.] [ASSUMPTION: A-2 — query is proxied server-side, not made from the frontend directly.]

#### FR-6: Manual Book Entry

When Open Library returns no result or is unreachable, the User can fill in title, author, totalPages, and genre to create the Book.

**Consequences (testable):**
- All four fields are required; form submission is blocked until all are present and valid.
- totalPages must be a positive integer.

#### FR-7: Genre Selection

The User selects genre from a predefined list presented as a dropdown. [ASSUMPTION: Fiction, Non-Fiction, Mystery, Science Fiction, Fantasy, Romance, Biography & Memoir, History, Self-Help, Other.]

**Consequences (testable):**
- Genre field is a dropdown constrained to the predefined list; free-text entry is not permitted.
- Genre is required; a Book cannot be submitted without a genre selection.

#### FR-8: Shared Catalog Deduplication

Each ISBN maps to exactly one Book in the Catalog regardless of how many Users add it.

**Consequences (testable):**
- A second User adding the same ISBN receives the existing Book without seeing an error.
- Concurrent adds for the same ISBN resolve to exactly one Catalog entry (enforced via unique constraint or upsert).

---

### 4.3 Personal Shelf

**Description:** The Shelf is the authenticated User's home screen. It shows current UserBooks in a scrollable card list with the Stats Strip above it. Realizes UJ-2 and UJ-4.

**Functional Requirements:**

#### FR-9: Shelf Display

An authenticated User's Shelf shows all their UserBooks as book cards. [ASSUMPTION: A-6 — only the most-recent UserBook per Book (highest readingNumber) is shown; prior reads are accessible via the Reading Journal.]

**Consequences (testable):**
- Each card displays: cover image (or placeholder if no coverImageUrl [ASSUMPTION: A-13]), title, author, color-coded Reading Status ribbon, Reader Count.
- Shelf is a scrollable flat list with no sorting, filtering, or pagination in v1. [ASSUMPTION: A-5]
- Shelf reflects the current state of all UserBooks on page load; real-time push is not required.

#### FR-10: Reader Count Display

Each book card displays "👥 N readers" where N = COUNT(DISTINCT userId) across all UserBooks for that Book.

**Consequences (testable):**
- Count includes the current User.
- Count reflects any new readers within one page refresh.

#### FR-11: Reading Status Color Ribbons

Each book card displays a color-coded ribbon corresponding to the UserBook's current Reading Status.

**Consequences (testable):**
- Four visually distinct colors, one per Reading Status (Resting, Started, Finished, Abandoned).
- [ASSUMPTION: A-15 — specific color mapping is deferred to the UX design artifact; PRD requires only that the four states are visually distinct.]

---

### 4.4 Reading Lifecycle

**Description:** Every UserBook moves through a four-state lifecycle. The card shows the single valid explicit action for the current Reading Status, and Started UserBooks can also reach Finished through page-progress updates. Every transition is recorded as a BookAction for auditability and stats. Realizes UJ-3.

**Functional Requirements:**

#### FR-12: Initial Reading Status

When a User adds a Book to their Shelf (creates a UserBook), the initial Reading Status is Resting.

**Consequences (testable):**
- New UserBook is persisted with status = Resting, currentPages = 0, startedAt = null, finishedAt = null.

#### FR-13: Context-Aware Action Button

The action button displayed on each book card reflects only valid transitions from the current Reading Status.

**Consequences (testable):**
- Resting: one button — "Start Reading." Transition sets status = Started, startedAt = server time.
- Started: one button — "Abandon." Transition sets status = Abandoned, finishedAt = server time. Finishing is triggered automatically by FR-15 when currentPages = totalPages; there is no explicit "Mark Finished" button.
- Finished: one button — "Read Again." Triggers FR-17.
- Abandoned: one button — "Read Again." Triggers FR-17.

#### FR-14: Status Transition Event

Every Reading Status transition produces an immutable BookAction.

**Consequences (testable):**
- BookAction created with: actionType = StatusChange, oldValue = prior status string, newValue = new status string, timestamp = server time, userId = authenticated user, userBookId = affected UserBook.
- No update or delete endpoint exists for BookAction records.

---

### 4.5 Page Progress

**Description:** Started UserBooks accept page-progress updates through a numeric stepper. Each update appends a BookAction, and the Reading Journal exposes the full event history for the User+Book pair. Realizes UJ-2.

**Functional Requirements:**

#### FR-15: Page Progress Update

On a Started UserBook, an authenticated User can update currentPages via a numeric stepper input.

**Consequences (testable):**
- Input accepts only integers in the range [0, totalPages]; values outside this range are rejected with a validation error and the field reverts.
- Successful update persists the new currentPages on the UserBook.
- A BookAction is created: actionType = PageUpdate, oldValue = previous currentPages (as string), newValue = new currentPages (as string), timestamp = server time.
- If the submitted currentPages value equals totalPages, the system additionally transitions status to Finished and sets finishedAt = server time, creating a StatusChange BookAction with oldValue = Started, newValue = Finished at the same timestamp.

**Out of Scope:**
- Page progress updates are not available on Resting, Finished, or Abandoned UserBooks.

#### FR-16: Reading Journal Popup

A User can open the Reading Journal popup for any UserBook to view the full BookAction history for that User+Book pair.

**Consequences (testable):**
- Journal displays all BookActions for the Book across all UserBooks for that User+Book pair, ordered by timestamp descending (newest first).
- Each entry shows: readingNumber, human-readable actionType label, oldValue, newValue, formatted timestamp.
- Journal is read-only; no editing or deletion of entries.

---

### 4.6 Re-reading

**Description:** "Read Again" creates a new UserBook instead of resetting the existing one, preserving prior reading history. Realizes UJ-3.

**Functional Requirements:**

#### FR-17: Read Again Action

Activating "Read Again" on a Finished or Abandoned UserBook creates a new UserBook record for the same User and Book.

**Consequences (testable):**
- New UserBook: status = Resting, currentPages = 0, readingNumber = MAX(existing readingNumber for this User + Book) + 1, startedAt = null, finishedAt = null.
- Prior UserBook record and all its BookActions are unchanged and remain accessible via the Reading Journal.
- The new UserBook becomes the Shelf card for that Book (replaces the prior card per FR-9 assumption).

---

### 4.7 Stats Strip

**Description:** A persistent summary bar at the top of the Shelf gives at-a-glance reading totals. Realizes UJ-4 (first glance).

**Functional Requirements:**

#### FR-18: Stats Strip Display

The Stats Strip displays four values for the authenticated User: total UserBooks, Finished count, Started count, pages read this calendar month.

**Consequences (testable):**
- Counts cover all UserBook records for the User across all readingNumbers.
- "Pages read this calendar month" = SUM(newValue − oldValue) for all PageUpdate BookActions where timestamp falls within the current calendar month and (newValue − oldValue) > 0. [ASSUMPTION: calendar month, not rolling 30 days.]
- Strip renders on page load without additional user interaction.

---

### 4.8 Stats Page

**Description:** The Stats Page provides full reading analytics derived directly from BookAction queries at request time. Realizes UJ-4 (deep analysis).

**Functional Requirements:**

#### FR-19: By-Status Counts

The Stats Page displays count of UserBooks by each Reading Status plus a total.

**Consequences (testable):**
- Counts: total, Resting, Started, Finished, Abandoned — all matching current UserBook records for the User.

#### FR-20: Period-Bucketed Completion Counts

The Stats Page shows books completed (status transitioned to Finished) across fixed rolling windows: 7, 30, 90, 180, 270, and 365 days.

**Consequences (testable):**
- "Completed in period" = COUNT of StatusChange BookActions where newValue = "Finished" and timestamp ≥ (now − period in days).
- Windows are rolling from the current moment, not calendar-aligned.

#### FR-21: Period-Bucketed Pages Read

The Stats Page shows pages read across the same six rolling windows as FR-20.

**Consequences (testable):**
- "Pages read in period" = SUM(newValue − oldValue) for PageUpdate BookActions where timestamp ≥ (now − period in days) and (newValue − oldValue) > 0.

#### FR-22: Unfinished Genre Insight

The Stats Page shows the Unfinished Genre insight when the User has sufficient data; otherwise shows a placeholder.

**Consequences (testable):**
- Unfinished Genre = genre with highest ratio of Started UserBooks to (Finished + Abandoned) UserBooks across all readingNumbers for the User.
- Insight is displayed only when User has ≥ 3 UserBooks with status Started across ≥ 2 distinct genres. [ASSUMPTION: A-14]
- Below threshold: insight area shows "Not enough data yet" placeholder.

#### FR-23: Event-Log Query Contract

All Stats Page and Stats Strip figures are computed from BookAction queries at request time; no precomputed counters or nightly aggregation jobs exist.

**Consequences (testable):**
- No stats column on User or UserBook is used as a source of truth for any displayed figure.
- Inserting a new BookAction directly into the database is reflected in stats on the next page load.

---

## 5. Cross-Cutting NFRs

### 5.1 Security

- Passwords stored as bcrypt hashes with minimum cost factor 12; plaintext password never persisted or returned.
- JWT token payload contains only userId and expiry; no sensitive user data in the token.
- All API endpoints (except register and login) validate that the authenticated userId owns or has rights to the requested resource before returning or mutating data.
- [ASSUMPTION: JWT tokens stored in localStorage on the frontend for simplicity as a demo app; httpOnly cookie storage and refresh token rotation are post-v1 concerns.]

### 5.2 Performance

- Stats Page queries complete in < 2 seconds for a User with up to 500 BookAction events.
- Shelf load completes in < 1 second for a User with up to 100 UserBooks.
- No background jobs, scheduled tasks, or caches are required to meet these targets at demo scale.

### 5.3 Code Navigability

- Backend folder and namespace structure must allow any class to be located by type (Controller, Service, Repository) and domain (Auth, Book, UserBook, Stats) within three file-tree traversals.
- Every Service and Repository must have a paired interface and implementation; no concrete class is injected directly.

### 5.4 Local Runnability

- Application starts from `git clone` plus two environment values: a PostgreSQL connection string and a JWT secret.
- README covers explicit steps to configure and run both backend and frontend locally.
- No cloud account, external service account, or additional tooling beyond .NET SDK, Node.js, and a local PostgreSQL instance is required.

---

## 6. Information Architecture

*Screen-level. Layout and visual design are deferred to the UX design artifact.*

| Route | Surface | Auth Required |
|---|---|---|
| `/register` | Registration form | No |
| `/login` | Login form | No |
| `/shelf` | Shelf (default post-login) | Yes |
| `/stats` | Stats Page | Yes |

**Navigation (when authenticated):** Top navigation bar with links to Shelf and Stats Page.

**Shelf layout:** Stats Strip (persistent top bar) → scrollable book card list → "Add Book" entry point (button or FAB).

**Add Book flow:** [ASSUMPTION: modal overlay on the Shelf.] Steps: ISBN input → system lookup → confirmation/edit form with genre dropdown → submit.

**Book card:** cover image (or placeholder), title, author, Reading Status ribbon, Reader Count, context-aware action button, page-progress stepper (Started UserBooks only), Reading Journal trigger (all UserBooks).

**Reading Journal:** modal popup, read-only, event list for the User+Book pair across all readingNumbers, ordered newest first.

---

## 7. Non-Goals (Explicit)

- **No profile edit page.** Name and date of birth are set once at registration and cannot be changed in v1.
- **No password confirmation field and no show/hide toggle.** Single password input only.
- **No Shelf pagination.** Scrollable flat list only.
- **No title or author search.** ISBN is the sole book-entry path.
- **No reading streak, calendar heatmap, or activity feed.**
- **No email notifications of any kind.**
- **No social features beyond Reader Count.** No follows, comments, or recommendations.
- **No cloud deployment.** Local run is the v1 delivery bar.
- **No mobile-native design.** Responsive layout acceptable; native app out of scope.
- **No custom date-range picker.** Fixed period buckets only (7 / 30 / 90 / 180 / 270 / 365 days).

---

## 8. MVP Scope

### 8.1 In Scope

- User registration and login with JWT bearer tokens
- ISBN-based lookup → Open Library prefill → manual entry fallback → shared Catalog
- Personal Shelf with color-coded Reading Status ribbons and Reader Count
- Four-state reading lifecycle with context-aware action button
- Page progress stepper with BookAction event logging
- Reading Journal popup (all BookActions for the User+Book pair across readingNumbers)
- Re-read support: new UserBook per reading attempt, prior record preserved
- Stats Strip (persistent on Shelf)
- Full Stats Page: by-status counts, period-bucketed completions and pages, Unfinished Genre insight
- Public GitHub repository with local-setup README

### 8.2 Out of Scope for MVP

- Profile edit page — deferred; no target version
- Reading streak / calendar heatmap — deferred to v2 (event log already holds the data)
- Ordered reading queue for Resting books on the Shelf — deferred to v2
- Genre filter tabs on Shelf — deferred to v2
- CSV export — deferred to v2
- Reading-pace estimator — deferred to v2
- Cloud deployment — deferred to v2
- Refresh token / httpOnly cookie auth hardening — deferred post-v1 [NOTE FOR PM: revisit before any shared or public deployment]

---

## 9. Success Metrics

**Primary**

- **SM-1:** Application runs from `git clone` + two env values with no additional setup steps required. Target: binary pass. Supports validation of FR-1 through FR-23 and directly validates §5.4 (Local Runnability NFR).
- **SM-2:** All core flows complete end-to-end without error: register → login → add book by ISBN (Open Library hit) → add book manually (Open Library miss) → change Reading Status → update page progress → view Stats Page. Target: 0 broken core flows. Validates FR-1 through FR-23 holistically.
- **SM-3:** Stats Page period-bucketed figures match raw BookAction data for three hand-verified scenarios (one per major period bucket). Target: 100% match. Validates FR-20, FR-21, FR-23.

**Secondary**

- **SM-4:** Reader Count on book cards reflects the correct distinct-user count within one page refresh. Target: 100% correct in verification scenarios. Validates FR-10.
- **SM-5:** Re-reading a Finished or Abandoned book creates a new UserBook (readingNumber incremented) without altering the prior record or its BookActions. Target: binary pass. Validates FR-17.
- **SM-6:** Unfinished Genre insight appears with the correct genre when the threshold is met, and the placeholder appears when it is not. Target: binary pass. Validates FR-22.

**Counter-metrics (do not optimize)**

- **SM-C1:** Feature count. Every feature that ships must work completely. Completeness over breadth. Guards delivery quality across FR-1 through FR-23.
- **SM-C2:** PRD section count. Sections exist because they are decision-ready. An open question is preferable to false certainty. Protects clarity around FR-1 through FR-23 and their assumptions.

---

## 10. Open Questions

1. **OQ-1: Genre list** — Is the assumed genre list (A-7) acceptable, or should it be replaced? Confirm or provide the canonical list before architecture begins.
2. **OQ-2: totalPages upper bound** — Should the system enforce a maximum value for totalPages (e.g., reject > 10,000) to guard against data-entry errors?
3. **OQ-3: Open Library timeout** — What is an acceptable wait time before falling back to the manual-entry form? (Suggest ≤ 3 seconds server-side.)

---

## 11. Assumptions Index

| ID | Assumption | Location |
|---|---|---|
| A-1 | ISBN-10 and ISBN-13 accepted as-entered; no normalization or cross-format matching in v1. | §3 Glossary |
| A-2 | Open Library API queried server-side (backend proxies the call, not frontend direct). | §4.2 FR-5 |
| A-3 | JWT tokens stored in localStorage on the frontend for simplicity as a demo app. | §5.1 Security |
| A-4 | JWT token validity period is 24 hours. | §4.1 FR-2 |
| A-5 | Shelf is a scrollable flat list; no sorting, filtering, or pagination in v1. | §4.3 FR-9 |
| A-6 | Only the most-recent UserBook per Book (highest readingNumber) is shown on the Shelf. | §4.3 FR-9, §4.6 FR-17 |
| A-7 | Predefined genre list: Fiction, Non-Fiction, Mystery, Science Fiction, Fantasy, Romance, Biography & Memoir, History, Self-Help, Other. | §4.2 FR-7 |
| A-8 | Started state shows one button — "Abandon"; finishing is triggered automatically when currentPages reaches totalPages. | §4.4 FR-13, §4.5 FR-15 |
| A-9 | Reading Journal shows all BookActions for the User+Book pair across all readingNumbers, ordered timestamp descending. | §4.5 FR-16 |
| A-10 | Single password input; no confirmation field and no show/hide toggle. | §4.1 FR-1 |
| A-11 | "Pages read this month" on the Stats Strip uses the current calendar month, not a rolling 30-day window. | §4.7 FR-18 |
| A-12 | Add Book flow implemented as a modal overlay on the Shelf. | §6 Information Architecture |
| A-13 | Cover image placeholder shown when no coverImageUrl is available. | §4.3 FR-9 |
| A-14 | Unfinished Genre insight threshold: ≥ 3 UserBooks in Started status across ≥ 2 distinct genres. | §4.8 FR-22 |
| A-15 | Color mapping for Reading Status ribbons is deferred to the UX design artifact; PRD requires only visual distinction between the four states. | §4.3 FR-11 |


