<!-- agent-review:v1 enrollment=kbp-codex-kingmaker-buff-planner-umm-0311-baseline-20261123182207-144d key=r01-review part=1/1 sha256=c75fbd4a3d8d8866ec2a55a348c719e15d8aaec32c993427dc784f7a7c76c452 -->
### Codex GPT-6.2 Astra — review — round 1 — candidate `ee05e37652`

**Decision:** `changes_required` — The production compiler pin is in place and the focused gate passed. The runtime harness still assumes one loader, and the package receipts do not establish which source, DLL, and build inputs are being reused. Both are explicit mission requirements.

## Continuation for Claude

Carry one measured loader profile through Buff Planner’s preflight, runtime request, in-process observation, result reader, and reuse checks. Bind candidate receipts to the DLL inside the validated ZIP and to the material inputs that produced it. Apply the same pin rules in standalone package validation and correct relative cache resolution.

Exercise these boundaries with temporary files and offline fixtures. Freeze one corrected package, record its source, ZIP and DLL hashes and MVID, and run the four static profile comparisons on those bytes. Run the configured focused gate and distinguish any separately stamped gate build. Update the current build instructions and restore the historical 0.5.3 release copy. Keep the existing runtime guards in force and all native rows marked NOT RUN while an approved disposable game copy is unavailable.

#### Findings

**R1-F1** · P1 · required_gap · blocks readiness — Runtime validation is hard-coded to UMM 0.33.0 and omits Harmony2
- Location: `scripts/RuntimeAutomation.Common.ps1:472`; acceptance: mission.md:220, mission.md:239, mission.md:242
- Trigger: Validate a legitimate 0.31.1, 0.32.4, or 0.33.4 result, or keep the owner’s UMM and Harmony12 files while changing Harmony2.
- Expected: Preflight, the request, the loaded process, the result reader, and reusable evidence agree on one selected loader stack, including Harmony2.
- Actual: The launcher selects an optional-mod profile. Assert-KbpRuntimeResult requires the owner’s UMM 0.33.0.0 version and hash for every result. RuntimeTestHost records UMM and the on-disk Harmony12 bridge, omits the underlying 0Harmony.dll identity, and the compatibility digest covers only the optional mods.
- Why it blocks / does not: A correct baseline or newer-loader result would fail the reader. A Harmony2-only mismatch on the owner row is invisible to the loader identity checks.
- Requested correction: Carry a verified loader identity alongside the optional-mod profile. Compare the observed loaded assemblies with that identity and include it in receipts and checks that admit earlier results.
- Requested evidence: Exercise matching records for the selected profiles and reject a different UMM row and a Harmony2-only mismatch in offline preflight and result-reader tests.

**R1-F2** · P1 · defect · blocks readiness — The build receipt can describe a different DLL from the packaged one
- Location: `scripts/Build-Local.ps1:58`; acceptance: mission.md:155, mission.md:194, mission.md:202
- Trigger: A receipt carries stale DLL metadata, or another build replaces the shared output DLL after the packager copies it.
- Expected: The receipt’s source revision, version, DLL hash, and MVID match the actual packaged assembly before the candidate is accepted for staging.
- Actual: Build-Local reads the DLL hash and MVID from the build output after copying the DLL into the package. Read-KbpBuildManifest checks HEAD and the whole ZIP hash but trusts the DLL fields. Install-Local accepts a syntactically valid commit and nonempty MVID, then checks the DLL hash only after staging. The standalone validator checks AssemblyRefs without reconciling these receipt fields.
- Why it blocks / does not: A correct ZIP hash can accompany the wrong DLL MVID or source attribution and pass preflight. The 37-case suite does not exercise this required receipt failure.
- Requested correction: Derive the receipt from one stable copy of the validated ZIP and its DLL. Reconcile the recorded fields, including the embedded source revision, in the preflight used by receipt-consuming paths.
- Requested evidence: Use two real same-version builds. Show acceptance of a matching archive and receipt, and refusal of a swapped receipt, wrong embedded revision, DLL hash, or MVID before staging.

**R1-F3** · P1 · required_gap · blocks readiness — Frozen package reuse does not detect build-input drift
- Location: `scripts/RuntimeAutomation.Common.ps1:16`; acceptance: mission.md:156, mission.md:194
- Trigger: Reuse a package after an ignored compiler reference changes, or after restoring source that was locally modified when the package was built.
- Expected: A material change to the pins, verifier, project, or resolved reference bytes invalidates an affected frozen candidate.
- Actual: The reusable manifest records HEAD and output identities. It records no identity for the effective compiler inputs. Build-Local can also issue a validated receipt from locally changed source under the current HEAD. WriteCompilerReferenceList has no caller that stores or checks its output when reusing a package.
- Why it blocks / does not: Git HEAD and the existing ZIP hash can both match after a material input changes. The frozen package can still be admitted as current.
- Requested correction: Bind reusable receipts to the actual source and material build inputs, including ignored local references selected by MSBuild. Verify that identity before reuse and distinguish ordinary dirty development builds from builds eligible for a frozen receipt.
- Requested evidence: In a temporary checkout, show unchanged reuse passing and the same receipt being refused after a material input changes at the same HEAD. Include a resolved-reference change and a changed pin, helper, or project input.

**R1-F4** · P2 · defect · blocks readiness — Standalone package validation can skip the UMM pin
- Location: `scripts/validate-package.ps1:62`; acceptance: mission.md:152, mission.md:193, mission.md:196
- Trigger: Validate a supplied ZIP with a pin file that omits UnityModManager or lists 0.33.0.0 alongside the 0.31.1 minimum.
- Expected: Standalone validation enforces both required loader identities and the relationship between UMM 0.31.1.0 and ManagerVersion 0.31.1.
- Actual: The standalone validator checks ManagerVersion against a literal and then loops over whichever pin entries exist. It does not enforce the canonical pair or the relationship between the UMM pin and the declared minimum. Omitting UMM skips its AssemblyRef check entirely.
- Why it blocks / does not: An inconsistent pin definition can make standalone validation accept the lowered-manifest legacy DLL that the build-side check is designed to refuse.
- Requested correction: Share the pin-definition checks with standalone validation so an incomplete or inconsistent definition fails before the packaged AssemblyRefs are evaluated. Keep supplied-ZIP inspection usable offline.
- Requested evidence: Run the standalone validator in temporary fixtures with a missing required pin and a UMM pin inconsistent with the declared minimum. Show refusal and retain a passing genuine baseline control.

**R1-F5** · P2 · defect · blocks readiness — A relative reference-cache path can verify the wrong directory
- Location: `scripts/Get-PinnedLoaderReferences.ps1:35`; acceptance: mission.md:151, mission.md:192
- Trigger: Invoke the helper with a relative -ReferenceDirectory after changing the PowerShell location.
- Expected: A relative cache argument identifies the filesystem directory relative to the caller’s PowerShell location.
- Actual: GetFullPath resolves a relative ReferenceDirectory against the process working directory. PowerShell’s current location can differ after Set-Location or Push-Location. The helper can verify or populate the wrong cache while the requested cache remains corrupt or missing.
- Why it blocks / does not: The helper can report successful verification for bytes outside the caller’s selected directory.
- Requested correction: Resolve caller-supplied cache paths using PowerShell filesystem path semantics, including paths for caches that do not yet exist.
- Requested evidence: Use distinct process and PowerShell directories with different cache contents. Verify that a relative argument selects the intended cache and refuses its corrupt bytes.

**R1-F6** · P2 · evidence_gap · blocks readiness — The package and API evidence identify a different DLL from the final gate
- Location: `docs/evidence/umm-0311/loader-baseline.json:3`; acceptance: mission.md:202, mission.md:233, mission.md:251
- Trigger: Use the submitted records to select the package qualified by this final review.
- Expected: One designated frozen artifact connects the handoff to its source SHA, ZIP hash, DLL hash, MVID, package check, and static profile results.
- Actual: The package and all four static rows identify ca04d74, ZIP 06a50dd3… and DLL ed79f02b…. The final controller gate produced DLL d7930413…. The intervening commit changed documentation only, but Build.ps1 embeds HEAD and the resulting DLL hashes differ. The supplied records give neither a package for d7930413… nor a retained ZIP and sidecar location for handing off the earlier freeze.
- Why it blocks / does not: The earlier records establish results for ed79f02b…. The handoff still needs to identify a consumable package with those results or qualify the separately stamped final DLL.
- Requested correction: Identify the frozen product explicitly. For a documentation-only successor, identify the retained matching package, receipt, and source worktree allowed by repository policy. For corrected product bytes, capture the package and API observations for that new freeze.
- Requested evidence: Provide a matching ZIP/DLL/receipt identity and validator and API-audit records. Attribute the controller gate separately whenever it stamps a different DLL.

**R1-F7** · P2 · required_gap · blocks readiness — The current build guides still direct developers to installed UMM references
- Location: `docs/BUILD-AND-RELEASE.md:8`; acceptance: mission.md:248, mission.md:250, mission.md:253
- Trigger: A developer follows the README or release prerequisites to build or verify the candidate.
- Expected: The current build and release instructions distinguish installed game references from pinned loader references and explain how to reproduce the focused checks.
- Actual: README.md line 259 still says production uses installed UMM references. The release guide likewise says GamePath.props supplies them. The new compatibility page lists outcomes, but the boundary suite copies a pre-existing cache and the four-profile audit has source samples without a complete reproduction command.
- Why it blocks / does not: The primary build instructions contradict the implemented baseline and leave required checks difficult to reproduce from a fresh configured checkout.
- Requested correction: Update the README and release guide to link to the pin and compatibility instructions. Document cache preparation, package validation, and the static audit command with its profile inputs. State the native and combined-stack status explicitly.
- Requested evidence: Provide focused text and link checks showing that the current build entry points lead to consistent, reproducible instructions.

**R1-F8** · P2 · defect · blocks readiness — The Nexus copy assigns the new minimum to release 0.5.3
- Location: `docs/NEXUS-RELEASE-COPY.md:52`; acceptance: mission.md:183, mission.md:255
- Trigger: Read the installation requirements for the exact v0.5.3 release asset.
- Expected: Version-specific release copy preserves the minimum declared by the artifact it names.
- Actual: This page explicitly directs the owner to upload and install the unchanged KingmakerBuffPlanner-0.5.3.zip. The change assigns it a 0.31.1 minimum. The v0.5.3 tag’s Info.json declares 0.28.2.
- Why it blocks / does not: The documentation retroactively attributes the new candidate’s minimum to a named historical release.
- Requested correction: Restore the 0.5.3 release copy’s historical requirement and keep the new minimum in documentation explicitly scoped to the baseline candidate.
- Requested evidence: Compare the corrected release copy with v0.5.3:src/KingmakerBuffPlanner/Info.json and the current candidate manifest.

**R1-F9** · P3 · optional_polish · non-blocking — Clarify how to verify the evidence hashes on Windows
- Location: `docs/evidence/umm-0311/INDEX.md:7`; acceptance: mission.md:253
- Trigger: An owner checks an exported SHA-256 directly against a Windows working-tree copy.
- Expected: The evidence index explains which bytes its exported hashes cover.
- Actual: All eight exported digests match the Git blobs. This Windows checkout uses CRLF, so Get-FileHash on the checked-out evidence files produces different values from the exported column.
- Why it blocks / does not: The canonical digests all verified. Clarifying the byte representation would make that check easier to reproduce.
- Requested correction: Label the exported digests as Git-blob digests and give a line-ending-independent verification method.
- Requested evidence: Show one representative digest reproduced from the documented Git blob.

#### Evidence assessment

- **Inspected:** The complete 1,671-line feature diff, repository instructions, validation policy, mission, developer report, transcript notice, and empty prior-findings ledger.; The production project and MSBuild targets, reference acquisition, local and release packaging, standalone validation, installation and deployment preflights, frozen-build consumers, runtime request and result code, and current build documentation.; The structured records for ca04d74 report 37/37 boundary cases, including rejection of the genuine 0.6.20 DLL with a lowered manifest. Its direct-build record lists both pinned Csc paths. Its static audit records 8 types and 24 members resolved across four profiles for DLL ed79f02b4f520107798219014d9dcec8f560409f48bb70ca52f350a61571692a, MVID 8903b77f-a338-4456-ad11-5bdd27a53b19.
- **Executed verification:** The controller receipt and matching log confirm Test-Development.ps1 -Only localization at ee05e376. Source validation passed 47/47, the Release build passed 1/1, and localization tests passed 14/14 in a 7.8-second gate. The DLL SHA-256 was d7930413729bad8b840ee7a623db36ed6abade185999c1770f4b3e66ca5a2718.; Read-only review checks verified HEAD, the mission digest, the gate log digest, all eight exported evidence digests against their Git blobs, and the local documentation link targets. git diff --check passed. The v0.5.3 tag independently confirmed its declared ManagerVersion was 0.28.2.
- **Source inspection only:** The manifest declares 0.31.1. The project retains .NET Framework 4.7 and C# 7.3. The wrapper verifies the pinned bytes, and the MSBuild target checks the resolved compiler paths.; Install-Local.ps1 and Deploy-Local.ps1 both call the standalone ZIP validator before staging. The receipt and runtime-profile failure paths in the findings were traced through those consumers without installing or launching the candidate.
- **Required gaps:** Offline proof that preflight and in-process results agree on the selected UMM, Harmony12, and Harmony2 identities.; Real-archive receipt checks and frozen-reuse tests that reject mismatched DLL metadata and changed material inputs.; A package and static-audit evidence chain for the designated handoff bytes, plus accurate current build instructions and historical release attribution.
- **Limitations:** The review snapshot was read-only. The foreground developer transcript was withheld because it mixed repositories. The focused gate is the only controller-recorded execution supplied for ee05e376.; Native settings, planner, persistence, reload, and cast checks were NOT RUN on UMM 0.31.1, 0.32.4, owner 0.33.0, and 0.33.4. The repository launcher targets the live Steam installation. No combined-stack run or separate IDE build was evidenced.

#### Summary

- **Delivered behavior:** Buff Planner declares UMM 0.31.1 and its production project selects hash-verified UnityModManager 0.31.1.0 and Harmony12 1.2.0.1. With the committed pin file, the standalone validator inspects the packaged DLL’s AssemblyRefs. Historical structured evidence covers the lowered-manifest regression and four static loader profiles.
- **Scope reviewed:** KingmakerBuffPlanner draft PR #26, covering the full feature diff from 913798712f3685236c8f9d5cc1e8c04ea9fc9248 to ee05e37652ae00ba342407b06a9eb46b72a2441f and the directly affected build, package, runtime, and documentation paths.
- **Pending later-phase work:** none
- **Unresolved questions:** none
- **Remaining owner review:** Availability and authorization of a disposable game copy and fixture for any future native compatibility claim.; Disposition of draft PR #26 after the corrected candidate receives independent review.

#### Provenance (controller-recorded)

| | Requested | Observed |
|---|---|---|
| Model | `gpt-6.2-astra` | `gpt-6.2-astra` (turns ['gpt-6.2-astra']) |
| Reasoning effort | `max` | `max` (turns ['max']) |
| Sandbox / approvals | read-only / never | `read-only` / `never` |

Owner guidance bound to this review: none.

Owner amendments bound to this review: none.

Codex CLI `0.170.0`, thread `01a20abb-e1cc-73a3-9d88-92c03bcbc38a`, duration 839.6s, response sha256 `37f55efad491dbfd7a851f6a2a8970b30c0962bc50df02e8430f9065384a57ec`, request `kbp-codex-kingmaker-buff-planner-umm-0311-baseline-20261123182207-144d-r01-a00d47`. Observed values come from the Codex session rollout's recorded thread settings, not from the model's own statements; the serving backend's model identity is not separately exposed.

<details><summary>Complete structured review (agent-review.review.v1)</summary>

```json
{
  "candidate_commit": "ee05e37652ae00ba342407b06a9eb46b72a2441f",
  "checkpoint_id": "final",
  "checkpoint_type": "final",
  "decision": "changes_required",
  "decision_rationale": "The production compiler pin is in place and the focused gate passed. The runtime harness still assumes one loader, and the package receipts do not establish which source, DLL, and build inputs are being reused. Both are explicit mission requirements.\n\n## Continuation for Claude\n\nCarry one measured loader profile through Buff Planner\u2019s preflight, runtime request, in-process observation, result reader, and reuse checks. Bind candidate receipts to the DLL inside the validated ZIP and to the material inputs that produced it. Apply the same pin rules in standalone package validation and correct relative cache resolution.\n\nExercise these boundaries with temporary files and offline fixtures. Freeze one corrected package, record its source, ZIP and DLL hashes and MVID, and run the four static profile comparisons on those bytes. Run the configured focused gate and distinguish any separately stamped gate build. Update the current build instructions and restore the historical 0.5.3 release copy. Keep the existing runtime guards in force and all native rows marked NOT RUN while an approved disposable game copy is unavailable.",
  "evidence_assessment": {
    "executed_verification": [
      "The controller receipt and matching log confirm Test-Development.ps1 -Only localization at ee05e376. Source validation passed 47/47, the Release build passed 1/1, and localization tests passed 14/14 in a 7.8-second gate. The DLL SHA-256 was d7930413729bad8b840ee7a623db36ed6abade185999c1770f4b3e66ca5a2718.",
      "Read-only review checks verified HEAD, the mission digest, the gate log digest, all eight exported evidence digests against their Git blobs, and the local documentation link targets. git diff --check passed. The v0.5.3 tag independently confirmed its declared ManagerVersion was 0.28.2."
    ],
    "inspected": [
      "The complete 1,671-line feature diff, repository instructions, validation policy, mission, developer report, transcript notice, and empty prior-findings ledger.",
      "The production project and MSBuild targets, reference acquisition, local and release packaging, standalone validation, installation and deployment preflights, frozen-build consumers, runtime request and result code, and current build documentation.",
      "The structured records for ca04d74 report 37/37 boundary cases, including rejection of the genuine 0.6.20 DLL with a lowered manifest. Its direct-build record lists both pinned Csc paths. Its static audit records 8 types and 24 members resolved across four profiles for DLL ed79f02b4f520107798219014d9dcec8f560409f48bb70ca52f350a61571692a, MVID 8903b77f-a338-4456-ad11-5bdd27a53b19."
    ],
    "limitations": [
      "The review snapshot was read-only. The foreground developer transcript was withheld because it mixed repositories. The focused gate is the only controller-recorded execution supplied for ee05e376.",
      "Native settings, planner, persistence, reload, and cast checks were NOT RUN on UMM 0.31.1, 0.32.4, owner 0.33.0, and 0.33.4. The repository launcher targets the live Steam installation. No combined-stack run or separate IDE build was evidenced."
    ],
    "required_gaps": [
      "Offline proof that preflight and in-process results agree on the selected UMM, Harmony12, and Harmony2 identities.",
      "Real-archive receipt checks and frozen-reuse tests that reject mismatched DLL metadata and changed material inputs.",
      "A package and static-audit evidence chain for the designated handoff bytes, plus accurate current build instructions and historical release attribution."
    ],
    "source_inspection_only": [
      "The manifest declares 0.31.1. The project retains .NET Framework 4.7 and C# 7.3. The wrapper verifies the pinned bytes, and the MSBuild target checks the resolved compiler paths.",
      "Install-Local.ps1 and Deploy-Local.ps1 both call the standalone ZIP validator before staging. The receipt and runtime-profile failure paths in the findings were traced through those consumers without installing or launching the candidate."
    ]
  },
  "findings": [
    {
      "acceptance_ids": [
        "mission.md:220",
        "mission.md:239",
        "mission.md:242"
      ],
      "actual": "The launcher selects an optional-mod profile. Assert-KbpRuntimeResult requires the owner\u2019s UMM 0.33.0.0 version and hash for every result. RuntimeTestHost records UMM and the on-disk Harmony12 bridge, omits the underlying 0Harmony.dll identity, and the compatibility digest covers only the optional mods.",
      "blocking_rationale": "A correct baseline or newer-loader result would fail the reader. A Harmony2-only mismatch on the owner row is invisible to the loader identity checks.",
      "blocks_readiness": true,
      "category": "required_gap",
      "expected": "Preflight, the request, the loaded process, the result reader, and reusable evidence agree on one selected loader stack, including Harmony2.",
      "id": "R1-F1",
      "location": {
        "file": "scripts/RuntimeAutomation.Common.ps1",
        "line": 472
      },
      "priority": "P1",
      "requested_correction": "Carry a verified loader identity alongside the optional-mod profile. Compare the observed loaded assemblies with that identity and include it in receipts and checks that admit earlier results.",
      "requested_evidence": "Exercise matching records for the selected profiles and reject a different UMM row and a Harmony2-only mismatch in offline preflight and result-reader tests.",
      "title": "Runtime validation is hard-coded to UMM 0.33.0 and omits Harmony2",
      "trigger": "Validate a legitimate 0.31.1, 0.32.4, or 0.33.4 result, or keep the owner\u2019s UMM and Harmony12 files while changing Harmony2."
    },
    {
      "acceptance_ids": [
        "mission.md:155",
        "mission.md:194",
        "mission.md:202"
      ],
      "actual": "Build-Local reads the DLL hash and MVID from the build output after copying the DLL into the package. Read-KbpBuildManifest checks HEAD and the whole ZIP hash but trusts the DLL fields. Install-Local accepts a syntactically valid commit and nonempty MVID, then checks the DLL hash only after staging. The standalone validator checks AssemblyRefs without reconciling these receipt fields.",
      "blocking_rationale": "A correct ZIP hash can accompany the wrong DLL MVID or source attribution and pass preflight. The 37-case suite does not exercise this required receipt failure.",
      "blocks_readiness": true,
      "category": "defect",
      "expected": "The receipt\u2019s source revision, version, DLL hash, and MVID match the actual packaged assembly before the candidate is accepted for staging.",
      "id": "R1-F2",
      "location": {
        "file": "scripts/Build-Local.ps1",
        "line": 58
      },
      "priority": "P1",
      "requested_correction": "Derive the receipt from one stable copy of the validated ZIP and its DLL. Reconcile the recorded fields, including the embedded source revision, in the preflight used by receipt-consuming paths.",
      "requested_evidence": "Use two real same-version builds. Show acceptance of a matching archive and receipt, and refusal of a swapped receipt, wrong embedded revision, DLL hash, or MVID before staging.",
      "title": "The build receipt can describe a different DLL from the packaged one",
      "trigger": "A receipt carries stale DLL metadata, or another build replaces the shared output DLL after the packager copies it."
    },
    {
      "acceptance_ids": [
        "mission.md:156",
        "mission.md:194"
      ],
      "actual": "The reusable manifest records HEAD and output identities. It records no identity for the effective compiler inputs. Build-Local can also issue a validated receipt from locally changed source under the current HEAD. WriteCompilerReferenceList has no caller that stores or checks its output when reusing a package.",
      "blocking_rationale": "Git HEAD and the existing ZIP hash can both match after a material input changes. The frozen package can still be admitted as current.",
      "blocks_readiness": true,
      "category": "required_gap",
      "expected": "A material change to the pins, verifier, project, or resolved reference bytes invalidates an affected frozen candidate.",
      "id": "R1-F3",
      "location": {
        "file": "scripts/RuntimeAutomation.Common.ps1",
        "line": 16
      },
      "priority": "P1",
      "requested_correction": "Bind reusable receipts to the actual source and material build inputs, including ignored local references selected by MSBuild. Verify that identity before reuse and distinguish ordinary dirty development builds from builds eligible for a frozen receipt.",
      "requested_evidence": "In a temporary checkout, show unchanged reuse passing and the same receipt being refused after a material input changes at the same HEAD. Include a resolved-reference change and a changed pin, helper, or project input.",
      "title": "Frozen package reuse does not detect build-input drift",
      "trigger": "Reuse a package after an ignored compiler reference changes, or after restoring source that was locally modified when the package was built."
    },
    {
      "acceptance_ids": [
        "mission.md:152",
        "mission.md:193",
        "mission.md:196"
      ],
      "actual": "The standalone validator checks ManagerVersion against a literal and then loops over whichever pin entries exist. It does not enforce the canonical pair or the relationship between the UMM pin and the declared minimum. Omitting UMM skips its AssemblyRef check entirely.",
      "blocking_rationale": "An inconsistent pin definition can make standalone validation accept the lowered-manifest legacy DLL that the build-side check is designed to refuse.",
      "blocks_readiness": true,
      "category": "defect",
      "expected": "Standalone validation enforces both required loader identities and the relationship between UMM 0.31.1.0 and ManagerVersion 0.31.1.",
      "id": "R1-F4",
      "location": {
        "file": "scripts/validate-package.ps1",
        "line": 62
      },
      "priority": "P2",
      "requested_correction": "Share the pin-definition checks with standalone validation so an incomplete or inconsistent definition fails before the packaged AssemblyRefs are evaluated. Keep supplied-ZIP inspection usable offline.",
      "requested_evidence": "Run the standalone validator in temporary fixtures with a missing required pin and a UMM pin inconsistent with the declared minimum. Show refusal and retain a passing genuine baseline control.",
      "title": "Standalone package validation can skip the UMM pin",
      "trigger": "Validate a supplied ZIP with a pin file that omits UnityModManager or lists 0.33.0.0 alongside the 0.31.1 minimum."
    },
    {
      "acceptance_ids": [
        "mission.md:151",
        "mission.md:192"
      ],
      "actual": "GetFullPath resolves a relative ReferenceDirectory against the process working directory. PowerShell\u2019s current location can differ after Set-Location or Push-Location. The helper can verify or populate the wrong cache while the requested cache remains corrupt or missing.",
      "blocking_rationale": "The helper can report successful verification for bytes outside the caller\u2019s selected directory.",
      "blocks_readiness": true,
      "category": "defect",
      "expected": "A relative cache argument identifies the filesystem directory relative to the caller\u2019s PowerShell location.",
      "id": "R1-F5",
      "location": {
        "file": "scripts/Get-PinnedLoaderReferences.ps1",
        "line": 35
      },
      "priority": "P2",
      "requested_correction": "Resolve caller-supplied cache paths using PowerShell filesystem path semantics, including paths for caches that do not yet exist.",
      "requested_evidence": "Use distinct process and PowerShell directories with different cache contents. Verify that a relative argument selects the intended cache and refuses its corrupt bytes.",
      "title": "A relative reference-cache path can verify the wrong directory",
      "trigger": "Invoke the helper with a relative -ReferenceDirectory after changing the PowerShell location."
    },
    {
      "acceptance_ids": [
        "mission.md:202",
        "mission.md:233",
        "mission.md:251"
      ],
      "actual": "The package and all four static rows identify ca04d74, ZIP 06a50dd3\u2026 and DLL ed79f02b\u2026. The final controller gate produced DLL d7930413\u2026. The intervening commit changed documentation only, but Build.ps1 embeds HEAD and the resulting DLL hashes differ. The supplied records give neither a package for d7930413\u2026 nor a retained ZIP and sidecar location for handing off the earlier freeze.",
      "blocking_rationale": "The earlier records establish results for ed79f02b\u2026. The handoff still needs to identify a consumable package with those results or qualify the separately stamped final DLL.",
      "blocks_readiness": true,
      "category": "evidence_gap",
      "expected": "One designated frozen artifact connects the handoff to its source SHA, ZIP hash, DLL hash, MVID, package check, and static profile results.",
      "id": "R1-F6",
      "location": {
        "file": "docs/evidence/umm-0311/loader-baseline.json",
        "line": 3
      },
      "priority": "P2",
      "requested_correction": "Identify the frozen product explicitly. For a documentation-only successor, identify the retained matching package, receipt, and source worktree allowed by repository policy. For corrected product bytes, capture the package and API observations for that new freeze.",
      "requested_evidence": "Provide a matching ZIP/DLL/receipt identity and validator and API-audit records. Attribute the controller gate separately whenever it stamps a different DLL.",
      "title": "The package and API evidence identify a different DLL from the final gate",
      "trigger": "Use the submitted records to select the package qualified by this final review."
    },
    {
      "acceptance_ids": [
        "mission.md:248",
        "mission.md:250",
        "mission.md:253"
      ],
      "actual": "README.md line 259 still says production uses installed UMM references. The release guide likewise says GamePath.props supplies them. The new compatibility page lists outcomes, but the boundary suite copies a pre-existing cache and the four-profile audit has source samples without a complete reproduction command.",
      "blocking_rationale": "The primary build instructions contradict the implemented baseline and leave required checks difficult to reproduce from a fresh configured checkout.",
      "blocks_readiness": true,
      "category": "required_gap",
      "expected": "The current build and release instructions distinguish installed game references from pinned loader references and explain how to reproduce the focused checks.",
      "id": "R1-F7",
      "location": {
        "file": "docs/BUILD-AND-RELEASE.md",
        "line": 8
      },
      "priority": "P2",
      "requested_correction": "Update the README and release guide to link to the pin and compatibility instructions. Document cache preparation, package validation, and the static audit command with its profile inputs. State the native and combined-stack status explicitly.",
      "requested_evidence": "Provide focused text and link checks showing that the current build entry points lead to consistent, reproducible instructions.",
      "title": "The current build guides still direct developers to installed UMM references",
      "trigger": "A developer follows the README or release prerequisites to build or verify the candidate."
    },
    {
      "acceptance_ids": [
        "mission.md:183",
        "mission.md:255"
      ],
      "actual": "This page explicitly directs the owner to upload and install the unchanged KingmakerBuffPlanner-0.5.3.zip. The change assigns it a 0.31.1 minimum. The v0.5.3 tag\u2019s Info.json declares 0.28.2.",
      "blocking_rationale": "The documentation retroactively attributes the new candidate\u2019s minimum to a named historical release.",
      "blocks_readiness": true,
      "category": "defect",
      "expected": "Version-specific release copy preserves the minimum declared by the artifact it names.",
      "id": "R1-F8",
      "location": {
        "file": "docs/NEXUS-RELEASE-COPY.md",
        "line": 52
      },
      "priority": "P2",
      "requested_correction": "Restore the 0.5.3 release copy\u2019s historical requirement and keep the new minimum in documentation explicitly scoped to the baseline candidate.",
      "requested_evidence": "Compare the corrected release copy with v0.5.3:src/KingmakerBuffPlanner/Info.json and the current candidate manifest.",
      "title": "The Nexus copy assigns the new minimum to release 0.5.3",
      "trigger": "Read the installation requirements for the exact v0.5.3 release asset."
    },
    {
      "acceptance_ids": [
        "mission.md:253"
      ],
      "actual": "All eight exported digests match the Git blobs. This Windows checkout uses CRLF, so Get-FileHash on the checked-out evidence files produces different values from the exported column.",
      "blocking_rationale": "The canonical digests all verified. Clarifying the byte representation would make that check easier to reproduce.",
      "blocks_readiness": false,
      "category": "optional_polish",
      "expected": "The evidence index explains which bytes its exported hashes cover.",
      "id": "R1-F9",
      "location": {
        "file": "docs/evidence/umm-0311/INDEX.md",
        "line": 7
      },
      "priority": "P3",
      "requested_correction": "Label the exported digests as Git-blob digests and give a line-ending-independent verification method.",
      "requested_evidence": "Show one representative digest reproduced from the documented Git blob.",
      "title": "Clarify how to verify the evidence hashes on Windows",
      "trigger": "An owner checks an exported SHA-256 directly against a Windows working-tree copy."
    }
  ],
  "prior_finding_assessments": [],
  "request_id": "kbp-codex-kingmaker-buff-planner-umm-0311-baseline-20261123182207-144d-r01-a00d47",
  "round": 1,
  "schema_version": "agent-review.review.v1",
  "summary": {
    "delivered_behavior": "Buff Planner declares UMM 0.31.1 and its production project selects hash-verified UnityModManager 0.31.1.0 and Harmony12 1.2.0.1. With the committed pin file, the standalone validator inspects the packaged DLL\u2019s AssemblyRefs. Historical structured evidence covers the lowered-manifest regression and four static loader profiles.",
    "pending_later_phase_work": [],
    "remaining_owner_review": [
      "Availability and authorization of a disposable game copy and fixture for any future native compatibility claim.",
      "Disposition of draft PR #26 after the corrected candidate receives independent review."
    ],
    "scope_reviewed": "KingmakerBuffPlanner draft PR #26, covering the full feature diff from 913798712f3685236c8f9d5cc1e8c04ea9fc9248 to ee05e37652ae00ba342407b06a9eb46b72a2441f and the directly affected build, package, runtime, and documentation paths.",
    "unresolved_questions": []
  }
}
```
</details>

_Model-generated review by Codex using the requested model, posted by the agent-review controller through the owner's GitHub account. Not a human review or approval._
