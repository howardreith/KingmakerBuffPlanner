# Environment Fingerprint

Status: IN PROGRESS

Captured from the isolated desktop on 2026-08-11. Local paths, account names, save names other than the reserved project prefix, and other machine-private values are intentionally excluded from this committed record. The exact local intake remains outside Git.

## Platform and build tools

| Item | Observed value |
|---|---|
| Windows | 10.0.19045 |
| Windows PowerShell | 5.1.19041.6456 |
| Git | 2.55.0.windows.3 |
| MSBuild | 17.14.51.32402 |
| .NET Framework reference target | v4.7 present |
| Steam app | 640820 |
| Kingmaker displayed version | 2.1.7b |
| Unity engine | 2018.4.10f1 |
| Unity Mod Manager | 0.28.2 |
| Harmony | legacy Harmony12 1.2.0.1 |

## Exact local binary identities

| Relative identity | Bytes | File version | SHA-256 |
|---|---:|---|---|
| `Kingmaker.exe` | 650752 | 2018.4.10.10503941 | `94a779c5423199fcb0470bd89884a3b3875dee2072eb1a7b1d7bc8e67accb1a1` |
| `Assembly-CSharp.dll` | 7262208 | 0.0.0.0 | `3b6450ffec440e296e586f71c711b195aed144b28d53e1cbb29406d18fef5afb` |
| `Assembly-CSharp-firstpass.dll` | 3867136 | 0.0.0.0 | `069a7362ce5e3ccd597206174aec13743c2db5a1bfbc2a42f15a5fbd1ea30d30` |
| `Newtonsoft.Json.dll` | 456704 | 8.0.0.0 | `3a23988f473e9e8304fbbcfff16872cca919bd1a1f3dee1e374acc1f0cbbbc80` |
| `UnityEngine.dll` | 72192 | 0.0.0.0 | `50b79c57f46bf4e060bb27461b6d4fac6f078e08b57e776c7a811554c4899fd6` |
| `UnityEngine.CoreModule.dll` | 848896 | 0.0.0.0 | `3a76df7f709d465e3273502e08edbffb536b1c2f78c3a132b8668e59fddd2803` |
| `UnityEngine.AnimationModule.dll` | 137216 | 0.0.0.0 | `18636cc226c7e6299e80210d4ae808eb2552cbcdb92c8713caee6a7f0e1be1a5` |
| `UnityEngine.AssetBundleModule.dll` | 20992 | 0.0.0.0 | `a11128e604a59eadd61ea870f2017ea18d4c19240dd1c1b8b6d4ffb7976b569b` |
| `UnityEngine.UI.dll` | 252928 | 1.0.0.0 | `4dee942e8974492511733e5482f89fde39e3964008f072ec9c12d05e40a9080d` |
| `UnityModManager.dll` | 184320 | 0.28.2 | `75b96e25a3a9fbadb47dd14a4ab490cb8c98143a6242aff3bba6145cd3047f39` |
| `0Harmony12.dll` | 102400 | 1.2.0.1 | `aa1cd48317254985d8b700cc74953477d1b40c3022ce9aa4c95ed2b8327e1292` |

The installed UMM differs from the transferred laptop fingerprint (0.32.4). Exact local assembly inspection establishes a coherent 0.28.2 target, but this deviation must be rechecked before runtime qualification.

## Local reference identities

| Reference | Identity | License | Status |
|---|---|---|---|
| BubbleBuffs | `f4871f763a23251284422ef0945a85e9f3fb788e` | MIT | PASS |
| BuffIt2TheLimit | `9c82f41ea89479048018fd09e878fe12a2761f65` | MIT | PASS |
| PathfinderAutoBuff | `6a327cbf5d98b461acbdeaf0cdf4114d46946b12` | MIT | PASS |
| KingmakerRebalance / Call of the Wild source | immutable 534-file transferred snapshot | MIT | PASS |
| Call of the Wild fixture 1.14.4c-2.1 | `4ebf8e1ed3e66ffed72ea33ea325595629423dacd5bffa23e3c9109144b26915` | read-only runtime fixture | PASS |
| Tabletop Added Rules fixture | no supplied snapshot | n/a | UNAVAILABLE-LOCAL-REFERENCE |

## External-state intake

- No Kingmaker process was running during intake.
- Steam and the UMM installer UI were already running and were not controlled or terminated.
- No `KBP_` save exists; all present saves are protected.
- Runtime state, staging, evidence, and backup roots were empty.
- No game, mod, save, Steam, or third-party reference state was altered.
