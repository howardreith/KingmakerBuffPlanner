# Alpha4 publication and subsequent checks

Owner requested publication first, then the discussed extended checks.
[Alpha4](https://github.com/howardreith/KingmakerBuffPlanner/releases/tag/v0.5.4-controller.4)
published **2026-10-18T02:18:48Z**. Draft=false, prerelease=true, latest=false.
Remote tag targets exact product69a48a4d4c6484abf84549ab8bb062195cedb380.
ZIP924265 bytes, SHA256 b307b18950f9254b4df68b13a7aa028abf0d012fa3352d347132c3de6b3b0f58;
DLL/MVID in the [frozen receipt](CONTROLLER-UI-CANDIDATE-0.5.4-CONTROLLER.4.md).
SHA256SUMS.txt110 bytes, SHA256 a1490e60fb9f7b4c38b8799d0a7f7e9a3bf4ae1de37438e82640f712d530a12b.

Guarded push WhatIf6/0 then fast-forward e08031b -> a78b6ab, exact remote verified.
An initial WhatIf was correctly refused on the uncommitted notes; after local
commit it passed. Draft upload used --target69a48a4, --prerelease, --latest=false.
Both draft assets downloaded and matched local bytes before --draft=false.
GitHub asset digests, final flags, exact tag/main and stableLatest verified.
Main remains dcf718fd7b70cf453dd25aa693fefc6aa503d826, stableLatest0.5.3. PR10
remains OPEN/DRAFT at a78b6ab after the feature-branch push; no merge/ready flip.
This artifact-only route reused the unchanged deterministic package; no rebuild
stamped the later documentation HEAD into the release. Previous alphas untouched.

## Broader check result — not hidden by publication

Selected frozen Test-SourceOnly stopped **before purity**: launcher arguments28/0,
source47/0, C#577/1. Failure classic-run-advances-only-while-the-world-runs came
from an assertion requiring a reset to be the very first ReleasePlayerUi statement.
It also fails under matched Alpha3 conditions (0/1, once). Runtime behavior in
the same test passes; source inspection shows the reset still precedes both
workspace/screen close, after legitimate controller cleanup. No new runtime crash
was inferred. Logs: artifacts/controller-alpha4/source-only-69a48a4.log and
classic-source-baseline-alpha3.log. Original failures remain FAIL, not relabeled.

The corrected test walks compiled IL and checks reset-false before BOTH close
calls, with positive native-cleanup-prefix and negative missing/late/wrong-value
fixtures. Existing world-held execution/cleanup behavior and all other guards are
retained. Focused affected pass39/0,5.7s, source47/build1/test compile PASS;
classic-compiled-contract-correction.log. Production code/DLL unchanged.

Test-ControllerAlpha4Safety is an explicit reader-only correction bound to exact
product/DLL/package and identical linked production inputs. It compiles the
corrected harness separately, then runs its full C# suite against Alpha4's actual
source and DLL from the frozen working directory. Every remaining original
source/safety aggregate child follows, unchanged, with full-history purity intact.
Real full-history purity attempts1, foreign lease wait120s fail early rather than
silently retrying; bounded fixture tests retain their normal retry defaults.
No native stage/installation is authorized by this checker. Its result is separate
from the original failed wrapper, not a claim that the old wrapper passed.

First corrected-reader invocation (a643b8a harness) passed launcher28/0, source47/0,
package6/0 and C#579/0. It then FAILED in the synthetic foreign-once retry case:
the wrapper's attempts1 default incorrectly reached the bounded fixture harness.
This is a wrapper-scoping defect, not an observed foreign lease or product failure.
Log: artifacts/controller-alpha4/corrected-reader-source-safety.log. The wrapper
now scopes those defaults to the two actual full-history guard stages only,
restoring caller defaults exception-safely after each stage. The unchanged retry
fixture is rerun as behavioral coverage; no validator/assertion was weakened.

Owner-authorized extended checks are in progress. No native/pixel/input/resource
PASS, game launch, live staging/settings/profile/save changes or restoration
claimed. Controller-specific observer/loader work remains a separate required
task before any controller game test; changed compiled bytes need their own
identity/evidence. No automatic new alpha/retag/main merge/permanent installation.
