# Autonomous Blockers

Status: IN PROGRESS

## Full-screen UI and input isolation

Current classification: ACTIVE REPAIR — not a hard stop.

Evidence: human playtesting of installed 0.0.1 proved the floating IMGUI strip is unacceptable, clicks reach world movement, and Long has no visible result. Static exact-assembly inspection confirmed the old IMGUI surface cannot satisfy `PointerController.InGui`, and the old gate incorrectly required zero blockers. Safe investigation, implementation, tests, no-save runtime scenarios, packaging, and guarded replacement installation remain independently actionable.

## Save-backed runtime qualification

Current classification: DEFER — EVIDENCED while independent work remains.

Evidence: the live save inventory contains no `KBP_` entry. Existing saves, including `KMG_` fixtures belonging to another product, are protected. The mission permits continued source-only and NO-SAVE work. Phase 9 therefore records 413 included candidates as `DEFER-runtime-qualification`, not PASS. Final save-backed qualification requires a safely distinguishable project-owned baseline/working pair created or imported through a guarded process.

## Tabletop Added Rules compatibility

Current classification: UNAVAILABLE-LOCAL-REFERENCE.

Evidence: transfer inventory and local examples contain no Tabletop Added Rules package or immutable snapshot. Strict Tabletop-only and combined profiles fail closed as `unavailable-local-reference`; Shield Other is recorded `FEATURE-NOT-PRESENT-IN-SNAPSHOT`. This explicitly optional row does not block native completion.

## Steam/offline runtime boundary

Current classification: PASS for current-session NO-SAVE preflight; revalidation required immediately before every launch.

Evidence: the current Steam session briefly attempted an App 640820 cloud download at startup, then logged off. Later App 640820 records establish `Sync Disabled` and `offlineMode=true`, after the last successful transfer; the app manifest remains fully installed at build ID 6757524. The harness derives sanitized timestamps and policy state from current-session logs and refuses launch if a later login or transfer supersedes them. It also refuses while the UMM installer or Kingmaker is running. No dialog or account state was manipulated.
