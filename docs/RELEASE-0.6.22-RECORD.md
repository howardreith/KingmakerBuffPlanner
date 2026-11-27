# Release 0.6.22

[PR #29](https://github.com/howardreith/KingmakerBuffPlanner/pull/29) (Greater Magic Fang repair) was accepted by the owner at `beff969` on November 26, 2026. The owner authorized merge and a release on the same day. The release-preparation commit `82db191` (version bump, release notes, changelog) was added to the PR branch, and `main` was fast-forwarded to it through the guarded push helper, which GitHub recorded as the merge of PR #29. [GitHub release v0.6.22](https://github.com/howardreith/KingmakerBuffPlanner/releases/tag/v0.6.22) was published as the latest stable release.

| Release identity | Value |
|---|---|
| Accepted PR #29 | reviewed source `beff96947e5f584a4cf8d65b37bb5d5a5c2dacb8`; merge (fast-forward) `82db19194a77edc07340245469c82a5eb717dbec` |
| Release source and tag target | `82db19194a77edc07340245469c82a5eb717dbec` (annotated tag object `9515eef7`) |
| Released ZIP SHA-256 | `236c45617ec5a1f6c1b2f9a0c0b0a3de8dd4b396078175bb735249f75b2cced3` (`KingmakerBuffPlanner-0.6.22.zip`) |
| Released DLL SHA-256 | `598953fd444e352b0daac8125dede6dac489e711dc5d435289e7c58099077c99`, MVID `b63194f1-1d3c-4e04-9f8c-296937e16e77`, embedded commit `82db1919` |
| `SHA256SUMS.txt` asset SHA-256 | `72ad0a1ea9244105cb59b46c9bbe1a9a2aafc828648feb6be1cf3209c32b30c5` |
| Declared UMM minimum | `Info.json` `ManagerVersion` 0.31.1 (unchanged from 0.6.21) |

The release was published from an isolated clone of `main` at `82db191` (`private/release/0.6.22-stable`). There, Windows PowerShell 5.1 ran `scripts/Build-Local.ps1`, then `scripts/Publish-Release.ps1 -Publish -ConfirmOwnerAuthorizedPreRuntimeRelease -ReleaseNotesPath docs/RELEASE-NOTES-0.6.22.md` (exit 0).

| Check | Result |
|---|---|
| Source validation | 47/47 |
| Complete offline C# suite (publisher) | 871/871 |
| Complete offline program on `82db191` (development selector, all prefixes plus native-fixture verification) | 872/872 |
| Publisher gates | 8/8 |
| Deterministic release build | 3/3, including two identical builds |
| Package validation | 13/13 locally and 13/13 on the downloaded ZIP |
| Published assets | The downloaded ZIP matched the local ZIP, the GitHub asset digest and `SHA256SUMS.txt` |

The publisher's suite counts 871 because the development run also selects the explicit native-fixture verification check.

The static loader API audit recorded for 0.6.21 was not repeated: the loader references and the declared UMM minimum are unchanged, and package validation checks the DLL's loader references.

The released bytes have not been run in the game. The live two-pet Greater Magic Fang check and native gameplay testing were NOT RUN, and no human or runtime acceptance is claimed. The owner checklist is in [MANUAL-ACCEPTANCE.md](MANUAL-ACCEPTANCE.md#greater-magic-fang--owner-in-game-acceptance-pending), and the repair record is [GREATER-MAGIC-FANG-REPAIR.md](GREATER-MAGIC-FANG-REPAIR.md). No Nexus upload was made.
