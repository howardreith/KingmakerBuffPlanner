<!-- agent-review:v1 enrollment=kbp-codex-kingmaker-buff-planner-umm-0311-baseline-20261123182207-144d key=r02-dispositions-8e060c100a part=1/1 sha256=8e060c100aafd5b3eec3f7786f9aea0212422a9831ac81b4ebe735c837b69bad -->
### Claude Opus — finding dispositions — round 2

Candidate after corrections: `7ad30694889088c3d1d03f98f62b142a29923d46`

- **R1-F1** — `fixed` — commits: `5cd32d0`, `e51e2ef`, `100086d`, `1602627`, `04c55bc`, `f33b945`, `7ad3069`
  docs/evidence/umm-0311/runtime-loader-identity.json (23/23): preflight-row-0.31.1-matching-loader-accepted, preflight-row-0.32.4-matching-loader-accepted, preflight-row-0.33.4-matching-loader-accepted, preflight-row-owner-0.33.0-matching-loader-accepted, preflight-cross-row-0.31.1-files-under-0.32.4-row-refused, preflight-harmony2-only-mismatch-refused, result-row-*-admitted (four rows), result-cross-row-refused, result-harmony2-only-mismatch-refused, result-loaded-versus-installed-mismatch-refused, result-for-request-without-loader-row-refused, request-carries-selected-row, compatibility-identity-binds-loader-row.
- **R1-F2** — `fixed` — commits: `5cd32d0`, `e51e2ef`, `100086d`, `1602627`, `04c55bc`, `f33b945`, `7ad3069`
  docs/evidence/umm-0311/loader-baseline.json (71/71 on a temporary clone of 5cd32d0): release-manifest-from-validated-release-zip, install-preflight-matching-release-passes-reconciliation (the validated copy matches; the installer then stops at an unrelated pre-existing lab runtime transaction owned by the operator, reported as such), install-preflight-wrong-release-commit-rejected, install-preflight-wrong-release-mvid-rejected, install-preflight-wrong-release-dll-digest-rejected, install-preflight-refusals-left-no-staging (all before state or staging).
- **R1-F3** — `fixed` — commits: `5cd32d0`, `e51e2ef`, `100086d`, `1602627`, `04c55bc`, `f33b945`, `7ad3069`
  The inventory is the solution build's Csc command line with the build's KbpSourceRevision, so generated BuildInfo (version and revision) and resources are hashed by content; Build-Local refuses a production command line that differs. docs/evidence/umm-0311/loader-baseline.json (71/71 on a temporary clone of 5cd32d0): receipt-records-clean-producing-build, receipt-after-project-file-change-rejected, receipt-after-resolved-local-reference-change-rejected, receipt-identical-local-references-relocated-accepted (unchanged-reuse control), receipt-after-inputs-restored-accepted.
- **R1-F6** — `fixed` — commits: `7ad3069`
  docs/evidence/umm-0311/retained-frozen-product.json: retained location in the persistent frozen worktree, read-back of the ZIP and sidecar hashes, embedded revision, DLL hash and MVID, Read-KbpBuildManifest acceptance there; api-audit.json and loader-profile-inventory.json are of that DLL (7c7fe150...); source continuity by the docs-only diff; the controller gate is attributed to the 7ad3069-stamped build with the one-clone stamp comparison.
- **R2-F1** — `fixed` — commits: `5cd32d0`, `e51e2ef`, `100086d`, `1602627`, `04c55bc`, `f33b945`, `7ad3069`
  docs/evidence/umm-0311/runtime-loader-identity.json (23/23): installation-preflight-matching-row-accepted, installation-preflight-harmony2-only-mismatch-refused (private game folder), profile-test-entry-point-refuses-harmony2-only-mismatch, profile-test-entry-point-accepts-matching-row, deploy-and-launcher-run-the-shared-preflight-before-staging.
- **R2-F2** — `fixed` — commits: `5cd32d0`, `e51e2ef`, `100086d`, `1602627`, `04c55bc`, `f33b945`, `7ad3069`
  docs/evidence/umm-0311/loader-baseline.json (71/71 on a temporary clone of 5cd32d0): transaction-refuses-package-replaced-after-admission (a second valid archive with the same DLL and different data at the admitted path), transaction-refusal-left-mods-untouched, transaction-stages-its-own-admitted-copy (state packageSha256 = admitted; staged DLL = receipt DLL).
- **R2-F3** — `fixed` — commits: `f33b945`
  docs/BUILD-AND-RELEASE.md: default downloads named, offline invocations Test-LoaderBaseline.ps1 -LegacyReleaseZip ... -WrongLoaderPackage ... and Test-RuntimeLoaderIdentity.ps1 -PackageDirectory ...; parameter names match the scripts and the fixtures are hash-checked inside them.

**Validation (developer-reported):**

| Command | Required | Result | Duration | Scope and limits |
|---|---|---|---|---|
| python C:/Dev/agent-review/agent_review.py verify --enrollment kbp-codex-kingmaker-buff-planner-umm-0311-baseline-20261123182207-144d --gate focused -- -Only localization | yes | pass | 10.1s | Controller receipt on 7ad3069 (DLL stamped 7ad3069; see retained-frozen-product.json). |
| scripts/Test-LoaderBaseline.ps1 (temporary clone of 5cd32d0) | no | pass | ~20 min | 71/71; docs/evidence/umm-0311/loader-baseline.json (71/71 on a temporary clone of 5cd32d0). |
| scripts/Test-RuntimeLoaderIdentity.ps1 -PackageDirectory <official packages> | no | pass | ~1 min | docs/evidence/umm-0311/runtime-loader-identity.json (23/23): private loader folders assembled from the official NuGet packages plus a read-only copy of the owner's install; fixture game folder; constructed results. |
| retained pair read-back and frozen receipt check in the frozen worktree; stamp equivalence | no | pass | ~5 min | retained-frozen-product.json, retained-build-receipt.json, retained-validation.json. |
| LoaderApiAudit on the retained DLL; profile inventory; docs link check | no | pass | ~2s | api-audit.json (8 types/24 members resolved on four profiles), loader-profile-inventory.json, docs-link-check.json (52 links, 0 broken). |
| Native checks on the four loader rows | no | not_run | 0 | No disposable game copy; live installation must not be modified. |

_Model-generated developer response (Claude opus / high), posted by the agent-review controller through the owner's GitHub account; not a human review._
