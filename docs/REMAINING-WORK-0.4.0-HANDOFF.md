# Remaining work — 0.4.0 release train (owner-review candidate)

Program handoff for the consolidated 0.4.0 candidate. One concise record;
bulky evidence stays under the lab's `runtime-evidence\<runId>\`.

- Branch: `claude/kbp-complete-remaining-work-2026-10-09` (worktree
  `private\worktrees\KBP040`), created from `origin/main`
  `16f87ed1dc9b00b683ac7192f4ae8d95da7853fc` (WP2A merged, PR #4).
- Public release: `v0.3.0`. No 0.4.0 tag, merge, or release is authorized.
- Owner-review candidate: **rc4 `009b6ddab80a5e70d7469962986c38222961f4ba`** (lead-review fixes; see the rc4 report below). rc3 `c452e01b360f380b9ca9268e0be3ea05d1b288c6` is superseded; its report is kept as history.
- Shell rule: run every gate/harness/launcher script through Windows
  PowerShell 5.1 as `powershell.exe -NoProfile -NonInteractive -Command
  "& .\scripts\Name.ps1 ..."`. Launched with `-File`, Test-RuntimeHarness
  fails spuriously (its script-scoped `$WhatIfPreference` reaches the 5.1
  `Get-FileHash`, which then returns nothing); the qualified WP2A head
  reproduces the same failure under `-File`.

## rc4 report (current owner-review candidate)

Product candidate: **`009b6ddab80a5e70d7469962986c38222961f4ba`** (rc4), internal
version 0.4.0. Not merged, not tagged, not released, not installed. Owner
acceptance is pending for every package. rc4 answers the lead review of rc3
(`c452e01`); the rc3 report below it is history.

| Identity | Value |
|---|---|
| Exact product candidate commit | `009b6ddab80a5e70d7469962986c38222961f4ba` (rc4) |
| Records-only branch head | the head of `codex/kingmaker-buff-planner-0.4.0` after this record (commits after `009b6dd` touch only `docs/`, `planning/` and the root records; no source, test, script, profile or version surface) |
| Package identity | `KingmakerBuffPlanner-0.4.0.zip`, SHA-256 `4be8a0b7dbf150d762350a88aa6a00eafac391fe0ac0e541d85dd53618f63adb`; DLL `0e76977b81b30153912537c13cc5a67b336f7b1112fb73dcd1b0f3cc2ea651a3`; MVID `50fcbfc1-751c-4bb7-a283-bf2399c33b47` |
| Complete-gate identity | `scripts\Test-SourceOnly.ps1` on worktree `KingmakerBuffPlanner-K044`, detached at `009b6dd`, clean (result under "Complete gate (rc4)") |

### rc3 complete gate (historical)

`scripts\Test-SourceOnly.ps1` on rc3 `c452e01` (worktree
`KingmakerBuffPlanner-K042`, clean): **PASS**, exit 0, 05:56:26 -> 11:12:23
(5 h 16 min): source validation 42/42; protocol (C#) 471/471; runtime harness
filesystem 38/38; problem navigation evidence 23/23; spellbook entry evidence
26/26; package validation 4/4 (`cc50fb3c...`); deployment WhatIf purity 5/5;
launcher `-File` WhatIf purity 13/13; fixture inventory evidence 3/3;
Restore-InstallLocal 16/16; guarded publisher gate 3/3; source-only suite 1/1.
Log archived as `package-archive\0.4.0-rc3\test-sourceonly-gate.log`.
Production code changed after it, so it is historical only.

### Review findings and corrections

**Finding 1 - WP5 removed genuine or opaque buffs.** The rc3 rule "every
component only keeps the books" was also true of a buff with no components at
all, and read that as "not a buff". rc4 (`7ccf0c9`, `009b6dd`):

- drops that rule: a buff with no mechanics of its own stays a payload unless a
  proved marker rule applies - its presence may be the state other blueprints
  read (Targeted Bomb Admixture's buff has no components; 24 alchemist bomb
  abilities read it through `ContextConditionCasterHasFact`);
- proves markers instead: a **lockout** (the ability forbids its own recast over
  the buff - `AbilityTargetHasFact` inverted, `AbilityCasterHasNoFacts`, Call of
  the Wild `AbilityTargetHasNoFactUnlessBuffsFromCaster` - and also does
  something else); beside an instantaneous **restoration**, a hidden buff or one
  with no mechanics of its own; a **hidden bookkeeping** buff whose own actions
  apply nothing and run nothing unrecognized;
- reads what a buff's own `AddFactContextActions` do (`factActions` in the
  export): a hidden buff whose actions apply a beneficial buff carries it; one
  that runs an unrecognized action, or applies a beneficial buff only when it
  ends, is **unproved** - unsupported with the reason, never assumed bookkeeping;
- reads two exact Call of the Wild contracts: `RunActionsDependingOnContextValue`
  (value-selected `ActionList[]` - Battle, Bone and Wind Ward and Draconic
  Resilience now show their ward buff; the hex cooldown is the lockout) and
  `ContextActionTreatDeadlyWounds` (a restoration);
- adds `revival-target-only`: the native `AbilityTargetBreathOfLife` admits only a
  dead or dying party member or an undead (its `CanTarget` IL) - Inspiring
  Recovery revives, and its hidden check buff casts the morale buff only when it
  ends (rc3 had excluded it for the wrong reason);
- adds `blueprint-references.json` to the catalogue scenario: every blueprint
  that reads each audited buff (81,292 / 104,383 / 108,808 blueprints walked in
  native-only / call-of-the-wild / full-user, 0 failures).

Per-entry adjudication of every rc3 removal (ability GUID, every persistent
buff/effect GUID, components, action path, actual ongoing behaviour, final
disposition, reason, adapter/override): `planning/CATALOG-AUDIT-0.4.0.md`.
Result over the 125 entries rc3 removed: **50 restored** as genuine buffs
(category 2: Targeted Bomb Admixture x3, Light x3, Daylight, Elemental Bastion,
Battle / Bone / Wind Ward and Draconic Resilience hexes x14, Heaven's Leap mark
x4, Venomous Strike x12, Activate School Understanding x8, Activate Cold Snap,
Select Wild Flanking Partner, Gunslinger Deadeye and Breeze-Kissed), **75
excluded** as not buffs (category 1: hostile riders and maneuvers x38, harmful
conditions x6, restorations with their cooldown / tracker / enabler / side-effect
buffs x26 - Treat Affliction x2, Treat Deadly Wounds, Counter Curse x14, Kinetic
Healer x3, Regenerative Sinew x4, Warpriest channel x2 - and revivals x5 -
Inspiring Recovery), **0 unsupported** (category 3). Adapters: the two exact
Call of the Wild contracts above; no override was added. The adapter also
**newly offers 20 Call of the Wild sources** whose only effects sat behind the
wrapper (Air Barrier x6, Spirit Shield x2, Ice Armor, Armor of Bones, Time Sight
x2, Gift of Claw and Horn x3, Mythmaker x6 - absent from rc3 and 0.3.0 under
both rule sets), each a genuine buff. Light and Daylight are kept by deliberate
product scope: verified light only (no mechanics; read by nothing but their own
abilities) - excluding them is the owner's call. Regressions
`catalog-adjudication-*` are generated from the rc4 export's exact facts; red
under the rc3 rule (Targeted Bomb Admixture dropped as mechanics-free; a lone
self-gated buff taken for a lockout) and without the revival rule (Inspiring
Recovery unsupported as a delayed effect).

**Finding 2 - Classic combat-refusal ordering** (`11b8d14`, evidence `e7bd532`).
`PlannerUiSession.ExecuteRoutine` runs through `ClassicRoutineAdmission`: the
running-routine guard, then the global refusal "Buff routines cannot run during
combat." - before `Refresh()` (profile rebinding and saving), the preview and
compiler, the review and partial-apply gates and any dispatch; `PrepareAndCast`
keeps its final-boundary check for combat that begins while it prepares.
Regression `execution-policy-classic-combat-refuses-before-preparation`: with
combat active, zero refreshes, profile writes (no file exists), previews,
review reads and spends, executor calls and resource spends; out of combat the
same probe crosses each once; red under the pre-rc4 order
(`refresh=1;profile-writes=1;previews=1`). Validate-Source pins the wiring (43
assertions). Native evidence: the physical combat run now also drives the
Classic route in the same held combat - `kbp040-rc4-combat-instant`: refused
with that sentence, 0 yielded, 0 refreshes, 0 previews, execution report
unchanged, not executing, Classic profile bytes unchanged, availability
`99>99`, no effect, and the casting-first HUD press refused before dispatch.
Exact route semantics: the casting-first routes refuse at Apply (before
compile, persistence, authorization and submission); the casting-first HUD
quick run first builds fresh discovery inputs (one Classic-session refresh,
whose Classic saves are suppressed in casting-first mode). With that wording,
"every route refuses before planning or saving" now holds.

**Finding 3 - full-user ownership** (`77e6946`). Ownership is proved per staged
mod by the mod's own inventory: the Call of the Wild library
`loaded_blueprints.txt` (Call of the Wild, TweakOrTreat, ZFavoredClass,
ProperFlanking2, BetterVendors; `AddAsset` records every blueprint it
registers, literal GUIDs included) and the Kingmaker Gunslinger identifier
manifest (`blueprints/blueprints.json`). A blueprint no inventory claims is
native only when that is proved (native-only profile, or every staged mod
declares an inventory); otherwise `unattributed`. The report resolves an
unattributed entry to native only from the native-only catalogue of the same
commit (`-NativeCatalogPath`) and fails if the regrouped counts do not
reconcile. Regression `catalog-ownership-multi-mod-inventories` (the fixture's
own formats and GUIDs; red under the rc3 index: native/native/native).
Verified in `kbp040-rc4-catalog-full`: Call of the Wild's representatives
(Dazzling Blade, Bless Weapon, Fortune) and Battle Ward are
`call-of-the-wild`; all 14 Gunslinger abilities (`KMG_*`) are
`kingmaker-gunslinger` and nothing else is; Targeted Bomb Admixture is
`unattributed` in the game's JSON and native in the report.

### Corrected counts and ownership (rc4, static before -> after / live before -> after)

| Profile | Ownership | Static | Live |
|---|---|---|---|
| native-only | all (native) | 379 -> 376 | 516 -> 509 |
| call-of-the-wild | all | 2355 -> 2311 | 3433 -> 3367 |
| call-of-the-wild | call-of-the-wild | 1975 -> 1947 | 2891 -> 2844 |
| call-of-the-wild | native (every staged mod declares an inventory) | 380 -> 364 | 542 -> 523 |
| full-user | all | 2418 -> 2368 | 3510 -> 3436 |
| full-user | call-of-the-wild | 1978 -> 1950 | 2896 -> 2848 |
| full-user | kingmaker-gunslinger | 7 -> 6 | 14 -> 13 |
| full-user | tweak-or-treat | 26 -> 24 | 32 -> 30 |
| full-user | proper-flanking2 | 19 -> 19 | 19 -> 19 |
| full-user | z-favored-class | 1 -> 1 | 1 -> 1 |
| full-user | native (native-only reference) | 387 -> 368 | 548 -> 525 |
| full-user | unattributed after the reference | 0 | 0 |

Removed by the 0.4.0 rules (rc4): native-only 7 (harmful 3, hostile rider 2,
restoration 1, revival 1); call-of-the-wild 66 (5 / 33 / 23 / 5); full-user 74
(6 / 38 / 25 / 5). No entry is added by the rules (`addedCount` 0); "before"
rose by 20 live entries in the two Call of the Wild profiles because the
adapter reads more of the same content under both rule sets. rc3 for
comparison: 379 -> 374 / 516 -> 505; 2355 -> 2268 / 3413 -> 3301; 2418 -> 2323 /
3490 -> 3366, with every full-user entry wrongly "native". Optional-mod counts
now cover proved ownership only (call-of-the-wild profile: 7,342 abilities,
4,937 candidates, 1,947 included, 0 unsupported).

### Testing

- Focused, red/green: `execution-policy-classic-combat-refuses-before-preparation`
  (mutation: prepare-then-check -> red); `catalog-ownership-multi-mod-inventories`
  (mutation: the rc3 index -> red); `catalog-adjudication-*` x4 (mutations: the
  rc3 rule -> 2 red; the revival rule disabled -> red); `physical-combat-judgement`
  (Classic violations); `catalog-audit-*`.
- Complete C# suite on rc4: **473/473**. Validate-Source **43/43**. Production
  build clean (warnings are errors, level 4).
- Pre-checks on `K044` before the campaign: runtime harness filesystem 38/38;
  launcher `-File` WhatIf meta-test 13/13 (an out-of-tree copy with the
  purity windows stubbed; the real windows run in the complete gate).
- Complete `Test-SourceOnly.ps1` on rc4: under "Complete gate (rc4)".

### Guarded runtime results (rc4 `009b6dd`, package `4be8a0b7...`)

Every rc4 run below loaded exactly the frozen build (DLL `0e76977b...`, MVID
`50fcbfc1-...`; freeze `runtime-backups\qualification-frozen\009b6dd...\FREEZE.json`).

| Run | Result | Evidence |
|---|---|---|
| `kbp040-rc4-catalog-native` / `-cotw` / `-full` | PASS x3 | catalogue, audit, blueprint references; counts above |
| `kbp040-rc4-combat-instant` | PASS | casting-first and Classic routes refused in held combat; nothing spent (Finding 2) |
| `kbp040-rc4-sel-720` | PASS | planner on the scroll paper at 1280x720 with the rc4 catalogue |
| `kbp040-rc4-authoring` | PASS | physical portrait add / remove / re-add / retarget / provider change / Undo / nested Escape |
| `kbp040-rc4-spellbook` | PASS | physical spellbook handoff (owner display) |
| `kbp040-rc4-sel-sticky` | PASS | Magic Circle selection on the advanced copy |
| `kbp040-rc4-cast-sticky-instant` | PASS | Magic Circle Instant: `EffectConfirmed`, one spend by the source data, available `1>0`, `StickyTouchDeliveryRuleCast` |
| `kbp040-rc4-cast-sticky-animated` | PASS | Magic Circle Animated: the native command path, same spend and effect |
| `kbp040-rc4-wsqual` (+ `-02`), `-03` | FAIL x2 (foreground), then **PASS 95/95** (`-03`) | workspace semantics PASS in every attempt (3 casts / 2 casters exact, refused add clean, retarget, Undo, save, reopen with exact IDs and order, budget); the OS-input hover probes failed twice ("Kingmaker foreground activation failed") and passed in `-03` (35 samples, one owner, no ghost) |
| `kbp040-rc4-combat-animated` (+ `-02`), `-03` | FAIL x2 (foreground), then **PASS** (`-03`) | `-03`: Animated mode - casting-first press refused before dispatch, Classic route refused with 0 yielded / 0 refreshes / 0 previews, report and profile unchanged, availability `99>99`, no effect; the two earlier attempts lost the foreground ("client bounds are invalid" / "foreground activation failed") |
| `kbp040-rc4-sel-1080` (+ `-02`), `-03` | FAIL x2 (foreground), then **PASS 83/83** (`-03`) | `-03`: planner and spell scroll on the paper at 1920x1080 with the rc4 catalogue (`physical-cf-graph*.png`, `physical-cf-inspect*.png`) |
| `kbp040-rc4p-*` (pre-candidate `7ccf0c9`, package `bfd32351...`) | PASS x4 | fact-gathering for the adjudication and the first native Classic evidence; not candidate evidence |

The foreground failures are environmental: Windows refused foreground
activation to the game while this desktop was in use around midday (the rc3
campaign ran at 05:00-06:00 and had none; rc4's own sel-720, authoring and
spellbook passed between failures). The harness refused to deliver input
rather than send it elsewhere - its guard working. With the desktop idle (17:51-18:00) all three passed on the same frozen candidate (`-03`).

### Remaining limitations (not native-qualified unless stated)

- Strict-Instant unsupported-source deep-link: source/integration proof only (no
  animated-only source in any guarded fixture after WP6).
- Group re-centring and Share conditional visibility: source proof only (not in
  the physical fixture; physical scenarios are not admitted on the advanced copy).
- Human-reproduction catalogue: not run (its BagOfTricks fixture identity no
  longer matches - the owner's profile).
- 2560x1440 or an alternate in-game UI scale: not run (1920x1080 session display;
  changing the scale would change the owner's game settings).
- Pre-existing, not introduced by 0.4.0: Call of the Wild's Battlemind Link (x4)
  applies `CasterBattlemindLinkBuff` through `ContextActionOnContextCaster`, which
  the generic wrapper reads as the target's buff; its confirmation would expect
  the caster's buff on the target. Unchanged since 0.1.x; a follow-up.
- Light and Daylight: kept (utility light); the owner may choose a scope exclusion.

### Safety

- Disposable fixtures only (`KBP_AUTOMATION_WORKING` full-user; the owner-approved
  advanced copy `KBP_ADVANCED_WORKING`); no save written; protected saves
  compared and clean on every save-backed run.
- Every run staged the candidate transactionally and restored `Mods` (and the
  display registry values for windowed runs) byte-exact: all 56 mission
  transactions are `Restored` with restoration verified; no deployment lock, no
  staging left, no dispatcher claim. The candidate is not installed.
- Casting allowances were written by `New-KbpRunAllowance.ps1` under this
  mission's authority, one per run, for the frozen build. Two cast attempts were
  refused by the launcher before launch (my script passed the allowance path
  with doubled backslashes); the unconsumed allowances were then used as written.
- No other lab's lease or process was touched; no guard, validator, allowlist or
  assertion was weakened; no merge, tag, release or release asset; no
  Gunslinger change.

### Owner-review checklist (main game)

Install the ZIP after backing up `Mods\KingmakerBuffPlanner` (with
`UserSettings`) outside `Mods`; Unity Mod Manager should list 0.4.0.

1. Open the spellbook (B) and click the Buff Planner button (below the window's
   close button): the spellbook closes and the planner opens.
2. Confirm the planner opens correctly on the scroll paper, all lanes readable.
3. Pick a buff, a caster and its exact source row, click a recipient (one
   casting, selected), then click the same recipient again: it is removed.
4. Add a direct buff again and click another recipient: it moves (an occupied
   or unreachable recipient is refused with the reason).
5. Add a group buff, click another portrait (re-centres), then its centre
   portrait (removes).
6. Confirm the simplified sidebar: no Disable, Duplicate, Cast By or
   required-recipient menus, no obsolete header text.
7. Confirm Share Transmutation appears only for a Brown-Fur Transmuter source
   that can use it.
8. Start a fight and press a HUD routine - in the casting-first planner and in
   the Classic planner: "Buff routines cannot run during combat."; nothing is
   spent, nothing is saved.
9. Compare Instant and Animated (Mode button): Instant casts at once; Animated
   plays the normal casting.
10. In Instant mode cast Magic Circle against Alignment on an ally: instant, the
    slot spent once, the circle appears.
11. Browse the buff catalogue for anything that is not a real buff.
12. Confirm Hideous Laughter, Treat Affliction, Treat Deadly Wounds and
    Inspiring Recovery are absent; confirm Targeted Bomb Admixture, the hex
    wards (Battle / Bone / Wind Ward), Venomous Strike and Elemental Bastion are
    present; decide whether Light and Daylight (utility light) belong.
13. Inspect the parchment planner at your own resolution.
14. Right-click a spell: the description is a spell scroll; the wheel scrolls
    it; Escape or a click outside closes it.
15. Run ordinary Long / Important / Short routines from the HUD.
16. Make a casting Not Ready (for example remove its slot) and confirm Not Ready
    navigation still focuses it.
17. Use Undo, close and reopen the planner and reload the save: the plan persists.
18. Report PASS or the exact defects.

### Complete gate (rc4)

`scripts\Test-SourceOnly.ps1` on rc4 - exactly
`009b6ddab80a5e70d7469962986c38222961f4ba`, worktree
`KingmakerBuffPlanner-K044`, clean - **PASS**, exit 0, 12:20:14 -> 17:50:59
(5 h 31 min): source validation 43/43; protocol (C#) 473/473; runtime harness
filesystem 38/38; problem navigation evidence 23/23; spellbook entry evidence
26/26; package validation 4/4 (`4be8a0b7...`); deployment WhatIf purity 5/5;
launcher `-File` WhatIf purity 13/13 (live purity windows); fixture inventory
evidence 3/3; Restore-InstallLocal 16/16 (7 rollback cases); guarded publisher
gate 3/3; source-only suite 1/1. Log archived as
`package-archive\0.4.0-rc4\test-sourceonly-gate.log`. Release package: two
`Build-Release.ps1` runs (each `deterministic=2`) produced byte-identical
`4be8a0b7...` - the package every rc4 run loaded; archived with its manifest
under `package-archive\0.4.0-rc4\`.

### Publication

Published through the project's guarded push helper
(`codex-policy\Push-KingmakerBuffPlanner.ps1`, fast-forward only, clean tree)
as `codex/kingmaker-buff-planner-0.4.0`: the mission branch
`claude/kbp-complete-remaining-work-2026-10-09`, same commits - the helper
admits only `codex/kingmaker-buff-planner*` names. Guarded push PASS at the
records head `ea6164c` (from `d358c38`, fast-forward). Draft PR against `main`:
https://github.com/howardreith/KingmakerBuffPlanner/pull/5 (#5, draft, opened at
`ea6164c`). Nothing is merged, tagged, released or installed.

## rc3 report (superseded by rc4; kept as history)

Candidate: `c452e01b360f380b9ca9268e0be3ea05d1b288c6` (rc3), internal
version 0.4.0. Superseded by rc4 after the lead review (above); its WP5
catalogue statements and counts are corrected there.

### A. Overall verdict

| Package | Implemented | Regressed | Source-qualified | Runtime-qualified (guarded, rc3) | Native-qualified | Owner |
|---|---|---|---|---|---|---|
| WP2B spellbook button | yes | yes | yes | yes | yes: physical OS-input handoff (`kbp040-rc3-spellbook`; first proven in `kbp040-wp2b-spellbook-02`) | pending |
| WP4 execution policy | yes | yes | yes | yes | combat refusal in Instant and Animated (`kbp040-rc3-combat-instant`, `-animated`); explicit Animated cast (`kbp040-rc3-cast-sticky-animated`). The strict-Instant blocker has **no native run**: no guarded fixture has an animated-only source after WP6 (source-proven only) | pending |
| WP3 direct manipulation | yes | yes | yes | yes | physical portrait add / same-portrait remove / re-add / retarget / provider change / Undo / nested Escape (`kbp040-rc3-authoring`); control gestures, autosave and reopen (`kbp040-rc3-wsqual`, 95/95). Group re-centring and Share visibility: source-proven only (the automation party has no group spell and no Share caster; physical scenarios are not admitted on the advanced copy) | pending |
| WP5 catalogue audit | yes | yes | yes | yes | catalogue exports on rc3 for native-only, call-of-the-wild and full-user; the in-game buff list of the automation party (`kbp040-rc3-sel-*`) | pending |
| WP6 Magic Circle | yes | yes | yes | yes | Instant and Animated casts on an ally with effect, single spend and cleanup (`kbp040-rc3-cast-sticky-instant`, `-animated`) | pending |
| WP7 parchment | yes | yes | yes | yes | live paper and spell scroll at 1920x1080 and 1280x720 (`kbp040-rc3-sel-1080`, `-720`); the look itself is the owner's judgement | pending |

### B. Branches and commits

- Integration branch `claude/kbp-complete-remaining-work-2026-10-09`
  (worktree `private\worktrees\KBP040`), base `origin/main`
  `16f87ed1dc9b00b683ac7192f4ae8d95da7853fc`. Published for review as
  `codex/kingmaker-buff-planner-0.4.0` (same commits; the project's guarded
  push helper admits only `codex/kingmaker-buff-planner*` names). Pushed
  state and the draft PR link are recorded at the end of this report.
- Focused branches merged into it: `claude/kbp-040-wp6-sticky-recipe`
  (`ad7ca01` `2dde708` `2ec8e63`, merge `d2a2a60`), `claude/kbp-040-wp7-parchment`
  (`d85e6e5` `ff9e877` `17d7420` `91d1c6e`, merge `567e273`), integrated on
  `claude/kbp-040-integration` and fast-forwarded.
- WP2B `e84a892` `e29c4ec` `11a30db` `a5ab3c3`; WP4 `d0d43e4` `512af36`; WP3
  `0274b7c` `898cfeb` `14c7902`; WP6 `3c80ed0` `c452e01` + recipe; WP5 `d8614ef`
  + records `1f4bac2`; WP7 + `7383347` `d8b659e` `168ad8d`; harness `98bd032`
  `fe06e26` `3cb3f90` `d31e857`; records `4edcf6f`; release prep `8e764a5`.
- Tested candidates (each frozen under
  `runtime-backups\qualification-frozen\<commit>`): rc1 `8e764a5` (13 runs;
  two harness/record defects found, fixed in `d31e857` and `c452e01`); rc2
  `d31e857` (frozen, never run, superseded); **rc3 `c452e01`** (all runs).
  The release-prep commit precedes the two fixes because the fixes were found
  by running the frozen 0.4.0 build; neither touches a version surface.
- Gunslinger: no branch, no change (WP6's owner is the Buff Planner).
- Worktrees: clean. Diff from `16f87ed`: 91 files under `src`, `tests`,
  `scripts`, `docs`, `planning` and one compatibility profile; no unrelated
  work.

### C. Package-by-package behaviour

Detailed sections follow this report (WP2B, WP4, WP3, WP6, WP5, WP7). In
short:

- **WP2B.** Root cause: the button sat under the service window's native
  Close (`ServiceWindow/Top/Close`) and the handoff looked for "Close" in the
  wrong subtree. Repair: the button is drawn above the top bar and placed
  against the live native controls; the handoff closes the spellbook through
  its real close, waits for release, opens the planner, and on failure
  reopens the spellbook. Physical evidence: three OS-input cycles plus one
  simulated recovery, every one clean, at 1920x1080 (rc3).
- **WP4.** Combat: every routine route refuses while `Player.IsInCombat`
  with exactly "Buff routines cannot run during combat." before compiling,
  saving, authorizing or dispatching. Native: with a party member held in the
  game's combat state the HUD moon press was refused with that text, no
  dispatch refusal, no run, no editor, availability `99>99`, no effect; after
  release the next press reached the session lock - in both modes. Strict
  Instant: a non-instant strategy is a casting-specific blocker in Instant
  (WP2A navigation focuses it); Animated is explicit. 0.3.0 profiles load;
  their stored preferences are read but not honoured and rewritten on the
  next save.
- **WP3.** Portrait matrix: nothing focused - add and focus, or focus the
  existing casting; focused direct - own portrait removes, another
  retargets, an occupied or unreachable one is refused with the reason;
  focused group - re-centres. Caster/source clicks change the provider (an
  ambiguous caster now shows its exact rows - `14c7902`, found by the
  physical run's design). Disable, Duplicate, Cast By, the retarget/coverage
  menus and the obsolete header text are gone; Share rows appear only where
  the mechanism applies. Group castings require nobody; 0.3.0 required
  recipients are archived byte-exact (`*.pre-0.4.0.orig`) on first save.
  Undo and autosave: proven natively by `kbp040-rc3-authoring` (Undo of a
  provider change and of a move, autosave durable) and `kbp040-rc3-wsqual`
  (Undo, save, close and reopen).
- **WP5.** Structural classifier rules (hostile ability rider, save-gated
  effect, harmful condition / faction change, mechanics-free buff, hidden
  bookkeeping marker, restoration tracker); one facts builder for live
  discovery and the export; the export classifies with the 0.3.0 rules too.
  rc3 counts (static before -> after / live before -> after): native-only
  379 -> 374 / 516 -> 505; call-of-the-wild 2355 -> 2268 / 3413 -> 3301;
  full-user 2418 -> 2323 / 3490 -> 3366. Removed: Hideous Laughter, Treat
  Affliction, Treat Deadly Wounds, Dazing Touch, Eyebite and the rest listed
  in `planning/CATALOG-AUDIT-0.4.0.md`; kept: Mage Armor, Haste, Bless,
  Prayer, Freedom of Movement, Bull's Strength, Mirror Image and every
  legitimate self, ally, party, ability and item buff checked. Audit
  files: `runtime-evidence\kbp040-rc3-catalog-{native,cotw,full}\native-buff-catalog-audit.json`;
  full human-readable report copied beside them as
  `catalog-audit-0.4.0-full.md`.
- **WP6.** Root cause: Magic Circle against Alignment (KingmakerGunslinger)
  delivers through a touch that may also target enemies; the planner
  classified every such delivery `AnimatedFallback`
  (`sticky-delivery-hostile-targeting-ambiguous`), so 0.3.0's default fallback
  cast it slowly through the animated two-command path. Owner: the Buff
  Planner. Strategy before -> after: `AnimatedFallback` ->
  `StickyTouchDeliveryRuleCast` (`supported-willing-target-sticky-touch-delivery`),
  with the native touch auto-hit condition checked before each cast. Native
  Instant (rc3): one rule cast of the derived delivery `b05e3a50...` from
  carrier `8d9b3a1b...`, `Spend()` once by the source data, prepared slot
  `1>0`, no carrier or delivery command, no held touch, cleanup settled, a
  new effect instance on the ally; a repeat casts nothing; Always recast with
  the slot spent is refused (`prepared-slots-exhausted`) before submission.
  Animated (rc3): the game's own command path with the same spend, effect,
  repeat and refusal.
- **WP7.** Visual architecture: two contract-checked native donors
  (`dialogue_backsheet` paper, `blockscroll_bottom` rule) found at runtime in
  the game's own character-build UI, drawn as owned half-scale nine-sliced
  layers with a shadow; exact fallback to the previous look. References:
  KingmakerGunslinger's Teleport modal (the native world-map dialog) and
  KingmakerDiceRoller's Roll for Stats (the same paper sheet). No game or
  third-party art is committed or packaged (`docs/VISUAL-THEME.md`). Live
  fixes: translucent washes drop their Outline (`7383347`); the header rule
  follows the routine bar (`d8b659e`). Screenshots: `kbp040-rc3-sel-1080`
  and `kbp040-rc3-sel-720` (`physical-cf-graph*.png`, `physical-cf-inspect*.png`).
  Input: the description's wheel scrolls only itself (the graph beneath
  did not move), Escape closes the description, then the planner, never the
  game menu; no world input leaked.

### D. Testing

- Per package: focused prefixes (`spellbook-`, `execution-policy-`,
  `direct-manipulation-`, `catalog-audit-`, `sticky-qual-`, `wp7-`,
  `physical-`), Validate-Source and production builds (warnings are errors
  at level 4) after every change; regressions failed against the prior
  behaviour where a mutation check was made (provider rows, settled touch,
  WP3/WP4/WP5 regressions as listed in their sections).
- Complete C# suite on rc3: **471/471**. Validate-Source 42/42.
- Complete `Test-SourceOnly.ps1` on rc3: see "Complete gate" below.
- Runtime (guarded, rc3): 15 runs PASS - `kbp040-rc3-sel-sticky`,
  `-cast-sticky-instant`, `-cast-sticky-animated`, `-sel-1080`, `-sel-720`,
  `-combat-instant`, `-combat-animated`, `-authoring`, `-spellbook`,
  `-wsqual`, `-catalog-cotw`, `-catalog-native`, `-catalog-full`; plus the
  advanced inspection `kbp040-rc1-insp-adv`. Earlier package-era runs are in
  the sections below.
- NOT RUN, and why: the strict-Instant blocker natively (no animated-only
  source exists in any guarded fixture after WP6); group re-centring and
  Share visibility natively (no group spell or Share caster in the
  automation party; physical scenarios are not admitted on the advanced
  copy); the human-reproduction catalogue (its BagOfTricks fixture identity
  no longer matches - environment drift, the profile is the owner's); a
  catalogue export on the advanced copy (not an admitted scenario); 2560x1440
  (this session's display is 1920x1080) and an alternate in-game UI scale
  (would change the owner's game settings).

### E. Candidate identity

| Item | Value |
|---|---|
| Commit | `c452e01b360f380b9ca9268e0be3ea05d1b288c6` |
| Internal version | 0.4.0 (`KbpVersion` 0.4.0; `Info.json` 0.4.0; assembly and file version 0.4.0.0; informational 0.4.0) |
| ZIP | `C:\Dev\KingmakerBuffPlannerLab\repo\KingmakerBuffPlanner-K042\artifacts\release\0.4.0\KingmakerBuffPlanner-0.4.0.zip` (copy: `C:\Dev\KingmakerBuffPlannerLab\package-archive\0.4.0-rc3\`) |
| ZIP SHA-256 | `cc50fb3c7629bc8cf3a03467780e72f2f0f1ec787cd83b1ce54e8de31a59b480` |
| DLL SHA-256 | `d6273e74a65b2977d7549e8d5f9d66e53e67a44a86090e7d961273ab56f47858` |
| DLL MVID | `7654b673-66fb-4972-ad5e-d4aa54a1a029` |
| Manifest | `release-manifest.json` beside the ZIP: `deterministicBuilds: 2`, `validated: true`, `publicationStatus: local-only` |
| Layout | `KingmakerBuffPlanner/{Info.json, KingmakerBuffPlanner.dll, NativeEffectOverrides.json, THIRD-PARTY-NOTICES.md}`; package validation 4/4 |
| Runtime identity | every rc3 run loaded exactly this package (same SHA-256, DLL and MVID), frozen at `runtime-backups\qualification-frozen\c452e01...\FREEZE.json` |

### F. Safety

- Disposable fixtures only: `KBP_AUTOMATION_WORKING` (full-user profile) and
  the owner-approved advanced copy `KBP_ADVANCED_WORKING`
  (`advanced-gunslinger-0136`); baselines never loaded for writing; no save
  written; protected saves compared and clean on every save-backed run.
- Every run staged the candidate transactionally and restored the `Mods`
  folder (and the game's registry display values for windowed runs)
  byte-exact: all 33 mission transactions are `Restored` with restoration
  verified; no deployment lock, no staging left, no dispatcher claim. The
  candidate is not installed.
- Casting allowances were written mechanically by `New-KbpRunAllowance.ps1`
  under this mission's authority, one per run, for the frozen build.
- No other lab's lease or process was touched; no guard was weakened; no
  merge, tag, release or release asset; no Gunslinger change.

### G. Owner-review checklist (main game)

Install the ZIP after backing up `Mods\KingmakerBuffPlanner` (with
`UserSettings`) outside `Mods`; Unity Mod Manager should list 0.4.0.

1. Open the spellbook (B) and click the Buff Planner button (below the
   window's close button): the spellbook closes and the planner opens.
2. Confirm the planner opens correctly on the scroll paper, all lanes
   readable.
3. Pick a buff, a caster and its exact source row, click a recipient (one
   casting, selected), then click the same recipient again: it is removed.
4. Add a direct buff again and click another recipient: it moves (an
   occupied or unreachable recipient is refused with the reason).
5. Add a group buff, click another portrait (re-centres), then its centre
   portrait (removes).
6. Confirm the simplified sidebar: no Disable, Duplicate, Cast By or
   required-recipient menus, no obsolete header text.
7. Confirm Share Transmutation appears only for a Brown-Fur Transmuter
   source that can use it.
8. Start a fight and press a HUD routine: "Buff routines cannot run during
   combat."; nothing is spent.
9. Compare Instant and Animated (Mode button): Instant casts at once;
   Animated plays the normal casting.
10. In Instant mode cast Magic Circle against Alignment on an ally: it is
    instant (no casting animation), the slot is spent once, the circle
    appears.
11. Browse the buff catalogue for anything that is not a real buff.
12. Confirm Hideous Laughter and Treat Affliction (and Treat Deadly Wounds)
    are absent; note whether losing Light, Daylight or Elemental Bastion is
    acceptable.
13. Inspect the parchment planner at your own resolution.
14. Right-click a spell: the description is a spell scroll; the wheel
    scrolls it; Escape or a click outside closes it.
15. Run ordinary Long / Important / Short routines from the HUD.
16. Make a casting Not Ready (for example remove its slot) and confirm Not
    Ready navigation still focuses it.
17. Use Undo, close and reopen the planner and reload the save: the plan
    persists.
18. Report PASS or the exact defects.

### Complete gate

`scripts\Test-SourceOnly.ps1` on rc3 (worktree `KingmakerBuffPlanner-K042`, clean): PASS, exit 0 (05:56:26 -> 11:12:23); layer counts in the rc4 report's "rc3 complete gate (historical)".

## Package status (history)

| Package | Commits | Regressed | Source-qualified | Native-qualified | Owner |
|---|---|---|---|---|---|
| WP2B spellbook entry | `e84a892` `e29c4ec` `11a30db` | yes | focused | physical PASS (`kbp040-wp2b-spellbook-02`) | pending |
| WP4 execution policy | `d0d43e4` `512af36` | yes | focused + full unit | physical `combat` expectation built (`fe06e26`); runs in the campaign | pending |
| WP3 direct manipulation | `0274b7c` `898cfeb` `14c7902` | yes | focused + full unit | physical `authoring` expectation built (`fe06e26`); runs in the campaign | pending |
| WP6 Magic Circle | `3c80ed0` + recipe `ad7ca01` `2dde708` `2ec8e63` | yes | focused + full unit | `sticky-touch-direct` recipe built; runs in the campaign | pending |
| WP5 catalogue audit | `d8614ef` | yes | focused + full unit | catalogue exports PASS (native-only, full-user); in-game buff list spot check | pending |
| WP7 parchment | `d85e6e5` `ff9e877` `17d7420` `91d1c6e` `7383347` `d8b659e` | yes | focused + full unit | live paper drawn at 1920x1080 and 1280x720 (`kbp040-wp7-sel-*`) | pending (visual judgement is the owner's) |

"Full unit" is the complete `KingmakerBuffPlanner.Tests.exe` suite (446/446 at
`d8614ef`), not the complete `Test-SourceOnly.ps1` gate.

The complete `Test-SourceOnly.ps1` gate runs once on the final integrated
head.

## WP2B — spellbook Buff Planner button

Root cause (from the recorded native UI contract,
`runtime-evidence\beta-0dd82937-adv-01\native-ui-contract.json`, 1920x1080):

1. The owned button was pinned 20/18 units inside the spellbook window's
   top-right corner. `SpellBook/BookBackground` is full screen and the
   service window's top-bar close button `ServiceWindow/Top/Close`
   (143x137, anchored top-right) occupies exactly that area, so a click
   reached the native Close, not the planner.
2. The handoff searched the spellbook subtree for a `Button` named
   `Close`. The only such button is `ServiceWindow/Top/Close`, outside that
   subtree (the spellbook's own "Close" nodes are per-slot containers whose
   buttons are named `Button`), so every click that did arrive was refused
   with `native-close-affordance-missing`.

Repair (`e84a892`): the button is the last child of the service-window root
(drawn and raycast above the top bar) and is placed by
`SpellbookEntryPlacement` — an ordered candidate list checked against the
live native controls. The locator resolves the native close by its exact
path. Every handoff closes the window through it and waits until both the
FullScreenUi mode and the window have released; a failure after closure
disposes any half-open planner and reopens the spellbook through the game's
`IServiceWindowUIHandler.HandleOpenSpellbook`. Strays from an earlier
controller are retired. No second entry route was added.

Guarded scenario (`e29c4ec`, corrected in `11a30db`): `live-workspace-physical
-PhysicalExpectation spellbook`. The host presses the game's own `OpenSpells`
key binding (read at runtime: `B`), clicks the button with the OS pointer and
presses Escape, three times, then repeats once with one simulated opener
refusal (`SpellbookHandoffFaults`, armable only in a locked runtime-test
session). The launcher gained a single unmodified A–Z `key` action.
`Test-SpellbookEntryEvidence.ps1` (26 cases) runs in Test-SourceOnly.

Evidence:

- `kbp040-wp2b-spellbook-01` on `e29c4ec` — FAIL by the check, not the
  product: every cycle handed off correctly, but the record required the
  FullScreenUi mode to be inactive after the click, and the planner's own
  input lease raises that mode on open (it refuses to open while another
  owner holds it). Rejected theory: "FullScreenUi still active means the
  spellbook kept ownership" — the native window was hidden and the
  workspace had opened, which its lease forbids under a foreign owner.
- `kbp040-wp2b-spellbook-02` on `11a30db` — PASS, complete, restoration
  verified, protected saves compared and clean. Package
  `a32ea34b16ac0ae436e2278455092b11c7211627104d37de6e1031be6b97e9c1`, DLL
  `a30c4e9de10bd90c7ca7d4aca70cc95602509da4979982ea16d37e66e6a7cbee`, MVID
  `b470a9d8-20c8-4a28-828b-04255484b3ec`. Each ordinary cycle: one owned
  button, one runtime listener, the first EventSystem hit at its centre is
  the button, placement `below-native-close` conflict free, one native
  release, one native close, one opener call, one workspace open, lease
  held, service window hidden, FullScreenUi owned by the planner, Escape
  closes the planner with the lease released and no native menu. Fault
  cycle: opener refused, no planner, spellbook reopened with its button.
  World input: no movement/ability commands, selection unchanged.
  Screenshots `spellbook-*-open.png` / `spellbook-*-after-click.png`.

Uncertainty: placement was observed at 1920x1080 only; other resolutions are
covered by the WP7 visual runs.

## WP4 — execution policy

`CastingExecutionPolicy` (Domain/Planning) is the single contract:
`OutOfCombatOnly = true`, `AllowAnimatedFallback = false`.

- **Never in combat.** `CastingWorkspaceSession.Apply` refuses first, before
  compilation, persistence flush, authorization or projection, while
  `Player.IsInCombat`, with exactly `Buff routines cannot run during
  combat.`; no casting is named or focused, the planner is not opened,
  nothing is spent. Every route (HUD moon/diamond/sun, planner Run, Ready
  Casts Only, Classic) goes through it; the executors keep their per-step
  refusal for combat that starts mid-run.
- **Strict Instant.** `ExplicitCastingCompiler.Compile(strictInstant)` makes
  a casting whose strategy is not instant-capable Blocked with
  `instant-route-unavailable:<strategy>:<reason>`, so WP2A navigation
  focuses it; an already-active skip stays a skip; `HybridCastExecutor`
  runs strict and refuses such a step before any native work. Animated is
  explicit through the Mode button.
- **Profiles.** 0.3.0's `outOfCombatOnly` / `allowAnimatedFallback` stay in
  the schema, are read but not honoured
  (`LegacyExecutionPreferencesOverridden`), are not rewritten on load, and
  the next deliberate save writes the enforced values. The two toggles are
  gone from the workspace and the classic settings view (shown as fixed
  rows).
- Tests: `ExecutionPolicyTests.cs` (combat refusal with a counting dispatch
  boundary, strict blocker + WP2A focus, legacy fallback profile, strict
  hybrid); legacy expectations updated in AutosaveLifecycle and
  ProductionExecution tests.
- Native evidence still owed: combat refusal in both modes with no spend,
  the strict-Instant blocker deep link, an explicit Animated run.

## WP3 — direct manipulation and the reduced inspector

- Portrait clicks (`CastingWorkspaceSession.DirectManipulation.cs`):
  nothing focused - add, or focus the existing casting of that buff on that
  portrait (first by order, then id); a focused direct casting - its own
  recipient removes it, another portrait retargets it, an unreachable one is
  refused with the reason and nothing changes; a focused group casting -
  re-centres; caster/source clicks change the provider (keeping target,
  routine, order, existing-effect policy and enhancements the new provider
  has; dropped ones are named; Undo restores).
- Removed from the normal UI: Disable/Enable (a legacy Disabled casting
  loads, never casts, blocks nothing, can be removed; the qualification
  driver keeps a test-only seam), Duplicate, Cast By, the retarget/coverage
  menus, the required-recipient group menu and the obsolete header text.
  Share Transmutation rows appear only where the mechanism applies (a saved
  inapplicable Share stays visible with its reason).
- Group semantics: nobody is "required"; a member outside the area never
  blocks or doubles a cast; confirmation needs at least one reached
  recipient for a mass cast. 0.3.0 required-recipient choices load, are
  archived byte-exact once (`*.pre-0.4.0.orig`) on the first save, and no
  longer constrain anything.
- Tests: `DirectManipulationTests.cs` (11) plus updated graph, navigation,
  converter and importer tests.
- Physical evidence still owed: add/remove/retarget/recentre/provider
  change/Share visibility/Undo, and input isolation.

## WP6 — Magic Circle against Alignment

Diagnosis: Magic Circle is registered by KingmakerGunslinger
(`KMG.Spells.MagicCircle.*`; installed 0.0.140, advanced fixture 0.0.136).
Its castable ability holds a touch (`AbilityEffectStickyTouch`) whose
delivery can target enemies but is `EffectOnAlly = Helpful`,
`EffectOnEnemy = Helpful`, with an ally branch that applies the carrier and
a non-ally branch behind a Will save. The planner classified every
enemy-capable delivery `AnimatedFallback`
(`sticky-delivery-hostile-targeting-ambiguous`), and 0.3.0's default
`allowAnimatedFallback = true` cast it through the slow two-command animated
path; under WP4's strict Instant it would have been Not Ready. Ownership:
the Buff Planner (the Gunslinger shape is correct). No Gunslinger branch or
change.

Repair (`3c80ed0`): an enemy-capable delivery that is Helpful to allies and
not Harmful to enemies is `StickyTouchDeliveryRuleCast`
(`supported-willing-target-sticky-touch-delivery`); the instant adapter
requires the native touch auto-hit condition (caster, or non-enemy,
non-neutral, unconfused target) before each delivery
(`sticky-delivery-not-auto-hit:<reason>`). Structural only.
Native evidence: a new `sticky-touch-direct` qualification recipe is being
added so the guarded casting harness can cast it on an ally in Instant and
Animated on the advanced fixture.

## WP5 — beneficial-buff catalogue audit

> rc4 corrects this package after the lead review (marker rules, the Call of
> the Wild adapters, revivals, ownership): see the rc4 report at the top and
> `planning/CATALOG-AUDIT-0.4.0.md`. The text below is the rc3-era record.

Diagnosis (from the full-user catalogue export of `wp1-4480e15-core-01`):
Hideous Laughter entered through Call of the Wild's Infectious Charms caster
rider (a Caster-target effect is always a safe recipient); Treat Affliction
and Treat Deadly Wounds through a visible, component-free cooldown counted
as a payload (live discovery classifies a party member's ability as
reachable, so the static accessibility index does not protect it).

Repair (`d8614ef`): structural rules in the classifier (hostile ability
rider, save-gated effect, harmful condition / faction change, mechanics-free
buff, hidden bookkeeping marker, restoration tracker), one shared facts
builder for live discovery and the export, and an in-game audit
(`native-buff-catalog-audit.json`, catalog schema 5) with before/after per
profile computed in the same run. Details and the rule table:
`planning/NATIVE-BUFF-COVERAGE-MATRIX.md`. Rejected: a display-name or GUID
list (not needed: every reported entry has a structural cause); an
enemy-branch rule (only 3 extra removals, all also caught by the
harmful-condition rule, and it would change the conditional identity that
saved plans match on).

WP5 native catalogue evidence (package `aeb5c52b14d827856b495bcb96b75cadc55f775b701eb31484f371c44f8a5bbc`,
DLL `74f151fc4e71a83d3921da838177db72bc198390877a7d95f615b25e830c4491`, MVID
`a94a4493-df0e-4647-aaa7-9f45d08f6b99`, commit `d8614ef`; the discovery code is
unchanged after it). Static = the exact player graph; live = every ability
classified as reachable (how live discovery judges a party member's
abilities). Before = the 0.3.0 rules, computed in the same run.

| Profile | Run | Result | Static before -> after | Live before -> after |
|---|---|---|---|---|
| native-only | `kbp040-wp5-catalog-native-01` | PASS, restored | 379 -> 374 | 516 -> 505 |
| call-of-the-wild | `kbp040-wp5-catalog-call-of-the-wild-01` | FAIL by the profile's stale expected blueprints (see below); restored | 2355 -> 2268 | 3413 -> 3301 |
| full-user | `kbp040-wp5-catalog-full-user-01` | PASS, restored | 2418 -> 2323 | 3490 -> 3366 |
| human-reproduction | `kbp040-wp5-catalog-human-reproduction-01` | BLOCKED before deployment: `Compatibility fixture identity mismatch: BagOfTricks` (environment drift; the profile is the owner's) | - | - |
| advanced-gunslinger-0136 | - | not runnable: the advanced copy only admits its listed scenarios | - | - |

Removed by rule (full-user): hostile-ability-rider 38, mechanics-free-marker-only
39, reactive-restoration-marker-only 36, harmful-only 6, offensive-carrier-only 5.
Native-only removes 11: Dazing Touch, Eyebite, Inspiring Recovery, Light,
Targeted Bomb Admixture (static) and Daylight, Elemental Bastion, Stunning
Barrier stun, The Binding of the Prince, Treat Affliction, Unburrow (live).
Spot checks in the full-user audit: Hideous Laughter, Treat Affliction and
Treat Deadly Wounds removed; Mage Armor, Haste, Bless, Prayer and Freedom of
Movement kept. The full-user ownership split is not meaningful (the
ownership index attributes only Call of the Wild), so per-mod counts come
from the per-profile runs.

The call-of-the-wild run's own check required Regenerative Sinew:
Restoration (now deliberately excluded) and Globe of Invulnerability (already
excluded since 0.0.17, so the check was stale on the 0.3.0 baseline). The
profile now asserts three still-included representatives (Dazzling Blade,
Bless Weapon, Fortune; see `docs/CALL-OF-THE-WILD-COMPATIBILITY.md`); the
profile must be re-run on the candidate.

Uncertainty for the owner: Light, Daylight and Elemental Bastion leave the
catalogue because their buffs carry no mechanics of their own (cosmetic or
presence-keyed). The same structural rule removes about twenty activation
and selection markers (School Understanding, Cold Snap, Deadeye, Heaven's
Leap, Wild Flanking Partner) that must not be planned; Elemental Bastion is
the one entry whose exclusion may be a loss. An include override in
`NativeEffectOverrides.json` would restore it if wanted.

## WP7 — parchment and spell-scroll visual overhaul

References (details, provenance and licensing: `docs/VISUAL-THEME.md`):
the Teleport modal of KingmakerGunslinger (it is the native world-map
`GlobalMapMessageBox`; the mod draws only hairline dividers) and Roll for
Stats in KingmakerDiceRoller (it borrows the native `dialogue_backsheet`
sheet at runtime and draws it as a half-scale nine-sliced layer with a soft
shadow). Neither ships game art, and neither does the planner.

Implementation (`d85e6e5` `ff9e877` `17d7420` `91d1c6e`, merged): two new
capabilities in the existing native-theme resolver, each with an exact
contract checked before use and failing on its own:
`ScrollPaper` (`dialogue_backsheet`, 858x551, border 268/169/258/165, 200 PPU)
and `ScrollRule` (`blockscroll_bottom`, 147x11, border 20/0/20/0), both found
under the in-game `CharacterBuild` UI. The workspace frame and the spell
description draw the paper as a layer at scale 0.25 x spritePPU / canvas
reference PPU (0.5 at the expected 100), so the borders are drawn at
67/42.25/64.5/41.25 units and never stretched; the paper overhangs the frame
so its folded bands stay outside the header and footer. Any failure restores
the previous flat look exactly. The description is a spell scroll (title,
duration/meta line, rule, native read-only body with its own wheel); Escape
closes it first; a complete click outside closes it and is consumed. The
theme summary and `physical-workspace.json` (`workspacePaperEvidence`,
`inspectPaperEvidence`) record what was drawn. Tests: `wp7-*` (9).

Display: this session's display is 1920x1080, so the launcher refuses
`windowed-2560x1440`; `168ad8d` adds `windowed-1600x900` and
`windowed-1280x720` as the meaningfully different supported sizes. An
alternate in-game UI scale would change the owner's game settings and was not
practical.

## Integration

Integration branch `claude/kbp-040-integration` (worktree
`private\worktrees\KBP040-INT`): `fe06e26` + merge of the WP6 qualification
recipe branch `claude/kbp-040-wp6-sticky-recipe` (`ad7ca01` `2dde708`
`2ec8e63`) + merge of `claude/kbp-040-wp7-parchment`; conflicts were only
the two test-registration lines (union). Full unit suite 468/468;
Validate-Source 42/42.

Live WP7 findings (each fixed with a regression):

- `kbp040-wp7-sel-1080-01` (on `3cb3f90`, windowed 1920x1080, PASS): both
  donors resolved (`paper=native`, refPPU 100, borders 67/42.3/64.5/41.3,
  undistorted; ornament rules drawn), the description read as a spell
  scroll, but every lane well looked like an opaque reddish-brown slab:
  Unity's Outline effect fills a translucent wash with its colour. Fixed in
  `7383347` (a translucent wash drops its Outline).
- `kbp040-wp7-sel-1080-02` (on `7383347`, PASS): the paper shows through
  every lane; cards, portraits and inks legible. `kbp040-wp7-sel-720-01`
  (1280x720, PASS): every key control on screen and the description scroll
  intact, but the header rule struck through the "Routine:" row. Fixed in
  `d8b659e` (the rule sits in the gap above the routines, or is hidden).
  Known 720p limitation, not WP7: the catalogue's Spells/Abilities tab
  captions wrap at that width.
- The same runs are the in-game WP5 spot check on the standard party: the
  buff list shows Aid Another (AC / Attack) and Resistance only - Light,
  Treat Affliction and Treat Deadly Wounds, offered by 0.3.0, are gone.

WP3 defect found while building the physical gesture run (`14c7902`): with
a casting focused, a caster who can cast the buff in more than one way was
refused, but the graph draws source rows for the chosen caster only and the
refusal left the previous caster chosen, so that caster's rows never
appeared and the provider could not be changed to it. The ambiguous click
now shows that caster's rows (focus kept); the regression fails without it.

Physical evidence harness (`fe06e26`, `3cb3f90`): `live-workspace-physical`
gains `-PhysicalExpectation combat` (a moon press while the host holds a
party member in the game's combat state - `UnitEntityData.JoinCombat`, no
enemy, the group's leave timer kept at zero - must be refused for combat
before dispatch with nothing spent; then the ordinary press reaches the
lock) and `-PhysicalExpectation authoring` (caster, exact row, portrait add,
same-portrait remove, re-add, retarget, provider change when a second caster
is on screen, Undo, card focus, Escape leaves the focus, Escape closes).
The launcher's outcome checks and their fixtures cover both. The launcher
meta-test (`Test-RuntimeLauncherFileWhatIf.ps1`) passes all its layers with
the three lab-wide purity windows stubbed (`PASS=13`); the windows run in the
final complete gate.

Strict-Instant blocker, native: not runnable on the 0.4.0 candidate. After
WP6 no guarded fixture has an animated-only source (the advanced fixture's
only ones were the Magic Circle variants; the standard party has Resistance,
Aid Another and the Heal skill). The blocker, its WP2A deep link and the
strict hybrid refusal are proven by the real compiler, gate, navigation and
executor in `ExecutionPolicyTests`. Explicit Animated is exercised natively
by the `sticky-touch-direct` animated casting run.

