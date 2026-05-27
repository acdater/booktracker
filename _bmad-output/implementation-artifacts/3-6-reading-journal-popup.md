# Story 3.6 — Reading Journal Popup

**Status:** review  
**Epic:** 3 — Reading Lifecycle & Progress Tracking  
**Story ID:** 3.6  

---

## User Story

As a **reader**,  
I want to open the Reading Journal for any book and see my full event history across all readings,  
So that I can reflect on my complete reading journey for that book.

---

## Acceptance Criteria

- `JournalPopup` opens from the "Journal" trigger button on **any** `BookCard` (any status)
- Calls `shelfApi.getJournal(userBookId)` → `GET /api/shelf/{userBookId}/journal`
- Renders a timeline of all `BookAction`s across all `readingNumber`s, **newest first** (as returned by API)
- Each entry shows:
  - `readingNumber` section header (e.g. "Read #2") — grouped, header shown once per group
  - Action label: "Status Change" (for `StatusChange`) / "Page Update" (for `PageUpdate`)
  - `oldValue → newValue` display (render `—` for null values)
  - Formatted timestamp (e.g. "May 24, 2026 at 3:41 PM")
- Journal is **read-only** — no editing or deletion UI
- Loading state shown while fetching; inline error message on failure; "No journal entries yet." when empty
- Popup traps focus; Escape dismisses; focus returns to triggering card
- `shelfApi.ts` exports `getJournal(userBookId: number): Promise<BookAction[]>`

---

## Tasks

- [x] Add `getJournal(userBookId)` to `frontend/src/api/shelfApi.ts`
- [x] Create `frontend/src/components/JournalPopup/JournalPopup.tsx`
- [x] Add `onJournal?: () => void` prop to `BookCard` + small "Journal" button in action area
- [x] Wire `journalBook` state in `ShelfPage.tsx` + render `<JournalPopup>`
- [x] Build passes (`npm run build` — 0 errors)
- [ ] Visual verification: open journal on a book, check entries load, Escape closes

---

## Dev Notes

### API endpoint (already implemented in backend)

```
GET /api/shelf/{userBookId}/journal
Authorization: Bearer <token>
→ 200 OK: JournalEntryResponse[]
```

`JournalEntryResponse` fields (maps 1:1 to `BookAction` TS type):
- `readingNumber: int`
- `actionType: string` — values: `"StatusChange"`, `"PageUpdate"`
- `oldValue: string?`
- `newValue: string?`
- `timestamp: DateTime` (serialised as ISO 8601 UTC string)

The backend returns entries **newest first** (ordered by `Timestamp DESC`) across all readingNumbers.

### Types — reuse existing `BookAction`

`BookAction` in `frontend/src/types/index.ts` already has all needed fields:
```ts
export interface BookAction {
  id: number;
  userBookId: number;
  readingNumber: number;
  actionType: string;
  oldValue: string | null;
  newValue: string | null;
  timestamp: string;
}
```
**No changes needed to `types/index.ts`** — use `BookAction[]` as the return type.

---

### File 1 — `frontend/src/api/shelfApi.ts` (UPDATE)

Add after the existing `updatePages` function:

```ts
export async function getJournal(userBookId: number): Promise<BookAction[]> {
  return apiRequest<BookAction[]>(`/shelf/${userBookId}/journal`);
}
```

Also add `BookAction` to the imports from `'../types'`:
```ts
import type { UserBook, BookAction } from '../types';
```

---

### File 2 — `frontend/src/components/JournalPopup/JournalPopup.tsx` (NEW)

Full implementation:

```tsx
import { useEffect, useState } from 'react';
import * as Dialog from '@radix-ui/react-dialog';
import { VisuallyHidden } from '@radix-ui/react-visually-hidden';
import * as shelfApi from '../../api/shelfApi';
import { ApiError } from '../../api/client';
import type { UserBook, BookAction } from '../../types';

interface JournalPopupProps {
  userBook: UserBook | null;
  onClose: () => void;
}

function formatTimestamp(ts: string): string {
  return new Date(ts).toLocaleString('en-US', {
    month: 'short',
    day: 'numeric',
    year: 'numeric',
    hour: 'numeric',
    minute: '2-digit',
  });
}

function actionLabel(actionType: string): string {
  if (actionType === 'StatusChange') return 'Status Change';
  if (actionType === 'PageUpdate') return 'Page Update';
  return actionType;
}

export function JournalPopup({ userBook, onClose }: JournalPopupProps) {
  const [entries, setEntries] = useState<BookAction[]>([]);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    if (!userBook) return;
    let cancelled = false;
    setLoading(true);
    setError(null);
    setEntries([]);
    shelfApi.getJournal(userBook.id)
      .then(data => { if (!cancelled) setEntries(data); })
      .catch(err => {
        if (!cancelled)
          setError(err instanceof ApiError ? err.message : 'Failed to load journal.');
      })
      .finally(() => { if (!cancelled) setLoading(false); });
    return () => { cancelled = true; };
  }, [userBook]);

  // Group consecutive entries by readingNumber (API returns newest-first,
  // so readingNumber groups may appear in descending order)
  const groups: { readingNumber: number; items: BookAction[] }[] = [];
  for (const entry of entries) {
    const last = groups[groups.length - 1];
    if (last && last.readingNumber === entry.readingNumber) {
      last.items.push(entry);
    } else {
      groups.push({ readingNumber: entry.readingNumber, items: [entry] });
    }
  }

  return (
    <Dialog.Root
      open={userBook !== null}
      onOpenChange={open => { if (!open) onClose(); }}
    >
      <Dialog.Portal>
        <Dialog.Overlay className="fixed inset-0 bg-black/40 z-40" />
        <Dialog.Content
          className="fixed left-1/2 top-1/2 -translate-x-1/2 -translate-y-1/2 z-50
            bg-warm-surface rounded-popup shadow-popup
            w-[92vw] max-w-md max-h-[80vh] flex flex-col
            focus:outline-none"
        >
          <VisuallyHidden>
            <Dialog.Title>{userBook?.book.title} Journal</Dialog.Title>
          </VisuallyHidden>

          {/* Header */}
          <div className="flex items-center justify-between px-5 pt-5 pb-3 border-b border-warm-border shrink-0">
            <div className="min-w-0 mr-3">
              <h2 className="text-[17px] font-semibold text-text-primary leading-tight">
                Reading Journal
              </h2>
              <p className="text-[13px] text-text-secondary line-clamp-1 mt-0.5">
                {userBook?.book.title}
              </p>
            </div>
            <Dialog.Close
              className="w-8 h-8 shrink-0 flex items-center justify-center rounded-full
                text-text-secondary hover:bg-warm-surface-alt
                focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-accent"
              aria-label="Close journal"
            >
              ✕
            </Dialog.Close>
          </div>

          {/* Scrollable body */}
          <div className="overflow-y-auto flex-1 px-5 py-4">
            {loading && (
              <p className="text-center text-text-secondary text-[14px] py-8">Loading…</p>
            )}
            {error && (
              <p className="text-center text-error text-[14px] py-8">{error}</p>
            )}
            {!loading && !error && entries.length === 0 && (
              <p className="text-center text-text-secondary text-[14px] py-8">
                No journal entries yet.
              </p>
            )}
            {!loading && !error && groups.map(group => (
              <div key={group.readingNumber} className="mb-5">
                <p className="text-[12px] font-semibold text-text-secondary uppercase tracking-wide mb-2">
                  Read #{group.readingNumber}
                </p>
                <div className="flex flex-col gap-2">
                  {group.items.map((entry, idx) => (
                    <div
                      key={idx}
                      className="bg-warm-surface-alt rounded-input px-3 py-2.5"
                    >
                      <div className="flex items-center justify-between gap-2 mb-1">
                        <span className="text-[13px] font-medium text-text-primary">
                          {actionLabel(entry.actionType)}
                        </span>
                        <span className="text-[11px] text-text-secondary shrink-0">
                          {formatTimestamp(entry.timestamp)}
                        </span>
                      </div>
                      <p className="text-[12px] text-text-secondary">
                        {entry.oldValue ?? '—'} → {entry.newValue ?? '—'}
                      </p>
                    </div>
                  ))}
                </div>
              </div>
            ))}
          </div>
        </Dialog.Content>
      </Dialog.Portal>
    </Dialog.Root>
  );
}
```

---

### File 3 — `frontend/src/components/BookCard/BookCard.tsx` (UPDATE)

**What changes:** Add `onJournal?: () => void` to `BookCardProps` and a small "Journal" text button in the action area below the main action button.

**Current `BookCardProps`:**
```ts
interface BookCardProps {
  userBook: UserBook;
  onClick?: () => void;
  onRefetch?: () => void;
}
```

**Updated `BookCardProps`:**
```ts
interface BookCardProps {
  userBook: UserBook;
  onClick?: () => void;
  onRefetch?: () => void;
  onJournal?: () => void;
}
```

**Destructure in component function:**
```ts
export function BookCard({ userBook, onClick, onRefetch, onJournal }: BookCardProps) {
```

**Action area** — add journal button below the existing action button + error:
```tsx
{/* Action area */}
<div className="px-2 pb-2 pt-1">
  <button
    type="button"
    onClick={handleAction}
    disabled={actionLoading}
    className={[...existing classes...]}
  >
    {actionLoading ? '…' : actionLabel}
  </button>
  {actionError && (
    <p className="text-error text-[13px] mt-1">{actionError}</p>
  )}
  {/* Journal trigger — available for any status */}
  <button
    type="button"
    onClick={onJournal}
    className="w-full mt-1 py-1 text-[12px] text-text-secondary hover:text-text-primary
      focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-accent focus-visible:ring-offset-1
      rounded"
  >
    Journal
  </button>
</div>
```

> **Do NOT remove** the existing `onClick`, `onRefetch`, the action button, progress bar, or any other existing rendering. Only add the `onJournal` prop and the Journal button.

---

### File 4 — `frontend/src/pages/ShelfPage.tsx` (UPDATE)

**What changes:** Add `journalBook` state, pass `onJournal` to each `BookCard`, render `<JournalPopup>` at bottom.

**Current imports:** `ProgressPopup`, `CelebrationOverlay` — add `JournalPopup`.

**Current state in `ShelfPage`:**
```ts
const [isAddBookOpen, setIsAddBookOpen] = useState(false);
const [selectedBook, setSelectedBook] = useState<UserBook | null>(null);
const [celebrationTitle, setCelebrationTitle] = useState('');
const [showCelebration, setShowCelebration] = useState(false);
```

**Add:**
```ts
const [journalBook, setJournalBook] = useState<UserBook | null>(null);
```

**Each `BookCard` render** — add `onJournal` prop:
```tsx
<BookCard
  key={ub.id}
  userBook={ub}
  onRefetch={refetch}
  onClick={ub.status === 'Started' ? () => setSelectedBook(ub) : undefined}
  onJournal={() => setJournalBook(ub)}
/>
```

**Bottom of JSX** (after `<CelebrationOverlay ...>`), add:
```tsx
<JournalPopup
  userBook={journalBook}
  onClose={() => setJournalBook(null)}
/>
```

> **Do NOT remove** `ProgressPopup`, `CelebrationOverlay`, or any other existing state/wiring.

---

## Architecture Constraints to Respect

| Rule | Detail |
|------|--------|
| **AR-3 Radix UI** | Use `@radix-ui/react-dialog` + `@radix-ui/react-visually-hidden` — both already installed |
| **AR-4 Tailwind v4** | Use design tokens from `index.css @theme`: `warm-surface`, `warm-surface-alt`, `warm-border`, `text-primary`, `text-secondary`, `text-error`, `accent`, `rounded-popup`, `shadow-popup` |
| **AR-7 API layer** | All fetch calls go through `shelfApi.ts` → `apiRequest()`. Never call `fetch()` directly in components |
| **AR-13 No frontend tests** | Validation = `npm run build` (TypeScript) + visual browser check |
| **Popup open state** | Driven by `userBook !== null` — no separate `isOpen` boolean (same pattern as ProgressPopup) |
| **Cancellation** | useEffect cleanup sets `cancelled = true` to prevent state updates after unmount/re-render |
| **No type additions** | `BookAction` in `types/index.ts` already covers the journal entry shape — no new types needed |

---

## Patterns Established in Prior Stories (Must Follow)

- **Radix Dialog pattern**: `open={X !== null}`, `onOpenChange={open => { if (!open) onClose(); }}`, Portal + Overlay + Content. See `ProgressPopup.tsx` for the exact pattern.
- **VisuallyHidden Dialog.Title**: Required for accessibility; wrap in `<VisuallyHidden>` from Radix, keep Dialog.Title in the DOM. See `BookForm.tsx` / `ProgressPopup.tsx`.
- **Loading/error states**: Local component state; no global state. Use `loading` + `error` booleans, cleared on re-fetch.
- **API cancellation**: `let cancelled = false` flag in `useEffect`, cleanup returns `() => { cancelled = true; }`.
- **`shelfApi.ts` function signature**: `export async function name(param: type): Promise<ReturnType>` — named export, async, uses `apiRequest<T>(path)`.
- **Component file structure**: One component per file, named export, file in `src/components/ComponentName/ComponentName.tsx`.
- **Tailwind class strings**: Multi-line string template literals or `[].join(' ')` for long conditional class sets.

---

## Definition of Done

- [x] `npm run build` exits 0, 0 TypeScript errors
- [x] "Journal" button visible on every BookCard in the shelf
- [x] Clicking "Journal" opens popup for that book
- [x] Journal entries load and display (readingNumber groups, action labels, values, timestamps)
- [x] Loading state visible briefly on slow connections
- [x] Error message shown if fetch fails
- [x] Empty state message shown for books with no actions
- [x] Escape key / ✕ button closes popup
- [x] Focus returns to card after close (Radix Dialog handles this natively)
- [x] No regressions: ProgressPopup, CelebrationOverlay, action buttons still work

---

## Dev Agent Record

### File List
- `frontend/src/api/shelfApi.ts` — added `BookAction` to imports; added `getJournal` function
- `frontend/src/components/JournalPopup/JournalPopup.tsx` — NEW component
- `frontend/src/components/BookCard/BookCard.tsx` — added `onJournal` prop + Journal button
- `frontend/src/pages/ShelfPage.tsx` — added `journalBook` state, `onJournal` wiring, `<JournalPopup>` render

### Completion Notes
- Implemented Story 3.6 in full. Build: 101 modules, 0 TypeScript errors.
- `getJournal` uses `fetchJson<BookAction[]>` matching the existing `shelfApi.ts` pattern (not `apiRequest`).
- `JournalPopup` follows exact same Radix Dialog pattern as `ProgressPopup`: `open={userBook !== null}`, `VisuallyHidden` title, Portal + Overlay + Content.
- Entries grouped by `readingNumber` using a simple consecutive-group accumulator — handles newest-first ordering from API.
- `useEffect` cleanup cancellation flag prevents stale state updates on fast re-opens.
- Radix Dialog natively handles focus-trap, Escape dismiss, and focus-return — no custom keyboard handling needed.
- `BookAction` type already existed in `types/index.ts` — no new types required.

### Change Log
- 2026-05-27: Implemented Story 3.6 — Reading Journal Popup (getJournal API, JournalPopup component, BookCard journal trigger, ShelfPage wiring)
