# Casting-graph UI correction — handoff to the next operator (Z.AI)

Written 2026-09-26 by the outgoing operator (Claude) at the owner's request:
Claude's weekly quota ends before the remaining live work can run, and Z.AI
continues. This file is self-contained: follow it top to bottom. When a step
here conflicts with `AGENTS.md` or the mission, those win; stop and ask the
owner.

## 1. Read first (in this order)

1. `AGENTS.md` — product boundary, architecture, runtime safety, Git and
   publication rules.
2. `planning/CASTING-GRAPH-UI-CORRECTION-MISSION.md` — the mission (owner,
   2026-09-25): scope, what is not authorized, required evidence, final
   report format (§14).
3. `planning/CASTING-GRAPH-UI-ADDENDUM-v1.1.md` — the product spec.
4. `planning/CASTING-GRAPH-UI-CORRECTION-STATUS.md` — the tracker: Phase 0
   identity proof, G01–G15 ledger, checkpoints 0–2 (exact commits, counts,
   run results).
5. `docs/MANUAL-USABILITY-HANDOFF.md` (top section) — the owner's in-game
   review procedure and checklist.

## 2. State at handoff

- **Branch:** `codex/kingmaker-buff-planner-casting-graph`, pushed through
  the guarded helper; draft stacked PR
  https://github.com/howardreith/KingmakerBuffPlanner/pull/3 onto
  `codex/kingmaker-buff-planner-casting-first` (PR #2 head `e8496ee`).
  The local branch `claude/casting-graph-ui-correction` holds the same
  commits; work only on the `codex/` branch from now on.
- **Owner decisions (2026-09-26):** publish under the `codex/` name through
  the unchanged helper (done); the owner's in-game review happens "later
  today" with Z.AI operating.
- **Version string:** `0.2.0-rc6`, unchanged on purpose (development builds
  keep the current version; the mission forbids a new release-candidate
  version before owner acceptance). Builds are identified by commit, package
  SHA-256, DLL SHA-256 and MVID.
- **Installed on this machine (never changed by this work):** Kingmaker Buff
  Planner 0.1.1-rc3, DLL SHA-256 prefix `78407DD4C7240CE2`; Kingmaker
  Gunslinger 0.0.136, DLL SHA-256 prefix `C6CCCDAC914ED59F`.
- **Source:** complete for the addendum (graph workspace, capacity from the
  plan ledger, one-casting inspector, group branches, readable buttons, one
  pointer highlight, mode labels). An independent review was repaired in
  `8e6a4e0`. The full source-only gate passed at `3ebdfe1`: source 42,
  protocol 362, harness 38, package 4, deployment WhatIf 5, launcher WhatIf
  12, fixture 3, Restore-InstallLocal 16, publisher 3.
- **Live so far:**
  - `casting-graph-qual-1200-01` (`b1cb4e0`): FAIL only at the hover record
    (the harness looked for Classic cards by their pool name; bound cards
    are `Source.<sourceId>`; fixed in `590ca28`). Everything else PASS. All
    30 judged hover readings single-owner, aligned to the real cursor within
    2 px.
  - `casting-graph-qual-1200-02` (`3ebdfe1`): FAIL because Windows refused
    to give the game the foreground (`Kingmaker foreground activation
    failed` — someone was using another window in the session), so most
    cursor moves were withheld by design. It did show the mechanism: rc6
    card click `tookSelection=True`, shipped `tookSelection=False`, and the
    shipped click-then-hover had one highlight.
  - Both: Kingmaker exited, Mods restoration verified, protected saves clean.

## 3. What remains, in order

1. Reconcile (§5), build and gate (§6) at the exact HEAD.
2. Four guarded runs with the owner connected and hands off (§7):
   qual 1920x1200, qual windowed 1920x1080, qual on the Advanced fixture,
   reload.
3. Read each result (§8); repair defects with tests; repeat 1–2 after any
   code change.
4. Measure caption contrast from the presented `button-states.png` (§9).
5. Update the tracker, journal and resume; push; update PR #3 (§11).
6. The owner's supervised review (§10); record the verdict verbatim.
7. Final report in the mission's §14 format (§12). Stop there: merge,
   release, tag, permanent install are not authorized.

## 4. Rules that must never bend (verbatim sources)

From the mission: "Not authorized: merge to `main`; publication, tagging,
release, or stable promotion; permanent installation; deletion of Classic;
destructive history changes; testing automation on a valued normal save;
weakening locks, transactions, rollback, or save protections; expanding into
Share Transmutation, exact physical rod identity, pets, or unrelated charter
gaps; another generalized harness redesign." "On every guarded run: acquire
the shared installation lock atomically; recheck other project locks and
running Kingmaker; prove staged artifact identity; preserve and compare Mods
and approved saves; restore on all terminal paths; stop immediately on
incomplete restoration." "Do not create a release candidate version or tag
until the owner accepts the corrected workspace." "Use an approved
disposable fixture for automated runs. Do not automate a valued normal
save." "Do not infer acceptance from automated callbacks or screenshots."

From `AGENTS.md`: "Only `KBP_AUTOMATION_WORKING` may be mutable.
`KBP_AUTOMATION_BASELINE` is immutable and every other save is protected."
"Stop on unexpected Steam, account, cloud, update, purchase, or credential
UI." "Never claim runtime qualification from compilation, detached
reflection, or a main-menu load alone." "Publish only through the
project-owned guarded push helper." "Never use destructive history/worktree
commands to discard unknown state." "Do not weaken warnings, validators,
package allowlists, or assertions to make a gate pass."

Also: only the repository's guarded scripts stage, deploy or launch; never
edit `codex-policy/`; never launch Kingmaker any other way.

## 5. Preconditions before ANY live run (all must hold)

Two conditions come from outside and have blocked most of this mission:

- **The owner's desktop session must be connected** (RDP `Active`) for two
  minutes before a run and for its whole duration. A disconnected session
  gives black presented frames and blocks physical input.
- **The owner must be hands-off with the game window in front** during each
  run (about 3–5 minutes each). Using another window makes Windows refuse the
  game's foreground activation; the harness then withholds cursor moves and
  the hover record fails (run `-02`).
- **The Kingmaker Gunslinger lab (another agent) shares the install.** It
  holds `C:\Dev\KingmakerGunslingerLab\compatibility-state\compatibility.lock`
  during its runs, with short gaps between steps. Wait for its explicit
  "ended/restored" message (it sends one after restoring the owner's install);
  never start in a lock gap. The owner gave Gunslinger priority.

Read-only reconciliation (PowerShell 7 or Windows PowerShell), run it and
compare with the expected values:

```powershell
$lab  = 'C:\Dev\KingmakerBuffPlannerLab'
$repo = Join-Path $lab 'repo\KingmakerBuffPlanner'
$mods = 'C:\Program Files (x86)\Steam\steamapps\common\Pathfinder Kingmaker\Mods'
"KMG lock present (expect False): " + (Test-Path 'C:\Dev\KingmakerGunslingerLab\compatibility-state\compatibility.lock')
"KBP locks/sentinels (expect 0): " + @(Get-ChildItem -LiteralPath (Join-Path $lab 'runtime-state') -File -ErrorAction SilentlyContinue | Where-Object { $_.Name -match 'lock|sentinel' }).Count
"Kingmaker processes (expect 0): " + @(Get-Process -Name Kingmaker -ErrorAction SilentlyContinue).Count
"Owner session (expect Active): " + ((qwinsta | Select-String howard | ForEach-Object { $_.Line.Trim() }) -join ' ')
"KMG DLL (expect C6CCCDAC914ED59F): " + (Get-FileHash (Join-Path $mods 'KingmakerGunslinger\KingmakerGunslinger.dll')).Hash.Substring(0,16)
"KBP DLL (expect 78407DD4C7240CE2): " + (Get-FileHash (Join-Path $mods 'KingmakerBuffPlanner\KingmakerBuffPlanner.dll')).Hash.Substring(0,16)
$tx = Get-ChildItem -LiteralPath (Join-Path $lab 'runtime-state\transactions') -Recurse -Filter transaction.json
"Transactions (expect all Restored): " + $tx.Count + " / " + @($tx | Where-Object { (Get-Content -LiteralPath $_.FullName -Raw | ConvertFrom-Json).status -eq 'Restored' }).Count
"Git status (expect empty): '" + ((git -C $repo status --porcelain) -join ';') + "'"
"Branch (expect codex/kingmaker-buff-planner-casting-graph): " + (git -C $repo branch --show-current)
"HEAD: " + (git -C $repo rev-parse HEAD)
"Package commit (must equal HEAD): " + ((Get-Content (Join-Path $repo 'artifacts\local-runtime\0.2.0-rc6\KingmakerBuffPlanner-0.2.0-rc6-local-runtime.zip.build-local.json') -Raw | ConvertFrom-Json).commit)
```

At handoff there were 240 transactions, all Restored. If anything differs,
stop and find out why before any run; an incomplete restoration takes
priority over everything.

Stability of the owner's session (bash): wait until it has been `Active`
for 120 s without a break:

```bash
stable=0; while true; do line=$(qwinsta 2>/dev/null | grep -i howard | head -1); if echo "$line" | grep -qi Active; then stable=$((stable+15)); else stable=0; fi; [ $stable -ge 120 ] && { echo "stable $(date)"; break; }; sleep 15; done
```

## 6. Build and gate (at the exact HEAD, before any live run)

Every commit changes HEAD; the launcher refuses a package whose commit is
not HEAD, and a dirty tree. So after ANY commit (documentation included):

```powershell
Set-Location 'C:\Dev\KingmakerBuffPlannerLab\repo\KingmakerBuffPlanner'
powershell.exe -NoProfile -ExecutionPolicy Bypass -File scripts\Build-Local.ps1
powershell.exe -NoProfile -ExecutionPolicy Bypass -File scripts\Test-SourceOnly.ps1
```

- Expected last lines: `Source validation: PASS=42 FAIL=0`, `Protocol
  tests: PASS=362 FAIL=0`, `Runtime harness filesystem tests: PASS=38
  FAIL=0`, `Package validation: PASS=4 FAIL=0`, `Deployment WhatIf purity:
  PASS=5 FAIL=0`, `Launcher -File WhatIf purity: PASS=12 FAIL=0`, `Fixture
  inventory evidence tests: PASS=3 FAIL=0`, `Restore-InstallLocal tests:
  PASS=16 FAIL=0`, `Guarded publisher gate tests: PASS=3 FAIL=0`,
  `Source-only suite: PASS=1 FAIL=0`.
- The gate takes 10–50 minutes (its comparison cases wait while the other
  lab is active). Do not edit or commit while it runs: its launcher WhatIf
  step requires a clean tree. It refuses while any Kingmaker process runs
  (`Pathfinder: Kingmaker is running`) — a correct refusal; rerun later.
- Quick unit tests only (after a code edit, before committing):

```bash
cd /c/Dev/KingmakerBuffPlannerLab/repo/KingmakerBuffPlanner
"/c/Program Files (x86)/Microsoft Visual Studio/2022/BuildTools/MSBuild/Current/Bin/MSBuild.exe" tests/KingmakerBuffPlanner.Tests/KingmakerBuffPlanner.Tests.csproj -t:Rebuild -p:Configuration=Release -m -nologo -v:minimal
KBP_TEST_GAME_PATH="C:\\Program Files (x86)\\Steam\\steamapps\\common\\Pathfinder Kingmaker" ./artifacts/tests/KingmakerBuffPlanner.Tests.exe | grep -E "^FAIL|Protocol tests"
```

  (Without `KBP_TEST_GAME_PATH` ~148 tests fail on a missing
  Newtonsoft.Json; that is the environment, not the code.)

## 7. The guarded runs (owner connected, hands off, game in front)

Tell the owner before each run that the game takes the screen and the
cursor moves by itself; tell them when they can use the PC again. Run each
from the repository root with Windows PowerShell (the launcher is
`ConfirmImpact='High'`, so `-Confirm:$false` is required). Use a fresh
`-RunId` each time (letters, digits, dot, underscore, hyphen).

```powershell
$launcher = 'C:\Dev\KingmakerBuffPlannerLab\repo\KingmakerBuffPlanner\scripts\Invoke-KingmakerRuntimeTest.ps1'
# 1. Standard scenario at the owner's display (1920x1200)
& $launcher -Scenario live-workspace-qual -CompatibilityProfileId full-user -RunId casting-graph-qual-1200-03 -TimeoutSeconds 1200 -Confirm:$false
# 2. The same in a 1920x1080 window
& $launcher -Scenario live-workspace-qual -CompatibilityProfileId full-user -DisplayMode windowed-1920x1080 -RunId casting-graph-qual-1080-01 -TimeoutSeconds 1200 -Confirm:$false
# 3. The Advanced copy (spell slots, spontaneous pools, rods, Powerful Change)
& $launcher -Scenario live-workspace-qual -FixtureFamily Advanced -CompatibilityProfileId advanced-gunslinger-0136 -RunId casting-graph-qual-adv-1200-01 -TimeoutSeconds 1200 -Confirm:$false
# 4. Save, reload in game, and the same plan and ownership afterwards
& $launcher -Scenario live-workspace-reload -CompatibilityProfileId full-user -RunId casting-graph-reload-01 -TimeoutSeconds 1200 -Confirm:$false
```

- A run takes about 3–5 minutes plus restoration. The launcher throws
  `Runtime scenario returned FAIL.` on a FAIL; read the evidence anyway.
- Run 3 is new for the graph: the launcher may refuse it on an Advanced
  precondition (binding, inspection). Record any refusal exactly; do not
  work around it.
- Evidence directory: `C:\Dev\KingmakerBuffPlannerLab\runtime-evidence\<RunId>\`.
- After every run check `run-completion.json`: `kingmakerExited`,
  `restorationVerified`, `protectedSavesCompared`, `protectedSavesClean`
  must all be `true`. If restoration is not verified, stop everything.

## 8. Reading a run

```powershell
$d = 'C:\Dev\KingmakerBuffPlannerLab\runtime-evidence\<RunId>'
$r = Get-Content "$d\runtime-result.json" -Raw | ConvertFrom-Json
"status=$($r.status) stage=$($r.stage)"
$r.assertions | Where-Object { $_.status -ne 'PASS' } | Format-List id, expected, observed
$h = Get-Content "$d\hover-ownership.json" -Raw | ConvertFrom-Json    # live-workspace-qual only
"ghostReproduced=$($h.ghostReproduced) ghostWithoutClick=$($h.ghostWithoutClick) rootCause=$($h.rootCause)"
$h.failures; $h.violations; $h.notes
$h.buttonStates | Format-Table state, control, unityState, interactable, selectedPalette, tintMatches, tint, screenRect, shown
$h.samples | Format-Table label, behaviour, physical, aimed, clicked, expectedOwner, @{n='highlighted';e={$_.highlighted -join '|'}}, selection, detail -Wrap
```

What the assertions mean (`workspace-*` in `runtime-result.json`):

- `workspace-interaction-sequence`: the graph was driven through its real
  controls (buff tile, caster, exact source row, target click → one casting;
  three castings from two casters; a refused click with no caster; line
  corridor and card focus; inspector retarget; Undo; Done; Save). Its
  evidence names the chosen buff (`interactionSource=`).
- `workspace-reopen-preserves-intent`: close and reopen keep the plan.
- `workspace-hover-ownership` (qual only), judged by
  `RuntimeTesting/HoverOwnershipRecord.cs`:
  - shipped behaviour: every reading has at most one drawn highlight, on the
    control Unity's raycast finds under the real cursor, gone on exit; no
    planner control holds the EventSystem selection;
  - real-cursor sweep: at least 5 aims plus a neutral point;
  - the owner's report reproduced with the rc6 behaviour
    (`PlannerUiReproduction`, set only by the host): on the Classic screen,
    a buff card (`Source.<id>`) clicked, then a portrait hovered with the
    real cursor → two highlights (`ghostReproduced=True`); never a second
    highlight without a click (`ghostWithoutClick=False`);
  - five button states drawn (tint matches the colour block): normal
    (Reload), hover (Save), pressed (ExecutionMode), selected (the routine
    tab), disabled (FocusedOrder.Earlier);
  - one planner root after the production close and reopen.
- `rootCause` names the sticky-selection cause only when the rc6 ghost is
  reproduced with the real cursor after a click, never without one, and the
  shipped behaviour is clean. Until then the cause is a theory.

Failure patterns and what to do:

| Symptom | Meaning | Action |
| --- | --- | --- |
| `physical-delivery-failed:...foreground activation failed` | Windows refused the game focus (someone used another window, or the session is not interactive) | Environment, not product. Ask the owner to be hands-off with the game in front; rerun with a new RunId |
| Presented frames black / `workspace-frame-nonblack` FAIL | The owner's session was disconnected | Rerun only with a stable connected session |
| `rc6-ghost-not-reproduced` while `hover-classic-rc6-click-then-hover` WAS delivered and the notes say `classic-rc6-click=...;tookSelection=True` | The card took the selection but no second drawn highlight appeared | The theory is refuted as the visible cause. Look at `hover-classic-rc6.png`, each sample's `ghosts=` geometry (name@dx,dy from the cursor; the owner saw "up and left"), and investigate other causes (duplicate overlays or listeners, native plus custom transitions, canvas camera, stale controls — mission §7). Report it; do not weaken the predicate |
| `rc6-ghost-without-click` | A second highlight with no click before it | Another cause exists; investigate as above |
| `more-than-one-hover-owner` / `hover-owner-misaligned` / `highlight-without-pointer` / `aimed-control-not-topmost` / `highlight-not-under-cursor` on shipped readings | A real product defect | Reproduce, fix at the cause, add a regression test |
| `button-state-not-shown:...` | A state is not drawn as claimed | Check the named control's colours; fix the product, not the test |
| `classic-card-missing` / `classic-control-missing` | The Classic screen showed no card or portrait | Look at `hover-classic-*.png`; harness or fixture issue |

Frames. Judge visuals only from presented-frame captures (what the owner
sees): `workspace-frame.png`, `workspace-control-frame.png`,
`workspace-closed-frame.png`, `hover-graph-fixed.png`, `button-states.png`,
`hover-classic-rc6.png`, `hover-classic-fixed.png`. The camera-path frames
(`ws-interact-*.png`, `workspace-camera-*.png`, `ws-reload-reopened.png`)
render planner text washed out and do not include the Classic screen at
all (it is a screen-space overlay): use them for layout only.

The mission's required evidence (§10) maps to: 1920x1200 and 1920x1080
graph with two casters and several connections (`hover-graph-fixed.png` of
runs 1 and 2); one selected connection with the inspector open
(`button-states.png`: casting #1 focused); a global budget change after a
spontaneous cast (run 3: compare `workspace-frame.png` before authoring
with `hover-graph-fixed.png` after; the Automation party has only cantrips
and skill actions, so runs 1–2 cannot show it); the five button states
(`button-states.png`); the pointer sweep (`hover-ownership.json`); close
and reopen (qual) and save/reload (run 4).

## 9. Caption contrast from the presented frame

Save as a script and run with Windows PowerShell (System.Drawing):

```powershell
param([Parameter(Mandatory = $true)][string]$EvidenceDirectory, [string]$Frame = 'button-states.png', [double]$GlyphFraction = 0.03)
Add-Type -AssemblyName System.Drawing
function Get-Channel([double]$c) { $c = $c / 255.0; if ($c -le 0.03928) { $c / 12.92 } else { [Math]::Pow(($c + 0.055) / 1.055, 2.4) } }
function Get-Luminance($k) { 0.2126 * (Get-Channel $k.R) + 0.7152 * (Get-Channel $k.G) + 0.0722 * (Get-Channel $k.B) }
$record = Get-Content (Join-Path $EvidenceDirectory 'hover-ownership.json') -Raw | ConvertFrom-Json
$bitmap = [System.Drawing.Bitmap]::FromFile((Join-Path $EvidenceDirectory $Frame))
try {
  foreach ($button in @($record.buttonStates)) {
    $p = ([string]$button.screenRect).Split(','); if ($p.Count -ne 4) { continue }
    $x = [int][double]$p[0]; $y = [int][double]$p[1]; $w = [int][double]$p[2]; $h = [int][double]$p[3]
    $l = [Math]::Max(0, $x + 3); $r = [Math]::Min($bitmap.Width - 1, $x + $w - 4)
    $t = [Math]::Max(0, $bitmap.Height - ($y + $h) + 3); $b = [Math]::Min($bitmap.Height - 1, $bitmap.Height - $y - 4)
    $s = New-Object System.Collections.Generic.List[object]
    for ($py = $t; $py -le $b; $py++) { for ($px = $l; $px -le $r; $px++) { $c = $bitmap.GetPixel($px, $py); $s.Add([pscustomobject]@{ L = (Get-Luminance $c) }) } }
    $sorted = @($s | Sort-Object L); $stone = $sorted[[int]($sorted.Count / 2)].L
    $n = [Math]::Max(1, [int]($sorted.Count * $GlyphFraction)); $glyph = ($sorted[($sorted.Count - $n)..($sorted.Count - 1)] | Measure-Object -Property L -Average).Average
    $ratio = ([Math]::Max($glyph, $stone) + 0.05) / ([Math]::Min($glyph, $stone) + 0.05)
    "{0,-9} {1,-24} ratio={2:N2} {3}" -f $button.state, $button.control, $ratio, $(if ($ratio -ge 4.5) { 'PASS>=4.5' } else { 'FAIL<4.5' })
  }
} finally { $bitmap.Dispose() }
```

Rects are Unity screen pixels (origin bottom left), recorded by the probe.
The glyph cores are the brightest 3% of the button's pixels
(anti-aliasing makes this conservative). Mission §8: normal text ≥ 4.5:1,
large text ≥ 3:1; disabled visibly disabled but legible.

## 10. The owner's supervised review (after runs 1–4 pass)

Follow the top section of `docs/MANUAL-USABILITY-HANDOFF.md`:

```powershell
& 'C:\Dev\KingmakerBuffPlannerLab\repo\KingmakerBuffPlanner\scripts\Invoke-KingmakerRuntimeTest.ps1' -Scenario live-workspace-manual -CompatibilityProfileId full-user -RunId casting-graph-manual-HHmm -ManualHoldSeconds 1200 -TimeoutSeconds 1620 -Confirm:$false
```

- The harness makes no synthetic input in this scenario. Wait for
  `manual-ready.json` in the run's evidence directory and the
  `MANUAL-READY acknowledged` launcher line, then tell the owner the
  workspace is open (native casting is disabled; nothing can cast).
- When the owner says "done", write `manual-done.json` in that run's
  evidence directory; for "stop", write `manual-stop.json` (stop wins). For
  example: `{"stage":"manual-done","by":"owner","runId":"<RunId>","atUtc":"<ISO time>"}`.
  Never use the word "rehearsal" in it.
- Record the owner's verdict **verbatim** in the status file. A deadline or
  an automated PASS is never acceptance.

## 11. Records and publication

- After each step update `planning/CASTING-GRAPH-UI-CORRECTION-STATUS.md`
  (G-ledger rows G10–G15, a new checkpoint with branch, HEAD, package
  SHA-256, DLL SHA-256, MVID, commands, exact counts, run IDs, evidence
  paths, rejected theories, uncertainty, next action),
  `KINGMAKER-BUFF-PLANNER-JOURNAL.md` (newest entry on top) and
  `AUTONOMOUS-RESUME.md` (LATEST entry); `AUTONOMOUS-BLOCKERS.md` for
  anything blocking.
- Push only with the guarded helper, on the `codex/` branch, clean tree:

```powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass -Command "& 'C:\Dev\KingmakerBuffPlannerLab\codex-policy\Push-KingmakerBuffPlanner.ps1' -WhatIf"
powershell.exe -NoProfile -ExecutionPolicy Bypass -Command "& 'C:\Dev\KingmakerBuffPlannerLab\codex-policy\Push-KingmakerBuffPlanner.ps1' -Confirm:`$false"
```

- Update PR #3's description with results (`gh pr edit 3 --body-file ...`);
  keep it a draft. Do not close or merge PR #2 or PR #3.

## 12. Final report (mission §14)

Lead with exactly one of: `IDENTITY PROVED — CLASSIC/OLD UI; GRAPH
CORRECTION IMPLEMENTED`, `IDENTITY PROVED — CASTING-FIRST UI; GRAPH
CORRECTION IMPLEMENTED`, `IDENTITY PROVED — CORRECTION PARTIAL, BLOCKED`,
`NO SAFE RUNTIME ACCESS — SOURCE WORK COMPLETE, LIVE CHECK BLOCKED`,
`OWNER ACCEPTED GRAPH WORKSPACE`, `OWNER REJECTED GRAPH WORKSPACE`. Then the
eight sections of §14. The identity section is already proved (status file
Phase 0): the rejected screen was the Classic view; DATA runs 0.1.1-rc3
(DLL `78407dd4…`, MVID `c0cc9cbc-…`); the owner most likely ran the
published rc6 alpha in its default Classic mode on their own machine (not
provable from DATA).

## 13. Open questions and known cosmetic points

- **Hover root cause** is a theory until run 1 (or 2) shows
  `ghostReproduced=True` with the real cursor: a click selects a planner
  control whose navigation is not None, and the installed Unity UI's
  `Selectable.IsHighlighted` keeps the selected control lit while another
  is hovered. The Classic grid (buff cards) sits above the portrait panel,
  consistent with the owner's "offset up and left", but that geometry is
  not proved yet.
- **Rejected theories so far:** a second camera rendering the planner
  canvas; the Classic tooltip (it is footer text); stale rebuilt controls
  (the probe reads active controls only); "the graph's text is pale" (true
  only of the camera-path frames).
- The casting cards sit on the book's centre fold at 1920 wide; lines cross
  the fold. Cosmetic; let the owner judge.
- `CastingGraphLayout` is pure and tested (`graph-layout-chips-never-overlap`);
  layout changes need test updates.

## 14. Environment facts

- Repository: `C:\Dev\KingmakerBuffPlannerLab\repo\KingmakerBuffPlanner`;
  lab root `C:\Dev\KingmakerBuffPlannerLab` (runtime-state, runtime-staging,
  runtime-backups, runtime-evidence); game
  `C:\Program Files (x86)\Steam\steamapps\common\Pathfinder Kingmaker`
  (`GamePath.props`).
- MSBuild: `C:\Program Files (x86)\Microsoft Visual Studio\2022\BuildTools\MSBuild\Current\Bin\MSBuild.exe`;
  .NET Framework 4.7, C# 7.3, `TreatWarningsAsErrors`; the test project
  links product sources explicitly (add new Unity-free files there too).
- Line endings: the repository stores LF; Git warns "LF will be replaced by
  CRLF" on Windows. That is expected.
- The owner's session is an RDP session (`qwinsta` user `howard`, id 3).
