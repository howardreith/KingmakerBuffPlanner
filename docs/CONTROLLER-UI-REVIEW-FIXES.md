# Controller PR review corrections

October 17, 2026. PR #10 review base
`0539c081db0a6c75d57e5df95805900686e6e2fb`. Full controller editing remains
deferred; native/physical qualification remains pending. This correction does
not promote any offline result to live acceptance.

## Corrected behavior

1. Entry lifetime: `ControllerEntryRegistry` records each borrowed native layer
   and the exact `BindDescription` objects returned by `AddButton`. Repeated
   installation on that layer is idempotent. Removal gates captured callbacks,
   detaches only the matching Rewired event delegate, clears the owned callback,
   removes those exact bindings and clears an action slot only if it still
   points to that binding. A newly conflicting third-party entry is preserved.
   Uninstall attempts every owned registration and still removes hints/patches
   if cleanup fails. No whole-layer unbind/rebind or native ownership snapshot.
2. Persistence: the controller header now uses `FooterSaveState` and the shared
   `WorkspaceFooterText` label/explanation. Corrupt, unsupported, blocked legacy
   import and backup-over-unreadable-primary states say **Not saved** before Run.
   Reading and navigating do not save/reload/repair those files.
3. Latest outcome: selector reconstruction reads `LastAttemptMessage` before
   `LastRunReport`. A refusal remains visible, with repair/Back guidance, until
   the canonical session records a later completed report. Reopening presents
   the canonical refusal summary; the immediate refusal additionally contains
   per-casting blocker details. No second outcome store or executor was added.

## Focused evidence

```powershell
.\scripts\Test-Development.ps1 -Only 'controller-,execution-policy-,production-apply-end-to-end-through-host,host-stops-between-castings,host-player-stop-,autosave-,failed-flush-,casting-workspace-campaign-bound-,native-window-takes-,planner-hotkey-,input-lease-,classic-file-and-hud-,refusal-feedback-,casting-world-clock-,first-open-import-,hud-routine-buttons-,planner-escape-,footer-save-status'
```

Final fix-tree pass: **43 PASS / 0 FAIL** (17 controller, 26 affected neighbors),
source validation 43/0, production build 1/0, test build successful; 6.3 s
reported, 6.5082444 s child wall. Log:
`artifacts/controller-ui/review-final-fixes.log`. All prefixes matched.

The two new presentation reproductions initially failed (0/2) on the reviewed
implementation; `review-reproduction.log` preserves that result. Intermediate
fix passes retained a test focus-setup error (16/1), then an existing explanation
assertion (42/1); these were corrected, not reclassified as baseline failures.
No validator was weakened. The retained-layer behavior test substitutes the
device registration boundary, while a second regression executes production
detach code against actual installed, **unbound** native layer/binding types.
It verifies callback neutralization, idempotent removal and foreign identities.
It does not execute a live Rewired subscription or Harmony menu handoff.

Exact native signatures/IL were re-inspected using the repository inspector;
private evidence: `artifacts/controller-ui/review-native-contracts.private.txt`.
Assembly-CSharp MVID remains `07fa1e4d-8618-41b3-9b8d-faa17d3b26f7`.
The verified native unbind uses `RemoveInputEventDelegate` with Update loop,
event type and action identity. No game assets/source are committed.

## Candidate and runtime boundary

The original `d04c1a781770194966fa55df15000d627e14662d` /
`0.5.2-controller.1` [candidate receipt](CONTROLLER-UI-CANDIDATE-0.5.2-CONTROLLER.1.md)
and its ZIP/DLL/MVID remain historical, unchanged, and NOT_RUN. It contains the
reviewed defects and is not the next live candidate. These fixes need their own
post-integration product/artifact identity; no build is relabeled as the old ZIP.
Resolve the fix checkpoint through this document's introducing commit.

First native observation must be entry/exit only: fresh controller startup,
native campaign menu -> planner, older native menu closes while planner stays
visible/current, navigate without running, Back and neutral, normal gameplay
input returns. Observe actual `OnUnbind` ordering/current layer and world input;
signature/IL checks alone do not prove this. Only then select finite-resource
execution/refusal/Stop/held-input/lifecycle observations. Preserve all existing
launcher, save, settings, allowance and restoration guards. No broad roster
campaign is selected. Runtime/restoration: NOT_RUN.
