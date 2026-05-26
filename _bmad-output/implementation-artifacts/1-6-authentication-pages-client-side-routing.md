# Story 1.6: Authentication Pages & Client-Side Routing

Status: review

<!-- Note: Validation is optional. Run validate-create-story for quality check before dev-story. -->

## Story

As a **visitor**,
I want to register and log in through a clean UI, with my session persisted across page refreshes,
so that I stay authenticated without re-entering credentials on every visit.

## Acceptance Criteria

1. Register form (email, password, firstName, lastName, dateOfBirth) calls `POST /api/auth/register`; on success, stores JWT + userId + firstName in `AuthContext` (localStorage) and redirects to `/shelf`; on 409 shows banner "An account with this email already exists"; on 400 shows the error message from the API
2. Login form (email, password) calls `POST /api/auth/login`; on success, stores token + redirects to `/shelf`; on 401 shows banner "Invalid email or password"
3. Form validation fires **on blur** (not onChange): required fields, email format — inline field-level messages per field
4. `AuthContext` (`src/context/AuthContext.tsx`) stores `{ token, userId, firstName }`, initialises from `localStorage` on app load, exposes `login(response: AuthResponse)` and `logout()` actions
5. `useAuth` hook (`src/hooks/useAuth.ts`) provides access to `AuthContext`
6. `<RequireAuth>` (`src/components/RequireAuth/RequireAuth.tsx`) redirects unauthenticated users to `/login` using React Router `<Navigate>`
7. React Router v7 routes in `App.tsx`: `/login → LoginPage`, `/register → RegisterPage`, `/shelf → <RequireAuth><ShelfPage/>`, `/stats → <RequireAuth><StatsPage/>`; `App.tsx` wraps all routes in `AuthProvider` and renders basic `NavBar`
8. On page refresh, a valid token in localStorage restores the session without redirecting to `/login`
9. `authApi.ts` (`src/api/authApi.ts`) exports typed `register()` and `login()` functions using `fetchJson`
10. `npm run build` passes with zero TypeScript errors

## Tasks / Subtasks

- [x] Task 1: Create `src/api/authApi.ts` (AC: 9)
  - [x] Export `register(data: RegisterData): Promise<AuthResponse>` — POST /api/auth/register
  - [x] Export `login(data: LoginData): Promise<AuthResponse>` — POST /api/auth/login
  - [x] Both use `fetchJson` from `./client` — no raw fetch

- [x] Task 2: Create `src/context/AuthContext.tsx` (AC: 4, 8)
  - [x] Define `AuthContextValue` interface: `{ token: string | null; userId: string | null; firstName: string | null; login: (r: AuthResponse) => void; logout: () => void }`
  - [x] `AuthProvider` reads `localStorage` on mount to initialise state
  - [x] `login()` saves token, userId, firstName to state AND localStorage (`'token'`, `'userId'`, `'firstName'`)
  - [x] `logout()` clears state AND removes all three localStorage keys
  - [x] Export `AuthContext` (for `useAuth` hook) and `AuthProvider` component

- [x] Task 3: Create `src/hooks/useAuth.ts` (AC: 5)
  - [x] Export `useAuth()` hook — calls `useContext(AuthContext)`, throws if used outside provider

- [x] Task 4: Create `<RequireAuth>` component (AC: 6)
  - [x] Create `src/components/RequireAuth/RequireAuth.tsx`
  - [x] If no token in AuthContext → `<Navigate to="/login" replace />`; else render children

- [x] Task 5: Create basic `NavBar` component (AC: 7)
  - [x] Create `src/components/NavBar/NavBar.tsx`
  - [x] Links to `/shelf` and `/stats`; active state styled with `text-accent` (Tailwind token)
  - [x] Bottom tabs on mobile (`< sm`), top bar on desktop (`sm:`)
  - [x] Use `useNavigate` / link-based navigation from `react-router`

- [x] Task 6: Create `LoginPage` (AC: 1, 2, 3)
  - [x] Create `src/pages/LoginPage.tsx`
  - [x] Controlled form: `email`, `password` fields
  - [x] Blur validation: email required + valid format; password required
  - [x] On submit: call `authApi.login()` → `AuthContext.login()` → `navigate('/shelf')`
  - [x] On `ApiError` with any code: show inline banner with the error message
  - [x] Loading state: disable submit button while request in flight

- [x] Task 7: Create `RegisterPage` (AC: 1, 3)
  - [x] Create `src/pages/RegisterPage.tsx`
  - [x] Controlled form: `email`, `password`, `firstName`, `lastName`, `dateOfBirth` fields
  - [x] Blur validation: email required + valid format; all other fields required; dateOfBirth must be a valid date
  - [x] On submit: call `authApi.register()` → `AuthContext.login()` → `navigate('/shelf')`
  - [x] On `ApiError` code `EMAIL_EXISTS`: banner "An account with this email already exists"; other codes: show `error.message`
  - [x] Loading state: disable submit button while request in flight

- [x] Task 8: Create `ShelfPage` and `StatsPage` stubs (AC: 7)
  - [x] `src/pages/ShelfPage.tsx` — renders placeholder `<div>Shelf coming soon</div>`
  - [x] `src/pages/StatsPage.tsx` — renders placeholder `<div>Stats coming soon</div>`

- [x] Task 9: Update `App.tsx` with routing + AuthProvider + NavBar (AC: 7, 8)
  - [x] Import `BrowserRouter, Routes, Route, Navigate` from `'react-router'`
  - [x] Wrap everything in `<BrowserRouter>` → `<AuthProvider>` → `<NavBar />` + `<Routes>`
  - [x] Route: `/login` → `<LoginPage>`, `/register` → `<RegisterPage>`, `/shelf` → `<RequireAuth><ShelfPage/></RequireAuth>`, `/stats` → `<RequireAuth><StatsPage/></RequireAuth>`, `*` → `<Navigate to="/shelf" replace />`

- [x] Task 10: Verify build passes (AC: 10)
  - [x] Run `npm run build` from `frontend/` — zero TypeScript errors

## Dev Notes

### ⚠️ CRITICAL: React Router v7 Import Path

```ts
// ✅ CORRECT — React Router v7
import { BrowserRouter, Routes, Route, Navigate, useNavigate } from 'react-router';

// ❌ WRONG — this package no longer exists in v7
import { ... } from 'react-router-dom';
```

React Router v7 merged `react-router` and `react-router-dom` into a single package. The installed package is `react-router`.

### ⚠️ CRITICAL: TypeScript 5.8 `erasableSyntaxOnly`

Parameter properties in constructors are banned (learned in Story 1.5):

```ts
// ❌ WRONG — TS1294 error
class Foo { constructor(public readonly x: string) {} }

// ✅ CORRECT
class Foo {
  readonly x: string;
  constructor(x: string) { this.x = x; }
}
```

Apply this to any new classes.

### ⚠️ CRITICAL: Tailwind v4 Class Names for Design Tokens

Color tokens defined as `--color-{name}` in `@theme {}` map to Tailwind classes:
- `--color-accent` → `bg-accent`, `text-accent`, `border-accent`
- `--color-text-primary` → `text-text-primary` (doubled word — that's v4)
- `--color-warm-bg` → `bg-warm-bg`
- `--color-error` → `text-error`, `bg-error`
- `--color-error-bg` → `bg-error-bg`
- `--shadow-card-rest` → `shadow-card-rest`
- `--radius-button` → `rounded-button`
- `--radius-input` → `rounded-input`

### Exact Code — `src/api/authApi.ts`

```ts
import { fetchJson } from './client';
import type { AuthResponse } from '../types';

export interface RegisterData {
  email: string;
  password: string;
  firstName: string;
  lastName: string;
  dateOfBirth: string; // ISO 8601: "YYYY-MM-DDT00:00:00Z"
}

export interface LoginData {
  email: string;
  password: string;
}

export const register = (data: RegisterData) =>
  fetchJson<AuthResponse>('/api/auth/register', {
    method: 'POST',
    body: JSON.stringify(data),
  });

export const login = (data: LoginData) =>
  fetchJson<AuthResponse>('/api/auth/login', {
    method: 'POST',
    body: JSON.stringify(data),
  });
```

### Exact Code — `src/context/AuthContext.tsx`

```tsx
import { createContext, useContext, useState } from 'react';
import type { AuthResponse } from '../types';

interface AuthContextValue {
  token: string | null;
  userId: string | null;
  firstName: string | null;
  login: (response: AuthResponse) => void;
  logout: () => void;
}

export const AuthContext = createContext<AuthContextValue | null>(null);

export function AuthProvider({ children }: { children: React.ReactNode }) {
  const [token, setToken] = useState<string | null>(() => localStorage.getItem('token'));
  const [userId, setUserId] = useState<string | null>(() => localStorage.getItem('userId'));
  const [firstName, setFirstName] = useState<string | null>(() => localStorage.getItem('firstName'));

  const login = (response: AuthResponse) => {
    localStorage.setItem('token', response.token);
    localStorage.setItem('userId', String(response.userId));
    localStorage.setItem('firstName', response.firstName);
    setToken(response.token);
    setUserId(String(response.userId));
    setFirstName(response.firstName);
  };

  const logout = () => {
    localStorage.removeItem('token');
    localStorage.removeItem('userId');
    localStorage.removeItem('firstName');
    setToken(null);
    setUserId(null);
    setFirstName(null);
  };

  return (
    <AuthContext.Provider value={{ token, userId, firstName, login, logout }}>
      {children}
    </AuthContext.Provider>
  );
}
```

### Exact Code — `src/hooks/useAuth.ts`

```ts
import { useContext } from 'react';
import { AuthContext } from '../context/AuthContext';

export function useAuth() {
  const ctx = useContext(AuthContext);
  if (!ctx) throw new Error('useAuth must be used within AuthProvider');
  return ctx;
}
```

### Exact Code — `src/components/RequireAuth/RequireAuth.tsx`

```tsx
import { Navigate } from 'react-router';
import { useAuth } from '../../hooks/useAuth';

export function RequireAuth({ children }: { children: React.ReactNode }) {
  const { token } = useAuth();
  if (!token) return <Navigate to="/login" replace />;
  return <>{children}</>;
}
```

### Exact Code — `src/App.tsx` (complete replacement)

```tsx
import { BrowserRouter, Routes, Route, Navigate } from 'react-router';
import { AuthProvider } from './context/AuthContext';
import { NavBar } from './components/NavBar/NavBar';
import { RequireAuth } from './components/RequireAuth/RequireAuth';
import { LoginPage } from './pages/LoginPage';
import { RegisterPage } from './pages/RegisterPage';
import { ShelfPage } from './pages/ShelfPage';
import { StatsPage } from './pages/StatsPage';

function App() {
  return (
    <BrowserRouter>
      <AuthProvider>
        <NavBar />
        <Routes>
          <Route path="/login" element={<LoginPage />} />
          <Route path="/register" element={<RegisterPage />} />
          <Route path="/shelf" element={<RequireAuth><ShelfPage /></RequireAuth>} />
          <Route path="/stats" element={<RequireAuth><StatsPage /></RequireAuth>} />
          <Route path="*" element={<Navigate to="/shelf" replace />} />
        </Routes>
      </AuthProvider>
    </BrowserRouter>
  );
}

export default App;
```

### NavBar Implementation Guide

Basic structure — full visual styling is Epic 2. Use design tokens and `useLocation` for active state:

```tsx
import { NavLink } from 'react-router';

export function NavBar() {
  return (
    <nav className="bg-warm-surface border-b border-warm-border">
      <div className="flex gap-4 p-4">
        <NavLink
          to="/shelf"
          className={({ isActive }) =>
            isActive ? 'text-accent font-medium' : 'text-text-secondary'
          }
        >
          Shelf
        </NavLink>
        <NavLink
          to="/stats"
          className={({ isActive }) =>
            isActive ? 'text-accent font-medium' : 'text-text-secondary'
          }
        >
          Stats
        </NavLink>
      </div>
    </nav>
  );
}
```

`NavLink` from `react-router` (v7) provides `isActive` in the className callback — use it for the active state.

### Form Validation Pattern (blur-based)

Validate on `onBlur`, not `onChange`. Store errors in a `Record<string, string>` state:

```tsx
const [errors, setErrors] = useState<Record<string, string>>({});

const validateEmail = (value: string) => {
  if (!value) return 'Email is required';
  if (!/^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(value)) return 'Enter a valid email address';
  return '';
};

// In JSX:
<input
  onBlur={(e) => {
    const err = validateEmail(e.target.value);
    setErrors(prev => ({ ...prev, email: err }));
  }}
/>
{errors.email && <p className="text-error text-sm mt-1">{errors.email}</p>}
```

### dateOfBirth Format for Register

Backend expects ISO 8601 UTC string. Convert the HTML date input value (`"YYYY-MM-DD"`) before sending:

```ts
const dateOfBirth = formData.dateOfBirth
  ? `${formData.dateOfBirth}T00:00:00Z`
  : '';
```

### Error Banner Pattern

API errors should show as an inline banner above the submit button:

```tsx
{apiError && (
  <div className="bg-error-bg text-error rounded-input p-3 text-sm">
    {apiError}
  </div>
)}
```

Reset `apiError` on each new submit attempt.

### Loading State Pattern

Prevent double-submit during API calls:

```tsx
const [loading, setLoading] = useState(false);

const handleSubmit = async (e: React.FormEvent) => {
  e.preventDefault();
  setLoading(true);
  setApiError('');
  try {
    const response = await authApi.login({ email, password });
    auth.login(response);
    navigate('/shelf');
  } catch (err) {
    if (err instanceof ApiError) setApiError(err.message);
    else setApiError('An unexpected error occurred.');
  } finally {
    setLoading(false);
  }
};

<button type="submit" disabled={loading}>
  {loading ? 'Signing in…' : 'Sign in'}
</button>
```

### Files to CREATE

```
frontend/src/api/authApi.ts
frontend/src/context/AuthContext.tsx
frontend/src/hooks/useAuth.ts
frontend/src/components/RequireAuth/RequireAuth.tsx
frontend/src/components/NavBar/NavBar.tsx
frontend/src/pages/LoginPage.tsx
frontend/src/pages/RegisterPage.tsx
frontend/src/pages/ShelfPage.tsx
frontend/src/pages/StatsPage.tsx
```

### Files to MODIFY

```
frontend/src/App.tsx   ← replace bare shell with full routing
```

### No Tests Required

Architecture rule AR-13: xUnit unit tests for backend service layer only. No frontend unit tests in v1. Verification is `npm run build` (TypeScript type-check) only.

### From Story 1.5 — Applied Learnings

- Proxy target is `http://localhost:5000` (HTTP, not HTTPS) — the backend `dotnet run` default profile
- `fetchJson<T>` and `ApiError` are in `src/api/client.ts` — import from `'./client'` in `authApi.ts`
- `AuthResponse` interface is in `src/types/index.ts`: `{ userId: number, email, firstName, token }`
- TS5.8 `erasableSyntaxOnly` — no parameter properties. Apply to any new classes.
- Tailwind v4 `@theme` tokens are in `src/index.css` — all 13 colors + font + radius + shadow

### References

- [Source: epics.md#Story 1.6 Acceptance Criteria]
- [Source: architecture.md#Frontend Architecture — React Context, React Router v7, API module]
- [Source: architecture.md#Frontend Project Structure]
- [Source: AR-15 — React Router v7, RequireAuth, 4 routes]
- [Source: UX-DR1 — color tokens for form styling]
- [Source: UX-DR14 — blur validation, inline errors, friendly copy]
- [Source: story 1.5 Dev Notes — proxy HTTP, TS5.8 fix, fetchJson pattern]

## Dev Agent Record

### Agent Model Used

Claude Sonnet 4.6

### Debug Log References

### Completion Notes List

### File List
