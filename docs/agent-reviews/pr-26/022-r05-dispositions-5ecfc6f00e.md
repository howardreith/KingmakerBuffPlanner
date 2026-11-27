<!-- agent-review:v1 enrollment=kbp-codex-kingmaker-buff-planner-umm-0311-baseline-20261123182207-144d key=r05-dispositions-5ecfc6f00e part=1/1 sha256=5ecfc6f00ef2e79993602e8782b486f632769c25890afee9dbb3a5fbd3935b11 -->
### Claude Opus — finding dispositions — round 5

Candidate after corrections: `45427841ae209ace947ec68541edcc18e3e09ce3`

- **R1-F3** — `fixed` — commits: `0f4cc30`, `51e8776`, `aebe2ed`, `4542784`
  docs/evidence/umm-0311/loader-baseline-r6.json (111/111 on a temporary clone of aebe2ed; code 51e8776): project-extension-target-is-imported-by-msbuild (MSBuild's preprocessor shows the production project importing an ignored obj\KingmakerBuffPlanner.csproj.local.targets whose KbpVersion change applies only to a real compile), receipt-with-project-extension-import-rejected and build-with-project-extension-import-rejected ('Project-extension imports apply to the production build'), receipt-after-project-extension-import-removed-accepted; the reference-relocation controls and the earlier local-import refusals still pass.
- **R4-F1** — `fixed` — commits: `0f4cc30`, `51e8776`, `aebe2ed`, `4542784`
  docs/evidence/umm-0311/loader-baseline-r6.json (111/111 on a temporary clone of aebe2ed; code 51e8776): release-with-disagreeing-builds-keeps-previous-release (the instrumented Build-Release copy lives under ignored artifacts, the checkout is clean, both producing Build-Local runs execute; the first producer is replaced by an internally consistent pair, its ZIP rewritten with one entry timestamp changed and its receipt naming that ZIP; refusal 'Clean-head deterministic build failed: package <A>/<B> DLL <same>/<same>', the previous ZIP and manifest hashes unchanged, no private folder left).
- **R4-F4** — `fixed` — commits: `0f4cc30`, `51e8776`, `aebe2ed`, `4542784`
  docs/evidence/umm-0311/loader-baseline-r6.json (111/111 on a temporary clone of aebe2ed; code 51e8776): the reconciliation helper reads two independent receipts and mutates only the second: release-reconciliation-empty-array-, string-false- and number-zero-dirty-flag-rejected and release-reconciliation-dirty-producing-receipt-rejected each refuse with the first receipt clean; release-reconciliation-matching-clean-pair-accepted is the control.
- **R5-F1** — `fixed` — commits: `0f4cc30`, `51e8776`, `aebe2ed`, `4542784`
  docs/evidence/umm-0311/loader-baseline-r6.json (111/111 on a temporary clone of aebe2ed; code 51e8776): release-occupied-destination-keeps-previous-release-at-recovery-path (an instrumented Build-Release occupies the versioned path right after moving the previous release aside: refused, the occupant untouched, the previous ZIP, manifest and notes byte-identical at the private recovery path the error names), release-fixture-restored-after-occupied-destination.
- **R5-F2** — `fixed` — commits: `0f4cc30`, `51e8776`, `aebe2ed`, `4542784`
  docs/evidence/umm-0311/loader-baseline-r6.json (111/111 on a temporary clone of aebe2ed; code 51e8776): receipt-with-test-project-user-file-accepted, build-with-test-project-user-file-succeeds and receipt-after-build-with-test-project-user-file-accepted (an ignored tests\...\KingmakerBuffPlanner.Tests.csproj.user); receipt-with-production-project-user-file-rejected and build-with-production-project-user-file-rejected (the production project's own .user file still refused), receipt-after-production-user-file-removed-accepted.
- **R5-F3** — `fixed` — commits: `0f4cc30`, `51e8776`, `aebe2ed`, `4542784`
  docs/UMM-COMPATIBILITY.md at aebe2ed, section 'Round 6': names 04c55bc as the last production-source change (the four RuntimeTesting files listed by git diff --name-status 75aeeff..c2a594c -- src), attaches round-2 protocol observations to the round-2 freeze 75aeeff only, and states that later commits change tooling, tests, documentation or evidence; the round-5 frozen-product section is retitled as history with the same correction.

**Validation (developer-reported):**

| Command | Required | Result | Duration | Scope and limits |
|---|---|---|---|---|
| python C:/Dev/agent-review/agent_review.py verify --enrollment kbp-codex-kingmaker-buff-planner-umm-0311-baseline-20261123182207-144d --gate focused -- -Only localization | yes | pass | 11.4s | Controller receipt on 4542784 (gate-focused-45427841ae20-d4b9488d083-a7f0c05bad5-20261126T141543986660Z.log): source validation, Release build and the localization selection. |
| scripts/Test-LoaderBaseline.ps1 -ReportPath <...> (temporary clone of aebe2ed) | no | pass | 355s | docs/evidence/umm-0311/loader-baseline-r6.json (111/111 on a temporary clone of aebe2ed; code 51e8776). Earlier direct runs on the code commits: 0f4cc30 failed 4 of its own new fixture assertions (206 s; a recovery path containing spaces and console-wrapped digests were matched too strictly; fixture-only, corrected in 51e8776), 51e8776 111/111 (229 s). Counted in the mission's validation ledger. |
| Native checks | no | not_run | 0 | Not authorized. |

_Model-generated developer response (Claude opus / high), posted by the agent-review controller through the owner's GitHub account; not a human review._
