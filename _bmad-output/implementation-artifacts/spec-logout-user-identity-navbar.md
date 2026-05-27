---
title: 'Logout and User Identity in NavBar'
type: 'feature'
created: '2026-05-27'
status: 'done'
route: 'one-shot'
---

## Intent

**Problem:** Users have no way to log out or see who is currently logged in — the NavBar shows only navigation links with no identity context.

**Approach:** Add `lastName` to the backend auth response (it was already stored in the DB but not returned), propagate it through the frontend type and AuthContext, then wire the existing `logout()` function to a "Log out" button and display `firstName lastName` on the right side of the NavBar for both desktop and mobile.

## Suggested Review Order

1. [`backend/BookTracker.Api/DTOs/Auth/AuthResponse.cs`](../../../backend/BookTracker.Api/DTOs/Auth/AuthResponse.cs) — added `LastName` property
2. [`backend/BookTracker.Api/Services/AuthService.cs`](../../../backend/BookTracker.Api/Services/AuthService.cs) — `LastName` now included in Register and Login responses
3. [`frontend/src/types/index.ts`](../../../frontend/src/types/index.ts) — `lastName` added to `AuthResponse` and `User`
4. [`frontend/src/context/AuthContext.tsx`](../../../frontend/src/context/AuthContext.tsx) — `lastName` state, localStorage, login/logout wired
5. [`frontend/src/components/NavBar/NavBar.tsx`](../../../frontend/src/components/NavBar/NavBar.tsx) — user name + Log out button on desktop and mobile strip
