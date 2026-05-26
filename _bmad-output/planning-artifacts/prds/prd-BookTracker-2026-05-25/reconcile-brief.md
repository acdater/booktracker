# Reconciliation — brief.md vs prd.md + addendum.md

## Coverage summary
The PRD plus addendum capture the brief very well at the feature, scope, architecture, and roadmap levels. Most concrete content is preserved and elaborated; the remaining gaps are mainly qualitative framing details and one UX-level constraint that became weaker or internally inconsistent in the PRD.

## Gaps identified
- **Registration as a single-form experience is not explicitly preserved.** The brief says in **The Solution**: "Registration collects email, password, first name, last name, and date of birth in a single form." PRD §4.1 captures the fields but not the single-form UX constraint. This should be stated in **PRD §4.1 User Authentication** and/or **§6 Information Architecture**.

- **The brief's minimalist product principle is only implicit, not preserved as a governing rule.** In **Executive Summary**, the brief says: "The scope is deliberately minimal: every feature that exists works completely... and nothing is included that does not serve a core user need." The PRD carries parts of this idea, but the "nothing is included that does not serve a core user need" standard is not stated directly. It belongs in **PRD §1 Vision** or **§8 MVP Scope** as a scope-governing principle.

- **Some problem/positioning language is weakened.** In **The Problem**, the brief calls out Goodreads-style platforms as adding "noise, lock-in, and opinionated features" and spreadsheets as offering "no UX, no computed stats, and no reading-state logic." PRD §2.2 keeps the emotional idea, but the sharper product-positioning rationale is softened. This should be captured more explicitly in **PRD §1 Vision** or **§2 Jobs To Be Done**.

- **The "single valid next action" idea is weakened by the PRD's Started-state assumption.** The brief says in **The Solution** and **Scope**: "A context-aware action button always shows the valid next action." PRD §4.4 repeats that language, but FR-13 then defines Started as having **two buttons** ("Mark Finished" and "Abandon"). That weakens the brief's simpler interaction model and should be reconciled in **PRD §4.4 Reading Lifecycle**.

## Captured well
- Core functional scope: ISBN lookup, shared catalog, four-state lifecycle, page-progress event logging, stats strip, full stats page, re-read support.
- Shared-catalog / personal-record separation and event-log-driven stats model are preserved clearly and in more operational detail.
- Architecture notes from the brief are preserved appropriately in `addendum.md`.
- Explicit out-of-scope items from the brief are carried forward well into PRD non-goals / out-of-scope sections.
- Post-v1 vision extensions (streaks, queue, genre filters, CSV export, pace estimator) are preserved in the addendum/PRD roadmap context.
