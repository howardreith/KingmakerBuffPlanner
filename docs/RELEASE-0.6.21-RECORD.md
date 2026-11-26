# Release 0.6.21

[PR #26](https://github.com/howardreith/KingmakerBuffPlanner/pull/26) (UMM 0.31.1 compile baseline) merged on November 26, 2026 as `d7622a84`, with its accepted review archive `5da3a61f` (reviewed source `445336c`) in the main-branch history. The owner accepted the PR's technical readiness and authorized merge and a stable pre-runtime GitHub release on the same day. [PR #27](https://github.com/howardreith/KingmakerBuffPlanner/pull/27) added the version bump and release notes and merged as `5cfc1b48`. [GitHub release v0.6.21](https://github.com/howardreith/KingmakerBuffPlanner/releases/tag/v0.6.21) was published as the latest stable release.

| Release identity | Value |
|---|---|
| Accepted PR #26 | reviewed source `445336c8d9c767b556878773001a6154c446a957`, archive `5da3a61f112aa5b1dad984058750bdde242e0c7e`, merge `d7622a84a82e3ba3c0a61a6fef3e4020b8d8297c` |
| Release source and tag target | `5cfc1b48dda00c93dc7353a9508796de932d3b2d` (PR #27 merge; annotated tag object `a7afed4b`) |
| Released ZIP SHA-256 | `fc37d6a1af8c74624e39e1fe1c8de9449584bb9800e51437af96a0b8360807c5` (`KingmakerBuffPlanner-0.6.21.zip`) |
| Released DLL SHA-256 | `e6e058b3b6d18153a9d77f4975775bca1b45163b82dcff34a614337ff9a2ebc9`, MVID `85b41212-dc67-450f-924c-7df32241148f`, embedded commit `5cfc1b48` |
| `SHA256SUMS.txt` asset SHA-256 | `705c28d7ff6615b2bdaea7b19ab9e9baea0631162ac0e2fe469b3e234561da0a` |
| Declared UMM minimum | `Info.json` `ManagerVersion` 0.31.1 |
| Compile baseline | pinned UnityModManager 0.31.1.0 and 0Harmony12 1.2.0.1 (the DLL's loader AssemblyRefs, read from the published file) |

From an isolated clone of `main` at `5cfc1b48` (`private/release/0.6.21-stable`), Windows PowerShell 5.1 ran `scripts/Publish-Release.ps1 -Publish -ConfirmOwnerAuthorizedPreRuntimeRelease -ReleaseNotesPath docs/RELEASE-NOTES-0.6.21.md` (80 seconds, exit 0).

| Check | Result |
|---|---|
| Source validation | 47/47 |
| Complete offline C# suite | 857/857 |
| Publisher gates | 8/8 |
| Deterministic release build | 3/3, including two identical builds |
| Package validation | 13/13 locally and 13/13 on the downloaded ZIP |
| Published assets | Downloaded ZIP matched the local ZIP, the GitHub asset digest and `SHA256SUMS.txt` |

**Static loader audit of the published DLL.** The existing metadata audit (`LoaderApiAudit`, the tool recorded in [the evidence index](evidence/umm-0311/INDEX.md)) read the DLL extracted from the downloaded release ZIP (`e6e058b3…2ebc9`, MVID `85b41212…`). Every bound UnityModManager, 0Harmony12 and 0Harmony reference (8 types, 24 members) is **RESOLVED** on UMM 0.31.1, 0.32.4, the owner's 0.33.0 and 0.33.4: [api-audit-0.6.21-release.json](evidence/umm-0311/api-audit-0.6.21-release.json). This is static metadata resolution, not a game run. The earlier audits (`api-audit.json`, `api-audit-r7.json`) keep their own DLL identities.

The released bytes have not been run in the game. Native gameplay testing and combined-stack testing were NOT RUN under any loader version, and no human or runtime acceptance is claimed. No Nexus upload was made.
