---
title: 'Fix: Reread Starts Book as Started Immediately'
type: fix
created: '2026-05-27'
status: done
route: one-shot
---

# Fix: Reread Starts Book as Started Immediately

## Intent

**Problem:** Clicking "Read Again" on a Finished/Abandoned book created a new `Resting` UserBook, showing a "Start Reading" button — requiring an unnecessary extra tap before reading could begin.

**Approach:** `RereadAsync` now creates the new `UserBook` with `Status = Started` and `StartedAt = DateTime.UtcNow`, atomically with a `StatusChange` BookAction (Resting → Started) via the new `CreateWithActionAsync` repository method. The frontend already shows "Abandon" for Started books, so no frontend changes were needed.

## Suggested Review Order

1. [`backend/BookTracker.Api/Services/ShelfService.cs`](../../backend/BookTracker.Api/Services/ShelfService.cs) — `RereadAsync`: new `UserBook` now has `Status=Started`, `StartedAt` set; uses `CreateWithActionAsync`
2. [`backend/BookTracker.Api/Repositories/UserBookRepository.cs`](../../backend/BookTracker.Api/Repositories/UserBookRepository.cs) — new `CreateWithActionAsync`: inserts UserBook + BookAction in one `SaveChangesAsync`
3. [`backend/BookTracker.Api/Repositories/Interfaces/IUserBookRepository.cs`](../../backend/BookTracker.Api/Repositories/Interfaces/IUserBookRepository.cs) — interface declaration for `CreateWithActionAsync`
4. [`backend/BookTracker.Tests/Services/ShelfServiceTests.cs`](../../backend/BookTracker.Tests/Services/ShelfServiceTests.cs) — `RereadAsync_FinishedBook` test updated: asserts `Started` status, `StartedAt` non-null, and BookAction captured
