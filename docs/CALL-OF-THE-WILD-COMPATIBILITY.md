# Call of the Wild Compatibility Report

Status: PASS for the exact available no-save profile; save-backed execution is `DEFER — EVIDENCED`

## Qualified identity

- Profile: `call-of-the-wild`, schema 1.
- Call of the Wild: UMM ID `CallOfTheWild`, version `1.14.4c-2.1`.
- Fixture: 266 files, 66,201,967 bytes; directory manifest SHA-256 `26ce134fda9a6421519959d9cc9c3f8c5d4cf3288f48ba7f768df47c7704541a`.
- `Info.json` SHA-256: `32c0bc48c26eb22787e99fb1eb86d074df2cd7dcfe4804ce8eb381a3a589d44d`.
- `CallOfTheWild.dll` SHA-256: `4ebf8e1ed3e66ffed72ea33ea325595629423dacd5bffa23e3c9109144b26915`.
- Kingmaker Buff Planner commit: `57ed740d6fd6bbdf68dcdfe8c26368c744a3c91f`; package `29ee2ddb86f6ace36d9b9ec1cc7e75a2468f81721956836e39dbdae26752deb2`; DLL `2c79eefe2afc9a93ec0574a0056c7c9331ebb1add170561f6da4c72468687b13`; MVID `18be2ec9-702a-4ba1-ae15-306765e4231d`.

The guarded transaction verified the read-only fixture before copying, re-verified its transaction-owned staged copy, and restored the original whole `Mods` tree exactly. Runtime assertions required exactly one `CallOfTheWild` UMM entry with the exact version, exactly one `CallOfTheWild` assembly with the exact hash, and exactly one Buff Planner UMM entry and assembly.

## Discovery result

Both fresh-process runs scanned 9,064 abilities and 5,907 player-accessible candidates. Ownership derived from the exact staged `loaded_blueprints.txt` inventory found 7,342 Call of the Wild abilities, 4,937 optional candidates, 2,096 included optional sources, and zero optional unsupported candidates. Optional support classes reconcile to 2,008 automatic, 61 bounded generic-reflection wrappers, and 27 existing explicit structural adapters; no Call of the Wild-specific hardcoded adapter was needed.

Four exact representative assertions cover distinct discovery paths:

| GUID | Ability | Evidence path |
|---|---|---|
| `0027cbfe0a484380ab76df1ad3d7326a` | Dazzling Blade | automatic `ContextActionApplyBuff` |
| `03963bcf8dd64abea3757311c1e8a79c` | Regenerative Sinew: Restoration | bounded `ActionList` wrapper with preserved diagnostics |
| `151b1f365c4217e5062a1fe50f7a63d3` | Bless Weapon | `ContextActionEnchantWornItem` |
| `4421fff35fed4afb9ea20cbd6e6a7c0d` | Globe of Invulnerability | `ContextActionSpawnAreaEffect` plus `AbilityAreaEffectBuff` |

Runs `20260811T2241040368066Z-native-buff-catalog` and `20260811T2242392501436Z-native-buff-catalog` each passed 26/26 assertions. Their 120 MB catalogs are byte-identical at SHA-256 `7b54f3f9f6d90d339c4cabeedb04c9d15bcb4d51d8e7d830150a18ab6eced659`.

## Harmony inventory

Both runs emitted the same ordered inventory: SHA-256 `a883dd60218a1f9e989a4e6b03d99318242d401d33f914cd6c068f767b308427`, 207 target methods and 228 patch records. Owners are CallOfTheWild (225 records), UnityModManager (2), and UnityModManager.UI (1). Each record includes patch kind, sequence, Harmony index, priority, owner, before/after constraints, patch method, and target method. There are zero multi-owner targets and zero Buff Planner overlaps because Buff Planner applies no Harmony patches.

## Qualification boundary

This is more than a main-menu load: exact runtime identities, full catalog completion, representative ownership/inclusion, zero unsupported optional candidates, deterministic output, ordered Harmony inventory, protected-save non-use, and exact restoration all passed twice. No project-owned `KBP_` save exists, so optional-provider planning, animated/instant firing, effect/resource equivalence, duration, and persistence were not executed and are not claimed. No protected save was selected or written.
