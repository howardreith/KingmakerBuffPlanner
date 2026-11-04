# Heroism repeat-cast investigation and diagnostic checkpoint

November 4, 2026. **The reporter's gameplay defect is still open.** This is a
validated development checkpoint, not native Heroism qualification or a claim
that the reporter's configuration is repaired. The owner confirmed that no
current native allowance or reporter log/save is available.

Branch: `codex/kingmaker-buff-planner-heroism-repeat-cast`.
Base: current main `d444b1720fa10704f59bc07fdcd4367f9314856f` (fetched this session).
Reproduction commit: `b232b428e1535bafcace1e128d2ea739cbbf4988`.
Production diagnostic change: `58b4fce0118c781f6e12e34c81b03acc4da6513e`.
Final tested product/test HEAD: `5b384efdf2ada02bfdce2ed43f2c67ad6d842620`.
Version remains 0.6.8. Documentation/publication commits do not qualify new bytes.
The original clean Clear plan branch at `c79154f33837bd348dcf05525e48024c55fe3272`
was preserved. The initial slash-form repair branch was renamed before publication
to coexist with the existing remote `codex/kingmaker-buff-planner` ref.

## What is established

The production-code fixture reproduces a submitted, successful, settled native
observation with an unchanged old buff. The attempted refresh spends one use,
confirms zero applications, and leaves the sentinel unprocessed in both modes.
In this fixture 90 rounds remain while a fresh attempt promises 100: passing the
ordinary half-duration skip policy does not fulfill an explicit refresh.
Stopping is appropriate for this observation; blindly continuing would hide a
failed requested result. The fixture does not establish that native Heroism
produced this state for the reporter.

Two diagnostic defects are established: both executors called an unconfirmed
application "expected effects absent", even when the old buff remained; and
Animated's successful coordinated report retained the final resource record
instead of the confirmation detail. Neither defect proves why the reporter's
cast failed. The installed version, mode, caster, source, target, metamagic,
enhancements, prior buff state and other mods in that report remain unknown.
This is not attributed to a regression in 0.6.7 or 0.6.8.

The prepared representative theory is unsupported: execution resolves reserved
available memorized tokens. Stateful tests exercise distinct prepared tokens,
an independently consumed spontaneous pool, exhausted-source covered skips and
new reservations after expiry/replenishment. A manually cast buff does not prove
the saved source remains usable. No concrete item/enhancement binding defect was
reproduced; the existing exhausted-required-rod skip regression remains selected.

## Changes and outcome boundaries

Both executors now say `expected-application-unconfirmed`. An optional read-only
evidence boundary adds terminal before/after recipient, effect kind/ID, instance
identity, expiry and suppression observations to existing run details. Native
buff observations also include suppression readability, stacking mode, remaining
ticks, caster level and metamagic where readable. Output is limited to 12
recipients and four instances each, with truncation markers. There is one
terminal evidence request per attempt, no new per-frame log stream, and a
diagnostic exception cannot change the verdict. Existing native result, resource
and cleanup fields remain separate. Animated confirmation detail now survives
the coordinator and host into existing `[KBP-CF-RUN]` casting logs.

No application, skip, resource, timeout, cleanup or continuation policy changed.
Confirmed applications continue. Adequate coverage with recast off skips before
submission/spending, including exhausted sources. Recast on submits once through
the selected route; replacement and same-instance later expiry confirm. Native
failure or unconfirmed application still halts, preserves spending, and leaves
later castings unprocessed. No retained-effect warning/success outcome was added.
No Heroism name/GUID exemption, presence-only success, timeout extension, general
per-step replanning, library, save migration or version bump was introduced.

A benign-retention outcome needs authoritative proof of the **full** fresh native
result and the complete supported gameplay shape, settled delivery/enhancements,
and supported resource observations. Existing duration-text estimates and the
skip policy are not that proof. This checkpoint deliberately supplies no such
authorization to continue.

## Focused evidence

The stateful fixture substitutes the game boundary only. It uses the real
session, compiler, apply gate, standard converter, both executors, coordinator,
host and reporting boundary. It rebuilds workspace inputs each run from the same
world's current effects, elapsed time, tokens and replenishment. A separate
sentinel ability/effect proves continuation. It does not use FiniteBuffWorld's
presence-only `EffectsObserved` or its successful `Fire`; it captures a baseline
and passes before/after instances through `AppliedEffectJudgement`.

Twenty-four new named tests cover fresh party casts plus sentinel, fully covered
exhausted pools with no active run, mixed three-skipped/one-runnable coverage,
expiry/replenishment, manual coverage, explicit replacement/refresh, unchanged
insufficient refresh, native failure over old coverage, wrong recipient/kind,
suppressed/missing/unreadable after-state and diagnostic read exceptions. Existing
negative completeness/identity, group, cancellation/deadline/cleanup, resource,
compiler and report neighbors remain selected. No full-source aggregate,
packaging, release or live campaign was selected.

Baseline command (before production edits):

```powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass -File scripts/Test-Development.ps1 -Only heroism-repeat
```

[Retained baseline](evidence/heroism-repeat/baseline.log): source 47/0, build 1/0,
tests **16 PASS / 2 FAIL** (the misleading absence assertions, one per mode).
The initial command did not instrument elapsed duration; the tool waited 10.010
seconds before yielding. An exact total duration is unavailable, not inferred.
This is a diagnostic failure reproduction, not a native Heroism test pass.

Final command on `5b384efdf2ada02bfdce2ed43f2c67ad6d842620`:

```powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass -File scripts/Test-Development.ps1 -Only 'heroism-repeat,confirmation-needs-an-instance-this-attempt-applied,existing-effect-sufficiency-rules,live-existing-effect-skip-in-compiler,exhausted-rod-waived-by-active-effect,explicit-run-stops-after-failure-both-modes,execution-host-runs-reports-and-halts,run-presentation-separates-effects-and-spending,submitted-without-effect-is-not-success,animated-executor-validates-before-queue-and-reports,instant-executor-revalidates-batches-and-reports,explicit-run-cancellation-disposes-executor,execution-host-cancel-deadline-shutdown,execution-policy-settled-touch-detail-survives-confirmation,group-mixed-coverage-keeps-adequate-recipients,direct-manipulation-group-confirmation-needs-one-reached'
```

[Final output](evidence/heroism-repeat/final.log): source **47/0**, build **1/0**,
tests **39 PASS / 0 FAIL**, including all 24 new names; all 16 prefixes matched.
Selector duration 7.6 seconds; [supervised child duration](evidence/heroism-repeat/final-time.txt)
7.917 seconds. Production compiles against the installed assemblies under C# 7.3
and .NET Framework 4.7. `git diff --check` passed.
Changed documentation local-link checks passed 10/0 (the complete focused
record and new checkpoint entries); recorded artifact hashes were re-read from
the files. Guarded publication is recorded below.

Intermediate local fixture work is not hidden as qualification: `artifacts/heroism-repeat/`
retains `development.log` 12/8 (9.879 s), `development-2.log` 18/2 (8.048 s),
`development-3.log` 20/0 (7.689 s), `focused.log` 15/24 (6.924 s),
`development-4.log` 24/0 (8.098 s), and `final.log` 39/0 (8.025 s) on the
pre-final-fixture source. Failures exposed lost Animated confirmation detail and
fixture assumptions: checking the sentinel before its later expiry, expecting
normal kind/readability in deliberately bad observations, and constructing
catalogue aliases with different expression instances. The final fixture keeps
the pre-submission baseline readable and changes only the after-observation for
those negative cases. These iteration failures are not reporter reproductions.

SHA-256 identities:

| Artifact | SHA-256 |
| --- | --- |
| Final DLL | `833148079111bb189e68a7f6f579786c7bb295588b2d0931290f8d391ff0cbd4` |
| Final test runner | `4fed7e29316ac322230dbece7bdd7b4b1643b3e8117c5fae8a107e4d2e36651f` |
| Baseline log (UTF-8 archival copy) | `57aa23f95e5790b5d89487ff1ef76b3ecaa9e4441e80f5771bbf511d1a2bd5a3` |
| Final log (UTF-8 archival copy) | `4bc1c0252a5099e61d2c1e63638543fd34e6d70f981fe6611ec93aa885e256db` |

## Installed contracts and native limits

Read-only decompilation with the existing local ilspycmd 10.1.1 tool inspected
`Kingmaker.UnitLogic.Buffs.BuffCollection` and
`Kingmaker.UnitLogic.Mechanics.Actions.ContextActionApplyBuff` from installed
`Kingmaker_Data/Managed/Assembly-CSharp.dll`. SHA-256:
`3b6450ffec440e296e586f71c711b195aed144b28d53e1cbb29406d18fef5afb`;
MVID `07fa1e4d-8618-41b3-9b8d-faa17d3b26f7`. Detached outputs remain at
`artifacts/heroism-repeat/BuffCollection.cs` and `ContextActionApplyBuff.cs`.
The local game is the repository's 2.1.7b contract target; no running-game build
or loaded mod identity was observed. Installed KBP Info.json says 0.1.1-rc3;
that is installation metadata, not the reporter's version or a loaded candidate.

The installed generic collection extends `Prolong` only when the proposed end
is later, retains `Ignore`, and replaces `Replace`. ApplyBuff calculates duration
from its mechanics context (or explicit seconds). These contracts explain a
possible retention mechanism; they do not establish live Heroism's stacking
configuration, computed duration, patches, or actual outcome. The historical
catalogue and local Call of the Wild reference source are not substituted for
loaded blueprints/patches. The conventional local Player.log path was absent.
No reporter evidence was found in the authorized repository/lab context.

All native rows below are **NOT RUN**, in both Instant and Animated:

| Row | Required observation |
| --- | --- |
| Initially unbuffed Heroism plus sentinel | Exact loaded build/source/target, new instances, cost, sentinel submission |
| Separate second run, recast off | Current effects/resources, skips before spending, no active run if fully covered |
| Legal explicit recast, recast on | Actual before/after instances/expiry, native result, full authoritative duration/strength, spending and cleanup |
| Expiry plus replenishment | New valid reservations/casts without stale tokens |
| Reporter configuration | Same caster/source/target/enhancements and manual comparison if available |

Precise remaining prerequisite: an approved selection/recipe for these Heroism
projections and the repository-required run-bound casting allowance for the exact
candidate package/DLL/MVID and submission budget. Existing allowance files bind
older builds and cannot be reused. Before real launch, satisfy the current
launcher safety/source and WhatIf purity guards, package identity, approved
disposable save pair, transactional staging/locking and exact restoration. None
were bypassed; no game, Mods installation or saves were mutated.

Next action: review this diagnostic checkpoint; obtain guarded native evidence
for the rows above, classify the actual failure, then implement and validate the
smallest demonstrated gameplay repair. A draft PR does not close the player
report. Merging, release, tagging and permanent installation remain out of scope.

## Draft publication

[Draft PR #18](https://github.com/howardreith/KingmakerBuffPlanner/pull/18) is open
against main `d444b1720fa10704f59bc07fdcd4367f9314856f`; initially published
documentation HEAD `0f3d0c5dc2d8a80ed5c3c788a013fcb0edb0a14b`.
`scripts/Test-GuardedPush.ps1` passed **6/0**, supervised duration 1.665 seconds.
The initial sandboxed invocation could not reach GitHub and claimed no pass;
the authorized network retry succeeded. Both local logs/time receipts remain
under `artifacts/heroism-repeat/guarded-push-check*`.

Publication used the existing tested helper:

```powershell
& C:/Dev/KingmakerBuffPlannerLab/codex-policy/Push-KingmakerBuffPlanner.ps1 -RepositoryRoot C:/Dev/KingmakerBuffPlannerLab/repo/KingmakerBuffPlanner -Confirm:$false
```

The helper verified the remote branch HEAD. The final documentation-only
publication receipt is pushed through the same helper; tested product/artifact
identity remains `5b384efdf2ada02bfdce2ed43f2c67ad6d842620`. The PR explicitly
leaves the gameplay repair open. No merge or release occurred.
