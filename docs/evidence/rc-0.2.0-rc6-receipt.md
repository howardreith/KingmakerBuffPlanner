# Release candidate 0.2.0-rc6 — receipt (2026-09-24)

The exact candidate for the owner's final review. Local only: nothing is
published, tagged or permanently installed, and the pull request stays a
draft. It supersedes 0.2.0-rc5 (`27234a4`, superseded after the advanced
qualification of an Extend rod found that a rod the player had left
switched on was spent on a casting that did not choose it); the rc5, rc4,
rc3, rc2 and rc1 receipts and frozen packages stay unchanged as history.

## Identity

| Item | Value |
| --- | --- |
| Commit | `24d9967f82e516f9ed4a4c1f06e6a842e18c5f96` |
| Version | 0.2.0-rc6 (assembly 0.2.0.0) |
| Package | `f271f3e63e81cb59eb2c715887b351914c95acda10fdbe7581ef3ad36833d9b4` (the release ZIP and the harness package are byte-identical) |
| DLL SHA-256 | `35d6cbb2b457374a69ac93227e1af7dcc0bea974e0c1fd1a724839a92071ea07` |
| MVID | `4cb12c0d-78c6-4755-aa62-b6ad50e1a02b` |
| Frozen copy | `C:\Dev\KingmakerBuffPlannerLab\runtime-backups\rc-frozen\24d9967f82e516f9ed4a4c1f06e6a842e18c5f96\` (read-only, `FREEZE.json`, frozen 2026-09-25T01:31:05Z) |
| Clean checkout | `C:\Dev\KingmakerBuffPlannerLab\repo\KingmakerBuffPlanner-RC6` (detached at the commit) |
| Release artifact | `...\KingmakerBuffPlanner-RC6\artifacts\release\0.2.0-rc6\` (`KingmakerBuffPlanner-0.2.0-rc6.zip`, `release-manifest.json`, `RELEASE-NOTES-DRAFT.md`) |

Reproducibility: two deterministic Release builds in `KingmakerBuffPlanner-RC6`,
one in `KingmakerBuffPlanner-RC6-repro` and the gate build in
`KingmakerBuffPlanner-G2` (three differently named checkouts) produced the
same package, DLL and MVID.

Later commits on the branch after `24d9967` are receipts and handoff
documents only; they change no source, script, test or version file.

## What changed since rc5

In `docs/RELEASE-NOTES-DRAFT.md` (for a player) and, finding by finding
with commits, in `docs/CASTING-FIRST-REVIEW-INDEX.md`. In short:
- casting-first castings apply exactly the enhancements they chose: a
  rod, Powerful Change or Share Transmutation the player left switched on
  is switched off for the cast and restored after (a rod the game keeps
  running is stopped at once; anything else still running refuses the
  cast); the copy of a chosen rod already running is the one used;
- the qualification tooling: the `ability-pool-direct` and
  `rod-extend-direct` recipes, the game clock and running state in the
  published reads, and the stricter plain-buff check.

The development evidence that found the rod defect and qualified the fix
is `docs/evidence/next-iteration-20260924-receipt.md`.

## Reviews and mutation

- Two independent read-only reviews: of the next-iteration diff since rc5
  (four findings), and of the exact-enhancements fix (five findings, no
  Classic change, no exit path leaving a player's toggle off). Every
  finding and its disposition is in `docs/CASTING-FIRST-REVIEW-INDEX.md`.
- Mutation of the rc6 work: ability pool 8 (6 killed; 2 second checks of
  rules earlier checks enforce), rod 7 (all killed), review fixes 15 (all
  killed, including the executor exactness in both modes and the Classic
  contrast), the rod recipe's duration boundary 2 (both killed). The
  game-side parts of the fix (switching toggles, stopping a rod, reading
  running state) cannot run without the game; the live qualifications
  judge them. Mutants show that the tests notice a broken guard; they are
  not evidence of behaviour in the game.

## Source tests at the candidate

Full gate at `24d9967` (Windows PowerShell 5.1, non-interactive, clean
tree): source validation 42/42, protocol 338/338, runtime harness 38/38,
package 4/4, deployment WhatIf 5/5, launcher WhatIf 12/12, fixture
inventory 3/3, Restore-InstallLocal 16/16, guarded publisher 3/3.

## Game runs on the frozen build

Run one after another from the RC6 checkout by the evidence chain
(`scratchpad/rc6_chain.ps1`), with no retry. Each run:
- loaded the exact build it staged (commit, package, DLL and MVID checked
  in the game);
- read the save folder and kept its baseline once it held the deployment
  lock;
- compared every save clean before releasing the lock;
- restored the Mods folder byte-exact.

The Gunslinger development session shares the installation; each side
ran only while the other's lock was released, and messaged before and
after each batch.

**Automation campaign** (the disposable automation fixture; party
Hedwirg, Linzi, Tartuccio):

| Run | Result | What it showed |
| --- | --- | --- |
| `classic-select-20260924-rc6-inst-01`, `-anim-01` | PASS, complete | The Classic (default) planner's Long routine recorded with its digest in each mode; nothing cast |
| `classic-cast-20260924-rc6-inst-01`, `-anim-01` | PASS, complete | Resistance from Linzi's level-0 spellbook entry, resolved to the at-will cantrip ability, confirmed by a new effect instance, free; the single-use grant consumed once; Classic unaffected by the casting-first change (its steps are not exact) |
| `casting-qual-select-20260924-rc6-01` | PASS, complete | Zero-cost recipe selected and forecast; an allowance written for each mode, bound to this build |
| `casting-qual-cast-20260924-rc6-anim-01`, `-inst-01` | PASS, complete | Every step as forecast in both modes: stop (the player's press in flight), complete, repeat (refused), recast, disable (in flight for Animated, before the step for Instant, labelled so), recover; zero violations. Casting-first steps are now always prepared; nothing changed for this party |
| `casting-ws-import-20260924-rc6-01` | PASS, complete | A genuine schema-4 (0.0.19) Classic file imported on first open: migrated, the Classic file unchanged and archived |
| `casting-ws-qual-20260924-rc6-1080-01` | FAIL (black frames), restored and clean | The owner's session showed as connected for a moment at 22:43, so the chain started this frame-judged run; it disconnected again at once and every frame was black (blackFraction 1.0). The build identity checks and every assertion that does not judge frames passed, restoration was verified and the saves compared clean. Not a product result; not repeated while disconnected. The chain now starts a frame run only after the session has stayed connected for two minutes |
| `casting-ws-reload-20260924-rc6-01`, layout at 1920x1200, manual-session rehearsal | Deferred | They judge game frames, which render black while the owner's session is disconnected |

**Advanced campaign** (the owner's `KBP_ADVANCED_SEED`, a disposable
*Beneath the Stolen Lands* fixture: Cleric, Alchemist, Fighter and Brown
Fur Transmuter at level 9; see
`docs/evidence/advanced-fixture-20260924-receipt.md`; not proof of
main-campaign-specific behaviour). The Transmuter carries two copies of
one Extend rod, the second switched on in the save; every casting-first
cast now switches it off unless the casting chose it.

| Run | Result | What it showed |
| --- | --- | --- |
| `advanced-inspect-20260924-rc6-01` | PASS, complete | Non-casting inspection: Tenebrous Depths I, party level 9, the full capability inventory |
| `casting-qual-select-20260924-rc6-adv-finite-01` | PASS, complete | `finite-direct-mixed`: Protection from Alignment — Chaos from the Transmuter's spontaneous level 1 (on the Alchemist) and the Cleric's exact prepared slot (on the Fighter) |
| `casting-qual-cast-20260924-rc6-adv-finite-anim-01`, `-inst-01` | PASS, complete | In both modes: stop (spontaneous 6 to 5; the slot untouched), complete (the first skipped, pool unchanged; the exact slot spent), repeat (refused `nothing-to-cast:2`), recast (spontaneous 5 to 4); 3 submissions |
| `casting-qual-select-20260924-rc6-adv-group-01` | PASS, complete | `group-mixed`: a direct prime on the Fighter; the Communal form centred on the Cleric with the Fighter pre-covered; a target-anchored communal spell from the Transmuter |
| `casting-qual-cast-20260924-rc6-adv-group-anim-01`, `-inst-01` | PASS, complete | In both modes: prime (one prepared slot 1 to 0); mixed (the Communal cast reached all four members for one invocation and one slot; the anchored cast all four for one spontaneous level-3 use, 5 to 4); 2 submissions |
| `casting-qual-select-20260924-rc6-adv-enh-01` | PASS, complete | `enhanced-direct`: Bull's Strength from the Transmuter's spontaneous level 2, plain on the Cleric, then with Powerful Change: Strength on the Alchemist; route `ProviderDirectRuleCast` |
| `casting-qual-cast-20260924-rc6-adv-enh-anim-01`, `-inst-01` | PASS, complete | In both modes: plain Strength +4, Arcane Reservoir 16 to 16; enhanced Strength +6, reservoir 16 to 15; both about 600 s (the rod left on no longer extends them); every switch and running state of the Transmuter's 17 toggles as before |
| `casting-qual-select-20260924-rc6-adv-ability-01` | PASS, complete | `ability-pool-direct`: the Alchemist's Mutagen (Strength), one use a day, on the Alchemist |
| `casting-qual-cast-20260924-rc6-adv-ability-anim-01`, `-inst-01` | PASS, complete | In both modes: use (the pool 1 to 0, a new Mutagen buff); repeat (nothing to cast); *Always recast* refused before casting (`resource-pool-exhausted`), the game's count 0 and the buff unchanged; 1 submission |
| `casting-qual-select-20260924-rc6-adv-rod-01` | PASS, complete | `rod-extend-direct`: Blur from the Transmuter's spontaneous level 2, plain on the Cleric, then with the Extend rod chosen on the casting on the Alchemist |
| `casting-qual-cast-20260924-rc6-adv-rod-anim-01`, `-inst-01` | PASS, complete | In both modes: plain 539.6 s / 540.0 s, rod charges 6 to 6 (the rod left on was switched off for it); with the rod 1079.6 s / 1080.0 s (twice), charges 6 to 5; the same strength; every switch and running state as before (the rod left on was on and running again) |

## Install and rollback of the exact artifact

Temporary install `rc6-temp-deploy-20260924-01` of the frozen release
package through the real installer, then the real rollback (evidence
`runtime-evidence\install-rc6-temp-deploy-20260924-01`,
`rollback-rc6-temp-deploy-20260924-01` and
`rc-deploy-rc6-temp-deploy-20260924-01\deploy-check.json`):

- Local install PASS: 0.2.0-rc6, package `f271f3e6...`, DLL `35d6cbb2...`;
  no other mod changed.
- Install rollback PASS: 0.1.1-rc3 restored exactly (DLL `78407dd4...`);
  the Mods folder equal to its state before the install (1117 entries).
  The owner's normal installation was checked again afterwards:
  KingmakerBuffPlanner 0.1.1-rc3, DLL `78407dd4...`.

## Artifact identity after merge

The tested candidate is the frozen package, not the source. A merge
creates a new commit, and the build embeds its commit, so a rebuild from
the merged branch produces a different DLL and MVID; that rebuild is not
the tested candidate even when the source tree is identical. Merging with
a merge commit (not a squash or rebase) keeps `24d9967f82e516f9ed4a4c1f06e6a842e18c5f96` in `main`'s
history, so the frozen package's manifest keeps naming a reachable commit.

To install exactly what was tested, install the frozen release package by
its hash (no rebuild):

```powershell
# From the candidate's clean checkout (C:\Dev\KingmakerBuffPlannerLab\repo\KingmakerBuffPlanner-RC6):
.\scripts\Install-Local.ps1 -ReleaseManifestPath .\artifacts\release\0.2.0-rc6\release-manifest.json `
    -InstallId <id> -ExpectedPriorVersion <the installed version>
```

Post-install check (it must print `True` twice):

```powershell
$dll = 'C:\Program Files (x86)\Steam\steamapps\common\Pathfinder Kingmaker\Mods\KingmakerBuffPlanner\KingmakerBuffPlanner.dll'
(Get-FileHash $dll -Algorithm SHA256).Hash.ToLowerInvariant() -eq '35d6cbb2b457374a69ac93227e1af7dcc0bea974e0c1fd1a724839a92071ea07'
[Reflection.Assembly]::ReflectionOnlyLoadFrom($dll).ManifestModule.ModuleVersionId.ToString() -eq '4cb12c0d-78c6-4755-aa62-b6ad50e1a02b'
```

Rollback, keeping settings edited since:

```powershell
.\scripts\Restore-InstallLocal.ps1 -InstallId <id>
```

If a build from a merged commit is to be released instead, it is a new
candidate:
1. Confirm the source is unchanged: `git diff 24d9967f82e516f9ed4a4c1f06e6a842e18c5f96 <merged> -- src
   scripts tests Version.props` must be empty.
2. Build it with `Build-Release`.
3. Freeze it.
4. Repeat the candidate's native set on that build: Classic select and
   cast in both modes, the zero-cost selection and both casting modes,
   the import, the advanced inspection and the finite, group, enhanced,
   ability-pool and rod qualifications in both modes, and the temporary
   install and rollback (plus the frame-judged runs once a connected
   session is available).

A matching source diff alone does not make the new bytes tested.

## Found after the freeze

- **The capability inventory prints an unmodeled action's type as `0`.**
  The adapter names an action type with its assembly and version
  (`...ContextActionRemoveBuff, Assembly-CSharp, Version=0.0.0.0`), and the
  inventory kept what follows the last dot, so rc6's live inventories read
  `empty!restorative-action:0` where the documents show
  `empty!restorative-action:ContextActionRemoveBuff`. The category (why
  the action is unmodeled) is right; only the type name is lost. This is
  diagnostic text in the evidence; nothing reads it back, and no planner
  or qualification decision depends on it. The source test used names
  without the assembly suffix. Fixed on the next-iteration branch
  (`caea6ea`, local), with the test now using the adapter's own format.
- **Ability pools with more than one use cannot be qualified on the
  advanced campaign.** The party's only multi-use pool whose effect is a
  plain buff is the Cleric's Agile Feet (9 uses a day), and it lasts one
  round: it would expire between the use, repeat and recast steps. The
  Mutagen (one use) remains the pool evidence.

## What is not yet shown

- **Main-campaign-specific behaviour.** The advanced evidence comes from
  a *Beneath the Stolen Lands* fixture the owner built for testing.
- **Pets** (none in either party), **ability pools with more than one
  use** (the advanced party's only plain one lasts a round; see above)
  and **metamagic spell variants** (no metamagic spell is prepared on
  either campaign): implemented and source-tested, not proven in the
  game.
- **An actual area transition.** Every exit of both test campaigns writes
  an autosave before leaving (the automation campaign's only exit also
  ends the prologue; both exits of the advanced campaign's *Tenebrous
  Depths, I* autosave with the game's autosave on). No route is safe under
  the owner's terms, and none was attempted. The lifecycle was qualified
  with the production paths instead: the player's stop in flight, disable
  during a run, re-enable and a new accepted run.
- **Frame-judged runs** (deferred): the in-game reload, the workspace
  layout at 1920x1200 and 1920x1080, the manual session rehearsal, and
  physical keyboard and mouse input. They need the owner's session
  connected; the commands are in `docs/MANUAL-USABILITY-HANDOFF.md`.
- **The supervised manual session** and the owner's verdict on the
  interface, including whether the Classic planner closing on APPLY is
  wanted.
- **Owner decisions:**
  - the Classic planner and toggles left on: when a buff chooses no
    enhancement, Classic leaves the game's toggles as they are, so a rod
    left switched on applies to every eligible Classic cast and spends its
    charges (unchanged from 0.0.19; casting-first now casts exactly what
    each casting chose). Recommendation: the casting-first rule;
  - worn-item enchantments (Magic Weapon, Magic Fang) are recast under
    skip-if-active, because their strength cannot be read (the owner's
    rule);
  - merge, release and permanent installation.

## Next iteration (bounded)

1. With the owner's session connected: the deferred frame-judged runs
   from the RC6 checkout, then the supervised manual session.
2. The owner's calls above. For worn-item enchantments, the game exposes
   a temporary enchantment's `IsTemporary`, `EndTime` and `Context` (with
   its caster level), so reading them would let skip-if-active skip one
   that is provably as strong and long (casting-first only; Classic reads
   presence as before). For Classic, the exact-enhancement rule is one
   flag on its steps plus its Classic qualification.
3. An ability pool with more than one use, on a fixture where one lasts
   longer than a round.
4. An area transition, only if a test campaign offers an exit that writes
   no save, or after the owner's decision about the autosave setting.

