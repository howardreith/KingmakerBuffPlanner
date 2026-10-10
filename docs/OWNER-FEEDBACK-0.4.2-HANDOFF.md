# 0.4.2 owner-feedback follow-up: evidence handoff

This is the follow-up to the owner's 0.4.1 feedback (prompt
`Claude_KBP_041_Followup_Prompt.md`, screenshots `references/01..05`). It is
a local owner-test candidate. Nothing was merged, tagged, published or
permanently installed, and no main, owner or protected save was touched.

| Item | Value |
| --- | --- |
| Base | v0.4.1, main `ab62462fa62c2e7a13a4c33a7a2947cd11b77b6d` (no later commits on `main` to preserve) |
| Branch | `codex/kingmaker-buff-planner-041-owner-polish` |
| Version | 0.4.2 (`Version.props`, `Info.json`, assembly 0.4.2.0) |
| Candidate commit, package SHA-256, DLL SHA-256, MVID | recorded in "Candidate identity" once the final gate has run |

## Commits

| Commit | Series | What |
| --- | --- | --- |
| `88f0b87` | - | Version 0.4.2 |
| `4aef348` | A | A class ability is its own buff entry (Ninja Shadow Clone) |
| `03a6098` | B | Retire castings of a spell removed from its spellbook |
| `15f2182` | C | Native card paper on the table, and the native paper-opening cue |
| `11cec99`, `73e75de` | C (diagnostic) | Native paper donor geometry, previews and UI sound table (runtime evidence only) |
| `783cfc4` | C (evidence) | Native reference screens and open-cue counts in one session |
| `0b4b5a0` | B (evidence) | Physical `removal` scenario: native `RemoveSpell` and spend, then the cold HUD press |
| `eb80b14` | B (fix) | Host request validation accepts `removal`; launcher/host parity regression |

## A. Shadow Clone

**Native ability.** Call of the Wild 1.14.4c-2.1, `NinjaShadowCloneMirrorImage`
`335a48ec4134454fb491b3d64f468ada`, "Shadow Clone". It is a Ninja trick of the
Rogue class's Ninja archetype (`20785e18...`), Supernatural, Standard action,
Personal (self only). It has `AbilityResourceLogic` on `NinjaKiResource`
`74b56c9eba8f43ac9166b5fa93e50197`, cost 1. Its effect is one
`ContextActionApplyBuff` of `MirrorImageBuff` `98dc7e7cc6ef59f4abe20c65708ac623`
on the caster. That is exactly the effect tree of the wizard spell Mirror
Image `3e4ab69ada402d145a5e0ad3ad4b8564` (0.4.1 catalogue export
`kbp041-rel-catalog-full`; ownership `call-of-the-wild`).

**Root cause.** A catalogue source was identified by its effect fingerprint
(`effect|sha256`). The workspace served an id only through the reference of
its representative expression instance, and Mirror Image sorts first. Shadow
Clone therefore shared Mirror Image's id, served nothing, and disappeared
from the casting-first buff list. Discovery and the classifier were correct.

**Fix.** `CatalogSourceIdentity.For` returns `ability|<base guid>` for every
non-spellbook source. Variants keep `variant|base|child`, and spells keep the
effect aggregate. `RoutinePlanService` matches through the same function.
`CastingSourceIdentityMigration` rewrites a stored class-ability casting's
stale id once. `CastingPlanRepository.ArchivePrimaryOnce` keeps the
pre-0.4.2 file (`.pre-0.4.2-source-identity`). There is no name list, forced
provider, buff injection, blueprint change or compile-time dependency.

**Evidence.**
- Regressions, run against the installed ability's facts:
  - `shadow-clone-has-its-own-catalogue-entry`;
  - `shadow-clone-authors-on-its-owner-with-one-ki`;
  - `shadow-clone-exhausted-ki-keeps-entry-and-casting`;
  - `shadow-clone-absent-without-owner-or-optional-mod`;
  - `shadow-clone-saved-under-shared-identity-migrates-once`;
  - `class-ability-identity-keeps-spells-aggregated`;
  - `classic-mirror-image-never-plans-shadow-clone`.
- With the new rule disabled, 6 of the 7 fail with "Shadow Clone is not in
  the buff list: Mirror Image".
- **Native cast: NOT RUN.** No approved fixture has a Ninja. The automation
  party is fighter, bard and sorcerer; the advanced copy is cleric, alchemist,
  fighter and arcanist. The owner's character and main save were not edited.

**Noted, not changed:** spells are still aggregated by effect by design. Two
different spell blueprints with one effect still share a card, and their
provider rows stay distinct.

## B. Spellbook removal

**Root cause.** Nothing reconciled saved castings with the native books.
Castings of a spell that was no longer prepared or known stayed in the plan as
blocked castings (the owner's stale Mind Blank records).

**Native contract (2.1.7b, Assembly-CSharp MVID
`07fa1e4d-8618-41b3-9b8d-faa17d3b26f7`; checked from the installed IL by
`spellbook-removal-native-contract-2-1-7b`):**
- `Memorize` fills a slot's `Spell` and sets `Available=false` until rest.
- `Spend` clears only `Available`.
- `ForgetMemorized` calls `SpellSlot.Clear` and then `ClearInternal`, which
  sets `Spell=null`.
- `GetAllMemorizedSpells` filters on `Spell` only.
- `RemoveSpell` removes the spell from the known, custom and special lists
  and clears its memorized slots.
- `AbilityData.SpendFromSpellbook` calls `Spellbook.Spend`.

So "still a member" (prepared: in a memorized slot, spent or not; spontaneous:
known, special or custom) separates a real removal from a resource spend.

**Decision table** (`SpellbookRemovalReconciliation.Decide`, pure, unit-tested
by `spellbook-removal-decision-table`):

| Observation for the casting's exact caster and book | Result |
| --- | --- |
| Not a spellbook casting (ability, item, scroll) | Not considered |
| No caster or book on the casting | Kept: `no-caster-or-book` |
| No membership read (adapter failed or not supplied) | Kept: `membership-unknown` |
| Party read unstable: loading, no player, or a service window (the spellbook) open | Kept: `membership-unstable:<reason>` |
| Caster or book not in the read (caster out of the party) | Kept: `book-not-observed` |
| Book `CasterUnavailable` (dead or unconscious), `RoleNotProven`, or `Unreadable` | Kept: `book-<status>:<reason>` |
| Book complete and still holds the exact member (base or variant parent, plus metamagic mask), even with every slot spent or zero uses | Kept: `still-held-by-the-book` |
| Book complete, member gone, and the casting was restored by Undo under the same observation (same book digest) | Kept: `undo-restored-same-observation` |
| Book complete, member gone, otherwise | **Removed**: "no longer prepared" (prepared book) or "no longer known" (spontaneous) |

Session guards run before the table (`ReconcileSpellbookRemovals`). It is
skipped during a run or submission, in combat, and while the stored plan or a
legacy import is unresolved. It runs only at idle boundaries: when the planner
opens, and in `Apply` before the gate on every route, the HUD included.
Removals are one `RemoveCastings` command: one history entry, one autosave,
one Undo. The stored file is archived once beforehand
(`.pre-spellbook-removal`). A removal that cannot be saved gets one more flush
and otherwise refuses the run (`persistence-failed:`). A restart observes
again; the suppression after an Undo lasts only for the session; the schema
is unchanged.

**Regressions** (real authoring, compiler, gate, session, repository and temp
files; only the native observations are fixtures):
- `spellbook-removal-compound-authoring-edit`;
- `-cold-load-hud-first` (the owner's two stale Mind Blank castings for
  Felix and Leinna, removed on the first HUD press before the gate);
- `-final-copy-unprepare`;
- `-one-of-two-copies-keeps`;
- `-spent-slot-keeps-across-reload`;
- `-uncertainty-never-deletes`;
- `-known-spell-forgotten`;
- `-variant-and-metamagic-identity`;
- `-undo-idempotence-and-restart`;
- `-save-failure-blocks-the-run`;
- `-leaves-no-stale-problem-focus`;
- `-physical-judgement`;
- `physical-expectation-launcher-host-parity`.

Mutations of the book check, the Undo suppression and the `Apply` hook are
each caught.

**Native evidence.** The physical `removal` scenario on the automation fixture
works on the in-memory books only. It authors two castings of spontaneous
spell X and one of spell Y, removes X with the game's own
`Spellbook.RemoveSpell`, and spends every slot of Y's level with the game's
own `AbilityData.SpendFromSpellbook`. It then presses the moon cold. The press
must remove exactly X's two castings, keep Y's casting and every seed, archive
and save, still be refused by the session lock, and show the notice in the
opened planner.
- `kbp042-removal-01` (commit `0b4b5a0`): **did not start.** The host's
  request validation did not list `removal`, so it rejected the request at
  boot ("Runtime request rejected: invalid-request:physical-expectation") and
  the launcher timed out at the menu. Fixed in `eb80b14` with a parity
  regression. `Mods` was restored (verified), protected saves were clean, and
  the working save was never loaded.
- Corrected run: recorded below when it has run.
- **Prepared-slot spend and forget in the game: NOT RUN.** No mutable fixture
  has a prepared caster. This is covered by the IL contract and the unit
  regressions.

## C. Native paper and sound

Details: [VISUAL-THEME.md](VISUAL-THEME.md), section "0.4.2".

**Donors** (verified at runtime in `kbp042-donors-02` and `kbp042-paper-01`;
read-only, never shipped):
- The page is `Card_Big` (2048x1566.8, PPU 84.27984) at
  `ServiceWindow/Journal/Cart`. It is the same sprite as the character
  sheet's, journal's and settings' page.
- The table is `ServiceWindow_TableBackGruond_3840_2022` (2048x1024, PPU 100)
  at `ServiceWindow/Background`.
- The 0.4.1 paper `dialogue_backsheet` remains the second tier.
- The sound is `UISoundType.CharacterScreenOpen`, Wwise event `JournalOpen`,
  which the character screen, spellbook and journal post on open.

**Before and after** (lab files, not in the repository: they show game art):
- Before (0.4.1, the owner's screenshots):
  `C:\Dev\KingmakerBuffPlannerLab\references\04-planner-041-normal.png` and
  `05-planner-041-unavailable.png`.
- Native targets (owner): `references\01-native-character.png`,
  `02-native-inventory.png` and `03-native-map.png`.
- After (0.4.2, same session as its native references, 1920x1200):
  - `C:\Dev\KingmakerBuffPlannerLab\runtime-evidence\kbp042-paper-01\spell-scroll.png`
    (the planner with the spell description open);
  - `native-inventory.png`, `native-character.png` and `native-map.png`;
  - `paper-sound-evidence.json`.

| Mean luma of the writing area | Top | Lower / right |
| --- | --- | --- |
| 0.4.1 planner | about 224 | about 224 |
| 0.4.2 planner | about 195 | about 171-176 |
| Native inventory / character pages | about 205 | about 159-165 |

**Sound: event call versus listening.** The automation proves the call:
`[KBP-WORKSPACE-SOUND] event=CharacterScreenOpen(Wwise JournalOpen)` with one
cue per successful open. In `kbp042-paper-01` the open/cue counts were 1/1,
1/1 after two refreshes, 1/1 after the spell description, then 2/2 and 3/3 on
two reopens. The in-game assertion `paper042-one-open-cue-per-open` passed.
It does **not** prove that anything reached the speakers: **listening is NOT
RUN** and is the owner's check.

**Regressions:**
- `paper-042-native-sheet-and-table-contracts`;
- `paper-042-sheet-and-table-resolve-and-fall-back`;
- `paper-042-sheet-geometry-keeps-native-edges`;
- `paper-042-sheet-is-drawn-untinted-on-the-table`;
- `sound-042-one-native-paper-cue-per-open`;
- `wp7-inks-stay-legible-on-the-paper` (explicit thresholds, darker sheet).

## Runtime runs (disposable automation fixture only)

| Run | Commit | Scenario | Result | Mods restored | Protected saves |
| --- | --- | --- | --- | --- | --- |
| `kbp042-donors-01` | `ac343d8` (pre-cherry-pick) | live-advanced-inspect (Automation) | PASS, complete | verified | clean |
| `kbp042-donors-02` | `13923c7` (pre-cherry-pick) | live-advanced-inspect (Automation) | PASS, complete | verified | clean |
| `kbp042-paper-01` | `783cfc4` | live-advanced-inspect (Automation) | PASS, complete | verified | clean |
| `kbp042-removal-01` | `0b4b5a0` | live-workspace-physical `removal` | FAIL, incomplete (host rejected the request, see B) | verified | clean |

After every run the lab install was back to `KingmakerBuffPlanner` 0.1.1-rc3,
DLL SHA-256 `78407dd4c7240ce25c7654715854e83061da744a18d8eb4a2198fdffec5e3080`.

## Gate and candidate identity

Not yet run. The complete, unchanged `scripts\Test-SourceOnly.ps1` runs once
on the final product commit. Interim checks at `eb80b14`:
- the unit suite passed, PASS=500 FAIL=0;
- `Validate-Source` passed, PASS=43 FAIL=0;
- the launcher meta-test passed, PASS=13 FAIL=0 on a scratch copy with the
  purity windows stubbed. The real windows run in the complete gate.

## NOT RUN

- Native Shadow Clone cast: no fixture has a Ninja.
- Native prepared-slot spend and forget: no mutable fixture has a prepared
  caster.
- Listening to the opening sound: only the owner can hear it.
- Owner visual acceptance of the aged page and table.

## Owner test list

1. Back up `Mods\KingmakerBuffPlanner` (with `UserSettings`). Install the
   0.4.2 ZIP. Unity Mod Manager should show 0.4.2.
2. Load your campaign and open the planner. If Mind Blank is no longer in
   Felix's and Leinna's spellbooks, the footer reads "Removed 2 Mind Blank
   castings: no longer prepared in Felix's and Leinna's spellbooks. Undo
   available." Press Undo once to check that they return, then remove them
   again.
3. Cast a buff normally so a slot is spent, then reopen the planner. Nothing
   should be removed.
4. Un-prepare (or retrain away) a planned spell, close the spellbook, and open
   the planner. Its castings should be removed with the notice.
5. Abilities tab: Shadow Clone should be listed. Add it on your ninja and run
   its routine. It should cast on the ninja and spend one ki.
6. Compare the planner with the inventory and character sheet, and listen for
   the paper sound when the planner opens.
