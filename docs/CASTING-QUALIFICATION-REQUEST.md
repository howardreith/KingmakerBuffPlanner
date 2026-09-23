# Casting qualification run request: zero-cost-mixed (automation fixture)

Status: **prepared, not run.** It follows the frozen Resistance probe
(`docs/LIVE-CAST-PROBE-REQUEST.md`), which must run first. Both are
currently blocked: the probe allowance file does not exist yet, and the
installed KingmakerGunslinger (0.0.136) no longer matches the sealed
`full-user` compatibility profile (0.0.133) that the automation fixture
needs to load.

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

Any other outcome fails the run, which then stops.

## Procedure

1. The frozen Resistance probe has run (or the owner explicitly waives
   that ordering).
2. The `full-user` profile loads the fixture again: Gunslinger 0.0.133 is
   restored, or the owner approves a reseal.
3. Selection run (non-casting):
   `Invoke-KingmakerRuntimeTest.ps1 -Scenario live-cast-qual-select -CompatibilityProfileId full-user -RunId <fresh>`.
   It writes `qual-outcome.json` with the selection and the three
   forecast projection ids and contracts.
4. The allowance (schema 3) is written once, exclusively, under
   `C:\Dev\KingmakerBuffPlannerLab\approvals\<runId>.json`, with the
   fields `runId`, `sourceCommit`, `packageSha256`, `dllSha256`,
   `assemblyMvid`, `fixtureGameId`, `recipe`, `approvedProjectionIds`
   (the three forecast ids in order), `maximumNativeSubmissions` = 6,
   `approvedBy` and `authority`. The build identity comes from the
   selection run's build manifest.
5. Casting run:
   `Invoke-KingmakerRuntimeTest.ps1 -Scenario live-cast-qual -CompatibilityProfileId full-user -RunId <runId> -QualificationAllowancePath <file>`.
   It makes one attempt and never retries.
6. Evidence: `qual-outcome.json`, `runtime-result.json`,
   `protected-saves.json`, `orchestration.json`, the frames, and the
   verified restoration.

## Authority

The owner mission of 2026-09-23 (sections 4 and 8) authorizes bounded,
out-of-combat buff qualification on the disposable WORKING fixture. It
requires a fresh run id and an exclusively created authorization record
per run. The tool permission classifier refused Claude's earlier write
under `approvals\`. Until the owner allows that write, or writes the
file, this run cannot start.
