# Final refinement after v0.6.11 - review checkpoint (0.6.12 unreleased)

Branch `codex/kingmaker-buff-planner-final-refinement`, started from main
`fe7e2d0e2c7fe6d5032c4dff3193ba8224c22a99` (v0.6.11 product `3379d15` plus its
publication record). PR #20 was already merged; no other hotfix work existed.
Current candidate: **`54d26f17acd42ce699889f61cd65e824d647882a`** (after two PR #21
review rounds), version **0.6.12 (unreleased)**. Earlier candidates `433d98b` and
`e483daf` are superseded; their identities below are kept as history.

Not authorized and not done: merge, release, retag, replacing v0.6.11 assets,
installing into the owner's game, or touching any save.

## Evidence classes

| Class | What exists |
| --- | --- |
| Owner playtest (v0.6.11) | "Otherwise everything seems to be working well", plus the four requests in the mission package. Not a measured PASS. |
| Automated (this branch) | Offline C# tests through the real session/compiler/gate/executors/coordinator/report; only the native boundary is simulated. Listed below. |
| Native (this branch) | **None.** No game launch, render, physical input or gameplay run on 0.6.12. |
| Historical | `historical_0.6.10_output_log.txt` (0.6.10 / 6c75425) is context only. |

## Second PR #21 review corrections (candidate 54d26f1)

The review of bd9d01c confirmed items 1-5 above as fixed and found two remaining
P2 issues in secondary confirmation. Both were reproduced at bd9d01c in both
modes (`docs/evidence/final-refinement-0.6.12/review2-repro-bd9d01c.txt`).

| Item | Root cause | Correction | Evidence |
| --- | --- | --- | --- |
| R2-1 (P2) | The recorder dated an in-place refresh at the read that first saw the instance unchanged, so a same-instance primary refresh after a pending read was dated before its own marker | Each seen identity keeps every read at which it became new or refreshed relative to the pre-submission baseline (or was refreshed again); unchanged sightings are not applications. Boundary and same-read rules use those reads | `rf-new-mark` unconfirmed at bd9d01c; now confirmed, with spending and the successor. Pending-read same-instance primary + marker refresh confirmed; unrelated same-instance and other-unit applications refused |
| R2-2 (P2) | Only units within radius - tolerance set the required count, so a stationary recipient at 9.0 m of 9.144 m with no marker passed as empty | While the rule has room, every unit the native filter admits within radius + tolerance must be marked; empty only when all are readably beyond radius + tolerance | `bd-missing` accepted at bd9d01c; now refused. Boundary marked confirmed; band (9.6 m) missing and empty-prediction boundary missing refused; beyond-band (10.5 m) empty confirmed |

Validation on 54d26f1 (same DLL bytes as the package): focused (same 17
prefixes) **179/0**; full offline runner **706/0**; reader 19/0; source 47/0;
package 6/0. No native run.

| Candidate identity | Value |
| --- | --- |
| Commit | 54d26f17acd42ce699889f61cd65e824d647882a |
| ZIP SHA-256 | e4ef274b6cf46b2a1bcca59c2633052db0439365001049fb2eb32235ac6bd005 |
| DLL SHA-256 | 0dc9e659c46798d892ee8d135256b72cf4256f0d96d76263d1d5e25d89909503 |
| MVID | 40c8b4c1-74cc-4f24-b4f7-c58bae595820 |

## PR #21 review corrections (candidate e483daf)

The review of 2970940 found four P2 and one P3 issue (source-traced). All five
are corrected; the old candidate 433d98b is superseded.

| Item | Root cause | Correction | Evidence |
| --- | --- | --- | --- |
| 1 (P2) | Binding iterated predicted owners, so an empty prediction deleted the secondary obligation and its candidates | One selection leaf carries rule, prediction and candidates whatever the prediction size; candidates stay in projection identity | `e-missing` accepted at 2970940; now refused. Empty-to-B confirmed; B eligible with marker missing / wrong owner / unreadable refused; empty-prediction candidate change alters identity |
| 2 (P2) | Any fresh marker counted; the predicted owner skipped geometry | Application boundary = read that first saw this attempt's own effect on the centre. The adapter records there every unit the wrapper's native filter admits within reach (party or not). Marked units must be first seen at that read, inside reach, nearest-first consistent within 1 m, complete and at most `maximum`; no trusted prediction; unobservable eligible units, missing geometry or earlier/later unrelated applications stay unconfirmed | `pred-far`, `pred-no-geo`, `in-range-wrong` accepted at 2970940 (0/8 refused, both modes); now refused, plus `pending` (unrelated application while the command runs) and `unobservable-nearer` |
| 3 (P2) | PowerShell reader applied schema-9 captions to every schema >= 9 | Schema 10 expects "Plan: Cast available" / "Plan: Require full" and refuses idle Saved; schema 8/9 unchanged | `Test-NativeUiOutcomeReader.ps1` 19/0 (was 12), schema-10 positive and six negatives |
| 4 (P2) | Tooltip followed the navigator's retained focus and was raised over its own dialog | Eligibility follows the topmost owner (navigator dialog, Clear, Resources, workspace); opening a navigator dialog dismisses it; Clear's own button tooltips remain | `footer-refinement-tooltip-yields-to-navigator-dialog` (failed with the fix removed) |
| 5 (P3) | Applied Clear bypassed the command path, leaving a stale refusal as the standing notice | Applied Clear retires the command problem and problem navigation; a current save failure stays | `footer-refinement-clear-retires-stale-run-refusal`, `footer-refinement-clear-keeps-failed-autosave-notice` (both failed with the fix removed) |

Validation on e483daf: focused `-Only 'infectious-charms-,heroism-repeat-,playtest-heroism-,clear-refresh-,footer-refinement-,clear-plan-,footer-,autosave-,human-feedback-,cast-available-,native-ui-,controller-workspace-,undo-history-,casting-workspace-,policy-route-,execution-policy-,blocked-navigation-'` **177/0**; full offline runner **704/0**; reader 19/0; source 47/0; package 6/0. Tests ran on the same DLL bytes as the candidate. The tooltip test simulates the navigator dialog's construction/teardown; real dialog rendering and the live Unavailable route are not exercised. No native run.

| Candidate identity | Value |
| --- | --- |
| Commit | e483daf6aff6a130226857686983e5c88492e6ca |
| ZIP SHA-256 | a4deb8013c9a600c609b22621c638b81fad5bcb1edde3a657af86e2954ab1a30 |
| DLL SHA-256 | a2c5738186dbcf9bc99ba2da4a393a86416040ef96faa86cbed687c32b23b5f3 |
| MVID | c178a404-219a-40a4-abd3-4ef1a10cdbc4 |

The H2 description in section 5 below is superseded by this table where they differ.

## Status by section

| Section | Status |
| --- | --- |
| 1. Policy button + footer tooltips | Implemented, offline-verified; native check pending |
| 2. Idle footer/Saved removed, space reclaimed, error-only save notice | Implemented, offline-verified (computed heights); native check pending |
| 3. Native scroll Clear dialog | Implemented; **rendered acceptance pending** (no render exists) |
| 4. Clear/Undo resource truth | Root cause found and fixed, offline-verified; native check pending |
| 5. H1 / H2 | Reproduced on unpatched source, fixed, offline-verified; native check pending |

## 1-2. Footer, policy and tooltips

- The checkbox is an ordinary native button captioned with the CURRENT policy:
  "Plan: Cast available" / "Plan: Require full", sized so both captions fit at
  full font size. One activation toggles the same persisted availability
  policy once and autosaves; no format change, no migration rerun, no effect on
  execution mode, assignments or other preferences. HUD, Run, keyboard and
  controller routes still read the same policy.
- One tooltip panel (the workspace's own native surface, no new library) covers
  Resources, Plan, Mode, Clear plan, Undo, Run [routine], Retry save and the
  dialog's Cancel / Clear plan: current behaviour, what a click does, and any
  disabled/refusal reason. Shown on pointer hover and on the existing
  keyboard/controller focus; screen-clamped, non-raycasting, dismissed on modal
  open, Resources, rebuild, close and reload. Undo names a cleared-plan restore
  and never implies refunds.
- The idle status row, Saved label and footer strip are deleted, not blanked.
  One 54-unit action row remains and the lanes extend into the space. Problems
  use an error-only header notice with Retry save / Reload that takes no space
  when empty. The durability barrier (a run needs the latest edit saved) is
  unchanged. The catalogue's right-click hint stays.

Usable content height, computed by the actual layout code offline (not a screen
measurement):

| Layout | Before | After | Gained |
| --- | --- | --- | --- |
| 1920x1200 mouse | 820.3 | 916.8 | +96.6 |
| 1920x1080 mouse | 732.1 | 809.4 | +77.4 |
| 1920x1200 controller | 782.0 | 870.3 | +88.2 |
| 1920x1080 controller | 693.8 | 762.9 | +69.0 |

Before-state visuals are the owner's original 1920x1200 screenshots in the
mission package (`01_workspace_footer.png`, `02_clear_plan_modal.png`). No after
screenshot exists.

## 3. Clear dialog

The opaque `OpaqueParchment` tan rectangle and the PaperSlip card are removed.
The dialog draws the game's own DialogMessageBox scroll paper
(`dialogue_backsheet`) through the existing `ParchmentSurface`, the native scroll
rule under a centred title in the native title role, native body text and two
ordinary native buttons; height follows content. A flat framed tint remains only
as the fallback when native paper is unavailable. Casting count, all-routine
scope, active-buffs-remain and Undo text are retained. Cancel focus, Escape/Back,
the input gate, opening/closing-frame protections and focus to Undo after Clear
are unchanged. Visual acceptance requires a render that has not been made.

## 4. "None left after plan" after Clear

Root cause: wording, not a missed refresh. The plan computation is keyed to the
document revision and Clear/Undo bump it, so the next build was already fresh.
`CastingGraphText.BlockedReason` labelled every empty source "No casts left after
the rest of the plan" even when the native resource was genuinely spent. Each
source row now also evaluates the same native resources with no reservations:
only a plan-caused shortage keeps the plan wording; a genuinely empty source says
"No prepared uses remaining." / "No uses remaining.". Nothing is refunded and no
save format changes. Regressions (`clear-refresh-*`) cover reservation-only vs.
genuinely spent, shared pool / prepared slots / item charges across three
routines, Clear with a populated inspector/draft, Undo re-reservation,
routine/buff switch and Resources reopen; the genuinely-spent case failed on the
old code with the owner's wording.

## 5. Infectious Charms source-review findings

Both findings were reproduced first, on the unpatched released source, in a
scratch worktree at `fe7e2d0` (production unchanged; test-only fixture knobs).
Run: `Test-Development.ps1 -Only 'baseline-repro-'`, production build SHA-256
`58d00aef131969de21fedd41054bb087e8825f097ff470043e0508132c82c2ac`; **PASS=0 FAIL=4**:

| Repro | Mode | Observed on unpatched source |
| --- | --- | --- |
| H1: long Heroism, 3 s markers expired, skip-if-active | instant, animated | `skipped=0`, Heroism recast, prepared Heroism slots 2 -> 1 |
| H2: predicted `unit-t2`, native selection `unit-t3` at application | instant, animated | `confirmed=0;notProcessed=1;fired=cast-1` - successor stopped after a successful cast |

### H1 - implemented and verified offline; native check pending

- Discovery reads the exact installed `ContextActionApplyBuff` lifetime
  (`Permanent`, `UseDurationSeconds`, `DurationSeconds`, `DurationValue`). A
  structurally fixed lifetime is carried on the effect leaf, doubled as worst
  case; dice/rank/property durations are unknown and stay lasting obligations.
- An application whose maximum lifetime is below the 12 s (two-round) minimum
  can never pass the existing expiry check, so it is a confirmation obligation
  only. `ExistingEffectSufficiency` assesses the lasting effects alone, keeping
  every strength/caster-level/metamagic/owner/duration check; a casting with no
  lasting effect is never covered (`no-lasting-coverage`). No GUID, spell name
  or blanket conditional exclusion is involved.
- Confirmation still requires the transient marker, freshly applied by this
  attempt (old presence never counts). `EffectBaseline` now keeps per-attempt
  post-submission observations; the animated adapter records them every frame
  while its command runs, so a marker seen during application is not judged
  "never applied" after it expires before the executor's single final read.
- Installed Call of the Wild (`4EBF8E1E...`) applies both markers with
  `UseDurationSeconds`/3 s (reflection-only IL read; upstream `1332fb0` agrees).

### H2 - implemented and verified offline; native check pending

- The builder still predicts nearby owners (preflight omission/refusal of
  unresolved/unreadable owners is unchanged). Each bound secondary leaf now also
  carries the authorized candidates: every snapshot party member/pet the
  wrapper's own filter admits (excluding the centre; caster only when `Self`).
- Candidates are baselined before submission and are part of projection
  identity (`secondarySelection`, `transientApplication`; absent keys leave all
  other identities unchanged), so a changed set never reuses an allowance.
- Confirmation: 1..maximum FRESH applications among candidates; an unpredicted
  owner counts only with a native `DistanceTo` read inside radius + 1 m at its
  first sighting. Nothing applied is accepted only if every candidate was
  readably out of range at the read that first saw this attempt's effect on the
  centre. Wrong owner, out of range, over maximum, stale predicted marker,
  unreadable candidate and unreadable geometry stay unconfirmed and stop the run.
- Limitation: a non-party ally/summon selected at application cannot be
  observed; that is reported unconfirmed (conservative), not success.

## Commands and results (Windows, this machine)

| Command | Result |
| --- | --- |
| Baseline `Test-Development.ps1 -Only 'heroism-repeat-,playtest-heroism-,clear-plan-,footer-,autosave-,human-feedback-availability,cast-available-'` on fe7e2d0 | PASS=79 FAIL=0 |
| Unpatched repro `-Only 'baseline-repro-'` (scratch worktree) | PASS=0 FAIL=4 (findings reproduced) |
| `-Only 'infectious-charms-'` | PASS=5 FAIL=0 |
| H1/H2 full offline runner before merge | PASS=693 FAIL=0 |
| UI branch focused (new prefixes plus the 79-test set) / full runner | PASS=87 FAIL=0 / PASS=696 FAIL=0 |
| Merged `-Only 'infectious-charms-,heroism-repeat-,playtest-heroism-,clear-refresh-,footer-refinement-,clear-plan-,footer-,autosave-,human-feedback-,cast-available-,native-ui-,controller-workspace-,undo-history-,casting-workspace-,policy-route-,execution-policy-'` | PASS=154 FAIL=0 (28 s) |
| Merged full offline runner `artifacts\tests\KingmakerBuffPlanner.Tests.exe` | PASS=701 FAIL=0 (36 s) |
| `Build-Local.ps1` at 433d98b | source 47/0, build 1/0, package 6/0 |

The merged focused/full runs used the exact 433d98b source before the version
commit was recorded (only the generated commit stamp differs). Earlier receipts
(684/688 tests, 1e1a312 native records) belong to their own identities.

## Candidate (local only, not installed)

| Identity | Value |
| --- | --- |
| Commit | 433d98be3f6c1da0b5fad911cb8ad25edb6dca16 |
| ZIP | `artifacts/local-runtime/0.6.12/KingmakerBuffPlanner-0.6.12-local-runtime.zip` in the branch worktree (untracked) |
| ZIP SHA-256 | e190307935805b272f863e735d4e99b2ba82850f261bccd6a04c7d47e8990e18 |
| DLL SHA-256 | a903cdddaf06c63a32a3c4b9c36d9e4986945f6664f5561e348882779a3408c2 |
| MVID | 46c14479-6306-4b41-abbd-5bd0703e4753 |

## Remaining native checks (not run)

A guarded native run needs a frozen candidate, the launcher's live-purity
prerequisites (hours on this lab) and the native-UI observer whose schema-10
expectations are implemented but not natively exercised. The automation fixture
has no Heroism provider or Infectious Charms caster. Pending, exactly:

1. Rendered workspace at 1920x1200 (owner) and 1920x1080: action row, lanes, no
   empty strip; header error notice with Retry save on a forced save failure.
2. Both policy captions; tooltips on hover and on keyboard/controller focus,
   disabled reasons, no stale overlay after Clear/Undo/close/reload.
3. Clear dialog render (paper edges, padding, centred title, buttons), physical
   Cancel/Escape/controller Back, and Clear -> Undo focus.
4. Clear -> Undo with one caster genuinely spent and one exhausted only by
   reservations.
5. Feat-present Heroism with Infectious Charms in both modes: first, skip after
   marker expiry, recast, and an A->B nearby change, each with a distinct
   successor and actual spending. Feat-absent separately.

The classic (legacy) preview screen's presence check was not changed; production
runs (HUD, workspace Run, keyboard, controller) use the casting-first path fixed
here.

## Owner playtest checklist

- Footer: one row; "Plan: ..." states the current policy; hover each button.
- Toggle Plan twice, close and reopen: the choice persists; Mode is unchanged.
- Clear plan: scroll dialog with a centred title; Cancel; Clear; Undo restores.
- After Clear, Leinna no longer reads "None left after the plan"; if her slots
  are really spent it reads "No prepared uses remaining."
- Heroism with Infectious Charms: run, wait a few seconds, run again with skip:
  no second Heroism cast, and a following spell still runs.
