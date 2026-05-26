# Visual Design Foundation

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
