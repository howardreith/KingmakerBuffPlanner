# Advanced fixture qualification — `KBP_ADVANCED_SEED` (2026-09-24)

**A Beneath the Stolen Lands fixture.** The owner created this disposable
save through the game's normal save interface for testing: a new *Beneath
the Stolen Lands* campaign, the party brought to level 9 with deliberately
granted experience, then the normal level-up, spell-selection and
preparation screens. It is an owner-designed test fixture, not a naturally
progressed main-campaign save. What it shows about finite resources, group
buffs, mixed coverage and a per-casting enhancement holds for this party
and these spells; it does not prove behaviour specific to the main
campaign.

## Fixture identity

| Item | Value |
| --- | --- |
| Seed | `KBP_ADVANCED_SEED`, SHA-256 `48f048ce1b9ea3887b1acc86c81ebcead0b3a6eca39aded4f1dfce1a914d9ace` (never modified; archived) |
| Campaign | Game id `cb1f405d-0b0c-4281-8b76-3b4f3b690abb`, Endless DLC Beneath the Stolen Lands; area Tenebrous Depths I; party level 9 |
| Derived pair | BASELINE `Manual_308` (`6b5b7ea0...`), WORKING `Manual_309` (`97876137...`), by the guarded bootstrap `bootstrap-advanced-fixture-20260924-01` |
| Party | Cleric 9, Alchemist 9, Fighter 9, Brown Fur Transmuter (Arcanist 9, Call of the Wild); no pets or companions |
| Profile | `advanced-gunslinger-0136`: the full-user mod set with Gunslinger 0.0.136 at the six values the owner approved (manifest `d08f0d5a...`, 238 files, 47128988 bytes, DLL `c6cccdac...`, Info.json `f66de05d...`), staged from the exact copy `examples\KingmakerGunslinger-0.0.136` (taken from the Gunslinger lab's 08:31 pre-deploy snapshot, verified before and after copying) |
| BagOfTricks | Frozen copy `examples\BagOfTricks-20260924` (`bc3e790b...`, 1805726 bytes; version, info and assembly unchanged), staged in both profiles; unlimited casting, metamagic, material components, infinite abilities, instant cooldown, restore after combat, no resource cost and the other resource-altering toggles off, spells-per-day multiplier 1 |
| Steam | Offline, Cloud sync disabled for Kingmaker, measured by the existing procedure on every run (never changed) |

## Capability matrix (non-casting inspection `advanced-inspect-20260924-01`)

| Capability | In this party |
| --- | --- |
| Prepared spellbooks | Cleric (26 prepared slots, all available; one slot for each prepared spell); Alchemist extracts (17) |
| Spontaneous spellbook | Brown Fur Transmuter (Arcanist): level 1 6/6, level 2 6/6, level 3 5/5, level 4 4/4 |
| Variants | Protection from Alignment (4 forms) and its Communal form (4), Magic Circle (4), Protection From Energy (5), Resist Energy Communal (5), Protection from Energy Communal (5), Beast Shape II (2), off-hand weapon forms |
| Metamagic forms | None prepared (every metamagic mask 0) |
| Direct buffs | Many, for example Shield of Faith, Protection from Alignment, Bull's Strength, Eagle's Splendor, Mage Armor, Enlarge Person, Barkskin and Aid; the Alchemist's extracts (Aid, Barkskin, Fox's Cunning, Shield, Displacement, Resinous Skin and others) |
| Caster-centred group buffs | Bless (prepared, and an at-will form), Protection from Alignment Communal, Resist Energy Communal, Protection from Energy Communal, Bone Fists (cleric) |
| Target-anchored group buffs | Remove Fear (cleric), Haste (alchemist, 9 rounds), Protection from Arrows Communal (transmuter) |
| Shared effects | Protection from Alignment and its Communal form apply the same buff (`4a691196...`): the basis of the mixed-coverage case |
| Ability resources | Cleric domain powers (Agile Feet 9/9, Resistant Touch 9/9); Alchemist Mutagen 1/1 (Strength, Dexterity, Constitution) |
| Enhancements | Extend Metamagic Rod (cleric 3 uses, transmuter 6); Powerful Change (6 scores, arcane reservoir 16); Share Transmutation (reservoir 16; it changes whom a spell reaches, so this version refuses it, whether chosen as a targeting modifier or as an enhancement) |
| Live effects at load | A few permanent instances, none timed; no unreadable caster level or suppression flag |

These are the development qualification runs on the advanced fixture,
each on a build frozen under `runtime-backups/qualification-frozen/` from
the clean checkout `repo/KingmakerBuffPlanner-A1`. Each ran through the
real launcher, protocol, inventory, request and staging contracts, and
was restored byte-exact with its saves compared clean. The same recipes
run again on the frozen release candidate (see its receipt). The defects
they found are listed with their fixes in
`docs/CASTING-FIRST-REVIEW-INDEX.md`.

## Finite resources (mission batch 3, section 7)

Selection `casting-qual-select-20260924-adv-finite-02` (build `2e565bd`)
chose Protection from Alignment — Chaos from two casters with different
finite pools:

| Casting | Caster and exact source | Recipient |
| --- | --- | --- |
| `qual-cast-1` | Brown Fur Transmuter, Arcanist spellbook, spontaneous level 1 (6 of 6) | Alchemist |
| `qual-cast-2` | Cleric, prepared slot `level-1\|type-0\|index-5` (a native id with delimiters, bound exactly) | Fighter |

Both casting runs passed every step exactly as forecast:
`casting-qual-cast-20260924-adv-finite-anim-02` (Animated) and
`casting-qual-cast-20260924-adv-finite-inst-01` (Instant).

| Step | What happened (the same in both modes) |
| --- | --- |
| stop | The player's routine press while the first cast was in flight (Animated) or before the next (Instant): `qual-cast-1` confirmed (new instance), `qual-cast-2` not processed. Spontaneous level 1: 6 to 5; the cleric's reserved slot stayed available |
| complete | `qual-cast-1` skipped as active (its pool unchanged, 5 to 5); `qual-cast-2` confirmed; its exact slot `level-1\|type-0\|index-5` went from available to spent, and no other slot changed |
| repeat | Refused before submission: `nothing-to-cast:2` (both buffs active; nothing spent) |
| recast | Always recast on `qual-cast-1`: cast again (spontaneous 5 to 4); `qual-cast-2` skipped, its slot still spent |

Three native submissions per run; no violation. The earlier run
`casting-qual-cast-20260924-adv-finite-anim-01` also reported PASS from
the game, but its restoration was refused because the runtime-harness
test suite had started a fake `Kingmaker.exe` during it (recorded in the
review index as an incident, not a product defect); the guarded
`Restore-Local.ps1` restored the installation byte-exact, and the case was
run again as `-anim-02`.

## Group buffs and mixed coverage (mission batch 3, section 8)

Selection `casting-qual-select-20260924-adv-group-02` (build `0c03930`):

| Casting | What it is | Caster and exact source | Beneficiaries |
| --- | --- | --- | --- |
| `qual-cast-1` | Direct | Cleric, Protection from Alignment — Chaos, prepared slot `level-1\|type-0\|index-5` | Fighter |
| `qual-cast-2` | Caster-centred group | Cleric, Protection from Alignment, Communal — Chaos, prepared slot `level-2\|type-0\|index-0` | Cleric, Alchemist, Fighter, Transmuter (Fighter pre-covered) |
| `qual-cast-3` | Target-anchored group | Transmuter, Protection from Arrows, Communal, spontaneous level 3 (5 of 5), anchored on the Cleric | Cleric, Alchemist, Fighter, Transmuter |

Both casting runs passed exactly as forecast:
`casting-qual-cast-20260924-adv-group-anim-02` (Animated) and
`casting-qual-cast-20260924-adv-group-inst-01` (Instant).

| Step | What happened (the same in both modes) |
| --- | --- |
| prime | `qual-cast-1` alone: the Fighter received the effect (new instance); its exact slot went from available to spent |
| mixed | `qual-cast-1` skipped (the Fighter held the effect at the Cleric's level). `qual-cast-2` cast once, one invocation and one slot (`level-2\|type-0\|index-0` available to spent) for four beneficiaries; every beneficiary new-instance. The game replaced the Fighter's longer direct instance with the communal one. `qual-cast-3` cast once, one spontaneous level-3 use (5 to 4) for four beneficiaries |

Two native submissions per run; no violation.

The first group run on the earlier build,
`casting-qual-cast-20260924-adv-group-anim-01`, passed its prime and mixed
steps but failed its former repeat step. Because the game had replaced
the Fighter's longer instance with the shorter communal one, the direct
casting was due again and its prepared slot was spent
(`blocked-casting:qual-cast-1:prepared-slots-exhausted`). The recipe now
ends after the mixed step, and the planner tells the player when a group
cast may shorten a recipient's longer buff (`0c03930`).

## A per-casting enhancement: Brown-Fur Powerful Change (mission batch 3, section 8)

Selection `casting-qual-select-20260924-adv-enh-01` (build `f66246c`):

| Casting | Caster and exact source | Recipient | Enhancement |
| --- | --- | --- | --- |
| `qual-cast-1` | Brown Fur Transmuter, Bull's Strength, Arcanist spellbook, spontaneous level 2 (6 of 6), caster level 10 | Cleric | none |
| `qual-cast-2` | the same source | Alchemist | Powerful Change: Strength (one Arcane Reservoir point per cast) |

The forecast routes `qual-cast-2` through the Brown-Fur provider's own
transaction (`ProviderDirectRuleCast`), because the installed provider
offers its version-1 direct contract. The enhancement was chosen on the
casting through the workspace's own option for the focused casting. The
casting was added plain, and the enhancement was then selected with the
command the inspector's chip calls. The recorded options show the chip
unselected before and selected after.

Both casting runs passed every rule:
`casting-qual-cast-20260924-adv-enh-anim-01` (Animated) and
`casting-qual-cast-20260924-adv-enh-inst-01` (Instant).

| Step | What happened (the same in both modes, except the route) |
| --- | --- |
| plain | `qual-cast-1` alone: the Cleric received Bull's Strength, and its Strength gained exactly one modifier from it, **Enhancement +4**. Spontaneous level 2: 6 to 5. Arcane Reservoir: **16 to 16**. All 17 activatable abilities of the Transmuter in the same state before and after |
| enhanced | `qual-cast-1` skipped (the Cleric's buff kept, still +4); `qual-cast-2` cast on the Alchemist: **Enhancement +6**, exactly the plain modifier raised by 2. Spontaneous level 2: 5 to 4. Arcane Reservoir: **16 to 15**. All 17 activatable abilities in the same state before and after (the one-shot Powerful Change choice was consumed, nothing left on) |
| route | Instant: the enhanced cast reported the provider's own transaction (`provider-direct:True`, strategy `ProviderDirectRuleCast`). Animated: the game's own cast (`native-command-spend-completed`) |

One native submission per step; no violation.

These runs also exposed a workspace defect. For Bull's Strength the
workspace offered Powerful Change for Constitution, Dexterity,
Intelligence and Wisdom as well. None of the Transmuter's spells raises
those scores, and an empty eligibility list was read as "any spell".
Choosing one would only have blocked the casting. It is fixed: the
workspace now offers exactly the enhancements the planner would accept
for the casting's source.

## Share Transmutation (refused, as designed)

The Transmuter owns Share Transmutation (Arcane Reservoir, 16 points).
The casting-first planner treats it as a targeting modifier. A casting
that selects it is compiled and shown, but its Standard execution is
refused before Apply, and the card says "a targeting modifier (such as
Share Transmutation) is not executed yet". The source tests pin that
refusal and its wording. No Share cast was attempted on this fixture. The
enhanced recipe's selection refuses Share, like every enhancement other
than Powerful Change.

## What this fixture does not show

- Main-campaign-specific behaviour: this is a Beneath the Stolen Lands
  campaign with a party the owner built for testing.
- Pets and companions: the party has none. A pet's coverage by a party
  buff stays a documented limitation.
- Metamagic variants and rods: no metamagic spell is prepared, and the
  Extend Metamagic Rods (cleric 3 uses, transmuter 6) were not used in a
  casting run.
- Ability pools: the Alchemist's Mutagen and the Cleric's domain powers
  were inventoried, not cast.
- An area change during a run, keyboard and mouse input from a person,
  and judgements of game frames.
