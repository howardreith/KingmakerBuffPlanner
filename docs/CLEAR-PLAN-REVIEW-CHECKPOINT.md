# PR #17 input review corrections — 2026-11-04

Branch `codex/kingmaker-buff-planner-clear-plan`.
Reviewed HEAD `502df08c3a8a765f0663de7fb65b99cf1efe2a34`.
Regression/adapters commit `6aa523e8f1733d7e8553a60da021585145654428`;
corrected product/test commit `8de9e8ec0894e3bcce51329d9b541fdd660b250a`.
Version stays 0.6.8. This supersedes the input-safety conclusions of the
[original checkpoint](CLEAR-PLAN-CHECKPOINT.md), while retaining its evidence
and the existing authoring/Undo/persistence implementation unchanged.

Both review findings were reproduced in detached production-path tests.
The confirmation had captured the controller editor's intermediate
`sendNavigationEvents=false`; its later disposal overwrote the outer owner's
restoration to true. The keyboard poll also ignored native description ownership,
allowing Enter/Space and navigation to reach the retained workspace focus.

The confirmation now borrows EventSystem selection only. It never captures,
changes or restores navigation dispatch: `ControllerEditorInputBoundary` and
the pointer-mode semantic navigator already own that setting throughout the
workspace lifetime. Removing the redundant nested write fixes both open-dialog
and post-dismissal release-wait teardown without changing outer teardown order.

Native descriptions now block workspace keyboard polling, semantic command
delivery, Tab traversal and authoring callbacks while open and through their
closing input transition. The existing neutral-frame latch requires release
before fresh input resumes. The native presenter's successful-open callback
records ownership even if the window opens and closes between planner polls.
Escape records the closing transition as well. Focus is preserved; no global
spatial-navigation or disabled-control behavior changes.

## Regression scope and limits

The new tests load the **compiled production DLL** and use detached instances
of the actual `BuffPlannerUiRoot`, `BuffPlannerInputLease`,
`ControllerEditorInputBoundary`, `CastingWorkspaceScreenView` and
`ControllerWorkspaceNavigator`. They execute the real focus-loss callback and
cleanup order, plus the real keyboard poll and semantic dispatch against a real
session with an enabled Undo. They check both original navigation values,
both modal teardown phases, blocked Enter/Space, repeated/queued closing input,
blocked arrows/Tab/direct navigator delivery, between-poll opening/closing,
and successful fresh input/Undo afterward.

Rendering, device polling and native description visibility are genuine external
boundaries supplied by the fixture. Constructors that create Unity objects are
not executed. Captured native input fields are seeded; native presentation
callbacks are inert. Native cursor/root/background calls were separated from
managed lifecycle work so the same production path can run detached. These are
behavior checks of the compiled view/navigator/cleanup, not visual or physical
controller acceptance and not a game reproduction.

## Checks and evidence

Windows PowerShell, official MSBuild, C# 7.3/.NET Framework 4.7 and installed
Kingmaker assemblies; no new dependency/framework, assertion weakening or
portable source exclusions. Under the [validation policy](VALIDATION-POLICY.md):

```powershell
# Before behavioral fixes, after introducing external input/rendering adapters:
scripts/Test-Development.ps1 -Only 'clear-plan-view-'
scripts/Test-Development.ps1 -Only 'clear-plan-view-focus-loss-'
# Corrected committed product:
scripts/Test-Development.ps1 -Only 'clear-plan-,controller-workspace-,controller-input-,controller-editor-back-,planner-escape-'
git diff --check
```

| Check | Result |
| --- | --- |
| Initial detached harness | 0 PASS / 5 FAIL, Unity ECall infrastructure failures; not defect reproductions |
| Intermediate reproduction | 0 PASS / 5 FAIL: three description behavior failures, two remaining ECall fixture failures |
| Teardown reproduction after separating native CanvasGroup call | **0 PASS / 2 FAIL**, both wrong-navigation assertions |
| Corrected working-tree pass | 38 PASS / 0 FAIL |
| Final focused gate on 8de9e8e | **39 PASS / 0 FAIL**, 8.6 seconds |
| New production-path regressions in final gate | **6 PASS / 0 FAIL**, included in 39 |
| Source validation / complete production build | **47/0** / **1/0** |
| Prefix audit | Five nonzero prefixes, 39 registered tests |
| `git diff --check` | PASS |
| Native mouse/controller/visual acceptance | **NOT RUN** |

The baseline runs used reviewed 502df08 plus the external-boundary/reflection
fixtures subsequently committed as 6aa523e; no behavioral fixes were present.
The full DLLs were compiled, but their source stamp remained 502df08 during
those working-tree reproductions. Do not relabel them as clean baseline builds.
The additional between-poll case and direct queued-command assertions belong
to the corrected fixture. Final DLL SHA256:
`09fc0fe3e2491578a007e2e7f30500ec1c46d0bb1a97f39faaca4db92f888e51`.
Later documentation commits do not rebuild or relabel that artifact.

Evidence: [final gate](evidence/clear-plan-review-2026-11-04/focused.log),
[description reproduction and fixture limits](evidence/clear-plan-review-2026-11-04/reproduction.log),
[teardown reproduction](evidence/clear-plan-review-2026-11-04/teardown-reproduction.log),
[prefix audit](evidence/clear-plan-review-2026-11-04/selection-audit.txt),
[hashes](evidence/clear-plan-review-2026-11-04/hashes.txt).
Raw verdicts remain separate; log encoding/whitespace normalization is recorded
with original/normalized hashes and ignored local originals. Rejected fixes:
restore another captured modal value after owner teardown, disable keyboard
support globally, trust EventSystem selection, or release description ownership
immediately without consuming its closing edge.

No game launch, staging, Mods/save mutation, package or release operation.
Native acceptance remains pending behind the unchanged current-product guarded
source/live/launcher-WhatIf prerequisites described in the original checkpoint.
Exact next native actions, in addition to the original clear/Undo flow: start
with native navigation enabled; open the controller editor and confirmation;
alt-tab once with it open and once immediately after dismissing it; verify
native navigation after each teardown. In pointer mode, Tab-focus Undo/Remove/
Run, right-click a buff description, press arrows/Tab/Enter/Space, then dismiss
it with held/repeated input. Verify no underlying action or focus movement until
release and a fresh press, then verify ordinary editing and run behavior. Keep
normal/compact rendering and physical-controller criteria NOT RUN until observed.

Guarded helper tests passed **6 PASS / 0 FAIL** on clean checkpoint
db4fe57ed9e3b8b693e3e9b5404dcfa7429a3d7f, and the unchanged project-owned
helper pushed and verified that HEAD. [Helper result](evidence/clear-plan-review-2026-11-04/guarded-push-test.log).
The description of draft
[PR #17](https://github.com/howardreith/KingmakerBuffPlanner/pull/17) was updated, retaining its
native acceptance caveat. The final receipt is documentation-only; no product rebuild or artifact relabeling. Next: review the corrections and perform the guarded native steps above. Merging/version changes/publication are outside scope.
