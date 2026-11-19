# Release 0.6.19

[PR #24](https://github.com/howardreith/KingmakerBuffPlanner/pull/24) merged on November 19, 2026. The owner authorized publication ahead of manual testing. [GitHub release v0.6.19](https://github.com/howardreith/KingmakerBuffPlanner/releases/tag/v0.6.19) was published as the latest stable release at 17:09:24 UTC.

| Release identity | Value |
|---|---|
| Merged PR head | `529e3a632c8e2d9873ca9b22bf258e4d2228f73f` |
| Tag target | `78b930e40fecdab4eea45dcdae5fd1612f8d0f0f` |
| Released ZIP SHA-256 | `20512c95fb3333adbee3464cd398ec92f76fe5ef438d575de22f369d50100ff7` |
| Released DLL SHA-256 | `cf20a146756a88dc29f12ff0c6fed4533cdd2d48c1ed4149b9271762e05e8c41` |

The guarded push fast-forwarded main. From the isolated `private/release/0619-stable` clone, Windows PowerShell 5.1 ran `scripts/Publish-Release.ps1 -Publish -ConfirmOwnerAuthorizedPreRuntimeRelease -ReleaseNotesPath docs/RELEASE-NOTES-0.6.19.md`. The transcript is in `artifacts/release-session/publish.log` in that clone.

| Check | Result |
|---|---|
| Source validation | 47/47 |
| Complete offline C# suite | 749/749 |
| Publisher gates | 8/8 |
| Deterministic release build | 3/3, including two identical builds |
| Package validation | 8/8 locally and 8/8 on the downloaded ZIP |
| Published assets | Downloaded ZIP matched the local ZIP and published SHA256SUMS.txt |

The released bytes await manual game testing. The earlier candidate from `7a732c4fed1fffc8d1cf740f9d54f5250c18313d` has its own game-side observations, artifact identities and recovery record. [Candidate evidence](LOCALIZATION.md#candidate-and-evidence).
