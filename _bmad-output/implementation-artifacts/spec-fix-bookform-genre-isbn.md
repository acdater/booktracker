---
title: 'Fix BookForm genre pre-select and ISBN in manual entry'
type: 'bugfix'
created: '2026-05-27'
status: 'done'
route: 'one-shot'
---

## Intent

**Problem:** Two UX bugs in the Add Book form: (1) genre is never pre-selected even when the DB/lookup returns it; (2) the manual-entry "Skip" path has no ISBN field, so users can submit with an empty ISBN.

**Approach:** Fix `genre: ''` to use `book.genre` in the lookup handler; add `isbn` to `FormData` (replacing the separate `resolvedIsbn` state), show it as a required field at the top of the form step, pre-filled from whatever the user typed or from the lookup.

## Suggested Review Order

1. [`frontend/src/components/BookForm/BookForm.tsx`](../../../frontend/src/components/BookForm/BookForm.tsx) — all changes: `FormData`/`TouchedFields` interfaces, `getErrors`, `handleLookup` (genre fix + isbn seeding), Skip button, `handleSubmit`, ISBN input JSX
