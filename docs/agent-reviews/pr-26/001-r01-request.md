<!-- agent-review:v1 enrollment=kbp-codex-kingmaker-buff-planner-umm-0311-baseline-20261123182207-144d key=r01-request part=1/1 sha256=4fee129b0ec0de069d661620974715bffe5ac7ac4da7caa210ecd2f05501b5db -->
### Claude Opus — review request — round 1 — candidate `ee05e37652`

| Field | Value |
|---|---|
| Request ID | `kbp-codex-kingmaker-buff-planner-umm-0311-baseline-20261123182207-144d-r01-a00d47` |
| Checkpoint | `final` (final) |
| Candidate H | `ee05e37652ae00ba342407b06a9eb46b72a2441f` |
| Target B (`main`) | `913798712f3685236c8f9d5cc1e8c04ea9fc9248` |
| Merge base M | `913798712f3685236c8f9d5cc1e8c04ea9fc9248` |
| Mission | `kingmaker-umm-0311-mission-20261123` sha256 `f7d990f396` |
| Workflow | `6d613defe7` |
| Owner guidance | none |
| Owner amendments | none |
| Developer observed | foreground session `e178a93b-ca53-4b6b-a4d2-a357d48eaa77`: models ['claude-opus-5-5'], effort ['high'], permission modes ['auto'] (CLI-recorded transcript; the headless permission profile does not apply to a foreground session) |
| Reviewer requested | Codex `gpt-6.2-astra`, effort `max`, read-only sandbox |
| Round budget | 1 of 5 |

**Scope:** The complete mission as written.

**Exit conditions:**
- Every requirement of the mission is implemented, with evidence appropriate to each claim; no requirement is weakened or silently dropped.
- Validation follows the repository's own instructions; commands, results and limits are reported honestly.
- Deviations, limitations and deferred optional items are stated explicitly, separate from required work.

**Developer summary:**

Buff Planner UMM 0.31.1 baseline (new PR #26, branch codex/kingmaker-buff-planner-umm-0311-baseline from main 9137987). This repository only.

Before: Info.json declared 0.28.2 but the csproj compiled against the installed loader ($(KingmakerInstallDir)/.../UnityModManager/UnityModManager.dll); the published 0.6.20 DLL references UnityModManager 0.33.0.0.

Changes: scripts/loader-reference-pins.json (UnityModManager 0.31.1.0 from the author's NuGet package, DLL 2e038ff2...; 0Harmony12 1.2.0.1, aa1cd483...; canonical set enforced) and scripts/Get-PinnedLoaderReferences.ps1 (fetch into ignored artifacts/references, verify archive/DLL hash and identity, Info.json ManagerVersion as a JSON string equal to the pin; no installed-loader fallback); LoaderReferences.targets imported by the csproj (re-verify on every build; after FindReferenceAssembliesForReferences the Csc list @(ReferencePathWithRefAssemblies) must contain exactly the two verified files by full path; DisableFastUpToDateCheck and loader inputs as compile inputs); csproj HintPaths and its missing-reference error point at the pinned directory; Info.json ManagerVersion 0.31.1 and Validate-Source expects that string; Build.ps1 (used by Build-Local, Build-Release and Test-Development) fetches/verifies the pins first; scripts/validate-package.ps1 (run by Build-Local and Build-Release) additionally requires ManagerVersion = the string 0.31.1, the packaged DLL's actual AssemblyRefs = pinned UnityModManager 0.31.1.0 and 0Harmony12 1.2.0.1, and text-only documentation/data entries; scripts/Test-LoaderBaseline.ps1 (37 checks, temp clone with a space, private hash-checked fixtures: genuine 0.6.20 release and author UMM 0.32.4); docs/UMM-COMPATIBILITY.md (new), INSTALLATION-AND-USE.md and NEXUS-RELEASE-COPY.md now state the 0.31.1 minimum; structured evidence in docs/evidence/umm-0311/.

Results: candidate DLL references UnityModManager 0.31.1.0 and 0Harmony12 1.2.0.1; Build-Local package validated (11 checks); the genuine 0.6.20 ZIP (54153abb...) with only its manifest lowered to 0.31.1 is refused by validate-package.ps1; focused gate (-Only localization: source validation 47/47, pinned build, 14/14 incl. language switch and plan-edit round trip) passes on ee05e37; static audit 8 types/24 members resolved on 0.31.1, 0.32.4, owner 0.33.0, 0.33.4 (component-assembled profiles labelled). Native runtime NOT RUN on every loader: the runtime harness transacts against the live installation; the runtime-profile plumbing items (preflight vs in-process loader identity, Harmony2 identity) are therefore not exercised by this change and are recorded as such in docs/UMM-COMPATIBILITY.md. Install-Local.ps1 installs only from a Build-Release manifest (its package passes validate-package.ps1) and was not run (owner policy denies it).

**Validation (developer-reported):**

| Command | Required | Result | Duration | Scope and limits |
|---|---|---|---|---|
| python C:/Dev/agent-review/agent_review.py verify --enrollment kbp-codex-kingmaker-buff-planner-umm-0311-baseline-20261123182207-144d --gate focused -- -Only localization | yes | pass | 7.8s | Controller receipt on ee05e37: Validate-Source 47/47, Build.ps1 with pinned references, 14/14 localization tests. Offline only. |
| scripts/Test-LoaderBaseline.ps1 (temporary clone of ca04d74) | no | pass | ~3 min | 37/37; docs/evidence/umm-0311/loader-baseline.json. ee05e37 adds only docs/evidence and docs text. |
| direct normal-verbosity MSBuild of the production project at ca04d74 | no | pass | ~20s | docs/evidence/umm-0311/direct-build-record.json: Csc /reference to <repo>/artifacts/references/UnityModManager.dll and 0Harmony12.dll. |
| LoaderApiAudit (source in docs/evidence/umm-0311/loader-api-audit-*.txt) | no | pass | ~1s | Static only. |
| Native checks (settings/language, planner open/close, persisted plans, representative cast) on UMM 0.31.1 / 0.32.4 / 0.33.0 / 0.33.4 | no | not_run | 0 | No disposable game copy; live installation must not be modified. |

**Evidence references:**
- docs/evidence/umm-0311/INDEX.md
- docs/evidence/umm-0311/loader-baseline.json
- docs/evidence/umm-0311/direct-build-record.json
- docs/evidence/umm-0311/api-audit.json
- docs/UMM-COMPATIBILITY.md

_Posted by the agent-review controller through the owner's GitHub account on behalf of the Claude Opus developer. Automated request; not a human review._
