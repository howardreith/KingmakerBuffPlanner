# Durable item-source selection

2026-11-08. Development fix prepared for draft PR review. Version remains
0.6.13; no release, staging, installation change or game/save mutation.

| Identity | Value |
| --- | --- |
| Branch | `codex/kingmaker-buff-planner-durable-item-source` |
| Base | `5122e3aec48cc16f77a6e5418c0fd722bd99dd04` (main, 0.6.13) |
| Tested product/test commit | `472e2f5502ec798f3e86ef9a45aade2e21b77a0d` |
| Built DLL SHA-256 | `f7ca13e5b1f6727fa5744e173c5f17f2b88ffa1ad9ed662e14c686126f5b91e4` |
| Game `Assembly-CSharp.dll` SHA-256 | `3b6450ffec440e296e586f71c711b195aed144b28d53e1cbb29406d18fef5afb` |
| Current tested commit after review | `5ebb83b186e9d9b922710dd63d9b29f8ca17729f` ([correction](#pr-23-review-correction---2026-11-08)) |

## Root cause

Saved item castings stored `session-item-<guid>`, a token assigned per
in-memory `ItemEntity`. A load reconstructs the object, so the token changes and
the saved key can never match: the compiler emitted `caster-not-capable` while
the same item was charged and discoverable.

## Supported contract inspected

In 2.1.7b `ItemEntity` is `[JsonObject(OptIn)]` with no persistent ID. Its
serialized members are blueprint, count, inventory index, enchantments, charges,
identify state, vendor/origin data and wielder/slot references. `Ability` is
re-created by `OnDidEquipped` and removed by `OnWillUnequip`, so it cannot carry
identity either. A per-item identity would need a save-contract hook, a marker
effect or a repurposed native field; all were rejected.

## Selection contract

`AbilityKey.SpecialSourceId` for an Item source holds an explicit rule, parsed by
`ItemSourceSelector`:

| Saved value | Meaning |
| --- | --- |
| `equipped-item:<item blueprint>` | This caster's equipped item of this type supplying this ability |
| `equipped-item:<item blueprint>@<slot>` | The same, in that equipment slot; written only by an explicit choice between identical copies |

These are rules, not physical identities: another copy of the type satisfies
them. `ProviderKey.SourceInstanceId` and the `item|<token>` charge pool remain
per live object and are never saved, so each run binds to one specific item.
`ValidateBinding` requires both that exact live token and that the rule still
describes the item.

| Situation | Result |
| --- | --- |
| Save/load, area transition, rescan, reopen | Resolves to the live item |
| Spending or restoring charges | Selection unchanged; exhaustion reads as exhaustion |
| Item moved to another ring slot (one copy) | Resolves |
| Replaced by another copy of the type | Resolves; uses that copy's charges |
| Unequipped, removed, or transferred | Not ready, `item-source-missing`; never follows the item to another caster |
| Different item type granting the ability | Does not satisfy the rule |
| Identical copies, plain rule | Not ready, `item-source-ambiguous`; explicit copy choice, durable afterwards |
| Slot rule, slot holds the item | Resolves, including after the other copy is gone |
| Slot rule, slot no longer holds it | Not ready, `item-source-slot-empty`; another slot's copy is not substituted |
| Identical copies whose slot cannot be read | Not pinnable; stays ambiguous |
| Copies on different casters | Separate providers and budgets |
| Sibling abilities of one item | One shared pool |
| Empty or unreadable saved selector | Not ready, `item-source-invalid`; never throws or guesses |

## Existing plans

`session-item-` tokens and legacy free-`Fact` item records carry no link to a
physical item, so nothing migrates automatically. They show "Source needed" with
the repair. One explicit reassignment (select the card, Change caster/source,
click the current Item charge row) replaces only the source and caster, keeps
every other setting and Undo, and survives later loads. An ordinary source-row
click still configures a new draft.

Identical-copy castings saved before this change need that same one-time choice.
The published rule for copies changes when a caster gains or loses a duplicate;
the slot rule is a position, so swapping identical copies swaps which one is used.

## Validation

Windows PowerShell 5.1, official MSBuild, .NET Framework 4.7, C# 7.3.

Baseline, unmodified production at `5122e3a`, new regression only: **0 PASS / 1
FAIL**, `caster-not-capable:unit-cleric`
([log](evidence/durable-item-source-2026-11-08/baseline-regression.log)). The
test round-trips a real `ItemEntityUsable` through the game's
`OptInContractResolver` and load constructor; only `BlueprintConverter` is
redirected, because `ResourcesLibrary` needs a running game.

At `472e2f5`:

```powershell
./scripts/Test-Development.ps1 -Only '<every registered first-word prefix>'
```

Source validation **47 PASS / 0 FAIL**; build **1 PASS / 0 FAIL**; complete
offline C# program **727 PASS / 0 FAIL** (713 before this change, 14 added).
Focused subset (`item-source-`, `duplicate-items-`, `stale-item-casting-`,
`fact-resource-`, `playtest-area`, `direct-manip`, `casting-graph`) **39 PASS / 0
FAIL**. `KingmakerPartySnapshotBuilder` runs only in the game: its rule choice is
tested through `ItemSourceSelector.ForDiscoveredItem`, the builder only compiled.

Revised deliberately: `TestNativeFactSavedSelection`, renamed
`fact-resource-saved-selection-follows-equipped-rule-and-survives-rescan`, now
expects another equipped copy of the type to resolve and a different type not to.
The duplicate and stale fixtures publish rules rather than tokens.

## Native status: NOT RUN

Offline tests are not gameplay evidence. Not attempted: `Kingmaker.exe` was
running in the owner's console session, and the recorded Automation fixture has
no charged ability-granting item. No launcher, staging, save or `Mods` change was
made. Bag of Tricks Instant Rest was not exercised, and nothing here establishes
that it caused the report.

Remaining acceptance, on a disposable save with a charged item:

1. Reconcile the signet casting once if it shows "Source needed".
2. Save, load, reopen: Ready, connector on the Item charge row.
3. Transition areas, revisit, run: expected recipients, exactly one charge spent.
4. Ordinary rest: charges restored, still connected.
5. Bag of Tricks Instant Rest (not Buff Planner Instant mode): the same.
6. Unequip: "Source needed". Re-equip: Ready.
7. With two identical copies on one caster, choose one, reload, confirm only that
   copy's charges are spent.

## PR #23 review correction - 2026-11-08

Review of `55e3603` found that the transition from two identical copies to one
lost the saved slot rule after the compiler resolved it. A saved Ring 2 selection
stays `equipped-item:<type>@Ring2`, while discovery publishes the sole remaining
copy under the plain rule.

| Finding | Before | Now |
| --- | --- | --- |
| Clicking an existing assignment | Raw key comparison chose Add and persisted a second executable casting | Recipient toggle, add guard and retarget guard use `ItemSourceResolution.Compare`, per caster; the click removes the casting |
| Execution binding | Step carried only the plain live key, so the item could leave Ring 2 and still validate | `RequiredItemSelector` travels from `ResolvedCasting` through `CastStep`; `ValidateBinding` checks the live provider and the authored rule, refusing with `fact-source-item-left-saved-slot` |
| Inspector | "whichever copy that is" | States the saved rule: "in Sayan's Ring 2 slot" |

A saved rule for the same item and ability that names no single live source
(`item-source-slot-empty`, `item-source-ambiguous`) could become the selected
source later, so adding beside it could eventually cast twice. Clicking its
recipient now focuses that casting instead (`InspectItemSource`) without changing
the plan. Two resolved copies remain independent sources on one recipient. Legacy
tokens and free-`Fact` records can never resolve and stay separate. The projection
carries `requiredItemSelector` only for slot-ruled steps, so every other projection
identity is unchanged. Choosing the plain row through Change caster/source is an
explicit change to the plain rule.

Baseline, unmodified production at `55e3603` with the three review regressions
added: **14 PASS / 3 FAIL**, one per finding
([log](evidence/durable-item-source-2026-11-08/review-baseline-regression.log)).

At `5ebb83b`, same entry point: source validation **47 PASS / 0 FAIL**; build **1
PASS / 0 FAIL**; complete offline C# program **731 PASS / 0 FAIL** (4 added);
focused subset **44 PASS / 0 FAIL**. DLL SHA-256
`9cb90a51d083700d398004236613839e7277292416cf041c519b21cfeac58cf2`.

`cantrips-cast-at-will-through-the-class-ability` pins the adapter's exact call
text, so its two call-site strings were updated for the added argument; its other
assertions are unchanged. The compiled-fallback regression calls `ValidateBinding`
with the step's own values. `KingmakerAnimatedCastAdapter`, which passes them in the
game, is compile-verified only.

Native remains **NOT RUN**; `Kingmaker.exe` was again running in an owner session.
Add to the steps above: with two copies, select Ring 2, remove the Ring 1 copy,
reopen, click the recipient (the casting toggles off); then repeat, start an animated
routine, move the signet to Ring 1 before its turn, and confirm the casting is
refused without spending a charge.
