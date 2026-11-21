# Missing beneficial buffs — repair and candidate audit

Mission: [planning/MISSING-BENEFICIAL-BUFFS-MISSION.md](MISSING-BENEFICIAL-BUFFS-MISSION.md)
(owner mission of 2026-11-21). Branch
`codex/kingmaker-buff-planner-missing-beneficial-buffs`, draft PR #25, from
evidence baseline `dd3e997100916576a268b24e46d49b4db7c34481` (mission commit
`a4ab152`). Product commit `e240d0a42e78af8539c79a3fbbeb4ab3878ad7b2`
(source, tests and audit harness; this record and its evidence follow in the
next commit). Version 0.6.19 unchanged (unreleased change). This record is
offline evidence only: **no native game check ran** (see
[Native checks](#native-checks-blocked)).

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
    hostile unit (`HarmfulToAlliesOfAHostileAbility`); it no longer vetoes an
    ability with no enemy targeting, whose payload is still judged by every
    harmful-buff, harmful-condition, offensive-carrier and marker rule;
  - `IsPersonalRange` / `CanBeAimedAtHostiles`: a Personal-range ability that
    can target itself is never "aimed at enemies" (hostile-current-target and
    hostile-ability-rider);
  - `IsUnitAnchoredAlliedArea`: a point-capable ability is admitted only when
    it may target self/friends, cannot target enemies, is not a sticky touch,
    and every persistent effect is a unit `Buff` reaching
    `AlliedAreaRecipients` through the ability's own first-child
    `AbilityTargetsAround` transform. Everything else point-capable keeps
    `point-target-without-placement`. No ground coordinate is invented: the
    existing provider-option builder offers only units the native
    `AbilityData.CanTarget` admits, and the existing allied-area geometry
    predicts recipients from that unit.
- No `NativeEffectOverrides.json` entry was added (the `include` shortcut
  would replace Good Hope's discovered allied area and conditional branch).
  The existing Freedom of Movement, True Seeing and Strength Surge overrides
  are unchanged and regression-tested.
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
- `NativeCatalogExporter` / `KingmakerBuffSourceDiscovery` take an explicit
  `NativeBlueprintLookup` (default: the loaded library, unchanged behavior) so
  the regression tests run the real adapter → scanner → exporter facts →
  classifier → live-discovery path on detached native blueprints. The
  report-only effect name read no longer throws outside a Unity player.

## Named families

| Family | Status | Evidence |
|---|---|---|
| Good Hope (`a5e23522…`, buff `85af9f0c…`) | **Fixed; offline-verified; native NOT RUN** | Baseline `harmful-ally-disposition`, repaired `valid-beneficial-party-effect`; live discovery admits it; whole expression kept (allied area, HasBuff branch, Crushing Despair removal); 15-ft radius read natively; one mass cast, one slot, out-of-radius ally never predicted; existing-buff verdicts read `GoodHopeBuff`; despairing recipient never reported as receiving Good Hope; rediscovery identical. |
| Haste (`486eaff5…`) | Unchanged (available before and after) | Same tests. |
| Firebelly (`b0652310…`, buff `7c33de68…`) | **Fixed; offline-verified; native NOT RUN** | Baseline `harmful-ally-disposition`, repaired `valid-beneficial-self-effect` (Caster payload). The breath it grants (`5e5b663f…`, AddFacts) stays excluded (point-target, offensive). |
| Abjuration Resistance — Acid/Cold/Electricity/Fire/Sonic | **Fixed; offline-verified; native NOT RUN** | Baseline hostile current target; repaired personal self buff, payload = own resistance; each removes exactly the other four; native cost `AbilityResourceLogic` 1 × `AbjurationResistanceResource`; no finite lifetime; Fire-then-Cold blocked as `removes-planned-effect`; active choice already satisfied; switching casts once. |
| Blessing of Luck and Resolve Mass, Feather Step Mass, Firebrand, Heroic Invocation | **Fixed (unit-anchor path); offline-verified; native NOT RUN** | Baseline `point-target-without-placement`, repaired party effect via own allied selector; native radii 30/15/30/30 ft; anchored plan predicts only the anchor's in-radius allies; one cast, one slot, step anchored on a unit. Native `AbilityData.TargetAnchor` is `Point` for these spells, so the in-game legality of a unit target (native `CanTarget`) is exactly what the blocked native check must confirm; until then the provider option builder fails closed (no legal anchor, no option). |
| Irresistible Dance (Call of the Wild) | Still excluded | Loaded-blueprint facts (0.4.1 full-user export): can target enemies, harmful to allies → `harmful-ally-disposition` under both rule sets. |
| Negative controls | Still excluded | Hideous Laughter, Call Lightning (`harmful-ally-disposition`: it can target enemies), Firebelly's breath, Dazing Touch (harmful condition), Treat Affliction (restoration + cooldown marker), Good Hope (Troll Lair) point variant with an `Any` selector. |

## Candidate audit (§3.D)

Method: `candidate-audit-missing-beneficial-buffs-*` streams every ability of a
guarded in-game catalogue export, rebuilds the exact classifier facts the
export recorded from the loaded blueprints, and judges them with the frozen
baseline classifier and the repaired one, under **both** Current and Pre040 and
in both live and static scope. Nothing is filtered (rows excluded under both
rule sets are kept). The exports' historical dispositions are not read; the
historical `planning/NATIVE-BUFF-CATALOG.json` labels are not used.

| Scope | Export (generator) | Abilities | Changed | Full report SHA-256 |
|---|---|---|---|---|
| native-only | `kbp040-rc4p-catalog-native` (`7ccf0c9`) | 1,722 | 16 | `40ca1ef8dfc263f2608841d0d6f681df641ed337c8e3feac32592fce1a249cec` |
| full-user | `kbp041-rel-catalog-full` (`85e7410`) | 10,443 | 48 | `45cb68e5eecc015515cd68eda95ef1dcbc32ad915368c15669607f3578d08005` |

Judged by classifier source SHA-256
`2ac1b4500994ec3afb212ba0de25862aa5e448b000fd18ef1305eafe3e45845f`. Changed
rows: [native-only-changes.tsv](../docs/evidence/missing-beneficial-buffs-2026-11-21/native-only-changes.tsv)
(`bb471ac3…9781`), [full-user-changes.tsv](../docs/evidence/missing-beneficial-buffs-2026-11-21/full-user-changes.tsv)
(`a61d6064…0207`).

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
Ifrit Lavasoul Firebelly ability. One row changed reason only: Shadow
Evocation, Greater (Caustic Eruption) moves from harmful-ally-disposition to
offensive-carrier-only (still excluded).

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

## Native checks (blocked)

Not run; no transaction was started. Blockers, re-verified 2026-11-21:

1. The lab's deployment lock is still owned by transaction
   `zhcn19-zh-auto-01` in `ModsRestoredPendingDisplay` (display restoration to
   the original single 1920×1080 desktop outstanding); every guarded launch,
   including `-WhatIf` preflight, refuses while it is unresolved
   ([AUTONOMOUS-BLOCKERS.md](../AUTONOMOUS-BLOCKERS.md)).
2. The automation fixture party (Hedwirg; Linzi, spontaneous bard with
   Heroism/Mirror Image/Resistance/Light; Tartuccio, sorcerer CL1) has none of
   the repaired spells or abilities, and the runtime casting scenarios cannot
   select an arbitrary ability or caster.

Exact remaining native evidence: (a) fresh guarded `native-buff-catalog`
exports (native-only, call-of-the-wild, full-user) showing the repaired
dispositions on currently loaded blueprints; (b) on a fixture that has them,
a real Good Hope cast (listing, legal target, one slot spent, Good Hope on
in-radius allies only, Crushing Despair branch), Firebelly (caster buff,
breath not listed), an Abjuration selection plus a switch (one resource use,
previous resistance removed), and one point-capable group spell cast on a unit
anchor (native `CanTarget` admits the unit, recipients, slot).

## Rejected approaches

- Name/GUID allow-list or an override for Good Hope/Firebelly: rejected —
  structural rule found; `include` overrides replace the discovered graph.
- Removing hostile/harmful checks broadly: rejected — Call Lightning and
  Irresistible Dance would enter under Pre040, and riders under Current.
- Treating the remove/apply conditional as the cause: rejected — Haste has the
  same structure.
- Rewriting Personal-range `CurrentTarget` leaves to `Caster` in the adapter:
  rejected — it would change every personal spell's graph and Share
  Transmutation's retargeting.

## Remaining limitations

- The retired Classic planner (honored only for an explicit legacy mode file)
  does not have the removal-conflict guard; the casting-first planner (default,
  the only authoring UI) does.
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

Baseline reproduction: each regression judges the same native facts with the
frozen `dd3e997` classifier and asserts the original exclusion before
asserting the repair. Native/purity/release coverage is deferred.
