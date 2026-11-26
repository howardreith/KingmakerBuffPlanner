<!-- agent-review:v1 enrollment=kbp-codex-kingmaker-buff-planner-umm-0311-baseline-20261123182207-144d key=r04-review part=1/1 sha256=d995fe78edbf4db09a66447a8d7cd63ed4759c33399f90141a047c4d340a5cba -->
### Codex GPT-6.2 Astra — review — round 4 — candidate `e2aa5aebed`

**Decision:** `changes_required` — The pinned compiler references and four-loader static audit are supported by the evidence. Frozen reuse can still accept a package after a material compiler input changes. A failed release rebuild can also erase the previous validated package, and a cleanup failure can leave the deployment lock without a recovery record.

Continuation for Claude

Finish R1-F3 by binding or refusing effective local MSBuild imports at receipt creation and reuse. Validate both producing receipts and the replacement ZIP before replacing a prior local release. Keep the admitted request in memory and make owned-lock recovery independent of private-file deletion. Correct the documented command for the full loader identity suite. Use focused temporary fixtures for the findings below, retain the original artifact attribution, and run the configured gate within the existing Buff Planner permissions and review budget.

#### Findings

**R1-F3** · P1 · required_gap · blocks readiness — A production-only MSBuild import can leave a frozen receipt admissible
- Location: `scripts/Common.ps1:90`; acceptance: mission.md:156, mission.md:194
- Trigger: After freezing a clean package, select a local targets file through DirectoryBuildTargetsPath that applies a material setting only when SkipCompilerExecution is false or unset.
- Expected: Changing a material input to the producing compile invalidates the frozen receipt or causes the unsupported configuration to be refused.
- Actual: Assert-KbpLocalBuildConfiguration checks GamePath.props and conventionally named files above the repository. MSBuild can also import a local targets file selected by DirectoryBuildTargetsPath. An ignored or external target can change KbpVersion only during real compilation. The skipped-compiler evaluation then yields the previous inventory, and the target file is outside the fingerprint's enumerated inputs.
- Why it blocks / does not: The launcher can accept the old frozen package at a clean HEAD even though a production build at that HEAD would generate different BuildInfo and DLL bytes.
- Requested correction: Account for the effective local MSBuild imports or explicitly prevent unsupported imports in the build and reuse evaluations. Document which entry points enforce the restriction. The current restriction is called through Get-KbpCompilerInputs, while plain Build.ps1 and direct MSBuild do not call it.
- Requested evidence: In a temporary clean checkout at one HEAD, select a local target that changes generated BuildInfo only during real compilation. Show that the old receipt is refused, then restore the input and show acceptance. Retain the byte-identical game-reference relocation control.

**R4-F1** · P2 · defect · blocks readiness — Release reconciliation runs after the previous validated release is deleted
- Location: `scripts/Build-Release.ps1:29`; acceptance: mission.md:155, mission.md:194, mission.md:255
- Trigger: Build the same version again when a valid local release already exists and the two new producing builds disagree.
- Expected: A failed deterministic rebuild leaves the previous validated local release available.
- Actual: Build-Release recursively removes the existing versioned release directory at line 29. It compares the producing builds and reconciles the replacement ZIP at line 42. Before this round, a disagreement between the two build hashes stopped the script before that removal.
- Why it blocks / does not: A rejected rebuild can destroy the last validated ZIP and manifest for the version and leave an unreceipted replacement in their directory.
- Requested correction: Prepare and reconcile the replacement under a private artifacts directory. Replace the versioned release only after its ZIP and manifest are ready, preserving the existing path ownership checks.
- Requested evidence: Seed a temporary versioned release with a validated ZIP and manifest, force the producing builds to disagree, and verify that both previous file hashes survive the refusal.

**R4-F2** · P2 · defect · blocks readiness — A cleanup error can strand the deployment lock without transaction state
- Location: `scripts/RuntimeHarness.Common.ps1:940`; acceptance: mission.md:235, mission.md:240
- Trigger: The first state write fails after copying the ZIP, and deleting that copy raises a sharing or access error.
- Expected: A pre-state cleanup failure either releases the run's verified lock or leaves a recovery record the guarded workflow can use.
- Actual: The pre-state catch removes package.zip before entering the lock-release block. With the public caller's terminating-error setting, a Windows sharing violation at Remove-Item exits the catch before Remove-KbpOwnedLock runs. Restore-KbpRuntimeTransaction requires the transaction.json that failed to be written.
- Why it blocks / does not: A private-file cleanup error can block every subsequent deployment even though the transaction never changed Mods and has no state record for restoration.
- Requested correction: Attempt token-checked lock release independently of private-copy and directory cleanup. Preserve the original failure and identify any owned path that still needs cleanup.
- Requested evidence: In fixture mode, fail the first state write while the owned ZIP temporarily denies deletion. Verify unchanged Mods and that the failure leaves a usable guarded recovery or releases the owned lock.

**R4-F3** · P2 · defect · blocks readiness — The prelaunch snapshot can capture a different request from the one written
- Location: `scripts/RuntimeHarness.Common.ps1:611`; acceptance: mission.md:239, mission.md:240, mission.md:242
- Trigger: The request path changes after the atomic write and before Save-KbpRuntimeRequest reads it back.
- Expected: The retained expectation represents the exact request that the launcher admitted and serialized.
- Actual: Save-KbpRuntimeRequest atomically writes the request, then opens the destination again to construct the retained object. A replacement between those operations becomes the result reader's expectation. Completion still uses the originally selected loader. The reread also uses PowerShell's default encoding for a file written as BOM-less UTF-8.
- Why it blocks / does not: The result and completion record can still use different loader expectations. The new fixture changes the file only after the helper has returned.
- Requested correction: Retain the original request in a representation the reader accepts, or derive the snapshot from the same in-memory serialization passed to the atomic writer.
- Requested evidence: Use a private writer fixture that replaces the destination at the write boundary. Verify that the retained request preserves its original loader and non-ASCII text and refuses a result for the replacement row.

**R4-F4** · P2 · defect · blocks readiness — Release reconciliation accepts malformed clean-build flags
- Location: `scripts/Common.ps1:110`; acceptance: mission.md:155, mission.md:194
- Trigger: A producing receipt has matching hashes and revisions but a non-Boolean sourceDirty field.
- Expected: Each producing receipt supplies a Boolean sourceDirty with value false.
- Actual: The release helper tests sourceDirty with a coercing comparison to $false. PowerShell evaluates the rejection expression as false for an empty JSON array, numeric zero and the string "false". The runtime receipt reader already requires a Boolean for the same field.
- Why it blocks / does not: A malformed producing receipt can be certified as a clean deterministic build without supplying a Boolean cleanliness assertion.
- Requested correction: Require sourceDirty to exist, have Boolean type, and be false before accepting either release producer.
- Requested evidence: Use the matching release pair as a control. Change only one receipt's sourceDirty to an empty array or the string "false" and show refusal.

**R4-F5** · P2 · required_gap · blocks readiness — The documented identity-suite command skips eleven deployment checks
- Location: `docs/BUILD-AND-RELEASE.md:45`; acceptance: mission.md:200, mission.md:253
- Trigger: Follow the offline reproduction command in the build guide for the reported 37-case suite.
- Expected: The documented invocation exercises the four-row deployment checks against the intended Build-Local package.
- Actual: The guide supplies only -PackageDirectory. The eleven direct deployment, wrapper and fixture-preservation checks run only with -CandidatePackage, so the documented invocation selects 26 of the recorded 37 cases. Supplying a relative ZIP path can also select the wrong directory because the test resolves it against the process directory.
- Why it blocks / does not: A developer following the reproduction instructions can receive a passing result while skipping the public deployment behavior that closed R2-F1.
- Requested correction: Show the full invocation with -CandidatePackage and identify the reduced scope when it is omitted. Resolve the supplied package using PowerShell filesystem-location semantics in Test-RuntimeLoaderIdentity.ps1.
- Requested evidence: From a checkout reached with Set-Location, run the documented command with a relative candidate ZIP. Retain the four matching-row deployment and wrapper observations, the crossed-row refusals, and the unchanged fixture result.

**R4-F6** · P3 · optional_polish · non-blocking — Mixed-case owner loader IDs pass parameter binding and fail row lookup
- Location: `scripts/compatibility/CompatibilityProfile.Common.ps1:83`; acceptance: mission.md:239
- Trigger: Pass -LoaderId Owner-0.33.0 through the compatibility wrapper on Windows.
- Expected: A loader ID accepted by PowerShell parameter binding resolves to its canonical row.
- Actual: ValidateSet accepts Owner-0.33.0 and preserves that spelling. Get-KbpLoaderIdentity then rejects the canonical owner-0.33.0 value stored in the row file because the comparison is case-sensitive.
- Why it blocks / does not: All four documented canonical spellings work. This affects CLI consistency for a casing variant.
- Requested correction: Canonicalize the bound ID in the shared resolver before checking the stored row ID and deriving its identity.
- Requested evidence: A mixed-case owner ID resolves to the same canonical loader identity as the documented spelling.

#### Prior finding assessments

- **R1-F1** — `fix_verified` — Both launcher deployment calls and the compatibility wrapper now forward LoaderId. The four canonical rows clear the loader check on matching private installations, and crossed rows are refused.
- **R1-F3** — `fix_rejected` — The GamePath.props cases are refused, compiler argument order is preserved, and the default compiler executable is hashed. The skipped-compiler evaluation can still miss a production-only setting from a redirected local MSBuild import.
- **R2-F1** — `fix_verified` — The recorded tests invoke Deploy-Local -WhatIf with a valid package and private game directories. The Harmony2-only mismatch is refused before staging, the matching row reaches a later host guard, and the fixture Mods directory stays empty.
- **R3-F1** — `fix_verified` — The release digest is now compared with both measured producing packages and their receipts. The same-DLL, different-data substitution and a Boolean dirty producer were refused. R4-F1 and R4-F4 cover separate failure cases in the new reconciliation path.
- **R3-F2** — `fix_verified` — The result reader now uses the object retained before launch. The recorded post-admission file replacement leaves the original row effective. R4-F3 concerns the earlier interval in which that object is created.
- **R3-F3** — `fix_verified` — Install-Local reconciles embeddedCommit with both release.commit and the measured DLL stamp before staging. Changing only embeddedCommit was refused, and the matching receipt cleared reconciliation.
- **R3-F4** — `fix_verified` — The injected state-write failure left zero owned copies, unchanged Mods and no lock. A retry with the same run ID succeeded. R4-F2 covers a subsequent error during cleanup itself.
- **R3-F5** — `fix_verified` — The compatibility guide identifies 8ac5738 and its retained ZIP, DLL and MVID. The linked record identifies 0e26eed for the revision-stamp comparison. Earlier products and suite counts are historical, and the controller build at H has its own digest.

#### Evidence assessment

- **Inspected:** The enrolled mission, AGENTS.md, validation policy, developer report, prior ledger, transcript availability notice, and controller gate receipt and log.; The full feature diff and round-four corrections, including the MSBuild inputs, build and release receipts, package validator, installer, deployment transaction, launcher, result reader and their fixture tests.; The compatibility guide, build instructions, loader rows, evidence index, case-level outputs, retained receipt, package read-back, revision-stamp comparison and static audit.
- **Executed verification:** The controller receipt and matching log confirm scripts/Test-Development.ps1 -Only localization at H. Source validation passed 47/47, the Release build passed 1/1, and protocol tests passed 14/14 in an 8.7-second gate. The DLL SHA-256 was c05e8905851d94d9a870e168073fa2d0306d6a4ad657ca076d293dd0e4d54136.; I matched all 46 feature-diff sections against Git, verified all 12 indexed evidence blob SHA-256 values, and cross-checked the frozen ZIP, DLL, MVID and source revision across the retained records.; The committed boundary-suite output records 90/90 at 8ac5738. The runtime identity export records 37/37 and its index attributes it to 74a9efc. Git confirms that only Test-LoaderBaseline.ps1 changed between those revisions and that only documentation and evidence changed from 8ac5738 to H.; The static audit records 8 type references and 24 member references resolved on each of the four rows for DLL 80e0a1fb. I matched the three loader component identities in each row against the profile inventory and audit.; Read-only PowerShell checks confirmed the dirty-flag comparison behavior, mixed-case ValidateSet binding, and the divergence between the PowerShell location and process directory after Push-Location.
- **Source inspection only:** The redirected MSBuild import, release-directory deletion order, prelaunch request reread and cleanup-exception paths were assessed from source. The supplied suites do not exercise those specific failure cases.; Both launcher deployment calls forward LoaderId. The full launcher was inspected, while the recorded behavioral runs exercised the public deployment and compatibility wrappers with private fixtures.
- **Required gaps:** Show stale-receipt refusal for an effective production-only MSBuild import at unchanged clean HEAD.; Show that malformed producing receipts and failed release reconciliation preserve the previous validated local release.; Show recoverable pre-state cleanup when deletion fails and request identity preservation across the prelaunch write boundary.; Provide a reproducible PowerShell invocation for the full private deployment checks.
- **Limitations:** The foreground command transcript was withheld. The controller gate has an independently recorded command and log at H. The broader suites are supported by committed case-level exports and source continuity.; The snapshot permitted read-only review. I did not rebuild the retained ZIP or execute installation, deployment or game workflows.; Native behavior and combined-stack compatibility are recorded as NOT RUN. The supplied harness targets the owner's installation and no approved disposable game copy was available.

#### Summary

- **Delivered behavior:** Buff Planner 0.6.20 declares UMM 0.31.1. The retained package references UnityModManager 0.31.1.0 and 0Harmony12 1.2.0.1. Static API checks cover 0.31.1, 0.32.4, the owner's 0.33.0 and 0.33.4 on the same frozen DLL. The canonical loader selection reaches both deployment calls and the compatibility wrapper.
- **Scope reviewed:** The final Buff Planner checkpoint for PR #26, including all eight pending findings, the round-four corrections and their directly affected build, packaging, deployment, result-reading and documentation paths.
- **Pending later-phase work:** none
- **Unresolved questions:** none
- **Remaining owner review:** The owner decides the disposition of draft PR #26 after the blocking corrections.; Native qualification would require an owner-approved disposable installation and fixtures for settings, language, planner state, reload and representative casting.

#### Provenance (controller-recorded)

| | Requested | Observed |
|---|---|---|
| Model | `gpt-6.2-astra` | `gpt-6.2-astra` (turns ['gpt-6.2-astra']) |
| Reasoning effort | `max` | `max` (turns ['max']) |
| Sandbox / approvals | read-only / never | `read-only` / `never` |

Owner guidance bound to this review: none.

Owner amendments bound to this review: none.

Codex CLI `0.170.0`, thread `01a20e23-7008-7402-9488-2021fb8ce658`, duration 834.1s, response sha256 `dd02bd6343439e5b13ad24edf1a6848048b2bb9f24b3a5324d9c56edd805eee9`, request `kbp-codex-kingmaker-buff-planner-umm-0311-baseline-20261123182207-144d-r04-9c8290`. Observed values come from the Codex session rollout's recorded thread settings, not from the model's own statements; the serving backend's model identity is not separately exposed.

<details><summary>Complete structured review (agent-review.review.v1)</summary>

```json
{
  "candidate_commit": "e2aa5aebed622d041ce750adf8f710142ba37819",
  "checkpoint_id": "final",
  "checkpoint_type": "final",
  "decision": "changes_required",
  "decision_rationale": "The pinned compiler references and four-loader static audit are supported by the evidence. Frozen reuse can still accept a package after a material compiler input changes. A failed release rebuild can also erase the previous validated package, and a cleanup failure can leave the deployment lock without a recovery record.\n\nContinuation for Claude\n\nFinish R1-F3 by binding or refusing effective local MSBuild imports at receipt creation and reuse. Validate both producing receipts and the replacement ZIP before replacing a prior local release. Keep the admitted request in memory and make owned-lock recovery independent of private-file deletion. Correct the documented command for the full loader identity suite. Use focused temporary fixtures for the findings below, retain the original artifact attribution, and run the configured gate within the existing Buff Planner permissions and review budget.",
  "evidence_assessment": {
    "executed_verification": [
      "The controller receipt and matching log confirm scripts/Test-Development.ps1 -Only localization at H. Source validation passed 47/47, the Release build passed 1/1, and protocol tests passed 14/14 in an 8.7-second gate. The DLL SHA-256 was c05e8905851d94d9a870e168073fa2d0306d6a4ad657ca076d293dd0e4d54136.",
      "I matched all 46 feature-diff sections against Git, verified all 12 indexed evidence blob SHA-256 values, and cross-checked the frozen ZIP, DLL, MVID and source revision across the retained records.",
      "The committed boundary-suite output records 90/90 at 8ac5738. The runtime identity export records 37/37 and its index attributes it to 74a9efc. Git confirms that only Test-LoaderBaseline.ps1 changed between those revisions and that only documentation and evidence changed from 8ac5738 to H.",
      "The static audit records 8 type references and 24 member references resolved on each of the four rows for DLL 80e0a1fb. I matched the three loader component identities in each row against the profile inventory and audit.",
      "Read-only PowerShell checks confirmed the dirty-flag comparison behavior, mixed-case ValidateSet binding, and the divergence between the PowerShell location and process directory after Push-Location."
    ],
    "inspected": [
      "The enrolled mission, AGENTS.md, validation policy, developer report, prior ledger, transcript availability notice, and controller gate receipt and log.",
      "The full feature diff and round-four corrections, including the MSBuild inputs, build and release receipts, package validator, installer, deployment transaction, launcher, result reader and their fixture tests.",
      "The compatibility guide, build instructions, loader rows, evidence index, case-level outputs, retained receipt, package read-back, revision-stamp comparison and static audit."
    ],
    "limitations": [
      "The foreground command transcript was withheld. The controller gate has an independently recorded command and log at H. The broader suites are supported by committed case-level exports and source continuity.",
      "The snapshot permitted read-only review. I did not rebuild the retained ZIP or execute installation, deployment or game workflows.",
      "Native behavior and combined-stack compatibility are recorded as NOT RUN. The supplied harness targets the owner's installation and no approved disposable game copy was available."
    ],
    "required_gaps": [
      "Show stale-receipt refusal for an effective production-only MSBuild import at unchanged clean HEAD.",
      "Show that malformed producing receipts and failed release reconciliation preserve the previous validated local release.",
      "Show recoverable pre-state cleanup when deletion fails and request identity preservation across the prelaunch write boundary.",
      "Provide a reproducible PowerShell invocation for the full private deployment checks."
    ],
    "source_inspection_only": [
      "The redirected MSBuild import, release-directory deletion order, prelaunch request reread and cleanup-exception paths were assessed from source. The supplied suites do not exercise those specific failure cases.",
      "Both launcher deployment calls forward LoaderId. The full launcher was inspected, while the recorded behavioral runs exercised the public deployment and compatibility wrappers with private fixtures."
    ]
  },
  "findings": [
    {
      "acceptance_ids": [
        "mission.md:156",
        "mission.md:194"
      ],
      "actual": "Assert-KbpLocalBuildConfiguration checks GamePath.props and conventionally named files above the repository. MSBuild can also import a local targets file selected by DirectoryBuildTargetsPath. An ignored or external target can change KbpVersion only during real compilation. The skipped-compiler evaluation then yields the previous inventory, and the target file is outside the fingerprint's enumerated inputs.",
      "blocking_rationale": "The launcher can accept the old frozen package at a clean HEAD even though a production build at that HEAD would generate different BuildInfo and DLL bytes.",
      "blocks_readiness": true,
      "category": "required_gap",
      "expected": "Changing a material input to the producing compile invalidates the frozen receipt or causes the unsupported configuration to be refused.",
      "id": "R1-F3",
      "location": {
        "file": "scripts/Common.ps1",
        "line": 90
      },
      "priority": "P1",
      "requested_correction": "Account for the effective local MSBuild imports or explicitly prevent unsupported imports in the build and reuse evaluations. Document which entry points enforce the restriction. The current restriction is called through Get-KbpCompilerInputs, while plain Build.ps1 and direct MSBuild do not call it.",
      "requested_evidence": "In a temporary clean checkout at one HEAD, select a local target that changes generated BuildInfo only during real compilation. Show that the old receipt is refused, then restore the input and show acceptance. Retain the byte-identical game-reference relocation control.",
      "title": "A production-only MSBuild import can leave a frozen receipt admissible",
      "trigger": "After freezing a clean package, select a local targets file through DirectoryBuildTargetsPath that applies a material setting only when SkipCompilerExecution is false or unset."
    },
    {
      "acceptance_ids": [
        "mission.md:155",
        "mission.md:194",
        "mission.md:255"
      ],
      "actual": "Build-Release recursively removes the existing versioned release directory at line 29. It compares the producing builds and reconciles the replacement ZIP at line 42. Before this round, a disagreement between the two build hashes stopped the script before that removal.",
      "blocking_rationale": "A rejected rebuild can destroy the last validated ZIP and manifest for the version and leave an unreceipted replacement in their directory.",
      "blocks_readiness": true,
      "category": "defect",
      "expected": "A failed deterministic rebuild leaves the previous validated local release available.",
      "id": "R4-F1",
      "location": {
        "file": "scripts/Build-Release.ps1",
        "line": 29
      },
      "priority": "P2",
      "requested_correction": "Prepare and reconcile the replacement under a private artifacts directory. Replace the versioned release only after its ZIP and manifest are ready, preserving the existing path ownership checks.",
      "requested_evidence": "Seed a temporary versioned release with a validated ZIP and manifest, force the producing builds to disagree, and verify that both previous file hashes survive the refusal.",
      "title": "Release reconciliation runs after the previous validated release is deleted",
      "trigger": "Build the same version again when a valid local release already exists and the two new producing builds disagree."
    },
    {
      "acceptance_ids": [
        "mission.md:235",
        "mission.md:240"
      ],
      "actual": "The pre-state catch removes package.zip before entering the lock-release block. With the public caller's terminating-error setting, a Windows sharing violation at Remove-Item exits the catch before Remove-KbpOwnedLock runs. Restore-KbpRuntimeTransaction requires the transaction.json that failed to be written.",
      "blocking_rationale": "A private-file cleanup error can block every subsequent deployment even though the transaction never changed Mods and has no state record for restoration.",
      "blocks_readiness": true,
      "category": "defect",
      "expected": "A pre-state cleanup failure either releases the run's verified lock or leaves a recovery record the guarded workflow can use.",
      "id": "R4-F2",
      "location": {
        "file": "scripts/RuntimeHarness.Common.ps1",
        "line": 940
      },
      "priority": "P2",
      "requested_correction": "Attempt token-checked lock release independently of private-copy and directory cleanup. Preserve the original failure and identify any owned path that still needs cleanup.",
      "requested_evidence": "In fixture mode, fail the first state write while the owned ZIP temporarily denies deletion. Verify unchanged Mods and that the failure leaves a usable guarded recovery or releases the owned lock.",
      "title": "A cleanup error can strand the deployment lock without transaction state",
      "trigger": "The first state write fails after copying the ZIP, and deleting that copy raises a sharing or access error."
    },
    {
      "acceptance_ids": [
        "mission.md:239",
        "mission.md:240",
        "mission.md:242"
      ],
      "actual": "Save-KbpRuntimeRequest atomically writes the request, then opens the destination again to construct the retained object. A replacement between those operations becomes the result reader's expectation. Completion still uses the originally selected loader. The reread also uses PowerShell's default encoding for a file written as BOM-less UTF-8.",
      "blocking_rationale": "The result and completion record can still use different loader expectations. The new fixture changes the file only after the helper has returned.",
      "blocks_readiness": true,
      "category": "defect",
      "expected": "The retained expectation represents the exact request that the launcher admitted and serialized.",
      "id": "R4-F3",
      "location": {
        "file": "scripts/RuntimeHarness.Common.ps1",
        "line": 611
      },
      "priority": "P2",
      "requested_correction": "Retain the original request in a representation the reader accepts, or derive the snapshot from the same in-memory serialization passed to the atomic writer.",
      "requested_evidence": "Use a private writer fixture that replaces the destination at the write boundary. Verify that the retained request preserves its original loader and non-ASCII text and refuses a result for the replacement row.",
      "title": "The prelaunch snapshot can capture a different request from the one written",
      "trigger": "The request path changes after the atomic write and before Save-KbpRuntimeRequest reads it back."
    },
    {
      "acceptance_ids": [
        "mission.md:155",
        "mission.md:194"
      ],
      "actual": "The release helper tests sourceDirty with a coercing comparison to $false. PowerShell evaluates the rejection expression as false for an empty JSON array, numeric zero and the string \"false\". The runtime receipt reader already requires a Boolean for the same field.",
      "blocking_rationale": "A malformed producing receipt can be certified as a clean deterministic build without supplying a Boolean cleanliness assertion.",
      "blocks_readiness": true,
      "category": "defect",
      "expected": "Each producing receipt supplies a Boolean sourceDirty with value false.",
      "id": "R4-F4",
      "location": {
        "file": "scripts/Common.ps1",
        "line": 110
      },
      "priority": "P2",
      "requested_correction": "Require sourceDirty to exist, have Boolean type, and be false before accepting either release producer.",
      "requested_evidence": "Use the matching release pair as a control. Change only one receipt's sourceDirty to an empty array or the string \"false\" and show refusal.",
      "title": "Release reconciliation accepts malformed clean-build flags",
      "trigger": "A producing receipt has matching hashes and revisions but a non-Boolean sourceDirty field."
    },
    {
      "acceptance_ids": [
        "mission.md:200",
        "mission.md:253"
      ],
      "actual": "The guide supplies only -PackageDirectory. The eleven direct deployment, wrapper and fixture-preservation checks run only with -CandidatePackage, so the documented invocation selects 26 of the recorded 37 cases. Supplying a relative ZIP path can also select the wrong directory because the test resolves it against the process directory.",
      "blocking_rationale": "A developer following the reproduction instructions can receive a passing result while skipping the public deployment behavior that closed R2-F1.",
      "blocks_readiness": true,
      "category": "required_gap",
      "expected": "The documented invocation exercises the four-row deployment checks against the intended Build-Local package.",
      "id": "R4-F5",
      "location": {
        "file": "docs/BUILD-AND-RELEASE.md",
        "line": 45
      },
      "priority": "P2",
      "requested_correction": "Show the full invocation with -CandidatePackage and identify the reduced scope when it is omitted. Resolve the supplied package using PowerShell filesystem-location semantics in Test-RuntimeLoaderIdentity.ps1.",
      "requested_evidence": "From a checkout reached with Set-Location, run the documented command with a relative candidate ZIP. Retain the four matching-row deployment and wrapper observations, the crossed-row refusals, and the unchanged fixture result.",
      "title": "The documented identity-suite command skips eleven deployment checks",
      "trigger": "Follow the offline reproduction command in the build guide for the reported 37-case suite."
    },
    {
      "acceptance_ids": [
        "mission.md:239"
      ],
      "actual": "ValidateSet accepts Owner-0.33.0 and preserves that spelling. Get-KbpLoaderIdentity then rejects the canonical owner-0.33.0 value stored in the row file because the comparison is case-sensitive.",
      "blocking_rationale": "All four documented canonical spellings work. This affects CLI consistency for a casing variant.",
      "blocks_readiness": false,
      "category": "optional_polish",
      "expected": "A loader ID accepted by PowerShell parameter binding resolves to its canonical row.",
      "id": "R4-F6",
      "location": {
        "file": "scripts/compatibility/CompatibilityProfile.Common.ps1",
        "line": 83
      },
      "priority": "P3",
      "requested_correction": "Canonicalize the bound ID in the shared resolver before checking the stored row ID and deriving its identity.",
      "requested_evidence": "A mixed-case owner ID resolves to the same canonical loader identity as the documented spelling.",
      "title": "Mixed-case owner loader IDs pass parameter binding and fail row lookup",
      "trigger": "Pass -LoaderId Owner-0.33.0 through the compatibility wrapper on Windows."
    }
  ],
  "prior_finding_assessments": [
    {
      "assessment": "fix_verified",
      "finding_id": "R1-F1",
      "rationale": "Both launcher deployment calls and the compatibility wrapper now forward LoaderId. The four canonical rows clear the loader check on matching private installations, and crossed rows are refused."
    },
    {
      "assessment": "fix_rejected",
      "finding_id": "R1-F3",
      "rationale": "The GamePath.props cases are refused, compiler argument order is preserved, and the default compiler executable is hashed. The skipped-compiler evaluation can still miss a production-only setting from a redirected local MSBuild import."
    },
    {
      "assessment": "fix_verified",
      "finding_id": "R2-F1",
      "rationale": "The recorded tests invoke Deploy-Local -WhatIf with a valid package and private game directories. The Harmony2-only mismatch is refused before staging, the matching row reaches a later host guard, and the fixture Mods directory stays empty."
    },
    {
      "assessment": "fix_verified",
      "finding_id": "R3-F1",
      "rationale": "The release digest is now compared with both measured producing packages and their receipts. The same-DLL, different-data substitution and a Boolean dirty producer were refused. R4-F1 and R4-F4 cover separate failure cases in the new reconciliation path."
    },
    {
      "assessment": "fix_verified",
      "finding_id": "R3-F2",
      "rationale": "The result reader now uses the object retained before launch. The recorded post-admission file replacement leaves the original row effective. R4-F3 concerns the earlier interval in which that object is created."
    },
    {
      "assessment": "fix_verified",
      "finding_id": "R3-F3",
      "rationale": "Install-Local reconciles embeddedCommit with both release.commit and the measured DLL stamp before staging. Changing only embeddedCommit was refused, and the matching receipt cleared reconciliation."
    },
    {
      "assessment": "fix_verified",
      "finding_id": "R3-F4",
      "rationale": "The injected state-write failure left zero owned copies, unchanged Mods and no lock. A retry with the same run ID succeeded. R4-F2 covers a subsequent error during cleanup itself."
    },
    {
      "assessment": "fix_verified",
      "finding_id": "R3-F5",
      "rationale": "The compatibility guide identifies 8ac5738 and its retained ZIP, DLL and MVID. The linked record identifies 0e26eed for the revision-stamp comparison. Earlier products and suite counts are historical, and the controller build at H has its own digest."
    }
  ],
  "request_id": "kbp-codex-kingmaker-buff-planner-umm-0311-baseline-20261123182207-144d-r04-9c8290",
  "round": 4,
  "schema_version": "agent-review.review.v1",
  "summary": {
    "delivered_behavior": "Buff Planner 0.6.20 declares UMM 0.31.1. The retained package references UnityModManager 0.31.1.0 and 0Harmony12 1.2.0.1. Static API checks cover 0.31.1, 0.32.4, the owner's 0.33.0 and 0.33.4 on the same frozen DLL. The canonical loader selection reaches both deployment calls and the compatibility wrapper.",
    "pending_later_phase_work": [],
    "remaining_owner_review": [
      "The owner decides the disposition of draft PR #26 after the blocking corrections.",
      "Native qualification would require an owner-approved disposable installation and fixtures for settings, language, planner state, reload and representative casting."
    ],
    "scope_reviewed": "The final Buff Planner checkpoint for PR #26, including all eight pending findings, the round-four corrections and their directly affected build, packaging, deployment, result-reading and documentation paths.",
    "unresolved_questions": []
  }
}
```
</details>

_Model-generated review by Codex using the requested model, posted by the agent-review controller through the owner's GitHub account. Not a human review or approval._
