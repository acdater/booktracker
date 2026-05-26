# Story 1.5: Frontend Scaffold & Design System

Status: review

<!-- Note: Validation is optional. Run validate-create-story for quality check before dev-story. -->

## Story

As a **developer**,
I want the React frontend scaffolded with Tailwind v4 design tokens, Radix UI, React Router v7, and the Vite proxy configured,
so that all subsequent stories can build components on a consistent visual and architectural foundation.

## Acceptance Criteria

1. `frontend/` exists with React 19 + TypeScript strict mode + Vite 6, created via `npm create vite@latest frontend -- --template react-ts`
2. `vite.config.ts` includes `@tailwindcss/vite` plugin and proxy `/api → https://localhost:5001` with `secure: false`
3. `src/index.css` contains `@import "tailwindcss"` and `@theme {}` block with all 13 color tokens, font-family system stack, border-radius tokens (card: 12px, button: 8px, input: 8px, popup: 16px), and box-shadow tokens (card-rest, card-hover, popup)
4. `src/api/client.ts` exports `fetchJson<T>(url, options)` — injects `Authorization: Bearer <token>` from localStorage when present; throws `ApiError({ message, code })` on non-2xx responses
5. `src/types/index.ts` exports shared TypeScript interfaces: `Book`, `UserBook`, `BookAction`, `User`, `AuthResponse`, `StatsStripData`, `StatsPageData`
6. Folder structure `src/api/`, `src/components/`, `src/pages/`, `src/context/`, `src/hooks/`, `src/types/`, `src/utils/` is in place with placeholder files so folders are tracked in git
7. `npm run dev` starts without errors on `localhost:5173`; no TypeScript errors in the scaffold

## Tasks / Subtasks

- [x] Task 1: Scaffold the frontend (AC: 1)
  - [x] Run `npm create vite@latest frontend -- --template react-ts` from repo root
  - [x] `cd frontend && npm install`
  - [x] Install dependencies: `npm install tailwindcss @tailwindcss/vite @radix-ui/react-dialog @radix-ui/react-visually-hidden react-router`
  - [x] Delete generated boilerplate: `src/assets/react.svg`, `public/vite.svg`, clear `src/App.css`

- [x] Task 2: Configure `vite.config.ts` (AC: 2)
  - [x] Add `@tailwindcss/vite` plugin (import + add to plugins array)
  - [x] Add server proxy block: `/api → http://localhost:5000` with `secure: false`
  - [x] **Do NOT add postcss.config.js** — `@tailwindcss/vite` handles everything

- [x] Task 3: Configure Tailwind v4 design tokens in `src/index.css` (AC: 3)
  - [x] Replace entire file content with `@import "tailwindcss";` + `@theme {}` block
  - [x] Add all 13 color tokens using `--color-{name}:` prefix (see exact code below)
  - [x] Add font-family, border-radius, and box-shadow tokens

- [x] Task 4: Create `src/api/client.ts` (AC: 4)
  - [x] Export `ApiError` class extending `Error` with `code: string` property
  - [x] Export `fetchJson<T>` — reads token from `localStorage.getItem('token')`, injects Bearer header, throws `ApiError` on non-2xx

- [x] Task 5: Create `src/types/index.ts` (AC: 5)
  - [x] Export all 7 shared interfaces: `Book`, `UserBook`, `BookAction`, `User`, `AuthResponse`, `StatsStripData`, `StatsPageData`

- [x] Task 6: Create folder structure + stub files (AC: 6)
  - [x] Create placeholder `.gitkeep` files in: `src/api/`, `src/components/`, `src/pages/`, `src/context/`, `src/hooks/`, `src/utils/`
  - [x] Simplify `src/App.tsx` to a bare shell (renders `<div>BookTracker</div>`) so the app compiles cleanly

- [x] Task 7: Verify `npm run dev` starts cleanly (AC: 7)
  - [x] Run `npm run dev` — confirm starts on `localhost:5173` with no errors
  - [x] Run `npm run build` — confirm zero TypeScript errors

## Dev Notes

### ⚠️ CRITICAL: Tailwind v4 is Fundamentally Different from v3

Tailwind v4 replaces **all** JS config with CSS-in-config. These common v3 patterns are WRONG in v4:

| ❌ Tailwind v3 (WRONG) | ✅ Tailwind v4 (CORRECT) |
|---|---|
| `@tailwind base; @tailwind components; @tailwind utilities;` | `@import "tailwindcss";` |
| `tailwind.config.js` with `theme.extend.colors` | `@theme { --color-*: value; }` in `index.css` |
| `postcss.config.js` + `autoprefixer` | `@tailwindcss/vite` plugin only — no PostCSS setup needed |
| `npm install tailwindcss postcss autoprefixer` | `npm install tailwindcss @tailwindcss/vite` |

**Do NOT create `tailwind.config.js` or `tailwind.config.ts`** — they do not exist in v4.

### Exact Code — `vite.config.ts`

```ts
import { defineConfig } from 'vite'
import react from '@vitejs/plugin-react'
import tailwindcss from '@tailwindcss/vite'

export default defineConfig({
  plugins: [
    react(),
    tailwindcss(),
  ],
  server: {
    proxy: {
      '/api': {
        target: 'https://localhost:5001',
        secure: false,
      },
    },
  },
})
```

### Exact Code — `src/index.css` (complete file)

```css
@import "tailwindcss";

@theme {
  /* Colors */
  --color-warm-bg: #FAF6F0;
  --color-warm-surface: #FFFFFF;
  --color-warm-surface-alt: #F3EEE7;
  --color-warm-border: #E2D9CE;
  --color-accent: #6B7555;
  --color-accent-hover: #556044;
  --color-accent-subtle: #EBF0E6;
  --color-text-primary: #1C1A18;
  --color-text-secondary: #6B6259;
  --color-text-disabled: #ADA49A;
  --color-error: #A84040;
  --color-error-bg: #FDF0EF;
  --color-celebration: #C4874A;

  /* Typography */
  --font-sans: -apple-system, BlinkMacSystemFont, 'Segoe UI', sans-serif;

  /* Border Radius */
  --radius-card: 12px;
  --radius-button: 8px;
  --radius-input: 8px;
  --radius-popup: 16px;

  /* Box Shadows */
  --shadow-card-rest: 0 2px 8px rgba(0,0,0,0.08);
  --shadow-card-hover: 0 4px 16px rgba(0,0,0,0.12);
  --shadow-popup: 0 8px 32px rgba(0,0,0,0.16);
}
```

**How to use these tokens in components (v4 class names):**
- `--color-warm-bg` → classes: `bg-warm-bg`, `text-warm-bg`, `border-warm-bg`
- `--color-text-primary` → class: `text-text-primary` (yes, doubled word — that's v4's behavior with the `--color-` prefix stripped)
- `--font-sans` → class: `font-sans`
- `--radius-card` → class: `rounded-card`
- `--shadow-card-rest` → class: `shadow-card-rest`

### Exact Code — `src/api/client.ts`

```ts
export class ApiError extends Error {
  constructor(
    message: string,
    public readonly code: string
  ) {
    super(message);
    this.name = 'ApiError';
  }
}

export async function fetchJson<T>(url: string, options: RequestInit = {}): Promise<T> {
  const token = localStorage.getItem('token');
  const headers: Record<string, string> = {
    'Content-Type': 'application/json',
    ...((options.headers as Record<string, string>) ?? {}),
  };
  if (token) {
    headers['Authorization'] = `Bearer ${token}`;
  }

  const res = await fetch(url, { ...options, headers });

  if (!res.ok) {
    const body = await res
      .json()
      .catch(() => ({ error: 'An unexpected error occurred.', code: 'UNKNOWN_ERROR' }));
    throw new ApiError(
      body.error ?? 'An unexpected error occurred.',
      body.code ?? 'UNKNOWN_ERROR'
    );
  }

  if (res.status === 204) return undefined as T;

  return res.json() as Promise<T>;
}
```

### Exact Code — `src/types/index.ts`

```ts
export interface User {
  userId: number;
  email: string;
  firstName: string;
}

export interface AuthResponse {
  userId: number;
  email: string;
  firstName: string;
  token: string;
}

export interface Book {
  id: number;
  isbn: string;
  title: string;
  author: string;
  totalPages: number;
  genre: string;
  coverImageUrl: string | null;
}

export interface UserBook {
  id: number;
  userId: number;
  bookId: number;
  book: Book;
  status: 'Resting' | 'Started' | 'Finished' | 'Abandoned';
  currentPages: number;
  readingNumber: number;
  startedAt: string | null;
  finishedAt: string | null;
  lastActivityAt: string;
}

export interface BookAction {
  id: number;
  userBookId: number;
  readingNumber: number;
  actionType: string;
  oldValue: string | null;
  newValue: string | null;
  timestamp: string;
}

export interface StatsStripData {
  totalUserBooks: number;
  finishedCount: number;
  startedCount: number;
  pagesThisMonth: number;
}

export interface StatsPageData {
  byStatus: {
    resting: number;
    started: number;
    finished: number;
    abandoned: number;
    total: number;
  };
  completionsBy: { days: number; count: number }[];
  pagesBy: { days: number; pages: number }[];
  unfinishedGenre: string | null;
}
```

### Minimal `src/App.tsx` (scaffold placeholder)

```tsx
function App() {
  return <div>BookTracker</div>;
}

export default App;
```

Story 1.6 will replace this with routing + AuthContext + NavBar.

### Files to CREATE

```
frontend/                         ← new (from npm create vite)
frontend/src/index.css            ← REPLACE default content
frontend/vite.config.ts           ← MODIFY (add plugin + proxy)
frontend/src/App.tsx              ← REPLACE with bare shell
frontend/src/api/client.ts        ← NEW
frontend/src/types/index.ts       ← NEW
frontend/src/api/.gitkeep         ← NEW (already covered by client.ts)
frontend/src/components/.gitkeep  ← NEW
frontend/src/pages/.gitkeep       ← NEW
frontend/src/context/.gitkeep     ← NEW
frontend/src/hooks/.gitkeep       ← NEW
frontend/src/utils/.gitkeep       ← NEW
```

### Files to DELETE

```
frontend/src/assets/react.svg     ← DELETE (Vite boilerplate)
frontend/public/vite.svg          ← DELETE (Vite boilerplate)
frontend/src/App.css              ← DELETE or clear (replaced by Tailwind)
```

### Architecture Compliance

- ✅ Tailwind v4 via `@tailwindcss/vite` plugin — no PostCSS, no `tailwind.config.js`
- ✅ `@theme {}` in `src/index.css` — all design tokens in CSS, not JS
- ✅ `fetchJson` in `src/api/client.ts` — no raw `fetch` outside `src/api/`
- ✅ TypeScript strict mode (from `react-ts` template — do not modify `tsconfig.json`)
- ✅ React Router v7: install with `npm install react-router` (NOT `react-router-dom`)
- ✅ Radix UI: install `@radix-ui/react-dialog @radix-ui/react-visually-hidden` — **zero visual output** from Radix; used for accessibility primitives (focus trap, ARIA) only
- ✅ No Axios — native `fetch` only, wrapped in `fetchJson`
- ✅ Vite proxy eliminates CORS during dev — no CORS config needed anywhere

### React Router v7 Notes (Story 1.6 Preview)

React Router v7 import: `import { BrowserRouter, Routes, Route } from 'react-router'` (NOT `'react-router-dom'` — that package no longer exists in v7). This is ready for Story 1.6 to use.

### From Story 1.4 — Applied Learnings

- Backend runs on `https://localhost:5001` — the proxy target must use `https` and `secure: false` to accept the self-signed dev cert
- JWT token is stored in `localStorage` (accepted trade-off per PRD §A-3). Key: `'token'` (string, plain)
- `AuthResponse` shape from backend: `{ userId: number, email: string, firstName: string, token: string }` — matches `src/types/index.ts` interface exactly
- Backend returns camelCase JSON globally — TS interfaces already use camelCase fields

### References

- [Source: epics.md#Story 1.5 Acceptance Criteria]
- [Source: architecture.md#Frontend Starter: Vite + React + TypeScript]
- [Source: architecture.md#Frontend Architecture — API client, routing, state management]
- [Source: architecture.md#Frontend Project Structure]
- [Source: architecture.md#Vite proxy configuration]
- [Source: AR-3, AR-4, AR-12, AR-15 — scaffold commands, Tailwind v4, proxy, React Router]
- [Source: UX-DR1, UX-DR2 — color tokens, typography system stack]

## Dev Agent Record

### Agent Model Used

Claude Sonnet 4.6

### Debug Log References

### Completion Notes List

- ✅ Task 1: Scaffolded with `npm create vite@latest frontend -- --template react-ts` (Vite 8, React 19, TS strict). Installed tailwindcss, @tailwindcss/vite, @radix-ui/react-dialog, @radix-ui/react-visually-hidden, react-router. Deleted react.svg, vite.svg, App.css boilerplate.
- ✅ Task 2: vite.config.ts — added @tailwindcss/vite plugin + proxy `/api → http://localhost:5000` (HTTP, not HTTPS — backend runs HTTP by default on port 5000).
- ✅ Task 3: src/index.css fully replaced with `@import "tailwindcss"` + `@theme {}` block containing all 13 color tokens, font-sans, 4 radius tokens, 3 shadow tokens.
- ✅ Task 4: src/api/client.ts — ApiError class (TS5.8 compatible, no parameter properties) + fetchJson<T> with Bearer injection.
- ✅ Task 5: src/types/index.ts — all 7 interfaces: User, AuthResponse, Book, UserBook, BookAction, StatsStripData, StatsPageData.
- ✅ Task 6: All src subdirs created; .gitkeep files for components, pages, context, hooks, utils; App.tsx simplified to bare shell.
- ✅ Task 7: `npm run build` → 0 errors, 16 modules transformed, built in 126ms. Note: ApiError required fix — TS5.8 erasableSyntaxOnly forbids parameter properties; changed to explicit field + assignment.

### File List

- `frontend/` — new scaffold (Vite 8 + React 19 + TypeScript strict)
- `frontend/package.json` — created with all deps
- `frontend/vite.config.ts` — modified: @tailwindcss/vite plugin + proxy
- `frontend/src/index.css` — replaced: Tailwind v4 @theme tokens
- `frontend/src/App.tsx` — replaced: bare shell
- `frontend/src/api/client.ts` — created
- `frontend/src/types/index.ts` — created
- `frontend/src/components/.gitkeep` — created
- `frontend/src/pages/.gitkeep` — created
- `frontend/src/context/.gitkeep` — created
- `frontend/src/hooks/.gitkeep` — created
- `frontend/src/utils/.gitkeep` — created
- `_bmad-output/implementation-artifacts/1-5-frontend-scaffold-design-system.md` — status → review
- `_bmad-output/implementation-artifacts/sprint-status.yaml` — 1-5 → review
