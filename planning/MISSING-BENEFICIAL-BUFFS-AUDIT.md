# Missing beneficial buffs — repair and candidate audit

Mission: [planning/MISSING-BENEFICIAL-BUFFS-MISSION.md](MISSING-BENEFICIAL-BUFFS-MISSION.md)
(owner mission of 2026-11-21). Branch
`codex/kingmaker-buff-planner-missing-beneficial-buffs`, draft PR #25, from
evidence baseline `dd3e997100916576a268b24e46d49b4db7c34481` (mission commit
`a4ab152`). Product commit `e240d0a42e78af8539c79a3fbbeb4ab3878ad7b2`
(source, tests and audit harness; this record and its evidence follow in the
next commit). The final checkpoint after the owner addendum (mission §9)
(`76193a2`) added offline tests and records only. Review round 1 (nine
findings, all fixed; see [Review round 1](#review-round-1)) changed production
source again in the corrections commit that follows `76193a2`.
Version 0.6.19 unchanged (unreleased change). This record is
offline evidence only: **no native game check ran**, and by owner decision the
in-game checks are owner acceptance (see
[Native checks](#native-checks-owner-acceptance)).

## Root causes

1. **Blanket harmful-ally veto.** `NativeCandidateClassifier.IsSafeRecipient`
   refused every payload of an ability whose `EffectOnAlly` is `Harmful`. The
   native 2.1.7b Good Hope and Firebelly blueprints carry that metadata although
   neither can target an enemy at all (Good Hope: self/friend targets and its
   own `AbilityTargetsAround` `Ally` selector; Firebelly: Personal, `ToCaster`).
   Haste survives only because its ally disposition is `None`; its
   remove-Slow/otherwise-apply structure is the same as Good Hope's
   remove-Crushing-Despair branch and is not the cause.
2. **Enemy flags read literally on Personal-range abilities.** The
   `AbjurationResistance{Element}Ability` selections are Personal with
   `CanTargetSelf`, but also `CanTargetEnemies` with `EffectOnEnemy = Harmful`.
   The hostile-current-target rule (and, behind it, the 0.4.0
   hostile-ability-rider audit rule) refused them. Natively a Personal ability
   is anchored on its owner (`AbilityData.TargetAnchor == Owner`, proven by
   the assembly-backed contract test below), so its current target is its caster.
3. **Blanket point-target refusal.** `CanTargetPoint` was read as "requires
   ground placement". Blessing of Luck and Resolve Mass, Feather Step Mass,
   Firebrand and Heroic Invocation may also target self/friends, can never
   target enemies, and land their buff only through their own allied
   `AbilityTargetsAround` selector — the same contract the planner already casts
   on a unit anchor for unit-only group spells.

## Changes

- `Discovery/NativeCandidateClassifier.cs` (shared by live discovery, the
  catalogue export and both rule sets):
  - harmful-to-allies still vetoes every ability that can be aimed at a
    hostile unit. After review R1-F6 it is overridden ONLY for an ability that
    cannot be aimed at an enemy AND a payload named by an explicit,
    evidence-backed versioned entry (`NativeEffectOverrides.json`, new
    disposition `admit-despite-ally-disposition`, passed to the classifier in
    `NativeCandidateAuditFacts.AllyDispositionExceptionEffectIds`), and only
    when that payload blueprint was read (harmful flag and components
    resolved). Enemy targeting alone is not proof of benefit: a friend-only
    ability applying the native Crushing Despair buff (no `m_Harmful` flag)
    would have passed the round-1 rule. The entry keeps the discovered graph
    (unlike `include`), the payload is still judged by every harmful-buff,
    harmful-condition, offensive-carrier and marker rule, and the classifier
    source holds no spell names or identities (the source validator forbids
    special-casing owner-reported spells there). Entries: native Good Hope →
    `GoodHopeBuff`, native Firebelly → `FirebellyBuff`, and the Call of the
    Wild / Kingmaker Gunslinger variants the full-user export shows applying
    those same buffs, each with its evidence in the entry's reason;
  - `IsPersonalRange` / `CanBeAimedAtHostiles`: a Personal-range ability that
    can target itself is never "aimed at enemies" (hostile-current-target and
    hostile-ability-rider);
  - `IsUnitAnchoredAlliedArea`: a point-capable ability is admitted only when
    it may target self/friends, cannot target enemies, is not a sticky touch,
    and every persistent effect is a unit `Buff` reaching
    `AlliedAreaRecipients` through the ability's own first-child
    `AbilityTargetsAround` transform, with (review R1-F7) only action lists
    and conditional branches between that selector and the applying
    `ContextActionApplyBuff`: a nested cast, a second selector, a party/pet
    transform or a reflected wrapper could select around each outer
    recipient, which one anchor disk cannot represent, so it is refused.
    Everything else point-capable keeps `point-target-without-placement`. No
    ground coordinate is invented: the provider-option builder offers only
    units the native `AbilityData.CanTarget` admits, and the existing
    allied-area geometry predicts recipients from that unit.
- No `include` override was added (it would replace Good Hope's discovered
  allied area and conditional branch); the R1-F6 entries use the new
  graph-preserving `admit-despite-ally-disposition` disposition. The existing
  Freedom of Movement, True Seeing and Strength Surge overrides are unchanged
  and regression-tested.
- Review R1-F8: the decisions of the two Unity-bound builders were extracted
  into game-free production code the builders now call, so offline tests
  drive them with injected live reads: `GameAdapters/PartyProviderAdmission`
  (discovery gate, spontaneous-level pools, provider keys, fact-provider
  pools/costs, ambiguity rule) used by `KingmakerPartySnapshotBuilder`;
  `KingmakerFactResources.TryPriceAbilityResource` (native resource pool and
  cost) used by `TryRead`; `GameAdapters/ProviderTargetingPlanner` (reachable
  units, legal anchors from the native `CanTarget` verdict, no caster fallback
  for allied-area effects, per-anchor recipients from live distances, option
  construction) used by `KingmakerProviderOptionBuilder`. Behavior is
  unchanged; the builders only read live state.
- Mutually exclusive selections are never planned as stackable:
  `KingmakerActionGraphAdapter` keeps which buff an exact
  `ContextActionRemoveBuff` removes (and from whom); the scanner carries it as
  planning-only metadata on the existing empty node (`EffectRemoval`; never
  serialized, catalogue/saved identity unchanged); `ExplicitCastingCompiler`
  blocks a later casting of a routine whose native actions remove an effect an
  earlier casting of the same routine applies or keeps active on the same unit
  (`removes-planned-effect:<earlier casting>:<effect>:<unit>`, player text in
  enGB and zhCN). It casts nothing and reserves nothing. Switching away from a
  resistance active before the run is the ordinary native switch and is not
  blocked; re-selecting the active one is already satisfied (no recast, no use).
  After review round 1 the guard runs inside the budget pass in execution
  order (R1-F1/F2/F3): only a casting finally admitted (funded and Ready, or an
  already-satisfied skip that stays active) constrains later castings; a skip
  whose effect an earlier admitted casting removes is no longer satisfied and
  is Blocked (`effect-removed-by-earlier-casting:<earlier>:<effect>:<unit>`,
  enGB/zhCN text); conflicts read each casting's provider-BOUND expression
  (`EffectChangeAnalysis`: a removal counts on any branch binding leaves
  possible, and only a certain later re-application on the same path cancels
  it). The one-pass forecast forgets a structural grant whose effect a later
  casting removes (R1-F4). The retired Classic planner (`CastPlanner`) applies
  the same rule (R1-F5).
- `NativeCatalogExporter` / `KingmakerBuffSourceDiscovery` take an explicit
  `NativeBlueprintLookup` (default: the loaded library, unchanged behavior) so
  the regression tests run the real adapter → scanner → exporter facts →
  classifier → live-discovery path on detached native blueprints. The
  report-only effect name read no longer throws outside a Unity player.

## Named families

| Family | Status | Evidence |
|---|---|---|
| Good Hope (`a5e23522…`, buff `85af9f0c…`) | **Fixed; offline-verified; native NOT RUN** | Baseline `harmful-ally-disposition`, repaired `valid-beneficial-party-effect` through its evidence-backed versioned entry (without it: still excluded); live discovery admits it; production provider admission gives a bard who knows it a castable entry and one who does not none; whole expression kept (allied area, HasBuff branch, Crushing Despair removal); 15-ft radius read natively; production anchor coverage = units within 15 ft; one mass cast, one level-3 slot, out-of-radius ally never predicted; existing-buff verdicts read `GoodHopeBuff`; despairing recipient never reported as receiving Good Hope; rediscovery identical. Unread payload or a penalty payload on the same shape stays excluded. |
| Haste (`486eaff5…`) | Unchanged (available before and after) | Same tests. |
| Firebelly (`b0652310…`, buff `7c33de68…`) | **Fixed; offline-verified; native NOT RUN** | Baseline `harmful-ally-disposition`, repaired `valid-beneficial-self-effect` (Caster payload) through its evidence-backed entry. The breath it grants (`5e5b663f…`, AddFacts) stays excluded (point-target, offensive). |
| Abjuration Resistance — Acid/Cold/Electricity/Fire/Sonic | **Fixed; offline-verified; native NOT RUN** | Baseline hostile current target; repaired personal self buff, payload = own resistance; each removes exactly the other four; production pricing: all five share the one native pool `wizard|resource|d16e573d…` at 1 use each, personal (only the owner is a legal target); no finite lifetime; Fire-then-Cold blocked as `removes-planned-effect`; active choice already satisfied; switching casts once; with Fire active, Cold-then-Fire blocks the Fire skip (`effect-removed-by-earlier-casting`, R1-F1); Classic planner refuses the same combinations (R1-F5). |
| Blessing of Luck and Resolve Mass, Feather Step Mass, Firebrand, Heroic Invocation | **Fixed (unit-anchor path); offline-verified; native NOT RUN** | Baseline `point-target-without-placement`, repaired party effect via own allied selector; native radii 30/15/30/30 ft; anchored plan predicts only the anchor's in-radius allies; one cast, one slot, step anchored on a unit. Native `AbilityData.TargetAnchor` is `Point` for these spells, so the in-game legality of a unit target (native `CanTarget`) is **unverified** and left to owner acceptance (check 8). Everything depending on it fails closed, with offline coverage (`missing-beneficial-buffs-point-group-unit-anchor-refused-fails-closed`): no admitted unit → anchored and caster-centred castings Blocked (`origin-anchor-illegal` / `origin-caster-illegal`, worded), nothing reserved, no step; a refused authored anchor never falls back to another centre; an anchor refused at cast time is never submitted by either engine (`target-invalid`, no spend, no confirmation). Since review round 1 the accepted and refused anchors come from the production `ProviderTargetingPlanner` under the injected native verdict (no caster fallback for these allied-area effects), and the provider/pool from `PartyProviderAdmission`. Enlarge/Reduce Person, Mass planned together on the same allies: the later casting is Blocked (`removes-planned-effect`); an unfunded one never blocks a funded alternative (R1-F2); the one-pass forecast recasts Enlarge after an intervening Reduce (R1-F4). A nested same-radius burst is refused (R1-F7). |
| Irresistible Dance (Call of the Wild) | Still excluded | Loaded-blueprint facts (0.4.1 full-user export entry, committed verbatim as a fixture and proven identical by the explicit verify check): can target enemies, harmful to allies → `harmful-ally-disposition` under both rule sets. |
| Negative controls | Still excluded | Hideous Laughter, Call Lightning (`harmful-ally-disposition`: it can target enemies), Firebelly's breath, Dazing Touch (harmful condition), Treat Affliction (restoration + cooldown marker), Good Hope (Troll Lair) point variant with an `Any` selector. |

## Candidate audit (§3.D)

Method: the explicitly selected evidence check
`evidence-candidate-audit-missing-beneficial-buffs-*` (renamed from
`candidate-audit-…` after review R1-F9; never part of an ordinary or
no-prefix run; export root overridable with `KBP_CANDIDATE_AUDIT_EVIDENCE_ROOT`)
streams every ability of a guarded in-game catalogue export, rebuilds the
exact classifier facts the export recorded from the loaded blueprints (plus
the current versioned ally-disposition entries, as live discovery applies
them), and judges them with the frozen baseline classifier and the repaired
one, under **both** Current and Pre040 and in both live and static scope.
Nothing is filtered (rows excluded under both rule sets are kept). The
exports' historical dispositions are not read; the historical
`planning/NATIVE-BUFF-CATALOG.json` labels are not used.

| Scope | Export (generator) | Abilities | Changed | Full report SHA-256 (round 1) |
|---|---|---|---|---|
| native-only | `kbp040-rc4p-catalog-native` (`7ccf0c9`) | 1,722 | 16 | `bb75bcc491a44184a403f2194fa5915fb6da71d856833396d88e58d4d39a915a` |
| full-user | `kbp041-rel-catalog-full` (`85e7410`) | 10,443 | 48 | `2cdd2a56f9c7e5cc91b6ca6e6807cbcd078d797e0965edd33f64f68823aef7a5` |

Judged (review round 1) by classifier source SHA-256
`95e923dce699566d6ff103f86c56b69e570eb419baafcde293c915f2f7ba5bb4` with
`NativeEffectOverrides.json` `c462aeb401e413599bf610db6e287e029f80f67d70c59de76eece2bb266478d0`
(working-tree bytes). Changed rows:
[native-only-changes.tsv](../docs/evidence/missing-beneficial-buffs-2026-11-21/native-only-changes.tsv)
(`ce707f14…39e7`), [full-user-changes.tsv](../docs/evidence/missing-beneficial-buffs-2026-11-21/full-user-changes.tsv)
(`021ebe4d…83ce`). The changed rows are byte-identical to the round-1 files
(classifier `2ac1b450…`, reports `40ca1ef8…` / `45cb68e5…`); only the header
label differs. The narrowed ally-disposition rule and the nested-burst rule
removed no audited row: every harmful-ally → include row applies
`GoodHopeBuff` or `FirebellyBuff` and has an entry, and every admitted
point-capable row is a direct allied burst.

**Every changed native row** (live Current; all four scopes agree except the
three Physical Enhancement rows, which the static index does not reach):

| Ability | Baseline reason | Repaired |
|---|---|---|
| Good Hope | harmful-ally-disposition | party effect |
| Firebelly | harmful-ally-disposition | self effect |
| Resistance — Acid, Cold, Electricity, Fire, Sonic | hostile current target | self effect |
| Physical Enhancement — Strength, Dexterity, Constitution (`TransmutationSchool*Ability`, live scope only) | hostile current target | self effect |
| Blessing of Luck and Resolve Mass, Feather Step Mass, Firebrand, Heroic Invocation | point-target-without-placement | party effect |
| Enlarge Person Mass, Reduce Person Mass | point-target-without-placement | party effect |

Credible related false negatives found by the audit and admitted by the same
structural rules: the transmuter's **Physical Enhancement** selections
(Personal, mutually exclusive choices of the Abjuration shape) and **Enlarge /
Reduce Person, Mass** (friend-only point-capable allied bursts; each removes
the other's buff, which the removal-conflict guard handles).

full-user: the 48 rows are the families above plus their Call of the Wild
variants (spell kenning, lore/wish manifestations, school understanding,
Kindness, Twinned Eidolon, Infernal Arcana) and the Kingmaker Gunslinger
Ifrit Lavasoul Firebelly ability. (Round 1 reported one reason-only change,
Shadow Evocation, Greater, to offensive-carrier-only; with the narrowed rule
its reason stays `harmful-ally-disposition`. It was and is excluded.)

Reviewed and correctly still excluded: every remaining
`harmful-ally-disposition` row can target enemies (Call Lightning, Call
Lightning Storm, Lightning Lord, Irresistible Dance, Blade Lash, Devour, Life
Lich); the remaining friend-only point rows use an ambiguous selector (Good
Hope Troll Lair, NPC-only); the remaining friend-capable hostile-current-target
rows are offensive (Fester, Baleful Polymorph, Cacophonous Call, Deadly
Performance, Corrosive Touch, Jolting Portent, Channel Positive — Damage Undead)
or NPC/cutscene abilities, plus Freedom of Movement, True Seeing and Strength
Surge, which keep their existing overrides.

Scope limits: facts come from the guarded in-game exports of 0.4.0/0.4.1
(adapter of that time; mod versions then staged). full-user staged 15 mods
including Call of the Wild (23,127 inventoried blueprints), Kingmaker
Gunslinger and others listed in the export's `ownershipSources`; mods without
an inventory are reported "unattributed". **Tabletop Added Rules was not
present** (no local copy). This is not full current installed-stack coverage:
a fresh guarded `native-buff-catalog` export per profile is the native
confirmation and is blocked (below).

## Native checks (owner acceptance)

**Not run; no runtime transaction was started for this mission.** The first
enrollment stopped `BLOCKED` on two facts (re-verified 2026-11-21): the lab
deployment lock is still owned by transaction `zhcn19-zh-auto-01` in
`ModsRestoredPendingDisplay`, so every guarded launch refuses; and the
automation fixture party (Hedwirg; Linzi, spontaneous bard with
Heroism/Mirror Image/Resistance/Light; Tartuccio, sorcerer CL1) has none of the
repaired spells, which the casting scenarios cannot select.

**Superseded by owner decision of 2026-11-21 (mission §9, option B):** the
automated native/in-game checks are not required for this mission. The owner
tests the change in their own game; the exact checklist, what each check proves
and what a failure means is in
[MANUAL-ACCEPTANCE.md](../docs/MANUAL-ACCEPTANCE.md#missing-beneficial-buffs--owner-in-game-acceptance-pending).
The `zhcn19-zh-auto-01` lock itself remains a separate, still-open lab item and
was not touched; no fixture, harness or launcher change was made.

Native behavior only the game can confirm, and how the code handles it:

| Unverified native behavior | Code dependency | Handling |
|---|---|---|
| Native `CanTarget` admits a unit centre for the four point-capable group spells (native anchor `Point`) | Unit-anchored planning and casting | Fails closed at planning (no legal anchor, Blocked, worded reason) and at cast time (`target-invalid`, never submitted); offline-tested. Owner check 8. |
| The loaded 2.1.7b blueprints match the dump/export facts (Good Hope, Firebelly, Abjuration, group spells) | Admission | A different loaded blueprint is judged by the same rules; the ally-disposition entries apply only to their named payload buffs, only when those buffs are read, and only on an ability that cannot be aimed at an enemy (otherwise excluded). Owner checks 1, 4, 5, 8. |
| Personal-range selections are castable only on their owner | Abjuration options | Unchanged existing path: the option builder offers only units native `CanTarget` admits (the same path as already-qualified personal spells such as Mirror Image); none admitted → `target-unreachable`. Owner check 5. |
| Native application/removal (Good Hope's Despair branch, resistance switching, area reach) | Confirmation and existing-effect decisions | Confirmation reads observed effects only; a recipient whose Despair was removed is never reported as receiving Good Hope (offline-tested). Owner checks 2, 3, 6, 7. |

## Review round 1

Codex review of `76193a2` (decision `changes_required`, nine findings). All
fixed in the corrections commit after `76193a2`; regressions named in the
validation table.

| Finding | Correction | Regression |
|---|---|---|
| R1-F1 switch invalidates a later already-active skip (P1) | Skip blocked when an earlier admitted casting removes its effect (`effect-removed-by-earlier-casting`) | `…-switch-invalidates-a-later-already-active-skip` |
| R1-F2 unfunded casting blocks a funded one | Conflicts judged in the budget pass; only finally admitted castings constrain | `…-unfunded-casting-does-not-block-a-funded-one` |
| R1-F3 alternatives treated as running together | `EffectChangeAnalysis` on the provider-bound expression, branch- and order-aware | `…-removal-conflicts-follow-bound-branches` |
| R1-F4 one-pass forecast keeps removed effects | Removal invalidates structural grants across the sequence | `…-one-pass-forecast-forgets-removed-grants` |
| R1-F5 Classic planner bypasses the exclusion | Same rule in `CastPlanner.PlanRoutine` | `…-classic-planner-refuses-exclusive-choices` |
| R1-F6 harmful-ally exception unproved (P1) | Evidence-backed graph-preserving registry entries; fail closed on unread payload | `…-ally-disposition-exception-is-evidence-bound` |
| R1-F7 nested bursts with different centres | Whole recipient path checked | `…-nested-allied-burst-refused` |
| R1-F8 tests supplied provider/anchor decisions (P1) | Decisions extracted to game-free production code; tests inject only live reads | `…-provider-admission-known-and-unknown-bard`, `…-abjuration-selections-share-the-native-pool`, rewritten area/anchor/fail-closed tests |
| R1-F9 machine-specific test inputs | Committed faithful fixture subset + Dance entry; inventory replay and fixture refresh are explicit `evidence-*` checks with configurable inputs | ordinary run reads no lab path; explicit capture/verify/audit checks |

## Review round 2

Codex review of `9296fbb` (decision `changes_required`). Verified: R1-F2,
R1-F4, R1-F5, R1-F7, R1-F9. Re-opened: R1-F1, R1-F3, R1-F6, R1-F8. New:
R2-F1 to R2-F6. All are fixed in the round-2 corrections commit.

| Finding | Correction | Regression |
|---|---|---|
| R1-F1 invalidated skip still noted "skipped" | Blocked skip's `already-active:` notes become `existing-removed-earlier:` ("active now, but an earlier casting in this routine removes it"), enGB/zhCN | `…-switch-invalidates-a-later-already-active-skip` (asserts the inspector text) |
| R1-F3 possible re-application cleared a removal | `EffectChanges.Guaranteed`; only guaranteed applications clear recorded removals | `…-removal-conflicts-follow-bound-branches` (reversed order: alternative then skip-if-active E → blocked; certain re-application → kept) |
| R1-F6 approved leaf admitted an unapproved branch | Exception requires EVERY persistent payload of the ability to be an approved, read buff | `…-ally-disposition-exception-is-evidence-bound` (Good Hope + Crushing Despair branch; + unread branch → excluded) |
| R1-F8 wrong slot level for three group spells | Fixture book and level from each blueprint's first native SpellListComponent (paladin 4, druid 3, wizard 7, wizard 9; Enlarge/Reduce wizard 4); plan cost, provider and converted step reservation asserted | `…-point-capable-group-spells-unit-anchored` |
| R2-F1 Despair removal treated as a Good Hope grant | One-pass structural grant only when every effect the cast may apply to the unit is guaranteed | `…-one-pass-grants-only-guaranteed-effects` (Good Hope twice → second casts; Feather Step Mass repeat → free) |
| R2-F2 structural skip left its effect unprotected | A proven structural repeat is decided first and admitted as a kept effect | `…-structural-skip-protects-its-effect` |
| R2-F3 Classic sequence carried Fire after a Cold switch | `SequentialForecastPlanner` drops each funded step's removals before projecting grants | `…-classic-sequence-forecast-applies-removals` |
| R2-F4 Classic rejected a provider before trying another centre | Removal conflicts evaluated per legal centre in preference order | `…-classic-picks-a-conflict-free-centre` |
| R2-F5 Mass Blessing coverage ignored recipient conditions | Recipient-fact gates (`ContextConditionHasFact`, planning metadata) bound per unit from the caster's and the unit's live facts; an unread fact withholds coverage; a point-capable selector with its own or an unread `m_Condition` yields no coverage (`KingmakerAreaCoverageResolver`) | `…-area-coverage-follows-recipient-conditions` |
| R2-F6 stale durable records | Coverage matrix and resource/casting inventory describe the registry entries and budget-order conflict check | text check below |

Consequence of R2-F5 for native behavior that stays unverified: unit facts
(creature types, buffs used as conditions) are read live with
`Descriptor.HasFact`; whether that read matches native `ContextConditionHasFact`
in every case is an owner observation. Coverage is withheld rather than claimed
when a fact cannot be read.

## Rejected approaches

- An `include` override for Good Hope/Firebelly: rejected — it replaces the
  discovered graph (allied area, conditional branch).
- The round-1 purely structural harmful-ally rule ("cannot target an
  enemy"): superseded after review R1-F6 — it admitted unproved payloads (an
  unread buff, a friend-only penalty buff with a clear harmful flag). Replaced
  by the narrow evidence-backed, graph-preserving versioned entries.
- A GUID list inside the classifier: rejected — the repository's source
  validator forbids special-casing owner-reported spells in production
  classification; proven exceptions belong in the versioned overrides file
  (AGENTS.md).
- Removing hostile/harmful checks broadly: rejected — Call Lightning and
  Irresistible Dance would enter under Pre040, and riders under Current.
- Treating the remove/apply conditional as the cause: rejected — Haste has the
  same structure.
- Rewriting Personal-range `CurrentTarget` leaves to `Caster` in the adapter:
  rejected — it would change every personal spell's graph and Share
  Transmutation's retargeting.

## Remaining limitations

- In-game behavior is unverified (owner acceptance pending, see
  [Native checks](#native-checks-owner-acceptance)); in particular native
  `CanTarget` for a unit centre of the four point-capable group spells. If the
  game refuses it, those spells stay unplannable (fail closed) and a separate
  targeting extension would be needed.
- The candidate audit reads the guarded 0.4.0/0.4.1 loaded-blueprint exports;
  no fresh current-stack export was made (it needs a guarded launch). Tabletop
  Added Rules was not in scope.

- The retired Classic planner checks removal conflicts conservatively (its
  candidate check binds with the option's own facts; a skip check without a
  pinned caster cannot resolve caster-role owners and so does not see a
  removal of a caster-role effect); round 1 closed the gap for the shapes in
  this mission (R1-F5).
- New ally-disposition exceptions are added only as evidence-backed entries in
  `NativeEffectOverrides.json`; a future modded variant applying the same buff
  under a new ability GUID stays excluded until it has an entry.
- Abjuration and Physical Enhancement durations stay "unknown" in the
  duration estimate (the native application is `AsChild` beside removals);
  no finite duration is invented and an active selection with no expiry
  satisfies a re-selection.

## Validation (offline)

Windows PowerShell 5.1, `scripts/Test-Development.ps1` (source validation
47/0 and Release build each time):

| Command (`-Only`) | Result | Duration |
|---|---|---|
| baseline `native-candidate,communal-canaries,persistent-beneficial,catalog-audit,catalog-adjudication,restorative-marker,area-recipient,conflicting-recipient` | 15/0 | 6.2 s |
| `missing-beneficial-buffs` | 13/0 | 6.8 s |
| `candidate-audit-missing-beneficial-buffs` | 2/0 | 7.9 s |
| all prefixes `a`…`z` (complete offline program, final working tree) | 764/0 | 37.5 s |

Final checkpoint (owner addendum §9; tests and records only, production
source unchanged), same entry point, on the final working tree before commit:

| Command (`-Only`) | Result | Duration |
|---|---|---|
| `missing-beneficial-buffs` (adds `…-point-group-unit-anchor-refused-fails-closed` and the Enlarge/Reduce Mass planning conflict) | 14/0 | 6.5 s |
| the eight baseline neighbor families above plus `missing-beneficial-buffs,candidate-audit-missing-beneficial-buffs` | 31/0 | 8.9 s |
| all prefixes `a`…`z` (complete offline program) | 765/0 | 39 s |

The candidate audit reproduced unchanged (native-only 1,722 / 16 changed,
full-user 10,443 / 48 changed, classifier `2ac1b450…`). The controller's
`focused` gate receipt (`-Only missing-beneficial-buffs`) is bound to the
committed candidate by the controller, not to this record.

Review round 1 corrections, same entry point, final working tree before
commit (DLL `741941be…` for that tree; Build.ps1 stamps HEAD, so the committed
candidate's DLL hash differs and is recorded by the gate receipt):

| Command (`-Only`) | Result | Duration |
|---|---|---|
| `missing-beneficial-buffs` (23 regressions, read only the committed fixtures) | 23/0 | 5.7 s |
| baseline neighbor families + `effect-overrides,missing-beneficial-buffs` + the three moved source-contract tests (`provider-choices-name-the-book-and-refuse-twins`, `cantrips-cast-at-will-through-the-class-ability`, `confirmation-needs-an-instance-this-attempt-applied`) | 42/0 | 5.9 s |
| all prefixes `a`…`z` (complete offline program; the two audit checks are now explicit, nine regressions were added) | 772/0 | 39.4 s |
| explicit `evidence-native-fixtures-missing-beneficial-buffs-capture` (every regression re-run against the full lab dump; 45 blueprints in full, 50 referenced) | 1/0 | 8 s |
| explicit `evidence-native-fixtures-missing-beneficial-buffs-verify` (45 files byte-identical to the dump, 50 reference types match, Dance entry identical to the export) | 1/0 | 5.4 s |
| explicit `evidence-candidate-audit-missing-beneficial-buffs` (1,722 / 16 and 10,443 / 48) | 2/0 | 13.5 s |

Review round 2 corrections, final working tree before commit (DLL
`ffc38327…` for that tree):

| Command (`-Only`) | Result | Duration |
|---|---|---|
| `missing-beneficial-buffs` (28 regressions, committed fixtures only) | 28/0 | 5.9 s |
| all prefixes `a`…`z` (complete offline program; includes every neighbor family above) | 777/0 | 34.6 s |
| explicit `evidence-native-fixtures-missing-beneficial-buffs-capture` (all 28 against the full dump; fixture subset unchanged, 45 / 50) | 1/0 | 7.7 s |
| explicit `…-verify` + `evidence-candidate-audit-missing-beneficial-buffs` (45 identical files, 50 types, Dance identical; 1,722 / 16 and 10,443 / 48) | 3/0 | 14 s |

The round-2 audit judged with classifier `9f6ba19b9089aad37a38e251b00eccc83c60704c1a6ba5dd4aae8bf39c459970`
(reports `d4b69b2805ba5416eac829db996382b7b596966dc7a2cb918a3c4403713d8f50` /
`351b1ee7d6c51427777b387b8b9575e49e9aa30003cd22f923502843cbbf871d`); the changed
rows are again identical, header label only. Round-2 reproductions were traced
against `9296fbb`, not re-executed on it.

The first complete run after the extraction failed 769/3: three existing
source-text contract tests pinned code that moved into
`PartyProviderAdmission` / `ProviderTargetingPlanner`. Each assertion was
moved to the new location with the same contract (none removed), then 3/0.
Reproduction of the round-1 defects: the corresponding regressions were traced
against the `76193a2` code (each counterexample in the review leads to the
asserted wrong outcome there); the old production code was not re-executed
with the new tests.

Baseline reproduction: each regression judges the same native facts with the
frozen `dd3e997` classifier and asserts the original exclusion before
asserting the repair. Native/purity/release coverage is deferred.
