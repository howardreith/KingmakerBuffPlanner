<!-- agent-review:v1 enrollment=kbp-codex-kingmaker-buff-planner-umm-0311-baseline-20261123182207-144d key=r02-review part=1/1 sha256=3ef34f6485c4fea8fc5684840bd7752504ff88c556fc878050abbb85d5c56eb9 -->
### Codex GPT-6.2 Astra — review — round 2 — candidate `62b65e2fbc`

**Decision:** `changes_required` — The compile pin and four static API rows are supported. Final readiness still depends on selecting and observing the intended runtime loader, binding the installed or staged bytes to their receipt, detecting material build-input changes, and identifying a retained frozen package.

## Continuation for Claude

Keep the corrections within Buff Planner. Make the measured loader rows selectable with one frozen candidate. Carry the selection through the existing profile checks, request, in-process observation and result reader. Use the validator’s package and DLL identities in the release installer, and bind transactional staging to the exact ZIP accepted by preflight. Correct the fingerprint so it follows the producing compile, including generated inputs.

Exercise the reported failure paths with temporary loader directories, archives and checkouts. Preserve the existing locks, save protections and restoration checks. Retain the corrected frozen ZIP, raw sidecar and matching source worktree, verify them at their handoff location, and attribute the static audit and controller gate to their respective DLLs. The mission permits native and combined-stack checks to remain NOT RUN while an approved disposable path is unavailable.

#### Findings

**R1-F1** · P1 · required_gap · blocks readiness — The runtime launcher still admits only the owner’s loader row
- Location: `scripts/Invoke-KingmakerRuntimeTest.ps1:328`; acceptance: mission.md:220, mission.md:239, mission.md:242
- Trigger: Select a matching non-owner loader for the frozen product, or load a same-version bridge from a different location.
- Expected: Each measured loader row can accompany the supported optional-mod profile, and the request and result agree on the UMM, Harmony12 and Harmony2 assemblies used by the process.
- Actual: All five profile IDs accepted by the launcher specify owner UMM 0.33.0.0. Editing one makes the worktree dirty, which the launcher refuses. The request carries the optional-mod profile ID without an expected loader identity. RuntimeTestHost hashes UMM and Harmony12 at constructed installation paths. Only the Harmony2 observation looks up a loaded assembly.
- Why it blocks / does not: A matching non-owner installation cannot pass normal launcher preflight with the same frozen candidate. On the owner row, a different same-version UMM or Harmony12 assembly can be bound while the result reports the expected on-disk hash.
- Requested correction: Make the measured loader identity selectable independently of optional-mod membership. Carry it into the request and bind result observations to the loaded assemblies. Preserve the loader-aware allowance and completion digest.
- Requested evidence: Using private copies and the normal selection/request code, admit matching 0.31.1, 0.32.4, owner 0.33.0 and 0.33.4 rows. Reject cross-row, Harmony2-only and loaded-versus-installed identity mismatches.

**R1-F2** · P1 · defect · blocks readiness — Install preflight still trusts the release receipt’s commit and MVID
- Location: `scripts/Install-Local.ps1:59`; acceptance: mission.md:155, mission.md:194, mission.md:196
- Trigger: Supply a valid release ZIP and its correct ZIP digest with a different well-formed commit or MVID in release-manifest.json.
- Expected: The release receipt’s package hash, DLL hash, MVID and source commit agree with the validated release archive before staging.
- Actual: Install-Local checks the commit’s syntax, a nonempty MVID and the ZIP hash. Its validator call discards the measured DLL identity. The installer compares the DLL hash after extraction and never reconciles the receipt’s commit or MVID with the DLL. Build-Release also reads HEAD again when writing the release receipt.
- Why it blocks / does not: A receipt with the correct ZIP hash and an incorrect source revision or MVID can pass install preflight and be recorded as the installed artifact’s identity.
- Requested correction: Derive the release receipt from the validated release ZIP and reconcile its fields in the installer’s preflight. Reuse the package identity comparison already implemented for the build-local path.
- Requested evidence: Exercise the release installer’s preflight with temporary files. A matching pair should pass. A wrong commit, MVID or DLL digest should fail before staging or transaction state is created.

**R1-F3** · P1 · required_gap · blocks readiness — The fingerprint can omit an input that changes the compiled DLL
- Location: `scripts/Common.ps1:26`; acceptance: mission.md:156, mission.md:194
- Trigger: Change a material local MSBuild setting that affects generated source after a clean package has been frozen.
- Expected: Frozen reuse detects changes to the effective inputs of the producing compile, including the values used to generate compiled source.
- Actual: The fingerprint runs a separate project reference-resolution target in a fresh intermediate directory. Build.ps1 rebuilds the solution with KbpSourceRevision set. The frozen receipt lists BuildInfo.Generated.cs as absent, and the captured settings omit KbpVersion. An ignored GamePath.props override of KbpVersion changes the compiled BuildInfo.Version while the fingerprint inputs stay the same.
- Why it blocks / does not: A clean receipt can remain eligible at the same HEAD after an effective build input changes the DLL. The existing reference-relocation and wrapper-mutation cases do not exercise this path.
- Requested correction: Use the producing solution and revision context for input identity. Bind generated inputs to their effective values, or enforce the authoritative values where local overrides are unsupported.
- Requested evidence: At one HEAD, change an effective generated or solution-conditioned input through the ignored local props. Show that the old receipt is refused, or that the unsupported override is refused. Retain the unchanged-reuse control.

**R1-F6** · P2 · evidence_gap · blocks readiness — The audited package still lacks a verified handoff location
- Location: `docs/evidence/umm-0311/frozen-build-receipt.json:1530`; acceptance: mission.md:202, mission.md:233, mission.md:251
- Trigger: Use the review records to retrieve the package qualified by the four static API rows.
- Expected: The handoff identifies a retained, usable package, raw receipt and matching source worktree, and attributes each build observation to the commit actually measured.
- Actual: The validator, receipt and API audit agree on ZIP 3eb97273…, DLL c7f7c10e… and MVID ed4ae28d-ff7c-48d9-b3b9-b09e063181d4. The only recorded package path is in the producing temporary clone. Retention of the raw pair at a usable frozen checkout is unconfirmed. Both stamp-test builds in frozen-product.json name 77ac9e9, although the developer report attributes the experiment to 62b65e2.
- Why it blocks / does not: The measurements identify the audited bytes, but the handoff does not establish where the owner can consume that package with its path- and HEAD-bound receipt.
- Requested correction: Record the retained location and verify the raw pair there against its frozen worktree. Attribute the 77ac9e9 stamp comparison and the separate H controller gate accurately.
- Requested evidence: Provide a read-back of the retained ZIP and sidecar with their hashes, embedded revision, DLL hash and MVID. Keep the API results attached to that DLL. A documentation-only successor can use the Git diff to establish source continuity.

**R2-F1** · P2 · defect · blocks readiness — Standalone profile and deployment preflights skip the loader check
- Location: `scripts/compatibility/CompatibilityProfile.Common.ps1:107`; acceptance: mission.md:237, mission.md:239, mission.md:242
- Trigger: Check or stage native-only directly against a directory whose Harmony2 file differs from the profile.
- Expected: Public preflights that admit the selected profile verify its loader against the requested game directory.
- Actual: Test-KingmakerCompatibilityProfile and Deploy-Local call Assert-KbpCompatibilityProfileFixtures, which checks only optional mods. For native-only the loop is empty. The installed-loader check is a separate call made by Invoke-KingmakerRuntimeTest.
- Why it blocks / does not: The standalone profile check can print PASS, and direct profile deployment can proceed, for an installation that the main launcher would refuse.
- Requested correction: Apply the loader check in the shared installation preflight used by the profile test and deployment paths. Pass through the caller’s KingmakerInstallDir.
- Requested evidence: Use a private game-directory fixture with matching optional mods. Show that both entry points refuse a Harmony2-only mismatch before staging and accept the matching loader.

**R2-F2** · P1 · defect · blocks readiness — Deployment reopens a mutable ZIP after receipt validation
- Location: `scripts/Invoke-KingmakerRuntimeTest.ps1:469`; acceptance: mission.md:155, mission.md:194, mission.md:240
- Trigger: Another local build replaces the versioned ZIP between launcher preflight and transactional staging.
- Expected: The guarded transaction extracts the exact archive bytes admitted by the frozen receipt.
- Actual: After Read-KbpBuildManifest verifies the receipt, the launcher passes only the original package path to Deploy-Local. The validator deletes its private snapshot, and Enter-KbpRuntimeTransaction reopens the path for extraction. Its package digest is never compared with the launcher’s admitted digest. The game result copies expectedPackageSha256 from the request.
- Why it blocks / does not: A second valid same-version ZIP can be staged under the first ZIP’s receipt. If only a packaged data file differs, the loaded DLL hash still matches and the result can attribute the run to the wrong package.
- Requested correction: Carry the expected ZIP digest into the transaction and validate and extract the same transaction-owned copy, or hold an equivalent stable file handle through extraction.
- Requested evidence: In the temporary staging fixture, replace the source archive after receipt admission with a second valid archive containing the same DLL and different data. Show refusal before activation or extraction of the originally admitted copy.

**R2-F3** · P3 · optional_polish · non-blocking — Clarify the boundary suite’s download requirements
- Location: `docs/BUILD-AND-RELEASE.md:37`; acceptance: mission.md:253
- Trigger: Follow the documented offline command with the reference cache prepared and network access unavailable.
- Expected: The guide distinguishes the default fixture downloads from running with locally supplied fixtures.
- Actual: The guide labels the default Test-LoaderBaseline.ps1 command offline. Without arguments it downloads the legacy release ZIP and the 0.32.4 NuGet package.
- Why it blocks / does not: The documented command works with network access, and the suite already accepts local fixture paths.
- Requested correction: Mention the default downloads and show -LegacyReleaseZip and -WrongLoaderPackage for an offline run.
- Requested evidence: Check the example against the suite’s parameter names and fixture hashes.

#### Prior finding assessments

- **R1-F1** — `fix_rejected` — The result reader now compares three expected components, and the compatibility digest includes them. All five selectable profiles still resolve to owner 0.33.0, the request omits the loader, and the UMM and Harmony12 hashes still come from installation paths.
- **R1-F2** — `fix_rejected` — The build-local producer and reader reconcile the validated snapshot, and the recorded mismatch cases cover that reader. The release installer named in the original finding still trusts a syntactically valid commit and nonempty MVID.
- **R1-F3** — `fix_rejected` — The new fingerprint detects the tested helper, project, wrapper and resolved-reference changes. Its separate MSBuild evaluation omits the effective generated BuildInfo version, leaving a concrete same-HEAD reuse gap.
- **R1-F4** — `fix_verified` — The standalone validator invokes the shared definition check before inspecting the archive. The canonical pair and minimum-to-UMM relationship are enforced. The records show refusal for a missing Harmony12 pin and inconsistent UMM version, plus a passing cache-free control.
- **R1-F5** — `fix_verified` — GetUnresolvedProviderPathFromPSPath resolves the cache from the caller’s PowerShell location. The recorded cases distinguish the process directory from the PowerShell location in both directions and cover a missing directory.
- **R1-F6** — `fix_rejected` — The frozen artifact identities now agree across the records. The remaining evidence gap is retention of the consumable raw pair in a matching checkout. The recorded stamp comparison also names 77ac9e9, while the H gate produced a035f2c8….
- **R1-F7** — `fix_verified` — The README and release guide now explain cache preparation, pinned references, package validation and the four-profile audit command. Both native and combined-stack status are explicit. The linked paths and build-guide anchor resolve.
- **R1-F8** — `fix_verified` — The Nexus copy again states 0.28.2 for release 0.5.3, matching the tagged manifest. The installation guide scopes 0.31.1 to the unreleased candidate.
- **R1-F9** — `fix_verified` — The index labels the exported hashes as Git-blob digests. I reproduced all 13, including 590efc63a1ae55c8cf07e9be7a497165c35558f3d2dd8fafa77dcf50bee613b6 for frozen-product.json.

#### Evidence assessment

- **Inspected:** The enrolled mission, AGENTS.md, validation policy, developer report, controller evidence and all nine prior findings.; The 41-file feature diff, the correction commits, and the directly affected build, package, installer, deployment, profile and in-process result paths.; The frozen build receipt, package validation, direct compiler record, API audit, loader inventory, boundary cases and documentation outputs.
- **Executed verification:** The controller receipt and matching log confirm Test-Development.ps1 -Only localization at 62b65e2. Source validation passed 47/47, the Release build passed 1/1, and localization passed 14/14. The gate took 8.1 seconds and produced DLL SHA-256 a035f2c8a8b2adb865eefc65dab388c911cfa66e6261ed54c64541a8fcacbda1.; The exported boundary report records 62/62 passing cases at 75aeeff, including rejection of the genuine 0.6.20 DLL with a lowered manifest. The offline loader-identity report records 21/21 passing cases using copied files and constructed results.; I cross-checked the frozen ZIP hash, DLL hash and MVID across the receipt, validator and applicable audit records. The compiler reference hashes match the pins. All four static rows report zero unresolved references across 8 types and 24 members.; I reproduced all 13 indexed Git-blob SHA-256 digests, matched 247 source and resource files to the frozen receipt, checked the paths in the 52-entry link report, and verified the v0.5.3 manifest. The supplied 4,840-line diff matches Git’s M..H diff byte for byte.
- **Source inspection only:** The remaining installer, staging, runtime-selection and fingerprint failure paths were traced through their consumers. The recorded tests stop short of these cases.; Git confirms that 75aeeff..H changes only documentation and evidence. The recorded revision-stamp experiment used 77ac9e9.
- **Required gaps:** Positive admission of each selectable loader row through the actual request path, loaded-assembly observation, and consistent refusal by the standalone profile and deployment preflights.; Release-receipt reconciliation before staging and evidence that the transaction consumes the same ZIP bytes preflight accepted.; A same-HEAD regression covering a material generated or solution-conditioned compiler input.; Read-back of the retained package and raw receipt in the matching frozen worktree, with accurate attribution of the revision-stamp experiment.
- **Limitations:** The controller withheld the mixed foreground transcript. The focused gate has a controller-recorded log at H. The supplemental suites and API observations are supported by committed structured outputs.; The read-only review did not rerun builds, staging, installation or game scenarios. The raw frozen ZIP and sidecar were outside the snapshot.; Native behavior is NOT RUN on 0.31.1, 0.32.4, owner 0.33.0 and 0.33.4. Combined-stack behavior is also NOT RUN.

#### Summary

- **Delivered behavior:** The candidate declares UMM 0.31.1 and the frozen build records show the pinned UnityModManager 0.31.1.0 and Harmony12 1.2.0.1 compiler inputs. Standalone validation inspects packaged AssemblyRefs. The same frozen DLL passed static API resolution on all four loader rows.
- **Scope reviewed:** Final round-2 review of KingmakerBuffPlanner PR #26, including the nine prior findings and the directly affected build, package, installer, deployment, runtime-profile and evidence consumers.
- **Pending later-phase work:** none
- **Unresolved questions:** Where are the raw audited ZIP, matching sidecar and frozen checkout retained for owner use?
- **Remaining owner review:** The owner can decide whether to authorize a future disposable native fixture and, after the blocking findings are resolved, whether to merge the draft PR.

#### Provenance (controller-recorded)

| | Requested | Observed |
|---|---|---|
| Model | `gpt-6.2-astra` | `gpt-6.2-astra` (turns ['gpt-6.2-astra']) |
| Reasoning effort | `max` | `max` (turns ['max']) |
| Sandbox / approvals | read-only / never | `read-only` / `never` |

Owner guidance bound to this review: none.

Owner amendments bound to this review: none.

Codex CLI `0.170.0`, thread `01a20b05-9c24-7040-b0a1-57c1c2dbadf3`, duration 832.5s, response sha256 `c89987e99a513ed75f1cb4212df1fa48db29f9df6cbe99808cbf22183dec7414`, request `kbp-codex-kingmaker-buff-planner-umm-0311-baseline-20261123182207-144d-r02-6c308b`. Observed values come from the Codex session rollout's recorded thread settings, not from the model's own statements; the serving backend's model identity is not separately exposed.

<details><summary>Complete structured review (agent-review.review.v1)</summary>

```json
{
  "candidate_commit": "62b65e2fbcf59d871d0cda974ab260152704b43e",
  "checkpoint_id": "final",
  "checkpoint_type": "final",
  "decision": "changes_required",
  "decision_rationale": "The compile pin and four static API rows are supported. Final readiness still depends on selecting and observing the intended runtime loader, binding the installed or staged bytes to their receipt, detecting material build-input changes, and identifying a retained frozen package.\n\n## Continuation for Claude\n\nKeep the corrections within Buff Planner. Make the measured loader rows selectable with one frozen candidate. Carry the selection through the existing profile checks, request, in-process observation and result reader. Use the validator\u2019s package and DLL identities in the release installer, and bind transactional staging to the exact ZIP accepted by preflight. Correct the fingerprint so it follows the producing compile, including generated inputs.\n\nExercise the reported failure paths with temporary loader directories, archives and checkouts. Preserve the existing locks, save protections and restoration checks. Retain the corrected frozen ZIP, raw sidecar and matching source worktree, verify them at their handoff location, and attribute the static audit and controller gate to their respective DLLs. The mission permits native and combined-stack checks to remain NOT RUN while an approved disposable path is unavailable.",
  "evidence_assessment": {
    "executed_verification": [
      "The controller receipt and matching log confirm Test-Development.ps1 -Only localization at 62b65e2. Source validation passed 47/47, the Release build passed 1/1, and localization passed 14/14. The gate took 8.1 seconds and produced DLL SHA-256 a035f2c8a8b2adb865eefc65dab388c911cfa66e6261ed54c64541a8fcacbda1.",
      "The exported boundary report records 62/62 passing cases at 75aeeff, including rejection of the genuine 0.6.20 DLL with a lowered manifest. The offline loader-identity report records 21/21 passing cases using copied files and constructed results.",
      "I cross-checked the frozen ZIP hash, DLL hash and MVID across the receipt, validator and applicable audit records. The compiler reference hashes match the pins. All four static rows report zero unresolved references across 8 types and 24 members.",
      "I reproduced all 13 indexed Git-blob SHA-256 digests, matched 247 source and resource files to the frozen receipt, checked the paths in the 52-entry link report, and verified the v0.5.3 manifest. The supplied 4,840-line diff matches Git\u2019s M..H diff byte for byte."
    ],
    "inspected": [
      "The enrolled mission, AGENTS.md, validation policy, developer report, controller evidence and all nine prior findings.",
      "The 41-file feature diff, the correction commits, and the directly affected build, package, installer, deployment, profile and in-process result paths.",
      "The frozen build receipt, package validation, direct compiler record, API audit, loader inventory, boundary cases and documentation outputs."
    ],
    "limitations": [
      "The controller withheld the mixed foreground transcript. The focused gate has a controller-recorded log at H. The supplemental suites and API observations are supported by committed structured outputs.",
      "The read-only review did not rerun builds, staging, installation or game scenarios. The raw frozen ZIP and sidecar were outside the snapshot.",
      "Native behavior is NOT RUN on 0.31.1, 0.32.4, owner 0.33.0 and 0.33.4. Combined-stack behavior is also NOT RUN."
    ],
    "required_gaps": [
      "Positive admission of each selectable loader row through the actual request path, loaded-assembly observation, and consistent refusal by the standalone profile and deployment preflights.",
      "Release-receipt reconciliation before staging and evidence that the transaction consumes the same ZIP bytes preflight accepted.",
      "A same-HEAD regression covering a material generated or solution-conditioned compiler input.",
      "Read-back of the retained package and raw receipt in the matching frozen worktree, with accurate attribution of the revision-stamp experiment."
    ],
    "source_inspection_only": [
      "The remaining installer, staging, runtime-selection and fingerprint failure paths were traced through their consumers. The recorded tests stop short of these cases.",
      "Git confirms that 75aeeff..H changes only documentation and evidence. The recorded revision-stamp experiment used 77ac9e9."
    ]
  },
  "findings": [
    {
      "acceptance_ids": [
        "mission.md:220",
        "mission.md:239",
        "mission.md:242"
      ],
      "actual": "All five profile IDs accepted by the launcher specify owner UMM 0.33.0.0. Editing one makes the worktree dirty, which the launcher refuses. The request carries the optional-mod profile ID without an expected loader identity. RuntimeTestHost hashes UMM and Harmony12 at constructed installation paths. Only the Harmony2 observation looks up a loaded assembly.",
      "blocking_rationale": "A matching non-owner installation cannot pass normal launcher preflight with the same frozen candidate. On the owner row, a different same-version UMM or Harmony12 assembly can be bound while the result reports the expected on-disk hash.",
      "blocks_readiness": true,
      "category": "required_gap",
      "expected": "Each measured loader row can accompany the supported optional-mod profile, and the request and result agree on the UMM, Harmony12 and Harmony2 assemblies used by the process.",
      "id": "R1-F1",
      "location": {
        "file": "scripts/Invoke-KingmakerRuntimeTest.ps1",
        "line": 328
      },
      "priority": "P1",
      "requested_correction": "Make the measured loader identity selectable independently of optional-mod membership. Carry it into the request and bind result observations to the loaded assemblies. Preserve the loader-aware allowance and completion digest.",
      "requested_evidence": "Using private copies and the normal selection/request code, admit matching 0.31.1, 0.32.4, owner 0.33.0 and 0.33.4 rows. Reject cross-row, Harmony2-only and loaded-versus-installed identity mismatches.",
      "title": "The runtime launcher still admits only the owner\u2019s loader row",
      "trigger": "Select a matching non-owner loader for the frozen product, or load a same-version bridge from a different location."
    },
    {
      "acceptance_ids": [
        "mission.md:155",
        "mission.md:194",
        "mission.md:196"
      ],
      "actual": "Install-Local checks the commit\u2019s syntax, a nonempty MVID and the ZIP hash. Its validator call discards the measured DLL identity. The installer compares the DLL hash after extraction and never reconciles the receipt\u2019s commit or MVID with the DLL. Build-Release also reads HEAD again when writing the release receipt.",
      "blocking_rationale": "A receipt with the correct ZIP hash and an incorrect source revision or MVID can pass install preflight and be recorded as the installed artifact\u2019s identity.",
      "blocks_readiness": true,
      "category": "defect",
      "expected": "The release receipt\u2019s package hash, DLL hash, MVID and source commit agree with the validated release archive before staging.",
      "id": "R1-F2",
      "location": {
        "file": "scripts/Install-Local.ps1",
        "line": 59
      },
      "priority": "P1",
      "requested_correction": "Derive the release receipt from the validated release ZIP and reconcile its fields in the installer\u2019s preflight. Reuse the package identity comparison already implemented for the build-local path.",
      "requested_evidence": "Exercise the release installer\u2019s preflight with temporary files. A matching pair should pass. A wrong commit, MVID or DLL digest should fail before staging or transaction state is created.",
      "title": "Install preflight still trusts the release receipt\u2019s commit and MVID",
      "trigger": "Supply a valid release ZIP and its correct ZIP digest with a different well-formed commit or MVID in release-manifest.json."
    },
    {
      "acceptance_ids": [
        "mission.md:156",
        "mission.md:194"
      ],
      "actual": "The fingerprint runs a separate project reference-resolution target in a fresh intermediate directory. Build.ps1 rebuilds the solution with KbpSourceRevision set. The frozen receipt lists BuildInfo.Generated.cs as absent, and the captured settings omit KbpVersion. An ignored GamePath.props override of KbpVersion changes the compiled BuildInfo.Version while the fingerprint inputs stay the same.",
      "blocking_rationale": "A clean receipt can remain eligible at the same HEAD after an effective build input changes the DLL. The existing reference-relocation and wrapper-mutation cases do not exercise this path.",
      "blocks_readiness": true,
      "category": "required_gap",
      "expected": "Frozen reuse detects changes to the effective inputs of the producing compile, including the values used to generate compiled source.",
      "id": "R1-F3",
      "location": {
        "file": "scripts/Common.ps1",
        "line": 26
      },
      "priority": "P1",
      "requested_correction": "Use the producing solution and revision context for input identity. Bind generated inputs to their effective values, or enforce the authoritative values where local overrides are unsupported.",
      "requested_evidence": "At one HEAD, change an effective generated or solution-conditioned input through the ignored local props. Show that the old receipt is refused, or that the unsupported override is refused. Retain the unchanged-reuse control.",
      "title": "The fingerprint can omit an input that changes the compiled DLL",
      "trigger": "Change a material local MSBuild setting that affects generated source after a clean package has been frozen."
    },
    {
      "acceptance_ids": [
        "mission.md:202",
        "mission.md:233",
        "mission.md:251"
      ],
      "actual": "The validator, receipt and API audit agree on ZIP 3eb97273\u2026, DLL c7f7c10e\u2026 and MVID ed4ae28d-ff7c-48d9-b3b9-b09e063181d4. The only recorded package path is in the producing temporary clone. Retention of the raw pair at a usable frozen checkout is unconfirmed. Both stamp-test builds in frozen-product.json name 77ac9e9, although the developer report attributes the experiment to 62b65e2.",
      "blocking_rationale": "The measurements identify the audited bytes, but the handoff does not establish where the owner can consume that package with its path- and HEAD-bound receipt.",
      "blocks_readiness": true,
      "category": "evidence_gap",
      "expected": "The handoff identifies a retained, usable package, raw receipt and matching source worktree, and attributes each build observation to the commit actually measured.",
      "id": "R1-F6",
      "location": {
        "file": "docs/evidence/umm-0311/frozen-build-receipt.json",
        "line": 1530
      },
      "priority": "P2",
      "requested_correction": "Record the retained location and verify the raw pair there against its frozen worktree. Attribute the 77ac9e9 stamp comparison and the separate H controller gate accurately.",
      "requested_evidence": "Provide a read-back of the retained ZIP and sidecar with their hashes, embedded revision, DLL hash and MVID. Keep the API results attached to that DLL. A documentation-only successor can use the Git diff to establish source continuity.",
      "title": "The audited package still lacks a verified handoff location",
      "trigger": "Use the review records to retrieve the package qualified by the four static API rows."
    },
    {
      "acceptance_ids": [
        "mission.md:237",
        "mission.md:239",
        "mission.md:242"
      ],
      "actual": "Test-KingmakerCompatibilityProfile and Deploy-Local call Assert-KbpCompatibilityProfileFixtures, which checks only optional mods. For native-only the loop is empty. The installed-loader check is a separate call made by Invoke-KingmakerRuntimeTest.",
      "blocking_rationale": "The standalone profile check can print PASS, and direct profile deployment can proceed, for an installation that the main launcher would refuse.",
      "blocks_readiness": true,
      "category": "defect",
      "expected": "Public preflights that admit the selected profile verify its loader against the requested game directory.",
      "id": "R2-F1",
      "location": {
        "file": "scripts/compatibility/CompatibilityProfile.Common.ps1",
        "line": 107
      },
      "priority": "P2",
      "requested_correction": "Apply the loader check in the shared installation preflight used by the profile test and deployment paths. Pass through the caller\u2019s KingmakerInstallDir.",
      "requested_evidence": "Use a private game-directory fixture with matching optional mods. Show that both entry points refuse a Harmony2-only mismatch before staging and accept the matching loader.",
      "title": "Standalone profile and deployment preflights skip the loader check",
      "trigger": "Check or stage native-only directly against a directory whose Harmony2 file differs from the profile."
    },
    {
      "acceptance_ids": [
        "mission.md:155",
        "mission.md:194",
        "mission.md:240"
      ],
      "actual": "After Read-KbpBuildManifest verifies the receipt, the launcher passes only the original package path to Deploy-Local. The validator deletes its private snapshot, and Enter-KbpRuntimeTransaction reopens the path for extraction. Its package digest is never compared with the launcher\u2019s admitted digest. The game result copies expectedPackageSha256 from the request.",
      "blocking_rationale": "A second valid same-version ZIP can be staged under the first ZIP\u2019s receipt. If only a packaged data file differs, the loaded DLL hash still matches and the result can attribute the run to the wrong package.",
      "blocks_readiness": true,
      "category": "defect",
      "expected": "The guarded transaction extracts the exact archive bytes admitted by the frozen receipt.",
      "id": "R2-F2",
      "location": {
        "file": "scripts/Invoke-KingmakerRuntimeTest.ps1",
        "line": 469
      },
      "priority": "P1",
      "requested_correction": "Carry the expected ZIP digest into the transaction and validate and extract the same transaction-owned copy, or hold an equivalent stable file handle through extraction.",
      "requested_evidence": "In the temporary staging fixture, replace the source archive after receipt admission with a second valid archive containing the same DLL and different data. Show refusal before activation or extraction of the originally admitted copy.",
      "title": "Deployment reopens a mutable ZIP after receipt validation",
      "trigger": "Another local build replaces the versioned ZIP between launcher preflight and transactional staging."
    },
    {
      "acceptance_ids": [
        "mission.md:253"
      ],
      "actual": "The guide labels the default Test-LoaderBaseline.ps1 command offline. Without arguments it downloads the legacy release ZIP and the 0.32.4 NuGet package.",
      "blocking_rationale": "The documented command works with network access, and the suite already accepts local fixture paths.",
      "blocks_readiness": false,
      "category": "optional_polish",
      "expected": "The guide distinguishes the default fixture downloads from running with locally supplied fixtures.",
      "id": "R2-F3",
      "location": {
        "file": "docs/BUILD-AND-RELEASE.md",
        "line": 37
      },
      "priority": "P3",
      "requested_correction": "Mention the default downloads and show -LegacyReleaseZip and -WrongLoaderPackage for an offline run.",
      "requested_evidence": "Check the example against the suite\u2019s parameter names and fixture hashes.",
      "title": "Clarify the boundary suite\u2019s download requirements",
      "trigger": "Follow the documented offline command with the reference cache prepared and network access unavailable."
    }
  ],
  "prior_finding_assessments": [
    {
      "assessment": "fix_rejected",
      "finding_id": "R1-F1",
      "rationale": "The result reader now compares three expected components, and the compatibility digest includes them. All five selectable profiles still resolve to owner 0.33.0, the request omits the loader, and the UMM and Harmony12 hashes still come from installation paths."
    },
    {
      "assessment": "fix_rejected",
      "finding_id": "R1-F2",
      "rationale": "The build-local producer and reader reconcile the validated snapshot, and the recorded mismatch cases cover that reader. The release installer named in the original finding still trusts a syntactically valid commit and nonempty MVID."
    },
    {
      "assessment": "fix_rejected",
      "finding_id": "R1-F3",
      "rationale": "The new fingerprint detects the tested helper, project, wrapper and resolved-reference changes. Its separate MSBuild evaluation omits the effective generated BuildInfo version, leaving a concrete same-HEAD reuse gap."
    },
    {
      "assessment": "fix_verified",
      "finding_id": "R1-F4",
      "rationale": "The standalone validator invokes the shared definition check before inspecting the archive. The canonical pair and minimum-to-UMM relationship are enforced. The records show refusal for a missing Harmony12 pin and inconsistent UMM version, plus a passing cache-free control."
    },
    {
      "assessment": "fix_verified",
      "finding_id": "R1-F5",
      "rationale": "GetUnresolvedProviderPathFromPSPath resolves the cache from the caller\u2019s PowerShell location. The recorded cases distinguish the process directory from the PowerShell location in both directions and cover a missing directory."
    },
    {
      "assessment": "fix_rejected",
      "finding_id": "R1-F6",
      "rationale": "The frozen artifact identities now agree across the records. The remaining evidence gap is retention of the consumable raw pair in a matching checkout. The recorded stamp comparison also names 77ac9e9, while the H gate produced a035f2c8\u2026."
    },
    {
      "assessment": "fix_verified",
      "finding_id": "R1-F7",
      "rationale": "The README and release guide now explain cache preparation, pinned references, package validation and the four-profile audit command. Both native and combined-stack status are explicit. The linked paths and build-guide anchor resolve."
    },
    {
      "assessment": "fix_verified",
      "finding_id": "R1-F8",
      "rationale": "The Nexus copy again states 0.28.2 for release 0.5.3, matching the tagged manifest. The installation guide scopes 0.31.1 to the unreleased candidate."
    },
    {
      "assessment": "fix_verified",
      "finding_id": "R1-F9",
      "rationale": "The index labels the exported hashes as Git-blob digests. I reproduced all 13, including 590efc63a1ae55c8cf07e9be7a497165c35558f3d2dd8fafa77dcf50bee613b6 for frozen-product.json."
    }
  ],
  "request_id": "kbp-codex-kingmaker-buff-planner-umm-0311-baseline-20261123182207-144d-r02-6c308b",
  "round": 2,
  "schema_version": "agent-review.review.v1",
  "summary": {
    "delivered_behavior": "The candidate declares UMM 0.31.1 and the frozen build records show the pinned UnityModManager 0.31.1.0 and Harmony12 1.2.0.1 compiler inputs. Standalone validation inspects packaged AssemblyRefs. The same frozen DLL passed static API resolution on all four loader rows.",
    "pending_later_phase_work": [],
    "remaining_owner_review": [
      "The owner can decide whether to authorize a future disposable native fixture and, after the blocking findings are resolved, whether to merge the draft PR."
    ],
    "scope_reviewed": "Final round-2 review of KingmakerBuffPlanner PR #26, including the nine prior findings and the directly affected build, package, installer, deployment, runtime-profile and evidence consumers.",
    "unresolved_questions": [
      "Where are the raw audited ZIP, matching sidecar and frozen checkout retained for owner use?"
    ]
  }
}
```
</details>

_Model-generated review by Codex using the requested model, posted by the agent-review controller through the owner's GitHub account. Not a human review or approval._
