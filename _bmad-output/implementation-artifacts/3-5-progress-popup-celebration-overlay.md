# Story 3.5: Progress Popup & Celebration Overlay

Status: review

## Story

As a **reader with a Started book**,
I want to update my page count through a popup stepper and feel rewarded when I finish,
So that logging progress is fast and reaching the last page feels like an achievement.

## Acceptance Criteria

1. **Given** user taps a `BookCard` with `Status = Started` → `ProgressPopup` opens (Radix UI Dialog): slides up from bottom on mobile / centred on desktop; shows book title, cover thumbnail, and `PageStepper` pre-loaded with `currentPages`
2. **And** tapping a `BookCard` with any status other than `Started` does NOT open `ProgressPopup` (tap is a no-op for the popup)
3. **And** `PageStepper` renders `−` / `+` buttons and a direct numeric input; all clamp to `[0, totalPages]`; the "Update" button is **disabled** when the displayed value equals the pre-loaded `currentPages`
4. **When** user taps "Update" → `shelfApi.updatePages(userBookId, newPages)` is called; on HTTP 200 with `status !== 'Finished'` → popup closes; shelf refetches; progress strip animates to new fill
5. **When** the API response has `status === 'Finished'` (auto-finish triggered) → popup closes; `CelebrationOverlay` fires — warm amber banner at bottom, not full-screen takeover; auto-dismisses after 3 seconds or on tap; shelf refetches; book ribbon transitions to Finished (soft sage) via CSS transition
6. **When** the API call fails → popup stays open with an inline error message; user can retry; no local state mutated
7. **And** `ProgressPopup` traps focus (Radix Dialog); Escape key dismisses; focus returns to the triggering element on close
8. **And** `CelebrationOverlay` requires no user interaction to dismiss — app is fully usable during the 3-second display
9. **And** `shelfApi.ts` exports `updatePages(userBookId: number, pages: number): Promise<UserBook>` calling `PATCH /api/shelf/{userBookId}/pages` with body `{ pages }`

## Tasks / Subtasks

- [x] Task 1: Add `updatePages` to `shelfApi.ts` (AC: 9)
  - [x] Add `export const updatePages = (userBookId: number, pages: number) => fetchJson<UserBook>(\`/api/shelf/\${userBookId}/pages\`, { method: 'PATCH', body: JSON.stringify({ pages }) });` to `frontend/src/api/shelfApi.ts`

- [x] Task 2: Create `PageStepper` component (AC: 3)
  - [x] Create `frontend/src/components/PageStepper/PageStepper.tsx`
  - [x] Props: `value: number`, `totalPages: number`, `onChange: (n: number) => void`
  - [x] `−` button: decrements by 1, clamp to 0
  - [x] `+` button: increments by 1, clamp to `totalPages`
  - [x] Direct numeric `<input type="number">`: clamp on blur; empty string during typing is allowed (treat as 0 on submit)
  - [x] Both buttons and input are `min-h-[44px]` for touch targets
  - [x] See Dev Notes for full implementation

- [x] Task 3: Create `ProgressPopup` component (AC: 1, 3, 4, 5, 6, 7)
  - [x] Create `frontend/src/components/ProgressPopup/ProgressPopup.tsx`
  - [x] Uses `@radix-ui/react-dialog` (already installed) — same pattern as `BookForm.tsx`
  - [x] Props: `userBook: UserBook | null`, `onClose: () => void`, `onFinished: (title: string) => void`, `onRefetch: () => void`
  - [x] `open` derived as `userBook !== null`
  - [x] Shows cover thumbnail (40×60 px, `object-cover`) or placeholder SVG
  - [x] Shows book title and `PageStepper` pre-loaded with `userBook.currentPages`
  - [x] "Update" button disabled when `pageValue === userBook.currentPages`
  - [x] On success with `status !== 'Finished'`: call `onClose()` then `onRefetch()`
  - [x] On success with `status === 'Finished'`: call `onClose()`, then `onRefetch()`, then `onFinished(book.title)`
  - [x] On error: show inline error, do NOT close popup
  - [x] See Dev Notes for full implementation

- [x] Task 4: Create `CelebrationOverlay` component (AC: 5, 8)
  - [x] Create `frontend/src/components/CelebrationOverlay/CelebrationOverlay.tsx`
  - [x] Props: `visible: boolean`, `bookTitle: string`, `onDismiss: () => void`
  - [x] Fixed position bottom banner (`position: fixed; bottom: 0; left: 0; right: 0`)
  - [x] Background: `bg-celebration`, text white
  - [x] Shows "🎉 Finished!" heading and book title
  - [x] `useEffect` auto-dismisses after 3000 ms when `visible` becomes true
  - [x] Tapping the banner calls `onDismiss()` immediately
  - [x] Slide-up CSS transition (`transform: translateY`) when visible
  - [x] See Dev Notes for full implementation

- [x] Task 5: Wire `ShelfPage` — state + popup + overlay (AC: 1, 2, 4, 5, 7, 8)
  - [x] Add `const [selectedBook, setSelectedBook] = useState<UserBook | null>(null)` to `ShelfPage`
  - [x] Add `const [celebrationBook, setCelebrationBook] = useState<string | null>(null)` (stores title of finished book, or null)
  - [x] Pass `onClick={ub.status === 'Started' ? () => setSelectedBook(ub) : undefined}` to each `BookCard`
  - [x] Render `<ProgressPopup>` and `<CelebrationOverlay>` below `<BookForm>` in `ShelfPage`
  - [x] See Dev Notes for exact wiring

- [x] Task 6: Build verification (AC: all)
  - [x] `npm run build` passes with 0 TypeScript errors
  - [x] Visual check: tap a Started card → popup opens pre-loaded with current page
  - [x] Increment pages, tap Update → popup closes, strip animates
  - [x] Set pages to `totalPages`, tap Update → popup closes, celebration banner slides up, auto-dismisses after 3s
  - [x] Tap banner before 3s → dismisses immediately

## Dev Notes

### Architecture Summary

- **4 new files** — `PageStepper.tsx`, `ProgressPopup.tsx`, `CelebrationOverlay.tsx` + one new export in `shelfApi.ts`
- **2 updated files** — `ShelfPage.tsx` (state + wiring), `shelfApi.ts` (new export)
- `BookCard.tsx` is NOT modified — `onClick` prop is already wired through the clickable upper div from Story 3.4
- Radix Dialog (`@radix-ui/react-dialog`) already installed; follow exact `BookForm.tsx` pattern
- No new npm packages needed

### Tailwind v4 Design Tokens (from `src/index.css` `@theme` block)

```
bg-warm-bg       #FAF6F0    bg-warm-surface       #FFFFFF
bg-warm-surface-alt #F3EEE7  bg-warm-border        #E2D9CE
bg-accent        #6B7555    hover:bg-accent-hover  #556044
text-primary     #1C1A18    text-secondary         #6B6259
text-disabled    #ADA49A    text-error             #A84040
bg-error-bg      #FDF0EF    bg-celebration         #C4874A
rounded-popup    16px       rounded-button         8px
shadow-popup     0 8px 32px rgba(0,0,0,0.16)
```

### Task 1 — `shelfApi.ts` addition

Add at end of file:

```typescript
export const updatePages = (userBookId: number, pages: number) =>
  fetchJson<UserBook>(`/api/shelf/${userBookId}/pages`, {
    method: 'PATCH',
    body: JSON.stringify({ pages }),
  });
```

Backend endpoint: `PATCH /api/shelf/{userBookId}/pages` — body `{ "pages": number }` — returns `UserBookResponse` (matches `UserBook` TS type). Auto-finishes when `pages === totalPages`, returning `status: "Finished"`.

### Task 2 — Full `PageStepper.tsx`

```tsx
interface PageStepperProps {
  value: number;
  totalPages: number;
  onChange: (n: number) => void;
}

export function PageStepper({ value, totalPages, onChange }: PageStepperProps) {
  const [inputStr, setInputStr] = useState(String(value));

  // Sync when the parent resets (popup reopens with a different book)
  useEffect(() => {
    setInputStr(String(value));
  }, [value]);

  function clamp(n: number) {
    return Math.min(Math.max(0, n), totalPages);
  }

  function commitStr(str: string) {
    const parsed = parseInt(str, 10);
    const clamped = clamp(isNaN(parsed) ? 0 : parsed);
    setInputStr(String(clamped));
    onChange(clamped);
  }

  return (
    <div className="flex items-center gap-2">
      <button
        type="button"
        onClick={() => { const n = clamp(value - 1); setInputStr(String(n)); onChange(n); }}
        disabled={value <= 0}
        className="w-11 h-11 flex items-center justify-center rounded-button border border-warm-border text-text-primary text-[20px] disabled:opacity-40 hover:bg-warm-surface-alt transition-colors focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-accent"
        aria-label="Decrease page count"
      >
        −
      </button>

      <div className="flex-1 text-center">
        <input
          type="number"
          min={0}
          max={totalPages}
          value={inputStr}
          onChange={e => setInputStr(e.target.value)}
          onBlur={e => commitStr(e.target.value)}
          onKeyDown={e => { if (e.key === 'Enter') commitStr(inputStr); }}
          className="w-full text-center border border-warm-border rounded-input px-2 py-2 text-[18px] font-semibold text-text-primary bg-warm-surface focus:outline-none focus:ring-2 focus:ring-accent min-h-[44px]"
          aria-label="Current page"
        />
        <p className="text-[12px] text-text-secondary mt-1">of {totalPages} pages</p>
      </div>

      <button
        type="button"
        onClick={() => { const n = clamp(value + 1); setInputStr(String(n)); onChange(n); }}
        disabled={value >= totalPages}
        className="w-11 h-11 flex items-center justify-center rounded-button border border-warm-border text-text-primary text-[20px] disabled:opacity-40 hover:bg-warm-surface-alt transition-colors focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-accent"
        aria-label="Increase page count"
      >
        +
      </button>
    </div>
  );
}
```

Add `import { useState, useEffect } from 'react';` at the top.

### Task 3 — Full `ProgressPopup.tsx`

```tsx
import { useState, useEffect } from 'react';
import * as Dialog from '@radix-ui/react-dialog';
import { VisuallyHidden } from '@radix-ui/react-visually-hidden';
import { PageStepper } from '../PageStepper/PageStepper';
import * as shelfApi from '../../api/shelfApi';
import { ApiError } from '../../api/client';
import type { UserBook } from '../../types';

interface ProgressPopupProps {
  userBook: UserBook | null;
  onClose: () => void;
  onFinished: (title: string) => void;
  onRefetch: () => void;
}

export function ProgressPopup({ userBook, onClose, onFinished, onRefetch }: ProgressPopupProps) {
  const [pageValue, setPageValue] = useState(0);
  const [isSubmitting, setIsSubmitting] = useState(false);
  const [error, setError] = useState<string | null>(null);

  // Reset local state whenever popup opens for a (possibly different) book
  useEffect(() => {
    if (userBook) {
      setPageValue(userBook.currentPages);
      setError(null);
    }
  }, [userBook]);

  async function handleUpdate() {
    if (!userBook) return;
    setError(null);
    setIsSubmitting(true);
    try {
      const updated = await shelfApi.updatePages(userBook.id, pageValue);
      onClose();
      onRefetch();
      if (updated.status === 'Finished') {
        onFinished(userBook.book.title);
      }
    } catch (err) {
      setError(err instanceof ApiError ? err.message : 'Something went wrong. Please try again.');
    } finally {
      setIsSubmitting(false);
    }
  }

  const isDirty = userBook !== null && pageValue !== userBook.currentPages;

  return (
    <Dialog.Root open={userBook !== null} onOpenChange={open => { if (!open) onClose(); }}>
      <Dialog.Portal>
        <Dialog.Overlay className="fixed inset-0 bg-black/40 z-50 animate-in fade-in-0" />
        <Dialog.Content
          className="fixed left-0 right-0 bottom-0 sm:left-1/2 sm:top-1/2 sm:bottom-auto sm:-translate-x-1/2 sm:-translate-y-1/2 sm:w-full sm:max-w-md bg-warm-surface rounded-t-popup sm:rounded-popup shadow-popup z-50 p-6 focus:outline-none"
          aria-describedby={undefined}
        >
          <Dialog.Title asChild>
            <VisuallyHidden>Update reading progress</VisuallyHidden>
          </Dialog.Title>

          {userBook && (
            <>
              {/* Header row */}
              <div className="flex items-start justify-between mb-5 gap-3">
                <div className="flex items-start gap-3">
                  {/* Cover thumbnail */}
                  <div className="w-10 h-[60px] flex-shrink-0 overflow-hidden rounded bg-warm-surface-alt">
                    {userBook.book.coverImageUrl ? (
                      <img
                        src={userBook.book.coverImageUrl}
                        alt={userBook.book.title}
                        className="w-full h-full object-cover"
                      />
                    ) : (
                      <div className="w-full h-full flex items-center justify-center">
                        <svg width="20" height="20" viewBox="0 0 20 20" fill="none" aria-hidden="true">
                          <rect x="3" y="2" width="14" height="16" rx="1.5" fill="#E2D9CE"/>
                        </svg>
                      </div>
                    )}
                  </div>
                  <div>
                    <h2 className="text-[17px] font-semibold text-text-primary leading-[1.35] line-clamp-2">
                      {userBook.book.title}
                    </h2>
                    <p className="text-[13px] text-text-secondary mt-0.5">{userBook.book.author}</p>
                  </div>
                </div>
                <Dialog.Close className="text-text-secondary hover:text-text-primary transition-colors p-1 rounded flex-shrink-0 focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-accent">
                  <svg width="20" height="20" viewBox="0 0 20 20" fill="none" aria-hidden="true">
                    <path d="M5 5L15 15M15 5L5 15" stroke="currentColor" strokeWidth="1.5" strokeLinecap="round"/>
                  </svg>
                  <span className="sr-only">Close</span>
                </Dialog.Close>
              </div>

              {/* Page stepper */}
              <div className="mb-5">
                <p className="text-[13px] font-medium text-text-secondary mb-3">Current page</p>
                <PageStepper
                  value={pageValue}
                  totalPages={userBook.book.totalPages}
                  onChange={setPageValue}
                />
              </div>

              {/* Error */}
              {error && (
                <div role="alert" className="bg-error-bg text-error text-sm rounded px-4 py-3 mb-4">
                  {error}
                </div>
              )}

              {/* Update button */}
              <button
                type="button"
                onClick={handleUpdate}
                disabled={!isDirty || isSubmitting}
                className="w-full bg-accent hover:bg-accent-hover disabled:opacity-50 disabled:cursor-not-allowed text-white py-3 rounded-button text-[15px] font-medium transition-colors min-h-[44px] focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-accent focus-visible:ring-offset-2"
              >
                {isSubmitting ? 'Updating…' : 'Update'}
              </button>
            </>
          )}
        </Dialog.Content>
      </Dialog.Portal>
    </Dialog.Root>
  );
}
```

### Task 4 — Full `CelebrationOverlay.tsx`

```tsx
import { useEffect } from 'react';

interface CelebrationOverlayProps {
  visible: boolean;
  bookTitle: string;
  onDismiss: () => void;
}

export function CelebrationOverlay({ visible, bookTitle, onDismiss }: CelebrationOverlayProps) {
  useEffect(() => {
    if (!visible) return;
    const timer = setTimeout(onDismiss, 3000);
    return () => clearTimeout(timer);
  }, [visible, onDismiss]);

  return (
    <div
      role="status"
      aria-live="polite"
      onClick={onDismiss}
      className="fixed bottom-0 left-0 right-0 z-[60] cursor-pointer transition-transform duration-300"
      style={{ transform: visible ? 'translateY(0)' : 'translateY(100%)' }}
    >
      <div className="bg-celebration text-white px-6 py-5 flex items-center gap-4">
        <span className="text-[32px] leading-none select-none" aria-hidden="true">🎉</span>
        <div>
          <p className="text-[17px] font-semibold leading-tight">You finished reading!</p>
          <p className="text-[14px] opacity-90 mt-0.5 line-clamp-1">{bookTitle}</p>
        </div>
      </div>
    </div>
  );
}
```

**Why `z-[60]`**: `ProgressPopup` uses `z-50` (same as `BookForm`). `CelebrationOverlay` renders after the popup closes, so there's no stacking conflict. `z-[60]` is a one-off value — no Tailwind config change needed in v4.

**Why not `aria-modal`**: The overlay is non-blocking intentionally — users can interact with the app behind it. `role="status" aria-live="polite"` announces the completion to screen readers without trapping focus.

### Task 5 — `ShelfPage.tsx` wiring

```tsx
// Add these imports:
import { ProgressPopup } from '../components/ProgressPopup/ProgressPopup';
import { CelebrationOverlay } from '../components/CelebrationOverlay/CelebrationOverlay';
import type { UserBook } from '../types';

// Inside ShelfPage():
const [selectedBook, setSelectedBook] = useState<UserBook | null>(null);
const [celebrationTitle, setCelebrationTitle] = useState('');
const [showCelebration, setShowCelebration] = useState(false);
```

Pass to each `BookCard`:
```tsx
onClick={ub.status === 'Started' ? () => setSelectedBook(ub) : undefined}
```

Add below `<BookForm ...>`:
```tsx
<ProgressPopup
  userBook={selectedBook}
  onClose={() => setSelectedBook(null)}
  onFinished={(title) => {
    setCelebrationTitle(title);
    setShowCelebration(true);
  }}
  onRefetch={refetch}
/>

<CelebrationOverlay
  visible={showCelebration}
  bookTitle={celebrationTitle}
  onDismiss={() => setShowCelebration(false)}
/>
```

**Full `ShelfPage.tsx` after changes:**

```tsx
import { useState } from 'react';
import { useShelf } from '../hooks/useShelf';
import { BookCard } from '../components/BookCard/BookCard';
import { StatsStrip } from '../components/StatsStrip/StatsStrip';
import { EmptyState } from '../components/EmptyState/EmptyState';
import { BookForm } from '../components/BookForm/BookForm';
import { ProgressPopup } from '../components/ProgressPopup/ProgressPopup';
import { CelebrationOverlay } from '../components/CelebrationOverlay/CelebrationOverlay';
import type { UserBook } from '../types';

export function ShelfPage() {
  const { shelf, loading, error, refetch } = useShelf();
  const [isAddBookOpen, setIsAddBookOpen] = useState(false);
  const [selectedBook, setSelectedBook] = useState<UserBook | null>(null);
  const [celebrationTitle, setCelebrationTitle] = useState('');
  const [showCelebration, setShowCelebration] = useState(false);

  return (
    <div className="bg-warm-bg min-h-screen">
      <StatsStrip />

      <div className="flex items-center justify-between px-4 sm:px-6 lg:px-8 pt-6 pb-2">
        <h1 className="text-[22px] font-semibold text-text-primary">My Shelf</h1>
        <button
          type="button"
          onClick={() => setIsAddBookOpen(true)}
          className="bg-accent hover:bg-accent-hover text-white px-4 py-2 rounded-button text-[15px] font-medium min-h-[44px] transition-colors focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-accent focus-visible:ring-offset-2"
        >
          + Add Book
        </button>
      </div>

      {loading && (
        <p className="text-text-secondary text-center py-12">Loading your shelf…</p>
      )}

      {error && (
        <div className="mx-4 sm:mx-6 mt-4 bg-error-bg text-error text-sm rounded px-4 py-3">{error}</div>
      )}

      {!loading && !error && shelf.length === 0 && (
        <EmptyState onAddBook={() => setIsAddBookOpen(true)} />
      )}

      {!loading && !error && shelf.length > 0 && (
        <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-3 gap-4 lg:gap-6 px-4 sm:px-6 lg:px-8 pb-6 max-w-[1200px] mx-auto">
          {shelf.map((ub) => (
            <BookCard
              key={ub.id}
              userBook={ub}
              onRefetch={refetch}
              onClick={ub.status === 'Started' ? () => setSelectedBook(ub) : undefined}
            />
          ))}
        </div>
      )}

      <BookForm
        isOpen={isAddBookOpen}
        onOpenChange={setIsAddBookOpen}
        onSuccess={() => {
          setIsAddBookOpen(false);
          refetch();
        }}
      />

      <ProgressPopup
        userBook={selectedBook}
        onClose={() => setSelectedBook(null)}
        onFinished={(title) => {
          setCelebrationTitle(title);
          setShowCelebration(true);
        }}
        onRefetch={refetch}
      />

      <CelebrationOverlay
        visible={showCelebration}
        bookTitle={celebrationTitle}
        onDismiss={() => setShowCelebration(false)}
      />
    </div>
  );
}
```

### Key Design Decisions

**Why `BookCard` is not modified:** `onClick` prop already wires through to the clickable upper `<div>` (Story 3.4). `ShelfPage` simply passes or withholds the callback based on status.

**Why popup uses `userBook !== null` as `open`:** Avoids a separate `isOpen` boolean that could drift out of sync. When `onClose` sets `selectedBook` to `null`, the Dialog closes via Radix.

**Why `onRefetch` + `onFinished` are separate:** `onFinished` triggers the celebration *after* the popup closes and shelf refetches. Merging them would require complex sequencing.

**`CelebrationOverlay` stays mounted:** `visible` prop drives `translateY` — the component is always in the DOM (no mount/unmount flicker). `useEffect` timers only run when `visible` is true.

**Why `celebrationTitle` persists after dismiss:** `setCelebrationTitle` is not cleared on dismiss — the banner slides out with the title still rendered. This avoids a "flash to empty" during the slide-out animation.

---

## Dev Agent Record

### Agent Model Used

claude-sonnet-4.6

### Debug Log References

### Completion Notes List

Story 3.5 implemented. Build passes (100 modules, 0 TypeScript errors).

- `shelfApi.ts`: added `updatePages` (PATCH /api/shelf/{id}/pages)
- `PageStepper.tsx`: NEW — `−`/`+` buttons + numeric input, clamped to [0, totalPages], `useEffect` syncs with parent value on popup re-open
- `ProgressPopup.tsx`: NEW — Radix Dialog (same pattern as BookForm), slides up mobile / centered desktop; pre-loads currentPages; "Update" disabled when unchanged; handles finish detection; inline error on failure
- `CelebrationOverlay.tsx`: NEW — fixed bottom banner, `bg-celebration`, slides up via `translateY` CSS transition, `useEffect` auto-dismisses after 3s, tap to dismiss early
- `ShelfPage.tsx`: added `selectedBook`, `celebrationTitle`, `showCelebration` state; passes `onClick` only for Started cards; renders `ProgressPopup` + `CelebrationOverlay`

### File List

- frontend/src/api/shelfApi.ts
- frontend/src/components/PageStepper/PageStepper.tsx
- frontend/src/components/ProgressPopup/ProgressPopup.tsx
- frontend/src/components/CelebrationOverlay/CelebrationOverlay.tsx
- frontend/src/pages/ShelfPage.tsx
