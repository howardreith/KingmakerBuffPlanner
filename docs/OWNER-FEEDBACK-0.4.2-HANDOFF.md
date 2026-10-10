# 0.4.2 owner-feedback follow-up: evidence handoff

This is the follow-up to the owner's 0.4.1 feedback (prompt
`Claude_KBP_041_Followup_Prompt.md`, screenshots `references/01..05`, and the
continuation instructions of 2026-10-10). It produced a **local owner-test
candidate**. Nothing was merged, tagged, published, released or permanently
installed. No main, owner or protected save was touched, and no unrelated
process was stopped.

## Identity

| Item | Value |
| --- | --- |
| Base | v0.4.1, main `ab62462fa62c2e7a13a4c33a7a2947cd11b77b6d` (no later `main` commits to preserve) |
| Branch | `codex/kingmaker-buff-planner-041-owner-polish` |
| Version | 0.4.2 (`Version.props`, `Info.json`, assembly 0.4.2.0) |
| **Tested product commit** | `e9a4c5ed3d34825aa0332cb3d64067b206e29070` |
| Owner-test package | `artifacts\release\0.4.2\KingmakerBuffPlanner-0.4.2.zip` in the worktree (local only; with `release-manifest.json`) |
| Package SHA-256 | `3f3d55af66aa33be94e28a50fa7247a78c5fb006ac292237617fa847326b3463` (two deterministic builds) |
| DLL SHA-256 | `b3f5023d74f539d5aa4452b7b0f43f64e69930b5f561fc9e4aff0b72498e7e0a` |
| DLL MVID | `bcb3d03e-6bc9-48a3-8ff9-4b98dfb6f1c1` |
| Native runs on these bytes | `kbp042-removal-05` and `kbp042-paper-02`: local-runtime package `3f3d55af66aa33be94e28a50fa7247a78c5fb006ac292237617fa847326b3463`, DLL `b3f5023d74f539d5aa4452b7b0f43f64e69930b5f561fc9e4aff0b72498e7e0a`, MVID `bcb3d03e-6bc9-48a3-8ff9-4b98dfb6f1c1` |
| Complete gate | `scripts\Test-SourceOnly.ps1` (unchanged) on the clean tested commit `e9a4c5e`: **exit 0**, `Source-only suite: PASS=1 FAIL=0`, 2026-10-10 05:05:55Z to 10:47:57Z (5 h 42 min) |
| Records | later commits on the branch change records and documentation only; the package contains no documentation, and the candidate was built at the tested product commit |
| Draft PR | see "Branch and draft PR" |

## Commits (product series, oldest first)

| Commit | Series | What |
| --- | --- | --- |
| `88f0b87` | - | Version 0.4.2 |
| `4aef348` | A | A class ability is its own buff entry (Ninja Shadow Clone) |
| `03a6098` | B | Retire castings of a spell removed from its spellbook |
| `15f2182` | C | Native card paper on the table, and the native paper-opening cue |
| `11cec99`, `73e75de` | C (diagnostic) | Native paper donor geometry, previews and UI sound table (runtime evidence only) |
| `783cfc4` | C (evidence) | Native reference screens and open-cue counts in one session |
| `0b4b5a0` | B (evidence) | Physical `removal` expectation |
| `eb80b14` | B (fix) | The host's request validation accepts `removal` (kbp042-removal-01); launcher/host parity |
| `d85818a` | B (fix) | A missing mod never removes castings; admission and routing of the exact launcher request proven |
| `9e57be0` | B (evidence) | The removal run proves the reconciled plan, kept intent, blocker and resources |
| `0f2a24d` | B (evidence) | The removal run fits the approved fixture (kbp042-removal-02) |
| `e9a4c5e` | B (evidence) | `live-workspace-removal`: the reconciliation without OS input |

Documentation and records commits: `6788780` (0.4.2 docs), `5284b5d` (interim
checkpoint), and the final records commit that adds this report.

## A. Shadow Clone

**Native ability (exact contract).** Call of the Wild 1.14.4c-2.1,
`NinjaShadowCloneMirrorImage` `335a48ec4134454fb491b3d64f468ada`, "Shadow Clone".
- Granted by `NinjaShadowCloneFeature` `85f19cdf...`, a Ninja trick of the
  Rogue class's Ninja archetype (`20785e18...`). Tweak or Treat's Sylvan
  Trickster can also take it.
- Supernatural, Standard action, Personal (self only).
- **Resource:** `AbilityResourceLogic` on `NinjaKiResource`
  `74b56c9eba8f43ac9166b5fa93e50197`, cost 1.
- **Effect:** one `ContextActionApplyBuff` of `MirrorImageBuff`
  `98dc7e7cc6ef59f4abe20c65708ac623` on the caster, exactly the effect tree
  of the wizard spell Mirror Image `3e4ab69ada402d145a5e0ad3ad4b8564`.
- **Discovery:** correct before the fix. The classifier included it
  (`valid-beneficial-self-effect`), and the party snapshot built its ki
  provider. Source: 0.4.1 catalogue export `kbp041-rel-catalog-full`;
  ownership `call-of-the-wild`.

**Discovery failure (root cause).** A catalogue source was identified by its
effect fingerprint (`effect|sha256`). The workspace served an id only through
the reference of its representative expression instance, and Mirror Image
sorts first. Shadow Clone therefore shared Mirror Image's id, served nothing,
and disappeared from the casting-first buff list. The classic card pooled the
Ninja's ki into Mirror Image instead.

**Fix** (`4aef348`, `Domain/Effects/CatalogSourceIdentity.cs`):

```diff
-            return string.IsNullOrWhiteSpace(ability.VariantGuid)
-                ? EffectAggregateIdentity.For(expression, ability.Canonical)
-                : VariantPrefix + ability.BaseAbilityGuid + "|" + ability.VariantGuid;
+            if (!string.IsNullOrWhiteSpace(ability.VariantGuid))
+                return VariantPrefix + ability.BaseAbilityGuid + "|" + ability.VariantGuid;
+            if (ability.SourceKind != SourceKind.Spellbook)
+                return AbilityPrefix + ability.BaseAbilityGuid;
+            return EffectAggregateIdentity.For(expression, ability.Canonical);
```

The supporting changes:
- `RoutinePlanService` matches through the same function.
- `PlannerSetupModel` rebinds a classic assignment through its persisted
  ability.
- `CastingSourceIdentityMigration` rewrites a stored class-ability casting's
  stale id once.
- `CastingPlanRepository.ArchivePrimaryOnce` keeps the exact pre-0.4.2 file
  (`.pre-0.4.2-source-identity`).

There is no name list, forced provider, buff injection, blueprint change,
classifier change or compile-time dependency.

**Evidence.** These regressions run against the installed ability's facts:
- `shadow-clone-has-its-own-catalogue-entry`;
- `shadow-clone-authors-on-its-owner-with-one-ki`;
- `shadow-clone-exhausted-ki-keeps-entry-and-casting`;
- `shadow-clone-absent-without-owner-or-optional-mod`;
- `shadow-clone-saved-under-shared-identity-migrates-once`;
- `class-ability-identity-keeps-spells-aggregated`;
- `classic-mirror-image-never-plans-shadow-clone`.

With the new rule disabled, 6 of the 7 fail with "Shadow Clone is not in the
buff list: Mirror Image".
**Native Shadow Clone cast: NOT RUN.** No approved fixture has a Ninja. The
automation party is a fighter, a bard and a sorcerer; the advanced copy is a
cleric, an alchemist, a fighter and an arcanist. The owner's character and
main save were not edited.

## B. Spellbook removal

**Root cause.** Nothing reconciled saved castings with the native books.
Castings of a spell that was no longer prepared or known stayed in the plan as
blocked "unavailable saved buff" castings: the owner's stale Mind Blank
records.

**Native contract** (2.1.7b, Assembly-CSharp MVID
`07fa1e4d-8618-41b3-9b8d-faa17d3b26f7`, read from the installed IL by
`spellbook-removal-native-contract-2-1-7b`):
- `Memorize` fills a slot's `Spell` and sets `Available=false` until rest.
- `Spend` clears only `Available`.
- `ForgetMemorized` calls `SpellSlot.Clear` and then `ClearInternal`, which
  sets `Spell=null`.
- `GetAllMemorizedSpells` filters on `Spell` only.
- `RemoveSpell` removes the spell from the known, custom and special lists
  and clears its memorized slots.
- `AbilityData.SpendFromSpellbook` calls `Spellbook.Spend`.

"Still a member" separates a real removal from a resource spend. A prepared
spell is a member while it is in any memorized slot, spent or not. A
spontaneous spell is a member while it is known, special or custom.

**Decision table** (`Planning/SpellbookRemovalReconciliation.Decide`, pure):

| Observation for the casting's exact caster and book | Result |
| --- | --- |
| Not a spellbook casting (ability, item, scroll) | Not considered |
| No caster or book on the casting | Kept: `no-caster-or-book` |
| No membership read | Kept: `membership-unknown` |
| Party read unstable: loading, no player, or a service window (the spellbook) open | Kept: `membership-unstable:<reason>` |
| Caster or book not in the read (caster out of the party) | Kept: `book-not-observed` |
| Book `CasterUnavailable` (dead or unconscious), `RoleNotProven` (an optional mod's spellbook contract unproven), or `Unreadable` | Kept: `book-<status>:<reason>` |
| Book complete and still holds the exact member (base or variant parent, plus metamagic mask), even with every slot spent or no uses left | Kept: `still-held-by-the-book` |
| Book complete, member gone, but the running game does not know the spell (its mod is not loaded) | Kept: `spell-not-in-this-game` (added in `d85818a`) |
| Book complete, member gone, and the casting was restored by Undo under the same observation (same book digest) | Kept: `undo-restored-same-observation` |
| Book complete, member gone, otherwise | **Removed**: "no longer prepared" (prepared book) or "no longer known" (spontaneous) |

**Session guards** (`CastingWorkspaceSession.ReconcileSpellbookRemovals`):
- **Never:** during a run or submission, in combat, or with the stored plan
  or a legacy import unresolved.
- **Only at idle boundaries:** when the planner opens, and in `Apply` before
  the gate on every route, the HUD included.
- **One edit:** removals are one `CastingAuthoringService.RemoveCastings`
  command, with one history entry, one autosave and one Undo.
- **Archive first:** the stored file is archived once beforehand
  (`.pre-spellbook-removal`). An archive or save failure fails the save and
  refuses the run (`persistence-failed:`).
- **Persistence:** a restart observes again; the suppression after an Undo
  lasts only for the session; the persistence schema is unchanged.

Core of the fix (`03a6098`):

```csharp
SpellbookMembershipFact book = membership.Find(casting.CasterUnitId, casting.SpellbookGuid);
if (book == null) { reason = BookNotObserved; return null; }
if (book.Status != SpellbookMembershipStatus.Complete) { reason = "book-" + book.Status + ":" + book.StatusReason; return null; }
string member = SpellbookMembershipFact.MemberKey(casting.Ability.BaseAbilityGuid, casting.Ability.MetamagicMask);
if (book.Copies(member) > 0) { reason = StillMember; return null; }
if (!membership.SpellResolves(casting.Ability.BaseAbilityGuid)) { reason = SpellUnresolved; return null; }   // d85818a
```

**Corrections found during qualification:**
- `eb80b14` (kbp042-removal-01): the host's request validation kept its own
  list of physical expectations and rejected `removal` at boot
  ("invalid-request:physical-expectation"). There is now one
  `RuntimeTestProtocol.PhysicalExpectations` list.
- `d85818a`: when the mod that provides a spell is not loaded, the books read
  completely without the spell, so its castings looked removed. They are now
  kept (`PartySpellbookMembership.SpellResolves`, which resolves through
  `ResourcesLibrary.TryGetBlueprint`).

**Prepared-spell and safety evidence (unit level).** All regressions pass in
the complete gate. Each new case was confirmed red under its mutation.

| Required case | Regression |
| --- | --- |
| Removing one of several prepared copies keeps the castings | `spellbook-removal-one-of-two-copies-keeps` |
| Removing the final prepared copy removes them | `spellbook-removal-final-copy-unprepare` |
| Spending the final available prepared slot keeps them | `spellbook-removal-spent-slot-keeps-across-reload` (Felix's only Mind Blank slot spent, `Available=false`) |
| Spent prepared castings survive a reload | the same test (second pass reloads the plan from disk) |
| Pending rest (newly prepared, unavailable until rest) | the same native state as a spent slot (`Spell` set, `Available=false`), proven by `spellbook-removal-native-contract-2-1-7b`, so the spent-slot test covers it |
| Intermediate preparation (the spellbook open mid-edit) | `spellbook-removal-uncertainty-never-deletes`, case `spellbook-open-mid-edit` (the adapter's `native-service-window-open`) |
| Cold-load reconciliation of already-stale records | `spellbook-removal-cold-load-hud-first` (the owner's two Mind Blank castings, Felix and Leinna, retired on the first HUD press before the gate) |
| Exact caster, book and variant scope | `spellbook-removal-decision-table` (another caster's equivalent spell; two books on one caster), `spellbook-removal-variant-and-metamagic-identity` |
| Absent caster, missing mod, incomplete discovery: no deletion | `spellbook-removal-uncertainty-never-deletes` (membership not read, loading, run in progress, combat, caster absent, caster unavailable, optional-mod contracts missing, book unreadable, spellbook open, spell's mod not loaded) |
| Archive or save failure blocks the run | `spellbook-removal-save-failure-blocks-the-run`, `spellbook-removal-archive-failure-blocks-the-run` |
| Undo is not immediately re-pruned by the same observation | `spellbook-removal-undo-idempotence-and-restart` |
| Idempotence, and cleared Not Ready navigation | `spellbook-removal-undo-idempotence-and-restart`, `spellbook-removal-leaves-no-stale-problem-focus` |
| One compound, undoable edit | `spellbook-removal-compound-authoring-edit` |
| A known spell forgotten (retraining) | `spellbook-removal-known-spell-forgotten` |
| Request admission of the exact launcher request | `spellbook-removal-exact-launcher-request-admission` (the `-01` request, byte-identical, SHA-256 `ab541d58...`), `physical-expectation-launcher-host-parity` |
| Routing to the removal press | `spellbook-removal-request-routes-to-the-removal-press` |
| Run verdicts | `spellbook-removal-physical-judgement`, `spellbook-removal-no-input-scenario-judgement`, `spellbook-removal-no-input-scenario-admission-and-routing` |

**Native evidence: `kbp042-removal-05`, PASS.** This is the
`live-workspace-removal` scenario on the approved automation fixture, at the
tested product commit.
- **Fixture:** `KBP_AUTOMATION_WORKING` loaded once (gameId `df33d1ff...`),
  with no unexpected write.
- **Before the planner ever opened:** a cold Long seed (Linzi's Resistance),
  then five castings: `rm-known-long` and `rm-known-important` (Resistance,
  Tartuccio's book), `rm-other-book` (Resistance, Linzi's book),
  `rm-spent-important` (Light, Linzi's book), and `rm-unrelated-draft`
  (a Draft with no caster).
- **Native removal:** `Spellbook.RemoveSpell` took Resistance out of
  Tartuccio's book `b3db3766...` (known before: true; after: false).
- **Separate resource spend:** `AbilityData.SpendFromSpellbook` spent Linzi's
  level-1 slots with Ear-Piercing Scream, 2 → 0. Linzi still knows
  Resistance and Light.
- **The press:** Long was pressed through
  `BuffPlannerUiRoot.PressRoutineForRuntime("long")`, the call the HUD button
  makes, with no OS input.
- **Result:**
  - exactly `rm-known-important` and `rm-known-long` were removed
    (`Applied`, durable, archived);
  - stored afterwards: `seed-long-1`, `rm-other-book`, `rm-spent-important`
    and `rm-unrelated-draft`;
  - every kept casting's saved intent and order was unchanged;
  - the unrelated Draft still reads `Draft:caster-unresolved` (not waived);
  - the reconciled Long plan was `seed-long-1` only, and the press was
    refused by the session lock after the gate (`native-submission-disabled`);
  - no run started, the editor stayed closed during the press, and every
    resource pool was unchanged by the press.
- **Notice:** shown in the opened planner's footer, "Removed 2 Resistance
  castings: no longer known in Tartuccio's spellbook. Undo available."
  (`physical-removal-notice.png`). The planner then closed with its lease
  released.
- **Evidence hashes:** `removal-reconcile.json` `2567a44c...`,
  `runtime-result.json` `2c3d33d6...`, `physical-removal-notice.png`
  `5a45c839...`.
- **Scope:** this proves a spontaneous book's removal and a real resource
  spend. It is **not** a prepared-spell run.

**Earlier attempts, kept unchanged:**

| Run | Commit / package | Outcome |
| --- | --- | --- |
| `kbp042-removal-01` | `0b4b5a0` / `350e1c8c...` | Request admission failed: `invalid-request:physical-expectation`. Removal behaviour NOT RUN. The supervising shell was stopped by Claude Code under memory pressure, but the launcher and game ran on and finished. The working save was not loaded. |
| `kbp042-removal-02` | `9e57be0` / `227a7cce...` | Admitted and the fixture loaded. Stopped before any native edit: `removal:no-two-spontaneous-buffs`, because the fixture knows no level 1+ buff. |
| `kbp042-removal-03` | `0f2a24d` / `fa25782d...` | Native edits done. The physical moon click was refused: "Kingmaker foreground activation failed". A console window held the foreground, and the harness rightly refuses synthetic input. |
| `kbp042-removal-04` | `0f2a24d` / `fa25782d...` | Same as `-03`, on an idle desktop. The foreground window was this session's terminal console. |

For each of these runs:
- `Mods` was restored and verified, and the transaction reads `Restored`;
- no lock and no process remain;
- protected saves are clean, and both test saves are byte-identical
  (`eded233c...`, `f13de02d...`);
- the lab install is back to `0.1.1-rc3` (`78407dd4...`).

**Not run in the game:**
- Prepared-spell removal and spend: no mutable fixture has a prepared caster.
- A buff whose own pool is exhausted: the fixture's only spellbook buffs are
  unlimited cantrips.

Both are covered by the unit regressions above and by the owner test list.

## C. Native paper and sound

Details: [VISUAL-THEME.md](VISUAL-THEME.md), section "0.4.2".

**Root causes** (`15f2182`):
- **Paper.** 0.4.1 drew the 858x551 `dialogue_backsheet` dialog sheet. Its
  332x217-pixel centre was stretched over the whole frame (enlarged grain),
  its torn edge was drawn at a quarter size (a thin, straight frame), under
  30%/60% cream washes (writing-area luma 226 against the native 175-205),
  over a black veil.
- **Sound.** The casting-first open path never posted any sound.

**Donors** (read at runtime, never shipped; `kbp042-donors-02`,
`kbp042-paper-01`, `kbp042-paper-02`):
- **Page:** `Card_Big` (2048x1566.8, PPU 84.27984) at
  `ServiceWindow/Journal/Cart`. It is the same sprite as the character
  sheet's, journal's, settings' and character build's page. Drawn untinted
  through a planner-owned nine-slice over the game's texture.
- **Table:** `ServiceWindow_TableBackGruond_3840_2022` (2048x1024, PPU 100)
  at `ServiceWindow/Background`.
- **Fallbacks:** `dialogue_backsheet` stays the second tier, and the flat look
  the last.
- **Sound:** `UISoundType.CharacterScreenOpen`, Wwise event `JournalOpen`, the
  event the character screen, spellbook and journal post on open
  (`ServiceWindowTabs.PlayShowSound`).

**Before and after** (lab files, not in the repository: they show game art):
- **Before** (0.4.1, the owner's screenshots):
  `C:\Dev\KingmakerBuffPlannerLab\references\04-planner-041-normal.png` and
  `05-planner-041-unavailable.png`. The owner's native targets are
  `01-native-character.png`, `02-native-inventory.png` and
  `03-native-map.png`.
- **After, on the tested bytes:**
  - `runtime-evidence\kbp042-removal-05\physical-removal-notice.png`
    (1920x1080, the planner with the removal notice);
  - `runtime-evidence\kbp042-paper-02\spell-scroll.png` (the spell
    description on the same page);
  - `native-inventory.png`, `native-character.png` and `native-map.png` from
    the same session;
  - `paper-sound-evidence.json`.
- **After, 1920x1200:** `runtime-evidence\kbp042-paper-01\` (commit
  `783cfc4`; the paper code is unchanged up to `e9a4c5e`).

| Mean luma of the writing area (`kbp042-paper-01`, 1920x1200) | Top | Lower / right |
| --- | --- | --- |
| 0.4.1 planner | about 224 | about 224 |
| 0.4.2 planner | about 195 | about 171-176 |
| Native inventory / character pages | about 205 | about 159-165 |

On the tested bytes at 1920x1080, mean luma of empty paper regions:
- the planner page (`kbp042-removal-05`, `physical-removal-notice.png`): about
  202 at the top of the graph lane, 181 lower, and 162 in the right lane;
- the native character sheet's right page (`kbp042-paper-02`,
  `native-character.png`, inside the Effects panel): about 166 and 153.

The planner now carries the same top-to-bottom shading and sits in the
native range; its top band stays a little lighter.

Geometry on the tested bytes (`kbp042-paper-02`, 1920x1080):
`Frame:paper=native-sheet;sprite=Card_Big;unitsPerTexel=0.500;screenScale=1.000;spritePPU=200.0;borders=60.0/65.0/65.0/55.0;edges=20.0/28.0/20.0/6.0;size=1912x1030;undistorted=true`;
the spell scroll `SpellScroll:paper=native-sheet;sprite=Card_Big;unitsPerTexel=0.350`;
`backdrop=native;sprite=ServiceWindow_TableBackGruond_3840_2022`.

**Audio: event call versus listening.**
- **Event identity:** `UISoundType.CharacterScreenOpen`, posted through
  `UISoundManager.Play`, which maps to the Wwise event `JournalOpen`.
- **Invocation counts** on the tested bytes (`kbp042-paper-02`), as
  open/cue pairs:
  - first open: 1/1;
  - after two refreshes: 1/1;
  - after the spell description: 1/1;
  - first reopen: 2/2;
  - second reopen: 3/3.
- **Assertion:** `paper042-one-open-cue-per-open` passed.
- **Listening: NOT RUN.** The automation proves the call and its count, not
  what reaches the speakers.

**Regressions:**
- `paper-042-native-sheet-and-table-contracts`;
- `paper-042-sheet-and-table-resolve-and-fall-back`;
- `paper-042-sheet-geometry-keeps-native-edges`;
- `paper-042-sheet-is-drawn-untinted-on-the-table`;
- `sound-042-one-native-paper-cue-per-open`;
- `wp7-inks-stay-legible-on-the-paper` (explicit thresholds on the darker
  sheet).

## Runtime runs (disposable automation fixture only)

| Run | Commit | Scenario | Result | Mods restored | Protected saves |
| --- | --- | --- | --- | --- | --- |
| `kbp042-donors-01` | `ac343d8` (pre-cherry-pick) | live-advanced-inspect (Automation) | PASS, complete | verified | clean |
| `kbp042-donors-02` | `13923c7` (pre-cherry-pick) | live-advanced-inspect (Automation) | PASS, complete | verified | clean |
| `kbp042-paper-01` | `783cfc4` | live-advanced-inspect (Automation), 1920x1200 | PASS, complete | verified | clean |
| `kbp042-removal-01` | `0b4b5a0` | live-workspace-physical `removal` | FAIL: request admission | verified | clean |
| `kbp042-removal-02` | `9e57be0` | live-workspace-physical `removal` | FAIL: fixture mismatch, before any edit | verified | clean |
| `kbp042-removal-03` | `0f2a24d` | live-workspace-physical `removal` | FAIL: foreground refused | verified | clean |
| `kbp042-removal-04` | `0f2a24d` | live-workspace-physical `removal` | FAIL: foreground refused | verified | clean |
| **`kbp042-removal-05`** | **`e9a4c5e`** | **live-workspace-removal** | **PASS, complete** | verified | clean |
| **`kbp042-paper-02`** | **`e9a4c5e`** | **live-advanced-inspect (Automation), 1920x1080** | **PASS, complete** | verified | clean |

After every run:
- the transaction reads `Restored`;
- no deployment lock, foreign lease or game process remains;
- the working and baseline saves are byte-identical to the fixture record;
- the lab install is `KingmakerBuffPlanner 0.1.1-rc3`, DLL SHA-256
  `78407dd4c7240ce25c7654715854e83061da744a18d8eb4a2198fdffec5e3080`.

**Resources.** Before each launch I measured free physical memory and commit
headroom. There is no lab memory policy, so the threshold I declared in
advance was 6.5 GiB free and 10 GiB commit. `-05` launched at 6.77 GiB, and
`-03` and `-04` above the threshold. One launch was held back at 6.02 GiB
until it recovered. No unrelated process was stopped, and no paging or limit
was changed. For the foreground diagnosis, this session's own terminal window
was minimized and later restored without activation. A minimize call on
another session's console window returned false and had no effect. No other
window or process was touched.

## Gate and candidate identity

`scripts\Test-SourceOnly.ps1`, unchanged, was run once on the clean tested product commit
`e9a4c5ed3d34825aa0332cb3d64067b206e29070`. It used the repository-required invocation,
`powershell.exe -NoProfile -NonInteractive -Command "& .\scripts\Test-SourceOnly.ps1"`
(Windows PowerShell 5.1), from 2026-10-10 05:05:55Z to 10:47:57Z (5 h 42 min).
Result: **exit 0**, `Source-only suite: PASS=1 FAIL=0`.
Log: `artifacts\kbp042-source-gate-e9a4c5e.log` (SHA-256
`e233d870847a8a4f519b2549c8be78dd147e2a2f56dbb01bc6250510fc17d532`).

| Suite | Result |
| --- | --- |
| Source validation | PASS=43 FAIL=0 |
| Protocol (C#) tests | PASS=505 FAIL=0 |
| Runtime harness filesystem tests | PASS=38 FAIL=0 |
| Problem navigation evidence | PASS=23 FAIL=0 |
| Spellbook entry evidence | PASS=26 FAIL=0 |
| Package validation | PASS=4 FAIL=0 (package `3f3d55af...`) |
| Deployment WhatIf purity | PASS=5 FAIL=0 |
| Launcher -File WhatIf purity | PASS=13 FAIL=0 |
| Fixture inventory evidence | PASS=3 FAIL=0 |
| Install rollback | 7 cases, each PASS=1 FAIL=0 |
| Restore-InstallLocal | PASS=16 FAIL=0 |
| Guarded publisher gate | PASS=3 FAIL=0 |

The log's one "cannot be read ... being used by another process" message comes from the
runtime harness's deliberate game-held-entry case; that suite passed 38/38.

**Candidate.** `scripts\Build-Release.ps1` was run at `e9a4c5e` on a clean tree, with two
deterministic builds. It produced `artifacts\release\0.4.2\KingmakerBuffPlanner-0.4.2.zip`:

- package SHA-256 `3f3d55af66aa33be94e28a50fa7247a78c5fb006ac292237617fa847326b3463`;
- DLL SHA-256 `b3f5023d74f539d5aa4452b7b0f43f64e69930b5f561fc9e4aff0b72498e7e0a`;
- MVID `bcb3d03e-6bc9-48a3-8ff9-4b98dfb6f1c1`;
- `publicationStatus: local-only`.

These bytes are identical to the local-runtime package that `kbp042-removal-05` and
`kbp042-paper-02` ran, so the native evidence is evidence of exactly this candidate.
Later commits change records and documentation only. The package holds no
documentation, and the candidate is not rebuilt from them.

## NOT RUN

- Native Shadow Clone cast: no fixture has a Ninja.
- Native prepared-spell behaviour (un-preparing, spending the final prepared
  slot, pending rest): no mutable fixture has a prepared caster.
- Native exhaustion of a buff's own pool: the fixture's spellbook buffs are
  unlimited cantrips.
- The physical OS-input removal run: the game could not take the foreground
  from a console window. The no-input scenario proved the same
  reconciliation.
- Audible verification of the opening sound.
- 1280x720 visual check of the new paper: it needs the physical display
  scenario.
- Owner visual acceptance.

## Branch and draft PR

Branch `codex/kingmaker-buff-planner-041-owner-polish` is pushed only through the guarded
helper `codex-policy\Push-KingmakerBuffPlanner.ps1`: `-WhatIf` first, clean tree,
fast-forward only, remote verified. A draft PR is opened against `main`; its link is
recorded in the publication commit that follows this report. The PR must not be
merged without the owner's decision.

## Owner test list

1. **Install.** Back up `Mods\KingmakerBuffPlanner` (with `UserSettings`)
   outside `Mods`, then install the owner-test ZIP. Unity Mod Manager should
   show 0.4.2.
2. **Stale Mind Blank castings.** Load your campaign and open the planner. If
   Mind Blank is no longer in Felix's and Leinna's spellbooks, the footer
   reads "Removed 2 Mind Blank castings: no longer prepared in Felix's and
   Leinna's spellbooks. Undo available." Press Undo once to see them return,
   then remove them again: close and reopen the planner, or prepare another
   spell first.
3. **Prepared spells** (the cases the automation could not run in the game):
   - Prepare a planned spell twice, un-prepare one copy, and open the
     planner. Nothing should be removed.
   - Cast the last prepared copy so the slot is spent, and reopen. Nothing
     should be removed.
   - Rest, un-prepare the last copy, close the spellbook, and open the
     planner. Its castings should be removed, with the notice.
4. **Spontaneous spells.** Retrain a planned known spell away. Its castings
   should be removed with the notice. Spending all of that caster's slots
   must not remove anything.
5. **Shadow Clone.** On the Abilities tab, Shadow Clone should be listed. Add
   it on your ninja and run its routine. It should cast on the ninja and
   spend one ki.
6. **Look and sound.** Compare the planner with the inventory and character
   sheet, and listen for the paper sound when the planner opens.
