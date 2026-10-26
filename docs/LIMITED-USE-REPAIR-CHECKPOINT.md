# Limited-use source and casting report repair

October 26, 2026. Reviewed **development checkpoint**, not native or release
qualification. Branch: `codex/kingmaker-buff-planner/resource-classification-repair`.
Base: `e7b544f6b86bdd552737a99539dd34eb228c89b0` (`origin/main`, v0.6.6).
Tested product/test commit: `eafb950a886c7ede741f0541d37b2140d9d4b500`.
Version remains **0.6.6 / 0.6.6.0**, unreleased changes on this branch.
Documentation commits do not transfer the build stamp to a later HEAD.

The original checkout was clean at `b8237a424df39f31a0f7f1f54c548f3814607945`
on the controller card branch. Work is isolated in
`private/worktrees/KBP-RESOURCE-REPAIR`; no existing worktree was reset or cleaned.
No existing local repair branch/commit was found. No merge, push, PR, tag,
package, installation, game launch, release, or tester message was performed.

## Diagnosis and verified contract

The v0.6.6 discovery predicate prices a non-spellbook fact as unlimited when
both its concrete/source ordinary `Resource` are absent. That contradicts a
verified native path: item charges. The shared result host also used the free
forecast to hide reported spending. Both defects are repaired. The existing
unexpected-spending halt and failure precedence over effect confirmation remain.

No reporter log, plan, source item, installed mod version or compatibility
profile was supplied/found. A read-only search of the lab's runtime evidence
found no `unexpected-resource-spent-on-unlimited-source` record identifying
this incident. The screenshot's truncated `s...` was not reconstructed.
An item-granted Aspect of the Falcon remains a hypothesis; stale binding is
also possible. No spell, character, item name or GUID is a production exception.

Installed Windows game assembly: SHA-256
`3b6450ffec440e296e586f71c711b195aed144b28d53e1cbb29406d18fef5afb`,
MVID `07fa1e4d-8618-41b3-9b8d-faa17d3b26f7` (2.1.7b lab contract).
The checked-in [raw IL and member evidence](evidence/resource-repair-2026-10-26/hashes.json)
records these contracts:

| Native contract | Observation |
|---|---|
| `AbilityData.Resource` | Ordinary blueprint resource logic only; does not describe item charges. |
| `GetAvailableForCastCount()` | Sticky-held count; converted-source delegation; spellbook; spending ability-resource quantity divided by native cost; then spending source item's `Charges`; otherwise exactly `-1`. Unread is separate. |
| `AbilityData.Spend()` | Material, source-item charges, spellbook, concrete blueprint resource logic, in that order. Unchanged by repair. |
| `SourceItem` | From the ability's fact. Variant/copy construction retains fact/conversion context. |
| `ItemEntity.SpendCharges(caster)` | Ordinary nonstacked path subtracts one charge. Native charge-saving feature may conserve it; stacks can reset charges while decrementing count. These are not cast-count units. |
| `ItemEntity.ReapplyAbilities()` | Adds the equipment's granted ability to its wielder through `UnitHelper.AddFact`; existing fact-ability discovery/resolution is the relevant route. |
| `ItemEntity.OnDidEquipped()` | Adds the identified equipment ability, assigns that exact item to `Fact.SourceItem`, then stores the ability on the item. This establishes the source/charge-owner relationship. |
| `ItemEntity`, `Fact`, `Ability` members | No persistent item-instance ID. Inventory position and blueprint are not unique item identity. |

The equivalent reproduction uses detached installed-assembly objects with an
ordinary empty resource-component array, a source fact and one charged equipment
object. `Resource == null`, native availability starts at 3, and the real native
`Spend()` produces 3 -> 2 -> 1 -> 0 on that same item. This reproduces the
baseline classification contradiction, **not** the reporter's exact setup or a
baseline in-game failure. Fixture construction uses bounded reflection and
`FormatterServices`; no Unity native constructor, Rulebook, game rendering or
real buff application is exercised offline.

## Resulting behavior and support limits

Discovery and runtime binding share `KingmakerFactResources`. Supported facts
use verified native resource quantities/costs or a verified `-1` unlimited
count. Single-item charge sources use cost 1, native charges/capacity and one
shared `ItemCharges` pool for every ability from the same item. Unknown finite
semantics, unread state, unsupported stacks, combined charges plus ability
resources, and inconsistent variant contracts get no free provider.

Item identity is deliberately **session bound**: a weak object-identity table
assigns one token to the exact live item. The token is part of both the existing
ability `SpecialSourceId` and provider instance identity. It survives rescans;
the existing saved-plan format preserves it. A replacement object or reload
cannot silently acquire that selection. The casting and targets remain saved
but unresolved until the player explicitly reselects the item. Old free-Fact
selections are retained and similarly require explicit reselection. Item source
labels disclose this limitation. This is finite-run support within the live
session plus safe containment across reload, **not persistent item tracking**.

Both submission routes validate the final resolved object and native count
after enhancement preparation. Instant checks again before its rule; Animated
checks again before queuing. Binding compares source kind, exact item token,
pool, unlimited flag and native cost. Refusals do not replay. Existing cleanup
ownership and native rule/spend order remain intact. A full-plan known shortage
is refused by the compiler/gate/dispatch boundary with zero submissions.

Reports separately retain planned native cost/unlimited, native Spend invocation,
observed use, effect observation and final validated state. Animated does not
invent evidence that `Spend()` was invoked. Observed spending survives failure
and free forecasts. Only validated casts with entirely free forecasts and no
contradicting observation appear in `plus N free`. The summary says **Casts with
observed resource use**, counting attempts with observed use, not charge/slot
units. Failed casts may disclose an observed effect without increasing the
confirmed total. Material/enhancement planning and checks remain independent.

## Focused verification

Command, run in the isolated worktree on the exact tested commit:

```powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass -File scripts/Test-Development.ps1 -Only 'fact-resource,available-count-judgement,cantrips-cast,at-will-cantrip,fact-source-choice,cantrip-route,execution-host,run-presentation,zero-cost-native-source,classic-runs-halt,execution-preflight,qualification-driver-refusals-and-deadline'
git diff --check
```

Final selector: **17 PASS / 0 FAIL**, 5.8 seconds; production build **1/0**;
source validator **47/0**. No zero-test selection. [Captured final output](evidence/resource-repair-2026-10-26/committed-focused.log).
Documentation review checked 16 relevant local links (16/0), all seven offline
matrix entries, artifact/hash agreement and unchanged product/test trees against
the tested commit. `git diff --check` passed. These are document checks, not
additional behavior or native acceptance tests. The later inspector-only addition
reads `OnDidEquipped`; its direct native inspection passed without rebuilding.
DLL SHA-256: `7e8a75c8cc45b5e77c0c02d419d67d1bce8c4ec8a1d7da58b9bdce66de0f0370`.
DLL MVID: `493b01aa-5955-429f-9b55-6735955742f9`;
[artifact identity receipt](evidence/resource-repair-2026-10-26/artifact-identity.json).
Runner SHA-256: `dd89d2f410bab34a4b5d90a8eb08046b5938f5a4aaf53f373c54addeaf538fd4`.
Native fixture source SHA-256: `2d7cb3564a1f0b6355f168189c80119bc2c2c30c93217cbdb196c36982292a01`.
These artifacts remain under `artifacts/build/Release` and `artifacts/tests`.

Selected test names:

```text
at-will-cantrip-choice-refuses-what-is-not-the-authored-cantrip
available-count-judgement-never-reads-unread-as-unlimited
cantrip-route-follows-the-reservation-and-ambiguity-stays-unresolved
cantrips-cast-at-will-through-the-class-ability
classic-runs-halt-after-a-failure-and-keep-cleanup
execution-host-cancel-deadline-shutdown
execution-host-runs-reports-and-halts
execution-preflight-runs-under-the-native-activation-lease
fact-resource-final-boundary-refuses-stale-counts
fact-resource-five-cast-finite-unlimited-and-full-plan-shortage
fact-resource-native-item-discovery-reservation-binding
fact-resource-saved-selection-refuses-replacement-and-survives-rescan
fact-resource-unexpected-spend-report-retains-effect-and-failure
fact-source-choice-keeps-the-provider-kind-and-reserved-pool
qualification-driver-refusals-and-deadline
run-presentation-separates-effects-and-spending
zero-cost-native-source-integration
```

The new tests exercise the production discovery resource reader, native getters
and Spend, ledger/compiler/gate/dispatch, binding, executors, persistence and
shared report. Only rule/command submission and effect application are simulated.
Five-cast routines use three fixture recipients, not the reporter's self-only
spell. Full structural graph discovery and actual adapter submission require the
game and remain outside this detached fixture. The existing spend-on-free halt
regression in `qualification-driver-refusals-and-deadline` is retained.

| Requirement | Offline result | Evidence and limits |
|---|---|---|
| R1 | PASS | Real native finite/no-Resource contract creates finite pool; unsupported stack/unread state refused. Discovery reader is the production seam. |
| R2 | PASS | Exact `-1` contract; five later casts in both engines; existing zero-cost integration retains independent material/enhancement costs. |
| R3 | PASS | Five finite casts in both engines; exact selected native item Spend, charges 5 -> 0; rule/effects simulated. |
| R4 | PASS | Two native abilities share one item pool; fourth reservation with three uses refused; five-cast full-plan shortage with two uses refused by dispatch, zero spending. |
| R5 | PASS | Finite/unread transition after validation refuses final submission, no replay/effects/spending, enhancement lease disposed, both executor seams. Actual adapters additionally source-reviewed. |
| R6 | PASS | Deliberately faulty external adapter permits observed 3 -> 2 under free forecast; both executors halt, use/effect retained, zero validated confirmations, no free label. |
| R7 | PASS | Same spell/different native item refused; same-object rescan stable; saved token/targets retained; old Fact retained unresolved. Reload persistence support remains containment. |

Review checked native order, exact identity, source/variant contracts, shared
pools, temporary-state cleanup, old saved intent, effect/failure precedence and
truthful wording. The review found and fixed a missing-blueprint case where
fact-based getters could still read a count. Early native-fixture construction
and integration failures were corrected, not reported as product PASS. Logs of
the development iterations remain in `private/resource-repair` (2/1, 14/1,
16/1 and intervening passing runs); only the committed final run qualifies the
hashes above. The 16/1 iteration called the internal host directly with a denied
projection; the regression was corrected to exercise the real dispatch boundary.

Native inspection commands (read-only, output above):

```powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass -File scripts/Inspect-KingmakerVariantContracts.ps1 -TypePattern 'AbilityData$' -MethodPattern '^(GetAvailableForCastCount|Spend|get_SourceItem|\.ctor)$'
powershell.exe -NoProfile -ExecutionPolicy Bypass -File scripts/Inspect-KingmakerVariantContracts.ps1 -TypePattern 'ItemEntity$|Fact$|AbilityData$' -MethodPattern '^(get_IsSpendCharges|get_Charges|SpendCharges|get_Ability|get_SourceItem|get_Resource|get_ResourceCost)$'
powershell.exe -NoProfile -ExecutionPolicy Bypass -File scripts/Inspect-KingmakerVariantContracts.ps1 -TypePattern 'ItemEntity$|BlueprintItemEquipment$' -MethodPattern '^(ReapplyAbilities|ReapplyFactsForWielder|get_GainAbility)$'
powershell.exe -NoProfile -ExecutionPolicy Bypass -File scripts/Inspect-KingmakerVariantContracts.ps1 -TypePattern 'Ability$|Fact$' -ListMembers
powershell.exe -NoProfile -ExecutionPolicy Bypass -File scripts/Inspect-KingmakerVariantContracts.ps1 -TypePattern 'ItemEntity$|BlueprintItemEquipment$' -ListMembers
powershell.exe -NoProfile -ExecutionPolicy Bypass -File scripts/Inspect-KingmakerVariantContracts.ps1 -TypePattern 'ItemEntity$' -MethodPattern '^OnDidEquipped$'
```

## Native acceptance and next action

The lab/game/assemblies and exact protected Automation pair exist. Read-only
`Assert-KbpNotRunning`, `Assert-KbpNoUnresolvedTransaction`,
`Get-KbpDisposableSavePair -Family Automation`, and `Assert-KbpSteamSafety`
passed; [Steam receipt](evidence/resource-repair-2026-10-26/steam-prerequisites.log).
Baseline SHA-256: `f13de02d7c59a70a34d335a413f0d90fc7e63cdcb327224ecf5885ed6e1f8300`;
working SHA-256: `eded233c784fd658ddf2c46b2b54d5a310c39bff62506f0324b0f35c045bbec7`.
No save or live Mods mutation occurred.

There is no current-product safety/purity qualification receipt. The unchanged
guarded live route requires whole-history before/after manifests; recorded lab
windows took 6,711.85 through 9,235.64 seconds, with a later planning estimate of
101 minutes just for two manifests. See the
[recorded cost and prerequisite](HUMAN-FEEDBACK-REFINEMENT-0.5.1-RC2.md).
That exceeds the [ordinary 30-minute verification ceiling](VALIDATION-POLICY.md).
No broad purity wrapper was launched or bypassed; old receipts were not reused.
This is a qualification-budget boundary, not an absent installation, absent
protected save pair, Steam safety failure, or confirmation of the player's bug.

| Native acceptance | Status | Reason |
|---|---|---|
| Instant controller Run Long, five legal casts | NOT_RUN | Current safety/purity prerequisite exceeds ordinary boundary. No physical controller input observed. |
| Matched-state Instant mouse entry | NOT_RUN | Same prerequisite; no mouse or programmatic live entry invoked. |
| Animated normal and mismatch refusal | NOT_RUN | Same prerequisite; detached seams are not a live command queue. |
| Native insufficient-use full-plan and genuine unlimited | NOT_RUN | Same prerequisite; offline/native-method fixture results only. |
| Reporter's exact source/configuration | NOT_RUN | Additionally missing full casting log/plan/source instance and compatibility profile. |

Next action: select a separate, explicitly budgeted native qualification boundary;
freeze this product commit, build one guarded local package, satisfy current
launcher source/WhatIf purity, then select and cast representative finite and
unlimited routines via the existing guarded launcher/allowance/staging/restore
workflow using only `KBP_AUTOMATION_WORKING`. Include a supported single-item
source if the fixture can establish one, matched controller/mouse input and the
Animated refusal. Preserve baseline/all other saves and distinguish physical
input from programmatic entry. Obtain the full tester `[KBP-CF-RUN] casting`
record independently; do not substitute that missing evidence with lab results.
