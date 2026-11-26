<!-- agent-review:v1 enrollment=kbp-codex-kingmaker-buff-planner-umm-0311-baseline-20261123182207-144d key=r04-request part=1/1 sha256=24c4fa08e15c923d820ea8ceb2f58465467a9a9b46862b4bab5be199a5b3e1c3 -->
### Claude Opus — review request — round 4 — candidate `e2aa5aebed`

| Field | Value |
|---|---|
| Request ID | `kbp-codex-kingmaker-buff-planner-umm-0311-baseline-20261123182207-144d-r04-9c8290` |
| Checkpoint | `final` (final) |
| Candidate H | `e2aa5aebed622d041ce750adf8f710142ba37819` |
| Target B (`main`) | `913798712f3685236c8f9d5cc1e8c04ea9fc9248` |
| Merge base M | `913798712f3685236c8f9d5cc1e8c04ea9fc9248` |
| Mission | `kingmaker-umm-0311-mission-20261123` sha256 `f7d990f396` |
| Workflow | `6d613defe7` |
| Owner guidance | none |
| Owner amendments | none |
| Developer observed | foreground session `e178a93b-ca53-4b6b-a4d2-a357d48eaa77`: models ['claude-opus-5-5'], effort ['high'], permission modes ['auto'] (CLI-recorded transcript; the headless permission profile does not apply to a foreground session) |
| Reviewer requested | Codex `gpt-6.2-astra`, effort `max`, read-only sandbox |
| Round budget | 4 of 5 |

**Scope:** The complete mission as written.

**Exit conditions:**
- Every requirement of the mission is implemented, with evidence appropriate to each claim; no requirement is weakened or silently dropped.
- Validation follows the repository's own instructions; commands, results and limits are reported honestly.
- Deviations, limitations and deferred optional items are stated explicitly, separate from required work.

**Developer summary:**

Round-4 corrections for Buff Planner PR #26 (this repository only), addressing the complete open ledger in one batch. Code e85dbc1 and 4cde7f6, code+tests b3c5ba4, test-only 74a9efc and 8ac5738; 0e26eed changes only docs/UMM-COMPATIBILITY.md and docs/BUILD-AND-RELEASE.md; e2aa5ae changes only docs/evidence/umm-0311/.

(1) -LoaderId is forwarded to both Deploy-Local.ps1 calls in Invoke-KingmakerRuntimeTest.ps1 and through Enter-KingmakerCompatibilityProfile.ps1 (default owner-0.33.0 kept for callers that omit it). (2) The launcher keeps the decoded request it wrote (Save-KbpRuntimeRequest) and checks the result against that snapshot. (3) Bounded local configuration: GamePath.props may only set KingmakerInstallDir and no outside Directory.Build.props/.targets may apply (refused at build and at reuse); the compiler inventory keeps argument order, starts with the effective compiler binary (CscToolPath/CscToolExe, by content) and the implicit csc.rsp when /noconfig is absent, and reuse compares it with the inventory MSBuild resolves now entry by entry. (4) Build-Release reads both deterministic build receipts while their outputs are current, requires clean receipts naming the release commit and their own packages, and compares the validated release ZIP with them (Assert-KbpReleaseReconciliation). (5) Install-Local reconciles embeddedCommit with release.commit and the measured stamp. (6) A transaction entry failure before the state file exists removes the transaction's own package copy (including a partial copy) and releases the lock. Tests use private fixtures: Deploy-Local and the compatibility wrapper with -WhatIf against private game directories for all four rows. Retained frozen product rebuilt at 8ac5738 in a dedicated worktree (ZIP 94e9a036, DLL 80e0a1fb, MVID b12670d5); the guide names it and keeps rounds 2 and 3 as dated history. Native runtime NOT RUN.

**Validation (developer-reported):**

| Command | Required | Result | Duration | Scope and limits |
|---|---|---|---|---|
| python C:/Dev/agent-review/agent_review.py verify --enrollment kbp-codex-kingmaker-buff-planner-umm-0311-baseline-20261123182207-144d --gate focused -- -Only localization | yes | pass | 8.7s | Controller receipt on e2aa5ae: source validation, Release build (DLL c05e8905..., stamped e2aa5ae, so not the retained DLL), protocol tests PASS=14 FAIL=0. |
| scripts/Test-LoaderBaseline.ps1 (temporary clone of 8ac5738, path with a space) | no | pass | not recorded | docs/evidence/umm-0311/loader-baseline.json (90/90 on a temporary clone of 8ac5738). |
| scripts/Test-RuntimeLoaderIdentity.ps1 (offline, private fixtures) | no | pass | not recorded | docs/evidence/umm-0311/runtime-loader-identity.json (37/37 at 74a9efc; only Test-LoaderBaseline.ps1 changed before 8ac5738). |
| Build-Local.ps1 in the frozen worktree at 8ac5738; read-back; revision-stamp builds; LoaderApiAudit; profile inventory | no | pass | ~6 min | docs/evidence/umm-0311/retained-frozen-product.json, retained-build-receipt.json, retained-validation.json (13/13), api-audit.json (8 types/24 members resolved on 0.31.1, 0.32.4, owner-0.33.0, 0.33.4), loader-profile-inventory.json; 0e26eed stamped with 8ac5738 reproduces DLL 80e0a1fb. |
| docs link check (UMM-COMPATIBILITY.md, BUILD-AND-RELEASE.md) | no | pass | <1s | docs/evidence/umm-0311/docs-link-check.json: 3 links, 0 broken. |
| Native checks on four loader rows; combined-stack | no | not_run | 0 | The runtime harness transacts against the live installation; no approved disposable game copy. |

**Evidence references:**
- docs/evidence/umm-0311/INDEX.md
- docs/evidence/umm-0311/loader-baseline.json
- docs/evidence/umm-0311/runtime-loader-identity.json
- docs/evidence/umm-0311/retained-frozen-product.json
- docs/evidence/umm-0311/retained-build-receipt.json
- docs/evidence/umm-0311/api-audit.json
- docs/UMM-COMPATIBILITY.md

**Prior findings to assess:** `R1-F1`, `R1-F3`, `R2-F1`, `R3-F1`, `R3-F2`, `R3-F3`, `R3-F4`, `R3-F5`

_Posted by the agent-review controller through the owner's GitHub account on behalf of the Claude Opus developer. Automated request; not a human review._
