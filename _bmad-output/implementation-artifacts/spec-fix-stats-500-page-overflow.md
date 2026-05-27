---
title: 'Fix stats page 500 on large page counts'
type: 'bugfix'
created: '2026-05-27'
status: 'done'
route: 'one-shot'
---

## Intent

**Problem:** Stats page returns 500 when a book has a very large `TotalPages` value — .NET's LINQ `Sum(Func<T,int>)` uses checked arithmetic and throws `OverflowException` when accumulated page deltas exceed `int.MaxValue` (~2.1 billion).

**Approach:** Fix the accumulation in `StatsService` to use `long` (handles any bad data already in DB); cap `TotalPages` at 10,000 in backend DTOs and the frontend form so the problem can't recur.

## Suggested Review Order

1. [`backend/BookTracker.Api/Services/StatsService.cs`](../../../backend/BookTracker.Api/Services/StatsService.cs) — `SumPositiveDeltas` and `pagesThisMonth` now use `long` accumulation, clamped back to `int`
2. [`backend/BookTracker.Api/DTOs/Books/CreateBookDto.cs`](../../../backend/BookTracker.Api/DTOs/Books/CreateBookDto.cs) — `[Range(1, 10000)]` replaces `[Range(1, int.MaxValue)]`
3. [`backend/BookTracker.Api/DTOs/Shelf/UpdatePagesDto.cs`](../../../backend/BookTracker.Api/DTOs/Shelf/UpdatePagesDto.cs) — `[Range(0, 10000)]` added
4. [`frontend/src/components/BookForm/BookForm.tsx`](../../../frontend/src/components/BookForm/BookForm.tsx) — validation rejects `> 10000`; `max={10000}` on the input
