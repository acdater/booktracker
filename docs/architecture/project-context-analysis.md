# Project Context Analysis

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
