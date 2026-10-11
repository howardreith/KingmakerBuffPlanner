# Current native UI development checkpoint - October 11, 2026

Read [validation policy](docs/VALIDATION-POLICY.md),
[workflow reset](docs/WORKFLOW-RESET-HANDOFF.md), and the
[native UI handoff](docs/NATIVE-UI-FINISH-0.5.0-CODEX-HANDOFF.md).
The owner's October 11 policy supersedes automatic broad-gate, packaging and
six-hour qualification sequences in older records. Ordinary work uses explicit
Test-Development prefixes and cheap launcher argument checks (15-minute target,
30-minute ceiling). Real launches retain all existing safety and purity requirements.

DATA development worktree: `C:\Dev\KingmakerBuffPlannerLab\repo\KingmakerBuffPlanner-NUI`;
branch `codex/kingmaker-buff-planner-native-ui-finish-2026-10-10`; handoff HEAD
`fa5f4a277558c998255b74d839eb831850c3ee29`. Incorporated `origin/main`
`358418f6f86ef0c34687a2f4b96374042c7393d2` (PR #9) without changing main.
Version remains 0.5.0. Current clean built source HEAD `673d1e3a62356af3d1b4d5dcbd19f233fd521734`; workflow merge
`0463f553a859ecc195f862fba9632ee91586e08c`. Launcher consumer repair
`3af7171a70f95ef13da3359245d00b030bc62843`: real shared -File checks 8/0 (1.542 s),
original direct argument checks 24/0 (0.263 s), source 43/0, builds PASS and affected
C# 6/0 (7.712 s). Guarded-push tests 6/0 (2.042 s). Selector behavior is unchanged.
See the [current review checkpoint](docs/NATIVE-UI-FINISH-0.5.0-REVIEW-CHECKPOINT.md)
for commands, identities, limits and next action; the earlier checkpoint preserves
its original 12/0, selector 6/0 and initial 11/1 fixture result.

The one-time gate audit at 2026-10-11T16:08Z found the frozen `4dbfa33`
Test-SourceOnly process ACTIVE (PID 16712, created 12:49:38Z), no GATE-EXIT,
last log write 14:33:28.9347261Z. Preserve NUIR and all watched runtime paths;
do not start a conflicting scenario or poll/restart this gate. Independent source
work continues in NUI. No live runtime work is selected for this checkpoint.

Fallback package remains built from `4dbfa33e1b36bf59bc9bb1a4ac3ebde860c52302`,
ZIP `cf658914130a010d7525ee47bc41a1f5c05f9bcde929852a1e31430ab3be8fcb`,
DLL `b32f320ecebd1288eb409cec4893953f2e685a1acad0898de1b8f03164e58e69`.
Its four final native runs retain their original attribution; receipt remains
PARTIAL. `3757758` changes records; `57f136a` changes product caption layout and
is absent from the fallback package. New observer changes also require affected
observations on their own bytes before any native acceptance claim.

Implemented: combined early launcher refusals/stages, the corrected broad native
fixture assertion and focused shared child-process check, caption/popup/filter
observations and changed-caster capture, plus schema-3 Metamagic action and key-route
preconditions. Schema 1/2 retain their original verdicts. New observations are NOT_RUN
in game. Clean 673d1e3 development DLL SHA-256
`ca3d6bc5a4a674397e1baab60601c972dc008086d93c247c7451ca97fb8a7250`,
MVID `a956e985-a762-4994-84e6-07f586a947ec`, version 0.5.0; no package/runtime receipt.

Next action: review the coherent changes and inspect the live prerequisite call graph;
once the old gate is verified exited at an explicit runtime boundary, select the
smallest guarded two-size native observation batch, declaring prerequisite cost and
retaining all safety guards. Inspect its screenshots. Physical keys, paused switching,
real disabled states and fixture/manual limits remain in the current checkpoint.
Final frozen-candidate qualification is deliberately deferred.

Historical chronology is preserved in
[Claude's native UI resume](docs/history/NATIVE-UI-FINISH-RESUME-at-Claude-handoff-2026-10-11.md)
and [pre-reset resume](docs/history/AUTONOMOUS-RESUME-before-workflow-reset-2026-10-11.md).
