# Release 0.6.20

[PR #25](https://github.com/howardreith/KingmakerBuffPlanner/pull/25) (missing beneficial buffs) merged on November 22, 2026. The owner approved the PR after the direct-implementation recovery of review round 12 and authorized merge and publication ahead of in-game testing. [GitHub release v0.6.20](https://github.com/howardreith/KingmakerBuffPlanner/releases/tag/v0.6.20) was published as the latest stable release at 07:03:54 UTC.

| Release identity | Value |
|---|---|
| Approved candidate | `334dff7301ff7b64a51cf4656fefe116de03c774` (product `75e5f41`) |
| Merged PR head and tag target | `583bf58864100acfe9ca623d24ffc13941bee15d` (adds the 0.6.20 version bump and release notes) |
| Released ZIP SHA-256 | `54153abb96c9aa5a3e0cb87dc18401dc23aeb47e603b9a69148b5a944668f780` |
| Released DLL SHA-256 | `4e923131e9dbb77e5bd5d7913c095c6708ca546b078597cfdde1bf8c15ebd6ed` |

The guarded push fast-forwarded main from `dd3e997` to `583bf58`, and GitHub marked PR #25 merged. From the isolated `private/release/0.6.20-stable` clone, Windows PowerShell 5.1 ran `scripts/Build-Local.ps1`, then `scripts/Publish-Release.ps1 -Publish -ConfirmOwnerAuthorizedPreRuntimeRelease -ReleaseNotesPath docs/RELEASE-NOTES-0.6.20.md`. The transcripts are in `artifacts/release-session/` in that clone.

| Check | Result |
|---|---|
| Source validation | 47/47 |
| Complete offline C# suite | 857/857 |
| Publisher gates | 8/8 |
| Deterministic release build | 3/3, including two identical builds |
| Package validation | 8/8 locally and 8/8 on the downloaded ZIP |
| Published assets | Downloaded ZIP matched the local ZIP and the published SHA256SUMS.txt |

The released bytes have not been run in the game. The owner's in-game checks for this change are in [MANUAL-ACCEPTANCE.md](MANUAL-ACCEPTANCE.md#missing-beneficial-buffs--owner-in-game-acceptance-pending). The offline evidence and review history are in the [recovery ledger](../planning/MISSING-BENEFICIAL-BUFFS-RECOVERY-LEDGER.md) and the [audit record](../planning/MISSING-BENEFICIAL-BUFFS-AUDIT.md).
