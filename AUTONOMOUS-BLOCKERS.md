# Autonomous Blockers

## Current R2 handoff

Current classification: INSTALLED — 0.0.2 human verdict FAIL; authoritative 0.0.3 human verdict pending.

Evidence: 0.0.3 replaces cloned native HUD objects with fresh retained-mode controls, makes modal opening presentation-first, and counts only confirmed expected effects as successful execution. It passes 21/21 source and 57/57 behavior gates plus two native 12/12 and two Call of the Wild 26/26 exact-release runs. The guarded install preserved settings and changed no non-planner mod. The campaign-only UI gate correctly returned `BLOCKED` because no authorized `KBP_` campaign exists; direct HUD/modal/input/Bless acceptance therefore remains the mandatory human boundary.

Status: IN PROGRESS

## R2 human rejection

Historical classification before installation: 0.0.2 human verdict FAIL; 0.0.3 qualification was pending.

Evidence: direct human playtesting proved cloned HUD controls overlap and activate native turn-based/pause controls, and proved F10 could acquire full-screen input suppression without a rendered planner. The current handoff section above supersedes this pre-install checkpoint.

## Full-screen UI and input isolation

Current classification: IMPLEMENTED — guarded campaign runtime/human verdict pending.

Evidence: 0.0.2 removes the IMGUI strip and implements the required native HUD/full-screen/input lifecycle. Source validation is 19/19 and behavior/protocol is 56/56. The corrected runtime gate requires a campaign `StaticCanvas` and returned the explicit campaign-UI precondition at the main menu; it does not re-certify the defect. Guarded replacement installation and human playtesting remain actionable.

## Save-backed runtime qualification

Current classification: DEFER — EVIDENCED while independent work remains.

Evidence: the live save inventory contains no `KBP_` entry. Existing saves, including `KMG_` fixtures belonging to another product, are protected. The mission permits continued source-only and NO-SAVE work. Phase 9 therefore records 413 included candidates as `DEFER-runtime-qualification`, not PASS. Final save-backed qualification requires a safely distinguishable project-owned baseline/working pair created or imported through a guarded process.

## Tabletop Added Rules compatibility

Current classification: UNAVAILABLE-LOCAL-REFERENCE.

Evidence: transfer inventory and local examples contain no Tabletop Added Rules package or immutable snapshot. Strict Tabletop-only and combined profiles fail closed as `unavailable-local-reference`; Shield Other is recorded `FEATURE-NOT-PRESENT-IN-SNAPSHOT`. This explicitly optional row does not block native completion.

## Steam/offline runtime boundary

Current classification: PASS for current-session NO-SAVE preflight; revalidation required immediately before every launch.

Evidence: the current Steam session briefly attempted an App 640820 cloud download at startup, then logged off. Later App 640820 records establish `Sync Disabled` and `offlineMode=true`, after the last successful transfer; the app manifest remains fully installed at build ID 6757524. The harness derives sanitized timestamps and policy state from current-session logs and refuses launch if a later login or transfer supersedes them. It also refuses while the UMM installer or Kingmaker is running. No dialog or account state was manipulated.
