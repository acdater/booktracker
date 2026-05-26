# Story 2.5: Shelf Layout, NavBar & BookCard Component

Status: review

## Story

As an **authenticated reader**,
I want to see my shelf as a warm, card-based grid with status ribbons and reader counts,
so that I can recognise my books at a glance on any device.

## Acceptance Criteria

1. `ShelfPage` calls `shelfApi.getShelf()` on mount and renders a `BookCard` for each `UserBook` in the response
2. A `StatsStrip` renders at the top of `ShelfPage` as a **static placeholder** (all-zeros; wired to live data in Epic 4)
3. Empty shelf (zero UserBooks) shows `EmptyState` invitation variant: warm encouraging copy + prominent "Add your first book" button
4. `BookCard` (`src/components/BookCard/BookCard.tsx`) renders:
   - Cover image at **2:3 aspect ratio** or warm-toned placeholder silhouette when `coverImageUrl` is `null`
   - Title in title type scale (17px/600), author in body type scale (15px/400)
   - `StatusRibbon` component showing status name in the correct status color
   - Reader count: "👥 N readers" in caption type scale (13px/400)
   - Thin **progress strip** along card bottom edge with `aria-label="Page X of Y"` for screen readers
   - Full card is the tap target with visible press state (`active:scale-[0.98]`)
5. Card styles: `rounded-card` (12px), `shadow-card-rest` at rest / `shadow-card-hover` on hover, `bg-warm-surface` background
6. `StatusRibbon` colors: Resting = `#8C98A8` (muted slate), Started = `#C4874A` (earthy amber), Finished = `#6B8F71` (soft sage), Abandoned = `#B07880` (dusty rose)
7. `NavBar` upgraded to: **bottom tabs on mobile** (< 640px, `fixed bottom-0`) and **top bar on desktop** (≥ 640px, `top` position); active link uses `text-accent`; links to `/shelf` and `/stats`
8. `App.tsx` authenticated layout wraps children in `<main className="pb-16 sm:pb-0">` to prevent mobile content from being obscured by fixed bottom tabs
9. Responsive grid in `ShelfPage`: 1 column < 640px (16px horizontal margin), 2 columns 640–1024px (16px gap), 3 columns > 1024px (24px gap, max-width 1200px centred)
10. All interactive elements have minimum 44×44px touch targets; keyboard focus rings: `focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-accent focus-visible:ring-offset-2`
11. `types/index.ts` `UserBook` interface gains `readerCount: number`
12. `src/api/shelfApi.ts` exports `getShelf(): Promise<UserBook[]>`

## Tasks / Subtasks

- [x] Task 1: Update types and create shelfApi (AC: 11, 12)
  - [x] Add `readerCount: number` to `UserBook` interface in `src/types/index.ts`
  - [x] Create `src/api/shelfApi.ts` — `export const getShelf = () => fetchJson<UserBook[]>('/api/shelf')`

- [x] Task 2: Create `useShelf` hook (AC: 1)
  - [x] Create `src/hooks/useShelf.ts` — `useState` + `useEffect` calling `shelfApi.getShelf()`; returns `{ shelf, loading, error }`
  - [x] Error: capture `ApiError.message` or fallback `'Failed to load shelf'`
  - [x] No retry logic needed at this scope

- [x] Task 3: Upgrade `NavBar` (AC: 7, 8, 10)
  - [x] Update `src/components/NavBar/NavBar.tsx`:
    - Desktop top bar (`hidden sm:flex bg-warm-surface border-b border-warm-border px-6 items-center gap-6 h-12`)
    - Mobile bottom tabs (`sm:hidden fixed bottom-0 left-0 right-0 bg-warm-surface border-t border-warm-border flex z-50`)
    - Each tab: `flex-1 flex flex-col items-center justify-center py-2 min-h-[44px]` + label text (12px/500)
    - Active state: `text-accent`, inactive: `text-text-secondary hover:text-text-primary`
  - [x] Update `App.tsx` `AuthenticatedLayout`: wrap `{children}` in `<main className="pb-16 sm:pb-0">{children}</main>`

- [x] Task 4: Create `StatusRibbon` component (AC: 6)
  - [x] Create `src/components/StatusRibbon/StatusRibbon.tsx`
  - [x] Accepts `status: 'Resting' | 'Started' | 'Finished' | 'Abandoned'`
  - [x] Renders a small pill (`inline-flex items-center px-2 py-0.5 rounded text-white text-[11px] font-medium`) with `style={{ backgroundColor: STATUS_COLORS[status] }}`
  - [x] `STATUS_COLORS` const: `{ Resting: '#8C98A8', Started: '#C4874A', Finished: '#6B8F71', Abandoned: '#B07880' }`

- [x] Task 5: Create `BookCard` component (AC: 4, 5, 6, 10)
  - [x] Create `src/components/BookCard/BookCard.tsx`
  - [x] Accepts `userBook: UserBook` prop; optional `onClick?: () => void` prop (for future Story 3 wiring)
  - [x] Outer element: `<button type="button">` with card styles + hover shadow + press state
  - [x] Cover image section (top, 2:3 ratio via `aspect-[2/3]`): `<img>` if `coverImageUrl`, else placeholder div with warm silhouette
  - [x] Placeholder SVG: a simple open-book SVG icon centered in `bg-warm-surface-alt`
  - [x] Card body (below image): title, author, StatusRibbon, reader count
  - [x] Progress strip: `<div role="progressbar" aria-label={"Page " + currentPages + " of " + totalPages} aria-valuenow={currentPages} aria-valuemin={0} aria-valuemax={totalPages}>` — 4px tall strip at card bottom; fill width `${(currentPages / totalPages) * 100}%` using `bg-accent`; background `bg-warm-border`; show strip always (0% fill when Resting)
  - [x] Progress strip background: `bg-warm-border h-1 w-full overflow-hidden`; fill child: `bg-accent h-full transition-all`

- [x] Task 6: Create `StatsStrip` placeholder (AC: 2)
  - [x] Create `src/components/StatsStrip/StatsStrip.tsx`
  - [x] Static placeholder — renders a strip with 4 metrics: "0 books", "0 in progress", "0 finished", "0 pages this month"
  - [x] Style: `bg-warm-surface-alt border-b border-warm-border px-4 sm:px-6 py-3 flex gap-6 overflow-x-auto`
  - [x] Each metric: `<div className="flex flex-col items-center min-w-[80px]"><span className="text-lg font-semibold text-text-primary">0</span><span className="text-[12px] text-text-secondary">books</span></div>`

- [x] Task 7: Create `EmptyState` component (AC: 3, 10)
  - [x] Create `src/components/EmptyState/EmptyState.tsx`
  - [x] Accepts `onAddBook?: () => void` prop
  - [x] Renders centred layout: book emoji or warm SVG icon, heading "Your shelf is empty", sub-copy "Start by adding your first book to track your reading.", CTA button "Add your first book"
  - [x] CTA button: `bg-accent hover:bg-accent-hover text-white px-6 py-3 rounded-button min-h-[44px] font-medium`

- [x] Task 8: Wire `ShelfPage` (AC: 1, 2, 3, 9)
  - [x] Update `src/pages/ShelfPage.tsx`:
    - Import `useShelf`, `BookCard`, `StatsStrip`, `EmptyState`
    - Render `<StatsStrip />` always at top
    - Loading state: spinner or "Loading your shelf…" text (centered, `text-text-secondary`)
    - Error state: `<div className="bg-error-bg text-error ...">` with error message
    - Shelf grid: `<div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-3 gap-4 sm:gap-4 lg:gap-6 px-4 sm:px-6 lg:px-8 py-6 max-w-[1200px] mx-auto">`
    - Empty state: `<EmptyState onAddBook={() => {/* Story 2.6 */}} />`
    - Page heading: `<h1 className="text-[22px] font-semibold text-text-primary px-4 sm:px-6 lg:px-8 pt-6 pb-2">My Shelf</h1>`

- [x] Task 9: Verify frontend builds without errors (AC: all)
  - [x] Run `npm run build` (or `npm run dev` briefly) from `frontend/`
  - [x] Zero TypeScript errors, zero build errors

## Dev Notes

### ⚠️ Tailwind v4 — No `tailwind.config.js`

This project uses **Tailwind CSS v4** with the `@tailwindcss/vite` plugin. All tokens are defined in `src/index.css` under `@theme {}` as CSS custom properties — **there is no `tailwind.config.js`**. Do NOT create one.

Token naming: `--color-warm-bg` → Tailwind class `bg-warm-bg`, `text-warm-bg`. The `--shadow-card-rest` → `shadow-card-rest`. The `--radius-card` → `rounded-card`.

**Status ribbon colors (`#8C98A8`, `#C4874A`, `#6B8F71`, `#B07880`) are NOT in the theme** — use inline `style={{ backgroundColor: STATUS_COLORS[status] }}` in `StatusRibbon`.

### Tailwind v4 Responsive Classes

Standard breakpoint classes work as expected:
- `sm:` = ≥ 640px
- `lg:` = ≥ 1024px

So the responsive grid is: `grid-cols-1 sm:grid-cols-2 lg:grid-cols-3`.

### `UserBook` in `types/index.ts` — Missing `readerCount`

The current `UserBook` interface does NOT have `readerCount`. The backend `UserBookResponse` has it. You **must** add it:

```ts
export interface UserBook {
  // ... existing fields ...
  readerCount: number;  // ADD THIS
}
```

### `shelfApi.ts` Pattern (Follow `authApi.ts`)

```ts
import { fetchJson } from './client';
import type { UserBook } from '../types';

export const getShelf = () => fetchJson<UserBook[]>('/api/shelf');
```

The Vite proxy (`vite.config.ts`) already forwards `/api/*` to `http://localhost:5000`.

### `useShelf` Hook Pattern

```ts
import { useState, useEffect } from 'react';
import { getShelf } from '../api/shelfApi';
import { ApiError } from '../api/client';
import type { UserBook } from '../types';

export function useShelf() {
  const [shelf, setShelf] = useState<UserBook[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    getShelf()
      .then(setShelf)
      .catch((err) => setError(err instanceof ApiError ? err.message : 'Failed to load shelf'))
      .finally(() => setLoading(false));
  }, []);

  return { shelf, loading, error };
}
```

### NavBar Responsive Layout

Renders TWO nav elements — one visible on desktop, one on mobile — controlled purely by Tailwind breakpoint classes:

```tsx
export function NavBar() {
  const linkClass = ({ isActive }: { isActive: boolean }) =>
    isActive ? 'text-accent font-medium' : 'text-text-secondary hover:text-text-primary transition-colors';

  return (
    <>
      {/* Desktop top bar */}
      <nav className="hidden sm:flex bg-warm-surface border-b border-warm-border px-6 items-center gap-6 h-12">
        <NavLink to="/shelf" className={linkClass}>Shelf</NavLink>
        <NavLink to="/stats" className={linkClass}>Stats</NavLink>
      </nav>

      {/* Mobile bottom tabs */}
      <nav className="sm:hidden fixed bottom-0 left-0 right-0 bg-warm-surface border-t border-warm-border flex z-50">
        <NavLink to="/shelf" className={({ isActive }) =>
          `flex-1 flex flex-col items-center justify-center py-2 min-h-[44px] text-[12px] font-medium transition-colors ${isActive ? 'text-accent' : 'text-text-secondary'}`
        }>
          📚
          <span>Shelf</span>
        </NavLink>
        <NavLink to="/stats" className={({ isActive }) =>
          `flex-1 flex flex-col items-center justify-center py-2 min-h-[44px] text-[12px] font-medium transition-colors ${isActive ? 'text-accent' : 'text-text-secondary'}`
        }>
          📊
          <span>Stats</span>
        </NavLink>
      </nav>
    </>
  );
}
```

### `App.tsx` Update — Mobile Content Padding

Change `AuthenticatedLayout` to add bottom padding on mobile so fixed bottom tabs don't cover content:

```tsx
function AuthenticatedLayout({ children }: { children: React.ReactNode }) {
  return (
    <>
      <NavBar />
      <main className="pb-16 sm:pb-0">{children}</main>
    </>
  );
}
```

### `BookCard` Full Implementation Pattern

```tsx
const STATUS_COLORS = {
  Resting: '#8C98A8',
  Started: '#C4874A',
  Finished: '#6B8F71',
  Abandoned: '#B07880',
} as const;

export function BookCard({ userBook, onClick }: { userBook: UserBook; onClick?: () => void }) {
  const { book, status, currentPages, readerCount } = userBook;
  const progressPct = book.totalPages > 0 ? (currentPages / book.totalPages) * 100 : 0;

  return (
    <button
      type="button"
      onClick={onClick}
      className="bg-warm-surface rounded-card shadow-card-rest hover:shadow-card-hover active:scale-[0.98] transition-all duration-150 text-left w-full overflow-hidden focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-accent focus-visible:ring-offset-2"
    >
      {/* Cover image — 2:3 aspect ratio */}
      <div className="aspect-[2/3] w-full overflow-hidden bg-warm-surface-alt flex items-center justify-center">
        {book.coverImageUrl ? (
          <img src={book.coverImageUrl} alt={book.title} className="w-full h-full object-cover" />
        ) : (
          <PlaceholderCover />
        )}
      </div>

      {/* Card body */}
      <div className="p-3 flex flex-col gap-1">
        <p className="text-[17px] font-semibold text-text-primary leading-[1.35] line-clamp-2">{book.title}</p>
        <p className="text-[15px] text-text-secondary leading-[1.5] line-clamp-1">{book.author}</p>
        <StatusRibbon status={status} />
        <p className="text-[13px] text-text-secondary mt-1">👥 {readerCount} {readerCount === 1 ? 'reader' : 'readers'}</p>
      </div>

      {/* Progress strip — always shown */}
      <div
        className="bg-warm-border h-1 w-full overflow-hidden"
        role="progressbar"
        aria-label={`Page ${currentPages} of ${book.totalPages}`}
        aria-valuenow={currentPages}
        aria-valuemin={0}
        aria-valuemax={book.totalPages}
      >
        <div className="bg-accent h-full transition-all duration-300" style={{ width: `${progressPct}%` }} />
      </div>
    </button>
  );
}
```

### `PlaceholderCover` Sub-component (inside BookCard.tsx)

```tsx
function PlaceholderCover() {
  return (
    <div className="w-full h-full flex items-center justify-center bg-warm-surface-alt">
      <svg width="48" height="48" viewBox="0 0 48 48" fill="none" aria-hidden="true">
        <rect x="8" y="6" width="32" height="36" rx="3" fill="#E2D9CE" />
        <rect x="12" y="14" width="24" height="2" rx="1" fill="#ADA49A" />
        <rect x="12" y="20" width="18" height="2" rx="1" fill="#ADA49A" />
        <rect x="12" y="26" width="20" height="2" rx="1" fill="#ADA49A" />
      </svg>
    </div>
  );
}
```

### `StatusRibbon` Component

```tsx
const STATUS_COLORS = {
  Resting: '#8C98A8',
  Started: '#C4874A',
  Finished: '#6B8F71',
  Abandoned: '#B07880',
} as const;

export function StatusRibbon({ status }: { status: 'Resting' | 'Started' | 'Finished' | 'Abandoned' }) {
  return (
    <span
      className="inline-flex items-center px-2 py-0.5 rounded text-white text-[11px] font-medium self-start"
      style={{ backgroundColor: STATUS_COLORS[status] }}
    >
      {status}
    </span>
  );
}
```

### `ShelfPage` Full Pattern

```tsx
export function ShelfPage() {
  const { shelf, loading, error } = useShelf();

  return (
    <div className="bg-warm-bg min-h-screen">
      <StatsStrip />
      <h1 className="text-[22px] font-semibold text-text-primary px-4 sm:px-6 lg:px-8 pt-6 pb-2">My Shelf</h1>

      {loading && (
        <p className="text-text-secondary text-center py-12">Loading your shelf…</p>
      )}

      {error && (
        <div className="mx-4 sm:mx-6 mt-4 bg-error-bg text-error text-sm rounded px-4 py-3">{error}</div>
      )}

      {!loading && !error && shelf.length === 0 && (
        <EmptyState onAddBook={() => { /* wired in Story 2.6 */ }} />
      )}

      {!loading && !error && shelf.length > 0 && (
        <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-3 gap-4 lg:gap-6 px-4 sm:px-6 lg:px-8 pb-6 max-w-[1200px] mx-auto">
          {shelf.map((ub) => (
            <BookCard key={ub.id} userBook={ub} />
          ))}
        </div>
      )}
    </div>
  );
}
```

### `StatsStrip` Placeholder Pattern

```tsx
export function StatsStrip() {
  const stats = [
    { value: 0, label: 'books' },
    { value: 0, label: 'in progress' },
    { value: 0, label: 'finished' },
    { value: 0, label: 'pages this month' },
  ];
  return (
    <div className="bg-warm-surface-alt border-b border-warm-border px-4 sm:px-6 py-3 flex gap-6 overflow-x-auto">
      {stats.map(({ value, label }) => (
        <div key={label} className="flex flex-col items-center min-w-[80px] shrink-0">
          <span className="text-lg font-semibold text-text-primary">{value}</span>
          <span className="text-[12px] text-text-secondary whitespace-nowrap">{label}</span>
        </div>
      ))}
    </div>
  );
}
```

### `EmptyState` Pattern

```tsx
export function EmptyState({ onAddBook }: { onAddBook?: () => void }) {
  return (
    <div className="flex flex-col items-center justify-center py-20 px-8 text-center">
      <div className="text-5xl mb-4">📚</div>
      <h2 className="text-[22px] font-semibold text-text-primary mb-2">Your shelf is empty</h2>
      <p className="text-text-secondary text-[15px] mb-8 max-w-xs">
        Start by adding your first book to track your reading.
      </p>
      <button
        type="button"
        onClick={onAddBook}
        className="bg-accent hover:bg-accent-hover text-white px-6 py-3 rounded-button min-h-[44px] font-medium transition-colors focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-accent focus-visible:ring-offset-2"
      >
        Add your first book
      </button>
    </div>
  );
}
```

### Files to Modify (Existing)

- `src/types/index.ts` — add `readerCount: number` to `UserBook`
- `src/components/NavBar/NavBar.tsx` — responsive upgrade
- `src/App.tsx` — `AuthenticatedLayout` wraps children in `<main className="pb-16 sm:pb-0">`
- `src/pages/ShelfPage.tsx` — wire to `useShelf`, render components

### Files to Create (New)

- `src/api/shelfApi.ts`
- `src/hooks/useShelf.ts`
- `src/components/StatusRibbon/StatusRibbon.tsx`
- `src/components/BookCard/BookCard.tsx`
- `src/components/StatsStrip/StatsStrip.tsx`
- `src/components/EmptyState/EmptyState.tsx`

### No Backend Changes

Story 2.5 is purely frontend. The backend `GET /api/shelf` endpoint is already implemented (Story 2.4). Do NOT modify any `.cs` files.

### Frontend Build Check Command

From the repo root:
```
cd frontend && npm run build
```

Vite will fail loudly on TypeScript errors. Zero errors is the pass condition.

### Key Non-Breaking Points for `AuthApi` and Auth Flows

- `LoginPage` and `RegisterPage` DO NOT use `NavBar` (they go through the public routes, not `AuthenticatedLayout`). The change to `AuthenticatedLayout` only affects `/shelf` and `/stats`.
- `RequireAuth` component is unchanged.
- `useAuth` hook is unchanged.

## Dev Agent Record

### Agent Model Used

claude-sonnet-4.6

### Debug Log References

### Completion Notes List

- All 9 tasks complete. Build: 0 TypeScript errors, 0 build errors ✅
- Added `readerCount: number` to `UserBook` type
- Created `shelfApi.ts` + `useShelf` hook (loading/error/data state)
- NavBar upgraded: desktop top bar (`hidden sm:flex`) + mobile fixed bottom tabs (`sm:hidden fixed bottom-0`)
- App.tsx `AuthenticatedLayout` adds `<main className="pb-16 sm:pb-0">` for mobile bottom tab clearance
- Created 5 new components: `StatusRibbon`, `BookCard`, `StatsStrip`, `EmptyState`, + `PlaceholderCover` inside BookCard
- Status ribbon colors via inline `style={{ backgroundColor }}` (not Tailwind tokens — correct approach for Tailwind v4)
- `BookCard` uses `aspect-[2/3]` for cover, progress strip via `role="progressbar"` with `aria-label`
- `EmptyState` `onAddBook` wired to no-op; Story 2.6 will replace it with modal open
- Frontend build: 38 modules, 245KB JS (gzipped 77KB), 15.5KB CSS

### File List

- `frontend/src/types/index.ts` (modified — added readerCount to UserBook)
- `frontend/src/api/shelfApi.ts` (new)
- `frontend/src/hooks/useShelf.ts` (new)
- `frontend/src/components/NavBar/NavBar.tsx` (modified — responsive upgrade)
- `frontend/src/App.tsx` (modified — main wrapper with pb-16 sm:pb-0)
- `frontend/src/components/StatusRibbon/StatusRibbon.tsx` (new)
- `frontend/src/components/BookCard/BookCard.tsx` (new)
- `frontend/src/components/StatsStrip/StatsStrip.tsx` (new)
- `frontend/src/components/EmptyState/EmptyState.tsx` (new)
- `frontend/src/pages/ShelfPage.tsx` (modified — full implementation)
