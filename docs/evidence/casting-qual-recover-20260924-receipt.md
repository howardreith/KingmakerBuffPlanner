# Casting-first qualification with the recover step: receipt (2026-09-24)

The zero-cost casting-first qualification gained a fifth step (mission batch
3, section 9, `b63da88`). **Recover** runs after the planner has been
disabled during a run and enabled again. It applies the same Always-recast
plan as a NEW run, which must be accepted and complete, while the stopped
run never resumes. Around it, the driver compares the owner's lifecycle
probe (live event subscriptions, HUD roots, HUD installation, planner
roots, game mode) before the disable and after the recover run, and checks
that every started run was reported exactly once with no callback failure.

Build: `13f6d37bc483f6d96a7ddd7dbb0c71325375580b`, package `13fb8ab6…`, DLL
`c6f5b9c3…`, MVID `e3c73780-022e-4462-aef0-bf21b3d3494a`. It is frozen under
`runtime-backups\qualification-frozen\13f6d37…`, is a development build, and
carries the inherited version string `0.2.0-rc2`. Fixture: WORKING automation
save, `full-user` profile with the exact external Gunslinger 0.0.133 copy.
The allowances were written by Claude under the owner's delegated mission
authority. Budget: 8 planned native invocations (3+2+1+1+1), 8 used.

| Run | Result |
| --- | --- |
| `casting-qual-select-20260924-45bff28-01` | entry refused by a sharing lock on the live Mods folder (see below); nothing deployed |
| `casting-qual-select-20260924-13f6d37-01` | PASS: five forecast steps (`recast`, `disable` and `recover` share projection `6c81c114…`); area and level-1 diagnostics |
| `casting-qual-cast-20260924-13f6d37-anim-01` | **PASS** (animated) |
| `casting-qual-cast-20260924-13f6d37-inst-01` | **PASS** (instant) |

| Step | Animated | Instant |
| --- | --- | --- |
| stop | the player's stop pressed while qual-cast-1 was in progress; it completed, nothing later started | pressed before qual-cast-1 finished; the same outcome |
| complete, repeat, recast | as forecast | as forecast |
| disable | the planner disabled **while the cast was in progress**: interrupted and cleaned up, nothing landed | disabled **before the run's first step**: the instant disable-before-start case, recorded as `before-start`. It is **not** an in-flight instant interruption (see the correction below) |
| recover | a new run: qual-cast-1 new instance, the rest unchanged | the same |
| lifecycle | `subscriptions=1;hudRoots=1;hudInstalled=True;plannerRoots=1;mode=Default` before and after | identical |
| runs | started 5, reported 5, no callback failure | the same |
| resources | Resistance -1 > -1 for every casting (free) | the same |

Every save was unchanged and none was created. The Mods folder was restored
and verified, and `run-completion.json` is complete for every passing run.

**Correction (batch 3 reviews B3 and B7).** An earlier version of this receipt
and of the driver's comment called an instant cast "atomic within one pump".
That is not accurate. The instant executor submits within one pump but
confirms and cleans up over later frames. The instant disable step lands
before the first submission, so it proves disable-before-start only, and no
in-flight instant interruption is claimed. Also, in these runs (build
`13f6d37`) the planner was enabled again in the same update as the disable.
From the review fix that adds `Main.SetEnabledForRuntime`, the driver holds the disable over five whole updates
through the mod toggle's own path, in which the planner root is not ticked.
It requires that the host neither runs nor accepts a run while disabled, and
that the lifecycle line comes from the owner's real probe. The runs above
predate that stricter step. They stay valid for what they show, and the rc3
qualification repeats the step under the stricter rule.

## Area transition: unavailable on this fixture

`area-diagnostics.json` of the selection run, read-only, records:

- Area `JamandisMansion` (`2849fdde…`), mode Default, not in combat.
- **The game's autosave setting is on.**
- One area transition, `MapExit`, to `JamandisMansionThroneroom` through the
  enter point `JamandisMansionThroneroom_EndPrologue`, with
  `AutoSaveMode=BeforeExit`.

`AreaTransitionGroupCommand.ExecuteTransition` hands the transition's
autosave mode to `Game.LoadArea`. For `BeforeExit` with autosave on, that
method saves to the next autosave slot before leaving. The owner's save
folder holds rotating `Auto_N` files, and the destination ends the prologue.
The only pre-existing route would therefore write an ordinary save and
advance the story. **No area transition was attempted.** It needs a fixture
with a noncombat transition that writes no autosave (or the owner's decision
about the autosave setting, which was not changed).

## Finite and group buffs: unavailable on this fixture

The level-1 diagnostics list every finite spell the party knows: Linzi,
Ear-Piercing Scream and Cure Light Wounds; Tartuccio, Burning Hands and
Sleep. None is a buff, and no group buff exists. Finite and group
qualification waits for the owner's advanced seed.

## Harness incident and fix

`casting-qual-select-20260924-45bff28-01`: moving the live Mods folder aside
was refused with "Access to the path … is denied" (a transient sharing lock)
after the staged tree was ready. The automatic restoration then failed
closed, because the `Prepared` status was not recognized as pre-activation,
leaving the lock held. That also blocks the owner's Gunslinger lab, which
waits on it.

The harness's own restoration, run again once the status was
`RestorationFailed`, did the following:
- verified the live Mods folder byte-exact against the recorded original
  (1117 entries);
- removed the staging;
- released the lock.

`recovery.json` in that run's evidence records it. `13f6d37` makes both entry
moves use the bounded retry, recognizes `Prepared` as pre-activation, and
releases the lock when an entry fails before its state exists, with harness
tests for each case.
