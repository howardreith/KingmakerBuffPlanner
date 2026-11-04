# Cast available default — development checkpoint

Prepared November 4, 2026. Branch:
`codex/kingmaker-buff-planner-cast-available-default`.
Inspected/current main: `52f66c895fc98d7a05395d7402bcfbefa0cb7a57`.
Committed product and exact HEAD at verification:
`3feceb5087bc7eeb5d85da095cec3841c622f883`.
Assembly version remains **0.6.9**; this is unreleased feature source, not the
published 0.6.9 binary. Later documentation/publication commits do not change
the verified product source. Publication receipt is recorded below.

## Player behavior and migration

Ordinary HUD, mouse and controller Run resolve the saved policy, defaulting to
Cast available. The existing fresh compiler, ReadyCastsOnly gate, resource
ledger, selected execution mode and authored order admit the executable subset.
No alternate providers, resource types or weaker enhancements are substituted;
saved assignments remain intact. Previews and Resources use the same policy.

The native **Require full plan** checkbox sits in the established execution
footer beside Resources and Mode. It is unchecked by default. Its help reads:
“Do not start this routine if a needed planned casting is unavailable.” Checking
it selects Ordinary admission: actual unavailable blockers prevent every
submission, and otherwise-ready entries are reported as withheld. Already-active
and disabled entries are benign. Readiness is checked before execution; there
is no rollback promise after native work starts.

Schema 6 **format 6.5** is the durable migration marker. Supported older files,
including serialized strict preferences, initialize Cast available once. They
did not record whether strict was deliberately chosen; those users must enable
it once after upgrading. Current valid choices persist exactly. Missing/null/
unknown optional policy strings normalize to available. Classic import normalizes
inherited settings before stamping; current-session explicit choices and pending
recovery preserve the effective policy. Document Undo does not own this setting.

Loading/previewing writes nothing. Migration remains pending and not durable
until the normal canonical save succeeds. The existing barrier flushes before
native submission and refuses if saving fails. Backup and protected-primary
behavior, atomic writes, archives and future-format refusal remain in place.

Every deliberate no-run admission and terminal run outcome uses shared visible
game-log summaries and named detail lines. Labels/reasons are frozen at admission;
numbered fallbacks retain unresolved entries and distinguish repeated invocations.
No omission-count truncation is applied. Refreshes/handoffs do not emit and old
completion IDs cannot replay reports. An open editor does not suppress logging.
Availability/projection refusals do not open the planner; explicit opening and
unrelated recovery/import workflows keep their existing behavior.

## Actual fixture output

These lines were emitted through the production session presentation boundary
with a substituted native log sink. They are source-test observations, not
screenshots or native gameplay proof. The prepared example uses a simulated
Heroism-like source, not a native Heroism repair.

```text
Buff Planner: Long finished with omissions - 2 cast, 2 unavailable.
Not cast: Heroism-like fixture (unit-wizard -> unit-t2) [casting 2] — not enough prepared uses; earlier planned castings have first claim on this pool.
Not cast: Heroism-like fixture (unit-wizard -> unit-t3) [casting 3] — not enough prepared uses; earlier planned castings have first claim on this pool.

Buff Planner: Long — 0 cast, 2 unavailable, 1 disabled.
Not cast: Buff (Linzi -> Valerie) [casting 1] — no level 2 spell slots remain.
Not cast: Buff (Linzi -> Valerie) [casting 2] — no level 2 spell slots remain.
Not cast: Buff (Linzi -> Valerie) [casting 3] — explicitly disabled.

Buff Planner: Long — 0 cast, 1 unavailable, 1 disabled, 1 withheld by Require full plan.
Not cast: Buff (Linzi -> Valerie) [casting 1] — ready, withheld by Require full plan.
Not cast: Buff (Linzi -> Valerie) [casting 2] — not enough level 2 spell slots; earlier planned castings have first claim on this pool.
Not cast: Buff (Linzi -> Valerie) [casting 3] — explicitly disabled.
```

## Focused validation

Tier: ordinary offline development, per [validation policy](VALIDATION-POLICY.md).
Exact command at the product HEAD above (PowerShell, repository root):

```powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass -File scripts/Test-Development.ps1 -Only 'cast-available,human-feedback-availability-policy,human-feedback-resources,refusal-feedback-rules,native-ui-game-log-lines,native-ui-planner-copy,heroism-repeat,sticky-qual-route-markers-are-the-instant-adapter-record,run-presentation-separates-effects-and-spending,qualification-enhanced-direct-recipe,qualification-rod-extend-recipe,sticky-qual-driver-passes-both-modes-and-pool-kinds,execution-policy-settled-touch-detail-survives-confirmation,confirmation-needs-an-instance-this-attempt-applied,controller-policies,controller-results-include,controller-latest-refusal,controller-latest-session,controller-configure-author,blocked-navigation,autosave,failed-flush-intent,rollback-candidate-format,execution-host-runs-reports-and-halts,execution-host-cancel-deadline-shutdown,casting-a07,casting-a08,casting-a10,casting-a13,reload-without-a-plan-file'
```

Result: **88 PASS / 0 FAIL**, **11.5 seconds**, with source validation
**47/0** and Release production build **1/0**, C# 7.3 / .NET Framework 4.7.
Includes 14 new cast-available cases, both executor modes, the 24 existing
Heroism stateful cases, shared-budget/enhancement, controller, navigation,
autosave/recovery and format rollback-model neighbors. A 150-entry unavailable
routine retains all 151 visible lines, and a depleted selected preparation is
distinguished from other uses still present in the same pool.

Local evidence: `artifacts/cast-available/3feceb5-development.log`.
Receipt SHA-256:
`a2bf4b08af564ea28cf7f16aacf890e187897809fae48317acd3340e2bfcf890`.
DLL: `artifacts/build/Release/KingmakerBuffPlanner.dll`, SHA-256:
`1fe8bf7838063389842a9b414df6e8e015737389f038b4199165e70ff267ab35`.
Generated artifacts are ignored, not published packages.

Review corrections retained: constructor-only default changes were rejected
because legacy strict bytes need one migration; marking normalized settings as
already saved was rejected; replaying LastRunReport for a new no-op was rejected.
Initial focused runs exposed obsolete strict/opening expectations, two fixture
setup mistakes, and strict navigation fixtures missing an explicit saved opt-in.
Those were corrected without relaxing runtime assertions. The final receipt
above supersedes intermediate failing development runs; historical qualification
receipts elsewhere are unchanged. Diff inspection and text/link checks accompany
the documentation commit. No full aggregate, layout matrix or packaging campaign
was selected.

The first source checkpoint `4c0c12fe413edd8af4f0a4c2421fb6593822ab88`
passed 87/0 in 14.7 seconds. Receipt and original hash remain at
`artifacts/cast-available/4c0c12f-development.log` (SHA-256
`77778d81245f7bc18c3754058e833fab9501b7372302b1246c28845adad4b57a`).
Final review found that choosing only the policy before Classic import could
override an unrelated inherited mode. Commit `3feceb5087bc7eeb5d85da095cec3841c622f883`
separates explicit mode/policy overrides and preserves both through pending
recovery. Correction reproduction/neighbors: 8/0 in 7.7 seconds, source 47/0,
build 1/0, `artifacts/cast-available/import-choice-correction.log`, followed by
the final committed 88/0 pass above. An attempted banner edit to the authoritative
mission copy was rejected by source validation before compilation; that edit was
restored exactly. The byte-identity guard and mission copy remain unchanged.

Documentation verification: local Markdown targets **296/0** at the initial
documentation pass, **285/0** after restoring the authoritative mission copy;
`git diff --check` passed. Evidence:
`artifacts/cast-available/documentation-checks.txt`. The link check extracts
Markdown link targets in all changed documentation plus this checkpoint, skips
web/fragment targets, resolves each local path relative to its file and requires
`Test-Path -LiteralPath` for every target. These are text/link checks only.

## Guarded draft PR delivery

Documentation/delivery snapshot: `1d04149254122aab7e450f88239513cc452126da`.
[Draft PR #19](https://github.com/howardreith/KingmakerBuffPlanner/pull/19)
is open against main `52f66c895fc98d7a05395d7402bcfbefa0cb7a57`, with the
product unchanged at `3feceb5087bc7eeb5d85da095cec3841c622f883`.

```powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass -File scripts/Test-GuardedPush.ps1
powershell.exe -NoProfile -ExecutionPolicy Bypass -File C:\Dev\KingmakerBuffPlannerLab\codex-policy\Push-KingmakerBuffPlanner.ps1 -RepositoryRoot C:\Dev\KingmakerBuffPlannerLab\repo\KingmakerBuffPlanner
```

Guarded helper tests: **6 PASS / 0 FAIL**, 1.8 seconds. Guarded push and exact
remote-HEAD verification passed, 3.9 seconds. The first sandboxed read-only
check could not reach GitHub; the authorized network-enabled retry passed.
A prior push invocation stopped at PowerShell switch-argument binding before
any Git mutation; invoking the helper with its ordinary defaults succeeded.
PR creation used the connected GitHub plugin, explicitly with `draft: true`.
Evidence: `artifacts/cast-available/guarded-push-tests-network.log` and
`artifacts/cast-available/guarded-push.log`. The final receipt-only commit is
published by the same clean-tree guarded route. No merge or release was made.

## Native acceptance and exact next action

**Native rendering/input/gameplay acceptance: NOT RUN.** No live staging,
installation, launch, save mutation or release publication was performed.
This checkpoint ends at focused development; it supplies no launcher purity,
runtime allowance, native spending/reuse or Linux acceptance receipt.

Review the draft PR, then separately select the existing guarded native workflow
on a frozen candidate. Satisfy its launcher safety/source and WhatIf purity,
transaction/staging locks, allowance and disposable-save requirements first.
Observe: default HUD mixed affordable/exhausted casts with readable visible log
and no forced planner; all-unavailable and opted-in strict attempts with zero
spend; mouse/controller/authoring parity; checkbox state and help; persistence
after reopening; prepared/spontaneous pools and native log detail ordering.
Verify no focus/pause/input interruption on refusals and no replay on a second
fully-covered exhausted-source Run. Preserve exact restoration.

Protective halting after late validation, unconfirmed application or uncertain
cleanup remains unchanged, including observed spending and confirmation evidence
plus original native records. The separate
[Heroism investigation](HEROISM-REPEAT-CAST-REPAIR.md) remains open: neither this
feature nor the published 0.6.9 establishes a native Heroism repair or native
prepared-slot reuse between runs without replenishment. Do not merge or publish
a release as part of this mission.
