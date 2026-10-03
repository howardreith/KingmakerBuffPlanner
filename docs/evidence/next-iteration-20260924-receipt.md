# Next iteration after rc5: ability pools, an Extend rod, and a rod left switched on (2026-09-24)

Development evidence on the owner's advanced fixture `KBP_ADVANCED_SEED`
(a *Beneath the Stolen Lands* save; profile `advanced-gunslinger-0136`).
It is not candidate evidence: the successor candidate repeats these
qualifications on its own frozen build. Nothing here proves behaviour
specific to the main campaign.

## Builds

| Build | Commit | Package SHA-256 | Gate |
|---|---|---|---|
| Recipes | `e2a495939afd883a3f20b4159cfa1a541b819a03` | `d2981a34883716f319ccb12cc0550c76303716115986c958dff6371ac4e6be7a` | source 42, protocol 337, harness 38, package 4, deploy WhatIf 5, launcher WhatIf 12, fixture 3, Restore-InstallLocal 16, publisher 3 |
| Fixed | `fb762e21352bfeb63979ed3c87f14887bba9a3d1` | `04527c6f534b676b75c906e83703f72887157fa5f2205a23aa648f5b70e44506` (DLL `5cee4cda…`, MVID `dab595ed-f1ad-4b18-8fe0-7c7884bc6ee8`; the same bytes from the gate worktree and the A1 build) | source 42, protocol 338, harness 38, package 4, deploy WhatIf 5, launcher WhatIf 12, fixture 3, Restore-InstallLocal 16, publisher 3 |
| Hardened | `64c6e22bc219c08579893e194d0e161011b4791f` | `270199dc1eacf967184e59a38e1536f73f32aea98aef81e8fb97ed280a158061` (DLL `4fe45768…`, MVID `b2935afe-51e1-4e15-bf1d-88656f327483`; the same bytes from the gate worktree and the A1 build) | source 42, protocol 338, harness 38, package 4, deploy WhatIf 5, launcher WhatIf 12, fixture 3, Restore-InstallLocal 16, publisher 3 |

Relative to rc5, `e2a4959` changes only qualification and observation code
(the recipes, the probe's caster and clock reads, the evidence fields), so
the player-facing behaviour it exercised is rc5's. `fb762e2` and
`64c6e22` change the product (below); the rc6 candidate carries
`64c6e22`'s product code.

## The Mutagen: an ability pool (`ability-pool-direct`), PASS

The Alchemist's *Mutagen — Strength* (an ability resource with one use a
day), cast by the Alchemist on the Alchemist (the Mutagen reaches only
its user). Runs `casting-qual-select-20260924-adv-ability-01`,
`casting-qual-cast-20260924-adv-ability-anim-01` and `-inst-01`: PASS,
complete, restoration verified, saves clean.

- **Use.** One Apply, one native cast: the pool went from 1 to 0 and a
  new Mutagen buff landed with its native modifiers read from the game
  (Strength +4 alchemical, Intelligence −2, natural armour +2). Animated
  cast through a native command; Instant by the game's rule
  (`strategy:DirectRuleCast`, the spend owned by the source ability).
- **Repeat.** Apply again under the default *skip if active*: nothing to
  cast (`nothing-to-cast:1`), nothing submitted.
- **Exhausted.** The casting set to *Always recast*: Apply is refused
  before anything is cast, naming the empty pool
  (`resource-pool-exhausted:…:0<1`); the game's own count stays 0 and the
  buff is unchanged.

On the fixed build (`casting-qual-select-20260924-adv-ability-02`,
`casting-qual-cast-20260924-adv-ability-anim-02` and `-inst-02`): PASS in
both modes, with the same three steps. Under the stricter plain-buff rule
the capability inventory now names what an empty action is: the Strength
and Dexterity Mutagens' first action is a null action (plain `empty`, no
unmodeled reason), so the Strength Mutagen still qualifies honestly. The
published observations carry the game clock (the Mutagen's buff had
about 5,400 s left, 10 minutes per level at level 9) and the record
publishes the exhausted step's accepted edit.

## The Extend rod (`rod-extend-direct`): a real defect found

Selection (`casting-qual-select-20260924-adv-rod-01`, PASS): *Blur*
(level 2, 1 minute per level, caster level 9), cast by the Brown Fur
Transmuter from the same source twice — plain on the Cleric, then with the
Extend rod chosen on the casting through the workspace on the Alchemist.

Animated (`casting-qual-cast-20260924-adv-rod-anim-01`): **FAIL at the
plain step**, restoration verified, saves clean; the chain stopped there,
so Instant did not run. The plain casting chose no rod, yet a rod charge
was spent (6 → 5) and the buff lasted 1079.6 s — Blur's 540 s doubled.
The transmuter carries two copies of one Extend rod, and the second copy
was switched on in the game before the cast.

**Cause.** Both executors skipped enhancement preparation for a step with
no enhancement, so whatever the player had left switched on applied to the
planner's cast and spent its resource. A casting that chose nothing was
not cast as authored. The same shortcut exists in the Classic planner
(see the owner decision below).

**Fix (`fb762e2`, casting-first only).** A casting-first step applies
exactly the enhancements its casting chose: every toggle the casting did
not choose is switched off for the cast and restored afterwards. Reading
the game's own code showed that switching a toggle off does not stop it at
once unless its blueprint says so (`ActivatableAbility.OnTurnOff`): it
keeps running, its buff applied, until the next round. An unchosen rod
still running is therefore stopped at once (`Stop(true)`, which removes
its buff); any other unchosen toggle still on or running refuses the cast
with a visible reason rather than casting with it. A regression test
reproduces the live failure (a rod left on, both modes) and fails without
the fix.

On the fixed build: selection
(`casting-qual-select-20260924-adv-rod-02`) PASS with the same castings,
and a cleric spell refused as too short to judge (9 rounds). Animated
(`casting-qual-cast-20260924-adv-rod-anim-02`) and Instant (`-inst-02`):
PASS, complete, restoration verified, saves clean.

| Mode | Plain casting (no rod) | Rod casting |
|---|---|---|
| Animated | 539.6 s; rod charges 6 → 6 | 1079.6 s (twice); 6 → 5 |
| Instant | 540.0 s; rod charges 6 → 6 | 1080.0 s (twice); 6 → 5 |

Same strength (Blur has no stat modifiers to compare), one spell-level
use per cast, and the caster's toggles as before after each step (the
rod the player left on was on again).

## Independent review of the next-iteration work

A read-only reviewer examined the diff since rc5 against its callers.

- **The plain-buff check treated every empty action as doing nothing.**
  Discovery also produces an empty expression for damage, healing,
  removing a buff, unknown actions and depth or cycle limits. Fixed: the
  expression records why (not serialized, no identity change); only a
  null action or an empty list is ignored; the capability inventory shows
  the reason.
- **The rod's duration check could not tell a plain cast from an
  extended one at a few seconds.** Fixed: the recipe needs ten rounds of
  expected duration and the plain cast must last a minute.
- **A condition could give the two recipients different durations.**
  Fixed: the rod recipe refuses a conditional source.
- **Evidence gaps.** Fixed: the published observations carry the game
  clock (or why it was not read), and the record publishes the exhausted
  step's accepted edit.
- The reviewer found no way for the ability-pool steps to pass without
  the behaviour they claim, and settled the two-rod question from the
  live reads: two copies of a rod dropped by exactly one charge per use.

15 mutants of the new guards, all killed; 338 tests. (Two more for the
rod recipe's duration boundary after the second review, both killed.)

## Second review, and the hardened build

A second read-only reviewer examined `fb762e2`. No Classic behaviour
changed and no exit path leaves a toggle the player had on switched off.
Its findings, all addressed in `64c6e22`:

- **Two copies of one rod.** The transmuter's rods are two copies of one
  item, only the second switched on. The rod casting chose the first
  copy, started it while the second's buff was applied, then stopped the
  second. It worked live (the copies' buffs are separate instances), but
  nothing guaranteed it. Casting-first now prefers the copy already
  running (then on), and switches everything unchosen off, stopping
  running rods, before switching anything chosen on.
- **A rod left running after the cast.** A rod switched on for the cast
  and back off would run until the next round, extending whatever was
  cast next. The lease now stops it. The caster read records each
  toggle's running state beside its switch, so "every toggle as before"
  covers one left running.
- **The duration boundary.** At exactly ten rounds the plain cast would
  read under a minute and fail every time; the recipe needs twenty.
- **Scope.** "Exact" covers the enhancements the planner models (rods,
  Powerful Change, Share Transmutation); other mods' spell toggles are
  not managed. Brown-Fur's toggles stop at once only with the installed
  provider's immediate-off patch; without it an unchosen one still
  running refuses the cast visibly. Both are documented.

On the hardened build, each run PASS, complete, restoration verified,
saves clean:

| Recipe | Runs | Result |
|---|---|---|
| Extend rod | `casting-qual-select-20260924-adv-rod-03`, `casting-qual-cast-20260924-adv-rod-anim-03`, `-inst-03` | plain 539.6 s / 540.0 s with no charge; with the rod 1079.6 s / 1080.0 s for one charge (6 → 5); the copy already running was the one used; both copies' switch and running state as before |
| Powerful Change | `casting-qual-select-20260924-adv-enh-02`, `casting-qual-cast-20260924-adv-enh-anim-02`, `-inst-02` | plain +4 Strength with no reservoir point; enhanced +6 for exactly one point (16 → 15); both about 600 s, so the rod left on no longer extends them; every switch and running state as before |

## Area transition: route check, unavailable

The seed starts in *Tenebrous Depths, I* (`Area_Dwarf_5`), not in combat.
Its read-only area diagnostics list two exits, `MapExit_toHub` (8.9 m
from the party) and `MapExit_deeper` (117.6 m), both with
`AutoSaveMode=BeforeExit`, and the game's autosave setting is on. Either
exit would write an ordinary autosave before leaving, which the owner's
terms forbid. No transition was attempted. A test needs an exit that
writes no save, or the owner's decision about the autosave setting (not
changed).

## For the owner

- **Classic and toggles left on (decision).** The Classic planner keeps
  the game's toggles as they are when a buff chooses no enhancement, so a
  rod left switched on applies to every eligible Classic cast and spends
  its charges; when a Classic buff does choose one, the others are
  switched off, but a rod whose deactivation the game defers still
  applies to that cast, without spending a charge (the rod's own code
  spends only from a copy that is switched on). Casting-first now always
  casts exactly what the casting chose. Classic was not changed, because
  it is the protected default. Recommendation: give Classic the same rule.
- The advanced seed's transmuter has one of its two Extend rods switched
  on. The earlier advanced runs (rc5) did not read rod charges, so some of
  their transmuter casts may have been extended by that rod; their
  verdicts stand for what they checked (effects, strength, the spell
  pool).
