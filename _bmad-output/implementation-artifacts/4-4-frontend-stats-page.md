# Story 4.4: Frontend Stats Page

**Epic:** 4 — Reading Analytics  
**Story ID:** 4.4  
**Story Key:** 4-4-frontend-stats-page  
**Status:** review

---

## User Story

As a **reader**,  
I want to visit the Stats Page and see my full reading analytics in a clear, readable layout,  
So that I can understand my reading habits across different time windows.

---

## Acceptance Criteria

- **AC-1:** `statsApi.ts` exports `getStats()` which calls `GET /api/stats` and returns `StatsPageData`.
- **AC-2:** `StatsPage` calls `statsApi.getStats()` on mount and renders all four analytics sections.
- **AC-3 (FR-19):** By-status section displays: total, resting, started, finished, abandoned counts.
- **AC-4 (FR-20):** Books completed section shows six rows (7/30/90/180/270/365 days) with counts labelled clearly (e.g. "Last 30 days: 2 books").
- **AC-5 (FR-21):** Pages read section shows the same six rolling windows with page totals.
- **AC-6 (FR-22):** Unfinished Genre section — when `unfinishedGenre` is not null: displays genre name with copy "You tend to leave [Genre] books unfinished"; when null: "Not enough data yet".
- **AC-7:** Layout adapts responsively — single column on mobile (< 640px `sm:`); sections use 2-column grid on desktop.
- **AC-8:** Loading state shown while fetching (centered "Loading stats…" text; no layout shift).
- **AC-9:** Error state on fetch failure (inline error message using established `bg-error-bg text-error` styling).
- **AC-10:** `StatsPageData` interface in `types/index.ts` matches the actual backend JSON shape (`booksCompleted`/`pagesRead` objects with named keys, NOT arrays).

---

## Tasks

- [x] **Task 1: Fix `StatsPageData` in `frontend/src/types/index.ts`**
  - Replace the current wrong interface (with `completionsBy`/`pagesBy` arrays) with the correct shape matching backend JSON.

- [x] **Task 2: Add `getStats()` to `frontend/src/api/statsApi.ts`**
  - Add `export const getStats = () => fetchJson<StatsPageData>('/api/stats');`
  - Add `StatsPageData` to existing imports from `../types`.

- [x] **Task 3: Implement `frontend/src/pages/StatsPage.tsx`** — replace placeholder with full analytics page
  - `useEffect` on mount: call `getStats()`, set `data` on success, set `error` on failure.
  - Loading state: centered "Loading stats…" paragraph.
  - Error state: inline `bg-error-bg text-error` div (same pattern as `ShelfPage`).
  - Render four `SectionCard` panels in a `grid-cols-1 sm:grid-cols-2` responsive grid.
  - See Dev Notes for complete component code.

- [x] **Task 4: Run `npm run build` — 0 errors, 0 type errors**

---

## Dev Notes

### Architecture Constraints (MUST follow)
- **API client is `fetchJson<T>`** from `./client` — import path from StatsPage is `'../api/statsApi'`.
- **Routing + NavBar already done.** `/stats` route in `App.tsx` and Stats links in `NavBar.tsx` are fully wired. Do NOT modify `App.tsx`, `NavBar.tsx`, or any other file outside the task list.
- **No new npm packages.** Use Tailwind tokens already in `index.css`.
- **Tailwind tokens:** `bg-warm-bg`, `bg-warm-surface`, `rounded-card`, `shadow-card-rest`, `text-text-primary`, `text-text-secondary`, `border-warm-border`, `bg-error-bg`, `text-error`.
- **Error variant:** The existing `EmptyState` component has no generic error variant — use the same inline error div pattern as `ShelfPage.tsx` (`bg-error-bg text-error text-sm rounded px-4 py-3`).

---

### `StatsPageData` — Current (WRONG) vs Required

**Current (WRONG — does not match backend):**
```ts
export interface StatsPageData {
  byStatus: {
    resting: number;
    started: number;
    finished: number;
    abandoned: number;
    total: number;
  };
  completionsBy: { days: number; count: number }[];   // ← wrong field name and shape
  pagesBy: { days: number; pages: number }[];          // ← wrong field name and shape
  unfinishedGenre: string | null;
}
```

**Required (matches backend `StatsPageResponse` JSON):**
```ts
export interface StatsPageData {
  byStatus: {
    total: number;
    resting: number;
    started: number;
    finished: number;
    abandoned: number;
  };
  booksCompleted: {
    days7: number;
    days30: number;
    days90: number;
    days180: number;
    days270: number;
    days365: number;
  };
  pagesRead: {
    days7: number;
    days30: number;
    days90: number;
    days180: number;
    days270: number;
    days365: number;
  };
  unfinishedGenre: string | null;
}
```

Backend DTO (`StatsPageResponse.cs`) serializes as:
```json
{
  "byStatus": { "total": 12, "resting": 5, "started": 3, "finished": 3, "abandoned": 1 },
  "booksCompleted": { "days7": 1, "days30": 2, "days90": 4, "days180": 6, "days270": 7, "days365": 10 },
  "pagesRead": { "days7": 120, "days30": 380, "days90": 950, "days180": 1800, "days270": 2400, "days365": 3100 },
  "unfinishedGenre": "Fantasy"
}
```

---

### Updated `statsApi.ts` (complete file after Task 2)

```ts
import { fetchJson } from './client';
import type { StatsStripData, StatsPageData } from '../types';

export const getStrip = () =>
  fetchJson<StatsStripData>('/api/stats/strip');

export const getStats = () =>
  fetchJson<StatsPageData>('/api/stats');
```

---

### Complete `StatsPage.tsx`

```tsx
import { useState, useEffect } from 'react';
import { getStats } from '../api/statsApi';
import type { StatsPageData } from '../types';

const WINDOWS = [7, 30, 90, 180, 270, 365] as const;
type WindowDay = typeof WINDOWS[number];
type PeriodKey = `days${WindowDay}`;

function SectionCard({ title, children }: { title: string; children: React.ReactNode }) {
  return (
    <div className="bg-warm-surface rounded-card shadow-card-rest p-5">
      <h2 className="text-[16px] font-semibold text-text-primary mb-3">{title}</h2>
      {children}
    </div>
  );
}

function StatRow({ label, value }: { label: string; value: string | number }) {
  return (
    <div className="flex justify-between items-center py-1.5 border-b border-warm-border last:border-0">
      <span className="text-[14px] text-text-secondary">{label}</span>
      <span className="text-[14px] font-semibold text-text-primary">{value}</span>
    </div>
  );
}

export function StatsPage() {
  const [data, setData] = useState<StatsPageData | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState('');

  useEffect(() => {
    getStats()
      .then(setData)
      .catch(() => setError('Failed to load stats. Please try again.'))
      .finally(() => setLoading(false));
  }, []);

  if (loading) {
    return (
      <div className="flex items-center justify-center py-20">
        <p className="text-text-secondary">Loading stats…</p>
      </div>
    );
  }

  if (error || !data) {
    return (
      <div className="mx-4 sm:mx-6 mt-6 bg-error-bg text-error text-sm rounded px-4 py-3">
        {error || 'Failed to load stats.'}
      </div>
    );
  }

  const periodKey = (days: WindowDay): PeriodKey => `days${days}`;

  return (
    <div className="bg-warm-bg min-h-screen">
      <div className="px-4 sm:px-6 lg:px-8 pt-6 pb-4">
        <h1 className="text-[22px] font-semibold text-text-primary">Reading Stats</h1>
      </div>

      <div className="grid grid-cols-1 sm:grid-cols-2 gap-4 px-4 sm:px-6 lg:px-8 pb-8 max-w-[1200px] mx-auto">
        {/* FR-19: By-status counts */}
        <SectionCard title="Library">
          <StatRow label="Total books"  value={data.byStatus.total} />
          <StatRow label="Reading now"  value={data.byStatus.started} />
          <StatRow label="Finished"     value={data.byStatus.finished} />
          <StatRow label="Resting"      value={data.byStatus.resting} />
          <StatRow label="Abandoned"    value={data.byStatus.abandoned} />
        </SectionCard>

        {/* FR-22: Unfinished Genre insight */}
        <SectionCard title="Reading Habit Insight">
          {data.unfinishedGenre ? (
            <p className="text-[14px] text-text-secondary">
              You tend to leave{' '}
              <span className="font-semibold text-text-primary">{data.unfinishedGenre}</span>{' '}
              books unfinished.
            </p>
          ) : (
            <p className="text-[14px] text-text-secondary">Not enough data yet.</p>
          )}
        </SectionCard>

        {/* FR-20: Books completed by rolling windows */}
        <SectionCard title="Books Completed">
          {WINDOWS.map((days) => (
            <StatRow
              key={days}
              label={`Last ${days} days`}
              value={`${data.booksCompleted[periodKey(days)]} books`}
            />
          ))}
        </SectionCard>

        {/* FR-21: Pages read by rolling windows */}
        <SectionCard title="Pages Read">
          {WINDOWS.map((days) => (
            <StatRow
              key={days}
              label={`Last ${days} days`}
              value={`${data.pagesRead[periodKey(days)]} pages`}
            />
          ))}
        </SectionCard>
      </div>
    </div>
  );
}
```

**TypeScript note on `periodKey`:** `PeriodKey = \`days${WindowDay}\`` is a template literal type. TypeScript infers it correctly as `"days7" | "days30" | "days90" | "days180" | "days270" | "days365"` — valid keys of `booksCompleted` and `pagesRead`. No casting needed.

---

### Existing Files — Do NOT Modify

| File | Reason |
|------|--------|
| `App.tsx` | Already has `/stats` route wired with `<StatsPage />` |
| `NavBar.tsx` | Already has Stats links for desktop + mobile |
| `ShelfPage.tsx` | No relation to Stats page |
| `StatsStrip.tsx` | Already complete from Story 4.3 |
| Any backend file | Story is frontend-only |

---

### Files to Create / Modify

| File | Action |
|------|--------|
| `frontend/src/types/index.ts` | UPDATE — replace `StatsPageData` with correct backend-matching shape |
| `frontend/src/api/statsApi.ts` | UPDATE — add `getStats()` export + `StatsPageData` import |
| `frontend/src/pages/StatsPage.tsx` | UPDATE — replace placeholder with full implementation |

---

## Dev Agent Record

### Implementation Plan
3 files modified — types fix, API extension, page implementation.

### Completion Notes
- **Task 1:** Replaced wrong `StatsPageData` (array-based `completionsBy`/`pagesBy`) with correct backend-matching shape using named-key objects `booksCompleted` and `pagesRead`.
- **Task 2:** Extended `statsApi.ts` with `getStats()` → `fetchJson<StatsPageData>('/api/stats')`. Added `StatsPageData` to imports.
- **Task 3:** Replaced placeholder `StatsPage.tsx` with full implementation: 4-section analytics layout (`Library`, `Reading Habit Insight`, `Books Completed`, `Pages Read`); responsive `grid-cols-1 sm:grid-cols-2`; loading state; error state; TypeScript template literal `PeriodKey` type for type-safe rolling-window access.
- **Task 4:** `npm run build` → 102 modules, 0 errors, exit code 0. `tsc -b` passed with 0 type errors.

### File List
- `frontend/src/types/index.ts` — MODIFIED: fixed `StatsPageData` interface
- `frontend/src/api/statsApi.ts` — MODIFIED: added `getStats()` export
- `frontend/src/pages/StatsPage.tsx` — MODIFIED: full analytics page implementation

### Change Log
- 2026-05-27: Story 4.4 implemented — Stats Page frontend (fix type, add API call, full page)
