# Design System Foundation

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
