# Classic cantrip casting, animated and instant: receipt (2026-09-24)

These are the first in-game casts through the **Classic** planner, the
default mode, after the shared casting code changed to cast cantrips at
will (mission batch 3, section 6). The Classic screen's own controls
authored the Long routine: the grid row of Resistance and the details
panel's target toggle. The HUD's routine entry then executed it once, under
a single-use grant for exactly the approved plan. The runtime-test lock
stayed on for every other route.

Fixture: WORKING automation save (campaign
`df33d1ff-4ec8-4707-bfa0-5e059bf9a049`), `full-user` profile with the
owner-approved exact KingmakerGunslinger 0.0.133 copy (the installed build
is recorded and restored byte-exact by every run). Every allowance was
written by Claude under the owner's delegated mission authority (batch 3,
sections 3 and 6). The owner did not type or inspect them.

## The plan

| Item | Value |
| --- | --- |
| Routine | Long, one step |
| Provider | Linzi (`2b56df7d-…`), bard spellbook `bc04fc15…`, level-0 entry `level-0\|heighten-0` |
| Spell | Resistance (`7bc8e27c…`), unlimited pool `…\|spellbook\|bc04fc15…\|unlimited` |
| Target | Hedwirg (`050aa19a-…`) |
| Strategy | DirectRuleCast |
| Plan digest | `ec085180434a3fa9b98c28bb6a04516963352c6ce39a2d0a00b619860325fc1a` (identical in every selection run and in both modes) |

## Runs

| Run | Build | Mode | Result |
| --- | --- | --- | --- |
| `classic-select-20260924-00aca73-anim-01` | `00aca73` | animated | refused before deployment: the owner's Gunslinger lab had just started the game (no lock, no transaction) |
| `classic-select-20260924-00aca73-anim-02` | `00aca73` | animated | PASS: plan recorded, no grant, no run |
| `classic-cast-20260924-00aca73-anim-01` | `00aca73` | animated | native cast **succeeded**, judged FAIL by a wrong rule (below) |
| `classic-select-20260924-00aca73-inst-01` | `00aca73` | instant | PASS |
| `classic-cast-20260924-00aca73-inst-01` | `00aca73` | instant | **PASS** |
| `classic-select-20260924-da0ee32-anim-01` | `da0ee32` | animated | PASS |
| `classic-cast-20260924-da0ee32-anim-01` | `da0ee32` | animated | refused before deployment: the other lab's game had started (no lock, no transaction) |
| `classic-cast-20260924-da0ee32-anim-02` | `da0ee32` | animated | **PASS** |

Builds (frozen under `runtime-backups\qualification-frozen\`):
- `00aca73…`: package `e43c372d…`, DLL `56c3c92e…`, MVID `2c9ab956-251a-4012-8aef-64246e55ab85`.
- `da0ee32…`: package `aaa60520…`, DLL `3363fead…`, MVID `e8a7bf59-7786-4cab-ba65-932d84d28f39`.

Both carry the inherited version string `0.2.0-rc2`. They are development
builds, not the rc2 candidate: rc2's frozen package and receipts are
unchanged.

## What each passing cast showed

| | Animated (`da0ee32`) | Instant (`00aca73`) |
| --- | --- | --- |
| Grant | used once (`attempts=1;consumed=True`) | used once |
| Classic result | Completed | Completed |
| Executor record | StrategySelected, Queued, CastStarted, EffectConfirmed | ExecutorSelected, StrategySelected, Submitted, CastStarted, SpendInvoked, EffectConfirmed |
| Resolution | `at-will-cantrip-ability;authored=spellbook:bc04fc15…/level-0;cast=026e5b80@7bc8e27c…` | `at-will-cantrip-ability;…;cast=9ff75b80@7bc8e27c…` |
| Effect on Hedwirg (independent fresh read) | new instance | new instance |
| Resistance availability (native count) | -1 > -1 | -1 > -1 |
| Finite pools | bard level 1 2 > 2, sorcerer level 1 5 > 5 | 2 > 2, 5 > 5 |
| Casting-first runs | 0 | 0 |
| Protected saves | unchanged, none created | unchanged, none created |

The cantrip resolved to the caster's class-granted at-will ability, and the
engine accepted it as available. The spend recorded in instant mode is that
ability's own unlimited spend: `spend-owner:source-ability-data`, count
unchanged. No finite resource moved.

## The one failure, and its fix

The first animated cast (`classic-cast-20260924-00aca73-anim-01`) succeeded
natively: `EffectConfirmed`, new instance, -1 > -1, pools unchanged,
at-will provenance. It was judged FAIL only because the Classic record
required `ExecutionReport.Submitted` to equal the step count in both modes.
The animated executor records `Queued` and `CastStarted`, never
`Submitted`, a distinction the casting-first judge already makes. Commit
`d6d5e33` judges each mode by what its executor records. The rerun on
`da0ee32` passed. This was a judgement defect, not a product defect, and it
is not counted as a Classic regression.

The instant detail's submit-frame field was renamed to
`effects-observed-at-submit` (`a93d94f`). It is read in the frame of
submission, before the buff lands on a later tick, so `false` there is
normal. The executor keeps reading until its confirmation window closes.

## Not claimed

- This is cantrip-only evidence (the automation party has no finite or
  group buffs: capability inventory `casting-qual-select-20260923-inv-01`).
  Finite and group Classic casting need the advanced seed.
- The installed player's copy (0.1.1-rc3) was not reinstalled to reproduce
  its Classic cantrip failure. That failure is inferred from the code path
  (a spellbook level-0 entry is refused by `UnitUseAbility.OnAction`'s
  `IsAvailable` guard, as observed in the casting-first run
  `casting-qual-cast-20260923-a1-anim-01`), not observed in Classic.
