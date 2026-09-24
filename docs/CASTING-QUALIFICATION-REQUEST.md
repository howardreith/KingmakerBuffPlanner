# Casting qualification run request: zero-cost-mixed (automation fixture)

Status: **prepared, not run.** It follows the one-cast Resistance probe
(`docs/LIVE-CAST-PROBE-REQUEST.md`). The automation fixture loads again:
`full-user` stages the owner-approved exact copy of the sealed
KingmakerGunslinger 0.0.133 (`7664f2f`), and the installed 0.0.136 is
recorded and restored byte-exact around every run.

## Purpose

This is the first in-game qualification of the casting-first production
path, not of a probe path. Every cast goes through the same session Apply
a player uses: fresh discovery, the routine's accepted contents, the gate,
the exact Standard projection, and the production execution host with its
real executors. Only an allowance-bound qualification boundary sits in
front of the host.

The run covers these mission section 8 items on the automation party:

| Item | Step |
| --- | --- |
| Genuine zero-cost source | Every cast is a verified-free cantrip; resources must stay unchanged |
| Mixed-caster routine, independently assigned castings | `qual-cast-1` and `qual-cast-3` by one caster, `qual-cast-2` by the other |
| Cancellation prevents later submissions | **stop**: the run is stopped after its first casting |
| Existing-effect skip (repeat use) | **complete**: `qual-cast-1` is skipped as already active; **repeat**: nothing to cast |
| Always recast | **recast**: `qual-cast-1` set to Always recast is cast again |
| Close and reopen, persisted acceptance | **recast** runs from a session reopened from disk, authorized by the restored acceptance |

The automation party has only cantrips, so finite prepared or spontaneous
resources, group buffs and metamagic variants need the advanced copy.

## Contracts

- **Recipe `zero-cost-mixed`.** One verified-free plain spellbook buff
  (on this fixture, Resistance) that two different casters cast by rule.
  It targets two or three other party members that do not have it now.
  The recipe is chosen deterministically; it refuses rather than
  improvise.
- **Projections.** The selection run (`live-cast-qual-select`) forecasts
  the exact projection of each executing step:
  - stop: all castings;
  - complete: the rest;
  - recast: `qual-cast-1`.

  The allowance names those three ids, in order. A step whose real
  projection differs is refused, and nothing further is submitted.
- **Budget.** At most 6 native submissions (3 + 2 + 1; the stop step
  submits 1 of its 3). The mission ceiling is 24.
- **Deadlines.** The run has a 240-second deadline. Each executing step
  also has the production host deadline (20 s plus 45 s per casting).
  Any deadline ends the run through the host terminal.
- **Scope.** WORKING automation fixture, `full-user` profile, instant
  mode, isolated test-mod settings (the owner's Mods folder is moved
  aside and restored exactly), the protected-save comparison around the
  run, and no save write.

## Expected observations (fresh native reads)

| Step | Report | Effects | Resources |
| --- | --- | --- | --- |
| stop | cancelled after 1 of 3 castings; `qual-cast-1` confirmed, the rest not started | target 1: new instance; targets 2 and 3: absent | unchanged |
| complete | completed; `qual-cast-1` skipped, the rest confirmed | target 1 unchanged; targets 2 and 3: new instance | unchanged |
| repeat | refused as `nothing-to-cast:3`; nothing submitted | unchanged | unchanged |
| recast | completed; `qual-cast-1` confirmed, the rest skipped | target 1: new instance or refreshed | unchanged |

Any other outcome fails the run, which then stops: each step is judged
the moment it ends, and a failed, uncertain, cancelled or otherwise
unexpected step ends the run before anything else is submitted (review
of `54d330b..47caeef`, P0). The casting run also writes no save at all:
the WORKING save and any new save file count as protected-save
violations.

## Procedure

1. The one-cast Resistance probe on the current candidate has run.
2. Selection run (non-casting):
   `Invoke-KingmakerRuntimeTest.ps1 -Scenario live-cast-qual-select -CompatibilityProfileId full-user -TimeoutSeconds 900 -RunId <fresh>`.
   900 seconds is the whole run's budget, counted from the game's start:
   the launcher refuses less. The game host stops the run itself 60
   seconds before it ends (no further native submission; the run fails
   as `overall-deadline`), and at its own deadline the launcher writes an
   abort marker (`abort.json`) that the host obeys at once, then waits up
   to 120 seconds for the game to finish (final review C3). A game that
   still does not stop is reported as failed, its Mods restoration stays
   blocked while Kingmaker runs, and `Restore-Local.ps1 -RunId <runId>`
   recovers it once the game has exited.
   It writes `qual-outcome.json` with the selection, the party roster it
   saw, and the three forecast projection ids and contracts.
3. Claude writes the allowance under the owner's delegated mission
   authority with `scripts\New-KbpRunAllowance.ps1 -Kind qualification`
   (schema 5), once, exclusively, under
   `C:\Dev\KingmakerBuffPlannerLab\approvals\<runId>.json`. The writer
   takes the recipe, the forecast projection ids (in order) and the
   profile, identity and WORKING save from the selection run's evidence,
   the build identity from the frozen build, and sets
   `maximumNativeSubmissions` to the sum of the forecast castings, which
   is exactly how the host's boundary counts them.
4. Casting run:
   `Invoke-KingmakerRuntimeTest.ps1 -Scenario live-cast-qual -CompatibilityProfileId full-user -TimeoutSeconds 900 -RunId <runId> -QualificationAllowancePath <file>`.
   It makes one attempt and never retries.
5. Evidence: `qual-outcome.json`, `runtime-result.json`,
   `protected-saves.json`, `orchestration.json`, the frames, and the
   verified restoration.

## Authority

The owner mission of 2026-09-23 (sections 4 and 8) authorizes bounded,
out-of-combat buff qualification on the disposable WORKING fixture, and
mechanically creating each run's allowance on the owner's behalf (owner
message of 2026-09-23, section 1). Each run needs a fresh run id and an
exclusively created allowance and authorization record; an existing
allowance is never overwritten or reused. A tool permission refusal is
reported with its exact action and path, never worked around.

## Recipe `finite-direct-mixed` (advanced copy)

Status: **prepared, not run.** It needs an owner-designated
`KBP_ADVANCED_SEED`, the guarded advanced bootstrap, a passing
`live-advanced-inspect` of the same bound pair (the launcher refuses a
casting run on the advanced copy without one), and a loadable
compatibility profile for that party.

It covers the mission section 8 items the automation party cannot:

| Item | How |
| --- | --- |
| Finite prepared exact slot | A prepared caster casts twice; each cast must spend exactly the token its step reserved (the recast reserves the next one) |
| Finite spontaneous | A spontaneous caster casts once; its level count must drop by one |
| Mixed casters | `qual-cast-1` and `qual-cast-2` have different casters and pools; a prepared plus spontaneous pair is preferred |
| Metamagic variant | Chosen when the party offers one (reported as coverage `metamagic`); the effect must carry the metamagic for the complete step to skip |
| Cancellation, skip, repeat, recast, reopen | The same stop, complete, repeat and recast steps as `zero-cost-mixed` |

### Contracts

- **Selection.** One plain direct buff source that two different casters
  cast by rule from finite spellbook pools (prepared slots or spontaneous
  levels), caster A with at least two casts available and caster B with
  at least one, on two other party members without the effect. The
  selection reports what it covers and every rejected candidate.
- **Projections.** stop: both castings (A executes, then the run is
  stopped); complete: `qual-cast-2`; recast: `qual-cast-1` with the next
  reserved slot. The forecast simulates exactly what each earlier step
  spends, so the three ids are the ones the run will submit.
- **Budget.** The sum of the forecast castings: 4 (stop 2, complete 1,
  recast 1), exactly as the host's boundary counts planned castings.
- **Resources, judged per step.** Each casting's native availability must
  drop by exactly the confirmed casts from its pool (for prepared slots,
  of the same spell) and never otherwise. A confirmed prepared casting must
  turn exactly its reserved tokens from available to spent; no other
  token may change. Any other observation ends the run at that step.
- **Saves.** None: every save, WORKING included, must be unchanged and no
  new save file may appear.

### Procedure

1. The owner designates `KBP_ADVANCED_SEED`; the guarded bootstrap seals
   the advanced pair (`New-KbpAutomationFixture.ps1 -Family Advanced`).
2. Non-casting inspection:
   `Invoke-KingmakerRuntimeTest.ps1 -Scenario live-advanced-inspect -FixtureFamily Advanced -CompatibilityProfileId <profile> -RunId <fresh>`.
3. Selection (non-casting):
   `Invoke-KingmakerRuntimeTest.ps1 -Scenario live-cast-qual-select -FixtureFamily Advanced -QualificationRecipe finite-direct-mixed -CompatibilityProfileId <profile> -TimeoutSeconds 900 -RunId <fresh>`.
   `qual-outcome.json` records the selection, its coverage and the three
   forecast projection ids and contracts.
4. Claude writes the allowance under the delegated authority with
   `scripts\New-KbpRunAllowance.ps1 -Kind qualification` (schema 5; the
   recipe `finite-direct-mixed` and the three ids in order come from the
   selection run, and `maximumNativeSubmissions` is 4) under
   `approvals\<runId>.json`.
5. Casting run:
   `Invoke-KingmakerRuntimeTest.ps1 -Scenario live-cast-qual -FixtureFamily Advanced -QualificationRecipe finite-direct-mixed -CompatibilityProfileId <profile> -TimeoutSeconds 900 -RunId <runId> -QualificationAllowancePath <file>`.
