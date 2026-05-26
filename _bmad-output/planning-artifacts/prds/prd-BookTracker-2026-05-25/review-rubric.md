# PRD Quality Review — BookTracker

## Overall verdict
This PRD is materially useful: the feature set is concrete, the event-log thesis is consistent, and the FRs are usually written with real testable consequences. What puts it at risk is not lack of detail but a few unresolved or internally conflicting decisions at exactly the points UX and architecture will need firmness—especially the re-read/history model and the product's split identity between reader app and BMad reference app.

## Decision-readiness — adequate
The document does make real decisions instead of hiding them in vague prose. Examples include the explicit fallback to manual entry when Open Library fails (§4.2 FR-5, "an empty editable form is presented with no error blocking submission"), the no-cache stats posture (§4.8 FR-23), and the addendum's explicit architecture-facing choices in §B.1–B.3, especially "All stats derived from BookAction event-log queries at request time."

What keeps this from strong is that two of the most consequential UX/data-shape decisions are both assumed and still open. §4.3 FR-9 assumes "Only the most-recent UserBook per Book" is shown on Shelf, and §3 Glossary / §4.5 FR-16 assume the Reading Journal is scoped to the current UserBook, but §10.2 and §10.3 reopen both questions. That is honest, but not yet fully decision-ready for downstream work.

### Findings
- **critical** Re-read/history model still unresolved (§4.3 FR-9; §4.5 FR-16; §10.2–§10.3) — The PRD currently assumes "Only the most-recent UserBook per Book" and a Journal "scoped to the current UserBook," then asks whether Shelf should show all UserBook records and whether Journal should aggregate across readingNumbers. Those choices change navigation, query shape, and what history the user can recover. *Fix:* Resolve both questions before UX and architecture start, or mark one binding MVP decision and move the alternative to post-v1.

## Substance over theater — strong
Most of the document's detail is earned. The event-log model in §1 is not decorative language; it is carried through FR-14 through FR-23, the Stats Strip/Page formulas, and addendum §B.2. The NFRs also avoid boilerplate by using concrete thresholds such as "< 2 seconds for a User with up to 500 BookAction events" (§5.2) and a specific navigability rule of "within three file-tree traversals" (§5.3).

The one theatrical risk is not verbosity but persona framing: the PRD sometimes speaks like a product for "individual readers" (§1) and sometimes like a showcase for "the BMad Method developer" (§2.1). That is a shape problem more than empty furniture, so it matters more in later dimensions than here.

## Strategic coherence — adequate
The PRD has a recognizable thesis: a self-hosted reading tracker whose differentiator is the event-log architecture and artifact legibility. §1 ties that thesis to the feature set, and the feature list mostly follows it cleanly: shared Catalog, per-user UserBooks, immutable BookActions, and stats derived from the log all reinforce the same bet. The addendum strengthens this with explicit rationale, e.g. §B.2's claim that request-time queries "keep the schema simple" while still supporting extension.

Success metrics also mostly validate the thesis rather than generic activity. SM-2 and SM-3 test end-to-end usability and stats correctness instead of vanity metrics.

### Findings
- **medium** Thesis-to-metric gap on architectural clarity (§1; §5.3; §9) — The PRD says "Architectural clarity is a feature, not an afterthought" and makes code navigability "the primary success criterion for the BMad Method demonstration purpose," but §9 has no success metric that verifies navigability or artifact-legibility directly. *Fix:* Add at least one explicit metric or acceptance check for §5.3, such as interface pairing completeness or a time-boxed class-locatability review.

## Done-ness clarity — adequate
This is one of the stronger parts of the PRD. Nearly every FR has at least one verifiable consequence: HTTP status codes in §4.1, field requirements in §4.2, exact formulas in §4.7–§4.8, and quantitative NFR bounds in §5.2. Downstream story creation will have plenty to source-extract.

The main weakness is not vagueness but contradiction. Where the document states a single interaction model, it needs to stay single.

### Findings
- **high** Action model contradicts itself (§4.4 description; §4.4 FR-13) — The feature description says "The button always shows the single valid next action for the current Reading Status," but FR-13 specifies that Started shows "two buttons — 'Mark Finished' and 'Abandon.'" That is a direct conflict in the core interaction model. *Fix:* Rewrite §4.4 to match the intended control scheme (single action vs. multiple valid actions) and propagate the same wording to UX-facing sections.

## Scope honesty — strong
The document is notably honest about what v1 is not. §7 Non-Goals is explicit and useful, §8.2 repeats the de-scoping in roadmap language, and §10 Open Questions names real unresolved items instead of hiding them in prose. The PRD also uses assumptions visibly, and the addendum keeps architecture notes out of the requirements body rather than laundering them into false certainty.

The open-items density is acceptable for a draft PRD, but because §10.2–§10.3 hit load-bearing flows, this honesty still needs conversion into decisions before downstream execution.

## Downstream usability — adequate
This PRD is generally built for handoff. §0 explicitly frames it for "UX designer, architect, developer agent," the glossary is substantial, FR numbering is contiguous through FR-23, and success metrics / assumptions are indexed and cross-referenceable. Most sections can be extracted on their own without collapsing into "see above" dependencies.

Its main usability weakness is not ID hygiene but source-of-truth ambiguity around personas and a few assumptions/mechanical roundtrips, noted below.

## Shape fit — thin
The PRD has not fully decided whether it is primarily a user-product PRD or a reference-application capability spec. §1 opens with "personal reading management" for "individual readers," but §2.1 defines the primary persona as "The BMad Method developer" whose "primary stake is in the artifact trail and codebase structure." Then §2.4 uses Alex and Sam as journey actors, with "Sam, individual reader" carrying two key journeys despite not being defined as a persona in §2.1.

That mixed shape is workable, but it weakens prioritization. A UX designer, architect, or story writer will not know whether to optimize first for reader ergonomics, for demo legibility, or for both with explicit trade-offs.

### Findings
- **high** Primary persona does not match the journey load (§1; §2.1; §2.4) — The PRD says the app serves "individual readers," but the only defined primary persona is the "BMad Method developer," while UJ-2 and UJ-4 center "Sam, individual reader" without a corresponding persona definition. That makes the document under-formed for a UX-heavy product shape. *Fix:* Either define explicit prioritized personas (e.g. primary = BMad developer, secondary = individual reader) and map journeys to them, or recast the document as a capability/reference-app spec and trim consumer-product persona language.

## Mechanical notes
- UJ persona linkage is not clean: UJ-1/UJ-3 use Alex and UJ-2/UJ-4 use Sam, but §2.1 defines only "The BMad Method developer" rather than exact persona labels that UJs can reference.
- Assumptions Index roundtrip is imperfect. A-2, A-10, and A-13 appear in §11, but the corresponding source locations do not carry explicit inline `[ASSUMPTION: ...]` tags in the same way most other assumptions do.
- ID continuity looks good: FR-1 through FR-23, SM-1 through SM-6 plus SM-C1/SM-C2, and A-1 through A-14 are contiguous and unique.
- Cross-references are mostly usable, but §10 reopens decisions already assumed in A-6 and A-9; resolve those to avoid downstream source-of-truth drift.
