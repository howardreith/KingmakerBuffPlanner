# Controller workspace navigation repair — October 20, 2026

## Windows integration checkpoint

The verified handoff bundle was imported onto a new branch from the current
default branch. This section records the Windows result; the original handoff
account below remains attributed to its earlier source and environment.

- Branch: `codex/kingmaker-buff-planner-controller-navigation-review`.
- Default-branch base: `6914ef2` (`origin/main` at integration).
- Integrated source commit: `0887225`; imported documentation commit: `8acf31c`.
- Version: `0.6.4`, following the published stable release on `main`.
- `git diff --check origin/main...HEAD`: passed.
- `powershell.exe -NoProfile -ExecutionPolicy Bypass -File scripts/Test-Development.ps1 -Only 'controller-workspace-'`:
  source validation **47/0**, production build **1/0**, controller workspace
  tests **18/0**, in 10.5 seconds. Build DLL SHA-256:
  `aac0ca90bf9f5574cf25da9e6748085020558725bb9cce35b2b9098d7851472b`.
- Local log: `artifacts/validation/controller-navigation-development.log`
  (ignored build evidence in the integration worktree).
- Native input and visual acceptance remain **NOT RUN**. The Windows build and
  focused tests establish a development checkpoint, not a game qualification.

The imported source files match the handoff commit. Changes on `main` since its
base affected version and release records, so no product-source conflict arose.
The exact rendered focus behavior, including the reported six-second Spells
case, still needs a guarded in-game observation before acceptance.

## Original handoff status and identity

Source repair prepared and reviewed locally; native visual acceptance is pending.

- Branch: `codex/kingmaker-buff-planner-controller-navigation`.
- Base: `df61617dc592df54bd3285c48d7eafe5aa66373c` (current main at intake,
  including PR12 and PR13).
- Product-source commit: `e9dee2156de48a60e64f283fea4a4d3416e316c6`.
- Version remains `0.6.4-alpha.1`; no DLL or release package was built here.
- No game launch, installation, save mutation, push, PR, merge, or release occurred.
- This record is a later documentation commit, not another product build.

The owner asked for the controller menu's selection order and method to become
intuitive after losing the focus marker when moving left and right. This is an
ordinary development checkpoint under [VALIDATION-POLICY.md](VALIDATION-POLICY.md),
not a resumption of an older broad runtime/release campaign.

## Evidence from the owner's recording

Input: `Kingmaker Controller Recording - Trim.mp4`, 39.59 seconds, 1920x1080.
The complete recording was sampled, with quarter-second inspection around the
focus loss. Times below are approximate; no controller input overlay was present.

| Time | Observation |
| --- | --- |
| 0:06 | The workspace opens with Close highlighted at the top right. |
| 0:08.5–0:09 | All and then Spells show a blue focus highlight. |
| 0:09.25–0:14.5 | The footer still reads `Catalogue > Spells`, but the blue focus marker is absent. |
| About 0:14.75 | Moving to another filter restores visible highlighting. |
| 0:19.5–0:22.5 | Catalogue navigation scrolls the highlighted row into view. This sequence does not establish a catalogue auto-reveal failure. |
| About 0:23 | Selecting Countless Eyes updates the graph. |

The recording proves a visible-focus failure while logical focus survives.
It does not prove the exact rendering mechanism or identify every directional
button press. A stationary mouse pointer is distinct from controller focus.

## Source findings

The old `ControllerWorkspaceFocus.Move` treated left/right as a circular list of
regions, and up/down as a circular list of controls in each region. The region
order was Header, Catalogue, Casters, Castings, Recipients, Inspector, Footer.
Consequently a horizontal command could jump vertically or across the screen.

Control enumeration also disagreed with visible placement: Search was appended
after the catalogue's buttons, footer buttons were created from right to left,
and horizontal filter/routine/inspector button rows used vertical navigation.
The initial discovered header button was Close.

The focus image was reparented under the selected control, hidden during rebuild
preparation, and used a translucent Image plus Outline. The precise cause of the
recorded disappearance was not reproduced in Unity. The replacement addresses
those rendering dependencies without claiming an identified engine defect.

The old explicit right-stick scroll path could retain an offscreen actionable
focus key. This is a separate source finding, not a failure demonstrated by the
catalogue-scrolling portion of the video.

## Implemented behavior

1. Directional movement uses the actual control rectangles. Horizontal button
   rows follow screen order, independently of hierarchy construction order.
2. Left/right moves between neighboring body columns and prefers a visible
   control at a similar height. Empty columns are skipped. Screen edges do not wrap.
3. Up/down traverses rows and connects the header, body, and footer spatially.
   Long scroll lists retain offscreen next/previous rows as navigation candidates.
4. Horizontal aim survives vertical movement through wide rows: Spells → down
   to a buff → up returns to Spells. Inspector pairs behave the same way.
5. Fixed search/filter controls cannot intercept movement inside a scrolled
   list. Horizontal same-row selection requires the same scroll container, so
   the full bounds of a partially clipped row cannot hit a filter behind it.
6. First populated view focuses the selected buff. Initialization waits until
   that view exists; an empty catalogue retains a useful catalogue fallback.
7. The marker is a persistent root-owned overlay with four opaque border strips
   and a faint fill. It is positioned every planner frame and explicitly clipped
   to the visible target. It is not a child of rebuilt/masked rows.
8. Scrolling reconciles focus to a visible item in the same pane. A pane with no
   visible actionable item reveals its retained item, or keeps a visible close
   control. Confirm refuses an offscreen item as a final defense.
9. Semantic identity persists through rebuilds. Removing a focused item chooses
   a nearby surviving item. Disabled controls remain inspectable without executing.
10. Controller help text describes adjacent controls. Routine bumpers, modal
    Back behavior, native-card entry, execution handoffs and Stop are preserved.

The view still delegates all actions to existing control callbacks. Navigation
does not introduce plan writes or execution calls. No project library was added.

## Validation performed

| Check | Result and limit |
| --- | --- |
| Actual production focus model plus new regression file | 13 cases passed, 0 failed |
| Existing focus identity/disabled, input repeat, keyboard apply/cancel cases | 3 cases passed, 0 failed; method bodies extracted verbatim from the repository into the local runner |
| Combined focused run | **16 passed, 0 failed** |
| Changed C# syntax in language version 7.3 | **6 files passed, 0 failed** |
| `git diff --check` | Passed |
| Full production/game-assembly compilation | **NOT RUN** |
| Native controller input and rendered visual acceptance | **NOT RUN** |

The focused runner used Microsoft .NET SDK 8.0.426/runtime 8.0.32, C# 7.3 and
warnings as errors. It linked the actual production model and actual test file;
no Unity mocks were used. The changed Unity bridge received syntax and source
review, not a game-assembly type check. An exact production build remains needed.

The final partial-row overlap regression first failed **15/1** with the new
regression assertion, then passed **16/0** after restricting same-row horizontal
targets to the same scroll container. Both outcomes are retained. The final
focused run and syntax check completed together in about 1.45 seconds.

Evidence included in the handoff archive:

- `validation/focus-verified-pass.log` and `validation/syntax-verified-pass.log`.
- `validation/masked-row-before-fix.log` (retained failure).
- `validation/verified-source-sha256.json` (all changed source/test files).
- The portable pure-source runner, exact neighboring-test extractor, and test
  source registration; these do not replace the project's Windows build route.
- Original extracted video frames before and during the disappearing highlight.

The Windows development device was offline. This environment lacks the
Kingmaker/Unity managed assemblies and the external guarded push helper at
`C:\Dev\KingmakerBuffPlannerLab\codex-policy\Push-KingmakerBuffPlanner.ps1`.
The repository requires that helper for publication, so a source bundle and
reviewable patch are the handoff; direct git/API publication was not substituted.

## Focused native acceptance still needed

After the existing guarded runtime prerequisites, use the following focused
native acceptance sequence. Record the exact candidate DLL/hash and screenshots;
model tests and this owner's original recording are not candidate visual proof.

1. Open from the native Buff Planner card. The selected buff has an obvious border.
2. Focus Spells, release all controls, and wait at least six seconds. The border
   remains visible; repeat after filtering and a workspace rebuild.
3. Traverse All, Spells, Abilities, Other with left/right. Move from each filter
   down to the list and back up; return to the same filter. Up reaches Search.
4. Traverse the buff, caster/source, casting, recipient, and options columns in
   both directions, including a buff with no castings. Outer edges do not wrap.
5. Traverse a long list, then right-stick scroll. Confirm always addresses the
   visibly bordered control. Test a partially clipped first visible row.
6. Traverse the footer in physical order: Resources, policy, mode, Undo, Run.
   Do not activate Run during the navigation-only portion.
7. Verify paired inspector actions, disabled controls, Resources, description
   and search modals, and Back restoration. Change a filter/routine and verify
   the remaining focus is sensible. Navigation-only inputs leave the plan unchanged.
8. On the permitted disposable working state, exercise an add/remove casting
   rebuild. Follow the existing owned run/Stop scenario if selected; do not use
   a protected personal save for mutations.
9. Observe at 1920x1080 (the reported case) and the supported smaller layout
   before declaring this substantial input change visually accepted. Preserve
   the existing runtime ownership and restoration checks.

Public release publication remains a separate boundary. The source retains the
existing version and must not be presented as a newly qualified release binary.
