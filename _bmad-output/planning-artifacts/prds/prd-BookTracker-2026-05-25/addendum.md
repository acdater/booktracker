---
title: "BookTracker — PRD Addendum"
created: 2026-05-25
updated: 2026-05-25
---

# PRD Addendum: BookTracker

*This companion file holds technical context that belongs in downstream artifacts rather than the PRD body. Read it alongside `prd.md`; it does not add v1 requirements.*

---

## A. Architecture Notes (from Brief)

These notes are sourced from the product brief and preserved as starting points for the architecture artifact. They are not PRD requirements and should be validated, refined, or replaced during architecture work.

### Backend (.NET)

- **Starting point:** Full MVC controllers rather than Minimal APIs.
- **Starting point:** Three-tier layering: `Controllers` → `Services` (interface + implementation) → `Repositories` (interface + implementation).
- **Starting point:** PostgreSQL via Entity Framework Core.
- **Starting point:** JWT bearer middleware.
- **Codebase organization outcome:** Keep each class locatable by type and domain. PRD §5.3 defines the navigability outcome; the architect can choose the exact namespace and folder shape.

### Frontend (React)

- **Starting point:** Vite + TypeScript.
- **Starting point:** REST communication with the backend API.
- **Candidate default:** React Context. [NOTE: The brief flags this as "to confirm at architecture stage" — a lightweight library (e.g., Zustand) may be preferred if Context proves cumbersome for auth + Shelf state.]

### Key Domain Entities

| Entity | Key Fields |
|---|---|
| `Book` | ISBN, title, author, totalPages, genre, coverImageUrl |
| `User` | email, passwordHash, firstName, lastName, dateOfBirth |
| `UserBook` | userId, bookId, status, currentPages, readingNumber, startedAt, finishedAt |
| `BookAction` | userId, userBookId, actionType, oldValue, newValue, timestamp |

---

## B. Options Considered

### B.1 Open Library Fallback: Error-Only vs. Manual Entry

**Decision:** Manual entry (all fields editable) when Open Library is unavailable or returns no result.

**Rejected alternative:** Error-only fallback ("Book not found — please try a different ISBN"). Rejected because it blocks legitimate additions for books not in Open Library's catalog.

**Confirmed by:** Alexei, 2026-05-25.

### B.2 Stats Computation: Event-Log Queries vs. Precomputed Counters

**Decision:** All stats derived from BookAction event-log queries at request time.

**Rationale:** Keeps the schema simple, avoids counter drift, and leaves analytics extensible without schema changes. The current performance target (< 2s for ≤ 500 events) does not require precomputation.

**Deferred:** If data volume grows beyond demo scope, the architect can add projections or materialized views without changing the event-log model.

### B.3 Shared Catalog vs. Per-User Book Records

**Decision:** Book metadata lives once in a shared Catalog (keyed by ISBN); UserBook holds per-user reading state.

**Rationale:** Avoids metadata duplication, makes Reader Count a simple COUNT query, and keeps shared catalog data separate from per-user reading state.

---

## C. Vision Extensions (Post-v1 Context)

*Roadmap context from the brief. These items are not part of v1 scope or commitment.*

Natural extensions supported by the current product direction:
- Reading streak and calendar heatmap (BookAction log already holds the data)
- Ordered reading queue for Resting books on the Shelf
- Genre filter tabs on the Shelf
- CSV export of reading history
- Reading-pace estimator (pages per day derived from BookAction timestamps)
