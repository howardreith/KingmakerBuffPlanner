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

## Review round 3

Codex review of `1bfc198` (decision `changes_required`). Verified: R1-F1,
R1-F3, R1-F6, R2-F1, R2-F2, R2-F3, R2-F4, R2-F6. Re-opened: R1-F8, R2-F5. New:
R3-F1 to R3-F5. All are fixed in the round-3 corrections commit.

| Finding | Correction | Regression |
|---|---|---|
| R1-F8 reservation against a spell-list ID | Each group spell is cast from a real native spontaneous `BlueprintSpellbook` whose `SpellList` equals one of the spell's own lists (Inquisitor 6, Bard 3, Sorcerer 7 / 9 / 4); the pool key is that book's GUID, as the snapshot builder keys it; plan cost, provider spellbook and converted step reservation asserted | `…-point-capable-group-spells-unit-anchored` |
| R2-F5 unread caster fact still covered undead | After binding a recipient's branch, a remaining caster-fact gate withholds definite coverage | `…-area-coverage-follows-recipient-conditions` (arcana unread: undead not covered, living covered) |
| R3-F1 Mass Reduce undid Enlarge on a removal-only ally | Options carry per-anchor affected units (every unit inside the burst) beside eligible recipients; both planners and the Classic forecast analyze removals over the affected units | `…-removal-only-ally-is-counted` (keep Enlarge on A → Reduce over A,B refused naming A, zero cost; without the keep: one cast, grant on B, removal on A accounted) |
| R3-F2 damage on an alternative branch under the exception | The ally-disposition waiver also requires no offensive action anywhere in the graph | `…-ally-disposition-exception-is-evidence-bound` (Despair-branch `ContextActionDealDamage` → refused by discovery and provider admission) |
| R3-F3 Classic projection lost branch and order | Grants first, then the step's NET removals from the selected provider's bound expression (affected units included) | `…-classic-sequence-uses-bound-net-changes` (gate false keeps E; gate true loses E; apply-then-remove leaves no E) |
| R3-F4 removal note for every group recipient | Only units whose effect an earlier admitted casting removes get the removal note; others read "still active (this casting is blocked and does not run)" | `…-removal-only-ally-is-counted` (A removed, x still active) |
| R3-F5 discarded centre's conflict reported over a funding failure | A removal conflict is reported only when no candidate had a conflict-free centre | `…-classic-picks-a-conflict-free-centre` (safe centre, empty pool → resource refusal) |

Validation (final tree before commit, DLL `91eaa7c4…`): `missing-beneficial-buffs`
30/0 (5.9 s); complete program 779/0 (37.8 s); explicit capture 1/0 (fixture
subset now 48 blueprints / 58 referenced types: the three native spellbooks
added); explicit verify + candidate audit 3/0 (48 identical files, 58 types,
Dance identical; 1,722 / 16 and 10,443 / 48; classifier
`9e0c65a74c129ca6104f7c7787bec5477107ce07cd55060d4dbae7648ae66dce`, reports
`175b90a3…b443` / `032fae87…1489`, changed rows identical apart from the
header). Reproductions traced against `1bfc198`, not re-executed on it at the
time; review round 4 executed them (below).

## Review round 4

Codex review of `bac569e` (decision `changes_required`, six findings). All are
fixed in the round-4 corrections commit.

| Finding | Correction | Regression |
|---|---|---|
| R4-F1 offensive branch admitted by the point-capable rule | `IsUnitAnchoredAlliedArea` also requires no offensive action anywhere in the graph (the check the ally-disposition waiver already used) | `…-offensive-branch-refuses-point-admission` (Mass Blessing with `ContextActionDealDamage` on its construct branch → excluded by discovery and provider admission; unmodified Mass Blessing still included) |
| R4-F2 activation damage on an approved payload | The offensive-action check also reads every effect's recorded fact actions (`Offensive` of `AddFactContextActions` Activated/Deactivated/NewRound) | `…-payload-activation-damage-refuses-ally-exception` (GoodHopeBuff with activation damage: the exporter records it offensive; discovery and admission refuse) |
| R4-F3 unaffected unit created a removal conflict | Options carry the FIXED recipient facts read for units inside their bursts (unit facts that are not buffs: creature types); conflict analysis in the compiler, the Classic planner and the Classic forecast binds them per unit (`ProviderPlanningOption.BindForRecipient`). Mutable facts (an opposing buff an earlier casting may add) stay unresolved, so every branch they could select is still analyzed | `…-unaffected-animal-creates-no-conflict` (Enlarge over h1 and Reduce over h2 overlapping only on an animal companion: both Ready, one slot each, own beneficiary; Classic plans both) |
| R4-F4 blocked mixed casting still promised a cast | A blocked casting's `already-covered`, `already-covered-longer` and `existing-active-recast` notes become kept/removed notes like `already-active`; `existing-insufficient` / `existing-incomplete` become `existing-not-cast-blocked:` ("not cast on X (this casting is blocked and does not run)", enGB/zhCN); a `removes-planned-effect` block now passes the earlier-removed owners, and a unit not in them reads "still active" | `…-blocked-mixed-casting-notes-do-not-promise-a-cast` |
| R4-F5 caster fact as the only gate ignored | With no recipient gate, coverage requires an application left after caster binding and no remaining caster gate: known false → no application; unread → withheld | `…-caster-only-gate-controls-area-coverage` (true → ally and caster, Ready, one slot; false or unread → no recipients, Blocked, zero cost) |
| R4-F6 round-3 corrections lacked executed reproductions | Both baselines executed (below) | — |

**Executed baselines** (R4-F6). Method: the production files touched by the
round's corrections were replaced in the working tree by their content at the
baseline commit (`git show <rev>:<path>`), keeping the current tests and the
complete committed fixture subset (48 blueprints / 58 referenced types, so no
regression failed for a missing fixture or a compile error); then
`.\scripts\Test-Development.ps1 -Only 'missing-beneficial-buffs'`; then the
files were restored.

| Baseline | Result | Failed behavioral assertions |
|---|---|---|
| Round 3: production of `1bfc198` (DLL `86a53faf…`) | 25/5 | `…-ally-disposition-exception-is-evidence-bound`: "A Good Hope variant that damages an ally on its Despair branch was admitted" (R3-F2); `…-classic-picks-a-conflict-free-centre`: "A safe but unfunded Reduce centre was reported as a removal conflict: reduce:removes-planned-effect:enlarge:…:p" (R3-F5); `…-area-coverage-follows-recipient-conditions`: "coverage ignores recipient conditions (arcana=): caster,living,undead" (R2-F5, arcana unread); `…-removal-only-ally-is-counted`: "Mass Reduce undid Enlarge on a removal-only ally the routine keeps: Ready" (R3-F1); `…-classic-sequence-uses-bound-net-changes`: "A known-false removal erased E between Classic routines." (R3-F3) |
| Round 4: production of `bac569e` (DLL `cf8b2335…`) | 30/5 | `…-offensive-branch-refuses-point-admission`: "A point-capable allied burst that damages constructs was admitted" (R4-F1); `…-payload-activation-damage-refuses-ally-exception`: "Good Hope was admitted although its buff damages its bearer on activation" (R4-F2); `…-unaffected-animal-creates-no-conflict`: "Blocked removes-planned-effect:enlarge:…:pet" (R4-F3); `…-blocked-mixed-casting-notes-do-not-promise-a-cast`: "already active on b (the cast goes ahead for the others)" (R4-F4); `…-caster-only-gate-controls-area-coverage`: "A known-false caster gate left beneficiaries or a cost: ally,caster Ready" (R4-F5) |

Limits of the round-3 baseline: R3-F4 (the per-unit removal note) is asserted
later in `…-removal-only-ally-is-counted`, after the R3-F1 assertion that
already fails on `1bfc198`, so it is masked there and not separately observed
(review round 5 observed it directly with an isolated reproduction, below).
The R1-F8 spellbook correction is test evidence (fixture books and pool keys),
so it passes on either production. To compile the current tests against
`1bfc198`, the R3-F1 test asserts covered units behaviorally instead of
through the later `AffectedTargetIdsForAnchor` accessor. In the first
round-4 run, R4-F2's assertion sat behind R4-F1's in one test; it was split
into its own test and the run repeated (30/5 above).

Corrected results, final working tree before commit (DLL `29592555…`):
`missing-beneficial-buffs` 35/0 (6.2 s); complete program 784/0 (34.9 s);
explicit capture 1/0 (fixture subset unchanged, 48 / 58); explicit verify +
candidate audit 3/0 (48 identical files, 58 types, Dance identical; 1,722 / 16
and 10,443 / 48; classifier
`1d2ec32d0c59932528e84ca64a1c2d680f1186a418f5fc9ca6a9de2308c175fd`, overrides
`c462aeb4…`, reports `43458c22…8d98` / `d99e781a…7834`, changed rows identical
apart from the header label).

Native behavior that stays unverified: which unit facts are fixed is decided
from the loaded blueprint type (`BlueprintUnitFact` that is not a
`BlueprintBuff`); an unresolvable fact is treated as mutable (conservative).

## Review round 5

Codex review of `1fa270d` (decision `changes_required`). Verified: R4-F1 to
R4-F5. Re-opened: R4-F6. New: R5-F1 to R5-F6. All are fixed in the round-5
corrections commit.

| Finding | Correction | Regression |
|---|---|---|
| R4-F6 R3-F4 baseline still masked | An isolated synthetic reproduction (no fixture): a direct casting applies X and removes E on a; a later skip-if-active group casting relies on E on a and b. Executed on `1bfc198` (below) | `…-removed-earlier-note-names-only-the-affected-ally` |
| R5-F1 harmful buff beside a valid one in a point-capable burst | `IsUnitAnchoredAlliedArea` also refuses any applied effect that harms its bearer (`m_Harmful`, a harmful-condition-only buff, or a buff whose own automatic actions apply one) | `…-point-burst-harmful-buff-refused` (Feather Step, Mass + the committed DazeBuff on its allied actions → excluded by discovery and admission; unmodified spell admitted) |
| R5-F2 debuff applied on activation under the waiver | The ally-disposition waiver also refuses an effect that harms its bearer through its recorded automatic applications (`FactActions.AppliedEffects`: harmful flag or harmful condition), and an automatic application whose blueprint was not read. A separately granted ability (Firebelly's breath, `AddFacts`) is not an automatic application | `…-payload-activation-debuff-refuses-ally-exception` (GoodHopeBuff `Activated` applies DazeBuff to its bearer; the exporter records the nested `Dazed`; discovery and admission refuse; real Good Hope and Firebelly still admitted) |
| R5-F3 mixed caster/recipient checker treated as unconditional | Area coverage counts only a leaf not behind an UNMODELLED conditional (non-empty native checker expressed as neither a caster- nor a recipient-fact gate). The exact native `ContextConditionHasBuff` is now modelled as a recipient-fact gate (read like `ContextConditionHasFact`); without that, the remove-opposing / otherwise-apply shape would lose all coverage. The mixed predicate itself is not evaluated: coverage is withheld also when both facts are true | `…-mixed-caster-recipient-condition-withholds-coverage` (arcana false and true → no recipients, Blocked, zero cost) |
| R5-F4 skip protected an effect on a non-beneficiary | A skip (live or structural) enters conflict state with its beneficiaries' changes only; an executing cast is still analyzed over every unit it reaches | `…-skipped-mass-casting-protects-only-its-recipients` (skip Enlarge over e; Reduce over u, disks overlapping only on reduced h → Ready, one slot; control: a Reduce disk reaching e is refused naming e) |
| R5-F5 sequential forecasts reused initial beneficiaries | `ProjectedEffectChanges` records each funded casting's/step's bound net changes; in a one-pass sequence a later routine's group beneficiary keeps its claim only if its payload still lands under every value an earlier routine may have left for its condition facts (more than one changed fact, or an unread one, withholds it); nobody left → `predicted-coverage-empty`. The Classic forecast narrows each later occurrence's options the same way. Same-routine changes are left to the removal-conflict guard; independent routine previews are unchanged | `…-sequential-forecast-follows-opposing-casts` (one-pass: Enlarge→a, Reduce→b only, one slot each; independent preview still a,b) and `…-classic-sequential-forecast-follows-opposing-casts` |
| R5-F6 unfunded mixed casting promised a cast | The ledger refusal finalizes notes like a removal block (shared `BlockedNotes`) | `…-unfunded-mixed-casting-notes-do-not-promise-a-cast` (one slot, Haste first, Good Hope over a longer-buffed and an unbuffed ally → resource refusal, zero cost, "still active (this casting is blocked and does not run)") |

**Executed reproductions.** Same method as round 4 (production files swapped
by `git show <rev>:<path>`, current tests, complete committed fixtures, then
restored from HEAD):

| Baseline | Command (`-Only`) | Result | Failed behavioral assertions |
|---|---|---|---|
| Round-5 regressions on the production of `1fa270d` (DLL `83ebc6f8…`) | `missing-beneficial-buffs` | 36/7 | point burst + Daze: "admitted: valid-beneficial-party-effect…" (R5-F1); activation Daze: "Good Hope was admitted although its buff dazes its bearer on activation" (R5-F2); mixed condition: "(arcana=False) was treated as unconditional: caster,living,undead Ready 1" (R5-F3); skip: "AlreadySatisfied / Blocked removes-planned-effect:skip:…:h" (R5-F4); one-pass: "Ready a,b" (R5-F5); Classic: "a,b / a=Fulfilled,b=Fulfilled" (R5-F5); unfunded notes: "already active on ally-near (the cast goes ahead for the others)" (R5-F6) |
| R3-F4 isolated, production of `1bfc198` (DLL `2ea03bb3…`) | `missing-beneficial-buffs-removed-earlier-note-names-only-the-affected-ally,missing-beneficial-buffs-removal-only-ally-is-counted` | 0/2 | isolated: the skip IS refused (first assertion passes), then "b note 'existing-removed-earlier:b:remaining-duration-not-compared' rendered 'active on b now, but an earlier casting in this routine removes it (not skipped, not cast)'" - the R3-F4 defect, observed directly; removal-only: "Ready" (R3-F1, as in round 4) |

For the `1bfc198` run the test helper's `isFixedFact` argument (an API added in
round 4) was removed temporarily so the tests compile; it was restored
afterwards. The R5-F5 Classic assertion was split into its own test before the
recorded run so that it is not masked by the one-pass assertion.

Corrected results, final working tree before commit (DLL `6c04525d…`):
`missing-beneficial-buffs` 43/0 (5.4 s); complete program 792/0 (35.5 s);
explicit capture 1/0 (fixture subset unchanged, 48 / 58); explicit verify +
candidate audit 3/0 (48 identical files, 58 types, Dance identical; 1,722 / 16
and 10,443 / 48; classifier
`ce8fadbdd47057367f17c7ca12e3e8b1035eb2fa5db3e53fe9ba41695b44328b`, overrides
`c462aeb4…`, reports `b5b1a6d1…0d6d` / `0ba8cc7b…209b`, changed rows identical
apart from the header label).

Limits: the sequential narrowing applies to group castings; a direct-target
casting keeps its explicit target (the removal-conflict guard protects it
inside a routine). A mixed caster/recipient checker is never evaluated, so
such a spell gets no coverage even when it would apply. Native behavior that
stays unverified: that `ContextConditionHasBuff` is matched by the live
`Descriptor.HasFact` read used for recipient facts (an unread unit gets no
coverage claim).

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
