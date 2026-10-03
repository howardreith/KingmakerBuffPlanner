# Release candidate 0.2.0-rc5 — receipt (2026-09-24)

The exact candidate for the owner's final review. Local only: nothing is
published, tagged or permanently installed, and the pull request stays a
draft. It supersedes 0.2.0-rc4 (`863a182`, superseded after the owner's
review of its source); the rc4, rc3, rc2 and rc1 receipts and frozen
packages stay unchanged as history.

## Identity

| Item | Value |
| --- | --- |
| Commit | `27234a445e95f2fc399857a880c35de0f397359f` |
| Version | 0.2.0-rc5 (assembly 0.2.0.0) |
| Package | `a05f1a83515cb48ea50c8147d72fdbfd8416bd615f65fe89e6c73ab4a1a2629a` (the release ZIP and the harness package are byte-identical) |
| DLL SHA-256 | `785e1b7946d5082b3a6bb944156ffeb31c1dd1b7eaf1045e383516776c6ade48` |
| MVID | `171a1599-fef3-419b-b748-2833a10f9d75` |
| Frozen copy | `C:\Dev\KingmakerBuffPlannerLab\runtime-backups\rc-frozen\27234a445e95f2fc399857a880c35de0f397359f\` (read-only, `FREEZE.json`, frozen 2026-09-24T19:15:15Z) |
| Clean checkout | `C:\Dev\KingmakerBuffPlannerLab\repo\KingmakerBuffPlanner-RC5` (detached at the commit) |
| Release artifact | `...\KingmakerBuffPlanner-RC5\artifacts\release\0.2.0-rc5\` (`KingmakerBuffPlanner-0.2.0-rc5.zip`, `release-manifest.json`, `RELEASE-NOTES-DRAFT.md`) |

Reproducibility: two deterministic Release builds in `KingmakerBuffPlanner-RC5`,
one in `KingmakerBuffPlanner-RC5-repro` and the gate build in
`KingmakerBuffPlanner-G2` (three differently named checkouts) produced the
same package, DLL and MVID.

Later commits on the branch after `27234a4` are receipts and handoff
documents only; they change no source, script, test or version file.

## What changed since rc4

In `docs/RELEASE-NOTES-DRAFT.md` (for a player) and, finding by finding
with commits, in `docs/CASTING-FIRST-REVIEW-INDEX.md`. In short:
- the owner's two rc4 findings;
- mixed-coverage group castings;
- the casting-first enhancement routing (Powerful Change in Instant
  mode);
- Share Transmutation refused as an enhancement;
- enhancement chips limited to what the casting can take;
- archives that never run out of names;
- notes that name the character;
- the advanced-seed qualification tooling: the advanced profile, the
  group-mixed and enhanced-direct recipes, and the native modifier,
  resource and toggle reads.

## Reviews and mutation

- The owner's review of the rc4 source (two findings), then an
  independent read-only review of the unpushed work (eight findings), and
  a re-review of its fixes before the freeze (five findings). Every
  finding and its disposition is in `docs/CASTING-FIRST-REVIEW-INDEX.md`.
  The re-review found the route binding, the recorded strategy, Share in
  imported plans, the focused spellbook filter and the null handling clean.
- Mutation of the rc5 work:
  - 31 C# mutants for the rc4 fixes, mixed coverage and the group recipe
    (28 killed);
  - 28 for the enhanced recipe and the routing (22 killed; one that did
    not compile was re-formed and killed);
  - 14 for the review and re-review fixes, all killed;
  - 6 PowerShell mutants for the staging, profile and recipe guards, all
    killed with the intended check.
  Eight survived, each named with its reason in the review index:
  - five second checks of rules that earlier rules already enforce;
  - two equivalent guards;
  - one unreachable check.
  Mutants show that the tests notice a broken guard; they are not
  evidence of behaviour in the game.

## Source tests at the candidate

Full gate at `27234a4` (Windows PowerShell 5.1, non-interactive, clean
tree): source validation 42/42, protocol 335/335, runtime harness 38/38,
package 4/4, deployment WhatIf 5/5, launcher WhatIf 12/12, fixture
inventory 3/3, Restore-InstallLocal 16/16, guarded publisher 3/3.

## Game runs on the frozen build

Run one after another from the RC5 checkout by the evidence chain, with
no retry. Each run:
- loaded the exact build it staged (commit, package, DLL and MVID checked
  in the game);
- read the save folder and kept its baseline once it held the deployment
  lock;
- compared every save clean before releasing the lock;
- restored the Mods folder byte-exact.

The Gunslinger development session shares the installation; each side
ran only while the other's lock was released.

**Automation campaign** (the disposable automation fixture; party
Hedwirg, Linzi, Tartuccio):

| Run | Result | What it showed |
| --- | --- | --- |
| `classic-select-20260924-rc5-inst-01` | PASS, complete | The Classic (default) planner's Long routine recorded with its digest; nothing cast |
| `classic-cast-20260924-rc5-inst-01` | PASS, complete | Instant: Resistance from Linzi's level-0 spellbook entry, resolved to the at-will cantrip ability; confirmed by a new effect instance; available count -1 before and after (free); the single-use grant consumed once |
| `classic-select-20260924-rc5-anim-01` | PASS, complete | The same plan in Animated mode |
| `classic-cast-20260924-rc5-anim-01` | PASS, complete | Animated: the same resolution and confirmation (queued and started through the game's own command) |
| `casting-qual-select-20260924-rc5-01` | PASS, complete | Zero-cost recipe selected; 5 projections forecast; an allowance written for each mode, bound to this build |
| `casting-qual-cast-20260924-rc5-anim-01` | PASS, complete | Every step as forecast: stop (the player's routine press, landing while the first cast was in flight), complete, repeat (refused `nothing-to-cast:3`), recast, disable (in flight; held 5 updates with nothing running or accepted), recover (a new run after enabling); 5 runs started and 5 reported; lifecycle probe unchanged; zero violations |
| `casting-qual-cast-20260924-rc5-inst-01` | PASS, complete | The same in Instant mode; the disable landed before the step started and is labelled disable-before-start |
| `casting-ws-import-20260924-rc5-01` | PASS, complete | A genuine schema-4 (0.0.19) Classic file imported on first open: migrated, the Classic file unchanged and archived, 2 castings, none Ready (drafts awaiting review) |
| `casting-ws-reload-20260924-rc5-01` | FAIL (black frames), restored and clean | The chain starts a frame-judged run only while the owner's session is connected, and at 16:04 the session showed as connected. It disconnected before the frames were captured, so every frame was black (the known disconnected-session condition). Every assertion that does not judge frames passed, restoration was verified and the saves compared clean. This is not a product result, and the run is not repeated while disconnected |
| layout at 1920x1200 and 1920x1080, manual-session rehearsal | Deferred | They judge game frames, which render black while the owner's session is disconnected |

**Advanced campaign** (the owner's `KBP_ADVANCED_SEED`, a disposable
*Beneath the Stolen Lands* fixture: Cleric, Alchemist, Fighter and Brown
Fur Transmuter at level 9; see
`docs/evidence/advanced-fixture-20260924-receipt.md`; not proof of
main-campaign-specific behaviour):

| Run | Result | What it showed |
| --- | --- | --- |
| `advanced-inspect-20260924-rc5-01` | PASS, complete | Non-casting inspection: campaign `cb1f405d...`, Tenebrous Depths I, party level 9, the full capability inventory |
| `casting-qual-select-20260924-rc5-adv-finite-01` | PASS, complete | `finite-direct-mixed`: Protection from Alignment — Chaos from the Transmuter's spontaneous level 1 (on the Alchemist) and the Cleric's prepared slot `level-1\|type-0\|index-5` (on the Fighter) |
| `casting-qual-cast-20260924-rc5-adv-finite-anim-01`, `-inst-01` | PASS, complete | In both modes: stop (spontaneous 6 to 5; the slot untouched), complete (the first skipped, pool unchanged; the exact slot spent, no other), repeat (refused `nothing-to-cast:2`), recast (spontaneous 5 to 4; the slot still spent); 3 submissions |
| `casting-qual-select-20260924-rc5-adv-group-01` | PASS, complete | `group-mixed`: direct Protection from Alignment — Chaos on the Fighter; its Communal form centred on the Cleric (the Fighter pre-covered); Protection from Arrows, Communal anchored on the Cleric |
| `casting-qual-cast-20260924-rc5-adv-group-anim-01`, `-inst-01` | PASS, complete | In both modes: prime (the Fighter covered; slot `level-1\|type-0\|index-5` spent); mixed (the direct casting skipped; the Communal cast reached all four members for one invocation and one slot, `level-2\|type-0\|index-0`; the anchored cast reached all four for one spontaneous level-3 use, 5 to 4; the Fighter's longer instance replaced by the shorter communal one); 2 submissions |
| `casting-qual-select-20260924-rc5-adv-enh-01` | PASS, complete | `enhanced-direct`: Bull's Strength from the Transmuter's spontaneous level 2, plain on the Cleric, then with Powerful Change: Strength on the Alchemist; forecast route `ProviderDirectRuleCast` |
| `casting-qual-cast-20260924-rc5-adv-enh-anim-01`, `-inst-01` | PASS, complete | In both modes: plain: Strength Enhancement +4, Arcane Reservoir 16 to 16. Enhanced (the plain casting skipped, the Cleric's +4 unchanged): Strength Enhancement +6, reservoir 16 to 15, spontaneous level 2 one use per cast. All 17 activatable abilities of the Transmuter unchanged. Instant ran through the provider's own transaction (`provider-direct:True`); Animated through the game's own command |

## Install and rollback of the exact artifact

Temporary install `rc5-temp-deploy-20260924-01` of the frozen release
package through the real installer, then the real rollback (evidence
`runtime-evidence\install-rc5-temp-deploy-20260924-01`,
`rollback-rc5-temp-deploy-20260924-01` and
`rc-deploy-rc5-temp-deploy-20260924-01\deploy-check.json`):

- Local install PASS: 0.2.0-rc5, package `a05f1a83...`, DLL `785e1b79...`;
  no other mod changed.
- Install rollback PASS: 0.1.1-rc3 restored exactly (DLL `78407dd4...`);
  the Mods folder equal to its state before the install (1117 entries).

## Artifact identity after merge

The tested candidate is the frozen package, not the source. A merge
creates a new commit, and the build embeds its commit, so a rebuild from
the merged branch produces a different DLL and MVID; that rebuild is not
the tested candidate even when the source tree is identical. Merging with
a merge commit (not a squash or rebase) keeps `27234a445e95f2fc399857a880c35de0f397359f` in `main`'s
history, so the frozen package's manifest keeps naming a reachable commit.

To install exactly what was tested, install the frozen release package by
its hash (no rebuild):

```powershell
# From the candidate's clean checkout (C:\Dev\KingmakerBuffPlannerLab\repo\KingmakerBuffPlanner-RC5):
.\scripts\Install-Local.ps1 -ReleaseManifestPath .\artifacts\release\0.2.0-rc5\release-manifest.json `
    -InstallId <id> -ExpectedPriorVersion <the installed version>
```

Post-install check (it must print `True` twice):

```powershell
$dll = 'C:\Program Files (x86)\Steam\steamapps\common\Pathfinder Kingmaker\Mods\KingmakerBuffPlanner\KingmakerBuffPlanner.dll'
(Get-FileHash $dll -Algorithm SHA256).Hash.ToLowerInvariant() -eq '785e1b7946d5082b3a6bb944156ffeb31c1dd1b7eaf1045e383516776c6ade48'
[Reflection.Assembly]::ReflectionOnlyLoadFrom($dll).ManifestModule.ModuleVersionId.ToString() -eq '171a1599-fef3-419b-b748-2833a10f9d75'
```

Rollback, keeping settings edited since:

```powershell
.\scripts\Restore-InstallLocal.ps1 -InstallId <id>
```

If a build from a merged commit is to be released instead, it is a new
candidate:
1. Confirm the source is unchanged: `git diff 27234a445e95f2fc399857a880c35de0f397359f <merged> -- src
   scripts tests Version.props` must be empty.
2. Build it with `Build-Release`.
3. Freeze it.
4. Repeat the candidate's native set on that build: Classic select and
   cast in both modes, the zero-cost selection and both casting modes,
   the import, the advanced inspection and the finite, group and enhanced
   qualifications in both modes, and the temporary install and rollback
   (plus the frame-judged runs once a connected session is available).

A matching source diff alone does not make the new bytes tested.

## What is not yet shown

- **Main-campaign-specific behaviour.** The advanced evidence comes from
  a *Beneath the Stolen Lands* fixture the owner built for testing.
- **Ability pools** (the Alchemist's Mutagen and the Cleric's domain
  powers, inventoried but not cast), **metamagic variants and rods** (no
  metamagic spell prepared; the Extend rods not used) and **pets** (none
  in either party): implemented and source-tested, not proven in the game.
- **An actual area transition.** The automation campaign's only exit
  autosaves and ends the prologue. No route on the advanced campaign has
  been verified as safe (noncombat, no dialogue, no ordinary-save write).
  The lifecycle was qualified with the production paths instead: the
  player's stop in flight, disable during a run (Animated in flight;
  Instant before its first step, labelled so), re-enable and a new
  accepted run. Deadlines are covered by source tests.
- **Frame-judged runs** (deferred): the in-game reload, the workspace
  layout with the new controls at 1920x1200 and 1920x1080, the manual
  session rehearsal, and physical keyboard and mouse input. They need the
  owner's session connected; the commands are in
  `docs/MANUAL-USABILITY-HANDOFF.md`.
- **The supervised manual session** and the owner's verdict on the
  interface, including whether the Classic planner closing on APPLY is
  wanted.
- **Two open owner decisions:**
  - worn-item enchantments (Magic Weapon, Magic Fang) are recast under
    skip-if-active, because their strength cannot be read (the owner's
    rule);
  - merge, release and permanent installation.

## Next iteration (bounded)

1. With the owner's session connected: the deferred frame-judged runs
   from the RC5 checkout, then the supervised manual session.
2. On the advanced campaign:
   - an ability-pool qualification (the Mutagen: one cast spends the
     pool's single use, a repeat casts nothing, Always recast is refused
     for want of the resource). Its effect is an empty action plus a
     condition whose two branches apply the same buff, a shape the
     plain-buff check does not yet accept;
   - an Extend Metamagic Rod casting (duration doubled, one charge spent).
3. A route check for a safe area transition on the advanced campaign;
   the transition test only if one is found.
4. The owner's call on worn-item enchantments. Reading a temporary
   enchantment's caster level and end time would let skip-if-active skip
   them safely.
