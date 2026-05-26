# Executive Summary

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
