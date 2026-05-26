# Story 2.6: Add Book Flow (Frontend)

Status: review

## Story

As an **authenticated reader**,
I want to add a book by ISBN through a modal on the Shelf,
So that I can catalog new books and see them appear immediately on my shelf.

## Acceptance Criteria

1. User taps "Add Book" (EmptyState CTA or page-header button on a non-empty shelf) → `BookForm` modal opens via Radix UI Dialog with focus trapped inside
2. User enters an ISBN and submits the lookup step → `booksApi.lookupISBN(isbn)` called (`GET /api/books/{isbn}`); if a `Book` is returned the form pre-fills title, author, totalPages, coverImageUrl (all fields remain editable); genre dropdown stays empty (user must select)
3. When lookup returns `null` (Open Library miss or unreachable) → empty editable form shown immediately with no blocking error — user fills all fields manually
4. User completes the form and confirms → `booksApi.createBook(dto)` called (`POST /api/books`), then `shelfApi.addToShelf(bookId)` called (`POST /api/shelf`); modal closes; shelf re-fetches and new Resting card appears at top
5. Genre is a `<select>` constrained to 10 predefined genres (Fiction, Non-Fiction, Mystery, Science Fiction, Fantasy, Romance, Biography & Memoir, History, Self-Help, Other); free text not permitted; genre is required
6. Form validation fires on `blur` (not `onChange`): title and author non-empty, totalPages is a positive integer, genre is selected — friendly inline messages per field
7. Submit button disabled until all required fields pass validation; a loading state is shown while API calls are in-flight
8. API errors display as an inline banner inside the modal; modal stays open on error
9. `booksApi.ts` (`src/api/booksApi.ts`) exports `lookupISBN(isbn: string): Promise<Book | null>` and `createBook(dto: CreateBookDto): Promise<Book>`
10. `shelfApi.ts` exports `addToShelf(bookId: number): Promise<UserBook>` in addition to existing `getShelf()`
11. `useShelf` hook returns `refetch()` in addition to `{ shelf, loading, error }` so `ShelfPage` can trigger a re-fetch after a successful add

## Tasks / Subtasks

- [x] Task 1: Create `booksApi.ts` and extend `shelfApi.ts` (AC: 9, 10)
  - [x] Create `src/api/booksApi.ts`:
    - `lookupISBN(isbn: string): Promise<Book | null>` → `fetchJson<Book | null>(\`/api/books/${encodeURIComponent(isbn)}\`)`
    - `interface CreateBookDto { isbn: string; title: string; author: string; totalPages: number; genre: string; coverImageUrl?: string | null }`
    - `createBook(dto: CreateBookDto): Promise<Book>` → `fetchJson<Book>('/api/books', { method: 'POST', body: JSON.stringify(dto) })`
  - [x] Extend `src/api/shelfApi.ts` — add: `addToShelf(bookId: number): Promise<UserBook>` → `fetchJson<UserBook>('/api/shelf', { method: 'POST', body: JSON.stringify({ bookId }) })`

- [x] Task 2: Add `refetch` to `useShelf` hook (AC: 11)
  - [x] Update `src/hooks/useShelf.ts`:
    - Add `const [fetchCount, setFetchCount] = useState(0)` trigger state
    - Change `useEffect` dependency array to `[fetchCount]`
    - Add `setLoading(true); setError(null)` at start of useEffect body (before the fetch) to reset state on refetch
    - Add `const refetch = useCallback(() => setFetchCount(n => n + 1), [])` (import `useCallback`)
    - Return `{ shelf, loading, error, refetch }`

- [x] Task 3: Create `BookForm` modal component (AC: 1, 2, 3, 5, 6, 7, 8)
  - [x] Create `src/components/BookForm/BookForm.tsx`
  - [x] Use `@radix-ui/react-dialog` for the Dialog shell; `@radix-ui/react-visually-hidden` for accessible title
  - [x] Two-step internal state: `step: 'isbn' | 'form'`
  - [x] **ISBN step**: single `<input>` for ISBN, "Look Up" button (calls `lookupISBN`); show loading spinner on button while in-flight; on success transition to `'form'` step with pre-filled data; on null result transition to empty `'form'` step
  - [x] **Form step**: fields — title (text input), author (text input), totalPages (number input, min=1), genre (`<select>` with 10 predefined options + empty default option), coverImageUrl shown as read-only text (only when pre-filled from lookup; hidden when null)
  - [x] **Validation** — validate each field on `blur`; track `touched` state per field; inline error messages shown only for touched + invalid fields; validation function:
    - title: `value.trim().length > 0` — error: "Title is required"
    - author: `value.trim().length > 0` — error: "Author is required"
    - totalPages: `Number.isInteger(+value) && +value > 0` — error: "Must be a positive number"
    - genre: `value !== ''` — error: "Please select a genre"
  - [x] Submit button: `disabled` while `!isFormValid || isSubmitting`
  - [x] On submit: set `isSubmitting = true`; call `createBook(dto)`, then `addToShelf(book.id)`, then call `onSuccess()` and close dialog; on any error set `apiError` string and keep modal open; clear `apiError` when user modifies a field
  - [x] API error banner: `<div role="alert" className="bg-error-bg text-error text-sm rounded px-4 py-3 mb-4">` shown when `apiError` is set
  - [x] Close/cancel: Escape key (Radix default) and explicit "Back" button; clicking outside is prevented during submit via `onInteractOutside`
  - [x] On modal close (`onOpenChange(false)`), reset all state (step back to `'isbn'`, clear fields, clear errors)
  - [x] Component signature: `BookForm({ isOpen, onOpenChange, onSuccess }: BookFormProps)`

- [x] Task 4: Wire `ShelfPage` and add "Add Book" trigger (AC: 1, 4)
  - [x] Update `src/pages/ShelfPage.tsx`:
    - Import `BookForm`
    - Add `const [isAddBookOpen, setIsAddBookOpen] = useState(false)`
    - Destructure `refetch` from `useShelf()`
    - Pass `onAddBook={() => setIsAddBookOpen(true)}` to `<EmptyState>` (replacing the no-op)
    - Add an "Add Book" button in the page header area (visible always)
    - Render `<BookForm isOpen={isAddBookOpen} onOpenChange={setIsAddBookOpen} onSuccess={() => { setIsAddBookOpen(false); refetch(); }} />`

- [x] Task 5: Verify frontend builds without errors (AC: all)
  - [x] Run `npm run build` from `frontend/`
  - [x] Zero TypeScript errors, zero build errors ✅ (97 modules, exit code 0)

## Dev Notes

### Radix UI Dialog — Installed & Import Pattern

Radix UI Dialog (`@radix-ui/react-dialog` v1.1.15) and VisuallyHidden (`@radix-ui/react-visually-hidden` v1.2.4) are already installed (see `package.json`). Zero visual output from Radix — all styling is ours.

```tsx
import * as Dialog from '@radix-ui/react-dialog';
import { VisuallyHidden } from '@radix-ui/react-visually-hidden';
```

Minimal Radix Dialog shell:

```tsx
<Dialog.Root open={isOpen} onOpenChange={onOpenChange}>
  <Dialog.Portal>
    <Dialog.Overlay className="fixed inset-0 bg-black/40 z-50" />
    <Dialog.Content
      className="fixed left-1/2 top-1/2 -translate-x-1/2 -translate-y-1/2 w-full max-w-md bg-warm-surface rounded-popup shadow-popup z-50 p-6 focus:outline-none"
      onInteractOutside={(e) => e.preventDefault()} /* keep open on outside click during submit */
    >
      <Dialog.Title asChild>
        <VisuallyHidden>Add a Book</VisuallyHidden>
      </Dialog.Title>
      {/* content */}
    </Dialog.Content>
  </Dialog.Portal>
</Dialog.Root>
```

**Mobile overlay:** On mobile (< 640px) the modal should slide up from the bottom. Use:
```tsx
// Dialog.Content mobile variant:
"sm:top-1/2 sm:-translate-y-1/2 bottom-0 sm:bottom-auto top-auto sm:translate-y-[-50%] w-full sm:max-w-md rounded-t-popup sm:rounded-popup"
```

### `booksApi.ts` Full Implementation

```tsx
import { fetchJson } from './client';
import type { Book } from '../types';

export interface CreateBookDto {
  isbn: string;
  title: string;
  author: string;
  totalPages: number;
  genre: string;
  coverImageUrl?: string | null;
}

export const lookupISBN = (isbn: string) =>
  fetchJson<Book | null>(`/api/books/${encodeURIComponent(isbn)}`);

export const createBook = (dto: CreateBookDto) =>
  fetchJson<Book>('/api/books', { method: 'POST', body: JSON.stringify(dto) });
```

### `shelfApi.ts` Updated

```tsx
import { fetchJson } from './client';
import type { UserBook } from '../types';

export const getShelf = () => fetchJson<UserBook[]>('/api/shelf');

export const addToShelf = (bookId: number) =>
  fetchJson<UserBook>('/api/shelf', { method: 'POST', body: JSON.stringify({ bookId }) });
```

### `useShelf` Updated (with `refetch`)

```tsx
import { useState, useEffect, useCallback } from 'react';
import { getShelf } from '../api/shelfApi';
import { ApiError } from '../api/client';
import type { UserBook } from '../types';

export function useShelf() {
  const [shelf, setShelf] = useState<UserBook[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [fetchCount, setFetchCount] = useState(0);

  useEffect(() => {
    setLoading(true);
    setError(null);
    getShelf()
      .then(setShelf)
      .catch((err) => setError(err instanceof ApiError ? err.message : 'Failed to load shelf'))
      .finally(() => setLoading(false));
  }, [fetchCount]);

  const refetch = useCallback(() => setFetchCount(n => n + 1), []);

  return { shelf, loading, error, refetch };
}
```

### Genre List (Exact Values — Must Match Backend)

```ts
const GENRES = [
  'Fiction',
  'Non-Fiction',
  'Mystery',
  'Science Fiction',
  'Fantasy',
  'Romance',
  'Biography & Memoir',
  'History',
  'Self-Help',
  'Other',
] as const;
```

These must match **exactly** what the backend validates against (Story 2.3 AC). `Biography & Memoir` with ampersand is the exact value.

### Form Validation Pattern (Blur-Based)

```tsx
const [touched, setTouched] = useState({ title: false, author: false, totalPages: false, genre: false });

const errors = {
  title: !formData.title.trim() ? 'Title is required' : '',
  author: !formData.author.trim() ? 'Author is required' : '',
  totalPages: (!formData.totalPages || !Number.isInteger(+formData.totalPages) || +formData.totalPages < 1)
    ? 'Must be a positive number' : '',
  genre: !formData.genre ? 'Please select a genre' : '',
};

const isFormValid = Object.values(errors).every(e => e === '');

// Field example:
<input
  value={formData.title}
  onChange={e => { setFormData(...); setApiError(''); }}
  onBlur={() => setTouched(t => ({ ...t, title: true }))}
  className={`...base classes... ${touched.title && errors.title ? 'border-error' : 'border-warm-border'}`}
/>
{touched.title && errors.title && (
  <p className="text-error text-[13px] mt-1">{errors.title}</p>
)}
```

### `totalPages` Input Note

Store `totalPages` as a string in form state (easier controlled input), convert to `number` only on submit:
```ts
const dto: CreateBookDto = {
  isbn: formData.isbn,
  title: formData.title.trim(),
  author: formData.author.trim(),
  totalPages: parseInt(formData.totalPages, 10),
  genre: formData.genre,
  coverImageUrl: formData.coverImageUrl || null,
};
```

### Two-Step Modal Flow

```
Step 'isbn':
  [ISBN input field]
  [Look Up button] → loading state while fetching
      ↓ success (book found or null)
Step 'form':
  [title, author, totalPages, genre, coverImageUrl(read-only if set)]
  [Submit button] → disabled unless valid
  [Back button] → returns to 'isbn' step, clears prefill
```

The "Back" button is optional but helpful UX — resets to ISBN step so user can try a different ISBN.

### State Reset on Close

When `onOpenChange(false)` fires (Escape, or programmatic close after success), reset:
```ts
const resetState = () => {
  setStep('isbn');
  setIsbnInput('');
  setFormData({ title: '', author: '', totalPages: '', genre: '', coverImageUrl: '' });
  setTouched({ title: false, author: false, totalPages: false, genre: false });
  setApiError('');
  setIsLookingUp(false);
  setIsSubmitting(false);
};
```

Pass a wrapper: `onOpenChange={(open) => { if (!open) resetState(); onOpenChange(open); }}`

### Input & Select Styling (Consistent with Auth Pages)

```tsx
// Text input base classes:
"w-full border rounded-input px-3 py-2 text-[15px] text-text-primary bg-warm-surface placeholder:text-text-disabled focus:outline-none focus:ring-2 focus:ring-accent focus:ring-offset-0"

// Error state: add "border-error" instead of "border-warm-border"

// Label:
<label className="block text-[13px] font-medium text-text-secondary mb-1">{label}</label>

// Submit button:
"w-full bg-accent hover:bg-accent-hover disabled:opacity-50 disabled:cursor-not-allowed text-white py-3 rounded-button text-[15px] font-medium transition-colors min-h-[44px] focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-accent focus-visible:ring-offset-2"
```

### ⚠️ Tailwind v4 — No `tailwind.config.js`

All tokens in `src/index.css` `@theme {}` block. Token `--radius-popup` → `rounded-popup`. Shadow `--shadow-popup` → `shadow-popup`. Do NOT create `tailwind.config.js`.

### Existing Files Being Modified

| File | Current State | What Changes |
|------|--------------|--------------|
| `src/api/shelfApi.ts` | Only `getShelf()` | Add `addToShelf(bookId)` |
| `src/hooks/useShelf.ts` | Returns `{shelf, loading, error}` | Add `fetchCount` + `refetch` |
| `src/pages/ShelfPage.tsx` | EmptyState no-op handler, no add button | Wire modal state, add "Add Book" button in header, render `<BookForm>` |

### Files to Create (New)

- `src/api/booksApi.ts`
- `src/components/BookForm/BookForm.tsx`

### No Backend Changes

Story 2.6 is pure frontend. All required endpoints are already implemented:
- `GET /api/books/{isbn}` → Story 2.2 ✅
- `POST /api/books` → Story 2.3 ✅
- `POST /api/shelf` → Story 2.4 ✅

Do NOT modify any `.cs` files.

### Vite Proxy

`vite.config.ts` already proxies `/api` → `http://localhost:5000` (already set to HTTP, not HTTPS). No changes needed.

### Previous Story Intelligence

From Story 2.5 learnings:
- `BookCard` `onClick` prop already exists but wired to `undefined` — Story 3 will wire it to the progress popup
- `EmptyState.onAddBook` is already wired as a no-op `() => {}` — this story replaces it with `() => setIsAddBookOpen(true)`
- Tailwind `rounded-popup` and `shadow-popup` tokens are already defined in `src/index.css` `@theme {}` — use them for the modal

### Frontend Build Check

```
cd frontend && npm run build
```
Zero TypeScript errors = pass condition.

## Dev Agent Record

### Agent Model Used

claude-sonnet-4.6

### Debug Log References

### Completion Notes List

- All 5 tasks complete. Build: 0 TypeScript errors, 0 build errors ✅ (97 modules, exit code 0)
- Created `booksApi.ts` with `lookupISBN` (GET /api/books/{isbn}) and `createBook` (POST /api/books) + `CreateBookDto` interface
- Extended `shelfApi.ts` with `addToShelf(bookId)` → POST /api/shelf
- Updated `useShelf` hook: added `fetchCount` trigger state + `refetch` callback; resets loading/error on each refetch
- Created `BookForm` (300+ lines): Radix UI Dialog (focus trap), two-step flow (isbn → form), blur validation for all 4 fields, 10 predefined genres, API error banner (role="alert"), state reset on close, mobile bottom-sheet styling
- Updated `ShelfPage`: modal state, "Add Book" button always visible in header, `EmptyState` CTA wired, `onSuccess` triggers refetch

### File List

- `frontend/src/api/booksApi.ts` (new)
- `frontend/src/api/shelfApi.ts` (modified — added addToShelf)
- `frontend/src/hooks/useShelf.ts` (modified — added refetch)
- `frontend/src/components/BookForm/BookForm.tsx` (new)
- `frontend/src/pages/ShelfPage.tsx` (modified — modal state, Add Book button, BookForm)

### Change Log

- Story 2.6 Add Book Flow (Frontend) implemented — 2 new files, 3 modified (Date: 2026-05-26)
