---
stepsCompleted: [1, 2]
inputDocuments: []
session_topic: 'Fullstack .NET + React Book-Keeping Application'
session_goals: 'Design a simple, working application that demonstrates the BMAD Method with core features: book catalog management, reading progress tracking, user statistics, and simple authorization'
selected_approach: 'ai-recommended'
techniques_used: ['Question Storming', 'SCAMPER Method', 'Reversal Inversion']
ideas_generated: []
context_file: ''
---

# Brainstorming Session Results

**Facilitator:** Mary (Business Analyst)
**Date:** 2026-05-25 14:24

## Session Overview

**Topic:** Fullstack .NET + React Book-Keeping Application

**Goals:** Design a simple, working application that demonstrates the BMAD Method — with core features including book identification & catalog management, reading progress tracking per user, genre-based statistics, and simple JWT/user-based authorization.

### Session Setup

**Core Features Identified:**
- Book identification by unique code (ISBN or custom); if new → fill in author, title, pages, genre
- User selects a book they are currently reading and logs pages-read progress
- User statistics: books read count, remaining pages, genre preferences (ranked or % breakdown)
- Simple authorization to separate users and scope data by user ID

**BMAD Goal:** The application should be a working demonstration of the full BMAD-Method lifecycle — from brainstorming through to a runnable app with functional core features.

### Technique Selection

**Approach:** AI-Recommended Techniques
**Analysis Context:** Fullstack .NET + React Book-Keeping Application with focus on BMAD Method showcase

**Recommended Techniques:**

- **Question Storming:** Frame the right design questions before generating solutions — ensures we solve the right problem
- **SCAMPER Method:** Systematically stress-test every feature through 7 lenses (Substitute, Combine, Adapt, Modify, Put to other uses, Eliminate, Reverse)
- **Reversal Inversion:** Flip the problem to identify non-negotiable MVP features vs. scope creep

**AI Rationale:** Topic is concrete and bounded; primary risk is building the wrong things. Define → Expand → Prioritize sequence is optimal.

---

## Technique 1 Results: Question Storming

### Key Design Decisions Made

**[Domain #1]: The Reading State Machine**
*Concept:* Books have 4 states — Resting → Started → Finished or Abandoned; finished/abandoned books can be re-started. UI shows a context-aware action button that reflects the current state.
*Novelty:* State-driven UX — the button always tells you exactly what your next action is.

**[Domain #2]: Shared Book Catalog, Personal Reading Records**
*Concept:* ISBN uniquely identifies a book in a shared catalog. Each user has their own UserBook record referencing that shared book — two users can be at 10% and 66% independently.
*Novelty:* Clean separation between "what the book is" (shared) and "my relationship with the book" (personal).

**[Domain #3]: JWT Auth with Rich Profile**
*Concept:* Email + password login, JWT/Bearer tokens. Profile: first name, last name, Date of Birth (not age — calculated dynamically).
*Novelty:* DoB stays accurate over time; age is derived, not stored.

**[Domain #4]: Multi-Dimensional Stats via Action Log**
*Concept:* Every meaningful action (StatusChanged, PagesUpdated with old+new value) is recorded as a timestamped BookAction row. Stats = SQL queries over this log filtered by date range.
*Novelty:* Stats are a view over history. "Books finished last 30 days" = query. No nightly jobs, no denormalization.

**[Domain #5]: Minimal Popup-Driven UI**
*Concept:* Search page (ISBN entry) → Bookshelf page (user's books) → Stats page/widget. Add book via popup, update reading progress via popup.
*Novelty:* Popup pattern keeps the app fast and focused — no full page navigations for common actions.

**[Architecture #6]: Re-Read as New Record with Sequence**
*Concept:* Each re-start creates a new UserBook record with incrementing readingNumber (1st, 2nd read…). UI shows "Reading #2" badge.
*Novelty:* Full history preserved — user can see they read it once 3 years ago and are reading it again now.

**[Architecture #7]: Pages Delta Stored in Action**
*Concept:* PagesUpdated action stores both oldValue and newValue. Delta = newValue - oldValue, trivially summable by date range for "pages read this month."
*Novelty:* No messy consecutive-row calculations at query time — delta is always explicit.

**[Architecture #8]: Predefined Genre Enum**
*Concept:* Fixed server-side enum (Fiction, Non-Fiction, Sci-Fi, Fantasy, Biography, History, Self-Help, etc.) exposed as dropdown.
*Novelty:* Clean filtering, consistent stats, no data rot from free-text inconsistency.

**[Integration #9]: Open Library Pre-fill**
*Concept:* When user enters an unknown ISBN, app calls Open Library API to pre-fill title, author, page count, and cover image URL. User confirms, picks genre.
*Novelty:* Book entry becomes a single confirm click + free cover art for the bookshelf.

**[UX #10]: Reading Journal / Activity Feed**
*Concept:* Book detail popup renders the action log chronologically — "May 1: Started reading", "May 3: Read to page 89", "May 25: Finished!"
*Novelty:* Zero extra work (it's just the action log rendered), makes the app feel alive and personal.

## Technique 2 Results: SCAMPER Method

### S — Substitute
*Decision:* Keep numeric page input with stepper arrows (up/down). No substitution needed — simple and clear.

### C — Combine ✅ All adopted as MVP
**[UX #11]:** Search IS the Add flow — inline result, one "Add to shelf" button, no separate popup
**[UX #12]:** Stats strip always visible on Bookshelf — 📚 total | ✅ finished | 📖 reading | 📄 pages this month
**[UX #13]:** Cover art + coloured status ribbon on bookshelf cards — green/blue/grey/red at a glance
**[UX #14]:** Update progress popup includes Reading Journal history below the input
**[UX #15]:** Registration form includes profile fields (first name, last name, DoB) in one single step

### A — Adapt (all to Future Backlog)
**[Backlog #A1]:** Progress bar under book cover card (Goodreads-style)
**[Backlog #A2]:** "Recently Active" row at top of bookshelf (Spotify-style)
**[Backlog #A3]:** Reading streak counter from action log timestamps
**[Backlog #A4]:** Ordered "Resting" queue with drag-reorder + "Next up 👆" badge
**[Backlog #A5]:** Calendar heatmap of reading activity (GitHub contribution graph-style)

### M — Modify
**[UX #16] MVP:** Smart Add popup — if Open Library returns complete data, show cover + pre-filled fields + genre dropdown + single "Add to shelf" button.
**[Backlog #M1]:** Title/author search fallback alongside ISBN
**[Backlog #M2]:** "At your current pace, finish in ~X days" reading estimate
**[Backlog #M3]:** Collapsed bookshelf sections by status
**[Backlog #M4]:** Custom date range picker for stats

### P — Put to Other Uses
**[Feature #17] MVP:** `👥 X readers` count on each book card — COUNT of UserBook records per ISBN.
**[Backlog #P1]:** CSV export of personal reading history
**[Backlog #P2]:** Genre-based "find more like this" recommendation prompt
**[Backlog #P3]:** "You haven't read in 7 days" email nudge
**[Backlog #P4]:** Genre filter/tabs on bookshelf

### E — Eliminate
**[Cut #1]:** ~~Profile edit page~~ — name + DoB set once at registration
**[Cut #2]:** ~~Password confirmation field~~ — single input with show/hide toggle
**[Cut #3]:** ~~Pagination~~ — simple scroll on bookshelf

### R — Reverse
**[Feature #18] MVP:** "Your unfinished genre" stat — genre with highest Started+Abandoned to Finished ratio. Nudges user to finish what they started.
**[Backlog #R1]:** "X pages waiting on your shelf" forward-looking stat
**[Backlog #R2]:** Progress-dominant book card layout
**[Backlog #R3]:** Browse catalog before registering

## Technique 3 Results: Reversal Inversion

### Worst App → MVP Requirements

| Worst Version | ✅ Required Feature |
|---|---|
| Manual full book entry, form clears on typo | Open Library pre-fill + field validation |
| Unsorted wall of text, no covers | Bookshelf with cover art + coloured status ribbons, sorted by last activity |
| Invisible action button | Clear, labelled context-aware button always visible |
| Raw DB row counts in stats | Human-readable labels, visual hierarchy |
| Log out after every action | JWT with 30-day expiry, persistent session |
| Free-text genre with duplicates | Predefined genre enum, no free text |

---

## Idea Organization and Prioritization

### Theme 1: Core Data Architecture
- Shared ISBN catalog + personal UserBook records
- 4-state reading machine: Resting → Started → Finished/Abandoned → re-startable
- Re-read creates new UserBook record with readingNumber sequence
- Action Log (StatusChanged + PagesUpdated with old+new value) as stats engine
- Predefined genre enum

### Theme 2: Smart Book Entry
- ISBN search IS the add flow — inline result, no separate popup
- Open Library pre-fill — title, author, pages, cover image
- Smart Add popup — one confirm click + genre pick
- 👥 X readers count on every book card

### Theme 3: Reading Progress UX
- Context-aware action button — always tells you what to do next
- Numeric page input with stepper arrows
- Coloured status ribbon on cover art (green/blue/grey/red)
- Update progress popup includes Reading Journal history

### Theme 4: Stats & Insights
- Stats strip always visible on Bookshelf
- Full Stats page — by status, pages read, books per period (week/month/3-6-9-12 months)
- "Your unfinished genre" — gentle nudge to finish what you started
- Date-range filtering via action log queries

### Theme 5: Auth & Onboarding
- JWT/Bearer token auth — email + password with show/hide toggle
- Single registration form — email, password, first name, last name, DoB
- No profile edit page

### Scope Cuts
- ~~Profile edit page~~
- ~~Password confirmation field~~
- ~~Pagination on bookshelf~~

### Future Backlog
- Goodreads-style progress bar, Spotify "Recently Active" row
- Reading streak counter, calendar heatmap
- Drag-reorder "Resting" shelf queue
- CSV export of reading history
- Genre filter tabs on bookshelf
- "X pages waiting on your shelf" forward stat
- Browse catalog before registering
- Reading pace estimate ("finish in ~X days")
- Custom date range picker for stats

---

## Session Summary

**Total Ideas Generated:** ~50 across 3 techniques
**MVP Features Confirmed:** 18
**Future Backlog Items:** 13
**Clean Cuts:** 3

**Key Breakthrough:** The Action Log architecture — recording every status change and page update as a timestamped event — serves triple duty as the stats engine, reading journal, and audit trail. Zero extra infrastructure, maximum insight.

**Next Step:** Product Brief creation using `bmad-product-brief` skill.




