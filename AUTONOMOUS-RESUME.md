# AUTONOMOUS-RESUME — historical session log

**Not current.** The single current status of the casting-first migration
is the top section of `planning/CASTING-FIRST-MIGRATION-STATUS.md`. The
entries below record past sessions, including temporary orchestration
notes (watcher and preflight state) that are not product status.

## Casting-graph UI correction: checkpoint 2, first live run (2026-09-25, LATEST)

- Tracker: `planning/CASTING-GRAPH-UI-CORRECTION-STATUS.md` (checkpoint 2).
  HEAD after the documentation commit; the package is rebuilt at the exact
  HEAD and the gate rerun before any live run.
- Live `casting-graph-qual-1200-01` (1920x1200): FAIL only at the hover
  record (Classic card lookup, repaired in `590ca28`); everything else
  PASS; safety clean.
- Waiting for a stable owner session (two minutes Active before a frame
  run). Then: `-02` at 1920x1200, the 1920x1080 run, reload, then the
  supervised manual session and the owner's verdict.
- Not pushed: branch publication is the owner's decision (AUTONOMOUS-BLOCKERS).

## Casting-graph UI correction: source complete to checkpoint 1 (2026-09-25)

- Tracker: `planning/CASTING-GRAPH-UI-CORRECTION-STATUS.md` (G-ledger and
  checkpoint 1). Branch `claude/casting-graph-ui-correction`, not pushed
  yet; the draft stacked PR is opened through the guarded push helper.
- Source gate at `757fc6b`: validation 42/42, protocol 360/360. The
  harness/WhatIf tests refused while the Gunslinger lab's Kingmaker ran
  (correct); rerun before any live run.
- Before a live run: the full read-only reconciliation (no KMG or KBP lock
  or sentinel, no Kingmaker process, KMG transactions Restored, Mods equal
  to the owner's baseline, owner's session connected for frame runs), then
  `Build-Local.ps1` at the exact HEAD, then `Test-SourceOnly.ps1`.
- Live plan: `live-workspace-qual` (default display, then `-DisplayMode
  windowed-1920x1080`), `live-workspace-reload`; judge frames and
  `hover-ownership.json`; then prepare the supervised manual session and
  stop for the owner's verdict.

## Casting-graph UI correction started (2026-09-25)

- The owner rejected the observed planner screen (a Classic view, proved in
  `planning/CASTING-GRAPH-UI-CORRECTION-STATUS.md`) and adopted the
  casting-graph addendum. Work continues on branch
  `claude/casting-graph-ui-correction` (from `e8496ee`); its status file is
  the tracker for this mission.
- After the machine reboot: every worktree clean, 238/238 KBP transactions
  Restored, deploys RolledBack, no lock or violation, no Kingmaker process.
  The rc6 frame watcher below is superseded and was not re-armed.
- The Gunslinger lab is active and has priority for the game; check its
  lock/lease and running Kingmaker before any KBP runtime transaction.

## Release candidate 0.2.0-rc6 frozen at `24d9967`; chain done (2026-09-25)

Candidate `24d9967f82e516f9ed4a4c1f06e6a842e18c5f96`: package
`f271f3e6...`, DLL `35d6cbb2...`, MVID `4cb12c0d-78c6-4755-aa62-b6ad50e1a02b`;
the same bytes from `repo/KingmakerBuffPlanner-RC6` (two deterministic
builds), `RC6-repro` and the G2 gate build. Frozen copy
`runtime-backups/rc-frozen/24d9967.../`. Receipt
`docs/evidence/rc-0.2.0-rc6-receipt.md`.

- Gate at the candidate: source 42, protocol 338, harness 38, package 4,
  deploy WhatIf 5, launcher WhatIf 12, fixture 3, Restore-InstallLocal 16,
  publisher 3.
- Chain (`scratchpad/rc6_chain.ps1`, status `rc6_chain.status`): Classic
  select and cast in both modes, zero-cost select and both modes, import:
  PASS. `casting-ws-qual-20260924-rc6-1080-01` FAIL on black frames (the
  session connected for a moment at 22:43 and disconnected; restored,
  saves clean; not a product result); reload, 1200 layout and rehearsal
  deferred; the chain now needs two minutes of stable connection before a
  frame run. Resumed with `-Only` for the rest: advanced inspection and
  finite, group, enhanced, ability-pool and rod, each a selection and both
  modes: PASS. Temporary install and rollback: PASS (0.1.1-rc3 restored
  exactly, 1117 entries).
- Waiting on the owner: the connected session for the frame-judged runs
  and the supervised manual session; the decisions on Classic and
  toggles left on, and on worn-item enchantments; merge, release and
  permanent installation.
- Armed 2026-09-25 00:10 for at most 12 hours: `scratchpad/rc6_frames.ps1`
  waits for the owner's session to stay connected for two minutes, then
  runs the deferred frame runs once (reload, layout 1200, layout 1080 as
  attempt `-02`, the labelled rehearsal) through `rc6_chain.ps1 -Only`.
  Status lines `FRAMES ...` in `rc6_chain.status`. Stopped at 00:11,
  before it ran anything, because the Gunslinger lab began a 2.5-3 hour
  batch with short gaps in its lock; to be re-armed after its "KMG
  finished" (launch: `powershell.exe -File scratchpad
c6_frames.ps1`).
- 00:43 re-arm preflight (the owner's rule: a full read-only
  reconciliation, never an absent lock alone): FAILED, watcher stays
  paused. A Gunslinger runtime run was in progress after "KMG finished"
  (Kingmaker.exe started 00:42:47 with a KMG runtime-test request; the KMG
  compatibility.lock held; KMG deployments at 00:11, 00:26, 00:41), and
  Mods holds KingmakerGunslinger 0.0.139 (DLL `3c07668a...`) instead of
  the owner's 0.0.136 (DLL `c6cccdac...`). KBP side clean (237
  transactions Restored and verified, every temporary deploy RolledBack,
  protected-save comparisons clean, no KBP lock); RC6 at `24d9967` with
  package `f271f3e6...`, DLL `35d6cbb2...`, MVID `4cb12c0d-...` unchanged;
  the owner's session was Active. Re-arm only after a new reconciliation
  passes.
- 01:53 second preflight (after the Gunslinger session's "KMG finished"
  at 01:52): the lab state passes. No KMG or KBP lock or sentinel, no
  Kingmaker process, the three KMG deployment transactions Restored and
  verified and its runtime leases Completed, the Mods content equal to
  the owner's baseline (KingmakerGunslinger 0.0.136 again; only
  write-time differences in 13 settings files the game rewrote on exit),
  KBP clean, RC6 unchanged. The watcher still stays paused. The owner's
  session was disconnected, with LogonUI in session 2, and Gunslinger
  processes were still active (its DomainTests.exe from its own worktree
  at 01:53:29). Next: when the owner connects and asks, repeat the
  reconciliation; the frame runs start only if every condition passes.
- Found after the freeze (in the receipt): the inventory prints an
  unmodeled action's type as `0` (diagnostic text only; fixed in
  `b4eed30`, pushed to this branch at the owner's request for review;
  source tests passed when it was made, not rebuilt, frozen or run in the
  game; first committed locally as `caea6ea`); multi-use
  ability pools cannot be qualified on the advanced campaign (the only
  plain one lasts a round).

## The next iteration toward rc6 (2026-09-24)

The next iteration (local branch `codex/kingmaker-buff-planner-next`,
`a0e0e5a..64c6e22`) was merged into the PR branch (`a036627`) and
versioned 0.2.0-rc6 (`24d9967`). Development evidence:
`docs/evidence/next-iteration-20260924-receipt.md`.

- Mutagen (`ability-pool-direct`): PASS in both modes on `e2a4959` and on
  the fixed `fb762e2` (use 1 > 0 with the buff landed, repeat casts
  nothing, Always recast refused for the empty pool).
- Extend rod (`rod-extend-direct`): on `e2a4959` the Animated run FAILED
  at its plain step, a real defect: the transmuter's second rod copy was
  switched on in the seed and the plain casting, which chose no rod,
  spent a charge (6 > 5) and was extended (1079.6 s for Blur's 540 s).
  Both executors skipped enhancement preparation for steps without
  enhancements. Fixed in `fb762e2` (casting-first steps apply exactly
  their chosen enhancements; an unchosen rod still running is stopped
  because the game keeps a switched-off toggle running until the next
  round, read from its IL), hardened in `64c6e22` after a second review.
  On `fb762e2` both modes PASS: plain 540 s with no charge, rod 1080 s
  for one charge.
- Two independent reviews, all findings fixed or documented; mutants of
  the new guards all killed. Area transition: route check only, both
  seed exits autosave (unavailable).
- Owner decision added: the Classic planner still lets a rod left
  switched on apply to casts that choose no enhancement (unchanged from
  0.0.19); recommendation: the casting-first rule.
- Hardened build `64c6e22` (gate PASS, package `270199dc...`): rod and
  Powerful Change qualifications PASS in both modes (the running rod copy
  used, no rod left running, plain casts no longer extended by the rod
  left on).
- rc6 then followed: freeze, gate, chain and receipt (the section above).

## Release candidate 0.2.0-rc5 frozen at `27234a4`; chain done (2026-09-24)

Candidate `27234a445e95f2fc399857a880c35de0f397359f`: package
`a05f1a83...`, DLL `785e1b79...`, MVID `171a1599-fef3-419b-b748-2833a10f9d75`;
the same bytes from `repo/KingmakerBuffPlanner-RC5` (two deterministic
builds), `RC5-repro` and the G2 gate build. Frozen copy
`runtime-backups/rc-frozen/27234a4.../`. Receipt
`docs/evidence/rc-0.2.0-rc5-receipt.md`; advanced fixture receipt
`docs/evidence/advanced-fixture-20260924-receipt.md`.

- Gate at the candidate: source 42, protocol 335, harness 38, package 4,
  deploy WhatIf 5, launcher WhatIf 12, fixture 3, Restore-InstallLocal 16,
  publisher 3.
- Chain (`scratchpad/rc5_chain.ps1`, status `rc5_chain.status`): Classic
  select/cast in both modes, zero-cost select and both modes, import:
  PASS. `casting-ws-reload-20260924-rc5-01` FAIL on black frames (the
  session showed as connected at the check and disconnected before the
  frames; restored, saves clean; not a product result); layout and
  rehearsal deferred. Advanced: inspection, finite, group and enhanced,
  each selection plus both modes: PASS. Temporary install and rollback:
  PASS (0.1.1-rc3 restored exactly, 1117 entries).
- Waiting on the owner: the connected session for the frame-judged runs
  and the supervised manual session; the worn-item enchantment decision;
  merge, release and permanent installation.
- Next iteration, started on the LOCAL branch `codex/kingmaker-buff-planner-next`
  (worktree `repo/KingmakerBuffPlanner-N1`, not pushed, so the PR branch stays
  receipts-only after the candidate): `a0e0e5a`/`6af9242` the
  `ability-pool-direct` recipe (the Mutagen: use, repeat, exhausted) and
  the plain-buff shape for empty actions and same-branch conditions;
  `75c8133`/`e2a4959` the `rod-extend-direct` recipe (an Extend rod chosen
  on a casting: duration doubled at the same strength for one charge; game
  clock on every read; rod charges read). 337 tests; mutants killed except
  two redundant ability-pool checks. Live runs wait for a quiet window.
- Next iteration (bounded, in the receipt): the Mutagen ability-pool
  qualification (the plain-buff check must first accept its shape), an
  Extend rod casting, a safe area-transition route check.

## Advanced seed and the path to rc5 (2026-09-24)

The owner created `KBP_ADVANCED_SEED` (a disposable *Beneath the Stolen
Lands* save: cleric, brown-fur transmuter (Arcanist), alchemist with
Transfusion, fighter; level 9; Gunslinger 0.0.136) and asked for the
advanced qualification plus two rc4 source fixes and a mixed-coverage
group case. Evidence from this fixture is labelled as a Beneath the Stolen
Lands fixture, never as proof of main-campaign-specific behaviour.

Done so far (O1 branch, not yet pushed as of this note):
- `a134f0a` profile `advanced-gunslinger-0136` (the six approved 0.0.136
  values, staged from the exact copy `examples\KingmakerGunslinger-0.0.136`
  taken from the Gunslinger lab's 08:31 pre-deploy snapshot); BagOfTricks
  resealed in both profiles as a frozen copy (`examples\BagOfTricks-20260924`,
  cheats verified off; it rewrites Settings.xml at every game exit); the
  launcher and host refuse any other pairing of profile and fixture.
- `cb684f2` rc4 source review: reused archives verified byte-exact;
  unreadable suppression or caster level never proves sufficiency.
- `cbdc2fd` + `0c81925` mixed-coverage group castings (pre-covered
  recipients keep their coverage) and the corrected skip-if-active claim.
- `7de4726`, `b3621bf` inspection records the capability inventory with
  effect leaves; `2e565bd` variant spells are plain buffs of themselves
  (the finite selection had refused every variant) and the `group-mixed`
  qualification recipe.
- `0c03930` the group recipe ends after its mixed step, and a pre-covered
  recipient whose effect outlasts the cast gets its own note ("... may
  shorten it"): in `casting-qual-cast-20260924-adv-group-anim-01` the
  game replaced the fighter's longer direct instance with the communal
  one, so the old repeat step found the direct casting due again.
- `9d530bc` + `fefd5ec` the `enhanced-direct` recipe (section 8's non-rod
  per-casting enhancement): Brown-Fur Powerful Change chosen on the
  casting through the workspace's own option; native reads of the
  Arcane Reservoir, every activatable ability of the caster, and the stat
  modifiers each effect instance gives.
- `163a1bb` casting-first routes an enhanced casting as the classic
  planner does (found reading the provider's design notes: a plain rule
  cast never enrols Powerful Change, so Instant castings would have
  landed unenhanced yet confirmed); `3868c39` the enhanced recipe binds
  the route that ran; `f66246c` an independent review's findings: Share
  chosen as an enhancement blocks, archive names never run out, notes
  name characters; three documented limitations (worn-item enchantments
  recast, same-routine coverage, the Instant fallback route only logged).
  Mutants for all of it killed except equivalent or unreachable ones.
- Live (advanced copy, from checkout `repo/KingmakerBuffPlanner-A1`,
  builds frozen under `runtime-backups/qualification-frozen/`):
  `advanced-inspect-20260924-01` PASS (non-casting; campaign
  cb1f405d..., Tenebrous Depths I, party level 9, no pets, no unreadable
  buff detail); `casting-qual-select-20260924-adv-finite-01` refused
  (the variant-shape defect, fixed in `2e565bd`; nothing cast); finite:
  selection `-adv-finite-02`, `-adv-finite-anim-02` and
  `-adv-finite-inst-01` PASS (`-adv-finite-anim-01`'s game result was
  PASS but its restoration was refused: the harness incident in the
  review index; restored by the guarded `Restore-Local.ps1`); group:
  selection `-adv-group-01` PASS, `-adv-group-anim-01` FAIL at the
  repeat step (above); selection `-adv-group-02`, `-adv-group-anim-02`
  and `-adv-group-inst-01` PASS on `0c03930`.

Coordination: the Gunslinger development session shares this Kingmaker
installation. Each side takes its own lock (ours
`runtime-state\deployment.lock`, theirs `compatibility.lock`), waits for
quiet, and messages before and after a batch; our runs stage frozen copies
of Gunslinger and BagOfTricks and restore its installed state.

Next: the enhanced selection and casting runs on `f66246c` (both modes;
C# mutation of the new rules in `repo/KingmakerBuffPlanner-M`), a full
gate while neither lab runs the game, push, then rc5: freeze, chain
(Classic regression, zero-cost, import, advanced inspect, finite, group,
enhanced), install/rollback, the advanced fixture receipt (labelled a
Beneath the Stolen Lands fixture), handoff.

## Release candidate 0.2.0-rc4 frozen at `863a182` (2026-09-24)

rc3 (`ea2a027`) was superseded after its final review; rc4 fixes those
findings and four further independent review rounds of the fixes
(re-review, focused, targeted, last), listed with commits in
`docs/CASTING-FIRST-REVIEW-INDEX.md`. Mutation after the final review of
rc3: 52 C# and 41 PowerShell mutants, each killed by its check (one
equivalent removed, three survivors answered with tests).

Candidate `863a182047950a75d7aafc548aa7b52a54c71b99`: package `9093cfd0...`,
DLL `d7081c0d...`, MVID `61b9aaa5-786d-4e18-b929-a6a6caa5b443`; the same
bytes from RC4 (two deterministic builds), RC4-repro and G2. Frozen copy
`runtime-backups/rc-frozen/863a182.../`; clean checkout
`repo/KingmakerBuffPlanner-RC4`; receipt
`docs/evidence/rc-0.2.0-rc4-receipt.md`. Gate at the candidate: source 42,
protocol 331, harness 37, package 4, deploy 5, launcher 12, fixture 3,
restore 16, publisher 3.

On the frozen build (all restored, saves compared clean under the lock):
Classic select and cast in both modes PASS (Resistance through the at-will
cantrip ability, confirmed by a new instance, pools unchanged, grant once);
casting-first qualification select and both modes PASS (stop, complete,
repeat, recast, disable, recover, 8 of 8 submissions); first-open import of
a genuine schema-4 file PASS; the finite selection refused as expected
(`no-eligible-qualification-recipe`). Temporary install and rollback of the
exact package PASS; the owner's 0.1.1-rc3 (DLL `78407dd4...`) restored.

Deferred (owner's session disconnected since 23:34 on 09-23, black frames):
in-game reload, layout at 1920x1200 and 1920x1080, manual rehearsal,
physical input - commands in the receipt, run them once the session is
connected. Owner inputs: the `KBP_ADVANCED_SEED` (finite, group, metamagic,
pets), the supervised manual session (`docs/MANUAL-USABILITY-HANDOFF.md`,
RC4 checkout; includes confirming that Classic APPLY now closes the
planner), and the final review. Merge, release, tag and permanent install
stay with the owner; PR #2 stays a draft.

## Batch 3 mission (2026-09-23): Classic regression, finite and group qualification, lifecycle

Reconciled at start: PR #2 head `b042e36` (draft); candidate rc2 `ae0181d`
(package `35382731...`, DLL `5ec74d2f...`, MVID `5d780e27-...`); installed
planner 0.1.1-rc3 (DLL `78407dd4...`) and Gunslinger 0.0.136 (DLL
`c6cccdac...`); no locks, no unfinished transaction (131 restored), game
not running; the owner's RDP session is active. External dependency: no
`KBP_ADVANCED_SEED` save exists (asked once; not re-asked). The
automation party has finite spontaneous slots (bard 2, sorcerer 5 at
level 1; diagnostics `casting-qual-select-20260923-d1-01`).

Worklist (updated as slices land; 2026-09-24 status in brackets):
1. Capability inventory of the automation party (read-only, live).
   [DONE `ca05fe3`, run `casting-qual-select-20260923-inv-01`: only free
   cantrips, Heal skill and Aid Another; finite spontaneous pools exist but
   hold no buff.]
2. Classic regression: resolver negatives in source; a bounded Classic
   cantrip cast through Classic's own routes, animated and instant.
   [DONE. Scenarios `live-classic-select`/`live-classic-cast` (`cca152f`,
   single-use grant, 45 mutants killed). Instant PASS
   `classic-cast-20260924-00aca73-inst-01`; animated PASS
   `classic-cast-20260924-da0ee32-anim-02` after the mode-aware judgement
   fix `d6d5e33`. Receipt `docs/evidence/classic-cast-20260924-receipt.md`.]
3. Finite spontaneous qualification on the automation party, both modes.
   [UNAVAILABLE on this fixture: its finite pools hold no buff. Needs the
   advanced seed.]
4. Group behavior with whatever the party really has; otherwise it waits
   for the seed. [Waits for the seed: the party has no group buff.]
5. Lifecycle: stop while pending, disable/unload, target loss, refusal,
   re-enable, a new accepted run; an area transition only on a safe route.
   [DONE for this fixture. Recover step `b63da88` (8 mutants killed): PASS
   animated `casting-qual-cast-20260924-13f6d37-anim-01` (disable in
   flight) and instant `…-inst-01` (disable before start, labeled so),
   lifecycle probe identical, 5 runs started and 5 reported. Area
   transition UNAVAILABLE: the only route (`MapExit` to the prologue's
   end) autosaves BeforeExit with the game's autosave on. Receipt
   `docs/evidence/casting-qual-recover-20260924-receipt.md`. Target or
   provider loss and native refusal stay unit-tested only: producing them
   live needs party or game-state manipulation, which is not authorized.]
6. UI and physical input, 1920x1080 and other resolutions (session active).
   [Scenario `live-workspace-physical` and `-DisplayMode` (`f945afc`): search
   typing, wheel, a press target, the game window's own focus loss and
   Escape through OS input; 1920x1080 as a borderless window with the game
   registry restored byte-exact. 2560x1440 is larger than this RDP session
   (1920x1200 physical, 125% scaling) and is refused without changing any
   display setting (`phys_2560_refusal`: "unsupported on this session's
   display (1920x1200); nothing was changed").
   PHYSICAL INPUT NEEDS A CONNECTED SESSION: the owner's session 2 is
   Disconnected (`query session`), so no window can take the foreground.
   `ws-physical-20260924-81b359f-owner-01` failed closed at its first
   delivery ("Kingmaker foreground activation failed"; 82/83 assertions
   passed, the workspace opened through the labeled programmatic fallback).
   Not repeated; no session or RDP setting was touched. With the session
   connected, run from a clean built worktree:
   `Invoke-KingmakerRuntimeTest.ps1 -Scenario live-workspace-physical
   -CompatibilityProfileId full-user -TimeoutSeconds 900 [-DisplayMode
   windowed-1920x1080] -Confirm:$false`. The layout at 1920x1080 without
   input runs as `live-workspace-qual -DisplayMode windowed-1920x1080`.]
7. Persistence and import gaps; install and rollback of the final artifact.
   [Persistence DONE `f524acf` from an independent audit: the review and
   planner-mode stores no longer overwrite newer or unreadable files, the
   plan repository no longer overwrites another campaign's file; new round
   trips (two spellbooks, variant, metamagic, Always recast), same-directory
   campaigns, and the Classic profile's bytes after workspace use (5 mutants
   killed). Install and rollback wait for rc3.]
8. Build reproducibility across checkouts (bounded, secondary).
   [DONE `30c8483`: the only path-dependent bytes of two builds of `67fa087`
   (G and G2) were the embedded PDB path; with PathMap, clean builds of
   `30c8483` in G and G2 gave the identical DLL `a502e5c5…` and the
   identical package `8a835fa6…`.]
9. Independent reviews, rc3, full gate, native qualification on rc3,
   receipt, PR handoff.
   [Review round (A source/budgets/execution, B UI/lifecycle/persistence,
   C harness) findings fixed, 2026-09-24:
   - `6cc75d9` Classic safety: halting runner, owned terminal, grant disarm.
   - `275fa59` harness: registry snapshot, lease-aware restore, entry gaps.
   - `ce3311a` persistence: invalid primaries kept, refusals shown.
   - `b3ab18d` physical scenario: physical open, focus, real selection change.
   - `b3723c6` source resolution (A2 reservation-driven cantrip route, A3
     pool-bound facts, A4 ambiguous cantrips unresolved, A7 unread counts
     uncertain, A10 cheap filter).
   - `b03e16f` held disable through the mod toggle path, real lifecycle
     probe (B7); instant disable labeled disable-before-start (B3).
   - `ec52531` allowance binding: profile, identity, WORKING save, purpose
     (Classic schema 2, qualification schema 5) (C5).
   - `953c511` launcher reads Classic and physical evidence itself (C6).
   Mutants: 32 C# (one survivor, fixed by `09b349d`) and 12 PowerShell,
   all killed. Full gates PASS at `ec52531` and `953c511` (protocol 311,
   harness 36, launcher 12, deploy 5, restore 16). A focused re-review of
   these fixes is running before rc3. Allowances are now written by the
   in-repo `scripts/New-KbpRunAllowance.ps1` (Classic schema 2,
   qualification schema 5), bound from the frozen build record and the
   selection run's evidence (profile, identity and WORKING save from its
   `run-completion.json`), exclusive creation; its output is tested
   against the launcher's own checks.]

Shared installation (2026-09-24): the owner's Gunslinger lab runs the same
game. Its runs are serialized with ours by its lease file and our
deployment lock, which is now double-checked after taking it (`16cd178`).
A launch that finds its game running is refused before deployment (two such
refusals today, recorded in the Classic receipt). Gates also need the game
closed (the rollback tests check for a running game). The installed
Gunslinger is now **0.0.139** (manifest `ee7af9cc…`, DLL `a53e53c9…`), not
the 0.0.136 identity named for the conditional advanced profile. That
profile is therefore NOT created. Every other full-user entry and the
external 0.0.133 copy match exactly.

## Release candidate 0.2.0-rc2 frozen at `ae0181d`

Receipt: `docs/evidence/rc-0.2.0-rc2-receipt.md`. Clean checkout
`repo/KingmakerBuffPlanner-RC2`; frozen copy
`runtime-backups/rc-frozen/ae0181d.../` (package `35382731...`, DLL
`5ec74d2f...`, MVID `5d780e27-...`). On the frozen build, all PASS and
complete, restored, saves clean:
- `casting-qual-select-20260923-rc2-01`;
- `casting-qual-cast-20260923-rc2-anim-01` and `-rc2-inst-01` (every
  step as forecast, zero violations);
- `casting-ws-import-20260923-rc2-01`, `casting-ws-reload-20260923-rc2-01`;
- `casting-ws-manual-20260923-rc2-rehearsal`.

Temporary install and rollback `rc2-temp-deploy-20260923-01` restored the
owner's 0.1.1-rc3 exactly (Mods manifest equal, 1117 entries). Gate at
`ae0181d`: source 42, protocol 300, harness 35, package 4, deploy 5,
launcher 12, fixture 3, restore 16, publisher 3.

Owner inputs (unchanged): the `KBP_ADVANCED_SEED` (asked once), the
supervised manual acceptance session (`docs/MANUAL-USABILITY-HANDOFF.md`,
now on the RC2 checkout), and the final review. Merge, release and
permanent installation stay with the owner.

## After rc1: animated mode, the player's stop, a cantrip defect (history)

Work toward the second candidate (0.2.0-rc1 stays frozen at `f8562a6`).

| Commit | Content |
| --- | --- |
| `2c04024`, `9f2bdf6` | The qualification runs on the planner's own host (its per-frame pump); the stop is the player's routine press while the first cast is in progress; allowance schema 4 names the casting mode (instant or animated), enforced by launcher, host, boundary and protocol |
| `46eaabf` | Every qualification run writes `cantrip-diagnostics.json` (how the game judges each caster's level-0 spells) |
| `70f135a` | **Cantrip fix**: a level-0 spellbook entry executes through the caster's at-will cantrip ability (as the game's action bar); validation is the cast command's own `IsAvailable`; discovery prices level 0 as free only with that ability. Qualification **disable step**: the planner's own disable and enable during a run (animated: while the cast is in progress; instant: before the first step) |

What the animated run found (`casting-qual-cast-20260923-a1-anim-01`,
FAIL, restored, saves clean): the player's stop landed in flight as
designed, but the game's own cast command for the first Resistance ended
with Fail; the planner halted (casting Failed, nothing spent, the other
two not attempted). Cause, from the game's code and the live diagnostics
(`casting-qual-select-20260923-d1-01`): the class grants each cantrip as
an ability usable at will (no spellbook, count -1); the spellbook's own
level-0 entry needs a level-0 slot and these books have 0 per day. rc1
submitted the spellbook entry: animated cantrips always failed natively;
instant mode cast it only because the cast rule skips availability.

Then: `70f135a` gated and frozen (qualification-frozen); selection
`casting-qual-select-20260923-a2-01` (four forecast steps), animated
`casting-qual-cast-20260923-a2-anim-01` and instant `-a2-inst-01` PASS;
0.2.0-rc2 built, frozen and run as above.

## Release candidate 0.2.0-rc1 frozen at `f8562a6`

Receipt: `docs/evidence/rc-0.2.0-rc1-receipt.md` (identities, gate, the
five runs on the frozen build, the temporary install and rollback, the
migration audit, what is not established). Clean checkout
`repo/KingmakerBuffPlanner-RC1`; frozen copy
`runtime-backups/rc-frozen/f8562a6.../`.

| Commit | Content |
| --- | --- |
| `85df35a`, `5b9f58b` | Launcher test timeouts; the scripted workspace casts pick a legal recipient for their caster (Aid Another cannot target its caster) |
| `d7f7af6` | Restoration retries a transient sharing lock on the Mods folder moves |
| `f8562a6` | No runtime transaction or local install while the Gunslinger lab's lease on the same installation is held (the two sessions check each other's locks) |

On the frozen build: `casting-qual-select-20260923-rc1-01`,
`casting-qual-cast-20260923-rc1-01` (native casts, zero violations),
`casting-ws-import-20260923-rc1-01`, `casting-ws-reload-20260923-rc1-01`
and `casting-ws-manual-20260923-rc1-rehearsal` all PASS and complete;
temporary install and rollback `rc1-temp-deploy-20260923-01` restored the
owner's planner exactly.

Owner inputs: the `KBP_ADVANCED_SEED` (asked once), the supervised manual
acceptance session (`docs/MANUAL-USABILITY-HANDOFF.md`), and the final
review. Merge, release and permanent installation stay with the owner.

## RC mission (2026-09-23, continued): review fixes, workspace text, in-game reload, 0.2.0-rc1 (history)

Worktrees: dev `repo/KingmakerBuffPlanner-O1` (PR branch); gate and live
runs from the clean detached `repo/KingmakerBuffPlanner-G` (moved to each
commit under test); `-Q1` (`d35b38f`) and `-Q2` (`48e46e3`) hold earlier
run candidates.

| Commit | Content |
| --- | --- |
| `548a90d` | Independent review of `e7c5207..f7726c9` (no P0/P1): probe pumps only while the world runs (P2-1); `CastingWorldClock` for the production and qualification hosts (P3-1/2); restoration failures no longer skip the protected-save comparison or `run-completion.json` (P3-3); no save change for the selection and every advanced run (P3-4); launcher logic in tested functions (P3-5) |
| `1332ed8` | Workspace text: refusals say what to do next; the header counts the routine; the inspector names the casting being edited; taller buff tabs |
| `478bb5b`, `07cf011` | `live-workspace-reload`: the exact WORKING save loaded again in game under a fresh read-only saver; the reopened planner must show the saved plan, one subscription, one HUD root, no run |
| `2024578` | Version 0.2.0-rc1, release notes, install text, manual session for the RC |
| `1ed2418`, `542cd66` | Canvas scale evidence; alphabetical buff grid |
| `a1bcc5f`, `fd2dfcb` | Independent review of `f7726c9..1332ed8` (no P0-P2): restoration failures reported in the record and the error; footer named and ordered; refusal advice; probe budget; canonical scenario spelling |
| `5c70f1a` | `live-workspace-import`: first open with a classic plan seeded from live discovery, judged against the real migration |
| `7255b8a` | Independent review of `1332ed8..542cd66` (no P0): the reload stays read-only for the whole load under write sentinels; strict saves for reload and import; disk read-back, exact area events and all HUD roots checked; committed buff selection; card reasons and import review items in words |

Live runs (all restored and verified, every save unchanged):

| Run | Result |
| --- | --- |
| `casting-ws-qual-20260923-q2-03` (`48e46e3`) | PASS: resource names and footer fixed in game; showed refusal codes, casting ids and "0 of 0 … ready to apply" |
| `casting-ws-qual-20260923-r1-01` (`1332ed8`) | PASS: those fixed in game |
| `casting-ws-reload-20260923-s1-01` (`478bb5b`) | FAIL, clean: the guarded reload's header protocol completed; the campaign was then read mid-load (party 0). Fixed in `07cf011` |
| `casting-ws-reload-20260923-s2-01` (`2024578`, 0.2.0-rc1) | **PASS**, 92 assertions: Game.LoadGame of the exact WORKING descriptor, header update and commit suppressed (no disk write), after-load callback, stable campaign identity after 6.5 s; reopened planner: saved plan, same campaign, subscriptions 1→1, HUD roots 1→1, no run, clean |
| `casting-ws-qual-20260923-t1-01` (`1ed2418`) | PASS: workspace and native canvas both at scale 1.000 at 1920×1200 |
| `casting-ws-import-20260923-i1-01` (`5c70f1a`) | **PASS**: the real migration imported the seeded classic plan as two drafts needing review; classic file byte-unchanged and archived byte-exact; the frames showed reason codes on the cards (fixed in `7255b8a`) |

## RC mission (2026-09-23, continued): live native evidence and the Pro review of `e7c5207` (history)

Owner message of 2026-09-23 (second): allowance files are written by
Claude under the delegated mission authority; the sealed Gunslinger
0.0.133 is staged from the owner-approved exact copy; the historical
`e964d2f` freeze is preserved but no longer blocks development; fix
RC1-RC4. Worktrees: dev `repo/KingmakerBuffPlanner-O1` (PR branch),
frozen probe candidates `repo/KingmakerBuffPlanner-P1` (`7664f2f`) and
`-P2` (`320a1b6`), historical `repo/KingmakerBuffPlanner` (`e964d2f`).

| Commit | Content |
| --- | --- |
| `7664f2f` | `full-user` stages Gunslinger 0.0.133 from `C:\Dev\KingmakerBuffPlannerLab\examples\KingmakerGunslinger` (all five sealed identities re-verified, provenance in `examples\KingmakerGunslinger.provenance.json`, never committed or packaged); the transaction records the installed 0.0.136 (238 files, manifest `d08f0d5a...`) before activation and re-verifies it after restore |
| `2a443c3` | RC1 typed slot-token readings (native ids contain "\|"); RC2 a step casts only on a complete before-read |
| `320a1b6` | Cast only while the world runs: the probe closes the planner and waits for Default mode, not paused; the production host and the qualification driver advance only then |
| `1af543c` | RC3 `run-completion.json` gates advanced casting on whole-run success; RC4 per-casting recipients (three-unit parties qualify); roster evidence |
| `d35b38f` | Probe receipt and review-index dispositions (frozen as the qualification candidate, worktree `repo/KingmakerBuffPlanner-Q1`) |
| `f7726c9` | Advanced seed: the one missing input and its compatibility decision (`docs/ADVANCED-SEED-COMPATIBILITY.md`) |
| `48e46e3` | Workspace text from the live frames: whose resource and what kind instead of pool keys, footer budget kept inside its third, routine display names on cards; qualification receipt |

Live runs (all with verified Mods restoration and unchanged protected saves):

| Run | Result |
| --- | --- |
| `casting-probe-select-20260923-p1-01` (7664f2f) | PASS: same Resistance source, caster, recipient and projection as the historical proposal |
| `casting-probe-cast-20260923-p1-01` (7664f2f) | FAIL, one invocation: cast inside the open planner, effect never landed (`TimedOutUnconfirmed`); cause fixed in `320a1b6` |
| `casting-probe-select-20260923-p2-01` (320a1b6) | PASS, same selection |
| `casting-probe-cast-20260923-p2-02` (320a1b6) | **PASS: first confirmed casting-first native cast** (new ResistanceBuff instance, free, zero violations) |

`casting-probe-cast-20260923-p2-01` is void (allowance mis-bound to the
P1 candidate by a script path error, never used, VOID notice beside it).
Receipt: `docs/evidence/casting-probe-20260923-receipt.md`.

Qualification on the frozen `d35b38f` (worktree `-Q1`):

| Run | Result |
| --- | --- |
| `casting-qual-select-20260923-q1-01` | PASS: roster Hedwirg, Linzi, Tartuccio (no pets); Resistance by Linzi and Tartuccio, three forecast projection ids |
| `casting-qual-cast-20260923-q1-01` | **PASS**: stop, complete, repeat and recast exactly as forecast; three approved submissions (6 planned casts of a 6 budget); recast from a session reopened from disk under the restored acceptance; every save unchanged, none created |
| `casting-ws-qual-20260923-q1-01` | PASS, 91 assertions (non-casting workspace run; source tabs invoked). Its frames showed internal pool keys, the footer under the buttons and "in long": fixed in `48e46e3` |

Receipt: `docs/evidence/casting-qual-20260923-receipt.md`.

Remaining owner input: a designated `KBP_ADVANCED_SEED` (and, if that
save was made under Gunslinger 0.0.136, the profile decision for it).
Next: the live workspace run on `48e46e3` (worktree `-Q2`), a temporary
guarded deployment of the actual release package with verified rollback
(section 11), remaining section 10 UI work, then the advanced copy once
designated.

## RC mission (2026-09-23): production execution integration and advanced-copy tooling (history)

Mission: the casting-first release candidate (owner message of 2026-09-23).
Development continues in the worktree `repo/KingmakerBuffPlanner-O1` on the
PR branch; the frozen probe checkout `repo/KingmakerBuffPlanner` stays
detached at `e964d2f`, untouched.

| Commit | Content |
| --- | --- |
| `3b69321` | Production execution. Planner-mode setting (`UserSettings/planner-mode.json`, Classic by default, casting-first chosen in the UMM panel). Every routine route (HUD, hotkey, spellbook, planner Apply, Ready Casts Only) goes through the session Apply to the production boundary and the execution host: one run at a time, pumped per frame, one terminal for completion, player stop (press the routine again), deadline, area change, disable, unload and teardown. Live existing-effect policy: weaker, expiring, unprovable or suppressed effects never count as satisfied, and an exhausted pool no longer blocks an already-active effect. Review acceptance is per routine and persisted as a digest. Save keeps the player's execution/UI settings. Runtime-test sessions are locked to the refusing boundary. |
| `6b92020` | Advanced-copy tooling: `live-advanced-inspect` (non-casting), protocol fixture families, launcher `-FixtureFamily Advanced` bound to the bootstrap manifest, protected-save comparison around every live run. Workspace cards disclose skips, recasts and "cannot run in this version" limits. |
| `7e864e7` | Casting-first feedback without floating results (refusals kept per routine on the HUD tooltip and planner footer); player guide `docs/CASTING-FIRST-PLAYER-GUIDE.md`. |
| `6e55990`, `7ebdb67`, `4e7e921` | Casting qualification: allowance schema 3 bound to commit/package/DLL/MVID/fixture, recipe `zero-cost-mixed`, step forecast with exact projection ids, bounded boundary (next approved id only, budget, no retry), judged driver (stop, complete, repeat, reopen, recast), scenarios `live-cast-qual-select` (no cast) and `live-cast-qual` (allowance-bound). |
| `0228278` | `docs/CASTING-QUALIFICATION-REQUEST.md`: purpose, contracts, expectations and procedure for the first qualification run. |
| `47caeef` | Fixes for the independent review of `ca0d636..325e4b3` (dispositions in `docs/CASTING-FIRST-REVIEW-INDEX.md`): launchable inspection scenario, exhausted rods waived by an active effect, acceptance never revoked by viewing, two-round expiry floor, graceful player stop, classic path locked in automation. |
| `4097ca7` | Routine tabs show names, counts and a gold selected state; header states the routine's own Apply gate. |
| `6adda37`, `0c9b04e` | Workspace: Spells / Abilities / Other tabs on the buff grid (the live scenario clicks them inside its no-mutation check); each casting card shows its own outcome in the last run. |
| `af5f0ca` | Fixes for the second independent review (`54d330b..47caeef`): the qualification driver judges each step the moment it ends and stops at the first failed, uncertain or cancelled step (P0, no run had happened); 900-second harness floor; strict saves for the casting run; compatibility over exhausted rods; non-vacuous selection-only evidence; exact reopen digest. |
| `ebf2329` | Qualification recipe `finite-direct-mixed` for the advanced copy: finite prepared exact slots, spontaneous levels, mixed casters, metamagic variants; spend-aware forecast; per-step availability and exact-token judging; casting on the advanced copy only with its allowance and after a passing inspection of the same pair. |

Gates at `ebf2329` (Windows PowerShell 5.1, non-interactive): source 42/42,
protocol 287/287, harness 34/34, deploy WhatIf 5/5, launcher WhatIf 12/12,
fixture 3/3, Restore-InstallLocal 16/16, publisher 3/3. Every new guard
has a mutant that the tests catch (17 in slice 1, 8 + 2 drift and 4 in
the two review-fix rounds, 8 for the finite recipe).

Live runs this slice (no native cast):

- `casting-ws-claude-qual-20260923-125047` at `3b69321`: refused before
  launch by the `full-user` compatibility check (KingmakerGunslinger
  identity mismatch).
- `casting-ws-claude-qual-20260923-125326-hr` at `3b69321`, profile
  `human-reproduction`: the automation fixture does not load without the
  full mod set (`Player.PostLoad` failed; save writes stayed suppressed),
  FAIL on the load timeout, restoration verified.

Owner blockers:

1. The frozen Resistance probe needs its allowance file. My write of
   `approvals/casting-probe-cast-20260923-01.json` was refused by the tool
   permission classifier; the owner can run
   `scratchpad/write_allowance.ps1` or allow writes under `approvals\`.
2. KingmakerGunslinger 0.0.136 was installed on 2026-09-23 around 11:45;
   `full-user` is sealed at 0.0.133. Every automation-fixture run and the
   frozen probe are refused until the owner restores 0.0.133 or approves
   resealing (the frozen probe checkout cannot change, so it needs
   0.0.133 back). A third option exists for the dev branch only: three
   Gunslinger lab backups of the live folder are byte-identical to the
   sealed 0.0.133 directory, so `full-user` could stage Gunslinger from
   an exact copy (the immutable-fixture mechanism of `3bd519b`). An
   attempt to set that up was stopped by the tool permission classifier
   as a shared-resource change and fully reverted; it is the owner's
   decision. An inert read-only copy remains at
   `C:\Dev\KingmakerBuffPlannerLab\examples\KingmakerGunslinger` (outside
   the repo, unused) for the owner to keep or delete.
3. No `KBP_ADVANCED_SEED` exists yet.
4. The casting qualification needs its own owner-created allowance after
   `live-cast-qual-select` reports the projection ids
   (`docs/CASTING-QUALIFICATION-REQUEST.md`); that selection run itself
   also needs blocker 2 resolved.

Next, independent of the blockers: a second independent review (of the
qualification commits and the review fixes), remaining UI items, RC
packaging and documentation.

## N-series (`7379dc8` review): frozen-artifact binding, terminal shutdown, exact prepared-slot reads — 2026-09-23 (history)

N1–N3 fixed in `b9721c3`; advanced-copy fixture family in `bdac45f`; the
active proposal is rewritten for the frozen artifact (the `7379dc8`
version is kept in `docs/probe/history/`). The frozen package for the
final commit is under `runtime-backups/probe-frozen/<commit>/` with
`FREEZE.json`; its identity is in the PR #2 body. No cast was run, no
allowance exists, and no live run was made in this slice. Evidence is
source and recording-runtime tests only.

## M-series (`1e3c95b` review): zero-cost path, authoritative observations, owned cleanup; concrete cantrip proposal — 2026-09-23 (history)

The review-and-roadmap document named in the instructions was not on this
machine; the work followed the pasted instructions. The L fixes, UI,
loader, staged rollback and coordinator are preserved. Normal dispatch
stays disabled. **No native cast was run.**

| Commit | Content |
| --- | --- |
| `a023293` | M1: verified Unlimited pools are real zero-unit native reservations end to end |
| `0a4c382` | M2: fresh, sequenced native observations; M3: one owned idempotent terminal cleanup |
| `ea45807` | Probe subset accepts discovery's self-reference wrapper around plain buffs |
| (docs) | Concrete probe proposal, unapproved template, advanced-copy request, release buckets |

Evidence layers:

- **Source tests:** full Windows PowerShell 5.1 gate at `ea45807`:
  source 42/42, protocol 254/254, harness 28/28, deploy WhatIf 5/5,
  launcher WhatIf 8/8, fixture 3/3, Restore-InstallLocal 16/16,
  publisher 3/3. 17 M-series mutants plus the wrapper mutant were caught.
  Mutation results show the tests are sensitive; they are not runtime
  proof.
- **Recording-runtime:** M1 drives both real executors (free confirmed,
  spend-on-free fails, unverified zero never reaches the runtime). M2/M3
  compose the real owner, session, boundary, coordinator and instant
  executor with a recording runtime and a scripted native observer.
- **Actual gameplay:** none.
- **Live non-casting runs:** two selection-only runs this session
  (`casting-probe-select-20260923-062552` at `0a4c382`: zero-cost
  accepted, refused on effect shape; `…-063054` at `ea45807`: **PASS**,
  Resistance by `2b56df7d…` on `050aa19a…`, verified Unlimited,
  ProjectionId `ee8e76b2…`). No boundary was constructed in either; the
  workspace closed, the lease was released and restoration was verified.
- **Manual UI acceptance:** nothing new.
- **Restoration:** verified for both runs. Rollback changes remain
  proven only on isolated fixtures.

Owner decisions pending:

1. Approve (or not) the one cantrip cast in `docs/LIVE-CAST-PROBE-REQUEST.md`
   by writing the allowance file. Claude will not write it.
2. Approve the advanced-save copy inspection in
   `docs/ADVANCED-SAVE-COPY-INSPECTION-REQUEST.md` by saving a
   `KBP_ADVANCED_SEED` in-game. The tooling for it is not implemented yet.

## L-review (`475d2b9`) answered; single-cast probe prepared and dormant — 2026-09-23 (history)

Pro's `475d2b9` follow-up review (L1–L6) is answered in source. The UI,
repaired loader, coverage fix, failed-import blocking, provenance ids,
staged rollback and stop-after-failure coordinator are all preserved.
Native dispatch stays disabled in the production workspace. No probe
cast was run.

Commits (all pushed through the guarded helper; remote HEAD verified at
each push):

| Commit | Content |
| --- | --- |
| `58a1aaf` | L1 imported requirements enforced in the gate, compiler and authoring service |
| `2929985` | L2 coordinator owns and disposes the active executor; animated cancel reporting |
| `cd5bc4a` | L3 complete versioned projection identity; L6 whole probe subset |
| `aa3b95c` | L4 partial-swap recovery; L5 candidate compatibility by declared format |
| `6f22a78` | Dormant probe: selector, strict allowance, one-shot boundary, gated scenarios |
| `2117438` | Probe selector records per-candidate rejection reasons |

Evidence layers, kept separate:

- **Source tests:** full Windows PowerShell 5.1 gate at `2117438`:
  source 42/42, protocol 250/250, harness 28/28, deploy WhatIf 5/5,
  launcher WhatIf 8/8, fixture 3/3, Restore-InstallLocal 16/16,
  publisher 3/3. Each new guard was mutation-checked (L1 6/6, L2 6/6,
  L3 6/7 with one equivalent survivor, L6 5/5, L4/L5 script 6/6, probe
  8/8).
- **Recording-runtime evidence:** L2 and the probe boundary drive the
  REAL instant and animated executors through scripted runtimes. This
  proves disposal, halting and one-shot logic, not game behaviour.
- **Actual gameplay:** none. No native cast has been attempted by the
  casting-first path.
- **Live non-casting runs:** two `live-cast-probe-select` runs
  (`casting-probe-select-20260923-052640`, `…-053136`) on the WORKING
  fixture. Each had zero synthetic input and no dispatch boundary; the
  workspace closed, the input lease was released and restoration was
  verified. Both returned FAIL at `probe-validation` because no eligible
  casting exists. The fixture's spellbook buffs are all cantrips, which
  the converter cannot project (zero native cost), and the rest are
  fact abilities.
- **Manual UI acceptance:** nothing new.
- **Restoration:** verified for both live runs. K6/L4/L5 rollback
  changes are proven only on isolated fixtures, never on the live
  installation.

Operational note: a gate run launched with an interactive stdin hung in
the launcher test's ShouldProcess pattern check (a confirmation prompt).
Gates are now run with `-NonInteractive` and stdin closed. A later gate
hit a transient "access denied" moving a freshly extracted folder in the
harness test's own temp area; the immediate rerun passed.

Next (owner decisions, see `docs/LIVE-CAST-PROBE-REQUEST.md`): either
approve modelling zero-cost native casts (cantrips) so a selection
exists on the current fixture (recommended; source work), or approve a
fixture with a level-1 buff slot. The cast itself always needs a
separate allowance file written by the owner.

## K-review (`258a1d0`) answered; first live cast prepared as a request — 2026-09-22 (history)

Pro's `258a1d0` review (K1–K7) is answered in source; the new UI, native
page borrowing, the converter and the earlier repairs are preserved; the
UI was not restarted and no loader/fixture investigation was reopened.
Native dispatch remains disabled.

Commits: K7 `93408bd`, K1 `e0509fe`, K2/K3 `0c7b760`, K4 `b69f873`,
K5 `20bbe82`, K6 `b243b02`, then the probe-scope test tightening and
these documents. Pushed through the guarded helper; remote HEAD verified.

Evidence layers, kept separate:
- **Source tests:** full PS 5.1 gate at `b243b02` — source 42/42,
  protocol 242/242, harness 28/28, deploy WhatIf 5/5, launcher WhatIf
  4/4, fixture 3/3, Restore-InstallLocal 12/12, publisher 3/3. The
  probe-scope tightening adds cases inside an existing test (still
  242/242); each probe refusal was mutation-checked.
- **Recording-runtime evidence:** K4/K5 run the existing executors over
  scripted/recording runtimes. This proves ordering and halting logic,
  not any game behaviour.
- **Actual gameplay:** none. No native cast has been attempted by the
  casting-first path.
- **Manual UI acceptance:** unchanged — the owner's second session was
  skipped at their request after a positive glimpse; no human
  acceptance of the current layout is claimed.
- **Restoration:** no live runs in this slice. K6 changes the install
  rollback script only and is proven on isolated fixtures, not on the
  live installation.

Next: the owner decides on `docs/LIVE-CAST-PROBE-REQUEST.md` (one cast,
single target, no rod, WORKING fixture, isolated candidate without
automatic import). Nothing in it runs without that separate approval.

## J-review repaired; implementer handoff Z → Claude — 2026-09-22 (history)

Implementation moved from Z to Claude (Z's quota exhausted); Pro remains
the independent reviewer. Same branch, same draft PR #2, same authority
scope; nothing was reset or restarted. Takeover reconciliation at
`19ecbe8`: local == origin, clean tree, no Kingmaker process, no
`deployment.lock`, latest transaction (`casting-ws-manual-rehearsal-6`)
`Restored` with `restorationVerified=true`, and the live `Mods` tree
independently re-compared to that transaction's original manifest
(1109 entries, 0 differences). Baseline gate at `19ecbe8`: source
42/42, protocol 226/226, harness 27/27; the launcher `-File` WhatIf
layer refused because the local package had been built at `0551949`
(the build-manifest/HEAD contract working as designed). The
rehearsal-6 package was archived locally before rebuilding.

J1 (automatic evidence adjacency mismatch) and J2 (manual terminal
accepted the final capture by filename, ignoring failure and camera
restoration) are repaired through Unity-free contracts in
`src/KingmakerBuffPlanner/RuntimeTesting/WorkspaceScenarioContracts.cs`
that the runtime host now calls; C3 (hold range/narrowing) aligned.
Protocol 231/231 (five new regressions, each mutation-checked). See
the CURRENT STATE section of `planning/CASTING-FIRST-MIGRATION-STATUS.md`
and the dispositions table in `docs/CASTING-FIRST-REVIEW-INDEX.md`.
Rehearsal-6 receipt: `docs/evidence/casting-ws-manual-rehearsal-6-receipt.md`
(done-path rehearsal only; its build predates the J2 repair — the
final capture's clean restoration was recovered from the game log).

RUNTIME (rehearsal, labeled — never human acceptance):
`casting-ws-claude-rehearsal-20260922-194113` on the `9aba411` package
(ZIP `f447a04b…`, DLL `71b72725…`, MVID `2747b002-15f5-44dd-8bcf-de3286637de9`)
PASSED under Windows PowerShell 5.1: manual-ready, rehearsal done marker,
`manual-final-capture` captured (nonBlack), `manual-camera-restoration`
targetsRestored/activeRestored/0 cleanup failures, `manual-workspace-closed`
(view closed, input lease released), owned exit, transaction Restored
verified. Its final frame — like rehearsal-6 and the gseries-081000
"acceptance" frame — showed every lane's scroll view collapsed to a
100x100 box mid-lane and the raw `variant|<guid>|<guid>` key in the
header: callback acceptance never checked layout. Repaired next (lane
scroll views fill their columns with growing content; header shows the
discovered buff name; caster names no longer run under Focus).

The harness must run under Windows PowerShell 5.1 (`powershell.exe`);
it now refuses PowerShell 7 (JSON date conversion broke manifest checks).

`casting-ws-claude-rehearsal-20260922-194601` on `5781301` confirmed
the lanes fill their columns in a live frame (PASS, restored verified).

SUPERVISED SESSION DONE: `casting-ws-claude-manual-20260922-200244`
(`5781301`) — lifecycle PASS on Howie's done instruction, restored
verified; usability verdict FAIL (functional Add/Undo, "I really hate
this UI"). Record: `docs/evidence/casting-ws-claude-manual-20260922-200244-session.md`.
Owner direction: Bubble Buffs–like look, casting as the atomic unit —
`docs/UI-END-GOAL.md`.

USABILITY PASS (`afe2749` + label follow-up): duplicate buff sources
disambiguated (variant names without the repeated base name, then
kind/caster, then ordinal); caster-lane button now picks the caster for
the next casting ("Use"/"Casting"); empty-lane guidance; routine tabs no
longer clipped; lanes clear the footer. Live frame
`casting-ws-claude-rehearsal-20260922-202050` (PASS, restored verified)
confirmed the tabs, hint and distinct labels. The automatic
`live-workspace-qual` scenario (J1 at runtime) has NOT been re-run since
the repair: it injects keystrokes and the owner was at the desktop.

J1 RUNTIME PROOF: `casting-ws-claude-qual-20260922-203846` on `6a6c42d`
(`live-workspace-qual`, full-user) — full PASS incl.
`workspace-interaction-sequence` with the exact producer shape that the
old validator rejected (`state1=invoked`, `state2/3=already-ready`) and
`saved=True` observed via `!IsDirty`; restored verified. The owner has
stated the lab is a dedicated dev machine: run scenarios (including
synthetic input) without asking.

BUBBLE BUFFS-STYLE LAYOUT (`a2c104b`): icon grid of buffs on top with
per-routine casting counts; castings of the selected buff as cards with
caster → recipient portraits; inspector with "Cast by" / "Cast on"
portrait tiles and the green/amber coverage legend.
`casting-ws-claude-qual-20260922-204316` on `a2c104b`: every interaction
assertion PASSED, but the run FAILED `workspace-frame-nonblack` because
BOTH the control frame (workspace closed) and the open frame were 100%
black on the display path — the documented intermittent
black-presentation state, not the layout (the camera-lane frames render
the new layout). Restored verified. Known capture-lane artifact: a
camera capture taken in the same frame as a rebuild can blend stale
and new UI (ws-interact-authored.png); stable frames are clean.

Rerun `casting-ws-claude-qual-20260922-204631` on `a2c104b`: identical
(interactions PASS, display frames 100% black, restored verified); at
that moment `query user` showed the owner's RDP session as Disc — the
known disconnected-session black backbuffer. Display-path acceptance of
the new layout therefore waits for a connected session; session state
is never changed by the agent.

`2e82a14` added buff search (view-only filter) and Ready-by-default
castings; `casting-ws-claude-qual-20260922-205126` again passed every
interaction with black display frames (RDP Disc), restored verified. Its
camera frames showed the coverage legend working, a clipped (blank)
search field, a meaningless "coverage 1/0" on direct cards (inverted
condition) and id-style card titles — all fixed next (cards read
"Caster → Recipient (Casting n in routine)").

DISPLAY-PATH PROOF of the new layout: `casting-ws-claude-qual-20260922-210851`
on `c9658f1` with the owner's session Active — FULL PASS (open frame
nonblack, changedFraction 0.948 vs control, all interactions), restored
verified. This confirms the black frames were the disconnected session.
Second supervised session `casting-ws-claude-manual-20260922-211054`:
the owner, after a glimpse ("significantly improved... a much better
direction"), asked to skip it; relayed as manual-stop → result FAIL
stage `manual-cancelled` by design (first runtime proof of the stop
path), final capture + camera restoration + close all PASS, restored
verified.

NATIVE PAGE ART (`a095aca`, `7a0f673`): the frame borrows the native
spellbook page (`ServiceWindow/SpellBook/BookBackground`, sprite
`Inventory_Book_Clear`) onto the owned frame only; lanes are unfilled
framed boxes; header/footer ink is light outside the book.
`casting-ws-claude-qual-20260922-211523` (`a095aca`, session Active):
FULL PASS incl. `page=native` in presentation evidence.
`...-211922` (`7a0f673`): interactions PASS, display black (session
Disc), camera frame shows the finished page look. Open nit: lane titles
clipped by the book's left edge.

FURTHER SLICES (each gated + pushed + live-run; FULL PASS on the
display path whenever the session was Active, black-frame-only FAIL
with all interactions PASS when Disc; every run restored verified):
- `f3f9509` red invalid-target state from the chosen caster's reachable
  targets (illegal tiles not selectable); lanes inset inside the page
  (`...-215709` FULL PASS).
- `0a56785` retarget / origin / coverage choices as portrait tiles;
  castings on the left page, inspector on the right (`...-220104`
  FULL PASS incl. retarget through the tiles).
- `f214553` Add Casting / Done pinned to the inspector title row
  (`...-220517` FULL PASS).
- `6a02a77` castings lane scope toggle: this buff / whole routine
  (`...-220903`, session Disc: interactions PASS, display black).
Per-caster popover deferred: the casting-first compiler accepts
ICastingTargetingModifier but no implementation exists; the legacy
Brown-Fur Share Transmutation adapter needs a transmuter the fixture
party lacks (would be Not Run).

- `c4d3b03` group cards: predicted-beneficiary portraits, missed
  coverage in red, anchored origin and "Outside coverage" by name.

EXECUTION INTEGRATION (charter §5.3 / takeover §10.B), native dispatch
still disabled:
- `2264ff9` `Planning/ExplicitCastingStepConverter`: an allowed apply
  decision → the CastPlan the existing executors consume; exactly one
  step per approved Ready casting in decision order with exact provider,
  target shape (direct, or ONE mass step at caster/anchor expecting the
  predicted beneficiaries), applied/omitted enhancements, enhancement
  pool usage, native reservation, material; any unconvertible casting
  refuses the whole conversion.
- `53c7fe1` Session.Apply projects before the disabled boundary (an
  unconvertible plan is refused and never reaches it); the footer says
  "Native casting is disabled — nothing was cast. Would run N casts".
- Regression drives the EXISTING `InstantCastExecutor` over the
  projection with a recording runtime: one fire per approved casting,
  in order. Live runs `...-221951`, `...-222501` (session Disc):
  interactions PASS, display black, restored verified.

- `b8ad634` the AnimatedCastExecutor likewise starts exactly one
  native command per approved casting, in order, over the projection.
- `28d8a83` enhancement toggles are compact chips (gold when selected).

MIGRATION / ROLLBACK (charter §7). A read-only gap analysis found the
schema-5→6 importer and migration service built and tested but NEVER
invoked by the product. Now:
- `85edff0` first workspace open with no candidate runs the migration
  (legacy file untouched, exact original archived, candidate written +
  reopened; discovery grouping kinds passed so group assignments are
  not split as pinned); report exposed on the session and summarized
  once in the footer; second open loads the candidate. Live log
  (`...-224326`): `load=Absent;legacyImport=LegacyAbsent` (the fixture
  has no legacy profile for the WORKING campaign).
- `1767239` legacy `ProfileRepository.Save` quarantines a corrupt or
  newer-schema primary byte-exactly (content-keyed `.orig`) before
  overwriting it; the migration archive is byte-exact (BOM survives).
- Rollback: `Restore-InstallLocal.ps1` deactivates schema-6 candidate
  profiles (archived under the rollback evidence, recorded in the
  install record) instead of merging them beside the older build.
Remaining §7 gaps: legacy Load still returns a default on failure
(no read-only refusal for a newer legacy schema), candidate write is not
staged-then-activated, group/automatic drafts have no explicit
acceptance UI, missing-reference checks against the catalogue.

NEXT (exact action): owner decision on the first live gameplay-test
scope (single-target, non-rod, dispatch enabled only for that run) —
separate authority; meanwhile the remaining §7 gaps above and an
import-review panel for imported drafts; rerun `live-workspace-qual`
when Active; continue `docs/UI-END-GOAL.md`: search
box, native parchment/frame pass (charter §6), per-caster popover,
recipient invalid-target state; supervised session on the new layout — icon-first
searchable buff grid with disambiguated variants, portrait strips with
the coverage colour legend, casting cards with portraits and enhancement
chips; then the native parchment pass. Historical next step:
when Howie confirms availability, the supervised manual session per
`docs/MANUAL-USABILITY-HANDOFF.md` with Claude relaying done/stop via
the run-bound marker. Afterward: fix the observed defects, then the §5
native aesthetic pass and the remaining charter gates. Native casting
stays disabled.

## I-review answered; manual success path PROVEN by rehearsal — 2026-09-22 (history)

The fifth review (I1-I5 at 5c927cb) is answered at pushed HEAD
`0551949` (verified): I1 the pre-open control frame is captured and
consumed for EVERY workspace scenario and manual opens programmatically
with no input request or wait; I2 manual-ready one-shot transitions
into the reachable marker-watching hold, stop deterministically wins
over done; I3 manual has its own result contract (ready acknowledged,
zero automatic authoring, done-path outcome) and inherits no automatic
assertions; I4 manual request validation enforces the COMPLETE live-save
contract plus exactly the hold (orchestration regressions: valid
combined request + eight rejected defect classes through real TryRead);
I5 the State outcome is per-step evidence accepted as invoked or
already-ready with correctness on the exact-record checks. Protocol
226/226.

LIVE PROOF (rehearsal, labeled — never human acceptance):
casting-ws-manual-rehearsal-4 ran the pre-fix build and confirmed I1
live (phase-0 input wait, 46k ticks, timeout; restored verified).
casting-ws-manual-rehearsal-6 on the fixed build PASSED end to end:
programmatic open after control capture, manual-ready.json
(syntheticInputRequested=false, workspaceOpen=true, hold 60s), the
rehearsal done marker consumed, final camera frame captured, workspace
closed via production lifecycle, PASS manual-session-outcome, owned
exit, transaction restored verified. Rehearsals 1-5 preserved as
history (request-clobber bug, throttled session, stale-package refusal,
running-game refusal — all restored or refused cleanly).

NEXT (exact action, owner required): the supervised manual session per
docs/MANUAL-USABILITY-HANDOFF.md — Z launches with -ManualHoldSeconds
900 -TimeoutSeconds 1500 and a concrete RunId (e.g.
casting-ws-manual-1435), confirms manual-ready.json + the MANUAL-READY
console line, reports build identity, then transfers interaction to
Howie; done/stop relayed through the marker files; acceptance is based
on Howie's observations, not the done file. Afterward: §5 native
aesthetic pass, launcher physical-input seam qualification.

## H-review answered; manual mode implemented; live rehearsal awaits a connected session — 2026-09-22

The fourth review (H1-H4 at c01f88bd) is answered at the pushed HEAD:
H3 identity resolves BEFORE the input lease (unresolved/failed opens
hold nothing); H2 the four group-callback defects fixed (remembered
restore reachable, caster-centered coverage never demands an anchor,
incoming coverage snapshotted before mutation so the draft's own list
is safe, focused origins from the FOCUSED record's provider) with the
seven specified regressions; H4 exact expected records (ability,
spellbook, routine, state, enhancements) + canonical serialized sibling
signature + state-aware control clicking. H1 live-workspace-manual is
a REAL scenario end to end: launcher params (ManualHoldSeconds 30-1200,
TimeoutSeconds >= hold+420, -ManualRehearseDone), protocol validation,
zero synthetic input in-scenario, manual-ready.json acknowledged only
with the workspace open + legacy closed, non-blocking hold, terminal
manual-done.json / manual-stop.json markers (deadline ends
manual-deadline and is NEVER acceptance), final capture + production
close + restore. Protocol 225/225.

LIVE REHEARSAL STATUS (honest): first attempt (rehearsal-1) exposed a
real request bug — the hold parameter was clobbered by the save-pair
parameter set, the request was rejected, and the game idled at the
menu; the owned process was stopped and the transaction RESTORED
VERIFIED. Bug fixed (0efa2fe). Second attempt reached the loaded
campaign but the game's update loop throttled to ~20 ticks/645s because
the owner's RDP session disconnected mid-run; transaction restored
verified. The done-path rehearsal needs ONE connected-session run
(-ManualRehearseDone) and is the same precondition as the supervised
session itself.

NEXT (exact action, owner required): with Howie connected — (1) Z runs
the rehearsal: live-workspace-manual -ManualHoldSeconds 60
-ManualRehearseDone -TimeoutSeconds 900; verify manual-ready.json +
MANUAL-READY console line + done marker consumed + PASS
manual-session-outcome + restored receipt. (2) The supervised session
per docs/MANUAL-USABILITY-HANDOFF.md (hold 900), Z acknowledges the
paused state before Howie touches anything, ends via the marker files.
Then the §5 aesthetic pass and launcher seam qualification.

## G-review answered; corrected control-path acceptance PASS — 2026-09-21

The third review (G1-G5 at 8dcdef0) is answered at HEAD (pushed,
verified): G3 campaign-bound session reuse via the pure
CastingWorkspaceSessionBinding policy with isolated-candidate lifecycle
regressions (same-campaign retention incl. undo; A→B cross-bind refused
and disclosed; unresolved identity refuses); G1 the ACTUAL Done control
and focused-enhancement toggles derived from the focused record's own
caster/ability (grep-verified in the pushed view this time — the
earlier F4 claim was partly never applied and is superseded); G2
remembered-recipient restore or deliberate pick-to-return, group-aware
SetFocusedTargeting (WithTargeting clone, never direct-target cloning
on group records), required-coverage authoring, and surfaced refusals;
G4 the interaction scenario drives EVERY action through real
active+interactable buttons (source/caster/recipient/state/Add/edit/
retarget/undo/done/save), asserts one-record growth + distinct
identities + exact fields + unchanged siblings per Add, and exercises a
deliberately refused Add; G5 DocumentIntentSignature is the canonical
serialized profile (modifiers, policies, routines included) with
same-session retention and fresh-session persisted round trip separated
plus single-field negative cases. A same-frame stale-button shadowing
bug in the invoke helper (DestroyChildren deactivates + defers) was
found and fixed (34a18b7).

**Run casting-ws-gseries-081000: FULL PASS** — all nine assertions:
cast1/2/3Exact=True through controls, refusedAddClean=True,
undoIntentRestored=True, doneClearedFocus=True, preserved=True,
dirty=False, display path non-black (0.73/0.14, changed 0.95).
Transaction restored+verified. Protocol 223/223.

OPEN (honest): supervised MANUAL usability session (the mission's
checklist: real mouse, names/readiness/labels, group↔direct, unsaved
close/reopen, scroll/long-labels/states/sounds) — prepared below;
physical-input seam acceptance; §5 native aesthetic pass; live group
authoring; exact rods/native adapters. Native casting remains disabled.

NEXT (exact action): supervised manual session with Howie per the
package in docs/MANUAL-USABILITY-HANDOFF.md (suspend auto input; record
identity; restore). Then the §5 aesthetic pass and the launcher
physical-input seam qualification.

## Follow-up review answered; full display-path acceptance PASS — 2026-09-21

The second review (F1-F7 + C1 at head 196389f) is answered at `0a7229e`
(pushed, verified): F1 Add self-resolves the exact ability+spellbook and
re-resolves on (source,caster) change (a UI draft with no ability
refuses with an explanation; deliberately unresolved intent keeps an
explicit ability for the plan to disclose); F2 the live scenario drives
REAL view buttons with no private draft fields and verifies browse,
Undo, and reopen by FULL authored-intent signatures with a clean dirty
state; F3 SetDraftTargeting is a validate-first coherent shape command
wired to every targeting callback; F4 Done-navigation, focus cleanup,
focused enhancement editing; F5 per-source labels and no silent
truncation; F6 the session survives close/reopen with dirty tracking
(teardown is the logged discard); F7 clicks revalidate foreground +
cursor-inside-client immediately before injection and programmatic UMM
close is a terminal state for every pending Escape; C1 Apply normalizes
its scope once.

**Run casting-ws-controls-051500: FULL PASS on the DISPLAY path** (the
owner's RDP session was connected — rdp-tcp#1 Active): all nine
workspace assertions green — non-black open frame (mean 0.73 vs control
0.14, changedFraction 0.95), all three castings authored through real
button invocations, undoIntentRestored=True, reopen preserved the full
signature with dirty=False. Transaction restored and verified. Frames
curated in docs/evidence.

Gates: source 42/42; protocol **221/221**; harness 27/27; WhatIf 5/5;
launcher -File 4/4; fixture 3/3; rollback 4/4; publisher 3/3.

OPEN (honest): launcher physical-input seam acceptance (real pointer/
keyboard delivery under supervision); §5 native aesthetic pass
(parchment donors, states, scroll, tooltips, sound); caster GUID
display names (UnitSnapshot plumbing); exact rods/native adapters;
native casting remains disabled. Group-mode and large-catalog UI cases
run at the protocol layer; live group authoring follows with the
aesthetic pass.

NEXT (exact action): push this checkpoint and update PR #2; then the
UnitSnapshot display-name plumbing, the §5 native-presentation pass,
and a supervised physical-input acceptance run with the owner connected.

## Review response: all findings addressed; live authoring PASSES — 2026-09-21

Branch `codex/kingmaker-buff-planner-casting-first` (draft PR #2,
https://github.com/howardreith/KingmakerBuffPlanner/pull/2). The
independent source review (request-changes, R1-R6 + C1-C4) has been
answered commit-by-commit; every slice pushed through the guarded
helper and verified remotely:

- **R2/R4/R5/R6/C3** — `2e7b95f`: lifecycle (hotkey toggle, Escape,
  disposal on ReleasePlayerUi/ReleaseAll, exactly-once
  BuffPlannerInputLease, documented unsaved-edit policy); exception-safe
  camera capture (per-change finally restoration, previous-active
  restore, screen-camera-only set, self-verifying verdict,
  failure-injection seam); launcher input ownership (no global Alt,
  verified-foreground-before-inject, tracked-keys finally release,
  programmatic-open marker suppressing the pending chord); control
  capture before EITHER opening route; immutable open-frame record
  bound to the nonblack assertion.
- **R3/C1/C2** — `40f814e`: selected-routine-scoped compile through
  BuildView/Present/Accept/Apply with the one-pass gate moved to the
  labeled whole-document ForecastOnePass; capability and first-source
  from discovered options (alias-instance contract); bounded 2s refresh
  at the production input boundary. Regressions:
  casting-workspace-routine-scoped-apply (the one-charge/two-routine
  counterexample), casting-workspace-fresh-buff-capability.
- **R1/C4** — `f637406`: complete draft editor (buff catalogue, capable
  caster selection, direct/group targeting with recipient+origin
  pickers, per-casting enhancement toggles, draft/ready state, Add
  Casting), routine tabs, focused-casting retarget via single-field
  PlannedCasting clones, Save/Reload; the live scenario drives the real
  session through browse→3 casts→edit→Undo→re-edit→Save→close→reopen
  with camera captures between milestones.
  **Run casting-ws-author-033000: workspace-interaction-sequence PASS
  (browseNoMutation=True; capableCasters=3; cast1/2/3=applied;
  edit=applied; undo=True; saved=True; intent=[cast-1,cast-2,cast-3])
  and workspace-reopen-preserves-intent PASS (exact IDs/order,
  loadStatus=Loaded).** Frames curated in docs/evidence. The run's
  visual gates failed ONLY on the backbuffer path (the classified
  disconnected-session display defect; camera lane captured all
  milestones).

Gates at this checkpoint: source 42/42; protocol **220/220**; harness
27/27; WhatIf 5/5; launcher -File 4/4; fixture 3/3; rollback 4/4;
publisher 3/3.

OPEN (honest): display-path visual acceptance needs a connected owner
session (RDP active during one run); caster GUID display names (names
exist in discovery — UnitSnapshot plumbing); native aesthetic pass
(review §5: parchment/typography/states/scroll/tooltips/sound);
physical-input acceptance of the launcher seam; exact rods/native
adapters unchanged. Native submission stays disabled.

NEXT (exact action): fix UnitSnapshot display-name plumbing (producer
side in KingmakerPartySnapshotBuilder); re-run the guarded scenario for
a named-caster frame; then the §5 native-presentation pass (borrowed
parchment donors, state sets, Escape order, tooltips) and the
physical-input acceptance run with the owner connected. Push each
checkpoint; keep PR #2 draft.

## Workspace rendered; publication live — 2026-09-21

Branch `codex/kingmaker-buff-planner-casting-first`, pushed through the
guarded push helper, verified remotely. Draft PR #2
https://github.com/howardreith/KingmakerBuffPlanner/pull/2 (base `main`,
head `codex/kingmaker-buff-planner-casting-first`; reviewed baseline
`c182061` recorded in the PR body and
`docs/CASTING-FIRST-REVIEW-INDEX.md`).

**THE WORKSPACE NOW RENDERS IN-GAME** (run `casting-ws-layer-030000`,
camera-render capture, mean luma 0.73 vs control 0.41): parchment panel,
header, caster lane (3 casters + Focus), casting cards, inspector,
footer Undo/Accept Plan/Review & Apply. Three stacked root causes were
found and fixed, each proven by a dedicated run:

1. `dbbbf86` — SetAnchors argument order: the view passed
   (minX,maxX,minY,maxY) against the factory's (minX,minY,maxX,maxY);
   node-dump rects matched the wrong-geometry predictions exactly
   (98x670, -420x179, 0x-1049). All 15 non-symmetric calls fixed.
2. `8e12b2b` — the nested canvas under StaticCanvas rendered in NO path;
   the root now follows the game's own service-window pattern (top-level
   ScreenSpaceCamera canvas bound to the native UI camera, order 32000).
3. `72209fc` — layer culling: factory GameObjects defaulted to layer 0
   while the native UI camera culls to the native canvas layer (5); the
   layer is copied at build and re-propagated after every rebuild
   (rootLayer=5 in evidence). The legacy screen escaped this only
   because ScreenSpaceOverlay bypasses camera culling.

Also this session: owner-authorized GitHub publication executed
(branch + curated evidence + review index + draft PR, verified by
ls-remote and content fetch); the launcher ShouldProcess decision guard
made fail-closed for real runs under `-File` (WhatIf honored only as a
deterministically negative outcome; four-layer regression including a
REAL-run zero-mutation refusal sentinel); the matched
control/open/closed comparison scenario shipped and gated
(workspace-vs-control-visible-change); a camera-render capture lane was
added that bypasses the dead display path (produces content even in
black sessions — it delivered the first rendered workspace frame).

KNOWN OPEN DEFECTS (visible in the rendered frame): caster names show
unit GUIDs — display names are not reaching
WorkspaceCasterRow.DisplayName (the enhancement probe logs names like
Hedwirg/Linzi, so the data exists in the discovery model; the workspace
inputs/row model plumbing must map it); lane/card label truncation.
Display-path (backbuffer) acceptance still needs a connected owner
session; the camera lane covers visual evidence meanwhile.

NEXT (exact action): fix the GUID-names defect (map display names from
the party snapshot into the workspace view model + regression), fix
label truncation, re-run the guarded scenario to a frame with named
casters, then begin the interaction checklist (browse without mutation,
mixed-caster cards, edit+Undo, group coverage, resource drill-down,
save/reopen) — all with dispatch still disabled. Push each step through
the guarded helper and keep PR #2 updated.

## Workspace qualification honesty chain — 2026-09-20

Branch `codex/kingmaker-buff-planner-casting-first`, HEAD `41893a2`
(clean). Gates: source 42/42; protocol 217/217; harness 27/27;
deployment WhatIf 5/5; launcher -File WhatIf 3/3 (new); fixture 3/3;
rollback 4/4; publisher 3/3.

The session chased the workspace visual claim through five guarded
runs; every earlier "workspace opened" claim is now corrected:

1. **casting-ws-visual-183000** — PASS but the frame showed the LEGACY
   catalog screen: the workspace dev-selection enable was dead code
   (inside the non-live UI-smoke gate, always false for
   live-workspace-qual). Captured PROVEN dual-path non-black machinery
   though (blackFraction=0.0285, focused=False, zero retries, both
   paths bit-identical) — the original 30 KB black png was a
   capture-timing artifact, not an unfocus limitation.
2. **casting-ws-root-190500** — honest FAIL after the enable's first
   relocation STILL sat after Update()'s live-UI return; the tightened
   phase gate refused the legacy screen (phase=1 timeout) and the
   transaction restored verified.
3. **7c76090** placed the enable at the top of Update();
   **casting-ws-root-200200** then opened the REAL workspace through
   the production hotkey path (log: `casting-first workspace
   opened;dispatch=native-submission-disabled`), all assertions PASS
   (workspaceRoot=active, legacyScreen=closed, non-black 0.3907) — but
   the frame showed the plain game HUD: the workspace drew nothing
   (hierarchy existed; two-update settle suspected too early).
4. **casting-ws-present-204500** — FAIL by design: presentation
   evidence captured (hierarchy FULLY presented: active, 1920x1200,
   canvas enabled, alpha 1.00, 19 renderable texts) while the frame was
   100% black across all 31 attempts.
5. **casting-ws-bisect-211500** — FAIL by design, DECISIVE bisection:
   open-workspace frame black AND closed-workspace frame black in the
   same session → the workspace does NOT blacken the screen; this is
   the documented intermittent black-presentation defect (menuinput-4/5
   precedent), suspected RDP-console related. See AUTONOMOUS-BLOCKERS.

Also repaired this session: `powershell.exe -File` +
`$PSCmdlet.ShouldProcess` NullReferenceException crashed the runtime
launcher after its read-only preflights (minimal repro: fails under
-File, works under -Command); fixed with a narrow catch + WhatIf
contract fallback (f3d6b48) and a three-layer regression test wired
into Test-SourceOnly (9808102). Eight other guarded scripts carry the
same latent defect (recorded, not swept).

NEXT (exact action): the connected-session test has NOT yet been
achieved — casting-ws-connected-224000 started with the owner's RDP
session Active but it disconnected mid-run; all three captures (control,
open, closed) were fully black, consistent with the disconnection
hypothesis. The matched-comparison scenario (control frame before the
production open, changed-fraction gate, return capture) is implemented,
gated, and green through source gates at `a2d4b30`. REQUIRED FROM OWNER:
reconnect the RDP session and STAY connected through one ~8-minute
guarded run while watching the game window; then report what was seen —
game rendered (capture-pipeline repair lane) vs game also black
(environment/display lane, stop launching). While connected, the owner's
own screenshot of the game window is separately labeled visual evidence
if automated captures stay broken. The interaction checklist (browse,
mixed-caster cards, edit+Undo, groups, resources, save/reopen) follows
once one presented workspace frame exists.

## Campaign-load repair — 2026-09-20

- **THE STANDING BLOCKER IS REPAIRED AND REPRODUCED.** The campaign load
  now completes through the game's own observed native chain in
  consecutive fresh guarded runs (liveui-chain-1 and liveui-chain-2, both
  full-user 15-mod profile, exact WORKING save
  Manual_305_KBP_AUTOMATION_WORKING, gameId df33d1ff...): exact Load Game
  button resolved by hierarchy/sibling/components/TMP-label/wired
  listeners; Button.onClick.Invoke() with a Harmony-observed
  MainMenuButtons.OnButtonLoadGame count of exactly one; ListOfSaves
  catalog captured (84 descriptors); exact working+baseline descriptors
  by identity fields; exact SaveSlot -> owning SaveLoadWindow ->
  ListOfSaves by object references; SaveSlot.OnButtonSaveLoad on the
  exact slot; then OBSERVED: SaveLoadWindow.HandleHardcodeMainMenuSaveLoad
  (same descriptor), MainMenu.LoadGame (same descriptor), SaveManager
  after-load callback, stable fingerprint (exact gameId, party 3). The
  read-only saver suppressed the single header update and commit (zero
  disk writes); native saver restored. Evidence:
  runtime-evidence/liveui-chain-1/ and liveui-chain-2/
  (saveload-chain-events.json + output_log [KBP-SAVE-LOAD] lines).
- Root causes fixed this session (each with reproduced evidence):
  1. The old loader waited for an ALREADY-ACTIVE SaveLoadWindow
     (FindObjectOfType excludes inactive) and invoked methods by name
     without observing downstream effects — replaced by the
     Gunslinger-working-save-smoke-derived observed chain
     (src/RuntimeTesting/LiveCampaignSaveLoader.cs,
     MainMenuLoadContracts.cs, GuardedReadOnlySaver.cs; commit c114940).
  2. PowerShell 5.1's array-subexpression binder crashes on
     List[object] ("Argument types do not match"), killing every harness
     wait loop before the first observation persisted; reproduced
     standalone; fixed with ToArray() (commit 0c4d81b).
  3. Update-count budgets expire in seconds because an unfocused Unity
     player with runInBackground spins far above 60 fps (observed
     ~800-1750 dispatches/s); all budgets are now wall-clock
     (commits 524107e, 1449b1f).
  4. Mono serves persisted image sidecars (Assembly.dll.<pid>.cache) for
     some mods; identity hashing now resolves the canonical assembly
     (LoadedAssemblyIdentity; the full-user profile pins were verified
     byte-exact on disk — no drift).
  5. Physical-input delivery failures (Windows foreground lock while the
     owner interacts) aborted runs; deliveries now retry and record
     errors instead.
- Render diagnostics: launchdiag-1 proved the automated session presents
  a rendered main menu (identical ReadPixels + engine captures, 1.67 MB,
  blackFraction 0.05, UMM overlay visible over it). menuinput-4/5 later
  captured fully black frames from BOTH capture paths while the game was
  foregrounded — the black presentation is INTERMITTENT and correlates
  with focus; NOT classified (display-path vs presentation), evidence
  retained. The owner's remote black screen therefore has in-game
  capture proof of both states.
- Physical click at the exact Load Game button could not be delivered
  while the owner was actively using the machine (foreground lock,
  deliveryFailed ack with error) — the programmatic onClick route is the
  qualified automation path; physical input remains timing-dependent.
- liveui-chain-3 was REFUSED correctly: an owner-controlled Kingmaker
  (PID 21420) was running. Transaction liveui-chain-2 was restored and
  verified after that session closed (restore refused while the owner
  played, per the running-game guard; no conflict bypassed). Note on the
  staged build during that window: it is the full planner product —
  normal play exercises the production HUD/hotkey surface — while the
  runtime-test scenario host additionally requires the
  -kbpRuntimeTestRequest flag. Normal-play safety and test-host
  activation are separate questions; neither was claimed from the other.
- AS OF HANDOFF: no active transactions, no deployment locks, tree
  clean at the final records commit, local-runtime package built from
  the final source HEAD. The runtime lane is ready for the next
  full live-ui-bootstrap pass through the post-load workspace phases
  (Pro-directed 2026-09-20): open/inspect the real workspace, authorized
  authoring + isolated save/reopen checks, entry points/input
  ownership/close-reopen lifecycle, fresh workspace screenshots, native
  spell submission DISABLED, then close out and verify restoration.
  Campaign loading is REPAIRED for the tested configuration (two
  consecutive fresh-run receipts); do not reopen the loader
  investigation unless the load itself regresses. Keep programmatic and
  physical input evidence distinct; never fight the owner's foreground.
- Gates: source 42/42; protocol 217/217; harness 27/27; package 4/4;
  WhatIf 5/5; fixture 3/3; rollback 4/4; publisher 3/3. HEAD `1449b1f`
  on codex/kingmaker-buff-planner-casting-first (commits d1e824b,
  83a7cbb, 79d314b, fe3d8f6, 0c4d81b, 524107e, 1449b1f + records).
- NEXT (development continuation thread): run the full
  live-ui-bootstrap pass (post-load workspace phases now have wall-clock
  budgets and retried input delivery) and continue the casting-first
  plan within its current authorization; treat the intermittent black
  presentation as its own diagnosis (compare focused vs unfocused
  captures in one run).

## Casting-first migration checkpoint 14 (campaign UI qualification attempts) — 2026-09-20

- ROOT CAUSE CHAIN IDENTIFIED across multiple guarded runs:
  1. Runtime protocol rejected full-user profile → FIXED (accepted, 15 mods)
  2. Human-reproduction (3 mods) insufficient → Player.PostLoad()
     InvalidOperationException (missing blueprint references)
  3. SaveManager.LoadZipSave returns null → FIXED (read m_SavedGames list)
  4. UI button interaction doesn't drive Kingmaker's UI state machine →
     FIXED (HandleHardcodeMainMenuSaveLoad)
  5. 30-second timeout too short for 15-mod campaign → FIXED (3 minutes)
  6. CURRENT: HandleHardcodeMainMenuSaveLoad invokes, scene transition
     starts (lightmaps), mods register blueprints, but the campaign load
     does not complete and neither the callback nor Player-state fallback
     fires within 3 minutes. CAUSE UNRESOLVED.
- All source gates green (210/210 protocol; 42/42 source; full suite).
  Package 9076e0a. Runs: casting-fulluser-final-1/2/3 (all restored
  verified). Evidence: runtime-evidence/casting-fulluser-final-*/.
- NEXT: determine whether HandleHardcodeMainMenuSaveLoad visually triggers
  the loading process (owner observation needed) or silently fails; try
  the game's own Continue button path; or accept manual-load as the
  campaign-context route for workspace qualification.

## Casting-first migration checkpoint 13 (bootstrap launched; campaign load engine-stalled) — 2026-09-20

- All three fixture approvals applied and verified. Protocol fix
  (stale 4→3 mod count for human-reproduction) committed `1099601`;
  all 210 tests pass.
- **RUN casting-first-bootstrap-3** (game launched, ran, exited, restored
  verified; Mods 1033 files): the scenario passed entry-point/version/
  commit assertions, found the exact WORKING save
  (Manual_305_KBP_AUTOMATION_WORKING, gameId df33d1ff..., area
  JamandisMansion), invoked SaveSlot.OnButtonSaveLoad, and the area
  began unloading — then the ENGINE stalled during the area transition.
  WeatherSystemBehaviour.Update() NRE spam every frame; the
  SaveManager.AddCallbackAfterLoad callback never fired; the loader
  timed out at campaign-load-completion (~120s of the 600s budget).
  Result: FAIL/unhandled-exception. Evidence:
  runtime-evidence/casting-first-bootstrap-3/runtime-result.json.
- **This is the same class of engine-level area-transition failure seen
  with the START-PROLOGUE seed in earlier sessions** — not a mod,
  fixture, or protocol defect. The automated campaign load cannot
  complete with this save in this game version (2.1.7b) and mod set.
- **OWNER ACTION NEEDED:** verify whether the WORKING save loads
  manually from Kingmaker's main menu (Load Game → KBP_AUTOMATION_WORKING).
  If it loads manually, the issue is automated-load timing. If it does
  not, a new WORKING save is needed at a later campaign point. Either
  way, the campaign-context UI qualification lane is blocked on this.
- All source work remains green (protocol 210/210; full gate).
  Commit: `1099601`. Package: `fea8b690...`. Restoration verified.

## Casting-first migration checkpoint 12 (all approvals applied; ready to launch) — 2026-09-20

- Owner approved CallOfTheWild (988e6130...) and CheatMenu (7960517c...)
  rebindings; applied with exact verification. A correction was noted:
  the earlier approval request showed abbreviated "from" hashes whose
  full values differed from the file; the approved "to" values were
  always correct and the actual "from" values were used for the diff.
- C# 7.3 method-group conversion fixed in the legacy-execution guard
  (BuffPlannerUiRoot lambdas); commit `393d514`.
- CONSOLIDATED PREFLIGHT: all three mod identities PASS; save pair
  present (304/305); no unresolved transactions; no deployment lock;
  Steam safety PASS; local runtime package efe309b0... built from clean
  HEAD; WhatIf preflight PASS (no mutation).
- **REMAINING BLOCKER: Kingmaker process PID 5680 (user-owned).** The
  guarded lane will launch the moment the game is closed. No game was
  launched this checkpoint; the owner should close Kingmaker and notify
  (or simply close it before the next continuation).
- NEXT: on game-closed, run the guarded live-ui-bootstrap → campaign
  context → CastingWorkspaceScreenView qualification with screenshots
  and restoration receipts.

## Casting-first migration checkpoint 11 (owner approval applied; blocker on remaining mods) — 2026-09-20

- Owner approval RECORDED and APPLIED for BagOfTricks (three fields in
  compatibility/profiles/human-reproduction.json; per-file inventory at
  runtime-state/fixture-inventories/fixture-inventory-BagOfTricks-c4487d11b264.json;
  record at planning/BAGOFTRICKS-APPROVED-BASELINE-RECORD.md).
  BagOfTricks now PASSES the fixture guard.
- GATES ALL GREEN on the changed source (source 42/42; protocol 210/210;
  harness 27/27; package 4/4; WhatIf 5/5; fixture-inventory 3/3;
  rollback 4/4; publisher 3/3).
- **NEW BLOCKER:** CallOfTheWild and CheatMenu also fail the guard —
  their profile aggregates (Aug 23) are stale relative to the current
  live directories. Per-file comparison proves live == September
  transaction receipts exactly (zero differences). Approval request
  prepared at planning/CALLOFTHEWILD-CHEATMENU-APPROVAL-REQUEST.md with
  exact values, inventories, and a proposed owner statement.
- Campaign UI qualification is BLOCKED on this decision; no guarded run
  was attempted (the guard would refuse before deployment).

## Casting-first migration checkpoint 10 (decision packet exposed) — 2026-09-20

- HEAD `6721d8e` on `e46f18e` (gates all green, unchanged counts). The
  exact-rod conclusion was narrowed to the inspected evidence with the
  serialization path examined (three identity tiers separated; per-item
  persisted Fact[] recorded as the live-proof candidate). The
  BagOfTricks fixture approval packet is reproduced INLINE in the
  handoff with a PROPOSED — NOT YET GRANTED owner statement; bootstrap
  and the campaign-UI lane await that explicit decision.
- NEXT: on owner approval of the exact packet identities, apply only the
  three approved binding changes, then bootstrap → donor inventory →
  observed workspace interaction and screenshots.

## Casting-first migration checkpoint 9 (runtime-qualification continuation) — 2026-09-20

- Bundle validated mechanically (git-am applicability to c182061
  reproduces tree 119c24f1; SHA256SUMS added; LOCAL, not delivered).
- Both narrow checks PROVEN with tests (no defects): sibling authored
  intent preserved vs legitimate derived changes with byte-exact Undo;
  disabled dispatch is attempt-only with in-flight release, plus a NEW
  production guard refusing every legacy quick-execution entry while the
  workspace is selected.
- Prospective seal hardening added (per-file inventory functions + 3/3
  gate tests; aggregate-only mismatches stay blocked); owner-approval
  packet prepared (NOT granted — bootstrap still blocked); A06 inspected:
  ItemEntity has no per-instance identity member (see
  planning/EXACT-ROD-IDENTITY-INSPECTION.md) — pooled stays unresolved.
- Gates: source 42/42; protocol 210/210; harness 27/27; package 4/4;
  WhatIf 5/5; fixture-inventory 3/3; rollback 4/4; publisher 3/3
  (`artifacts/casting-first-checkpoint9-gate.log`). Records corrected to
  remove the inferential "mutable file" claim about the August delta.
- NEXT: owner decides the BagOfTricks baseline (packet ready); then
  bootstrap → donor inventory → workspace visual/input qualification.

## Casting-first migration Phase 4 checkpoint 8 (workspace) — 2026-09-19

- FIXTURE DIAGNOSED, NOT RESEALED: the live BagOfTricks directory is
  byte-identical to BOTH September guarded-install transaction manifests
  (all 40 sha256 match); the stale Aug-23 aggregate is off by exactly one
  unrecoverable 182-byte mutable file. live-ui-bootstrap stays blocked;
  prerequisite = owner rebind authority or the recommended additive
  per-file-manifest comparator evidence. No guard weakened, nothing
  resealed.
- THE CONNECTED WORKSPACE: CastingWorkspaceSession (browse-never-mutates,
  draft-for-next-only, explicit commands + Undo, shared-plan read models,
  candidate persistence with corruption blocking) + review/Apply
  integration (Present/Accept/Apply through gate + coordinator + an
  explicitly DISABLED dispatch boundary that records identity and never
  claims gameplay) + the Unity CastingWorkspaceScreenView on the
  factory/theme seams behind CastingWorkspaceDevSelection (default off),
  consuming the same discovery data as the legacy screen. View is
  source-integrated and compiled; NOT visually qualified.
- Gates: source 42/42; protocol 208/208 (3 workspace integration tests);
  harness 27/27; package 4/4; WhatIf 5/5; rollback 4/4; publisher 3/3
  (`artifacts/casting-first-checkpoint8-gate.log`). Bundle regenerated at
  `artifacts/review-bundles/casting-first-migration/` (local only).
- NEXT: additive per-file-manifest comparator evidence; owner-side seal
  resolution then live-ui-bootstrap donors + workspace screenshots;
  runtime-qualify the view; A06 exact-rod inspection.

## Casting-first migration Phase 2 checkpoint 7 — 2026-09-19

- A13 isolated boundary implemented: CastingPlanMigrationService
  (exact-original per-boundary archive, candidate-only writes,
  reopen-validate, torn-candidate CandidateUnusable, newer-candidate
  refusal, legacy never modified); protocol 205/205; full gate green
  (`artifacts/casting-first-checkpoint7-gate.log`).
- Live lanes advanced with honest results: local-runtime package built
  from `29f1ac8` (zip 2b3138e4...); ui-native-contract-probe ran and
  restored verified but the scenario needs campaign UI (main menu has
  none) — failed honestly; live-ui-bootstrap REFUSED pre-deployment by
  the BagOfTricks fixture manifest mismatch (DLL/Info hashes match;
  41/1,805,907 recorded vs 40/1,805,725 installed — one mutable file
  removed). Blocked op: guarded fixture-manifest refresh, then re-run
  bootstrap. Evidence:
  runtime-evidence/casting-first-nativeprobe-1.
- NEXT: fixture refresh + live-ui-bootstrap donor inventory; connected
  parchment workspace wiring CastingReviewCoordinator (Phase 4 start);
  exact-rod identity inspection (A06).

## Casting-first migration Phase 2 checkpoint 6 (C1-C5) — 2026-09-19

- Continuation mission reconciled at `061968d4...` (clean; five
  checkpoints preserved). C1-C5 verified against the real services; one
  defect and one gap repaired, three missing layers implemented:
  C1 no-registry modifiers now BLOCK (was silent unmodified execution);
  C2 Disabled state + gate routine scoping (saved drafts block Ordinary
  Apply; disabled/out-of-scope never block); C3 CastingReviewCoordinator
  (presented/accepted/submitted signatures; material change refuses;
  refresh keeps acceptance); C4 modifier UsageDemands merged into the
  atomic cost vector; C5 structural effect projection in one-pass
  forecasts (AlreadySatisfied; conservative equivalence).
- Gates: source 42/42; protocol 204/204; harness 27/27; package 4/4;
  WhatIf 5/5; rollback 4/4; publisher 3/3
  (`artifacts/casting-first-checkpoint6-gate.log`). Reviewable bundle:
  `artifacts/review-bundles/casting-first-checkpoints-1-6/` (patches +
  manifest; checkpoint 6 commit applies on top).
- NEXT: guarded native donor investigation (Build-Local +
  ui-native-contract-probe / live-ui-bootstrap with restore), then the
  connected parchment workspace; isolated A13 persistence tests alongside.

## Casting-first migration Phase 2 checkpoint 5 — 2026-09-19

- A05 targeting-modifier resolution implemented for the new model
  (`Planning/CastingTargetingModifiers.cs` + compiler wiring): enabled
  modifiers transform recipient eligibility on the proven option only;
  unavailable modifiers block as repairable intent; unknown ids block
  against a provided registry; no-registry stays unvalidated
  diagnostics; disabled selections are neutral; application is pure.
- Gates: source 42/42; protocol 199/199; harness 27/27; package 4/4;
  WhatIf 5/5; rollback 4/4; publisher 3/3. Log:
  `artifacts/casting-first-checkpoint5-gate.log`. Domain layer only.
- Live lane verified actionable read-only (no game process; automation
  trio present; clean tree). NEXT (critical path): Phase 3 donor
  inventory — build the local-runtime package
  (`scripts/Build-Local.ps1`), run the guarded
  `ui-native-contract-probe` / `live-ui-bootstrap` lanes, restore after
  each run. Remaining Phase 2: A06 exact-source identity (blocked on a
  durable contract), A14 effect-satisfaction modeling.

## Casting-first migration Phase 2 checkpoint 4 — 2026-09-19

- Forecast views and the apply gate implemented: routine-scoped and
  one-pass `CastingForecastService` previews (fresh ledger per routine
  view, one shared carried-forward ledger for the sequence, assumption
  labels, A09) plus the stateless `CastingExecutionGate` (ordinary
  Apply refuses blocked work; explicit Ready-Casts-Only discloses every
  omission with reasons and preserves the plan; evaluation never
  authorizes by repetition, A10/A11).
- Gates: source 42/42; protocol 198/198; harness 27/27; package 4/4;
  WhatIf 5/5; rollback 4/4; publisher 3/3. Log:
  `artifacts/casting-first-checkpoint4-gate.log`. Domain layer only.
- NEXT: Phase 3 native donor inventory through the guarded harness
  (automation fixture now present), or A05 modifier-resolution modeling
  while live access is unavailable.

## Casting-first migration Phase 2 checkpoint 3 — 2026-09-19

- Shared atomic budget reservation implemented per charter 5.1:
  `Planning/CastingBudget.cs` (native pools incl. linked prepared-token
  pairs, enhancement usage reservoirs with minimum-of-balances shared
  semantics, materials) wired into `ExplicitCastingCompiler` as a
  persisted-order budget pass. Each Ready casting's complete cost vector
  reserves atomically — a deficit blocks the casting and reserves
  nothing anywhere; `ResolvedCasting.Cost` and per-pool
  `CastingBudgetLine`s (available/requested/allocated/unmet/forecast
  with responsible casting traces) are the authoritative read model.
- Gates: source 42/42; protocol 195/195 (A07 + A08 new); harness 27/27;
  package 4/4; WhatIf 5/5; rollback 4/4; publisher 3/3. Log:
  `artifacts/casting-first-checkpoint3-gate.log`. Domain-layer evidence
  only — live native spending (A07 live lane) remains unclaimed.
- NEXT: exact-source identity plumbing, then routine forecast views
  (A09), then the Phase 3 native donor inventory (now unblocked by the
  automation fixture).

## Casting-first migration Phase 2 checkpoint 2 — 2026-09-19

- Schema-5 → schema-6 import converter implemented
  (`Persistence/CastingPlanImporter.cs`) per charter §7.2: pinned
  single-target children split per recipient with order/enhancements/
  policy preserved; automatic children become review drafts (no silent
  best-caster pinning); group children import as one draft with
  coverage preserved and origin/count pending review; deterministic
  provenance-derived IDs make re-import idempotent; target-less
  children stay visible as unresolved mappings; full import report
  with per-child disposition. No live profile was read or converted.
- Gates: source 42/42; protocol 193/193 (5 new import tests);
  harness 27/27; package 4/4; WhatIf 5/5; rollback 4/4; publisher
  3/3. Log: `artifacts/casting-first-checkpoint2-gate.log`.
- NEXT: shared atomic budget reservation in ExplicitCastingCompiler
  (A07/A08), then exact-source identity plumbing.

## Casting-first migration Phase 1 checkpoint 1 — 2026-09-19 (commit `4398588`)

- The adopted casting-first charter is being implemented on branch
  `codex/kingmaker-buff-planner-casting-first`, created clean from the
  charter-reviewed commit `c182061354e9e761c09648ca779ab334588ba379`
  (rc3, legacy schema 5 unchanged). Baseline receipt and per-checkpoint
  detail: `planning/CASTING-FIRST-MIGRATION-STATUS.md`.
- Implemented: canonical `PlannedCasting`/`CastingPlanDocument` domain
  model (one Ready record = one invocation; explicit caster/origin/
  coverage/enhancement/provenance); `CastingAuthoringService` (single
  mutation authority, disclosed edit scopes, bounded Undo);
  `ExplicitCastingCompiler` (one resolved casting per authored casting,
  distinct readiness reasons, capability separate from readiness, honest
  group coverage gaps — never auto-expansion); schema-6 candidate
  persistence with Absent/Loaded/Recovered/UnsupportedSchema/Corrupt
  load states and no default-overwrite.
- Gates: source 42/42; protocol 188/188 (7 new: charter A01–A4 plus
  authoring-scope/undo, blocked-readiness, round-trip/load-states);
  harness 27/27; package 4/4; WhatIf 5/5; rollback 4/4; publisher 3/3.
  Log: `artifacts/casting-first-checkpoint1-gate.log`. All A01–A04
  evidence is deterministic domain-layer only — no runtime/visual claim.
- IMPORTANT fixture change: the authorized automation save trio now
  exists (SEED 303 / BASELINE 304 / WORKING 305, read-only verified);
  the old "no automation pair" live-lane blocker is resolved for future
  phases. Nothing was launched or staged in this checkpoint.
- NEXT: Phase 2 — shared atomic budget reservation in the compiler,
  then the schema-5→6 import converter with charter conversion rules
  and import-report tests.

## RC3 review-correction pass — 2026-09-19 (supersedes the RC2 section below for current work)

- A source review of published rc2 (at `a30c07e`/`c56befa`) confirmed four
  findings; all are repaired on this branch (fix commit below):
  - C1 spell-scoped coverage: `EnhancementBudgetModel.SpellCoverage`
    matches both canonical and aggregate source identities and
    distinguishes requested/funded/skipped targets, communal casts, and
    allocated charges; the routine aggregate is never labeled "this
    spell".
  - C2 assignment-scoped chooser: `SelectedCastingViewModel.Create` now
    reads THAT child's selections/policies and per-assignment provider
    applicability; unavailable selected choices stay individually
    removable in both scopes (`SetEnhancement` gained the same
    is-removal bypass `SetAssignmentEnhancement` already had); policy
    captions are honest (REQUIRED/OPTIONAL vs REQUIRED (targeting)).
  - C3 bounded detail: sticky summary and row notes are length-bounded
    (300/160) with full pool detail preserved on tooltips and in
    Assignments & Resources.
  - C4 real rollback: new guarded `scripts/Restore-InstallLocal.ps1`
    (reads `runtime-state/installations/<id>/install.json`, preserves
    post-install UserSettings edits, archives evidence, refuses
    non-Installed records; isolated-state tests 4/4 wired into
    Test-SourceOnly). The rc2 handoff's `Restore-Local.ps1 -RunId ...`
    command was for the wrong transaction type — corrected everywhere.
  - Review observations: theme late-donor retry triggers at chooser
    rebuild boundaries; spellbook button borrows complete native button
    states fail-soft; plan-summary rect no longer overlaps Edit
    Assignments.
- Gates: source 42/42; protocol 181/181 (three new regressions);
  harness 27/27; Restore-InstallLocal 4/4; package 4/4; WhatIf 5/5;
  publisher gate 3/3; deterministic Release build 2/2.
- PUBLISHED: https://github.com/howardreith/KingmakerBuffPlanner/releases/tag/v0.1.1-rc3
  (non-draft prerelease; v0.0.19 keeps Latest; main unmerged). Asset
  `KingmakerBuffPlanner-0.1.1-rc3.zip` SHA-256
  `52ab5d94b77c559ce90bb9742cc4efd05355b6d3ea50ebcbaa42bc5b35bf0313`,
  re-downloaded and verified (hash + package validation 4/4). Tag
  `v0.1.1-rc3` == source `2aad5d4` == pushed branch HEAD. PR #1 updated.
- Local machine: the guarded transaction `rc3-published-install-1`
  installed the EXACT published rc3 package over the prior local rc2
  (which had replaced 0.0.19; the 0.0.19 original remains archived
  under `runtime-backups/install-rc2fix-liveui-3`). Rollback chain:
  `Restore-InstallLocal.ps1 -InstallId rc3-published-install-1` (back to
  rc2), then `-InstallId rc2fix-liveui-3` (back to 0.0.19) — each
  preserves post-install UserSettings profiles. No other mods/saves
  touched; no locks or unresolved transactions.
- NEXT: owner runs the rc3 five-minute check and reports. Stable
  promotion remains merge review + default-branch publish.

## RC2 product recovery — 2026-09-19 (LATEST; supersedes the rc1 closeout below)

- Owner acceptance of v0.1.1-rc1 FAILED (flat panels, no spellbook button,
  chooser showed per-spell `3 uses` and numeric metamagic labels such as
  `268435456 Spell`). The live Mods folder holds 0.0.19 — the owner rolled
  back to stable after testing rc1.
- Root causes found in the rc1 source and repaired on
  `codex/kingmaker-buff-planner-z-native-assignments` (fix commit `ab155c2`,
  version bump `5d94502`, installer guard `3625534`):
  - P1 theme lookup resolved `ServiceWindow/...` donors from the planner's
    own overlay root (guaranteed failure); donors now resolve from the
    StaticCanvas while application stays owned-scope.
  - P2 paper reached only three direct child names (the nested
    `EnhancementChooser/EnhancementChooserFrame` never matched); owned
    surfaces are now registered explicitly, fallback tints/outlines yield,
    and missing capabilities get a bounded retry.
  - P3 spellbook button: exact-path + tolerant bounded-scan locator
    (ambiguity refuses), corner-anchored caption-fitted natively-styled
    button, placement logged; handoff machinery unchanged.
  - P4 chooser/card budgets now derive from `ResourcePoolAllocation`
    (`enhancement:<pool>` lines: native now/requested/allocated/unmet/
    projected + affected casts + this-assignment coverage + reorder
    behavior); no second charge counter.
  - P5 metamagic naming: game enum → CallOfTheWild `MetamagicExtender`
    display-name contract (fail-soft reflection; offline-verified values
    Persistent=0x10000000, Piercing=0x80000, Selective=0x2000000,
    ThrenodicSpell=0x2000) → item-derived descriptor; masks are
    diagnostic-only.
  - Header caption is now `Assignments & Resources`; selected spell gains
    `Edit Assignments`.
- Gates: source 42/42, protocol 178/178 (six new released-failure
  regressions), harness 27/27, package 4/4, WhatIf 5/5, publisher gate
  3/3, deterministic Release build 2/2.
- Runtime lane: the guarded live-UI attempt was blocked because an
  owner-controlled Kingmaker process was running (PID 1896); the guards
  refused correctly, no state was touched. Earlier failed install attempt
  (prerelease CLR-version parsing in the identity guard) rolled back
  exactly; fixed in `3625534`. Rendered appearance, visible spellbook
  entry, and in-game budget displays are therefore **source-verified but
  not live-observed**; the release notes disclose this prominently.
- PUBLISHED: https://github.com/howardreith/KingmakerBuffPlanner/releases/tag/v0.1.1-rc2
  (non-draft prerelease; v0.0.19 keeps the Latest channel; main unmerged).
  Asset `KingmakerBuffPlanner-0.1.1-rc2.zip` SHA-256
  `c5f888b91252bfa0dcf4f286a934772c6d15b692e8632b6c30b5db4e85ff649e`,
  re-downloaded and verified (hash match + package validation 4/4).
  Tag `v0.1.1-rc2` == source `a30c07e` == pushed branch HEAD. The
  publisher rebuilt at the final records commit (an earlier deterministic
  build of the same fixes hashed f6aa4de1...); PR #1 body updated.
- Local machine state: the guarded install transaction `rc2fix-liveui-3`
  has rc2 installed over the prior 0.0.19 (backup at
  `runtime-backups/install-rc2fix-liveui-3`); it is the f6aa build of the
  same fixes (commit `5d94502`). Restorable via `Restore-Local.ps1 -RunId
  rc2fix-liveui-3` or by reinstalling the published ZIP. No other mods,
  saves, or settings were touched; no locks or unresolved transactions.
- NEXT: owner runs the five-minute check in the release notes and reports;
  stable promotion remains merge review + default-branch publish.

## Release closeout — 2026-09-19 (rc1; superseded by the RC2 recovery above)

- **TESTING PREVIEW PUBLISHED:** https://github.com/howardreith/KingmakerBuffPlanner/releases/tag/v0.1.1-rc1
  (non-draft prerelease, --latest=false; stable v0.0.19 and the latest
  channel untouched). Asset
  `KingmakerBuffPlanner-0.1.1-rc1.zip` SHA-256
  `ea1fa19e839df4a5c5ddaf095d15a2933acec3f573b6ff3868430028c50b4c7b`,
  verified against a fresh download (hash match + package validation 4/4).
  Tag `v0.1.1-rc1` == source `5da102f` on the feature branch, pushed.
  Main-branch merge deferred to owner review.
- Gates at publish: source 42/42, protocol 172/172, harness 27/27,
  package 4/4, WhatIf 5/5, publisher gate 3/3, deterministic build 2/2.
- Live runtime acceptance: UNVERIFIED, disclosed in the release notes.
  Automated save-load remains blocked by the fixture save's mod
  dependencies under minimal staging (GUID evidence in the tracker) and
  by full-config boots not finishing inside the automated environment.
  The owner's own configuration loads the seed (owner-verified).
  **Do not request another seed; do not resume the engine-only
  investigation — that theory is superseded by the GUID dependency
  evidence. Manual owner testing is the acceptance path.**
- Known preview limitations (also in the release notes): one-way
  spellbook return; pooled-per-caster rod selection; theme fallback is
  not verified native artwork.
- NEXT ACTIONS (only after owner feedback): address owner-reported
  defects; stable promotion path = merge review + default-branch publish
  through the guarded publisher (feature-branch switch is prerelease-only).

# AUTONOMOUS-RESUME — top section is current; planning/Z-NATIVE-ASSIGNMENTS-STATUS.md is the per-checkpoint tracker.

## First live fixture + smoke attempt handoff — 2026-09-19 (LATEST)

- Status: fixture bootstrap + teardown BOTH proven against the REAL save
  directory with every other save byte-identical. One integration defect
  (fixture/deployment state-root collision) and one environment change
  (UMM 0.33.0) were found by the first real run and are fixed with
  regressions (harness 27/27, source 42/42, protocol 172/172, package
  4/4, WhatIf 5/5).
- The smoke run itself failed INSIDE the game engine loading the new
  seed: Player.PostLoad NRE during SaveManager.LoadRoutine. The seed was
  saved at the start-prologue point (area entry
  `..._startprologue.json`), which this engine cannot load from the main
  menu — an engine-level save-point incompatibility, not a mod/harness
  defect. The manifest-bound teardown removed the pair cleanly; the seed
  file remains.
- HUMAN ACTION NEEDED (smallest, two steps in one session):
  1. From Kingmaker's main menu, try loading `KBP_AUTOMATION_SEED`
     manually once. (Expected to fail with the same crash — this merely
     confirms the diagnosis; either result is useful evidence.)
  2. In the SAME disposable campaign, continue past the opening Jamandi
     conversation until the party is freely controllable (you can walk
     and open inventory), then save again named exactly
     `KBP_AUTOMATION_SEED` (overwriting or as a new file is fine), and
     fully exit Kingmaker.
  Then I re-run the guarded bootstrap (pair indices continue at 306/307)
  and immediately the live-ui-bootstrap smoke.
- Branch pushed through the guarded helper; candidate local-only.

# AUTONOMOUS-RESUME — top section is current; planning/Z-NATIVE-ASSIGNMENTS-STATUS.md is the per-checkpoint tracker.

## N1-N4 review continuation handoff — 2026-09-18 (LATEST)

- Status: all four 92f13fc-review findings closed with production-path
  regressions (protocol 172/172, harness 26/26). The fixture helper's
  write-ahead recovery is now complete across the journal-before-move
  window, rollback failure retains the lock for -Recover, and teardown
  validates containment/role/campaign/hash before deleting anything.
- Candidate: package SHA-256
  `562cb2699cca7b0921b7697f226829cfd5f5ed807b506a664058bf004ebd2b91`,
  source `ebeaab6`, local-only, deterministic 2/2.
- THE ONE HUMAN ACTION (unchanged): create a genuinely NEW disposable
  campaign, save once named exactly `KBP_AUTOMATION_SEED` at a
  controllable moment, fully exit Kingmaker. Then I run the repaired
  guarded bootstrap, establish normal-save/profile protection, provision
  the casting fixture, and run the first short rendered/input smoke
  (planner open/close, button states, overflow chooser wheel+scrollbar,
  assignment add/edit/remove, spellbook open) before the wider matrix.
- Known incomplete feature: spellbook same-context return — qualify the
  native reopen contract during the first isolated spellbook run.

# AUTONOMOUS-RESUME — top section is current; planning/Z-NATIVE-ASSIGNMENTS-STATUS.md is the per-checkpoint tracker.

## R1-R4 targeted repairs handoff — 2026-09-18 (LATEST)

- Status: R1-R4 closed with production-path regressions (protocol 169/169,
  harness 23/23). R1 real-save-use safety gate: PASS on isolated-root
  evidence; the real save directory remains untouched pending a genuine
  `KBP_AUTOMATION_SEED` (absence re-verified; do not fabricate one).
- Candidate: package SHA-256
  `43f911f1c3f06e2f00a4d6951ed75ac84e13574cc390748ebd272e7ed6e8697d`,
  source `80847e6`, local-only, deterministic 2/2.
- THE ONE HUMAN ACTION (unchanged): create a genuinely NEW disposable
  campaign, save once named exactly `KBP_AUTOMATION_SEED` at a controllable
  moment, fully exit Kingmaker. Then I run the repaired guarded bootstrap
  (real process checks, lock/token binding, write-ahead publication,
  manifest-bound teardown all harness-proven), establish normal-save/
  profile protection, provision the casting fixture, and start the live
  lanes (rendered smoke first).
- Known incomplete feature: spellbook same-context return — awaiting the
  live native-reopen contract inspection during the first spellbook run.

# AUTONOMOUS-RESUME — top section is current; planning/Z-NATIVE-ASSIGNMENTS-STATUS.md is the per-checkpoint tracker.

## PR1 repair follow-up handoff — 2026-09-18 (LATEST)

- Status: ALL SIX SOURCE FINDINGS REPAIRED with regressions (F1 bootstrap
  guard bypass + recoverable transaction; F2 assignment editor; F3 real
  sequential forecast; F4 acknowledged review state + full cost signature;
  F5 unresolvable-request coverage; F6 spellbook opener/deferred
  presentation/recovery). Gates: source 42/42, protocol 165/165, harness
  18/18, package 4/4, WhatIf 5/5.
- Candidate: package SHA-256
  `79142e31cc39f829509804898e667023b5bdad7458068f697cd44bbb39d3a2fa`,
  source `4ccd119`, DLL `75a81b22`, MVID `8ef72d40-681a-4ac3-aa56-6e4a9623c70c`,
  local-only. Draft PR #1 body updated.
- Spellbook same-context return: EXPLICIT GAP, not a live-test placeholder —
  no verified offline native spellbook-reopen contract exists; recovery
  lands in the planner instead and logs the missing contract.
- THE ONE HUMAN ACTION (unchanged): create a genuinely NEW disposable
  campaign, save once named exactly `KBP_AUTOMATION_SEED` at a controllable
  moment, fully exit Kingmaker. Then run the REPAIRED
  `scripts/New-KbpAutomationFixture.ps1 -Confirm:$false` (guard now really
  checks for a running game). Then provision the casting fixture through
  the disposable-fixture process and run the live lanes.

# AUTONOMOUS-RESUME — top section is current; planning/Z-NATIVE-ASSIGNMENTS-STATUS.md is the per-checkpoint tracker.

## Z continuation mission handoff — 2026-09-18 (LATEST)

- Status: REVIEWABLE + COMPLETION WORK DONE, live lanes awaiting ONE
  human-only seed save. Draft PR #1:
  https://github.com/howardreith/KingmakerBuffPlanner/pull/1 (branch pushed
  through the guarded helper; further commits below pushed after this
  record). All Checkpoint 1/2/4 items complete; Checkpoint 3 complete except
  the human seed; Checkpoint 5 NOT RUN pending the seed; Checkpoint 6 rebuilt.
- Branch `codex/kingmaker-buff-planner-z-native-assignments`; continuation
  commits: editor+gate `461d999`, fixture bootstrap + rod record `09a366e`,
  records commit (this one). Deterministic gates: source 42/42, protocol
  161/161, harness 12/12, package 4/4, WhatIf 5/5.
- Candidate (supersedes 0d1a1af3): package SHA-256
  `a9bf3ad363deed4187feb7c937c56491001b2ea8799095d9014d66475907d35b` at
  `artifacts/release/0.1.0/KingmakerBuffPlanner-0.1.0.zip`, source commit
  `09a366e`, DLL `679c2544...`, MVID `e6c10be8-5602-4076-ac3d-0aa6c36f4ec7`,
  local-only.
- Exact-rod identity: BLOCKED BY NATIVE CONTRACT (assembly evidence recorded
  in the status doc): ItemEntity/ItemsCollection are object-rooted with no
  durable instance identity; pooled per-caster semantics are correct and
  remain the only offered behavior. Do not "fix" T03 by inventing identity.
- THE ONE HUMAN ACTION (unblocks every live lane): in a Kingmaker session
  the user controls, create a deliberately disposable campaign and save once
  with the exact name `KBP_AUTOMATION_SEED` (any controllable moment). Then
  run `powershell -ExecutionPolicy Bypass -File
  scripts/New-KbpAutomationFixture.ps1 -Confirm:$false` and expect
  "Fixture bootstrap PASS". Then execute Checkpoint 5 lanes from the status
  doc (donor inventory -> rendered chooser -> assignment editor ->
  spellbook -> casting/resource runs -> regressions) with the approved
  harness, restoring after each run.

# AUTONOMOUS-RESUME — see planning/Z-NATIVE-ASSIGNMENTS-STATUS.md for the live checkpoint tracker of the active Z native-assignments mission.

## Z native-assignments mission handoff — 2026-09-18 (LATEST)

- Status: 0.1.0 CANDIDATE PREPARED, all checkpoints source/build complete;
  every save-backed live lane BLOCKED (authorized `KBP_AUTOMATION` fixture
  pair absent; only protected `KMG_` fixtures remain). Not fully complete;
  do not label the mission complete while mandatory live acceptance is
  unproven.
- Branch: `codex/kingmaker-buff-planner-z-native-assignments` (local, unpushed
  beyond the inherited diagnosis branch base `164737e`). No merge to main, no
  tag, no release, no guarded push run for this work.
- Checkpoints: A overflow repair `a588538`; B theme foundation `3721932`;
  C schema-5 assignments `27156d2`; D casting order/partial apply `dc192a0`;
  E spellbook entry `ef8b1b3`; F records/version/package (final commit).
- Gates: source 42/42, protocol 159/159, harness 8/8, package 4/4,
  deployment WhatIf 5/5, deterministic Release build PASS. Candidate
  package/hashes: see `docs/QUALIFICATION.md` 0.1.0 section and
  `artifacts/release/0.1.0/`.
- Open blockers: (1) no authorized `KBP_AUTOMATION_BASELINE`/`WORKING` save
  pair — blocks every live lane including rendered chooser/theme/spellbook
  qualification and T03 exact rod identity; (2) theme donor inventory
  promotion (BoundedScan locators need a live capture); (3) spellbook
  native-close affordance assumption (`Close` button name) unverified live;
  (4) same-context return-to-spellbook not implemented (no verified native
  reopen API).
- Exact next safe action: recreate/import the authorized fixture pair through
  the guarded process, then run the live qualification lanes starting with
  `ui-polish`-style donor inventory capture to promote the scan locators,
  followed by rendered chooser/spellbook acceptance per
  `docs/MANUAL-ACCEPTANCE.md` 0.1.0 checklist.

# Autonomous Resume

## 2026-09-06 failed human validation: routing diagnosis

Product-bearing checkpoint: `de57d90b38711c4c641d470900339bd8815a3fa8`.
Final candidate ZIP/DLL/MVID and exact command counts are recorded in the
investigation report's delivery checkpoint. The package remains diagnostic-only;
final bridge 21/21, exact metadata 87/87, deterministic builds 2/2, candidate
deployment purity 5/5 and release installer purity 5/5 all pass. No gameplay
coverage is promoted. The reproducing-machine cast log remains the next action.

Instant Share remains unresolved; gameplay **NOT VERIFIED**. On local machine
DATA the installed Planner 0.0.19 / Gunslinger 0.0.115 DLLs match both released
hashes and MVIDs. Executing the real production bridge accepts that pair and
rejects the actual older 0.0.114 provider. No affected casting log or running
game is available; the exact Kingmaker save root is absent.

Active branch: `codex/kingmaker-buff-planner-instant-share-routing-diagnosis`;
starting/audited HEAD: `fd0e6dc1c32dfc929a56dbc575163e641b150746`;
version remains 0.0.19. Diagnostic-only source adds pre-cast routing evidence,
loaded-pair identity, provider-direct phase records and visible fallback
outcomes. The native transaction and resource policies are unchanged.
Focused gates: source 42/42, behavior 150/150, harness 8/8, package 4/4,
deployment WhatIf 5/5; candidate production bridge 21/21 assertions in 3/3
processes. No failed checks are counted as passes.

Commands, exact identity tables, evidence under
`artifacts/instant-share-diagnosis/`, rejected theories, and limits are in
[the investigation report](docs/INSTANT-SHARE-FAILED-VALIDATION.md). This supersedes any interpretation of the historical public
release records below as proof that Instant Share worked in Howie's game.
Exact next action: capture one affected cast from the reproducing machine and
identify its capability/selected-executor/Fire discriminator. A diagnostic
candidate is not a gameplay fix or authorization trigger for a new public release.

## 2026-09-06 paired public release complete

Owner-authorized releases are public and independently download-verified:
Planner 0.0.19 at `583b1e984f8e02720b09362f41082bfbd077a186` on
`main`; provider 0.0.115 at `473f83bd901602ebe610cfdf291f11ce4a3faa57`
on `master`. Release gates PASS; gameplay NOT RUN. The final guarded resolver
reports `The exact Kingmaker save root is unavailable.` No game, Mods,
installed dependency, or save was changed. Earlier in-progress/authorization
blockers below are historical and superseded. Full commands, counts, hashes,
MVIDs, package paths, and release links are in `docs/QUALIFICATION.md`.
Exact next action: commit/guarded-push this documentation-only completion
record and fast-forward the defaults; then only the protected-save procedure
in `docs/MANUAL-ACCEPTANCE.md` remains for a future gameplay claim.

## 2026-09-06 paired release authorization

The owner explicitly authorized committing all task changes, merging to both
default branches, pushing, and publishing new releases. This supersedes the
earlier authorization blocker, not the save-backed gameplay evidence boundary.
Planner branch is now `codex/kingmaker-buff-planner/share-transmutation-instant`
at `e0fc450b8414581ce6adcc38d46d5c56b328aaec`, version 0.0.19. Provider branch
`codex/share-transmutation-instant` is at
`e985aad7671992885210df448deca18d05081096`, version 0.0.115.
The provider worktree and all generated evidence were preserved in
`C:/Dev/KingmakerGunslingerLab/worktrees/share-transmutation-instant`.

Final cancellation review extended the existing regression: Hybrid disposal
now forwards inner cleanup-failure records even when its iterator is cancelled.
`scripts/Test-SourceOnly.ps1` passes source 42/42, behavior 148/148, harness
8/8, package 4/4, deployment WhatIf 5/5, aggregate 1/1. Provider
`scripts/Build-Local.ps1 -ReferenceBundleDir <qualified-private-references>`
passes focused source validation, 1393/1393 domain tests, exact-reference
Release compile, supply-icon, output, SoundBank, and strict 135-file package
gates. Logs: `artifacts/share-release-precommit-source.log` and provider
`artifacts/share-release-precommit-build.log`. Current exact contract gate is
87/87; final release commits and hashes must be captured after deterministic
rebuilds.

Both missing lab push guards were restored with exact-origin, approved-branch,
clean-tree, unfinished-operation, protected-file, credential, fast-forward,
and remote-hash checks; Planner `Test-GuardedPush.ps1` passes 6/6 WhatIf
checks. No installed dependencies were downgraded or replaced. Provider
publication can explicitly use its existing provenance-checked builder;
the missing external bundle input resolves only to identical hash-validated
tracked bytes. No asset was edited.

Exact next action: commit/guarded-push the feature checkpoints, merge into
`main`/`master`, run exact-merge deterministic and contract gates,
guarded-push defaults, publish both versions, independently download and
validate their assets, then record final identities. Runtime: NOT RUN,
protected save pair baseline=0/working=0; no ordinary save substitutes.

## 0.0.19 Instant Share Transmutation - 2026-09-06

- Continue on primary/provider branch `codex/share-transmutation-instant`.
  Primary base/current HEAD are
  `622760c9aeb1d2f583652bb71d391956b4a6576b` /
  `95b723da35046f5b1fa1c229ee3e318a0aaedf38`, version 0.0.19. Provider
  base/current HEAD are `6874dc15a27ded132456dbdd480f47c794543a05` /
  `a788d2269fcc4aaa24f8c49f820257ceb9cf7403`; its 0.0.115 release-default
  roll-forward is uncommitted pending explicit scope authorization.
- Core implementation and regressions are complete. Planner gates pass 148/148
  behavior, 42/42 source, 8/8 harness purity, 4/4 package, and 5/5 deployment
  WhatIf; provider gates pass 1393/1393 domain,
  focused source validation, and exact-reference compile.
- Provider strict build-output/package validation and two deterministic package
  creations pass using its manifest-matching tracked bundle only in generated
  workspace output. The all-in-one builder's legacy external Unity path remains
  absent; no external Unity tree, live Mods, or saves were touched. Primary
  clean release packaging follows after the provider worktree is finalized and
  removed.
- Exact next action: finish the authorized provider package gate, capture
  package/DLL/MVID hashes, commit both coherent branches, run primary clean
  source/contract/deterministic release gates, and record final identities.
- Runtime gameplay remains NOT RUN: no exact protected
  `KBP_AUTOMATION_BASELINE` / `KBP_AUTOMATION_WORKING` pair is available.

## 0.0.18 public release completion - 2026-09-01

- Exact release/tag commit is
  `1477979fa8b44e220adc3ece0afc85581a8b5811`; annotated tag object is
  `31578aa531a4d49620d6cf3939c1689d366f986b`. Public release published at
  `2026-09-02T02:23:47Z`:
  `https://github.com/howardreith/KingmakerBuffPlanner/releases/tag/v0.0.18`.
- Exact-merge and publisher gates passed 42/42 source, 145/145 protocol/domain,
  8/8 runtime filesystem, 4/4 package, 5/5 deployment WhatIf, aggregate 1/1,
  Debug/Release 1/1 each, deterministic builds 2/2, release builder 3/3,
  install WhatIf 5/5, guarded push WhatIf 6/6, and runtime WhatIf.
- Published ZIP is 287,409 bytes, SHA-256
  `b81797f67b7a47b6caa8ee6bca327fdbfbe91571e38dc1ee7ce3486beac4c54f`.
  Packaged DLL SHA-256/MVID are
  `4dec770681762893a77584cf83bc56e7a00d8bbe4e2fcbc6f6d843b2d0ad5f04` /
  `2bd9bceb-1aab-4310-be32-34f661c3e3ff`; independent GitHub asset/API,
  checksum, and strict validation all agree.
- No install, launch, Mods mutation, or save access occurred. The remaining
  gameplay evidence boundary is baseline=0/working=0. Exact next action is to
  commit and guarded-push this documentation-only completion record; then only
  future protected-save manual acceptance remains.

## 0.0.18 publication authorization - 2026-09-01

- The owner explicitly authorized merging accepted feature HEAD
  `ebc146ec3abb8b08475b1b1c055ea121007fefb2` to `main`, guarded-pushing it,
  and publishing a new public 0.0.18 release.
- GitHub preflight: authenticated owner `howardreith`, public repository,
  default branch `main`, synchronized local/remote main
  `81df77847487683d1857f0a8f400a1a2781a6244`, and no existing `v0.0.18` tag
  or release.
- The accepted runtime limitation remains baseline=0/working=0 and is retained
  in public notes. Exact next action is authorization commit, non-fast-forward
  main merge, exact-merge qualification, guarded push, guarded publication,
  independent asset verification, and a completion record.

## 0.0.18 sticky-touch instant repair - 2026-09-01

- Branch `codex/kingmaker-buff-planner-sticky-touch-instant`; start
  `81df77847487683d1857f0a8f400a1a2781a6244` / 0.0.17; implementation and
  regression commits `0504474` and `1c0a409`; exact product-bearing candidate
  commit `5111f52d0407e12a5c05614a1f7c6e0fa8378be3`.
- Structural sticky delivery now stays in Instant mode, rule-casts a derived
  delivery `AbilityData`, spends the exact reserved source once, and will not
  advance through residual held/command state. Animated mode watches the
  carrier plus generated delivery and cleans its own state.
- Final gates: source 42/42, protocol/domain 145/145, runtime filesystem 8/8,
  package 4/4, deployment WhatIf 5/5, aggregate 1/1, Debug/Release 1/1 each,
  deterministic builds 2/2, release builder 3/3, install WhatIf 5/5. Version
  is 0.0.18. ZIP/DLL/MVID are
  `b47698e66bc8b82ff916abc115b8d712799ab0b941b6525bbca1d60dca525029` /
  `4c81954380b50612d1df65f9590fb4058f733dfa18cffb8905f11eaf0a67c4b0` /
  `4659c6e8-41a5-4a11-a996-e64826c37c9d`.
- Live qualification is unavailable at baseline=0/working=0. The no-save
  runtime WhatIf is pure; save-backed preflight refused before staging. Do not
  install, launch, or substitute another save. Exact next action is the manual
  protected-save checklist when an authorized pair exists; publication is
  forbidden without separate authorization.

## 0.0.17 public release completion - 2026-09-01

- Release engineering is complete. Exact release/tag commit is
  `edf9e642c4a743309ef0686e377ed798a2a88340`; annotated tag object is
  `9476b556a64157b4965257b8bc66e62303b36ee8`. The public, non-draft,
  non-prerelease release was published at `2026-09-02T00:17:46Z`:
  `https://github.com/howardreith/KingmakerBuffPlanner/releases/tag/v0.0.17`.
- Guarded main push passed at exact release source. The publisher repeated
  source 41/41, protocol/domain 135/135, runtime filesystem 8/8, package 4/4,
  deployment WhatIf 5/5, aggregate 1/1, deterministic builds 2/2, and release
  builder 3/3 before pushing the annotated tag and publishing.
- Published ZIP is 279,019 bytes, SHA-256
  `eda62dbf9c70d0e416fec923428f88a6f9cbe22ccd98b8fe95d2a61e1467bdb8`.
  Packaged DLL SHA-256/MVID are
  `0f2d3309b5ed309b5f537e3707cd5dd1977c32ed8fd6d96eb1e5d3e48d38eb61` /
  `7be5178e-f5e6-4fb6-b4b5-dad581aca39b`. Downloaded GitHub assets, API digest,
  `SHA256SUMS.txt`, strict package validation 4/4, DLL, and MVID all agree.
- No install, game launch, Mods staging, or save access occurred. The missing
  baseline/working save pair remains an explicit runtime-evidence limitation,
  not a release-engineering blocker or a claimed gameplay PASS.
- Exact next action: commit and guarded-push this documentation-only completion
  record. After that, no release-engineering action remains.

## 0.0.17 publication authorization - 2026-09-01

- The owner explicitly authorized finalization, non-rewriting merge to `main`,
  guarded remote push, annotated tag, and a new public GitHub release.
- Read-only preflight proved authenticated account `howardreith`, public
  repository `howardreith/KingmakerBuffPlanner`, default branch `main`, and
  origin `https://github.com/howardreith/KingmakerBuffPlanner.git`.
- Before this authorization record, feature HEAD is
  `9d460e025a934d91c2d310bbaeced225f398ff2b`; local `main` and `origin/main`
  are synchronized at `f9cf2ac35535c8201dea7ef7f5172ebaa051e7ad`, which is an ancestor of the
  five candidate commits. Both remote tag and GitHub release `v0.0.17` are
  absent.
- Release notes and changelog are publication-safe while continuing to state
  the exact save-backed limitation. The owner accepts the qualified candidate
  for publication without relabeling the unavailable runtime scenarios as
  PASS.
- Exact next action: commit this authorization record, create the established
  no-fast-forward release merge on `main`, qualify that exact merge commit,
  guarded-push `main`, run the guarded public publisher, independently verify
  the tag and downloaded assets, then record and guarded-push completion.

## 0.0.17 final local handoff - 2026-09-01

- Exact qualified source/package commit:
  `5976a4b5222c408f0b6c3c8fb3f5314c8db5c54f`; branch
  `codex/kbp-communal-share-transmutation-infusion`; version 0.0.17; schema 4.
  The next commit is provenance-only.
- Final gates: source 41/41, protocol/domain 135/135, runtime filesystem 8/8,
  package 4/4, deployment WhatIf 5/5, aggregate 1/1, Debug 1/1, Release 1/1,
  Brown Fur exact contract 57/57, deterministic builds 2/2, release builder
  3/3, install WhatIf 5/5, guarded runtime WhatIf PASS.
- ZIP/DLL/MVID:
  `e8991848e9d11168f2f7a4f6ea67a7ff233661e497e0d8867505f384286f963d` /
  `0451807d8c0f7431467c2cb3be22ba4e20edc9b552bfb6f66445fd69128e8d01` /
  `983a62c2-5e63-4261-b7d0-996cbd836aaa`.
- Save preflight is blocked at baseline=0/working=0; actual gameplay runs=0.
  The guarded push WhatIf is non-applicable/refused because its external
  allowlist does not contain this dedicated mission branch. No policy was
  changed and no install, launch, push, merge, tag, or publication occurred.
- Exact next action: await an explicitly authorized exact KBP automation save
  pair and run `docs/MANUAL-ACCEPTANCE.md`. Do not substitute another save.

## 0.0.17 communal/Share/Infusion checkpoint - 2026-09-01

- Start: clean `main`
  `f9cf2ac35535c8201dea7ef7f5172ebaa051e7ad`, version 0.0.16. Active branch:
  `codex/kbp-communal-share-transmutation-infusion`. Implementation/test
  checkpoint:
  `98d41723cec611a4d2a7528ac801bf7c3654bdb9`. Candidate metadata is 0.0.17;
  schema remains 4.
- Communal failure was in provider-option normalization: an allied-area
  expression whose selected concrete blueprint did not yield one radius fell
  into ordinary direct `AbilityData.CanTarget`, erasing the recipient map
  before the intact presentation layer. Exact declared-source/selected-variant
  geometry is now recovered only for a proven relationship; unsafe or missing
  contracts have zero options.
- `EffectiveProviderOptionResolver` makes target legality assignment/routine
  aware. Optional Share uses exact native feature/toggle/marker/reservoir
  contracts and native targeting; enhancement groups and shared pools compose
  Share + Powerful Change at aggregate cost 2. Native code alone debits.
  Alchemist Infusion is passive native targeting with no toggle/surcharge.
- Latest post-implementation source-only result: source 41/41, protocol 135/135,
  runtime filesystem 8/8, package 4/4, WhatIf 5/5, aggregate 1/1. Optional
  Brown Fur assembly contract is 57/57 against version 0.0.113.0, SHA-256
  `97a1ad535a7b384759272cf37c0fe8705843b9d149a61e9e8b6c41df39437913`,
  MVID `685d2575-41e1-4897-881c-a314229ad7cf`.
- Live qualification is blocked honestly at baseline=0/working=0. No save,
  install, game launch, live staging, push, merge, tag, or publication occurred.
- Exact next action: commit the 0.0.17 metadata and durable records, run the
  clean-head source/Debug/Release/exact-contract/deterministic release/package/
  diff gates, then append exact package hashes in a provenance-only commit. Do
  not publish or install.

## 0.0.16 public release completion - 2026-09-01

- `main`, `origin/main`, and annotated tag `v0.0.16` resolve to
  `2628738dbae09051ebce467fd35c2da6dd27f58d`. The public release is
  `https://github.com/howardreith/KingmakerBuffPlanner/releases/tag/v0.0.16`.
- Publisher results: source 39/39; protocol/domain 127/127; runtime filesystem
  8/8; package 4/4; deployment WhatIf 5/5; aggregate 1/1; deterministic builds
  2/2; release builder 3/3. Published ZIP/DLL/MVID are
  `0f46a16cd7210a9be4d92138bad5135b32f5017b5c270b353309f7036f8a44f6` /
  `4c7249ad7a953522ea755e8bb46c5b89b136b9479a7360440526f417fb171597` /
  `49488746-8fa8-46b0-827e-7a73cc00f1af`; independent download agrees.
- Guarded install `published-0.0.16-final-install-20260831` is Installed with
  settings preserved and other mods verified unchanged; installed bytes match.
- No release-engineering action remains. Runtime manual acceptance remains
  blocked at `baseline=0; working=0`; do not use an ordinary save.

## 0.0.16 catalog/native-HUD publication authorization - 2026-08-31

- Active branch is `codex/kingmaker-buff-planner-0.0.16-catalog-native-hud`,
  based on clean `main` `91f198f53733b0fa63bfbc6c93ee133360b9b194`; product
  metadata is 0.0.16 and persistence remains schema 4.
- Structural provider roles, restorative payload classification, per-anchor
  allied coverage, routine membership chips, and native lower-left HUD style,
  parchment tooltip, and setup-opening sound are implemented. The exact
  installed Call of the Wild contracts and native UI contracts are recorded in
  `planning/BUFF-CATALOG-NATIVE-HUD-EVIDENCE.md`.
- Release source is `90e5f43ed0c3447a3f73ca799706d653aa4a67f7`. Final
  source-only gates pass source 39/39, protocol/domain 127/127, runtime
  filesystem 8/8, package 4/4, deployment WhatIf 5/5, aggregate 1/1; the
  clean-head release builder passes deterministic builds 2/2 and release
  builder 3/3. ZIP/DLL/MVID are
  `0cced8d7dffc6543686ee413885bcd12d645af9c4ece8ad7d2a3ca2b2600c4a8` /
  `d1164180519dc6c91d3fe851aa87192f0985cfbc0a8005f97186e165654acdde` /
  `a91c37ec-1d91-4ab2-99d0-24da8fe5b686`.
- The package is guarded-installed locally under installation ID
  `kbp-0.0.16-catalog-native-hud-20260831`: status Installed, settings
  preserved, other mods verified unchanged, installed bytes exact. No game was
  launched.
- The owner explicitly accepted the qualified candidate and authorized its final
  documentation commit, non-rewriting merge to `main`, guarded push, annotated
  tag, and GitHub release. Exact next action: execute that guarded sequence,
  independently verify the public release bytes, and record it. The absent
  exact baseline/working pair remains an honest runtime boundary; no ordinary
  save may substitute.

## 0.0.15 publication authorization - 2026-08-29

- The owner explicitly authorized the final commit, merge to `main`, remote
  push, and a new public release for the completed caster-controls mission.
- Public repository/default branch/authenticated account are
  `howardreith/KingmakerBuffPlanner` / `main` / `howardreith`. Feature HEAD
  before release preparation is
  `ed6108f1549aadf8b570d686e9f97f2aa80940b3`; refreshed local and remote
  `main` are both `011a57ff0565b5954745a8b0e742726a74b4315f`. `v0.0.15`
  is unused locally and on GitHub.
- Product metadata and release notes advance to 0.0.15. Existing schema 4 is
  unchanged. The guarded baseline=0/working=0 runtime limitation remains
  explicit and no ordinary save will be substituted.
- Exact next action: commit and fully qualify the clean release source, merge
  without history rewriting, qualify exact `main`, then use the guarded push
  and publication helpers and independently verify the published assets.

## Buff catalog/caster-controls checkpoint - 2026-08-29

- Active branch is `codex/buff-catalog-caster-controls`; implementation commit
  is `ec718fa96ae1cbbb1feeb5b3acd1900e867b699a`; qualified implementation/
  record HEAD is `ce7099b089440e40716cbbd39c4e377c4fbe21c2`; version remains
  0.0.14.
- Implementation is complete for HUD feedback retirement, exact current-caster
  variant selectability, safe persistent-payload classification, allied area
  semantics, and exact-provider caster policy UI/persistence/preview.
- Exact gates: inspection exit 0; source 38/38; protocol/domain 119/119;
  runtime filesystem 8/8; package 4/4; deployment WhatIf 5/5; aggregate 1/1;
  Release build 1/1; deterministic builds 2/2; release builder 3/3; diff check
  pass.
- Local-only ZIP/DLL/MVID:
  `239eb9de3657de030c88dabfffeaa3fab344ec01e8d561e6649281d7a9cf0571` /
  `6fe6d6837f5155b5ad1b1cdd1e64d47974cadbab44a824efa42dc0edea48b4d6` /
  `80732098-e9da-45b2-b402-7b4ca6f52752`.
- Structured implementation/contract/probe evidence is
  `planning/BUFF-CATALOG-CASTER-CONTROLS-EVIDENCE.md`.
- Runtime remains honestly blocked at exact guarded inventory
  `baseline=0; working=0`. Nothing was installed, launched, pushed, tagged,
  or published.
- Exact next action: use the bounded manual checklist only after an authorized
  save pair exists. No further safe source/package work remains.

## 0.0.14 published completion - 2026-08-28

- Release/tag commit is `1ad148780d801d63d7ab40e52bba94b7c4627b47`;
  annotated tag object is `d9cd66949623f66f1da3a1c902df16f8f45a9955`.
  Guarded push verified remote main at that commit before publication.
- Publisher repeated source 34/34, protocol/domain 112/112, runtime filesystem
  8/8, package 4/4, deployment WhatIf 5/5, aggregate 1/1, and deterministic
  release 2/2. Public release is non-draft/non-prerelease:
  `https://github.com/howardreith/KingmakerBuffPlanner/releases/tag/v0.0.14`.
- Published ZIP/DLL/MVID are
  `a319a7f18aa7a20e47282fdf5b10dfee0adafd355677cf870a7fbb065028484b` /
  `4602cfe124470f5b5e5d336018ddc51a474cd931b2327e58d58eac299e4bc5bf` /
  `9f2db5b0-f61b-41c5-a3f7-54bc0296e3fe`; checksum asset SHA-256 is
  `4bdc2449090f506382772e5efeef4805c4ea103c0b6ad7c908cf4d18f1e19a8d`.
  Independent downloads match and the ZIP validates 4/4.
- No install or game launch was performed. The documented baseline=0/working=0
  in-game limitation remains. The post-publication record has been committed
  and guarded-pushed; no release-engineering action remains. Optional
  save-backed manual acceptance remains separately documented.

## 0.0.14 publication authorization - 2026-08-28

- Owner explicitly authorized final commit, merge to the repository default
  branch, remote push, and a new public release.
- Public repository/default branch/authenticated account are
  `howardreith/KingmakerBuffPlanner` / `main` / `howardreith`. After an explicit
  fetch, local and remote main are both
  `8460ae08157af40cd69b8f0a11259364ceceb885`; feature HEAD before this record is
  `d7b88b83953ac31e7af5eb4a803c6551ce601cab`. No `v0.0.14` tag or release exists.
- Release notes are publication-safe and continue to state that save-backed
  visual/cast/resource verification was unavailable at baseline=0/working=0.
- Exact next action is authorization-record commit, non-rewriting merge to
  `main`, exact merge-commit qualification, guarded push, guarded publication,
  independent asset verification, and a final publication record.

## 0.0.14 spell names and concrete variants - 2026-08-28

- Started clean from `main` at
  `8460ae08157af40cd69b8f0a11259364ceceb885`; active local branch is
  `codex/buff-spell-variants-ui`. No fetch, pull, push, remote mutation, tag, or
  publication occurred.
- Exact Kingmaker evidence: `Assembly-CSharp.dll` SHA-256
  `3b6450ffec440e296e586f71c711b195aed144b28d53e1cbb29406d18fef5afb`,
  MVID `07fa1e4d-8618-41b3-9b8d-faa17d3b26f7`; runtime `AbilityData.Variants`
  is a null stub, and the parent-data/child-blueprint constructor is the native
  context-preserving route.
- Implementation expands structurally eligible declared children, stores parent
  plus concrete child identities, groups in declared order, renders/searches
  complete localized names, preserves parent-backed availability, resolves the
  exact child for execution, and refuses ambiguous legacy inference.
- Current gates: source 34/34, protocol/domain 112/112, runtime filesystem 8/8,
  package 4/4, deployment WhatIf 5/5, aggregate 1/1, and deterministic release
  2/2. Feature/release-source commits are
  `932da35cb6633031d4077e43df65ab659bc9bd84` and
  `a78869c329e39734cd77f4b587d3d97b05fede70`.
- Local-only ZIP/DLL/MVID are
  `182a597b899875851bd4f6e125a7222018a86bdf7688d455bcf750a512f4e5cd` /
  `7c6ce2b7bf79fd24625d0d6263d36803a4ec1cd7a873be211fa569d877811fae` /
  `9896cd99-f01e-44c5-afe3-980ca1d043b9`. Package source is exact clean HEAD
  `a78869c329e39734cd77f4b587d3d97b05fede70`; publication status is local-only.
- Rejected theories: the game does not populate `AbilityData.Variants`; using a
  child GUID without source data is insufficient; presentation aliases cannot
  repair availability/spending; and selecting the first declared child would
  invent player intent.
- Save audit is exactly `baseline=0; working=0`; in-game verification is not
  claimed. Exact next action: commit this local evidence record, rerun the full
  gate, verify a clean branch, and hand off without remote or live-game action.

## 0.0.13 published completion - 2026-08-27

- No release work remains. `main`, `origin/main`, and annotated tag `v0.0.13`
  resolve to release commit `3c329cfff3530fe8397012565c238a81d55cec1d`;
  tag object is `1ac6387f9d969053a4ca2a608021e106bae3b9ee`.
- Guarded-push preflight passes 6/6. The publisher passes source 34/34,
  protocol/domain 95/95, runtime filesystem 8/8, package 4/4, deployment WhatIf
  5/5, aggregate 1/1, release builder 3/3, and two deterministic builds.
- Public release, published `2026-08-27T17:33:41Z`, draft=false,
  prerelease=false:
  `https://github.com/howardreith/KingmakerBuffPlanner/releases/tag/v0.0.13`.
- Published ZIP/DLL/MVID are
  `67768176032d6d980f09b708a636dfa8f07e5b052530deb327d833e8e4882d96` /
  `b41f31da57f9b7ee69a4e693792bf4bb1a6f7e5ea7dbff0e723c72f24d02bf86` /
  `995ed895-bb45-412c-b626-692816b1f833`. Independent GitHub download matches
  the GitHub digest and checksum and passes strict package validation 4/4;
  `SHA256SUMS.txt` hashes to
  `a5ebdab942d616c30f9e053641a76679371351e69342d5f796dfb4bd56cb15e3`.
- Guarded published install
  `brown-fur-powerful-change-0.0.13-published-install-20260827` is `Installed`;
  exact DLL/MVID match, settings preserved, every other mod verified unchanged,
  staging absent, recovery backup retained, and no Kingmaker/UMM process. Its
  result SHA-256 is
  `49e81844fd1d111b09e4c69389a5764a9692c95b7210a2b81df34268c41afa2c`.
- Direct in-game cross-mod reservoir/modifier behavior remains unclaimed and is
  retained only as the recommended diagnostic follow-up in
  `docs/MANUAL-ACCEPTANCE.md`. Exact next action: commit and guarded-push this
  post-publication evidence record; no further release-engineering action.

## 0.0.13 install acceptance and publication authorization - 2026-08-27

- Checkpoint branch/HEAD/version before this record:
  `codex/brown-fur-powerful-change-fix` /
  `07370364b3ff4e5fc3cc2b2842b24cd079fdec63` / `0.0.13`; worktree was clean.
- Guarded installer dry-run passed package validation 4/4 and mutation purity,
  then transaction `brown-fur-powerful-change-0.0.13-install-20260827` upgraded
  live UMM 0.0.12 to exact 0.0.13. Package/DLL/MVID are
  `9182e45cc5e31c137062ac9d2252a80836effc7bf8506676f303ed5276a7aa63` /
  `6e88ea23d54fb1e3ab7e7dc264129592ea36739c96fe6bb49f9d75890b216551` /
  `3a61d90c-74b2-4944-b68d-6e2229fd3eb4`.
- `status=Installed`, `settingsPreserved=true`, `otherModsVerified=true`, no
  staging or Kingmaker/UMM process remains, and install-result SHA-256 is
  `e546995c3c6fe0f63ef16e7ba729894794f6d545c9fb96c83cb34bb6f0dc957a`.
- Owner verdict: `This is acceptable.` The owner explicitly authorized final
  commit, merge to default `main`, push to remote `main`, and a new release.
  Direct cross-mod numerical cast evidence remains unclaimed and documented as
  a post-release diagnostic boundary.
- GitHub preflight: authenticated owner `howardreith`; public repository
  `howardreith/KingmakerBuffPlanner`; default branch `main`; fetched
  `origin/main` is `1a568c8af22b4c4f547be5ebb3a9ae8af86a931c`; neither tag nor release
  `v0.0.13` exists.
- After release-facing record edits, `./scripts/Test-SourceOnly.ps1` passes
  source 34/34, protocol/domain 95/95, runtime filesystem 8/8, package 4/4,
  deployment WhatIf 5/5, and aggregate 1/1. Exact next action: commit this
  acceptance record, run the deterministic release gate, merge, guarded-push
  `main`, and invoke the guarded public publisher.

## 0.0.13 Powerful Change diagnostic repair - 2026-08-27

- Active local branch: `codex/brown-fur-powerful-change-fix`; implementation
  checkpoint: `650605aaf2c1c1f7272893074b5e7ad7ed9a9224`; exact release source:
  `f086c0257c8c8636cd5af0df9ca37c4f5ac7f794`; version: 0.0.13.
- The old selector was empty because Buff Planner implemented rods only and
  rejected every class-feature enhancement. Installed-provider inspection
  disproved wrong Bull's Strength structure, rank mismatch, UI filtering,
  persistence loss, and wrong-caster caching as causes.
- The repair detects the exact live feature and validated score toggles,
  qualifies Transmutation spells by structural positive ability-score carriers
  from the exact Arcanist spellbook, shares the Arcane Reservoir across score
  choices, and routes the real toggle through native command execution. It has
  no compile-time optional-mod dependency and fails safely when absent.
- Automated status: source 34/34, protocol/domain 95/95, runtime filesystem 8/8,
  package 4/4, deployment WhatIf 5/5, aggregate 1/1, deterministic release 2/2.
  Local ZIP/DLL/MVID are
  `9182e45cc5e31c137062ac9d2252a80836effc7bf8506676f303ed5276a7aa63` /
  `6e88ea23d54fb1e3ab7e7dc264129592ea36739c96fe6bb49f9d75890b216551` /
  `3a61d90c-74b2-4944-b68d-6e2229fd3eb4`. No install or remote action occurred.
- Exact next action: run the bounded real-campaign procedure in
  `docs/MANUAL-ACCEPTANCE.md`; do not claim cross-mod resource/modifier
  qualification until it passes.

## 0.0.12 published completion - 2026-08-24

- The accepted hotfix was merged to default `main` and released from exact
  commit `a48bfae2185a50f1c50d9151666e0b5ce0a0bc3e` under annotated tag
  `v0.0.12`. Release:
  `https://github.com/howardreith/KingmakerBuffPlanner/releases/tag/v0.0.12`.
- The guarded publisher passes source 34/34, protocol 91/91, runtime filesystem
  8/8, package 4/4, deployment WhatIf 5/5, aggregate 1/1, and release build 3/3
  with two deterministic builds.
- Published ZIP/DLL/MVID are
  `1cbb2b215a78ab4dea2af5c99ebae211fe21e1f3532eb1865af3420a90ea8494` /
  `1964b0220fd0ddd4a15009900a30ee3ec3af83c4d90b022eebb87d27cde03cac` /
  `3947e19e-fd3b-4b11-95d8-8a1b360cf9a4`. A fresh GitHub download matches and
  passes strict package validation 4/4.
- Guarded install `hud-lifecycle-0.0.12-published-install-20260824` installed
  those exact published bytes with settings preserved and every non-planner mod
  unchanged. Install-result SHA-256 is
  `bc7435f972acd52b4466781f2fcb2ead515364fe5d82528d7a03be1a4adefb76`;
  no process or deployment lock remains.
- The separate published 0.0.11 release remains intact. Exact release evidence
  is in `planning/HUD-LIFECYCLE-0.0.12-RELEASE.md`. No release-engineering work
  remains.

## 0.0.12 HUD lifecycle hotfix - 2026-08-24

- Dedicated branch `codex/kbp-hud-lifecycle-hotfix-0.0.12` started clean from fetched `origin/main` commit `4a83aec19e0f6098e23b2965b3992c328136c576`, version 0.0.11. The focused implementation/test commit is `376e4a1` and the working version sources are 0.0.12.
- Root cause is confirmed: 0.0.11 consumed one install invalidation before a Boolean attempt could distinguish retryable readiness from candidate creation, while 120-frame candidate expiry could not notify or re-arm the unchanged outer-HUD coordinator. Installed liveness also omitted the inner hosting chain.
- The repair uses typed attempt/candidate/lifecycle states, one retry per 30 active-HUD frames, explicit unload/disable suspension, candidate-expiry and stale-anchor feedback, complete held-reference hosting-chain validation, and scoped `GetComponentInChildren<IngameMenuController>(true)` discovery only. Deferred placement/glyph/raycast validation remains intact.
- Exact qualified artifact source `083dfbfcf651d44bb01b302ccbbabac823e236e9` passes source 34/34, protocol 91/91, runtime filesystem 8/8, package 4/4, deployment WhatIf 5/5, aggregate 1/1, warning-free Release compilation 1/1, and deterministic release 2/2. ZIP/DLL/MVID are `eabb4785be75129cbc6cffcab030afc9e4afac32957fb7b82af2c16b0e0ac72a` / `6db3693e0ba38b3672bd0eac36b06df6e5965de29a3e78f9b97513c6accfe9e1` / `920b3246-e7f7-4818-92f4-e54294ef2db0`.
- Guarded performance run `hud-lifecycle-0.0.12-performance-1` passes 9/9 at 88.780 average FPS, all 18 moving samples at 90.710-90.896 FPS, zero HUD searches/absent-HUD dispatches, and exact restoration. Native 12/12 and Call of the Wild 26/26 pass and restore. The unchanged repeat honestly rejected a 49.810 FPS non-moving startup bucket while every moving sample remained at least 88.940 FPS.
- Explicitly authorized guarded install `hud-lifecycle-0.0.12-human-test-install-20260824` is `Installed`; exact DLL/MVID match, `otherModsVerified=true`, `settingsPreserved=true`, no process/lock remains, and install-result SHA-256 is `e1b42a8287718a2ee8aba625e6155cf9ba404e56efcf109bb16abf4798595f84`.
- Exact authorized save audit remains `Disposable save ambiguity: baseline=0; working=0`, but the owner tested the guarded-installed build in a campaign and accepted the fix. Log SHA-256 `6fec850ea76a8cf43983b20cf58a50cf84b98ef1b064181d052fadf54b055548` proves the same candidate progressed from loading-screen-blocked Pending to Installed after 57 validation frames with four buttons/listeners, then Setup opened successfully.
- The owner explicitly authorized merge to default `main` and publication of new version 0.0.12, incremented from published 0.0.11. That authorization was executed in the published-completion checkpoint above without altering 0.0.11.

## 0.0.11 published completion - 2026-08-23

- No work remains for the bounded crash/release mission. Human runtime acceptance confirms the severe movement regression is gone; the test-process crash is corrected in `f6bbe648e0311e8b0022ec9810b533fc66cc6502` and integrated at release/tag commit `3661f5c31a1060bca67758c2369b2ef361a339c9`.
- Final publisher gates: source 32/32, protocol 86/86, runtime filesystem 8/8, package 4/4, deployment WhatIf 5/5, source aggregate 1/1, deterministic release 2/2. Direct repository-style test invocation: stdout 86/86, empty stderr, exit 0, zero new crash events, zero residual processes.
- Published release: `https://github.com/howardreith/KingmakerBuffPlanner/releases/tag/v0.0.11`; ZIP `89cbebd2a1eb594d2307c4388c19588e1d4ea9c845284d36081c3e72d492795c`; DLL `95f484907f9a1008798e3557e46212faa1e41406bccf9d109d78e1921e9d46c6`; MVID `bf949174-0601-4822-a121-9c9d9c14597f`.
- Exact published bytes are installed under UMM by `published-0.0.11-final-install-20260823`; settings and non-planner mods were preserved. No Kingmaker/test process or deployment lock remains. The existing 0.0.10 release was not altered.

## 0.0.11 performance repair - 2026-08-23

- Dedicated branch `codex/kbp-performance-regression-0.0.11`; starting commit `c06793d2238577093b96a2dc3172839070e7d69a`; current production fix `f23a07b7560b2aa4cd7b3d1635436c3abffd575a`.
- Exact diagnostic A/B: unfixed normal path `perf-0.0.11-unfixed-hud-on-2` averaged 11.358 FPS with 228 global HUD searches consuming 18,874.614 ms; same DLL with only discovery suppressed averaged 89.234 FPS. Fixed normal-path runs average 89.236 and 88.671 FPS with zero searches and exact transaction restoration.
- Source-only gates pass 32/32, 78/78, 8/8, 4/4, and 5/5. Candidate package before the fixture-record commit is `912eabb41e87122c20ac3186293e3beff569bede2b4e849c423cee66d69803a4`; DLL `6c4bc51d2099cb12add499cbda3ca1caa964475bdd86e5dfc8ec81383fa8dadd`.
- `human-reproduction` was rebound read-only to the exact currently installed BagOfTricks, CallOfTheWild, and CheatMenu fixture identities. Their primary binaries/Info files and file counts are unchanged; mutable directory content drifted. CraftMagicItems is absent locally and was not installed or reconstructed.
- The guarded save resolver now uses the current Windows profile rather than obsolete `C:\Users\Howie`. No authorized `KBP_AUTOMATION_BASELINE` / `KBP_AUTOMATION_WORKING` pair exists under the current profile; unrelated saves are not eligible.
- The removed immutable CallOfTheWild example fixture was not recreated. Its profile now binds the exact current live directory identity with unchanged primary DLL/Info hashes so the no-save optional catalog gate can run transactionally.
- Exact later opening run `perf-0.0.11-exact-hud-on-1` passed 1,786 frames / 20.014 seconds, 18 moving samples, 59.559/89.237/90.991 FPS, zero searches, and restored. Native `perf-fix-0.0.11-exact-native-1` passed 12/12; Call of the Wild `perf-fix-0.0.11-exact-cotw-2` passed 26/26; both restored.
- Final local artifact source is `d3af9be4e62ab8aa796e29343ab30b75e918fb8c`; ZIP `eac7dd50afdb8b68f9d3a6577eb7fff9863883b966df8404fed67d623d407d34`; DLL `1bde124702d013c7f66b159963c01a23eef691104ffa5523f9e944498238c4e7`; MVID `cab52ff1-e758-4f8f-ba92-5ad3cd4eb867`. Exact-artifact performance `perf-0.0.11-final-release-2` passed 9/9 at 56.687/89.061/90.873 FPS with zero searches and restoration verified. `final-release-1` is retained as a non-moving startup-bucket threshold failure; all moving buckets were approximately 90 FPS.
- Exact next action: after a distinct authorized KBP save pair is supplied/imported, rebuild the recorded package and run guarded Animated/Instant campaign workflows plus ordinary-area, world-map, and cutscene performance. Do not substitute or rename another save, install, merge, push, or publish.

## 0.0.9 final local handoff — 2026-08-12

- Release source `f026a4a9974af8e4191ff7fb104e472f11c2016f`; package `471e86e0043b47bc899322b640fb448105bfc1689f796b56611ed5d980d4bbe8`; DLL `d66edcacedcfe9d862e5cd433e2e58166abbc5a5a5404b9b7c5d6fd39ae898a1`; MVID `174e2e17-9006-4667-b06d-85d372a2bb77`.
- Exact release-source runs: `ui-polish-0.0.9-exact-animated` 74/74, `ui-polish-0.0.9-exact-instant` 74/74, `ui-polish-0.0.9-exact-native` 12/12, and `ui-polish-0.0.9-exact-cotw` 26/26; all restored exactly.
- Guarded install `ui-polish-0.0.9-final-install` is Installed; settings preserved; every non-planner mod verified unchanged; installed DLL exact. No process or deployment lock remains.
- Exact next action: human completes the 0.0.9 checklist at the top of `docs/MANUAL-ACCEPTANCE.md`, with special attention to native HUD integration and a multi-member indirect `COVERED` portrait preview. No merge, push, or public publication is authorized.

## 0.0.9 exact-package qualification — 2026-08-12

- Exact source `a9f18f88252a30d92bda4dafe1da70099a4fdb73`; local package `703d850025ede3fb501f7581fb911006432c705e40393e85ddc26f03942ea593`; DLL `fd1cc4c664f5d1a04aac7afc3be18b11e15f4fec381ac142cbc98b076b2b88b5`; MVID `b5eceb04-7f6a-4f46-9cbe-2f093a20f285`.
- Exact runs: Animated 74/74, Instant 74/74, native 12/12, Call of the Wild 26/26; every transaction restored exactly. Catalog evidence is 11 provider-backed abilities -> 10 cards with one two-ability/two-provider Bless card.
- Deterministic gates pass source 30/30, behavior 72/72, filesystem 8/8, package 4/4, deployment WhatIf 5/5.
- Exact next action: commit qualification docs, build deterministic local release, then guarded-install over validated 0.0.8 and record final identity/handoff.

## 0.0.9 runtime fixture checkpoint — 2026-08-12

- Runtime preflight found BagOfTricks `Settings.xml` had grown by one byte since the 0.0.8 qualified inventory; every other manifest entry and the primary DLL/Info hashes are unchanged. The human-reproduction profile inventory hash/byte count was refreshed read-only; no third-party file was edited.
- `ui-polish-0.0.9-animated-native-1` used native-only, which intentionally omits mods required by the disposable save; it stopped at campaign-load timeout and restored exactly.
- Exact next action: commit the verified fixture-inventory refresh, rebuild clean, then repeat Animated with `human-reproduction`.

## 0.0.9 polish intake — 2026-08-12

- Branch `codex/ui-source-consolidation` starts from clean documentation HEAD `47d1777`; qualified installed/release 0.0.8 source remains `6e5d02b21e587db84f2c7e7d2a34a63bace3e942`.
- Preserved rollback package SHA-256 `22ce0c0e44c6f6b1f895199e58fe1afe5f639e6b38443e062fd6f4204ec8dbb2` under `artifacts/release-candidate-backups/ui-polish-start-0.0.8-47d1777/`.
- Exact consolidation and migration rule is documented in `planning/UI-POLISH-AND-SOURCE-CONSOLIDATION.md`; this is an effect-semantic aggregate, never a display-name merge.
- Frozen: discovery, planning ranking/resources, Animated/Instant execution, persistence durability, hotkey, HUD hitboxes/listeners/actions/tooltips/pointer ownership, modal lease, and guarded runtime transactions.
- Exact next action: implement the fingerprint/aggregate domain adapter, multi-ability planning request, and legacy assignment rebinding with deterministic tests before visual edits.
- Implementation checkpoint: aggregate cards, multi-ability automatic provider planning, legacy assignment union/round trip, explicit five-state portrait presentation, centered four-column metrics, and styling-only HUD integration are implemented. Tests pass source 30/30, behavior 71/71, filesystem 8/8, package 4/4, deployment WhatIf 5/5.
- Exact next action: commit the bounded implementation, bump to 0.0.9, extend runtime evidence for aggregate counts and portrait/HUD style, then run native, Call of the Wild, Animated, Instant, persistence, input/modal, catalog, package, install, and screenshot qualification.

## 0.0.8 four-column release handoff — 2026-08-12

- Final qualified release source: `6e5d02b21e587db84f2c7e7d2a34a63bace3e942`; version 0.0.8; package `22ce0c0e44c6f6b1f895199e58fe1afe5f639e6b38443e062fd6f4204ec8dbb2`; DLL `593db3bb0ce76316840f94e52d4698c7cd2353bc2aa31610608368478bcdda4b`; MVID `a8265c4e-e37d-4f54-a3e4-ee6578fdefa6`.
- Exact final runs: `ui-grid-0.0.8-final-animated` 72/72, `ui-grid-0.0.8-final-instant` 72/72, `ui-grid-0.0.8-final-native` 12/12, `ui-grid-0.0.8-final-cotw` 26/26. Every runtime transaction restored exactly.
- Final guarded install `ui-grid-0.0.8-final-install` is Installed; settings preserved; all non-planner mods verified unchanged. No merge, push, or publication occurred.
- Final screenshot review caught and fixed stale category/Selected-only visual state before this package. Human visual acceptance remains authoritative; next action is the checklist at the top of `docs/MANUAL-ACCEPTANCE.md`.

## 0.0.8 release-source preparation — 2026-08-12

- Branch `codex/ui-grid-rebuild`; current implementation HEAD before this documentation checkpoint is `8e94b1e3777c072d9069c5fbed18555805a8d87c`; version 0.0.8.
- New shell/grid/direct-target/migration/hotkey/HUD work is complete. Deterministic results: source 30/30, behavior 67/67, filesystem 8/8, package 4/4, deployment WhatIf 5/5.
- Candidate `ui-grid-0.0.8-animated-7` completed the full scenario, produced All/Spells/Abilities/Other 11/2/9/0 and Selected only Long/Important/Long 1/0/1, and restored. Its only failed assertion expected authored 0.960 rather than the actual 8-bit sampled 0.961 red channel; the worktree corrects that evidence value.
- Earlier exact-package candidate runs: Animated 71/71, Instant 71/71, native 12/12, Call of the Wild 26/26; all restored. They are superseded by final documentation-inclusive release-source repetitions.
- No Kingmaker process or unresolved transaction remains. Exact next command after committing this checkpoint: `scripts/Build-Local.ps1`, then final Animated, Instant, native, and Call of the Wild runs against that exact clean commit, followed by deterministic release packaging and guarded 0.0.7-to-0.0.8 installation.

## Four-column UI rebuild intake checkpoint — 2026-08-12

- Mission branch: `codex/ui-grid-rebuild`, created from exact starting HEAD `c2bc534827997fb75c1839a5cee5d1342a860369`; source version remains 0.0.7.
- Preserved qualified release source/package/DLL/MVID: `2f125f9f1024692d83a1b2570209d1858d62eff1` / `9feed6dffa668812ed826c75b743d72892e6e8371b0f81585fb557aea8fcf453` / `bf8c72874377d56f91bcdb6daedaa8b28b340a948aee06583a32954d61b38927` / `966b7d8f-bd5f-46b9-beda-62774f82ccac`.
- Backup directory: `artifacts/release-candidate-backups/ui-grid-rebuild-start-0.0.7-c2bc534`; installed 0.0.7 matches the preserved DLL/MVID.
- Intake safety: Kingmaker/UMM closed; deployment lock absent; unresolved transactions zero. Unchanged baseline passes source 30/30, protocol 63/63, harness 8/8, package 4/4, deployment WhatIf 5/5, aggregate 1/1.
- Human evidence and BubbleBuffs `f4871f763a23251284422ef0945a85e9f3fb788e` were inspected. The authorized delete/retain map and replacement component boundaries are in `planning/FOUR-COLUMN-UI-REBUILD-ARCHITECTURE.md`.
- Exact next action: checkpoint the intake records, then replace the planner presentation from the shell outward with the four-column grid, active-routine direct portrait commands, simplified filters/settings, v3 hidden migration, Ctrl+Shift+B input binding, and HUD-only dark/gold tint specialization.

## 0.0.7 presentation release checkpoint — 2026-08-12

- Final state: qualified release source `2f125f9f1024692d83a1b2570209d1858d62eff1`; package `9feed6dffa668812ed826c75b743d72892e6e8371b0f81585fb557aea8fcf453`; DLL `bf8c72874377d56f91bcdb6daedaa8b28b340a948aee06583a32954d61b38927`; MVID `966b7d8f-bd5f-46b9-beda-62774f82ccac`; installed 0.0.7 exact.
- Exact-release runs: Animated 71/71, Instant 71/71, native 12/12, Call of the Wild 26/26; all restored. Install evidence is `runtime-evidence/install-ui-polish-0.0.7-install/install-result.json`; settings and every other mod were preserved.
- External state: Kingmaker closed, deployment lock absent, no unresolved transaction. No merge, push, or publication occurred.
- Exact next action: human opens installed 0.0.7 and completes the top checklist in `docs/MANUAL-ACCEPTANCE.md`; visual verdict remains authoritative.

- Branch `codex/ui-parchment-bubblebuffs`; current implementation/version HEAD before documentation checkpoint is `bec2addaf302ce6977be7c59c17d3c064feed978`, version 0.0.7.
- Physical final runs `ui-polish-0.0.7-animated-1` and `ui-polish-0.0.7-instant-1` pass 71/71 each and restore. Native final passes 12/12; Call of the Wild final passes 26/26. Baseline remains `afca8ac5e42219bc50f428eb334a657cbcc2e31e8f2eb39c6ab53691cbb076d3`.
- No Kingmaker process, unresolved deployment lock, or unrestored transaction remains. Package is not yet installed and human visual acceptance is intentionally pending.
- Exact next command: commit the release-source documentation, run `scripts/Build-Release.ps1`, record its package/DLL/MVID, guarded-install only 0.0.7 over 0.0.6, verify identity/settings/other mods, then create the documentation-only handoff commit.

## Parchment/BubbleBuffs presentation checkpoint — 2026-08-12

- Status: UI-only Phases A-D are implemented on `codex/ui-parchment-bubblebuffs`; committed Phase C HEAD is `7439e53`, and the Phase D filter/settings/Casting Source refinement is the current four-file worktree change awaiting its checkpoint commit.
- Frozen mechanics remain green after every phase: source `30/30`, protocol/behavior `63/63`, runtime-harness filesystem `7/7`, deployment WhatIf `5/5`, source-only aggregate `1/1`, and package `4/4` where applicable. The latest Phase D development DLL SHA-256 is `079abcaeb2091bb1d7cf57b2583c01adc7940ea18d5d2df1782891649044e241`; package SHA-256 is `727191be376e75e92385b0cd43bb07deddef42d7acb6a979502fcba68c605c6f`.
- Preserved MVP: source `e656812572adea8bc312419372b61ee8c4834e5a`, version `0.0.6`, package `ce7492b262f01a9afb5a7666fe7e4bda9be1821395eb00244f5898b6882208e9`, DLL `6144256c6a0623e908c3d9e821a1b87ee5800195759fbfabb1e587eaf9be1d9b`, MVID `bff11809-aa53-42c2-8ab7-ef3564450e61`.
- Native UI inventory run `ui-polish-0.0.6-native-inventory-2` captured exact 2.1.7b theme candidates and then stopped behind the UMM startup overlay before planner phases. Its guarded transaction was explicitly restored and verified; no Kingmaker process, deployment lock, or unresolved transaction remains.
- Exact next command: rerun the complete Phase D mechanical suite, commit `feat: simplify planner setup controls`, then add presentation-focused live diagnostics/screenshots and begin guarded native campaign qualification.

## Live row-rendering recovery complete — 2026-08-12

- Status: COMPLETE and guarded-installed for human handoff. Qualified release source is `e656812572adea8bc312419372b61ee8c4834e5a`; package `ce7492b262f01a9afb5a7666fe7e4bda9be1821395eb00244f5898b6882208e9`; DLL `6144256c6a0623e908c3d9e821a1b87ee5800195759fbfabb1e587eaf9be1d9b`; MVID `bff11809-aa53-42c2-8ab7-ef3564450e61`.
- Two exact-package fresh processes (`production-3`, `production-4`) passed 71/71 and restored. Screenshot `cb234368...` visibly shows ten production rows and selected Bless details; canary absent; first-five row luminance ranges `162,159,184,163,157`, title `125`.
- Native 12/12 and Call of the Wild 26/26 passed/restored. Deterministic release 2/2 and package 4/4 passed. Guarded install `row-render-0.0.6-install` reports settings preserved and all other mods unchanged; installed CLR is 0.0.6.0; no process or lock remains.
- Final documentation-only checkpoint follows the release source and must not be mistaken for a rebuilt binary. No merge/push/publication is authorized or performed.
- Exact next action: human opens installed 0.0.6 and follows `docs/MANUAL-ACCEPTANCE.md`; report only a visual difference from screenshot hash `cb234368...` or a regression in the preserved HUD/input lifecycle.

## Production run 2 duplicate-gate checkpoint — 2026-08-12

- Run `row-render-0.0.6-production-2` (`877c618`, package `674733b8...`, DLL `642d4df9...`) has 71/71 named assertions PASS, confirmed Bless `1/1/1/1/1`, and production screenshot SHA-256 `cb2343683ebc4d3dfbb066de4b030c1745c518063354a6357a331a6d53d75c19`, but overall status FAIL because a duplicated aggregate condition still required the spent Bless row to remain visible.
- Transaction is restored/verified. The worktree replaces that duplicate with pre-cast Bless row/material evidence and the captured five-row/selection/no-canary/screenshot contract.
- Exact next command: commit, rebuild local 0.0.6, run `row-render-0.0.6-production-3`, then a second fresh-process PASS.

## Canary-free production run 1 checkpoint — 2026-08-12

- Source/package/DLL: `3a4b503` / `9591b25c...` / `78263b64...`. Run `row-render-0.0.6-production-1` restored exactly but overall status is FAIL because one stale final-state assertion required a spent prepared Bless to remain in the available-only list.
- Production visual evidence itself passed: screenshot SHA-256 `d3f271a09152663e1db0bd650706e903c9fd69b2a297433d4b7d819dc78712e2`; ten readable rows; Bless title/details; canary absent; five row pixel ranges 157–184 and title range 125.
- Exact Bless evidence is `require=False,item=none,count=1,hasEnough=False,consumableRequired=False`. Corrected execution outcome is confirmed success with planned/submitted/started/confirmed/spent `1/1/1/1/1`; post-cast spellbook Bless is correctly unavailable as `resource pool exhausted`.
- Worktree changes only the runtime assertion to accept post-cast catalog availability while adding explicit pre-cast screenshot and Bless material-contract assertions. Local source-only suite remains source 30/30, behavior 62/62, harness 7/7, package 4/4, deployment WhatIf 5/5.
- Exact next command: commit, rebuild `0.0.6`, and run `row-render-0.0.6-production-2`.

## Live row production qualification checkpoint — 2026-08-12

- Status: IN PROGRESS. The A/B canary has proved and rechecked the shared viewport-stencil root cause. `row-render-0.0.6-canary-fixed-1` passed 69/69 and restored exactly; visible screenshot SHA-256 is `71a6bbf6ddafd3c5eb25903c223d4052e9a8b63bb5e0e07651c5d18bf12496dd`.
- Current committed HEAD before this worktree checkpoint is `ac5c384af29e4540c2e5eb07a13b54d37add37b2`. Worktree removes the canary, enforces actual requested row/detail heights, records production renderer/screenshot evidence in the runtime result, adds an independent screenshot pixel-contrast gate, and corrects material validation to short-circuit unless `RequireMaterialComponent` is true.
- Exact local result: source 30/30, protocol/behavior 62/62, runtime filesystem 7/7, package 4/4, deployment WhatIf 5/5, source-only 1/1; production build SHA-256 `731489e683b50d78b1f82c74f54ce812608d6463623ffc2f961f394915aa6ea1` (interim build from pre-checkpoint HEAD).
- External state: no retained canary package is installed; the canary run transaction is `Restored` with `restorationVerified=true`; immutable baseline remains `afca8ac5e42219bc50f428eb334a657cbcc2e31e8f2eb39c6ab53691cbb076d3`.
- Exact next command: commit this checkpoint, run `./scripts/Build-Local.ps1`, then guarded `live-ui-bootstrap` with run ID `row-render-0.0.6-production-1`.

## Live row rendering recovery intake — 2026-08-12

- Status: IN PROGRESS. Human evidence invalidates 0.0.5 row/details visibility while preserving HUD icons, tooltips, pointer isolation, F10, opaque modal, and close/input/HUD restoration.
- Branch/HEAD: `codex/kingmaker-buff-planner` / `94cbca8810d908d320eec0a2ca89533c7d4e0e05`; source/installed version remains 0.0.5 at intake. Worktree contains the user-supplied live-row mission and the independent intake record.
- Exact installed identity: package `3eba3158aa92a6b66e249ec35aa297500eb4c5decdf73974c26992219922349c`; DLL `6999284085bd6898f6bd871900783f6f81343a6f801b2d2c95acd208c6513b56`; MVID `d2fed415-bfa2-47a7-90ba-f50fa8d1c7de`.
- Earliest unproven stage: both blank panes share `ScrollRect/Viewport/Mask/Content`; no prior CanvasRenderer, stencil/clip, or pixel evidence exists. Internal active/geometry/intersection values are invalidated as visibility proof.
- External state: Kingmaker/UMM closed; latest runtime transactions restored and verified; no top-level lock; installed 0.0.5 unchanged.
- Exact next command: implement the temporary same-Content render canary plus screenshot/render-chain diagnostics, build an exact diagnostic package, and run one guarded `live-ui-bootstrap` pass against `KBP_AUTOMATION_WORKING`.

## Live row canary root cause — 2026-08-12

- Diagnostic commit/package: `a5e551f4899f3680fc9aa5fbd59283704a8ef121`; package `ad8b26bcc6b7622a1e5002f102e2d33b8d1bc98db5ebecb1a44911a841e64007`; DLL `a3bc89b6be2cdfb7256dd0c9aadddddf6df154865a340606f62fa8561a1e2b5f`.
- Guarded run `row-render-0.0.6-canary-3` passed its existing mechanics and restored exactly. Screenshot `planner-render-canary.png` SHA-256 `4b3f7e05a47d830831582c1d2ff0e99ad14fbdeff51f6b42325784b31a08d886` visibly contains neither canary nor rows/details.
- Exact cause: viewport `Mask` uses `UI/Default` with `AlphaClip=True`, but code sets its source Image alpha to `0.001`; masked children use stencil `Comp:Equal` and therefore render no pixels despite non-culled alpha-1 CanvasRenderers. Both blank panes share this contract.
- External state: canary run and the earlier permission-denied transaction are both restored/verified; installed 0.0.5 remains exact; no process or deployment lock.
- Exact next command: retain the canary temporarily, make the hidden mask source opaque, rebuild, and repeat the guarded canary screenshot to prove rows/details pixels before removing the canary.

## Catalog/HUD R3 complete and installed — 2026-08-12

- Status: COMPLETE for automated acceptance; installed 0.0.5 is ready for authoritative human visual retest. Branch `codex/kingmaker-buff-planner`; qualified release source `390bb8b5f514a38edf1c553962813e29a1b526fd`; documentation-only checkpoint follows it.
- Release identity: package `3eba3158aa92a6b66e249ec35aa297500eb4c5decdf73974c26992219922349c`; DLL `6999284085bd6898f6bd871900783f6f81343a6f801b2d2c95acd208c6513b56`; MVID `d2fed415-bfa2-47a7-90ba-f50fa8d1c7de`; CLR version 0.0.5.0; deterministic builds 2/2; local-only.
- Exact final campaign runs: `catalog-input-0.0.5-five-second-physical-1` and `catalog-input-0.0.5-five-second-physical-2`, 69/69 each. Both show 11 total entries, 10 available view models, 10 instantiated/active rows, 5 viewport-visible rows after final layout, selected details bound, and a visible/active/available spellbook Bless row.
- Physical input evidence is `0/0/0/0/0/True/True/0` for player/movement/ability/selection/target deltas, unchanged selection/camera, and native activation. Tooltip held continuously for 5,010 ms across 344 frames with one new enter, four listeners, zero raycast graphics, `blocksRaycasts=false`, and in-screen bounds.
- Empty Long/Important/Short outcomes are explicit. Configured Bless produced exact refusal `FailedValidation ... material-component-unavailable`, with planned 1, submitted 0, confirmed 0; no success was claimed.
- Native run `catalog-input-0.0.5-five-second-native` passed 12/12; Call of the Wild `catalog-input-0.0.5-five-second-cotw` passed 26/26. All Mods restorations verified. Immutable baseline remains `afca8ac5e42219bc50f428eb334a657cbcc2e31e8f2eb39c6ab53691cbb076d3`; only Working changed through permitted game loads.
- Install `catalog-input-0.0.5-five-second-install` is `Installed`; external settings preserved and every non-planner mod unchanged. No Kingmaker process or unresolved transaction remains.
- Exact next action: human retest installed 0.0.5 using the checklist in `docs/MANUAL-ACCEPTANCE.md`; do not rebuild, merge, or publish this qualified binary.

## Catalog/HUD R3 physical qualification checkpoint — 2026-08-12

- Current HEAD `122e560` on `codex/kingmaker-buff-planner`; version 0.0.5. `catalog-input-0.0.5-live-diag-2` passed/restored and proved 10 live active rows, 4 viewport-visible rows, bound details, and visible/prepared/available Bless, but used the prior synthetic HUD click gate.
- The committed harness now sends OS cursor hover/click events to exact native-canvas HUD centers, monitors tooltip stability for 60 frames, probes world/selection/camera/native control effects, exercises all four HUD controls, selects/configures the live spellbook Bless row, records the confirmed or exact configured outcome, and runs 20 rebuild cycles.
- No process, active lock, or unrestored transaction exists. Installed 0.0.4 remains unchanged.
- Exact next action: build the clean 0.0.5 package, then run `./scripts/Invoke-KingmakerRuntimeTest.ps1 -Scenario live-ui-bootstrap -CompatibilityProfileId human-reproduction -RunId catalog-input-0.0.5-physical-1 -TimeoutSeconds 420 -Confirm:$false`.

## Catalog/HUD R3 first live correction — 2026-08-12

- Current implementation HEAD is `157b1d8`; the first 0.0.5 run `catalog-input-0.0.5-live-diag-1` failed in `Main.Load` because exact `get_InGui` has no `PropertyInfo` metadata row. The direct `GetMethod` correction and callback-first fail-soft ordering pass all local gates.
- Transaction `catalog-input-0.0.5-live-diag-1` is restored and verified after terminating only its owned PID 4148. No Kingmaker process, active lock, or unrestored state remains.
- Exact next action: commit these evidence records, rebuild the clean 0.0.5 package from that commit, and run fresh `catalog-input-0.0.5-live-diag-2`.

## Catalog/HUD input/tooltip R3 implementation checkpoint — 2026-08-12

- Status: IN PROGRESS. Root cause evidence is committed at `f99f0b1`; the first repair implementation is committed at `06ac7177d0b63e4f77ea7e25347c035bc4342cf5` on `codex/kingmaker-buff-planner`; version is `0.0.5`.
- Implemented: end-to-end catalog/filter/layout diagnostics, explicit nonblank catalog states, manual scroll-content sizing, selection/details binding checks, generic provider availability, Bless vertical-slice logging, one cached non-layout/non-raycast/clamped tooltip, independently refreshed quick actions with exact refusal/result logs, and a Harmony12 postfix limited to `PointerController.InGui` while the pointer is inside registered planner rectangles.
- Local gates: source `27/27`, protocol `60/60`, runtime-harness filesystem `7/7`, package `4/4`, deployment WhatIf `5/5`, source-only suite `1/1`; `git diff --check` has no errors.
- External state remains unchanged: Kingmaker/UMM closed; installed 0.0.4 remains exact; no deployment lock or unresolved transaction; immutable baseline remains protected.
- Exact next action: commit this checkpoint record, build the clean 0.0.5 package, then run `./scripts/Invoke-KingmakerRuntimeTest.ps1 -Scenario live-ui-bootstrap -CompatibilityProfileId human-reproduction -RunId catalog-input-0.0.5-live-diag-1 -Confirm:$false` to obtain real-campaign catalog geometry and Bless evidence before strengthening the physical mouse scenario.

## Catalog/HUD input/tooltip R3 intake — 2026-08-12

- Status: IN PROGRESS. Human verdict partially passes 0.0.4 bootstrap/presentation but blocks release on blank catalog/details, tooltip flicker/width, HUD click-to-move, and unhelpful quick actions.
- Starting branch/HEAD: `codex/kingmaker-buff-planner` / `d2aecb00e02a63b5ea33976e34ff1eefc9765a1a`; current version `0.0.4`; user mission plus intake/root-cause records are the intended worktree changes.
- Exact installed/release identity: source `5b96f3b4e713489ce677db3ac5acb83a10f80f01`; package `cb3799e799f641b1a9f7d79eb71942025b5df71a8de956e17369b24fe2f14d16`; DLL `6f72c38ef7e445121291ff2f17f207d49210ea30a2e07fe1105595133b706f1c`; MVID `305a8a6c-2b49-4e3b-a365-286638cbfafa`.
- Earliest catalog failure: after the 11-source normalized model and source/detail bind loops, at unmeasured content/row geometry or clipping. Tooltip and physical-input causes are exact in `planning/CATALOG-HUD-INPUT-TOOLTIP-ROOT-CAUSE.md`.
- Runtime/external state: Kingmaker/UMM closed; no active/non-restored transaction or lock; installed 0.0.4 and protected human profile unchanged.
- Exact next action: add catalog trace and layout evidence, explicit scroll content sizing, single non-layout tooltip, conditional `PointerController.InGui` planner-region capture, ordered quick flow, and physical-input live scenario coverage.

## Live UI bootstrap recovery complete — 2026-08-12

- Status: COMPLETE for the authoritative recovery mission; 0.0.4 is guarded-installed for human visual confirmation. No merge or public release occurred.
- Release source: `5b96f3b4e713489ce677db3ac5acb83a10f80f01`; deterministic package `cb3799e799f641b1a9f7d79eb71942025b5df71a8de956e17369b24fe2f14d16`; DLL `6f72c38ef7e445121291ff2f17f207d49210ea30a2e07fe1105595133b706f1c`; MVID `305a8a6c-2b49-4e3b-a365-286638cbfafa`.
- Real-campaign PASS twice: `bootstrap-0.0.4-human-live-6` and `bootstrap-0.0.4-human-live-7`, 65/65 each. Both prove one active Setup/Long/Important/Short row, F10 armed and physically observed once, visible planner root, 21 balanced open/close cycles, no duplicates, no native/world click-through, restored mode/selection, exact Working load, and immutable baseline `afca8ac5...`.
- Exact HUD object paths/instance IDs/active states/screen centers/world corners are in each `runtime-result.json` under `uiHudObjectEvidence`. Result hashes: run 6 `cd1d22813a7f6a3131b9074b851e13d5a8a2feba7965c8fcc609274d81a05378`; run 7 `4ad411a5536ec25f1fba99138db18ea6fbb5279134d7c2f771e3c46551d6f918`.
- Release regressions: native `bootstrap-0.0.4-release-native-regression` 12/12; Call of the Wild `bootstrap-0.0.4-release-cotw-regression` 26/26; exact restoration for both.
- Guarded install `bootstrap-0.0.4-local-install`: `Installed`; installed DLL/MVID exact; settings preserved; other mods verified unchanged; no Kingmaker process or deployment lock.
- Final records commit is documentation-only and follows the exact installed release-source commit above. No further command is required for this recovery mission.

## Live UI bootstrap campaign checkpoint — 2026-08-12 12:08Z

- End-to-end diagnostic `bootstrap-0.0.4-human-live-5` proved the requested behavior: 1 UI root, 4 HUD buttons/listeners in Setup/Long/Important/Short order, row above native cluster, owned hitboxes, physical F10 armed/observed once, 21 open cycles, 21 balanced closes, opaque 100% planner coverage, zero duplicate roots after close, zero native activation/world input, and exact Working load. Its result is still FAIL because the test captured mode/selection after F10 opened the planner and expected Cheat Menu's primary DLL while UMM loaded its exact `.cache` file. Pre-F10 baseline capture and distinct exact loaded-assembly hash are now fixed; UI behavior itself needs fresh repetition before PASS.
- Third campaign diagnostic: `bootstrap-0.0.4-human-live-4` confirmed correct screen-space hit coordinates, but UMM 0.28.2 `Params.xml` has `ShowOnStart=1` and its still-open `UMM blocking UI/Image` owned every button center. The live harness now writes an exact blocker marker, physically sends Escape to the foreground Kingmaker window, waits for HUD ownership, and only then physically sends F10. Production remains retryable and F10 remains independently armed. Run 4 restored exactly; evidence output-log SHA-256 `93a50f87cad38d89d7ce75aec4a5d3801e69a0da163092c339236cd813c1faff`.
- Second campaign diagnostic: `bootstrap-0.0.4-human-live-3` atomically failed and restored exactly. The out-of-layout row now passes the above-cluster predicate, but hit validation used world center `(-845.1,-505.2)` as `PointerEventData.position`. The current repair converts every HUD button world center through the native raycaster event camera into screen coordinates for both validation and runtime click dispatch. Evidence: `runtime-evidence/bootstrap-0.0.4-human-live-3`; output-log SHA-256 `0a1c31985ba1d27012778537895021b09df4c34aa4c720bcbdee54dfaf1d713a`.
- Transaction recovery update: the first attempt to start `bootstrap-0.0.4-human-live-2` failed before game launch while moving staged Mods into Program Files. Its automatic restore misclassified a bound null PID as a running process. Read-only audit proved live Mods absent, the exact original parked at the owned backup, no process, and matching lock/token; the guarded restore then returned `Restored`, `restorationVerified=true`, exact manifest equality, no lock, and no staging residue. The null-PID filter and regression are now implemented.
- Status: the exact human-reproduction campaign now loads successfully under the guarded harness. The first real-campaign 0.0.4 attempt is a diagnostic failure, not qualification.
- Branch/HEAD before the current repair: `codex/kingmaker-buff-planner` at `db48cd50fb809b7606b6f46b76b4e9f178840c8e`; the three current source edits are intentionally uncommitted until their gates pass.
- Run `bootstrap-0.0.4-human-live-1` proved `Main.Load`, `OnToggle(true)`, first `OnUpdate`, independent F10 arming, retained controller construction, EventBus subscription, five scene/area lifecycle callbacks, exact Working save selection, and campaign load.
- Earliest live failure: the HUD candidate participated in the native `Menu_Buttons48px` layout, which overrode its above-cluster anchor. Exact predicate: `row-not-above-native-cluster:rootBottom=-579.0969;clusterTop=-534.0093`; 77 bounded install attempts remained retryable while F10 stayed armed.
- Current repair: set the HUD root `LayoutElement.ignoreLayout = true`; latch live-scenario exceptions and atomically write one failure result instead of throwing on every UMM update.
- Diagnostic evidence: `C:\Dev\KingmakerBuffPlannerLab\runtime-evidence\bootstrap-0.0.4-human-live-1`; transaction status `Restored`, `restorationVerified=true`; full output log SHA-256 `afd2513c3667802b05ea035ded08d2cab3313bffe8fc9213d032e1745bfb11db`.
- Save safety: baseline remains immutable at `afca8ac5e42219bc50f428eb334a657cbcc2e31e8f2eb39c6ab53691cbb076d3`; mutable Working is now `75519ac954f8cf7a010366af24c03666ae911fb29d57902c1a1c7b0f7cd75414` after the permitted load/write.
- Current local gates: source 24/24, protocol/behavior 60/60, filesystem harness 6/6, package 4/4, deployment WhatIf 5/5, build 1/1.
- Exact next action: commit the out-of-layout HUD repair, rebuild the clean commit, and run `bootstrap-0.0.4-human-live-2` from a fresh process.

## Live UI bootstrap recovery checkpoint — 2026-08-12

- Status: 0.0.3 is human-failed (`UMM active, but live campaign HUD controls absent and F10 unregistered/nonfunctional` from the user's perspective); 0.0.4 repair is source-complete and awaiting clean-commit live qualification.
- Branch/intake: `codex/kingmaker-buff-planner`; forensic HEAD `d069fffb788147de3c76f2bd0d752f7b2db20f3d`; release source `d5a20aa7ddbb2ec7d131a4bed44f1ca65ecaaa65`.
- Exact failed identity: installed/release DLL `5d95368ee237e658e06b4948209f805568a417ea150eb36c3023df9b155f0950`, MVID `f3f691a4-d691-4112-90a4-7beb9f06aad2`, package `42f823d6b8454ffe4497f4f652752a07d50738d5990c5a5243d091ba92d363e0`.
- Root cause: both retained UI paths synchronously demanded EventSystem top-hit ownership in their graphics-creation frame. The live modal was active, opaque, 1280x720, and raycaster-backed, but its same-frame hit list was empty. The screen rolled back and the HUD silently destroyed its row through the identical timing assumption.
- Rejected loader theories: logs and UMM 0.28.2 IL prove `Main.Load`, callback assignment, `OnToggle(true)`, `OnUpdate`, retained controller construction, and the F10-originated screen attempt occurred. Production applies zero Harmony patches; 0.0.3 had no persistent scene/area observer.
- Repair: `[KBP-BOOT]` lifecycle/exception diagnostics; F10 polling directly in `Main.OnUpdate`; UMM diagnostics panel; EventBus scene/area observer; two-frame readiness gates; retryable exact HUD/modal failures; one retained/disposed controller; save-backed `live-ui-bootstrap` scenario with physical F10 delivery.
- Authorized saves proven present: baseline `Manual_296_KBP_AUTOMATION_BASELINE.zks` SHA-256 `afca8ac5e42219bc50f428eb334a657cbcc2e31e8f2eb39c6ab53691cbb076d3`; working `Manual_297_KBP_AUTOMATION_WORKING.zks` SHA-256 `961c4721d31de5740416ae3c864e63351f6916cf61a0b4327094701f5579e1b2`; game ID `3d556254-8ba9-4e9f-8d11-755eecd0b661`.
- Current gates: source validation 23/23, behavior/protocol 59/59, runtime harness 6/6, package 4/4, deployment WhatIf 5/5. No live claim yet.
- Exact next command after checkpoint commit and clean build: `./scripts/Invoke-KingmakerRuntimeTest.ps1 -Scenario live-ui-bootstrap -CompatibilityProfileId native-only -RunId bootstrap-0.0.4-native-live-1 -Confirm:$false`.

## R2 installed handoff — 2026-08-12

- Status: validated 0.0.3 is guarded-installed for authoritative human campaign retest; automated campaign UI and save-backed execution are not claimed.
- Branch/release source: `codex/kingmaker-buff-planner`; release commit `d5a20aa7ddbb2ec7d131a4bed44f1ca65ecaaa65`; evidence-record checkpoint `fc94060a861db6356a5bdb8d2520f377ec52b0c5` was guarded-pushed and remote-verified.
- Release/install identity: package `42f823d6b8454ffe4497f4f652752a07d50738d5990c5a5243d091ba92d363e0`; DLL `5d95368ee237e658e06b4948209f805568a417ea150eb36c3023df9b155f0950`; MVID `f3f691a4-d691-4112-90a4-7beb9f06aad2`; installed version `0.0.3`.
- Gates: source 21/21, behavior 57/57, harness 6/6, package 4/4, deploy WhatIf 5/5, install WhatIf 5/5; native runs `r2-0.0.3-release-native-1/2` 12/12; Call of the Wild `r2-0.0.3-release-cotw-1/2` 26/26; all restoration exact.
- Campaign UI boundary: `r2-0.0.3-release-ui-boundary` is correctly `BLOCKED` at `campaign-ui-unavailable`; no authorized `KBP_` save exists. This is not a UI/input/Bless pass.
- Install state: `r2-0.0.3-local-install` is `Installed`; other mods verified unchanged, settings preserved, profile SHA-256 still `3723e3181c56bff6427a15b2ba85ffd76fd40e98f3f482253b15910f038d6b48`; no Kingmaker/UMM process or deployment lock.
- Persisted Long: Bless ability `90e59f4a4ada87243b7b3535a06d0638`, target `8d7086b2-a4d5-43d5-aed6-51c789971b53`, expected fact `87b8c6270ea85c743afc734dfe99afee`; no prior provider preference or migration. The next live run supplies the actual provider/outcome diagnostics.
- Exact next action: run the mandatory campaign checklist in `docs/MANUAL-ACCEPTANCE.md` against installed 0.0.3; record the human verdict and exact provider/Bless outcome. Do not run automated save-backed qualification unless an authorized `KBP_AUTOMATION_BASELINE` / `KBP_AUTOMATION_WORKING` pair exists.

## R2 correction checkpoint — 2026-08-12

- Status: R2 CORRECTION IN PROGRESS; installed 0.0.2 failed direct human playtesting.
- Active mission: `planning/HUD-MODAL-EXECUTION-R2-CORRECTION-MISSION.md`; its human observations supersede the R1 UI-complete result.
- Exact repository state at intake: branch `codex/kingmaker-buff-planner`, HEAD `a1e30a8d9e55eef3aac959e66039fe6ab0d578f3`; only the user-supplied R2 mission was untracked before durable intake edits.
- External state: installed DLL `c2598e0d31e464eaf8446e15280cbe13b3eeb4e56b0de92e20cc8f29fb458e84`, MVID `e43f060b-a2b7-48db-b19f-b45704ef77c4`; no game/UMM process, deployment lock, unresolved transaction, or unrestored external state.
- Profile state: schema-2 profile preserves one Long Bless assignment (`90e59f4a4ada87243b7b3535a06d0638`) to unit `8d7086b2-a4d5-43d5-aed6-51c789971b53`; expected fact `87b8c6270ea85c743afc734dfe99afee`; no provider preference saved and no migration ran.
- Confirmed failures: native hierarchy cloning leaves uncontrolled hit geometry and disables visible-icon raycasts; modal input lease precedes all presentation proof; queued animated commands are called fired and can be reported complete without expected facts.
- Exact next command: implement the fresh retained-mode HUD row and transactional presentation-first screen lifecycle, then run focused behavior tests before execution-outcome changes.

Status: INSTALLED FOR HUMAN UI RETEST — campaign UI and save-backed qualification pending

- Repository/branch: standalone Kingmaker Buff Planner / `codex/kingmaker-buff-planner`
- Starting repair HEAD: `ec153837401c8815b1909cb15e85ab658a1ee26a`; release commit: `447bbd288c803a4aec609db84a4c6076cbfe94f3`; final records checkpoint pending
- Development/installed version: `0.0.2`; installed DLL SHA-256 `c2598e0d31e464eaf8446e15280cbe13b3eeb4e56b0de92e20cc8f29fb458e84`
- Worktree: final install/release records only
- Authoritative repair mission: `planning/FULLSCREEN-UI-INPUT-ISOLATION-REPAIR-MISSION.md`
- Confirmed root cause: the production UI is fixed-coordinate IMGUI, while exact Kingmaker `PointerController.InGui` only recognizes EventSystem pointer-over-GameObject state. No native full-screen mode or raycast surface is acquired.
- Exact native evidence: `Assembly-CSharp.dll` SHA-256 `3b6450ffec440e296e586f71c711b195aed144b28d53e1cbb29406d18fef5afb`, MVID `07fa1e4d-8618-41b3-9b8d-faa17d3b26f7`; full-screen event enters/stops `GameModeType.FullScreenUi`; native service canvas blocks raycasts and hides/restores HUD.
- Invalid prior gate: it hardcoded routine count/layout and required zero blockers/subscriptions without dispatching real UI/world input.
- Long finding: an empty/zero-step routine reports only inside the closed setup window, so HUD execution can appear silent even if its coroutine ran.
- Runtime state: Kingmaker/UMM closed; no deployment lock; live `Mods\KingmakerBuffPlanner` is guarded-installed validated 0.0.2; all non-planner mods verified byte-identical; no save accessed.
- Separate existing hard stop: no authorized `KBP_AUTOMATION_BASELINE` / `KBP_AUTOMATION_WORKING` pair; this does not block UI-only no-save repair qualification.
- Last runtime gates: native `ui-repair-0.0.2-final-native-1/2` 12/12 each and Call of the Wild `ui-repair-0.0.2-cotw-1/2` 26/26 each, exact restoration. Corrected UI gate rejected main-menu-only evidence and restored exactly.
- Exact release: package `1f328e26fdf2524fc85e8482077e5330bc5f7a48ce0d8841bd372997685d652f`, DLL `c2598e0d31e464eaf8446e15280cbe13b3eeb4e56b0de92e20cc8f29fb458e84`, MVID `e43f060b-a2b7-48db-b19f-b45704ef77c4`, commit `447bbd288c803a4aec609db84a4c6076cbfe94f3`.
- Install evidence: `C:\Dev\KingmakerBuffPlannerLab\runtime-evidence\install-ui-repair-0.0.2-local-install\install-result.json`.
- Exact-release runtime: native `ui-repair-0.0.2-release-native-1/2` 12/12; Call of the Wild `ui-repair-0.0.2-release-cotw-1/2` 26/26; UI boundary `ui-repair-0.0.2-release-ui-boundary` structured `BLOCKED` because no campaign fixture; all restored exactly.
- Exact next command: commit these final evidence records, rerun guarded push, then hand off the installed build for authoritative human playtesting.
# Parchment UI polish intake checkpoint — 2026-08-12

- Branch/HEAD: `codex/ui-parchment-bubblebuffs` / `21c4dd702868e5cfc89963ba50bb64420f18915d`; source version 0.0.6; presentation source is not yet changed.
- Qualified MVP: release source `e656812572adea8bc312419372b61ee8c4834e5a`; package `ce7492b262f01a9afb5a7666fe7e4bda9be1821395eb00244f5898b6882208e9`; DLL `6144256c6a0623e908c3d9e821a1b87ee5800195759fbfabb1e587eaf9be1d9b`; MVID `bff11809-aa53-42c2-8ab7-ef3564450e61`. Backup is under `artifacts/release-candidate-backups/mvp-0.0.6-21c4dd702868e5cfc89963ba50bb64420f18915d/`.
- Intake gates: source 30/30, behavior 62/62, runtime filesystem 7/7, deployment WhatIf 5/5, package 4/4; actual-version installer WhatIf passed without mutation. The wrapper's stale expected prior version 0.0.2 is the only baseline command failure.
- External state: installed 0.0.6 DLL is exact; Kingmaker is closed; no deployment lock or unresolved transaction exists; no live file was changed.
- Forensics: `planning/PARCHMENT-BUBBLEBUFFS-UI-FORENSICS.md` records human screenshots, BubbleBuffs commit/source/provenance, adapted concepts, prohibited assets/code, and the pending exact Kingmaker inventory.
- Frozen MVP behaviors and guarded runtime boundaries remain authoritative.
- Exact next command: implement and test a diagnostic-only expansion of `NativeUiContractProbe`, then clean-build/package and run one guarded `live-ui-bootstrap` inventory capture against `KBP_AUTOMATION_WORKING` before changing the presentation.

## Phase A checkpoint — 2026-08-12

- Current branch is `codex/ui-parchment-bubblebuffs`; inventory commits are `2b3e281` and `28b79c8`; Phase A source is currently uncommitted.
- `ui-polish-0.0.6-native-inventory-2/native-ui-contract.json` is the exact Kingmaker 2.1.7b campaign inventory. Its run timed out at the UMM overlay before HUD readiness, then restored exactly. No lock/process remains.
- The theme shell now uses role tokens and exact optional Kingmaker paths with centralized fallbacks; presentation hierarchy and behavior are unchanged. Mechanical regression is fully green.
- Exact next command: commit Phase A, add/test `BuffCardViewModel` and icon resolver, render icon-first cards, then rerun `./scripts/Test-SourceOnly.ps1` before Phase C.

## Phase B checkpoint — 2026-08-12

- Phase A commit: `1e0b5ed`. Phase B icon-first card/view-model source is uncommitted and passes source 30/30, behavior 63/63, harness 7/7, deployment WhatIf 5/5, Release build 1/1, package 4/4.
- Catalog cards resolve actual blueprint icons without changing discovery data and use a stable centralized neutral fallback. Player-facing state is derived from the existing model/profile only.
- External state is restored and idle; installed qualified MVP 0.0.6 remains unchanged.
- Exact next command: commit Phase B, implement Phase C portrait legality/fulfillment/hover and selected detail hierarchy, then run the complete mechanical suite.

## Phase C checkpoint — 2026-08-12

- Phase B commit: `c51b6c0`. Phase C source is uncommitted and mechanically green: source 30/30, behavior 63/63, harness 7/7, deployment WhatIf 5/5, build 1/1, package 4/4.
- Portrait status/hover is presentation-only; bulk changes use the existing assignment identity/list and save once. Selected details and plans use player language.
- External runtime remains restored and idle; installed qualified MVP is unchanged.
- Exact next command: commit Phase C, implement Phase D primary/advanced filters, one Settings mode control, readiness summaries, collapsed Casting Source, then rerun all mechanical gates.
# 0.0.10 visual-clarity release-source checkpoint — 2026-08-13

- Branch `codex/ui-clarity-presentation`; implementation candidate HEAD `73de462b885bc7b24162d8670bc2be1b806baf37`; version 0.0.10. The next commit contains only required durable qualification records.
- Candidate exact identity: package `19683ab7a01c9e583e8d2b8cd3f790b6ce6ca03284ab324fd42e4b035cf3da1b`; DLL `92b2dcf09937c3d277af5e5235310e8a28c9737400911baa36535e7b88d47444`; MVID `0c5f4132-baed-4a4b-90c5-0b5764125224`.
- Final candidate runs: `ui-clarity-0.0.10-final-animated-1` 77/77, `ui-clarity-0.0.10-final-instant-1` 77/77, `ui-clarity-0.0.10-final-native-1` 12/12, `ui-clarity-0.0.10-final-cotw-1` 26/26. All transactions are Restored/verified.
- Deterministic gates: source 32/32; behavior 75/75; filesystem 8/8; package 4/4; deployment WhatIf 5/5. Current tree has no unresolved runtime transaction or Kingmaker process.
- Active physical rendering: 1280x720, HUD alignment/glyph centering/hit ownership true, native activation zero; Arial/UI-Default, unit scale, no planner scaler, zero fractional rendered text/card transforms. Full screenshots are under the Animated run.
- Runtime limitation: the authorized one-member save cannot exhibit a second indirect-covered or controlled invalid portrait. No hardcoded/fake evidence was introduced; deterministic plan tests cover it and human acceptance remains open.
- Exact next command after committing this checkpoint: `.\scripts\Build-Local.ps1`, then exact-source final runtime repetitions if the resulting commit/hash changes, `.\scripts\Build-Release.ps1`, guarded `.\scripts\Install-Local.ps1`, exact identity/other-mod verification, and a documentation-only handoff commit.
# 0.0.10 installed handoff — 2026-08-13

- Engineering/runtime/package/install work is complete. Final release source `14719e816c31d4efadf829733d499774c6f5e741`; version 0.0.10; package `46d741b7dd16120e5687069c215a5cc270b9ec1e8430fd764dd36a2bdb05f013`; DLL `bcd6ed91e4d6898dec74e69f501389adb15728cd03fe0a0915522e5a1a18c55e`; MVID `8bb28075-83cf-41d2-ad3d-e883886c4961`.
- Exact runs `ui-clarity-0.0.10-exact-animated` 77/77, `ui-clarity-0.0.10-exact-instant` 77/77, `ui-clarity-0.0.10-exact-native` 12/12, and `ui-clarity-0.0.10-exact-cotw` 26/26 all restored exactly.
- Install `ui-clarity-0.0.10-final-install`: Installed; settings preserved; all non-planner mods verified; exact installed DLL/MVID; no lock/process remains.
- Exact next action: human executes `docs/MANUAL-ACCEPTANCE.md` 0.0.10 at 1920x1080 and 1600x900, especially indirect/invalid portraits in a multi-member party. No merge, push, or publication is authorized.
