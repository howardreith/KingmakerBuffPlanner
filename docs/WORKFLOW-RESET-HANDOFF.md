# Workflow reset review checkpoint - October 11, 2026

Branch: `codex/kingmaker-buff-planner-workflow-reset-2026-10-11`, from fetched main
`a9c6d0f766370cdfb5176665fc6279b6620c4884`. The owner's everyday checkout and all
frozen candidate worktrees/artifacts were preserved. No merge, release, permanent
installation or old acceptance campaign is part of this repair.

[VALIDATION-POLICY.md](VALIDATION-POLICY.md) is authoritative. Root AGENTS,
current resume, the 0.4.2 handoff and both byte-identical mission files now state
the development default. Earlier resume chronology is intact under
[history](history/README.md). The actual shared home is `C:\Users\howar\.codex`
(CODEX_HOME unset), with no home AGENTS/override or ancestor/nested override found.
The template remains a template; no unrelated global profile was changed. A fresh
read-only Codex session confirmed the effective development default and 15/30
minute limits. The referenced workflow handoff is now supplied here.

## Ordinary entry and preserved broad routes

```powershell
# Presentation work: relevant view inspection accompanies these contracts.
.\scripts\Test-Development.ps1 -Only 'presentation-view-models,spellbook-handoff'
# Catalogue ownership: choose actual affected prefixes; native removal/casting
# changes still need their relevant native scenario after review.
.\scripts\Test-Development.ps1 -Only 'optional-blueprint-ownership,variant-membership'
# Direct cheap refusal/reader checks need no DLL/package or live purity window.
.\scripts\Test-RuntimeLauncherArguments.ps1
.\scripts\Test-ProblemNavigationEvidence.ps1
```

The default call graph is `Test-Development -> Build -> Validate-Source + production
MSBuild`, then test-runner MSBuild, the current runner `--only <prefixes>`, and
explicitly selected optional reader/filesystem checks. It uses Get-KbpGamePath,
rejects zero matches, propagates failures and restores KBP_TEST_GAME_PATH even
when a check fails. The default contains no live purity, deployment, restore,
package creation or publisher step. `-Check RuntimeHarness` is explicit and
requires its already built matching local package; it does not silently create it.

`Test-SourceOnly.ps1` remains the explicit broad aggregate. Standalone
`Test-RuntimeLauncherFileWhatIf.ps1`, `Test-RuntimeHarness.ps1`,
`Test-RestoreInstallLocal.ps1` and `Test-PublishReleaseGate.ps1` remain available.
Cheap display/scenario refusals now run before expensive windows, with stable
`KBP_DISPLAY_MODE_SCENARIO` and no side effects, including case-insensitive
supported arguments. Every real launch retains current Mods/save/lock/ownership,
staging, rollback and restoration checks. Whole-history live purity scans remain
on the explicit safety path: dependency-bound reuse is not proved by this patch.
The publisher's aggregate/build path is unchanged; candidate/receipt reuse there
is a separate bounded follow-up, not an assumed capability.

## Focused verification

Supervised Windows PowerShell 5.1 (`-NoProfile -ExecutionPolicy Bypass`) logs are
retained locally under `C:\Dev\.workflow-reset\logs`, outside tracked evidence.

| Actual command | Result | Elapsed |
| --- | --- | --- |
| `scripts/Test-DevelopmentSelector.ps1` | 6 selector/environment/zero-match/failure checks PASS | 1.080 s |
| `scripts/Test-RuntimeLauncherArguments.ps1` | 6 positive/negative/case-normalization checks PASS; bounded filesystem unchanged | 0.231 s |
| `scripts/Test-Development.ps1 -Only 'presentation-view-models','spellbook-handoff'` | Source 43/43, production and current test runner compile PASS; selected tests 3 PASS, 0 FAIL | 9.545 s |

The selector fixture executes the actual selector and Build wrappers, replacing
only compiler/runner boundaries; it observes two builds and one selected runner
invocation and refuses missing/zero/failing selections. The argument fixture
executes the actual launcher up to the external import boundary. Neither fixture
is a stubbed purity qualification or game PASS.

Actual development DLL SHA256:
`2b5750e3dc9e1ce045217a245956d579b602f7c5d5edf0533eadfdaf57bbb5ac`.
Build stamped main base `a9c6d0f766370cdfb5176665fc6279b6620c4884`; production
source was unchanged and validation ran on the uncommitted workflow diff at that
HEAD. This is an unpackaged development artifact. A later workflow/documentation
commit does not turn it into that HEAD's qualified binary; it is not rebuilt for
reporting. Frozen 0.4.2/0.4.3 evidence keeps its original product/reader/fixture
and environment identities.

Deferred: full C# aggregate, existing bounded harness against a validated package,
deployment/launcher live purity/restore/publisher qualification, relevant native
catalogue/removal/casting acceptance, full resolution/layout matrices where
selected, and release builds/reproducibility. No fresh native or release coverage
is claimed. The original/copy mission integrity check remains enabled and passes.
