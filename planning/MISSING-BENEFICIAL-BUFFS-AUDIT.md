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
| full-user | `kbp041-rel-catalog-full` (`85e7410`) | 10,443 | 48 (38 since review round 7, see there) | `2cdd2a56f9c7e5cc91b6ca6e6807cbcd078d797e0965edd33f64f68823aef7a5` |

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
coverage claim). (Superseded in review round 6: the buff predicate now uses
its exact native read, assembly-backed.)

## Review round 6

Codex review of `0f253aa` (decision `changes_required`). Verified: R4-F6,
R5-F3, R5-F4. Re-opened: R5-F1, R5-F2, R5-F5, R5-F6. New: R6-F1 to R6-F3. All
are fixed in the round-6 corrections commit (`3b24210`).

**Assembly-backed observations** (no game launch): the test
`…-native-condition-contracts-pinned` decodes the IL of the referenced 2.1.7b
`Assembly-CSharp` and asserts:

| Native member | Observed check |
|---|---|
| `ContextConditionHasBuff.CheckCondition` | `Target.Unit.Descriptor.Buffs` → `FactCollection.HasFact(Buff)` |
| `ContextConditionHasFact.CheckCondition` | `UnitHelper.HasFact(Target.Unit.Descriptor, Fact)` |
| `ContextConditionIsEnemy` / `IsAlly.CheckCondition` | `Target.Unit.IsEnemy` / `IsAlly(Context.MaybeCaster)` |
| `ConditionsChecker.Check` | `if (!HasConditions) return true;` first |

and that the planner's live read `NativeRecipientFactRead.Read` (new, beside
the adapter; the option builder calls it) makes exactly those calls.

| Finding | Correction | Regression |
|---|---|---|
| R5-F1 penalty-only buff in a point-capable burst | The exporter records `modifierSigns` (per component with a native `ModifierDescriptor`: Penalty descriptor or constant negative value = penalty; constant positive = bonus; otherwise unknown). A buff whose every mechanic is a penalty modifier (bookkeeping and do-nothing actions aside) harms its bearer; point admission refuses it. Enlarge/Reduce (size change plus a +2) stay mixed | `…-point-burst-penalty-buff-refused` (Feather Step, Mass repointed to the committed Crushing Despair → refused by discovery and admission; Feather Step, Enlarge and Reduce Mass admitted) |
| R5-F2 automatic penalty under the waiver | The waiver requires every automatic application of an approved buff to be an approved payload itself (a readable penalty included) | `…-payload-activation-penalty-refuses-ally-exception` (GoodHopeBuff activation applies Crushing Despair; exporter records it; discovery and admission refuse; Good Hope and Firebelly admitted) |
| R5-F5 empty projection kept beneficiaries | The projected (empty) list is stored before the refusal | `…-sequential-forecast-empty-coverage-clears-beneficiaries` (fully overlapping disks: Reduce `[]`, `predicted-coverage-empty:b`, zero cost); the partial-overlap tests still pass |
| R5-F6 initially exhausted pool kept the promise | A row that arrives already refused (Blocked / Disabled / Draft) gets the not-running notes | `…-exhausted-mixed-casting-notes-do-not-promise-a-cast` (zero slots: `resource-pool-exhausted`, zero cost, "still active (this casting is blocked and does not run)") |
| R6-F1 Prayer lost its friendly recipients | `ContextConditionIsEnemy` / `ContextConditionIsAlly` are modelled recipient gates (relation keys), read live with the native `IsEnemy` / `IsAlly(caster)`; relation keys are fixed for conflict analysis. A checker mixing caster and recipient predicates stays unmodelled (R5-F3) | `…-prayer-friendly-recipients-keep-coverage` (native Prayer from the committed fixtures: one funded cast over caster and ally; the ally's bound branch applies PrayerBuff, never PrayerDebuff) |
| R6-F2 Good Hope's buff lookup unverified | `ContextConditionHasBuff` keys are `buff-predicate:<guid>` and are read with `descriptor.Buffs.HasFact(buff)`, the observed native check; an unreadable unit, buff or caster is unknown (no coverage claim) | the contract test above, and `…-good-hope-despair-read-is-the-native-buff-check` (near ally's Despair present / absent / unreadable: the bard still receives Good Hope, one slot; the near ally only when Despair is known absent) |
| R6-F3 unreachable alternative supplied coverage | An empty native checker establishes coverage only through its true branch (observed IL); an unknown checker (absent or unmodelled) only with a definite leaf on both branches | `…-empty-checker-follows-the-branch-that-runs` (application on the true branch: covered, one slot; only on the false branch: no recipients, Blocked, zero cost) |

**Executed reproductions** on the production of `0f253aa` (seven files swapped
by `git show 0f253aa:<path>`, current tests and the 51/61 fixtures; the
contract test and one audit-reader line, which use APIs new in this round,
were stubbed for that run; everything restored from `3b24210`):
`missing-beneficial-buffs` 43/8 (DLL `cbfea135…`). Failures: contract (stub);
penalty burst "admitted: valid-beneficial-party-effect…" (R5-F1); activation
penalty "Good Hope was admitted although its buff applies Crushing Despair to
its bearer" (R5-F2); "Blocked [a,b] predicted-coverage-empty:b" (R5-F5);
"already active on ally-near (the cast goes ahead for the others)" (R5-F6);
Prayer "Blocked [] predicted-coverage-empty:caster" (R6-F1); "Despair check is
not keyed as the native buff predicate: 36897b36…" (R6-F2); "unreachable
branch supplied coverage: Ready [ally,caster]" (R6-F3).

Corrected results (final tree, DLL `687b9b0b…`): `missing-beneficial-buffs`
51/0; complete program 800/0 (40.9 s); explicit capture 1/0 (fixture subset
now 51 blueprints / 61 referenced types: Prayer, PrayerBuff, PrayerDebuff
added from the lab dump); explicit verify + candidate audit 3/0 (51 identical
files, 61 types, Dance identical; 1,722 / 16 and 10,443 / 48; classifier
`a3b7bf88c2482cd3472e04a0caf5cc5fa6f72ed2d9aa9869d9de0d50957e6497`, overrides
`c462aeb4…`, reports `5c343b98…a43e` / `59747898…338e35`, changed rows identical
apart from the header label).

Limits: the 0.4.0/0.4.1 exports predate `modifierSigns`, so the candidate
audit cannot show the penalty rule (a fresh guarded export would). A modifier
with a context-dependent value under a non-Penalty descriptor is "unknown" and
never counts as a penalty. Native behavior that stays unverified in play:
`IsEnemy` / `IsAlly` for every party member (a charmed or otherwise hostile
member would read as an enemy and get no coverage claim); owner check added to
[MANUAL-ACCEPTANCE.md](../docs/MANUAL-ACCEPTANCE.md).

## Review round 7

Codex review of `bead27a` (decision `changes_required`). Verified: R5-F1,
R5-F2, R5-F5, R5-F6, R6-F1, R6-F2, R6-F3. New: R7-F1 to R7-F6, all fixed in the
round-7 corrections commit (`a9bac29`).

| Finding | Correction | Regression |
|---|---|---|
| R7-F1 kill action passed both exceptions | Both narrow exceptions (unit-anchored allied bursts, the ally-disposition waiver) refuse any reachable offensive action and any gameplay action the adapter could not classify (an `unknown-node`, e.g. native `ContextActionKillTarget`, on any branch, or an unrecognized action in an applied buff's own lists); presentation (`ContextActionSpawnFx`) stays a harmless adjunct | `…-kill-action-refuses-both-exceptions` (Mass Blessing construct branch and Good Hope Despair branch with `ContextActionKillTarget` → refused by discovery and admission; unmodified spells admitted) |
| R7-F2 penalty rescued by Personal range | When Personal range rescues an enemy-flagged ability (§3.B), a current-target payload that harms its bearer (penalty-only, harmful condition, or applying one) is still refused | `…-personal-penalty-payload-refused` (native Resistance — Fire repointed to the committed Crushing Despair → refused by discovery and fact-provider admission; the five real selections admitted) |
| R7-F3 reselection after levelling spent the daily use | The adapter records a buff's `CasterLevelStrength` when its only caster-level dependence is one native `ContextRankConfig` (CasterLevel base, Custom progression) and every other value is constant or that rank; sufficiency then compares the progression value strictly below the last breakpoint (at or above it - wizard-level-20 immunity - the plain caster-level comparison stays). Every other effect keeps the caster-level comparison | `…-resistance-reselect-in-tier-is-free` (level-7 wizard, one use, permanent Fire from level 6: AlreadySatisfied, zero cost; Cold: Ready, one use. The modelled values equal the native `ContextRankConfig.ApplyProgression` for levels 1-19, invoked on the detached native component; 6/7 sufficient, 10/11 and 19/20 insufficient, 12/19 sufficient) |
| R7-F4 Prayer's penalty satisfied a friend | Existing-effect sufficiency and mixed-coverage notes bind each recipient's fixed facts (`BindForRecipient`, the caster relation included) | `…-prayer-penalty-never-covers-a-friendly-ally` (PrayerBuff on the caster, only PrayerDebuff on the ally: Ready, one slot, no already-covered note for the ally; PrayerBuff on both: zero-cost skip) |
| R7-F5 unreachable removal blocked a cast | Change analysis follows only the true branch of an empty native checker (applications, guaranteed applications and removals) | `…-empty-checker-unreachable-removal-is-no-conflict` (Mass Blessing funded, then Feather Step with its buff on the empty checker's true branch and a removal of Mass Blessing's buff on the false branch: Ready, one slot; control with the removal on the true branch: refused naming the planned Mass Blessing) |
| R7-F6 later Good Hope could not gain allies | `SequentialRecipientState`: options record every live read of their recipient-fact gates; each funded casting's changes, bound under the state so far, update it (certain application → present, certain removal → absent, possible → unknown unless already agreeing; `EffectChanges.CertainlyRemoved` added). A later routine's group beneficiaries are recomputed from it - narrowed or widened - in the one-pass compiler (a casting with an empty burst is kept, not dropped, so it can be re-evaluated) and the Classic forecast; unread or unknown conditions withhold the claim; independent previews unchanged | `…-sequential-good-hope-gains-allies` (allies at 0/10/20 ft, Despair on the latter two; Good Hope at 10 ft then at 20 ft: one slot each, the second benefits the allies at 10 and 20; the second routine previewed alone: empty, refused) and `…-classic-sequential-good-hope-gains-allies` |

**Executed reproductions** on the production of `bead27a` (eleven files
swapped by `git show bead27a:<path>`, current tests and the 51/61 fixtures; the
R7-F3 test's checks of the new strength model were stubbed for that run;
everything restored from `a9bac29`): `missing-beneficial-buffs` 51/7 (DLL
`849962ea…`). Failures: kill action "admitted: valid-beneficial-party-effect…"
(R7-F1); Personal penalty "admitted: valid-beneficial-self-effect…" (R7-F2);
"Reselecting Fire from level 6 at level 7: Ready" (R7-F3); "AlreadySatisfied
already-active:ally" (R7-F4); "Blocked removes-planned-effect:bless:…:ally"
(R7-F5); one-pass "Blocked [] predicted-coverage-empty:f" and Classic "no step
/ f=Unfulfilled,m=Unfulfilled" (R7-F6).

Corrected results (final tree): `missing-beneficial-buffs` 58/0; complete
program 807/0 (35.6 s); explicit capture 1/0 (fixtures unchanged, 51 / 61);
explicit verify + candidate audit 3/0 (51 identical files, 61 types, Dance
identical; classifier
`3b11c7871d3d179e1960a709e91080f2673008b2cdf9626d40710ff777066ebf`, overrides
`c462aeb4…`, reports `82a83ffd…73d2` / `decdd0f7…c690`). Native-only 1,722 / 16
changed, rows identical apart from the header label. **Full-user 10,443 / 38**
(48 before): the R7-F1 rule refuses the ten Enlarge / Reduce Person, Mass rows
(native and Call of the Wild variants) because under Call of the Wild their
buffs run `CallOfTheWild.NewMechanics.ContextActionRemoveBuffs` on activation,
an action the adapter does not classify; the other 38 rows are unchanged. The
four mission group spells remain admitted in both scopes; Enlarge/Reduce were
audit-found extras and stay admitted for an unmodded 2.1.7b (native-only
export, detached fixtures).

Limits: an unclassified mod action is refused rather than modelled; the
`CasterLevelStrength` rule covers only a single Custom CasterLevel progression
and falls back to the caster-level comparison at or above its last
breakpoint; the sequential state starts from the reads an option made (units
outside every burst, or keys never read, stay unknown).

## Review round 8

Codex review of `6c4d9af` (decision `changes_required`). Verified: R7-F2 to
R7-F6. Re-opened: R7-F1. New: R8-F1 to R8-F6. All are fixed in the round-8
corrections commit (`2abe58c`).

| Finding | Correction | Regression |
|---|---|---|
| R7-F1 incomplete scan authorized a cast | Both narrow exceptions refuse any scan diagnostic other than a recognized restoration or a presentation-only node (a traversal stopped at `maximum-depth`, a `cycle`, an unknown node), and an automatically applied buff whose own automatic actions the export did not read (an applied buff carrying `AddFactContextActions`: an uninspected chain) | `…-incomplete-scan-refuses-both-exceptions` (Good Hope with damage nested 70 empty conditionals deep on its Despair branch: `maximum-depth`, refused; Feather Step, Mass whose buff activates a second buff that damages its bearer: refused; unmodified spells admitted). The direct kill-action controls still pass |
| R8-F1 another wizard's Firebrand satisfied a casting | Live instances carry their applying caster (`ActiveEffectInstance.CasterUnitId`, from the buff context's `MaybeCaster`). A leaf whose buff has a native caster-relative component (`AddImmunityFirebrand`; its IL compares the damage initiator with its own buff's `MaybeCaster`, pinned in the contract test) is `CasterRelative`: sufficiency needs the planned caster's instance (unread origin proves nothing); its one-pass structural grant key includes the caster; a Classic skip needs instance detail from that provider's caster | `…-firebrand-needs-its-own-caster` (wizard a's existing Firebrand: b's cast Ready, one slot; b's own: zero-cost skip; a funded earlier Firebrand from a: b Ready, one slot; a's own repeat: structural skip) |
| R8-F2 a resistance leaf carried a harmful action | When Personal range rescues an enemy-flagged ability, the whole cast is judged: any application harming its bearer, or any unsafe or unread action, excludes it | `…-personal-rescue-judges-the-whole-cast` (Resistance — Fire keeping its resistance and adding Crushing Despair, or a `ContextActionKillTarget`: refused by discovery and fact-provider admission; five real selections admitted) |
| R8-F3 no same-routine coverage gain | The sequential recipient state also exposes the routine's own earlier funded castings, in every view (independent previews reset per routine, one-pass sequences carry across); an empty burst is kept for re-evaluation in every view; a cast that would undo an earlier requested effect is checked for its removal conflict first | `…-same-routine-good-hope-gains-allies` (one routine, two slots: two funded casts, the second for the allies at 10 and 20 feet, in the plain compiler, the selected-run compiler (`budgetRoutineScope`) and the one-pass forecast; first disabled: second empty) |
| R8-F4 Classic counted Prayer's penalty | `CastPlanner` resolves the applicable providers first and grants a skip only when every applicable provider's bound branch for the target is present | `…-classic-prayer-follows-the-friendly-branch` (RoutinePlanService, pinned caster: PrayerDebuff on the ally → one step; PrayerBuff on both → free skip) |
| R8-F5 unreachable buff satisfied presence | `EffectPresenceEvaluator` (presence and confirmation) follows only an empty native checker's true branch; unknown conditions keep both alternatives | `…-empty-checker-presence-follows-the-true-branch` (only the false-branch buff active: one funded Feather Step; a fresh observation of only that buff does not confirm delivery) |
| R8-F6 unreadable anchor inherited coverage | An area centre whose own position cannot be read is not a legal anchor | `…-unreadable-anchor-inherits-nothing` (two CanTarget-approved centres, one unreadable: no cast and no reservation there; the readable centre casts once for its own recipients) |

**Executed reproductions** on the production of `6c4d9af` (twelve files
swapped by `git show 6c4d9af:<path>`, current tests and the 51/61 fixtures;
the Firebrand test's caster-relative metadata check and instance origins were
stubbed for that run; everything restored from `2abe58c`):
`missing-beneficial-buffs` 58/7 (DLL `d97dd442…`). Failures: incomplete scan
"Good Hope with an unread action beyond the scan bound was admitted" (R7-F1);
"Wizard a's Firebrand satisfied wizard b's casting: AlreadySatisfied" (R8-F1);
"Resistance - Fire with Crushing Despair beside the resistance was admitted"
(R8-F2); "compiler: … Blocked [] predicted-coverage-empty:f" (R8-F3);
"ally=SkippedAlreadyActive,caster=SkippedAlreadyActive" (R8-F4); "An
unreachable false-branch buff satisfied the empty-checker cast:
AlreadySatisfied" (R8-F5); "An unreadable centre inherited coverage: Ready
[ally,caster,near]" (R8-F6). Masked in that run: the point-chain case of
R7-F1 and the kill-action variant of R8-F2 (each asserted after its first,
failing, case).

Corrected results (final tree): `missing-beneficial-buffs` 65/0; complete
program 814/0 (40.2 s); explicit capture 1/0 (fixtures unchanged, 51 / 61);
explicit verify + candidate audit 3/0 (51 identical files, 61 types, Dance
identical; native-only 1,722 / 16 and full-user 10,443 / 38, rows identical
apart from the header label; classifier
`b29c098f96a0f6c15ffc24781d6a5981025c1b9e53299df025a69278dbdc4ff5`, overrides
`c462aeb4…`, reports `559a2bc5…46ef` / `b3069d9a…0aaf`).

Limits: caster-relative benefit is recognized only for the native components
listed (`AddImmunityFirebrand`); a projected Classic marker carries no origin,
so a caster-relative repeat in a Classic forecast casts again
(conservative). Native behavior that stays unverified: the live
`MaybeCaster` read of an existing buff's origin.

## Review round 9

Codex review of `4cda586` (decision `changes_required`). Verified: R8-F5.
Re-opened: R7-F1, R8-F1, R8-F2, R8-F3, R8-F4, R8-F6. New: R9-F1 to R9-F6. The
code findings are fixed in the round-9 corrections commit (`9b7dc1c`); R9-F6
(the developer report) is corrected in the round-9 handoff.

| Finding | Correction | Regression |
|---|---|---|
| R7-F1 an unread buff blueprint passed the point-target check | A buff record the exporter could not read (no harmful flag) refuses the whole-cast exceptions (unit-anchored bursts, ally-disposition waiver, Personal rescue), whether the cast applies it directly or an applied buff applies it on activation | `…-unread-buff-refuses-point-admission` (Feather Step, Mass with its direct buff withheld from `NativeBlueprintLookup`; with only the buff its buff applies on activation withheld: both refused by discovery and provider admission; resolved spell admitted) |
| R8-F2 a caster-directed resistance bypassed whole-cast safety | The Personal-rescue whole-cast check (and the Personal penalty refusal) also covers an application explicitly directed to the caster (`ToCaster`) | `…-caster-directed-rescue-judges-the-whole-cast` (Resistance — Fire with `ToCaster` set and the native `ContextActionKillTarget`: refused by discovery and fact-provider admission; `ToCaster` alone admitted; the five real selections admitted, each removing the other four resistances) |
| R8-F1 Firebrand forecast assumed both casters' protection survives | Whether a caster-relative buff from caster A stays effective after caster B applies the same buff is not established offline, so a funded caster-relative application by another caster invalidates the earlier casters' structural grants of that effect on every unit it may reach; the Classic sequential forecast likewise replaces other casters' instances with the applying caster's | `…-firebrand-other-caster-overlap-invalidates-grant` (one-pass a, b, a over the same allies: a's repeat Ready for one slot; disjoint bursts and the same overlap with b's casting disabled keep a's free structural repeat) |
| R8-F3 an initially satisfied Good Hope ignored a newly eligible ally | The budget pass re-assesses an initially satisfied group casting (recipients, existing-effect verdict, notes, readiness) when earlier funded castings of the sequence change its beneficiaries; the Classic planner keeps a routine recipient state and recomputes later assignments' group coverage from it | `…-satisfied-good-hope-gains-a-newly-eligible-ally` (allies at 0/10/20 ft, the middle under Despair, the far with a sufficient Good Hope, two slots: plain and selected-run compilers fund the second cast for the middle ally; first disabled: free skip; a later Classic assignment for the middle ally is planned) |
| R8-F4 a Classic Prayer skip protected the enemy penalty | A Classic skip relies on and protects only each applicable provider's bound branch; an unread relation (a target outside every measured disk) or a hostile one withholds a relation-dependent skip | `…-classic-prayer-skip-protects-its-branch` (after a friendly Prayer skip, a Feather Step, Mass that also removes PrayerDebuff is funded and one removing PrayerBuff is refused; a far friendly target with only PrayerDebuff and no recorded relation is not skipped) |
| R8-F6 a failed second geometry read left an anchor without coverage | Legal area anchors are exactly the centres whose coverage was built, from one self read each; a failed or throwing live distance read is unavailable for that pair only (planner and production delegate) | `…-failed-geometry-read-keeps-anchors-separate` (a centre readable once then unavailable inherits nothing; a centre whose reads throw is no anchor, zero cost, no step; the readable centre keeps its own disk) |
| R9-F1 a repeated resistance selection created a false shortage | A same-routine repeat of a payload whose every application is a permanent native application (`ContextActionApplyBuff.Permanent`, new leaf metadata) is structurally satisfied in every view; a timed effect's repeat keeps the existing contract (charged outside the one-pass view, which reports it redundant) | `…-repeated-resistance-selection-costs-one-use` (one daily use, Fire then Fire: one paid cast, one executable step, one reserved use, a free second row, in the selected-run and plain compilers; Fire then Cold keeps its removal conflict) |
| R9-F2 another caster's Firebrand confirmed the submitted cast | Execution observations carry the applying caster (buff context `MaybeCaster`); a caster-relative leaf is confirmed only by an instance of the submitted caster, never by an unread origin; caster-less confirmation never proves one | `…-firebrand-confirmation-needs-the-submitted-caster` (b's group step: fresh Firebrand from a or of unread origin does not confirm; b's own does) |
| R9-F3 Prayer's hostile branch counted as beneficial coverage | A unit the relation predicate reads as hostile to the caster is affected, never a beneficiary (area planner and sequential state); a group step's confirmation expects only the friendly branch | `…-prayer-hostile-member-is-no-beneficiary` (unmodified Prayer, an in-radius member with IsEnemy true: affected, not predicted; a fresh PrayerDebuff alone does not confirm, PrayerBuff does) |
| R9-F4 an unreachable predicate suppressed coverage | Recipient binding and caster-gate inspection ignore a verified empty checker's never-run false branch (the expression is kept whole) | `…-empty-checker-unreachable-predicate-keeps-coverage` (unread HasFact only on the false branch: recipients kept, one slot; the same predicate gating the reachable application still fails closed) |
| R9-F5 Classic Firebrand skips and forecasts discarded instance state | A Classic caster-relative skip needs a readable, unsuppressed instance of that caster; the Classic sequential forecast carries live instance detail until an admitted step changes it | `…-classic-firebrand-uses-effective-instances` (b pinned: a's active plus b's suppressed instance → one funded cast; b's effective instance → free skip, also in the first sequential-forecast occurrence) |
| R9-F6 the checkpoint report misattributed validation | Corrected in the round-9 handoff (commands with their `-Only` selectors, commit identities, changed-disposition counts, evidence README path, one regression per disposition) | — |

**Executed reproductions** on the production of `4cda586` (fifteen files
swapped by `git show 4cda586:<path>`, current tests and the 51/61 fixtures; the
Firebrand confirmation test's instance origin argument was stubbed for that
run; everything restored from `9b7dc1c` and the tree verified clean):
`missing-beneficial-buffs` 65/11 (DLL `8d9b5734…`). Every subcase runs and
reports independently (no masking). Failures: R7-F1 both subcases ("…direct
buff could not be read was admitted", "…applies an unread buff was
admitted"); R8-F2 the kill subcase; R8-F1 "a's Firebrand stayed free …
AlreadySatisfied already-satisfied-structural"; R8-F3 compiler, selected-run
("AlreadySatisfied [f] already-active:f") and Classic ("later-middle:m=Unfulfilled");
R8-F4 the penalty-removal and unread-relation subcases; R8-F6 both subcases
(inherited "[ally,caster,near]"; the throwing read escaped as "position
unreadable"); R9-F1 both views ("Blocked resource-pool-exhausted…0<1");
R9-F2 the other-caster and unread-origin subcases; R9-F3 prediction
("[ally,caster,turned]") and confirmation; R9-F4 the never-run-branch
subcase ("Blocked [] predicted-coverage-empty:caster"); R9-F5 the suppressed
and sequential subcases. Every control subcase passed on the baseline.

The first complete run after the R9-F1 correction failed 822/3: a general
same-routine structural grant changed three established contracts (an
identical parallel timed casting is charged and reported redundant only in
one pass; strict admission; a five-cast item routine). The rule was narrowed
to permanent applications (above), not the contracts; then 825/0.

Corrected results (committed source `9b7dc1c`): `missing-beneficial-buffs`
76/0; complete program 825/0 (35.1 s); explicit capture 1/0 (fixtures
unchanged, 51 / 61); explicit verify + candidate audit 3/0 (51 identical
files, 61 types, Dance identical; native-only 1,722 / 16 and full-user
10,443 / 38 changed dispositions, rows identical apart from the header label;
classifier `7ce036b7f8529fa4c0b63cfc4f035a53a530aca78299f763a914f3f121346f27`,
overrides `c462aeb4…`, full reports `803ad0e3…6be7` / `89a1ffe0…84bd`, tables
`69de998e…3605` / `a5ac1b61…3a89`).

Limits: the cross-caster rule is conservative (a coexisting instance would be
an unneeded recast, never a false skip); the live existing-effect assessment
of a skip is not re-checked against another caster's same-routine
application. The applying caster of an observed instance and the native
relation reads remain unverified in game (owner acceptance).

## Review round 10

Codex review of `6d6acc3` (decision `changes_required`). Verified: R7-F1,
R8-F4, R9-F2, R9-F6. Re-opened: R8-F1, R8-F2, R8-F3, R8-F6, R9-F1, R9-F3,
R9-F4, R9-F5. New: R10-F1 to R10-F4. All are fixed in the round-10
corrections commit (`3df014f`).

| Finding | Correction | Regression |
|---|---|---|
| R10-F1 Ally-only bursts counted members the selector excludes | `AlliedAreaCoverage` carries the native `AbilityTargetsAround` selector type; for `TargetType.Ally` a unit read as not allied to the caster is not reached, and one with an unread relation is affected but never a beneficiary; the option keeps the restriction (`RequiresAllyRelation`) for recomputed coverage. Prayer's Any selector is unchanged. Test fact readers that call the planner directly now answer the relation as the production reader does (friendly party members) | `…-ally-selector-honours-the-caster-relation` (unmodified Feather Step, Mass, one legal centre: affected {caster, friend, unread}, predicted {caster, friend}, one slot; recomputed coverage keeps the restriction; Prayer's selector read as Any) |
| R10-F2 a friend-only weapon delivery passed the point-target exception | An offensive ability-level delivery component (weapon attack, projectile, chain, bomb; `Weapon` range) refuses the whole-cast exceptions | `…-weapon-delivery-refuses-point-admission` (Feather Step, Mass plus native `AbilityDeliverAttackWithWeapon`: refused by discovery and provider admission; original admitted) |
| R10-F3 Firebelly coverage ended before the catalogue | Test only | `…-firebelly-reaches-the-castable-catalogue` (Firebelly via production spell admission, targeting and `PlannerSetupModel`: one caster-only entry; the granted breath through fact admission: no provider, no entry) |
| R10-F4 Classic treated a suppressed Good Hope as coverage | With instance detail, a Classic skip needs a readable, unsuppressed instance of an ordinary buff too | `…-classic-suppressed-good-hope-is-no-coverage` (suppressed, unread suppression: no skip; effective: skip) |
| R8-F1 a live Firebrand skip survived another caster's application | The budget pass keeps the standing live evidence: a funded caster-relative application voids other casters' live instances of that effect on the units it reaches, and a skip resting on them is re-assessed (readiness, notes, cost, step); the Classic routine withholds such a skip | `…-firebrand-live-skip-after-other-caster-recasts` (selected run, a's Firebrand live on the disk, b funded over it, then a: paid, two executable steps; b disabled or a disjoint disk: a's free skip) |
| R8-F2 neutral enemy disposition bypassed Personal whole-cast safety | The whole-cast check covers every Personal ability the hostile rules would refuse: harmful enemy disposition, or enemy-capable without friend targeting | `…-personal-neutral-disposition-rescue-is-whole-cast` (caster-directed Resistance — Fire with `EffectOnEnemy = None`, `CanTargetFriends = false` and the native kill action: refused by the frozen baseline, discovery and fact admission; without the kill admitted) |
| R8-F3 a later Classic routine lost a newly eligible ally | A re-evaluated option carries the sequential state as its recipient fact reads, so a routine's own state starts from what earlier occurrences changed | `…-classic-sequence-keeps-inherited-coverage` (first routine clears Despair; second funds Haste, then Good Hope keeps the allies and reserves one slot) |
| R9-F1 Classic charged twice for the same permanent resistance | A Classic repeat of an all-permanent payload that an earlier step of the routine certainly left on the unit (per-routine recipient state) is skipped | `…-classic-repeated-resistance-costs-one-use` (two saved Fire assignments, one use: one step, one reservation, the second skipped; Fire then Cold refused as a removal) |
| R9-F3 Classic Prayer confirmed from its penalty | A Classic mass step's expected effects are the friendly relation branch (confirmation); the sequence forecast analyzes the complete expression of the step's source; the owner checklist distinguishes friendly and hostile members | `…-classic-prayer-confirmation-needs-the-friendly-buff` (RoutinePlanService step: PrayerDebuff alone does not confirm, PrayerBuff does) |
| R9-F4 an unreachable relation predicate vetoed a benefit | Relation checks (beneficiary prediction, Classic skip) use only reachable predicates (`ReachableFactIds`) | `…-empty-checker-unreachable-relation-keeps-coverage` (Prayer rebuilt with an empty checker: a hostile-read member still predicted; an unread-relation Classic target with an effective buff skipped) |
| R9-F5 Classic carried stale Firebrand instances | The Classic forecast projects leaves through a group step's allied-area (or party) wrapper onto its recipients, so an overlapping Firebrand replaces other casters' carried instances | `…-classic-sequence-firebrand-replaces-other-caster` (a's live instances, b funded in the first routine, a requested in the second: paid; disjoint: free) |
| R8-F6 a non-finite self-distance admitted an anchor | A non-finite or negative reading is unusable geometry | `…-failed-geometry-read-keeps-anchors-separate` extended (NaN and +∞ self-distance: zero cost, no step; the finite centre keeps its disk) |

**Executed reproductions** on the production of `6d6acc3` (eight files
swapped by `git show 6d6acc3:<path>`, current tests and the 51/61 fixtures;
the two `RequiresAlly` reads in the R10-F1 test were stubbed for that run, so
its "selector contract" subcase failed by construction; everything restored
from `3df014f` and the tree verified clean): `missing-beneficial-buffs` 76/11
(DLL `ecbe6cae…`). Every new regression failed on its target subcases except
`…-firebelly-reaches-the-castable-catalogue`, which passes on the baseline
(R10-F3 was an evidence gap, not a defect). Failures included: the
NaN/+∞ centres "Ready [flaky]"; the Ally burst "affected
[caster,friend,hostile,unread] predicted [caster,friend,hostile,unread]";
the weapon delivery admitted; suppressed and unread-suppression Good Hope
skipped; "a's live Firebrand skip survived b's overlapping cast:
AlreadySatisfied"; the neutral-disposition kill variant admitted; the
inherited Good Hope "f=Unfulfilled, m=Unfulfilled"; the repeated Classic Fire
"z-fire-again … no-valid-provider-or-resource"; Classic PrayerDebuff
confirmation; the unreachable relation "[caster]" and "far=Unfulfilled"; the
Classic Firebrand "a=SkippedAlreadyActive". Every control subcase passed.

Corrected results (committed source `3df014f`): `missing-beneficial-buffs`
87/0; complete program 836/0 (39.5 s); explicit capture 1/0 (fixtures
unchanged, 51 / 61); explicit verify + candidate audit 3/0 (51 identical
files, 61 types, Dance identical; native-only 16 of 1,722 and full-user 38
of 10,443 changed dispositions, rows identical apart from the header label;
classifier `7eb7fcf09bcc28bc26d5f460b1937adcf5d1edb01857369a513499e8654e433d`,
full reports `e86d93ce…0601` / `f6e8d8b6…1df2`, tables `8f126085…0fa8` /
`1d569b25…86bc`).

Limits: the Firebelly regression uses the spontaneous-level pool shape for
the cleric book (slot accounting is covered elsewhere); the native relation
read (`IsAlly` / `IsEnemy`), the applying caster of a buff, and cross-caster
Firebrand coexistence stay unverified in game (owner acceptance), and every
planner decision that depends on them fails closed.

## Review round 11

Codex review of `de20c98` (decision `changes_required`). Verified: R8-F2,
R8-F3, R8-F6, R9-F1, R9-F3, R10-F2, R10-F4. Re-opened: R10-F1, R10-F3, R8-F1,
R9-F4, R9-F5. New: R11-F1 to R11-F5. All are fixed in the round-11
corrections commit (`c64a34f`).

| Finding | Correction | Regression |
|---|---|---|
| R10-F1 an unread ally relation became definite after a funded cast | A unit an Ally-only selector may skip (relation not read as allied) gets only possible changes in the sequential recipient state (nothing definite); `EffectiveTargeting.WithStrategy` keeps `RequiresAllyRelation` | `…-unread-ally-relation-never-becomes-definite` (two bards; m under Despair, unread for A and allied for B: after A's funded cast B does not promise m; a strategy-changing copy (native-command rod) keeps the restriction when recomputed) |
| R10-F3 the Firebelly test bypassed prepared slots | Test: the native prepared cleric spellbook (`4673d19a…`, captured into the fixtures) carries Firebelly at level 1; one memorized slot; the cast reserves that token; the breath's single trace is the discovery refusal (no beneficial trace, no effect entry) | `…-firebelly-reaches-the-castable-catalogue` (rewritten) |
| R8-F1 Firebrand reassessment used the wrong units | Caster-relative invalidation uses each leaf's landing units (`EffectApplicationLifetime.CasterRelativeRoles`: the caster only for a caster-role leaf, otherwise the direct target or the selected disk); a Ready row resting on the same evidence is re-assessed too, refreshing its pre-covered credit and notes | `…-firebrand-reassessment-uses-landing-units` (b's remote ally anchor excludes b: a's skip on b stays free; a partially covered a row drops the pre-covered credit b's overlap may have replaced) |
| R9-F4 a known outer condition left an unreachable relation veto | Relation eligibility follows the recipient's selected path (`HostileOnPath`: read non-relation gates select their branch first) in prediction, recomputation and Classic skips | `…-known-outer-condition-makes-relation-unreachable` (Prayer rebuilt with a fixed creature-fact outer condition: the hostile-read member with the fact keeps coverage, recomputed coverage, and a Classic skip) |
| R9-F5 Classic kept another caster's Firebrand on a possibly reached unit | The Classic forecast voids other casters' carried caster-relative instances on every unit a step may reach, independently of projecting the step's benefit onto confirmed recipients | `…-classic-firebrand-possible-replacement-invalidates` (m's relation to b unread: a's next-routine request for m is paid; disjoint b disk: free) |
| R11-F1 an independent on-cast action bypassed whole-cast safety | The shared facts report a non-trivial `AbilityExecuteActionOnCast` list (anything beyond presentation, or unreadable) as an `on-cast-action` diagnostic, which refuses the whole-cast exceptions; the discovered graph is unchanged | `…-on-cast-action-refuses-point-admission` (Feather Step, Mass plus a damaging on-cast list: refused by discovery and provider admission; original admitted) |
| R11-F2 Classic spent two Good Hope slots where one centre covered both | Classic ranks legal, conflict-free mass centres by outstanding requested coverage; being requested only breaks ties | `…-classic-mass-centre-covers-most-requested` (allies at 0 and 20 ft, one slot: the 10-ft ally is the centre, one step, both fulfilled) |
| R11-F3 a saved Prayer forecast applied its friendly branch to hostile members | The sequence forecast resolves the complete expression by the step's provider ability (`DiscoveredEffectLookup.ForAbility`), so a saved aggregate source id no longer falls back to the confirmation expression | `…-saved-prayer-forecast-uses-complete-expression` (catalogue-saved Prayer, canonical-keyed effects, a hostile-branch member; a later PrayerBuff-gated Feather Step does not reach it) |
| R11-F4 Firebelly planning bypassed the live caster and owner verdict | A personal cast's owner is a legal anchor only with a readable live caster and native `CanTarget` admitting it | `…-personal-cast-needs-live-owner-verdict` (unreadable caster, refused owner: no anchor, no reservation, no step; accepted owner: one slot) |
| R11-F5 area coverage accepted distances beyond the recorded radius | Coverage uses the recorded radius exactly (no margin without native evidence) and needs a finite, positive radius | `…-area-boundary-is-the-recorded-radius` (4.57 m in, 4.575 m out of the 4.572 m disk; infinite and NaN radius: no centre, no reservation) |

**Executed reproductions** on the production of `de20c98` (eight files
swapped by `git show de20c98:<path>`, current tests and the 52/62 fixtures;
the new `OnCastActionCode` constant was replaced by its literal for that run;
everything restored from `c64a34f` and the tree verified clean):
`missing-beneficial-buffs` 87/9 (DLL `14addbf7…`). All nine new regressions
failed on their target subcases (the prepared Firebelly test passes on the
baseline: R10-F3 is an evidence gap); the on-cast subcase failed at its first
assertion (the diagnostic), so its admission assertion was not reached there.
Every control subcase passed.

Corrected results (committed source `c64a34f`): `missing-beneficial-buffs`
96/0; complete program 845/0 (37.2 s); explicit capture 1/0 (fixtures 52 /
62: the cleric spellbook added in full, its spells table as a reference);
explicit verify + candidate audit 3/0 (52 identical files, 62 types, Dance
identical; native-only 16 of 1,722 and full-user 38 of 10,443 changed
dispositions; classifier unchanged `7eb7fcf09bcc…`, tables unchanged).

Limits: the historical exports predate the on-cast facts, so the R11-F1 rule
cannot fire in the inventory audit; among the committed native fixtures only
Firebelly's breath (excluded) carries `AbilityExecuteActionOnCast` (Physical
Enhancement is not in the fixtures). The native selector boundary (any margin
or corpulence term) is unverified, so the planner uses the recorded radius.

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
