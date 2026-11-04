# 0.6.10 release record

Published stable GitHub **Latest** release, November 4, 2026, **20:53:51 UTC**.
The owner accepted and merged PR #19, then explicitly requested a new release
and version increment. [Release and installable assets](https://github.com/howardreith/KingmakerBuffPlanner/releases/tag/v0.6.10).

## Identity

- Merged-main base: `4804ecd5b5e07e02eed60898e23eafad5a1b28d2`, including PR #19
  and its active-run reporting correction.
- Preparation branch: `codex/kingmaker-buff-planner-release-0.6.10`.
  Version/notes commit: `b647f2239601f9757c562043205f1bb934f77292`.
- Final main/product: `6c754257b25ebe456a760bc1d413352ac7844de4`.
  This adds only qualification-fixture corrections after version preparation;
  no production casting code changed during release preparation.
- UMM/informational version **0.6.10**; CLR version **0.6.10.0**. C# 7.3,
  .NET Framework 4.7, Harmony12, standalone identity and minimum game-version
  gate remain unchanged. Candidate profile format is **6.5**, as implemented
  in PR #19.
- Annotated `v0.6.10`, tag object
  `b4174b7ee353390b73ac5fe611e4524dd32187b6`, peels to the final product above.
- GitHub release ID **434442186**; independently verified non-draft,
  non-prerelease and Latest.
- ZIP `KingmakerBuffPlanner-0.6.10.zip`, SHA-256:
  `4a931b5079ba06bc4d44f2fc38f0eb86f12b25a37f60a299d2f984c284840b0d`.
- DLL SHA-256:
  `b02802a923149741a5c7e14288cc8c0822343d1ea4367fd055b2e48e374a20d5`.
- DLL MVID: `d0fae283-59c1-4513-8568-4d7a1c18cd55`.
- Downloaded `SHA256SUMS.txt` SHA-256:
  `0f35f11dc0ec03120372b32f30ffddc4a2afc08654e6774d65962bf38040cc6c`.

Frozen product checkout: `private/release/0610-stable` at 6c75425. Local release
assets/manifest are under its `artifacts/release/0.6.10`; public downloads/API
receipts under `artifacts/verification/0.6.10`; publisher logs under
`artifacts/release-evidence/0.6.10`. Documentation is maintained separately in
`private/release/0610-record`, branch
`codex/kingmaker-buff-planner-release-0.6.10-record`. The original feature
worktree remains unchanged. No worktree was reset or cleaned.

## Release qualification

Selected the existing **OwnerAuthorizedPreRuntime** route with a **30-minute**
release-validation budget. The owner's release request follows the disclosed
native limits; acceptance of the PR does not establish human/in-game acceptance.
The unmodified publisher performed its complete official offline behavior suite,
publisher gate, deterministic builds and strict package checks.

| Check | Actual result |
| --- | --- |
| Source validation | 47 PASS / 0 FAIL per invocation |
| Final complete offline C# suite | **671 PASS / 0 FAIL** |
| Publisher gate tests | 8 PASS / 0 FAIL |
| Guarded push helper tests | 6 PASS / 0 FAIL |
| Deterministic release build | 3 PASS / 0 FAIL; two identical DLL/ZIP builds |
| Package validation | 6 PASS / 0 FAIL per invocation |
| Independently downloaded package validation | 6 PASS / 0 FAIL |
| ZIP / published API asset digest / downloaded checksum | Exact match |
| Downloaded DLL / UMM / CLR version / manifest MVID | Exact match |
| Annotated tag / frozen product | Exact match |
| Native casting, UI/input and Linux acceptance | **NOT RUN** |

Final publisher duration: **50.711 seconds**. Exact clean pushed main was
6c75425. The earlier failed attempt below created no version tag or release.
Both branch and default-branch updates used the tested project-owned helper;
main advanced only with `--ff-only` in the isolated checkout.

```powershell
# Preparation: source and publisher preflight.
powershell.exe -NoProfile -ExecutionPolicy Bypass -File scripts/Validate-Source.ps1
powershell.exe -NoProfile -ExecutionPolicy Bypass -File scripts/Test-PublishReleaseGate.ps1
powershell.exe -NoProfile -ExecutionPolicy Bypass -File scripts/Test-GuardedPush.ps1

# From the original workspace; existing owned helper, explicit default-branch scope.
powershell.exe -NoProfile -ExecutionPolicy Bypass -File C:\Dev\KingmakerBuffPlannerLab\codex-policy\Push-KingmakerBuffPlanner.ps1 -RepositoryRoot C:\Dev\KingmakerBuffPlannerLab\repo\KingmakerBuffPlanner\private\release\0610-stable -AllowDefaultBranch

# From the frozen release checkout at 6c75425.
powershell.exe -NoProfile -ExecutionPolicy Bypass -File scripts/Publish-Release.ps1 -Publish -ConfirmOwnerAuthorizedPreRuntimeRelease -ReleaseNotesPath docs/RELEASE-NOTES-0.6.10.md

# Independent public verification, same frozen checkout.
gh release download v0.6.10 --repo howardreith/KingmakerBuffPlanner --pattern KingmakerBuffPlanner-0.6.10.zip --pattern SHA256SUMS.txt --dir artifacts/verification/0.6.10
gh api repos/howardreith/KingmakerBuffPlanner/releases/tags/v0.6.10
gh api repos/howardreith/KingmakerBuffPlanner/releases/latest
git rev-parse 'v0.6.10^{}'
powershell.exe -NoProfile -ExecutionPolicy Bypass -File scripts/validate-package.ps1 -PackagePath artifacts/verification/0.6.10/KingmakerBuffPlanner-0.6.10.zip
```

In addition, a read-only ZIP inspection compared the downloaded UMM version,
CLR version, DLL hash and MVID to the local manifest, and required the downloaded
checksum, GitHub asset digest, local ZIP hash and peeled annotated tag to match.

[Publisher log](evidence/release-0.6.10/publish.log),
[duration](evidence/release-0.6.10/publish-time.txt),
[release manifest](evidence/release-0.6.10/release-manifest.json), and
[verification receipt](evidence/release-0.6.10/verification.json) bind these results
to the published bytes. The manifest's `publicationStatus=local-only` describes
its pre-upload build state; the release API/download receipts establish current
public state without rewriting the manifest. Tracked text copies normalize encoding and trailing whitespace;
raw originals remain in the frozen checkout. Archived publisher log SHA-256:
`c9d982d0fc6e456a2ff9e5f7fccfafed3c2521b202885653e60f5774a644cf93`.

## First attempt and fixture corrections

The first publisher attempt at b647f22 stopped at **666 PASS / 5 FAIL**, after
**39.112 seconds**, before creating a tag or release. All five were obsolete
fixture assumptions following the intentional policy/UI change:

- Routine-scoped one-pass forecasting and strict Instant blockers now explicitly
  select Require full plan where the scenario tests strict admission.
- Settings-preservation and retired-combat/fallback fixtures initialize a valid
  current-format availability value when asserting a clean saved profile, rather
  than accidentally exercising optional-policy normalization. They additionally
  assert that the availability choice survives the settings save.
- The reason-wording fixture expects the published prepared-uses explanation.
- The strict Instant fixture still requires its exact blocker, focus and zero
  submissions; it now requires the availability refusal to stay noninterrupting.

No warnings, validators, resource assertions or admission safety checks were
relaxed. Commit 6c75425 changes tests only. Its focused correction/neighbors pass
**23/0 in 8.0 seconds**:

```powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass -File scripts/Test-Development.ps1 -Only 'casting-workspace-routine-scoped-apply,session-save-preserves-player-settings,legacy-out-of-combat-preference-is-enforced,card-reasons-and-review-items-in-words,execution-policy-strict-instant-blocker-is-casting-specific,cast-available'
```

[First failed receipt](evidence/release-0.6.10/publish-first-attempt.log),
[first duration](evidence/release-0.6.10/publish-first-attempt-time.txt) and
[focused corrections](evidence/release-0.6.10/fixture-corrections.log) remain
separate from the final 671/0 publisher pass. The archived first log SHA-256 is
`81d63a301c5467adfe34d03e23ea6d599d282844f2c23bc8d0647788719e9cbd`.

## Behavior and remaining limits

0.6.10 defaults to casting the affordable subset in saved order, names every
uncast entry in the normal game log and preserves unavailable assignments.
Require full plan is an unchecked execution-footer opt-in. Format 6.5 resets
older strict defaults once because historical choice provenance was not stored;
strict users must enable it once after upgrading. Current valid preferences
then persist. Active runs preserve their original reporting context across
subsequent no-op attempts, including reopened mouse-planner Run.

This exact release has not been run in the game. Native checkbox/log rendering,
physical input, casting behavior and Linux acceptance remain NOT RUN. This
release establishes neither a native Heroism repair nor prepared-slot reuse
between runs without replenishment. Protective runtime stopping, launcher
source/WhatIf purity, disposable-save constraints, transactional staging/locks
and exact restoration remain in force.

Next action: user installation/observation or a separately selected guarded
native acceptance campaign using the frozen candidate and the
[recorded native checklist](CAST-AVAILABLE-DEFAULT-CHECKPOINT.md).
See [release notes](RELEASE-NOTES-0.6.10.md) and the
[Heroism investigation](HEROISM-REPEAT-CAST-REPAIR.md). No game, live Mods staging,
permanent installation or save was changed by this release workflow.
Release documentation local-link checks passed **278/0**; `git diff --check`
passed. Documentation does not change the published product, tag or assets.
