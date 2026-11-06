# Prepared-slot spend repair checkpoint (after 0.6.12)

Branch `codex/kingmaker-buff-planner-prepared-slot-repair`, from main
`de80e87` (published 0.6.12, product `cedb623`). Product commit
`ff2a237`; version unchanged at 0.6.12. This is not a release.

## Owner report

In a 0.6.12 log (`cedb623`), Long with Instant execution and Cast available
stopped at the second Heroism from the same prepared caster:
`FailedValidation:provider-ability-not-found`. That casting was never
submitted and nothing was spent for it. The first Heroism had cast and
confirmed (native available casts 2 -> 1). A second Long press cast it
(1 -> 0) and finished the routine. The pair happened twice. Both successful
casts resolved slot `level-3|type-0|index-1`, and each reservation was two
native units. The raw log and screenshots stay in the owner's local evidence
package. Nothing from them is published here.

## Mechanism (installed assembly, not inferred from the log)

Installed 2.1.7b `Assembly-CSharp.dll` SHA-256
`3b6450ffec440e296e586f71c711b195aed144b28d53e1cbb29406d18fef5afb`,
decompiled with ilspycmd 9.1:

- `AbilityData.Spend()` -> `SpendFromSpellbook()` -> `Spellbook.Spend(this)`
  -> `SpendInternal(blueprint, spell, doSpend: true)`. No slot is passed.
- `SpendInternal` (prepared book) loops `for (i = list.Count - 1; i >= 0; i--)`
  over that level's slot list and spends the **first available slot whose
  `Spell.Equals(spell)`**.
- `AbilityData.Equals` is value equality on blueprint, caster, fact,
  spellbook, `ConvertedFrom` and `MetamagicData`. Every copy of an identical
  preparation is equal.
- `SpellSlot.Spend()` makes the slot unavailable. When the slot
  `IsOpposition`, it makes every linked slot unavailable too.
  `Spellbook.Memorize` gives an opposition-school spell two linked slots, and
  `LinkedSlots[0]` is the main slot.
- `GetAvailableForCastSpellCount` counts the level's available slots with the
  same blueprint and halves the count for an opposition school. So two
  two-slot preparations read as 2, matching the log's 2 -> 1 -> 0 with
  `reservation-native-units=2`.

The party snapshot lists a preparation group's main slots in ascending
(level, type, index) order, and the ledger reserves the first one available.
So the first casting reserves the lowest preparation and the second casting
the next one. The resolver required the step's own reserved slot. The game's
spend took the **last** equal slot, which was the second casting's
reservation. The second casting's slot was gone even though an equivalent
preparation remained, and the resolver returned `null`. The game offers no
slot-bound spend, so the planner can't steer which copy it consumes.

Why this fits the log: the first cast resolved its own reserved slot. The
game spent the later equivalent preparation, so the first one stayed
available. The fresh second run reserved and resolved that same slot again.
The exact slot the owner's game consumed was not recorded at 0.6.12. The new
diagnostics below record it on the next native run.

Rejected theories: overspending (the count dropped by one per cast as
expected, and two units is the linked cost); index compaction (`SpellSlot`
indices are readonly); an effect-confirmation fault (the failure came before
submission).

## Repair

- `PreparedSlotChoice` is a pure production choice (Execution layer, no
  Unity), now used by the shared resolver for both Instant and Animated
  execution. A step's own available reserved slot still wins. If that slot
  is spent, the step may resolve an **equivalent** slot that a casting of
  the **same run** already confirmed had reserved and that the game left
  available. Equivalent means the same native `AbilityData` (so the same
  spell, variant, metamagic and heighten), the same level, slot type,
  opposition flag and linked count, with every linked slot available and
  within that released reservation. It never takes a later casting's slot,
  an unreserved slot, another preparation, another book or another caster.
  The authorized count per preparation is preserved. Nothing is refilled,
  re-enabled or submitted twice.
- `PreparedRunContext` is how both run loops (`ExplicitCastingRunCoordinator`
  and `HaltingPlanRunner`) pass in the tokens of the run's earlier
  **confirmed** castings from the same pool, as
  `ResourceReservation.EarlierConfirmedTokenIds`. This is run context only.
  Plans, projections, their identities and the recorded `ResourceTokenIds`
  stay the authorized tokens.
- Counting argument: each confirmed casting spent exactly one preparation
  (otherwise the count judgement would have failed it and the run would have
  halted). So when casting *k* starts, at least one of the slots reserved by
  castings 1..*k* is still available, and casting *k* never needs a later
  casting's slot.
- Refusals are now precise:
  `prepared-source-exhausted:reserved=...;released=n;released-equivalent=n;released-equivalent-available=0`
  (the preparation is present but spent) versus
  `prepared-source-missing:reserved=...;book-slot=absent|other-preparation`.
  Other unresolved sources keep `provider-ability-not-found`. Execution still
  halts on every failure.
- Native spend evidence: the Instant and Animated details now add
  `reservation-tokens`, `earlier-confirmed-tokens`, `resolution`
  (`reserved-memorized-slot:` or `reconciled-memorized-slot:X;reserved=Y;...`)
  and `prepared-consumed:<slots that went available -> unavailable>`. These
  are read-only diagnostics.

## Tests (offline; simulated native spellbook)

`tests/KingmakerBuffPlanner.Tests/PreparedSlotSpendTests.cs`. The simulated
spellbook spends in `SpendInternal` order and never reads
`step.Reservation`. The session, compiler, gate, converter, coordinator,
both executors, the report and `PreparedSlotChoice` are real. This
simulation is **not** proof of the game's selection contract. That proof is
the decompiled method above, and native confirmation is still pending.

| Test | What it proves |
| --- | --- |
| `prepared-spend-repeat-single-{instant,animated}` | Two single-slot Heroisms on different recipients plus a distinct successor from the same book confirm in one run. Counts go 2>1>0, the first spend consumed the second casting's reservation, the second casting resolved the left slot, each submission consumed one preparation, there were no duplicates, and the re-run spends nothing. |
| `prepared-spend-repeat-linked-{instant,animated}` | The same with two-slot (opposition) preparations, matching the owner's two-unit shape. |
| `prepared-spend-shortage-{instant,animated}` | One preparation for two castings: Cast available casts one plus the successor and omits one. Require full refuses before any cast. |
| `prepared-spend-choice-contract` | Own slot first; reconciliation only from released equivalent slots; no borrowing from future or unreserved slots; metamagic/heighten/type/linked-cost and variant mismatch refused; linked slots must be available; missing vs exhausted; pool-scoped run context; spend-evidence diff. |

Failing before: a strict extraction of the `cedb623` resolver semantics
(uncommitted working tree on `de80e87`, DLL `b41dfb44...9ba7`) ran
`prepared-spend-` at **2/5**. All four repeat cases failed exactly like the
owner log: the first casting was confirmed, the second failed with
`FailedValidation:provider-ability-not-found`, and the successor was
`NotProcessed`. The choice contract also failed. Both shortage cases passed.
Retained locally as
`artifacts/prepared-slot-repair-evidence/before-fix-strict-chooser.log`
(`b21938db...36cb`).

Passing after, at `ff2a237` (Release DLL
`83851fec22da0cb14d2a0f59c5372c4bf54cd5a34f37993ee4467a9155e1d056`):

```powershell
.\scripts\Test-Development.ps1 -Only 'prepared-,playtest-heroism-,infectious-charms-,finite-,explicit-,classic-,cast-available'
#   source 47/0, build 1/0, focused 65/0, 13.8 s
.\scripts\Test-Development.ps1 -Only '<every registered test prefix>'
#   source 47/0, build 1/0, complete offline program 713/0 (706 at 0.6.12 + 7), 35.4 s
```

The complete offline program was selected because both shared run loops
changed. Its first run failed one source-text assertion
(`cantrips-cast-at-will-through-the-class-ability`), which pinned the old
single-loop predicate `(forObservation || s.Available)`. The assertion now
pins the split branches: observation reads the reserved slot whatever its
availability, and execution goes through `PreparedSlotChoice`. No assertion
was removed.

## Not done / uncertainty

- **Native acceptance NOT RUN.** The decisive check is one uninterrupted Long
  through both Heroisms and the successor on this candidate. Two Long presses
  don't count. The automation fixture party has no mutable prepared caster
  with a repeated preparation, so no guarded native scenario exists for it.
  This needs the owner's playtest or an approved new disposable fixture.
- Animated is covered offline only. The owner's reproduction was Instant.
- The guarded qualification driver's per-token reading still expects the
  exact reserved token to be consumed. A future native qualification recipe
  with repeated preparations would need that judgement made
  equivalence-aware. Ordinary play does not use it.
- Cross-type native spends (a Favorite slot consumed for a Common
  reservation) are not reconciled. Such a step halts with
  `prepared-source-exhausted`.
- No packaging, live purity, installation or save change was performed.

## Next action

Owner review of the draft PR. Then the owner (or a guarded run on an
approved disposable fixture) presses Long once with the original 14-casting
plan. Expected: both Heroisms and every successor confirmed in that one run,
the second Heroism's detail showing `reconciled-memorized-slot:...`, and
`prepared-consumed` naming the slots the game actually spent.
