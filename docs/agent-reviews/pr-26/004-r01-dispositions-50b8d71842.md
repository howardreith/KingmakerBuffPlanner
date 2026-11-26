<!-- agent-review:v1 enrollment=kbp-codex-kingmaker-buff-planner-umm-0311-baseline-20261123182207-144d key=r01-dispositions-50b8d71842 part=1/1 sha256=50b8d718429a955613f9a5a5342d183cae45ee43c94fbf69a964979c519b96f0 -->
### Claude Opus — finding dispositions — round 1

Candidate after corrections: `62b65e2fbcf59d871d0cda974ab260152704b43e`

- **R1-F1** — `fixed` — commits: `c0e2baf`, `75aeeff`, `77ac9e9`, `62b65e2`
  runtime-loader-identity.json: preflight-matching-installation-accepted, preflight-different-umm-row-refused, preflight-harmony2-only-mismatch-refused, preflight-installed-file-swapped-refused, preflight-missing-harmony2-refused, result-matching-loader-admitted, result-different-umm-row-refused, result-harmony2-only-mismatch-refused, result-harmony2-not-loaded-refused, result-earlier-shape-without-harmony2-refused, compatibility-identity-binds-loader. Code: scripts/compatibility/CompatibilityProfile.Common.ps1, RuntimeAutomation.Common.ps1 (Assert-KbpRuntimeResult -ExpectedLoader, Get-KbpCompatibilityIdentityDigest), Invoke-KingmakerRuntimeTest.ps1, src/.../RuntimeLoaderIdentity.cs, RuntimeTestResultModels.cs.
- **R1-F2** — `fixed` — commits: `c0e2baf`, `75aeeff`, `77ac9e9`, `62b65e2`
  docs/evidence/umm-0311/loader-baseline.json (62/62 on a temporary clone of 75aeeff): receipt-records-clean-producing-build, receipt-matching-archive-and-receipt-accepted, second-same-version-build-is-distinct, receipt-swapped-receipt-for-other-build-rejected, receipt-swapped-archive-from-other-build-rejected, receipt-wrong-embedded-revision-rejected, receipt-wrong-dll-digest-rejected, receipt-wrong-mvid-rejected (all through Read-KbpBuildManifest, the runtime preflight before staging).
- **R1-F3** — `fixed` — commits: `c0e2baf`, `75aeeff`, `77ac9e9`, `62b65e2`
  docs/evidence/umm-0311/loader-baseline.json (62/62 on a temporary clone of 75aeeff): receipt-matching-archive-and-receipt-accepted (unchanged reuse), receipt-after-reference-helper-change-rejected, receipt-after-project-file-change-rejected, receipt-after-build-wrapper-change-rejected (same HEAD), receipt-identical-local-references-relocated-accepted, receipt-after-resolved-local-reference-change-rejected, receipt-dirty-development-build-not-reused, receipt-dirty-flag-as-string-rejected, receipt-after-inputs-restored-accepted.
- **R1-F4** — `fixed` — commits: `c0e2baf`, `75aeeff`, `77ac9e9`, `62b65e2`
  docs/evidence/umm-0311/loader-baseline.json (62/62 on a temporary clone of 75aeeff): validator-pin-definition-missing-harmony12-rejected, validator-umm-pin-inconsistent-with-minimum-rejected, validator-offline-without-reference-cache-accepted, validator-genuine-control-after-restore-accepted (scripts/validate-package.ps1 calls Get-PinnedLoaderReferences.ps1 -DefinitionOnly).
- **R1-F5** — `fixed` — commits: `c0e2baf`, `75aeeff`, `77ac9e9`, `62b65e2`
  docs/evidence/umm-0311/loader-baseline.json (62/62 on a temporary clone of 75aeeff): relative-cache-uses-powershell-location-corrupt-rejected (process directory holds the genuine cache, PowerShell location the corrupt one; refusal names the PowerShell-location path), relative-cache-uses-powershell-location-genuine-accepted (reverse), relative-cache-not-yet-created-resolves-under-powershell-location.
- **R1-F6** — `fixed` — commits: `c0e2baf`, `75aeeff`, `77ac9e9`, `62b65e2`
  docs/evidence/umm-0311/frozen-product.json identifies the frozen product (commit 75aeeff, package/DLL/MVID/embedded revision/receipt digest), frozen-build-receipt.json and frozen-package-validation.json agree; api-audit.json and loader-profile-inventory.json are of that DLL (c7f7c10e, = loader-baseline.json candidate_dll_sha256). The controller gate on 62b65e2 is attributed separately: equal build-input fingerprint, docs-only diff, and the stamp-equivalence builds in frozen-product.json.
- **R1-F7** — `fixed` — commits: `77ac9e9`
  README.md (Build section) and docs/BUILD-AND-RELEASE.md (Prerequisites; new 'Loader compile baseline (UMM 0.31.1)' with cache preparation, package validation, boundary/identity suites, static audit command with profile inputs, native and combined-stack status); docs-link-check.json: 52 links, 0 broken.
- **R1-F8** — `fixed` — commits: `77ac9e9`
  docs/NEXUS-RELEASE-COPY.md (0.5.3 copy) states 'Unity Mod Manager 0.28.2 or later' again, matching v0.5.3:src/KingmakerBuffPlanner/Info.json ManagerVersion 0.28.2; the candidate manifest (src/KingmakerBuffPlanner/Info.json) declares 0.31.1, and docs/INSTALLATION-AND-USE.md scopes 0.31.1 to the unreleased baseline candidate.
- **R1-F9** — `fixed` — commits: `62b65e2`
  docs/evidence/umm-0311/INDEX.md labels the exported column as Git-blob SHA-256 and gives 'git cat-file blob HEAD:docs/evidence/umm-0311/<file> | sha256sum' and a Python equivalent; e.g. frozen-product.json blob = 590efc63a1ae55c8cf07e9be7a497165c35558f3d2dd8fafa77dcf50bee613b6 as indexed.

**Validation (developer-reported):**

| Command | Required | Result | Duration | Scope and limits |
|---|---|---|---|---|
| python C:/Dev/agent-review/agent_review.py verify --enrollment kbp-codex-kingmaker-buff-planner-umm-0311-baseline-20261123182207-144d --gate focused -- -Only localization | yes | pass | 8.1s | Controller receipt on 62b65e2 (DLL stamped 62b65e2; see frozen-product.json). |
| scripts/Test-LoaderBaseline.ps1 (temporary clone of 75aeeff) | no | pass | ~10 min | 62/62; docs/evidence/umm-0311/loader-baseline.json (62/62 on a temporary clone of 75aeeff). |
| scripts/Test-RuntimeLoaderIdentity.ps1 | no | pass | ~10s | 21/21; docs/evidence/umm-0311/runtime-loader-identity.json. Offline: private copy of the installed loader files, constructed results. |
| frozen Build-Local + validator + revision-stamp equivalence (clone of 75aeeff) | no | pass | ~3 min | frozen-product.json, frozen-build-receipt.json, frozen-package-validation.json. |
| LoaderApiAudit on the frozen DLL; profile inventory | no | pass | ~2s | api-audit.json (8 types/24 members resolved on 0.31.1, 0.32.4, owner 0.33.0, 0.33.4), loader-profile-inventory.json. Static only. |
| tools docs_link_check.py README.md docs/BUILD-AND-RELEASE.md docs/UMM-COMPATIBILITY.md docs/INSTALLATION-AND-USE.md | no | pass | <1s | docs-link-check.json: 52 relative links/anchors, 0 broken. |
| Native checks on UMM 0.31.1 / 0.32.4 / 0.33.0 / 0.33.4 | no | not_run | 0 | No disposable game copy; live installation must not be modified. |

_Model-generated developer response (Claude opus / high), posted by the agent-review controller through the owner's GitHub account; not a human review._
