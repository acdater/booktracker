# Core Interaction Design

### Defining Experience

> **"Log my reading progress before I put the book down."**

This is the moment BookTracker exists for. Every other feature — stats, journal, adding books — supports this moment but never competes with it. If a user can open the app, update their page count, and close it in under 30 seconds with a feeling of quiet satisfaction, the product has succeeded.

### User Mental Model

Readers think in pages, not percentages. They know they're on page 142; they don't think "I'm 44% through." The interface must speak pages everywhere — input, progress strip, stats.

The mental model is: **the Shelf is the reading desk**. The most recent book is at the front. The user picks it up (tap), notes their progress, puts it back. The app remembers where they left it. BookTracker adds memory and a quiet record to how a physical reading life already works.

Users expect:
- Instant recognition of their in-progress book (it's first, always)
- No navigation required to log progress — the action lives on the card itself
- Immediate visual feedback that the number changed
- No confirmation dialogs for routine updates

### Success Criteria

A successful progress update feels like:
1. **Under 30 seconds** from app open to closed
2. **Zero navigation** — popup opens from the Shelf, returns to the Shelf
3. **Visual confirmation** — the progress strip on the card visibly moves; the page number updates in place
4. **No friction at the finish line** — at `currentPages = totalPages`, submission auto-finishes; no extra step required

### Interaction Patterns

**Established patterns adopted:**
- Card tap → contextual popup (familiar from mobile apps everywhere; no user education needed)
- Numeric stepper with +/− (familiar form control; widely understood)
- Submit to close (standard modal pattern)

**BookTracker's specific approach:** The stepper pre-loads the *current saved page* (Option A). The user only adjusts the delta — how many pages did I read today? Faster than re-entering the full page number; more natural than a percentage slider.

### Experience Mechanics

**Initiation:** The entire book card is the tap target — no small icon or hidden affordance. A subtle press state signals it's interactive.

**Interaction:**
1. Tap card → progress popup slides up (mobile) or appears centred (desktop)
2. Popup shows: book title, cover thumbnail, current page pre-loaded in stepper
3. User taps +/− or types directly to set new page count
4. "Update" button activates as soon as the value differs from current
5. Tap Update → popup closes

**Feedback:**
- Progress strip on the card animates to the new fill position
- Page count updates in place on the card
- If new value = totalPages: popup closes, celebration overlay fires, book card transitions to Finished state

**Completion:**
- Routine update: popup dismissed, Shelf visible, card shows updated strip. Done.
- Auto-finish: warm celebration moment → Shelf with book in Finished state.

**Error path:** If the update fails (network/server error), the popup stays open with an inline error message. User can retry. No data lost.
