# Core User Experience

### Defining Experience

The heartbeat of BookTracker is a single three-beat interaction that happens after every reading session:

> **Find my in-progress book → update my page count → feel satisfied.**

Everything in the design serves this loop. The Shelf is the entire app for 90% of visits. Adding books, exploring stats, reviewing the Reading Journal — all of these are one intentional tap away, but they never crowd the primary surface.

### Platform Strategy

- **Responsive web** — desktop, tablet, and phone browser are all first-class. Layouts adapt from single-column (phone) to multi-column (desktop) without sacrificing any functionality.
- **Touch and mouse equally supported** — tap targets sized for thumbs; hover states for mouse users.
- **No offline requirement** — local-run setup; connectivity is assumed.
- **No native capabilities leveraged** — pure browser experience; no camera, push notifications, or device sensors in v1.

### Effortless Interactions

1. **Page progress in 3 taps.** Tap book card → popup opens with stepper pre-loaded at current page → adjust → submit. No navigation, no form pages, no waiting.

2. **Most recently touched book is always first.** The Shelf sorts by last activity (add or update). A returning user never needs to scroll to find what they were reading.

3. **Auto-finish as reward, not surprise.** When the page stepper reaches totalPages and the user submits, the transition to Finished happens automatically — announced by a warm celebration moment (small animation + message), so the user feels the achievement rather than discovering a silent state change.

4. **Stats without navigation.** The Stats Strip is permanently anchored above the book list. The user sees their monthly page count and in-progress count on every Shelf visit without tapping anything.

### Critical Success Moments

1. **First ISBN lookup.** The moment a user types an ISBN and sees their book appear with a cover image is the product's first "this is magic" beat. It must be fast and visually satisfying.

2. **The finish celebration.** The first time a user logs their last page and sees the warm "You finished it!" moment, they understand this app cares about their reading life. This is the highest-value delight moment in the product.

3. **Opening the app and seeing their book first.** Returning after a day away and immediately seeing their in-progress book at the top — no searching — confirms the app knows them. Builds habit.

4. **Reading Journal scroll.** Seeing the full history of a re-read book — timestamps, page milestones, status changes — turns a simple tracker into a personal reading memoir.

### Experience Principles

1. **Shelf first, always.** The Shelf is the home, the workspace, and the destination. It must never feel crowded or require scrolling past irrelevant content to reach what matters.

2. **Last touched = first seen.** Sort by recency, not alphabet or date added. The user's current reading life is always front and center.

3. **Logging feels like a reward.** Progress updates are a satisfying micro-interaction — fast input, immediate visual feedback, and (at the finish line) a moment of celebration. Never a chore.

4. **Delight through restraint.** One animation (the finish celebration) lands harder than constant motion. Warmth comes from color and typography, not decorative movement.

5. **Responsive without compromise.** The same three-beat core interaction — find, update, feel good — works identically on a 375px phone and a 1440px desktop.
