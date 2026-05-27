# Story 4.3: Frontend StatsStrip (Live Data)

**Epic:** 4 — Reading Analytics  
**Story ID:** 4.3  
**Story Key:** 4-3-frontend-statsstrip-live-data  
**Status:** review  

---

## User Story

As a **reader**,  
I want the Stats Strip on the Shelf to show my live reading totals on every visit,  
So that I get an honest summary of my reading life without any extra navigation.

---

## Acceptance Criteria

- **AC-1:** `statsApi.ts` exports `getStrip()` which calls `GET /api/stats/strip` and returns `StatsStripData`.
- **AC-2:** `StatsStrip` calls `statsApi.getStrip()` on mount and renders four values: total books, finished, started, pages this calendar month.
- **AC-3:** Labels match the spec: `"{n} books"`, `"{n} finished"`, `"{n} reading"`, `"{n} pages this month"`.
- **AC-4:** Loading state shows `"—"` for each value while the request is in flight; no layout shift (structure identical in loading and loaded states).
- **AC-5:** `StatsStrip` is permanently anchored above the book card grid — already satisfied by `ShelfPage.tsx` rendering it at the top (no change to `ShelfPage` needed).
- **AC-6:** `StatsStrip` uses `warm-surface-alt` background — already present in current markup, must be preserved.
- **AC-7:** `StatsStripData` interface field names match the backend JSON (`totalBooks`, not `totalUserBooks`).

---

## Tasks

- [x] **Task 1: Create `frontend/src/api/statsApi.ts`**
  - Export `getStrip()` → `fetchJson<StatsStripData>('/api/stats/strip')`.
  - Import `fetchJson` from `./client` and `StatsStripData` from `../types`.

- [x] **Task 2: Fix `StatsStripData` in `frontend/src/types/index.ts`**
  - Rename `totalUserBooks` → `totalBooks` to match backend camelCase serialization.

- [x] **Task 3: Update `StatsStrip.tsx` to fetch and render live data**
  - Add `useState<StatsStripData | null>` for `data` (initially `null`).
  - Add `useState<boolean>` for `loading` (initially `true`).
  - `useEffect` on mount: call `getStrip()`, set `data` on success, set `loading = false` in both success and error paths.
  - Render: if `loading` or `data === null`, show `"—"` for each value; else show the live numbers.
  - Preserve existing layout exactly: `bg-warm-surface-alt border-b border-warm-border px-4 sm:px-6 py-3 flex gap-6 overflow-x-auto` wrapper; each stat is `flex flex-col items-center min-w-[80px] shrink-0`.
  - **Remove** the hardcoded `STATS` constant — replace with dynamic rendering from `data`.

- [x] **Task 4: Run `npm run build` — 0 errors, 0 type errors**

---

## Dev Notes

### Architecture Constraints (MUST follow)
- **API client is `fetchJson<T>`** from `./client` — NOT `axios`, NOT `apiRequest`. All API calls use this pattern.
- **No `useEffect` dependencies to suppress.** The effect only runs on mount (empty `[]` dep array) — correct.
- **No layout shift.** The strip must have the same physical structure and dimensions in loading state as in loaded state. Use `"—"` as a placeholder string (same width region as a short number).
- **No error UI required by story.** The AC does not require an error state display — if the fetch fails, the `"—"` placeholders simply remain (loading stays true, or silently stay null). Handle the error path by catching and just leaving `data = null`.
- **Do not change `ShelfPage.tsx`.** It already renders `<StatsStrip />` at the top correctly.
- **Tailwind v4 tokens** — use `bg-warm-surface-alt`, `text-text-primary`, `text-text-secondary` — these are already used in the component, preserve them.

---

### `StatsStripData` — Current vs Required

**Current (WRONG):**
```ts
export interface StatsStripData {
  totalUserBooks: number;   // ← wrong field name
  finishedCount: number;
  startedCount: number;
  pagesThisMonth: number;
}
```

**Required (matches backend JSON):**
```ts
export interface StatsStripData {
  totalBooks: number;       // ← matches backend TotalBooks → totalBooks
  finishedCount: number;
  startedCount: number;
  pagesThisMonth: number;
}
```

Backend DTO (`StatsStripResponse.cs`):
```csharp
public int TotalBooks { get; set; }      // → JSON: "totalBooks"
public int FinishedCount { get; set; }   // → JSON: "finishedCount"
public int StartedCount { get; set; }    // → JSON: "startedCount"
public int PagesThisMonth { get; set; }  // → JSON: "pagesThisMonth"
```

---

### Complete `statsApi.ts`

```ts
import { fetchJson } from './client';
import type { StatsStripData } from '../types';

export const getStrip = () =>
  fetchJson<StatsStripData>('/api/stats/strip');
```

---

### Complete `StatsStrip.tsx`

```tsx
import { useState, useEffect } from 'react';
import { getStrip } from '../../api/statsApi';
import type { StatsStripData } from '../../types';

export function StatsStrip() {
  const [data, setData] = useState<StatsStripData | null>(null);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    getStrip()
      .then(setData)
      .catch(() => {/* stay null */})
      .finally(() => setLoading(false));
  }, []);

  const stats = [
    { value: data?.totalBooks,      label: 'books' },
    { value: data?.startedCount,    label: 'reading' },
    { value: data?.finishedCount,   label: 'finished' },
    { value: data?.pagesThisMonth,  label: 'pages this month' },
  ];

  return (
    <div className="bg-warm-surface-alt border-b border-warm-border px-4 sm:px-6 py-3 flex gap-6 overflow-x-auto">
      {stats.map(({ value, label }) => (
        <div key={label} className="flex flex-col items-center min-w-[80px] shrink-0">
          <span className="text-lg font-semibold text-text-primary">
            {loading || value === undefined ? '—' : value}
          </span>
          <span className="text-[12px] text-text-secondary whitespace-nowrap">{label}</span>
        </div>
      ))}
    </div>
  );
}
```

**Why `value === undefined`:** During loading, `data` is null so `data?.totalBooks` is `undefined` — the check `loading || value === undefined` ensures `"—"` shows for all values during load. After load, `value` is a number (even `0`), so it renders correctly.

---

### Existing `StatsStrip.tsx` — What to Preserve

Current file (hardcoded static):
```tsx
const STATS = [
  { value: 0, label: 'books' },
  { value: 0, label: 'in progress' },
  { value: 0, label: 'finished' },
  { value: 0, label: 'pages this month' },
] as const;

export function StatsStrip() {
  return (
    <div className="bg-warm-surface-alt border-b border-warm-border px-4 sm:px-6 py-3 flex gap-6 overflow-x-auto">
      {STATS.map(({ value, label }) => (
        <div key={label} className="flex flex-col items-center min-w-[80px] shrink-0">
          <span className="text-lg font-semibold text-text-primary">{value}</span>
          <span className="text-[12px] text-text-secondary whitespace-nowrap">{label}</span>
        </div>
      ))}
    </div>
  );
}
```

**Changes:**
- Remove `STATS` const
- Add `useState` / `useEffect` imports
- Add `getStrip` import from `../../api/statsApi`
- Add `StatsStripData` type import from `../../types`
- Replace static values with live `data` values, `"—"` during loading
- Change label `"in progress"` → `"reading"` (matches AC-3)

**Must preserve:** outer div classNames exactly (background, border, padding, flex).

---

### `ShelfPage.tsx` — Do NOT Modify

`ShelfPage.tsx` already renders `<StatsStrip />` at the top of the page, above the book card grid. No changes needed.

---

### Files to Create / Modify

| File | Action |
|------|--------|
| `frontend/src/api/statsApi.ts` | CREATE |
| `frontend/src/types/index.ts` | UPDATE — rename `totalUserBooks` → `totalBooks` in `StatsStripData` |
| `frontend/src/components/StatsStrip/StatsStrip.tsx` | UPDATE — replace static with live fetch |

**Do NOT modify:** `ShelfPage.tsx`, `StatsPage.tsx`, `client.ts`, `shelfApi.ts`, or any backend file.

---

## Dev Agent Record

### File List
- `frontend/src/api/statsApi.ts` — NEW
- `frontend/src/types/index.ts` — `StatsStripData.totalUserBooks` renamed to `totalBooks`
- `frontend/src/components/StatsStrip/StatsStrip.tsx` — replaced static render with live `useEffect` fetch

### Completion Notes
All 4 tasks complete. `npm run build`: 102 modules, 0 errors, 0 type errors.

Key decisions:
- `loading || value === undefined` guard ensures `"—"` during load even when value is `0` (avoiding falsy confusion).
- Error path uses `.catch(() => {})` — data stays null, placeholders remain on failure (no error UI required by story).
- Label order matches AC-3: books, reading, finished, pages this month.
- Label changed from `"in progress"` → `"reading"` per AC-3.
- `ShelfPage.tsx` untouched — already renders `<StatsStrip />` at correct position.

### Change Log
- 2026-05-27: Implemented Story 4.3 — Frontend StatsStrip Live Data
