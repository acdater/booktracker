---
stepsCompleted: [1, 2, 3, 4, 5, 6, 7, 8]
inputDocuments:
  - _bmad-output/planning-artifacts/prds/prd-BookTracker-2026-05-25/prd.md
  - _bmad-output/planning-artifacts/prds/prd-BookTracker-2026-05-25/addendum.md
  - _bmad-output/planning-artifacts/briefs/brief-Agentic AI-2026-05-25/brief.md
---

# UX Design Specification — BookTracker

**Author:** Alexei
**Date:** 2026-05-25

---

<!-- UX design content will be appended sequentially through collaborative workflow steps -->

## Executive Summary

### Project Vision

BookTracker is a warm, personal reading companion — self-hosted, dependency-free, and built around one honest interaction: a reader logging their progress after a session and feeling good about it. The design must make that interaction effortless on any device, in under 30 seconds, without ever feeling cold or clinical.

The visual language is **warm minimalism**: clean lines and forms, a muted-warm color palette, and progressive disclosure that keeps the daily surface simple while rewarding deeper exploration. Not a social platform. Not a spreadsheet. A well-designed personal space.

### Target Users

**Primary — The Regular Reader**
Reads consistently (a chapter here, a few pages there), opens BookTracker at the end of a session to log pages and take a quick glance at their stats. They want to be in and out fast. They care that the app feels like *theirs* — personal, not generic. Tech-comfortable but not technical; they shouldn't need to learn the app, it should be self-evident.

**Secondary — The BMad Developer**
Uses BookTracker as a reference implementation of the BMad Method. Cares less about emotional warmth, more about architectural legibility — but still benefits from a clean, well-structured UI that maps directly to the underlying component and domain model.

### Key Design Challenges

1. **The Shelf must do the heavy lifting.** It is the primary workspace for 90% of sessions — Stats Strip + book cards + quick actions must coexist without crowding, on screens from 375px to 1440px wide. A user should locate their in-progress book and update pages without any navigation.

2. **State machine legibility at a glance.** Four reading states (Resting / Started / Finished / Abandoned) need to be instantly readable on a book card. Color ribbons carry most of the signal; the action button carries the rest. On mobile, this is tight real estate — hierarchy must be ruthless.

3. **Progress update as a micro-interaction.** The page stepper is the most frequent action in the app. It must be: discoverable on first use, fast to operate, and satisfying to complete. The auto-finish mechanic (reaching totalPages triggers Finished) needs clear affordance so it feels rewarding, not surprising.

4. **Stats readability across screen sizes.** Six time-period buckets + by-status counts + Unfinished Genre insight on one page risks becoming a wall of numbers on mobile. Visual hierarchy must surface the most relevant answer in 5 seconds.

### Design Opportunities

1. **Status ribbon color as emotional design.** A warm, considered palette for the four states — earthy amber (Started), soft sage (Finished), dusty rose (Abandoned), muted slate (Resting) — makes the Shelf feel like a real bookcase, not a task list. Color does the emotional work so the layout can stay minimal.

2. **Stats Strip as the "feel-good moment."** Always-visible on the Shelf, the strip is the first thing a user sees on every visit. Getting the hierarchy and micro-copy right here creates a satisfying feedback loop: open app → feel the progress → log more.

3. **Reading Journal as quiet delight.** Seeing a full timeline of reads — "you read this book twice, 8 months apart" — is the kind of detail that makes a personal app feel alive. The Journal popup is worth treating as a moment of reflection, not just a data list.

4. **Progressive disclosure keeps daily use frictionless.** Shelf = simple. Journal, full Stats Page, Add Book flow = one intentional tap away. Complexity never leaks into the primary surface.

## Core User Experience

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

## Desired Emotional Response

### Emotional North Star

> **"This is mine. I'm glad I recorded that."**

BookTracker should feel like a personal reading diary with good memory — not a challenge platform or a social feed. The app never pushes. It simply holds a quiet, honest record of a reader's curiosity.

### Primary Emotional Goals

| Moment | Target Feeling | What Creates It |
|---|---|---|
| Opening the app | Familiar comfort | Most-recent book at top, no setup required |
| Logging pages | Small, honest satisfaction | Fast 3-tap interaction, immediate count update |
| Finishing a book | Quiet pride + warmth | Gentle celebration — felt, not performed |
| Browsing journal | Reflective nostalgia | Chronological history of a reading life |
| Checking stats | Honest self-awareness | Clear numbers, no streaks or comparisons |
| Adding a book | Curiosity and readiness | ISBN magic moment — cover appears instantly |

### Emotions to Explicitly Design Against

- **Pressure** — no streaks, no reading challenges, no "you haven't updated in 3 days" prompts. Reading desire must come from within, not from the app.
- **Judgement** — no comparisons, no public visibility, no metrics that imply a "good" or "bad" reader.
- **Overwhelm** — the Shelf stays clean. Complexity is always one tap away, never in the way.
- **Guilt** — Abandoned books carry no negative framing. Abandoning is a valid choice, not a failure.

### Micro-Emotional Design

**Empty states** — warm, encouraging tone with clear signpost action. A new user's empty shelf should feel like an *invitation*, not a void. Brief friendly copy + a prominent "Add your first book" call to action.

**Error states** — neutral-red palette (not alarm red) communicates that something went wrong without panic. Copy is factual and helpful: what happened, what the user can try. ISBN/Open Library failures offer the manual entry form immediately as the next step — no dead ends.

**Form validation** — inline field-level guidance appears on blur (not on submit). Friendly, specific: *"ISBN should be 10 or 13 digits"* rather than *"Invalid input."* Required fields are marked; the path to completion is always visible.

**The finish celebration** — a single restrained moment: soft warm animation (confetti or a gentle glow, not a full-screen takeover) + a personal message. One beat, then back to the Shelf with the book now in Finished state. The understatement is intentional — it mirrors how readers actually feel when closing a last page.

## UX Pattern Analysis & Inspiration

### Inspiring Products Analysis

#### Apple Books

Apple Books is the primary inspiration reference for BookTracker. It succeeds at exactly the emotional territory we're targeting: personal, warm, never clinical.

**What it does exceptionally well:**
- **Book cover as the UI** — the cover art *is* the card. No labels needed; the visual does all the work. A shelf of beautiful covers feels like an actual bookshelf.
- **Generous whitespace + soft shadows** — cards breathe. Nothing is cramped. The cream/warm-white background recalls paper without being literal about it.
- **SF Pro system typography** — clean, readable at every size, and instantly feels "premium native" without effort.
- **Subtle depth** — soft drop shadows under cards create a tactile, physical-world feeling without heavy skeuomorphism.
- **Reading progress bar** — a thin strip at the bottom of a book card communicates progress instantly, no numbers needed unless you look.
- **Fluid transitions** — opening a book feels like lifting a physical object. Transitions are purposeful, not decorative.
- **Restrained color** — accent color is used sparingly (one action color); the rest is neutral warm tones. Never garish.
- **Empty state invitation** — an empty library in Apple Books feels like a clean shelf waiting to be filled, not a broken state.

**What doesn't apply to BookTracker:**
- The reading experience itself (page turning, typography settings) — BookTracker is a tracker, not a reader.
- iCloud sync complexity — scope is simpler.
- Social/store features — BookTracker is purely personal.

### Transferable UX Patterns

**Navigation Patterns:**
- **Bottom tab bar (mobile)** — BookTracker's 3–4 destinations (Shelf, Add Book, Journal, Stats) map naturally onto this pattern on small screens.
- **Top nav on desktop** — horizontal nav bar at top for wider screens; same destinations.

**Card Patterns:**
- **Cover-first cards** — book cover dominates the card face; title/author below in smaller type. Progress strip along the bottom edge of the card.
- **Rounded corners + soft shadows** — `border-radius: 8–12px`, subtle `box-shadow` to lift cards off the background.
- **Fixed aspect ratio** — cards maintain portrait book proportions (roughly 2:3) regardless of screen size.

**Interaction Patterns:**
- **Tap card → contextual popup** — already decided (Variant B). Aligns with how Apple Books surfaces options on tap.
- **Smooth state transitions** — when a book moves from Started → Finished, the transition is animated, not an instant DOM swap.
- **Progress stepper** — simple +/- or direct numeric input; no sliders. Clean and fast.

**Visual Patterns:**
- **Warm off-white background** — not pure `#FFFFFF`; closer to `#FAF8F5` or `#F5F0EB`. Recalls paper.
- **One accent color** — used for primary actions (CTA buttons, active states) only. Everything else is neutrals.
- **Generous padding** — cards never touch each other or the viewport edge without breathing room.

### Anti-Patterns to Avoid

- **Goodreads pressure patterns** — challenge counters, reading streaks, friend activity feed, "you're behind on your reading challenge" alerts. All pressure-creating; antithetical to our emotional goals.
- **Over-animation** — gratuitous motion on every interaction cheapens the finish celebration. Reserve animation for that one earned moment.
- **Dense list views** — rows of text with no cover art feel like a spreadsheet. Covers are the identity of a book.
- **Modal stacking** — avoid stacking modals (confirm dialogs inside popups inside drawers). Navigation should feel fluid, never trapped.
- **Aggressive form validation** — red errors appearing while the user is still typing are irritating. Validate on blur, not on keystroke.

### Design Inspiration Strategy

**Adopt directly:**
- Cover-first card layout with warm background, rounded corners, soft shadows
- Bottom tab navigation on mobile; top nav on desktop
- Thin progress strip on book cards (visual, not numeric by default)
- Warm off-white as base background color
- System font stack: `-apple-system, BlinkMacSystemFont, 'Segoe UI', sans-serif`
- Single accent color for all primary CTAs

**Adapt for BookTracker:**
- Apple Books' long-press menu → our tap-to-popup (Variant B) for page updates
- Apple Books' library grid → Shelf grid sorted by last activity rather than manually arranged
- Apple Books' clean empty state → invitation empty state with warm encouraging copy

**Avoid entirely:**
- Any social, competitive, or streak-based patterns
- Heavy skeuomorphism (wood shelves, page curl effects) — warmth through color and type, not texture
- Pure white backgrounds — always slightly warm

## Design System Foundation

### Design System Choice

**Custom Component Library — Tailwind CSS + custom React components**

BookTracker uses Tailwind CSS as the styling utility layer with fully custom-authored React components. No third-party component library is introduced. All interactive controls (cards, popups, steppers, navigation, form inputs) are built from scratch and styled with Tailwind utility classes and scoped CSS where needed.

### Rationale for Selection

- **Apple Books fidelity** — no pre-built component brings its own visual opinions. Every shadow value, border radius, and color token is set to exactly match the warm native feel we're targeting. Nothing to override, nothing to fight.
- **Full ownership** — every component file is project code. No dependency updates breaking visual behaviour. No "the library changed its button padding in v5."
- **Tailwind utility model fits the aesthetic** — spacing scale, color palette, shadow presets, and responsive breakpoints are all configurable in `tailwind.config.js`. The warm color palette and Apple-derived tokens live in one place.
- **Appropriate scope** — BookTracker has a small, well-defined component set (book card, progress popup, stepper, stats strip, nav bar, form inputs, empty states, error panels). Custom-building each is entirely feasible.
- **Stack alignment** — React + Vite + TypeScript pairs naturally with Tailwind. No additional tooling complexity.

### Implementation Approach

**Token layer** — all design decisions defined as Tailwind config extensions:

| Token group | Values |
|---|---|
| Colors | `warm-bg`, `warm-surface`, `warm-border`, `accent`, `accent-hover`, `text-primary`, `text-secondary`, `error`, `error-bg` |
| Border radius | card: 12px, button: 8px, input: 8px, popup: 16px |
| Box shadow | `card-rest`, `card-hover`, `popup` |
| Font family | `-apple-system, BlinkMacSystemFont, 'Segoe UI', sans-serif` |

**Custom component inventory:**
- `BookCard` — cover image, title, author, progress strip, tap target
- `ProgressPopup` — modal overlay, page stepper, submit/cancel
- `PageStepper` — +/− controls + direct numeric input
- `StatsStrip` — anchored summary bar (pages this month, in-progress count)
- `NavBar` — bottom tabs (mobile), top bar (desktop)
- `BookForm` — ISBN lookup + manual fallback fields
- `EmptyState` — invitation variant + error variant
- `CelebrationOverlay` — finish animation + warm message
- `ErrorPanel` — neutral-red, factual copy, action link

Radix UI primitives may be used selectively for accessibility-critical behaviours only (focus trapping in popups, dialog ARIA roles) — zero visual output from them.

### Customization Strategy

All visual tokens are centralized in `tailwind.config.js`. Color palette, spacing, shadows, and typography are defined once and consumed everywhere. To adjust warmth or accent color globally, one config change propagates across all components.

## Core Interaction Design

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

## Visual Design Foundation

### Color System

**Base palette:**

| Token | Hex | Usage |
|---|---|---|
| `warm-bg` | `#FAF6F0` | App background — warm cream, like reading by lamplight |
| `warm-surface` | `#FFFFFF` | Card surfaces — pure white lifts cards off the cream bg |
| `warm-surface-alt` | `#F3EEE7` | Popup overlays, stats strip background |
| `warm-border` | `#E2D9CE` | Dividers, card borders, input outlines |
| `accent` | `#6B7555` | Primary CTAs, active nav, progress strip fill, links |
| `accent-hover` | `#556044` | Hover/pressed state for accent elements |
| `accent-subtle` | `#EBF0E6` | Accent tint for selected/highlighted backgrounds |
| `text-primary` | `#1C1A18` | Headings, labels — near-black with warmth |
| `text-secondary` | `#6B6259` | Supporting text, metadata, page counts |
| `text-disabled` | `#ADA49A` | Placeholder text, inactive controls |
| `error` | `#A84040` | Error messages, validation — neutral-red, not alarming |
| `error-bg` | `#FDF0EF` | Error panel backgrounds |
| `celebration` | `#C4874A` | Warm amber — used only in the finish celebration moment |

**Semantic mapping:**
- Progress fills, submit buttons, active nav items → `accent`
- Abandon button → `text-secondary` (subdued — abandoning is valid, not punished)
- Error panels (auth failures, network errors) → `error` text on `error-bg`
- Celebration overlay → `celebration` on `warm-bg`

**Accessibility:** `accent` (#6B7555) on white surface achieves ~4.6:1 contrast (WCAG AA). `text-primary` on `warm-bg` achieves AAA.

### Typography System

**Font stack:** `-apple-system, BlinkMacSystemFont, 'Segoe UI', sans-serif`

System font renders as SF Pro on Apple devices, Segoe UI on Windows — matching the native app feeling with zero font loading cost.

**Type scale:**

| Role | Size | Weight | Line height | Usage |
|---|---|---|---|---|
| `display` | 22px | 600 | 1.3 | Page headings (Shelf title, Stats heading) |
| `title` | 17px | 600 | 1.35 | Book title on card, popup heading |
| `body` | 15px | 400 | 1.5 | Author name, journal entries, form labels |
| `caption` | 13px | 400 | 1.4 | Page counts, dates, secondary metadata |
| `label` | 12px | 500 | 1.3 | Nav bar labels, badges, input hints |

**Principles:** All sizes in `rem`. Letter-spacing slightly loose on `label` (+0.02em) for legibility at small sizes. No custom web fonts.

### Spacing & Layout Foundation

**Base unit:** 4px. All spacing values are multiples of 4.

| Token | Value | Usage |
|---|---|---|
| `xs` | 4px | Icon gaps, tight inline spacing |
| `sm` | 8px | Card internal padding (tight), input padding |
| `md` | 16px | Standard card padding, section gaps |
| `lg` | 24px | Between cards in grid, popup padding |
| `xl` | 32px | Section margins, page-level padding |
| `2xl` | 48px | Top-of-page breathing room |

**Responsive grid:**
- Mobile (< 640px): 1 column, full-width cards, 16px horizontal margin
- Tablet (640–1024px): 2 columns, 16px gap
- Desktop (> 1024px): 3 columns, 24px gap, max-width 1200px centred

**Card spec:** 2:3 aspect ratio cover image, 12px border radius, `box-shadow: 0 2px 8px rgba(0,0,0,0.08)` at rest, `0 4px 16px rgba(0,0,0,0.12)` on hover.

### Accessibility Considerations

- All interactive elements minimum 44×44px touch target
- Focus rings: 2px solid `accent`, 2px offset — visible on keyboard navigation
- Error states use both color and text — never color alone
- Progress strip includes `aria-label` with numeric page value for screen readers
- Progress popup traps focus; Escape key dismisses; focus returns to triggering card on close
- Celebration overlay auto-dismisses after 3 seconds or on tap; no interaction required to proceed
