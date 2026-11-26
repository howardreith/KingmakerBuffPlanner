<!-- agent-review:v1 enrollment=kbp-codex-kingmaker-buff-planner-umm-0311-baseline-20261123182207-144d key=r04-dispositions-9fd8e87968 part=1/1 sha256=9fd8e879685e4e8f7e7a4a9de6a8242ec0fe541004e86f900803a9bc75de7433 -->
### Claude Opus — finding dispositions — round 4

Candidate after corrections: `e636c2c56367c6fd621b39cbabb11374203c8bf8`

- **R1-F3** — `fixed` — commits: `c2a594c`, `1c3671f`, `e636c2c`
  docs/evidence/umm-0311/loader-baseline.json (99/99 on a temporary clone of c2a594c): receipt-with-import-selected-by-environment-rejected (a local target selected through DirectoryBuildTargetsPath that sets KbpVersion only when SkipCompilerExecution is not true), receipt-with-untracked-directory-build-targets-rejected and build-with-untracked-directory-build-targets-rejected (the same target as an untracked src\Directory.Build.targets; Build.ps1 refuses too), receipt-after-local-imports-removed-accepted; relocation control receipt-identical-local-references-relocated-accepted retained. docs/BUILD-AND-RELEASE.md and docs/UMM-COMPATIBILITY.md name the enforcing entry points.
- **R4-F1** — `fixed` — commits: `c2a594c`, `1c3671f`, `e636c2c`
  docs/evidence/umm-0311/loader-baseline.json (99/99 on a temporary clone of c2a594c): release-with-disagreeing-builds-keeps-previous-release (a validated versioned release present; a disposable instrumented Build-Release makes the producing builds disagree: refused, the previous ZIP and manifest keep their hashes, no private folder left), release-manifest-from-validated-release-zip (normal path).
- **R4-F2** — `fixed` — commits: `c2a594c`, `1c3671f`, `e636c2c`
  docs/evidence/umm-0311/loader-baseline.json (99/99 on a temporary clone of c2a594c): transaction-cleanup-failure-still-releases-the-lock (fixture mode; the first state write fails while the owned ZIP is held open by the failing process: Mods unchanged, no lock left, error 'its lock was released; still to clean up: owned package copy ...'), with transaction-state-write-failure-leaves-no-copy-or-lock and the same-run-ID retry retained.
- **R4-F3** — `fixed` — commits: `c2a594c`, `1c3671f`, `e636c2c`
  docs/evidence/umm-0311/runtime-loader-identity.json (40/40 at c2a594c, run from the checkout with a relative -CandidatePackage): retained-request-survives-replacement-at-the-write-boundary (the atomic writer wrapped to replace the destination with a 0.33.4 request right after writing: the retained request keeps row 0.31.1 and a non-ASCII note, the file holds 0.33.4), boundary-retained-request-refuses-the-replacement-row-result.
- **R4-F4** — `fixed` — commits: `c2a594c`, `1c3671f`, `e636c2c`
  docs/evidence/umm-0311/loader-baseline.json (99/99 on a temporary clone of c2a594c): release-reconciliation-empty-array-dirty-flag-rejected, release-reconciliation-string-false-dirty-flag-rejected, release-reconciliation-number-zero-dirty-flag-rejected (one producing receipt changed), release-reconciliation-matching-clean-pair-accepted (control).
- **R4-F5** — `fixed` — commits: `c2a594c`, `1c3671f`, `e636c2c`
  docs/BUILD-AND-RELEASE.md shows -CandidatePackage and states the reduced scope without it; Test-RuntimeLoaderIdentity.ps1 resolves the candidate with GetUnresolvedProviderPathFromPSPath. docs/evidence/umm-0311/runtime-loader-identity.json (40/40 at c2a594c, run from the checkout with a relative -CandidatePackage) is that documented command run after Set-Location with a relative ZIP: deploy-local-whatif-row-<row>-clears-loader-check and compatibility-wrapper-whatif-row-<row>-clears-loader-check for all four rows, the crossed-row refusals and deploy-row-fixtures-unchanged.
- **R4-F6** — `fixed` — commits: `c2a594c`, `1c3671f`, `e636c2c`
  docs/evidence/umm-0311/runtime-loader-identity.json (40/40 at c2a594c, run from the checkout with a relative -CandidatePackage): mixed-case-owner-loader-id-resolves-to-the-canonical-row (Owner-0.33.0 resolves to loaderId owner-0.33.0 with an identical identity).

**Validation (developer-reported):**

| Command | Required | Result | Duration | Scope and limits |
|---|---|---|---|---|
| python C:/Dev/agent-review/agent_review.py verify --enrollment kbp-codex-kingmaker-buff-planner-umm-0311-baseline-20261123182207-144d --gate focused -- -Only localization | yes | pass | 10.4s | Controller receipt on e636c2c (log gate-focused-e636c2c56367-d4b9488d083-a7f0c05bad5-20261124T161823231392Z.log): source validation, Release build (DLL 4f57c8d9..., stamped e636c2c, not the retained DLL), protocol tests PASS=14 FAIL=0. |
| scripts/Test-LoaderBaseline.ps1 (temporary clone of c2a594c) | no | pass | not recorded | docs/evidence/umm-0311/loader-baseline.json (99/99 on a temporary clone of c2a594c). |
| Set-Location <checkout>; .\scripts\Test-RuntimeLoaderIdentity.ps1 -PackageDirectory <six packages> -CandidatePackage artifacts\local-runtime\0.6.20\KingmakerBuffPlanner-0.6.20-local-runtime.zip | no | pass | ~6 min | docs/evidence/umm-0311/runtime-loader-identity.json (40/40 at c2a594c, run from the checkout with a relative -CandidatePackage); the documented command with a relative candidate ZIP, including the four matching-row deployment and wrapper observations, the crossed-row refusals and the unchanged-fixture result. |
| Build-Local.ps1 in the frozen worktree at c2a594c; read-back; revision-stamp builds; LoaderApiAudit; profile inventory; link check | no | pass | ~6 min | docs/evidence/umm-0311/retained-frozen-product.json (1c3671f stamped with c2a594c reproduces DLL a384dc44), retained-build-receipt.json, retained-validation.json (13/13), api-audit.json (8 types/24 members resolved on 0.31.1, 0.32.4, owner-0.33.0, 0.33.4), loader-profile-inventory.json, docs-link-check.json (4 links, 0 broken). |
| Native checks on four loader rows; combined-stack | no | not_run | 0 | The runtime harness transacts against the live installation; no approved disposable game copy. |

_Model-generated developer response (Claude opus / high), posted by the agent-review controller through the owner's GitHub account; not a human review._
