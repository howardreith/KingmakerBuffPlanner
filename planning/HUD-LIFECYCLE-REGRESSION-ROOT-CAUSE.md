# 0.0.12 HUD Lifecycle Regression Root Cause

Date: 2026-08-24

Branch: `codex/kbp-hud-lifecycle-hotfix-0.0.12`

Starting branch: `main`

Starting and fetched `origin/main`: `4a83aec19e0f6098e23b2965b3992c328136c576`

Starting version: `0.0.11`
Starting worktree: clean

## Intake evidence

The baseline was captured before production changes with:

```powershell
git status --porcelain=v2 --branch
git rev-parse HEAD
git fetch origin main --tags --prune
git rev-parse origin/main
git diff v0.0.10..v0.0.11 -- src/KingmakerBuffPlanner/UI
```

The `v0.0.10` to `v0.0.11` comparison confirms that commit
`f23a07b7560b2aa4cd7b3d1635436c3abffd575a` correctly replaced the global
`FindObjectOfType<IngameMenuController>()` update-path search with a scoped
`hudHost.GetComponentInChildren<IngameMenuController>(true)` lookup dispatched
through `HudInstallInvalidationGate`. That performance boundary remains required.

The same comparison confirms the lifecycle regression:

1. `HudInstallInvalidationGate.ObserveHost` consumes its request before the
   installation outcome is known.
2. `BuffPlannerHudButtonController.TryInstall()` returns `false` both for
   retryable native-HUD readiness failures and for a successfully constructed
   provisional candidate.
3. `BuffPlannerUiRoot.Tick()` ignores that distinction.
4. A provisional candidate may destroy its owned root after 120 failed deferred
   validation frames, but the controller cannot report expiry to the gate.
5. The unchanged outer `UISectionHUDController` then supplies no new invalidation,
   so the controls can disappear permanently.
6. `IsInstalled` checks only `_installed && _root != null`; it does not reject an
   inactive/detached root, inactive/replaced inner anchor or native cluster, or a
   candidate that no longer belongs to the active outer HUD hierarchy.

The update log at
`%USERPROFILE%\AppData\LocalLow\Owlcat Games\Pathfinder Kingmaker\output_log.txt`
(last written 2026-08-24 12:00:57 local time) contains additional concrete
evidence. Lines 3342-3392 show `OnAreaBeginUnloading` cancelling the request,
followed by candidate `-431240` being created under
`StaticCanvas/HUDLayout/Menu_Buttons48px` while loading was still in progress,
then repeated deferred validation failures before the area-loaded callbacks.
The run exited before the source-defined 120-frame expiry, so it does not prove
the final expiry transition directly. It does prove that host observation can
override unload cancellation and construct against a transient loading HUD.

No other normal-load owner was found. The complete owned-root disposal paths are
candidate expiry/staleness inside `BuffPlannerHudButtonController`,
`OnAreaBeginUnloading`, `OnDisable`, mod disable/unload, explicit runtime root
reconstruction, and exception recovery in the outer UI update. The first four
are lifecycle-relevant; runtime reconstruction is harness-only.

`Ctrl+Shift+B` calls `HudInstallInvalidationGate.Request()` in `HandlePlannerHotkey`,
so source inspection confirms that it can re-arm a consumed request. The available
log does not contain a post-disappearance hotkey attempt, so an actual runtime
return of the buttons is not claimed from this intake evidence.

## Rejected or bounded theories

- The `v0.0.11` performance repair itself is not reverted: the global object
  search was the measured `v0.0.10` slowdown and scoped discovery is correct.
- Deferred placement, glyph, and raycast validation is not removed. The failure
  is loss of lifecycle feedback and retryability, not proof that those safety
  predicates are invalid.
- No evidence identifies catalog, planning, execution, persistence, targeting,
  metamagic, or compatibility code as part of this regression.
- The recent log ends at process shutdown before candidate expiry. Source control
  flow proves that expiry is terminal under an unchanged outer host, but fresh
  campaign runtime evidence remains conditional on authorized save fixtures.

## Implemented repair and qualification

Replace the Boolean installation contract with explicit attempt and candidate
outcomes; evolve the invalidation gate into a lifecycle state machine with a
bounded active-HUD retry cadence; suspend discovery across unload/disable; report
candidate expiry and stale hosting chains to the owner; and retain zero discovery
for absent, stable-installed, and live-candidate steady states.

That design is implemented in commit
`376e4a153753ae45fd2daad447790b4bd2c31590`; exact qualified artifact source is
`083dfbfcf651d44bb01b302ccbbabac823e236e9`. Deterministic tests pass 91/91 and
cover retryable readiness, exact expiry/re-arm, stale hosting chains, performance
steady states, lifecycle transitions, disable/enable, unloading/loading, and
hotkey invalidation. Source guards reject global HUD searches and native UI
destruction.

Guarded no-save performance `hud-lifecycle-0.0.12-performance-1` passes with
zero searches and 88.780 average FPS; native and Call of the Wild catalog runs
pass. Campaign persistence of the row cannot be observed because the authorized
save resolver returns baseline=0 and working=0. No unrelated save was
substituted. The exact candidate was later guarded-installed only after separate
human authorization. The owner accepted that installed campaign build;
its log proves the same provisional candidate progressed from a loading-screen
raycast failure to `Installed` after 57 validation frames with four buttons and
four listeners, followed by successful Setup presentation. The bounded evidence
is in `planning/HUD-LIFECYCLE-0.0.12-HUMAN-ACCEPTANCE.md`. The accepted repair
was subsequently published as `v0.0.12`; exact release evidence is in
`planning/HUD-LIFECYCLE-0.0.12-RELEASE.md`.
