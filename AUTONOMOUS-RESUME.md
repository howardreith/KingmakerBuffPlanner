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
`fa5f4a277558c998255b74d839eb831850c3ee29`. Incorporating `origin/main`
`358418f6f86ef0c34687a2f4b96374042c7393d2` (PR #9) without changing main.
Version remains 0.5.0. Current source HEAD `a652441b07bb1f427c42e3036b10e67d8e9163d1`; workflow merge
`0463f553a859ecc195f862fba9632ee91586e08c`. Launcher arguments 24/0, selector 6/0,
source 43/0 and focused C# 12/0 pass. See the
[development checkpoint](docs/NATIVE-UI-FINISH-0.5.0-DEVELOPMENT-CHECKPOINT.md)
for exact commands, costs, artefact identities, initial 11/1 fixture failure and limits.

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

Implemented: combined early launcher refusals and native UI stages, offline caption
regression, schema-2 actual caption/popup/filter observations and changed-caster capture.
New observations are NOT_RUN in game. Next action: inspect the exact key-dispatch and
Metamagic contracts and add bounded paused-window/Metamagic observations, using
native-window, native-ui-record and spellbook-handoff prefixes; later select a
small guarded two-size native observation batch after the old gate exits.
Final frozen-candidate qualification is deliberately deferred.

Historical chronology is preserved in
[Claude's native UI resume](docs/history/NATIVE-UI-FINISH-RESUME-at-Claude-handoff-2026-10-11.md)
and [pre-reset resume](docs/history/AUTONOMOUS-RESUME-before-workflow-reset-2026-10-11.md).
