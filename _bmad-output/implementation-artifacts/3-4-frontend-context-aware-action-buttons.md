# Story 3.4: Frontend Context-Aware Action Buttons

Status: review

## Story

As a **reader**,
I want each book card to show only the valid action for its current status,
So that I can start, abandon, and re-read books directly from the shelf.

## Acceptance Criteria

1. A `BookCard` with `Status = Resting` renders one action button: **"Start Reading"** — tapping calls `shelfApi.updateStatus(userBookId, 'Started')`; on success, shelf refetches and ribbon animates to Started (earthy amber) via CSS transition
2. A `BookCard` with `Status = Started` renders one action button: **"Abandon"** — styled with `text-secondary` color (subdued, non-punishing); tapping calls `shelfApi.updateStatus(userBookId, 'Abandoned')`; on success, ribbon animates to Abandoned (dusty rose) via CSS transition
3. No "Mark Finished" button exists — finishing is triggered exclusively via the page stepper (Story 3.5)
4. A `BookCard` with `Status = Finished` or `Abandoned` renders one action button: **"Read Again"** — tapping calls `shelfApi.reread(userBookId)`; on success, shelf refetches and new Resting card appears at the top (sorted by `LastActivityAt DESC` from API)
5. All status ribbon color changes use CSS transitions (`transition: background-color 0.3s ease`) — **not** instant DOM swaps (UX-DR15)
6. API errors from action buttons display as **inline card-level error messages** below the button; card state does NOT mutate on error (no optimistic update)
7. `shelfApi.ts` exports two new functions: `updateStatus(userBookId: number, status: string): Promise<UserBook>` calling `PATCH /api/shelf/{userBookId}/status` and `reread(userBookId: number): Promise<UserBook>` calling `POST /api/shelf/{userBookId}/reread`
8. `BookCard` is restructured from a full-card `<button>` to an `<article>` wrapper, so action buttons (nested `<button>` elements) are valid HTML — `onClick` prop is preserved for Story 3.5 (progress popup on card tap)
9. `ShelfPage` passes `refetch` as `onRefetch` to each `BookCard`

## Tasks / Subtasks

- [x] Task 1: Add `updateStatus` and `reread` to `shelfApi.ts` (AC: 7)
  - [x] Add to `frontend/src/api/shelfApi.ts`:
    ```typescript
    export const updateStatus = (userBookId: number, status: string) =>
      fetchJson<UserBook>(`/api/shelf/${userBookId}/status`, {
        method: 'PATCH',
        body: JSON.stringify({ status }),
      });

    export const reread = (userBookId: number) =>
      fetchJson<UserBook>(`/api/shelf/${userBookId}/reread`, { method: 'POST' });
    ```

- [x] Task 2: Add CSS transition to `StatusRibbon` (AC: 5)
  - [x] Update `frontend/src/components/StatusRibbon/StatusRibbon.tsx`:
    - Add `transition: 'background-color 0.3s ease'` to the inline style object of the `<span>`
    - This enables smooth ribbon color animation when `status` prop changes after shelf refetch

- [x] Task 3: Restructure `BookCard` and add action buttons (AC: 1, 2, 3, 4, 6, 8, 9)
  - [x] Change outer `<button>` to `<article>` in `frontend/src/components/BookCard/BookCard.tsx`
  - [x] Wrap cover + title + author + ribbon + reader count + progress strip in a `<div onClick={onClick}>` with `cursor-pointer` when `onClick` is provided — this preserves the tap target for Story 3.5
  - [x] Add `onRefetch?: () => void` to `BookCardProps`
  - [x] Add local state: `const [actionError, setActionError] = useState<string | null>(null)` and `const [actionLoading, setActionLoading] = useState(false)`
  - [x] Add `ActionButton` helper or inline logic for the 3 button variants — see Dev Notes for full implementation
  - [x] Add inline error display: `{actionError && <p className="text-error text-[13px] mt-1">{actionError}</p>}`

- [x] Task 4: Update `ShelfPage` to pass `onRefetch` (AC: 9)
  - [x] Update `frontend/src/pages/ShelfPage.tsx`:
    - Change `<BookCard key={ub.id} userBook={ub} />` → `<BookCard key={ub.id} userBook={ub} onRefetch={refetch} />`

- [x] Task 5: Verify in browser (AC: all)
  - [x] `npm run dev` starts without errors
  - [x] Resting card shows "Start Reading" → click → card refetches and ribbon animates to amber
  - [x] Started card shows "Abandon" (muted style) → click → ribbon animates to dusty rose
  - [x] Finished/Abandoned card shows "Read Again" → click → new Resting card appears at top
  - [x] API error (e.g., network off) → inline message appears under button; no state mutation

## Dev Notes

### Current `BookCard.tsx` Structure

The current `BookCard` is a single `<button onClick={onClick}>` wrapping the entire card. HTML forbids nesting `<button>` inside `<button>`, so we must change the outer element.

**Change outer `<button>` → `<article>`** and wrap the "clickable body" portion in a `<div>` that receives `onClick`. The action buttons live below, outside this inner div.

### Full `BookCard.tsx` Implementation

```tsx
import { useState } from 'react';
import { StatusRibbon } from '../StatusRibbon/StatusRibbon';
import * as shelfApi from '../../api/shelfApi';
import { ApiError } from '../../api/client';
import type { UserBook } from '../../types';

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

interface BookCardProps {
  userBook: UserBook;
  onClick?: () => void;
  onRefetch?: () => void;
}

export function BookCard({ userBook, onClick, onRefetch }: BookCardProps) {
  const { book, status, currentPages, readerCount } = userBook;
  const progressPct = book.totalPages > 0 ? (currentPages / book.totalPages) * 100 : 0;
  const [actionError, setActionError] = useState<string | null>(null);
  const [actionLoading, setActionLoading] = useState(false);

  async function handleAction() {
    setActionError(null);
    setActionLoading(true);
    try {
      if (status === 'Resting') {
        await shelfApi.updateStatus(userBook.id, 'Started');
      } else if (status === 'Started') {
        await shelfApi.updateStatus(userBook.id, 'Abandoned');
      } else {
        await shelfApi.reread(userBook.id);
      }
      onRefetch?.();
    } catch (err) {
      setActionError(err instanceof ApiError ? err.message : 'Something went wrong. Please try again.');
    } finally {
      setActionLoading(false);
    }
  }

  const actionLabel =
    status === 'Resting' ? 'Start Reading' :
    status === 'Started' ? 'Abandon' :
    'Read Again';

  const isAbandoned = status === 'Started';

  return (
    <article className="bg-warm-surface rounded-card shadow-card-rest hover:shadow-card-hover transition-shadow duration-150 overflow-hidden">
      {/* Clickable upper section — for Story 3.5 progress popup */}
      <div
        onClick={onClick}
        className={onClick ? 'cursor-pointer active:scale-[0.98] transition-transform duration-150' : ''}
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
          <p className="text-[13px] text-text-secondary mt-1">
            👥 {readerCount} {readerCount === 1 ? 'reader' : 'readers'}
          </p>
        </div>

        {/* Progress strip */}
        <div
          className="bg-warm-border h-1 w-full overflow-hidden"
          role="progressbar"
          aria-label={`Page ${currentPages} of ${book.totalPages}`}
          aria-valuenow={currentPages}
          aria-valuemin={0}
          aria-valuemax={book.totalPages}
        >
          <div
            className="bg-accent h-full transition-all duration-300"
            style={{ width: `${progressPct}%` }}
          />
        </div>
      </div>

      {/* Action area */}
      <div className="px-3 pb-3 pt-2">
        <button
          type="button"
          onClick={handleAction}
          disabled={actionLoading}
          className={[
            'w-full py-2 rounded-button text-[14px] font-medium min-h-[44px] transition-colors focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-accent focus-visible:ring-offset-2 disabled:opacity-50',
            isAbandoned
              ? 'text-text-secondary bg-warm-surface-alt hover:bg-warm-border'
              : 'bg-accent text-white hover:bg-accent-hover',
          ].join(' ')}
        >
          {actionLoading ? '…' : actionLabel}
        </button>
        {actionError && (
          <p className="text-error text-[13px] mt-1">{actionError}</p>
        )}
      </div>
    </article>
  );
}
```

### Key Design Decisions

**Why `<article>` not `<div>`?**
An `<article>` represents a self-contained composition (a book card is semantically a content unit). Using a semantic HTML element is good for accessibility without needing `role="article"` explicitly.

**Why wrap only the upper section in `<div onClick>`?**
Story 3.5 will add "tap card to open progress popup" for Started cards. By isolating the clickable region to the upper body (and not the action button row), tapping the action button won't accidentally trigger the popup in Story 3.5.

**Why NOT optimistic updates?**
AC #6 explicitly states "card state does not mutate on error." Optimistic updates would require rollback logic. `onRefetch()` after success is the correct pattern here — it re-fetches the full shelf, which is already the established pattern from `useShelf`.

**CSS Transitions for ribbon color:**
`StatusRibbon` uses an inline `style={{ backgroundColor }}`. Adding `transition: 'background-color 0.3s ease'` to the same style object enables smooth color changes when the parent re-renders with a new status after `onRefetch()`.

**`Abandon` button styling:**
Uses `bg-warm-surface-alt hover:bg-warm-border text-text-secondary` — intentionally subdued per UX-DR spec ("non-punishing"). All other actions use the primary `bg-accent text-white` style.

### `shelfApi.ts` — Existing Exports to Preserve

Current exports: `getShelf`, `addToShelf` — must NOT be removed or renamed.

New exports to add: `updateStatus`, `reread`. Add at the bottom of the file.

### `StatusRibbon.tsx` — Minimal Change

Only add `transition: 'background-color 0.3s ease'` to the inline style. Do not change the `STATUS_COLORS` map or any other logic.

### `ShelfPage.tsx` — One-Line Change

Only change: `<BookCard key={ub.id} userBook={ub} />` → `<BookCard key={ub.id} userBook={ub} onRefetch={refetch} />`

The `refetch` function is already available from `useShelf()` and already used by `BookForm.onSuccess`.

### No New Files Required

All changes are to existing files. No new components, hooks, or API files needed.

### Existing Files to Modify

| File | Change |
|------|--------|
| `frontend/src/api/shelfApi.ts` | Add `updateStatus` and `reread` exports |
| `frontend/src/components/StatusRibbon/StatusRibbon.tsx` | Add CSS transition to inline style |
| `frontend/src/components/BookCard/BookCard.tsx` | Restructure outer element, add action buttons, add state |
| `frontend/src/pages/ShelfPage.tsx` | Pass `onRefetch={refetch}` to `BookCard` |

### Tailwind Classes Reference

From `src/index.css` `@theme` block (Tailwind v4 — design tokens in CSS, NOT in tailwind.config.js):
- `bg-accent` = #6B7555, `hover:bg-accent-hover` = #556044
- `text-text-secondary` = #6B6259
- `bg-warm-surface-alt` = #F3EEE7, `hover:bg-warm-border` = #E2D9CE
- `text-error` = #A84040
- `rounded-button` = 8px, `rounded-card` = 12px
- `shadow-card-rest`, `shadow-card-hover` — existing card shadows

### API Response Types

`updateStatus` and `reread` both return `UserBook` from the backend's `UserBookResponse` (camelCase from global JSON serialization). Type is already defined in `frontend/src/types/index.ts`.

---

## Dev Agent Record

### Agent Model Used

claude-sonnet-4.6

### Debug Log References

### Completion Notes List

Story 3.4 implemented. All 4 files modified, build passes (97 modules, 0 errors).
- `shelfApi.ts`: added `updateStatus` (PATCH /status) and `reread` (POST /reread)
- `StatusRibbon.tsx`: added `transition: background-color 0.3s ease` to inline style
- `BookCard.tsx`: restructured `<button>` → `<article>`, clickable upper body in `<div onClick>`, action buttons in separate `<div>`, local `actionError`/`actionLoading` state, inline error display, `onRefetch` prop
- `ShelfPage.tsx`: passes `onRefetch={refetch}` to `BookCard`

### File List

- frontend/src/api/shelfApi.ts
- frontend/src/components/StatusRibbon/StatusRibbon.tsx
- frontend/src/components/BookCard/BookCard.tsx
- frontend/src/pages/ShelfPage.tsx
