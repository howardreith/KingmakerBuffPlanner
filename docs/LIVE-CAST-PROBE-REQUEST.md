# First live casting probe — prepared, dormant, awaiting approval

Status: **NOT EXECUTED. Nothing in this document is approval.** The probe
code exists and is tested with recording runtimes, but it cannot submit a
native cast unless the owner writes a run-bound allowance file (section
4). The production workspace still uses the disabled dispatch boundary.

This replaces the earlier request, which asked for approval of a commit
that did not yet contain the probe code (review L, "sequencing loop").

## 1. What exists (source, all at `2117438`)

| Piece | Where | What it guarantees |
| --- | --- | --- |
| Probe subset | `Planning/ExplicitCastingStepConverter.cs` (`SingleCastProbe` scope) | Exactly one direct-target casting; spellbook source, no metamagic, no special source; caster ≠ target; target in the verified reachable set; plain direct rule-cast strategy; current-target buff effects only; no enhancement, targeting modifier, group, material or non-native cost. Undefined scope values refused. |
| Projection identity | same file (`CanonicalContract`, `ProjectionId`) | SHA-256 of a versioned canonical JSON of every executable/observed step field, including exact reserved tokens and the expected effect tree. |
| Selector | `Execution/SingleCastProbe.cs` (`SingleCastProbeSelector`) | Deterministic first eligible casting from live discovery (ordinal provider, then target). It only picks; the converter enforces. |
| Allowance | same file (`SingleCastProbeAllowance`) | Strict owner-written JSON: exact members, this run id, the 64-hex approved projection id, caster, target, source, `maximumNativeSubmissions = 1`, approver. |
| Boundary | same file (`SingleCastProbeBoundary`) | Refuses by default. Recomputes the identity from the steps it is handed. Requires the approved id and selection. Consumes the allowance before anything reaches the game. Runs the existing `InstantCastExecutor` + `KingmakerInstantCastAdapter` through `ExplicitCastingRunCoordinator` with one submission. Disposal cancels and reaches the executor's cleanup. Never retries. |
| Scenarios | `RuntimeTesting/RuntimeTestHost.cs`, `RuntimeTestProtocol.cs`, `scripts/Invoke-KingmakerRuntimeTest.ps1` | `live-cast-probe-select`: WORKING fixture, zero synthetic input, records the selection, projection id and canonical contract, never constructs a boundary. `live-cast-probe`: the same plus the allowance, a 60 s run deadline and a `probe-stop.json` marker. The launcher requires `-ProbeAllowancePath` under `C:\Dev\KingmakerBuffPlannerLab\approvals\` with matching run id and build commit; the protocol accepts the allowance only on `live-cast-probe`, instant mode only. |

Source tests (recording runtimes, not gameplay):
`single-cast-probe-is-dormant-and-one-shot`,
`probe-scope-enforces-whole-subset`, `projection-identity-is-complete`,
`explicit-run-cancellation-disposes-executor`,
`probe-scenario-request-validation`, `single-cast-probe-run-record-rules`,
and the launcher gating cases in `Test-RuntimeLauncherFileWhatIf.ps1`.

## 2. Build identity (the code a probe would run)

| Item | Value |
| --- | --- |
| Source commit | `2117438756a4c8a202bd0008d87f0a9731f9dd87` (pushed; remote HEAD verified) |
| Package | `KingmakerBuffPlanner-0.1.1-rc3-local-runtime.zip`, SHA-256 `6ae6f24e8b801d5727fbb8b4f9473c5dbe045c364094ea8993d6051fd5cb6ed4` |
| DLL | SHA-256 `90ce738cc92f7156114b5340f8334ebb8f90ce459353413edba53bc097ed1ed9`, MVID `07569a08-9fdc-4647-b63c-705e95c5bb90` |
| Fixture | `Manual_403_KBP_AUTOMATION_WORKING.zks`, re-hashed by the launcher on every run (never trusted by name) |

## 3. Selection: none is possible on the WORKING fixture yet

Two selection-only live runs found **no eligible probe casting**. Both
used `live-cast-probe-select` with the full-user profile and zero
synthetic input. Neither constructed a dispatch boundary. Both closed
the workspace, released the input lease and verified restoration.

| Run | Result |
| --- | --- |
| `casting-probe-select-20260923-052640` | 8 target candidates, no reasons recorded (led to the diagnostics commit) |
| `casting-probe-select-20260923-053136` at `2117438` | 8 target candidates plus skipped options, every one rejected with a reason |

The recorded reasons from the second run:

- **Every spellbook option is a level-0 spell (a cantrip).** A cantrip
  has no native resource cost. The converter requires exactly one native
  pool cost line per step, so it refuses with
  `native-cost-count:probe-cast-1:0`. Cantrips therefore cannot be
  projected in the standard scope either. This is a real gap, not a
  probe artifact.
- **Every other option is a fact-sourced ability**, which the probe
  subset excludes (`source-kind-or-metamagic`).

So there is no projection id to approve yet. There are two ways forward,
and both are the owner's decision:

1. **Model zero-cost native casts** (recommended). Teach the converter an
   explicit zero-cost native contract for cantrips (no reservation,
   expected resource delta 0), keep every other probe restriction, and
   rerun the selection. The pass rule would then observe the effect and
   an unchanged pool. This is source work within current authority; only
   the cast itself needs approval.
2. **Use a fixture with a level-1 buff slot.** This changes the approved
   fixture and needs explicit owner approval.

## 4. The allowance file (the owner writes it; Claude does not)

Once a selection run reports a projection id, the allowance uses the
values from that run's `probe-selection.json`.

Save it under the lab approvals directory as `<run id>.json`:

```json
{
  "schemaVersion": 1,
  "kind": "kbp-single-cast-probe",
  "runId": "<run id>",
  "sourceCommit": "<the commit the probe runs at>",
  "approvedProjectionId": "<64-hex projectionId from probe-selection.json>",
  "casterUnitId": "<casterUnitId>",
  "targetUnitId": "<targetUnitId>",
  "sourceId": "<sourceId>",
  "maximumNativeSubmissions": 1,
  "approvedBy": "Howie"
}
```

Then the run is:

```powershell
& 'scripts/Invoke-KingmakerRuntimeTest.ps1' -Scenario live-cast-probe `
  -CompatibilityProfileId full-user -RunId <run id> `
  -ProbeAllowancePath 'C:\Dev\KingmakerBuffPlannerLab\approvals\<run id>.json' `
  -TimeoutSeconds 900 -Confirm:$false
```

The allowance is consumed by that one run. It cannot be reused: the run id
is single-use, and the boundary refuses a second submission.

## 5. Observation and pass rule

- **Before:** the selected pool's remaining count (recorded at selection).
- **One submission** through the coordinator. The executor itself
  requires out-of-combat, validates the target, fires the rule cast,
  observes the expected effect on the target and the resource delta, and
  settles or cleans up delivery state.
- **After:** the pool's remaining count from a fresh discovery.
- **PASS** only if the allowance was valid, the one casting is
  `EffectConfirmed` with no failure record, the outcome reports the
  approved projection id, the pool dropped by exactly the reserved units,
  and the workspace closed with the input lease released.
- **Deadline/stop:** 60 s or `probe-stop.json` disposes the run
  (executor cleanup runs) and the result is FAIL, never a retry.
- **Restoration:** the existing transaction restores Mods and verifies
  the fixture hashes. That does not undo the in-process effect or the
  spent slot. The WORKING save is never written by the scenario.

## 6. What a PASS would and would not mean

A PASS proves one native cast of one plain spell through the
casting-first projection on the WORKING fixture, and clean restoration.
It does not qualify rods, metamagic, enhancements, modifiers, groups,
multi-cast routines, animated mode, or any release.
