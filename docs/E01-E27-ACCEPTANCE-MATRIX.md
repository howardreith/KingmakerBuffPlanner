# E01–E27 acceptance matrix — everyday-use v1.2 private preview

Status at the 2026-10-02 stopping point. Candidate 3c1c5d4a5193d3d84aa2
05e234c0a9b09b4face7 (package 1b6d67c0f7fd5ba8ba8d4a9047073500e0a5c3e
01a76851c6e13ae410a2d5665, DLL c727c5e2565adff320d9c53a313e815f57c479e
9d198dcb6188542214462f7a3, MVID 6fb77f64-c7f7-4fbb-8340-5e6ca6c839f6),
full gate PASS at this exact tree (artifacts/gate-3c1c5d4.log).

Labels: source (assembly-backed protocol test), native-UI (guarded live
run), NOT-RUN (the r13 batch will produce it), BLOCKED (concrete
blocker recorded). Historical evidence keeps its own commit and is
never relabelled.

| E | Acceptance item | Verdict | Evidence |
| --- | --- | --- | --- |
| E01 | Casting-first default; Instant default, explicit Animated kept | PASS | source: defaults/migration tests (protocol 390/390); native-UI: every guided run opened casting-first (beta-f0cf4f16r11-w1080-01, -adv-01, -reload-01) |
| E02 | Explicit Animated preference never reset | PASS | source: profile default/migration tests |
| E03 | Casting-first is the sole normal route (importer + rollback retained) | PASS | source: route wiring tests; native-UI: ui-reload run beta-8a53ced8r12-reload-01 (done+verified) |
| E04 | Continuous parchment scroll, no book gutter | PASS | native-UI: beta-f0cf4f16r11-w1080-01 presentation evidence "page=continuous-scroll;book-art-retired" |
| E05 | Right-click opens the full native spell description | PASS | native-UI: beta-f0cf4f16r11-phys-sel-01 — inspectChip=chip:seed-long-1, InspectOpened, all cf-* actions acknowledged |
| E06 | Inspect lifecycle isolation; no document mutation by browsing | PASS | native-UI: same run — document signature equal before/after, inspect closed by Escape |
| E07 | Every deliberate edit saves itself (no Save button) | PASS | source: autosave boundary tests; native-UI: beta-f0cf4f16r11-w1080-01 "saveControl=already-ready;saved=True" + session release "autosave=saved" |
| E08 | Monotonic revisions; campaign-bound single writer | PASS | source: repository/revision tests |
| E09 | Flush-before-run/close | PASS | source: flush tests; native-UI: reload run reconstruction |
| E10 | No save storms from browsing | PASS | source: browse-no-write tests; native-UI: hover/browse no-mutation evidence |
| E11 | Save failure keeps in-memory + last good; run refuses while not durable | PASS | source: failure-path tests |
| E12 | Cold moon left-click runs Long only, editor stays closed | PARTIAL → r13 | native-UI selection contract PROVED: beta-f0cf4f16r11-phys-sel-01 — press delivered, refused BY THE LOCK (cf-grant-absent), zero violations; the cast run (grant-armed, one run) is built as ui-phys-cast in the r13 batch: NOT-RUN yet |
| E13 | Scroll frames judged from presented frames | PASS | native-UI: same run — graphWheelEvidence from the physical wheel + captured frames |
| E14 | Upgrade/import from prior profile | NOT-RUN | r13 job ui-import (the scenario and its import/archive rollback contract are gate-tested) |
| E15 | Persistence across restart | PASS | native-UI: beta-f0cf4f16r11-w1080-01 reopen "load=Loaded...preserved=True"; beta-8a53ced8r12-reload-01 reload reconstruction (done+verified) |
| E26 | Autosave equals persistence across restart | PASS | same evidence as E15 |
| E16 | Share Transmutation selectable (exact caster/source, arm Share, ally) | PASS | source: recipe/selection tests (shared-personal/shared-powerful in protocol suite); native-UI chains built for r13: sel runs NOT-RUN yet |
| E17 | Combined cost budgeted atomically (reservoir + slot) | PASS | source: cost-vector tests; native-UI cast runs NOT-RUN (r13) |
| E18 | Disabled Share intent visible/blocked/repairable; lossless recovery | PASS | source: recovery/undo tests (R579/C853 repairs) |
| E19 | Share cleanup verified; no leak to the next cast | PASS | source: isolation-judge tests (TestSharedIsolationJudge); native-UI NOT-RUN (r13) |
| E20 | Combined Share + Powerful Change | PASS | source: TestSharedPowerfulCombined; native-UI NOT-RUN (r13) |
| E21 | Enhancements offered are meaningful per native semantics | PASS | source: relevance policy tests; native-UI: advanced budget phases (r11 adv run) |
| E22 | Already-selected odd options stay visible/removable | PASS | source: visibility tests |
| E23 | Beneficial catalogue excludes non-beneficial | PASS | source: classifier tests + catalog evidence (every guided run's discovery lines) |
| E24 | Fire Shield and legitimate variants stay | PASS | source: variant normalization tests; native-UI variant-eligibility traces (r11 runs) |
| E25 | Global accounting preserved (combined pools) | PASS | native-UI: beta-f0cf4f16r11-w1080-01 one-pass budget phases; advanced chain NOT-RUN (r13) |
| E27 | Prior beta package untouched; rollback preserved | PASS | draft release asset digest 214b795b…8b20564 re-verified unchanged at delivery time |

The r13 batch (jobs built at dispatch/jobs.json, run IDs
beta-3c1c5d4ar13-*) will fill every NOT-RUN cell on the delivered
candidate before the private delivery is sent.
