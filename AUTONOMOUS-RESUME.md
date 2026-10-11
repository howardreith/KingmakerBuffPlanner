# Current native UI capture checkpoint - October 11, 2026

Read [validation policy](docs/VALIDATION-POLICY.md),
[workflow reset](docs/WORKFLOW-RESET-HANDOFF.md) and the
[current capture checkpoint](docs/NATIVE-UI-FINISH-0.5.0-CAPTURE-CHECKPOINT.md).
The owner policy supersedes automatic broad/six-hour sequences in older records:
ordinary explicit test prefixes, 15-minute planning target / 30-minute ceiling.
Live launcher/source/WhatIf, ownership and restoration safeguards still apply.

DATA development worktree: `C:\Dev\KingmakerBuffPlannerLab\repo\KingmakerBuffPlanner-NUI`;
branch `codex/kingmaker-buff-planner-native-ui-finish-2026-10-10`. Starting reviewed
HEAD 9dd89974ddec4eea6ab8d19e05665a8486461ef5. Clean product/observer source and
package commit `7410f92604621aabfba97e26cb1c9f6f17044b63`, version 0.5.0.
Workflow merge 0463f55 includes origin/main 358418f6 and repair f658726.

Capture requests now hold the displayed state until a later frame and complete,
readable fresh PNG, then Return/Escape/next-mode changes run in a later Update.
Ten-second timeout records failure and uses existing cleanup. Related sequences
are corrected together. Focused development: source 43/0, builds PASS, C# 9/0
(5.108625 s); local builder 3.361913 s, package validation 4/0. Guarded push tests
6/0. Previous launcher 24/0 direct and 8/0 shared child checks are unchanged.

Local package ZIP SHA-256
`1d75a0038d2f99eaa55ebc9b774a2c9e72211e8af0991258fd24fd1728299088`, DLL
`568dec0091a13f65cecfeb4a262c3f762cbae75b04dbfe996f437f97df65d1c6`,
MVID `95959d77-29f9-488a-87cd-a55fb5ca5eb4`. Compiled identity verified.
Preserved under `artifacts/native-ui-capture-2026-10-11/`; packaged, NOT_RUN,
never staged. Later documentation/publication HEAD does not relabel these bytes.

One-time 17:56Z boundary audit: old frozen 4dbfa33 gate has exited and its log
ends with the old decision-refusal assertion at FileWhatIf:104 (17:08:42Z).
Status FAILED; numeric exit unrecorded. PID 16712/owned children absent, no
runtime/fixture/foreign locks found, 495 transaction records Restored. No
cancellation or rerun; NUIR and watched paths preserved. The 16:08Z ACTIVE audit
and prior unobserved-completion checkpoint remain dated historical results.

Receipt stays PARTIAL. Fallback remains source 4dbfa33/version 0.5.0, ZIP
`cf658914130a010d7525ee47bc41a1f5c05f9bcde929852a1e31430ab3be8fcb`, DLL
`b32f320ecebd1288eb409cec4893953f2e685a1acad0898de1b8f03164e58e69`.
Its four original in-game PASS/restoration receipts keep their attribution;
the old aggregate gate is FAILED. Caption fix 57f136a and newer observations
require game evidence on the new bytes. No criterion is promoted by offline tests.

Next: separately budget the unchanged current whole-history WhatIf purity route
and the selected 1920x1080/1280x720 native qualify batch, on clean source 7410f92;
verify restoration and inspect actual captures against R01-R15. A read-only
1,038-file manifest sample took 8.41 s; 374,395 current files before/after imply
about 101 minutes for required inventories. This exceeds the ordinary ceiling;
no live-purity window, gate, game or publisher was launched. No receipt reuse or
pruning is authorized by this checkpoint. Physical/remapped input, paused
switching, real availability and fixture/manual limits remain in the current
checkpoint. The package is ready for that explicit milestone without restamping.

Previous source 673d1e3 and launcher repair 3af7171, exact earlier commands and
identities are in the [previous review checkpoint](docs/NATIVE-UI-FINISH-0.5.0-REVIEW-CHECKPOINT.md).
The original resume chronology is retained in
[Claude handoff history](docs/history/NATIVE-UI-FINISH-RESUME-at-Claude-handoff-2026-10-11.md)
and [pre-reset history](docs/history/AUTONOMOUS-RESUME-before-workflow-reset-2026-10-11.md).
