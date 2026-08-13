# Qualification

## 0.0.8 four-column UI qualification

Status: implementation and deterministic qualification PASS; exact release packaging/runtime/install checkpoint follows; human visual acceptance remains required.

Final release source is `6e5d02b21e587db84f2c7e7d2a34a63bace3e942`; package `22ce0c0e44c6f6b1f895199e58fe1afe5f639e6b38443e062fd6f4204ec8dbb2`; DLL `593db3bb0ce76316840f94e52d4698c7cd2353bc2aa31610608368478bcdda4b`; MVID `a8265c4e-e37d-4f54-a3e4-ee6578fdefa6`. Final Animated/Instant are 72/72 each, native 12/12, and Call of the Wild 26/26; all transactions restored. Guarded install `ui-grid-0.0.8-final-install` preserved settings and all other mods. Publication remains local-only.

- Deterministic gates: source 30/30; behavior/protocol 67/67; runtime filesystem 8/8; package 4/4; deployment WhatIf 5/5; aggregate 1/1.
- Grid gate: four columns at the 1920x1080 content width and compact 1600x900 width, no horizontal scrolling, 2,500 entries map to 625 rows while allocation remains capped at 32 cards.
- Candidate physical UI: actual four-column cards/icons, direct Bless portrait assignment, routine independence, all four category listeners, Selected only Long=1/Important=0/Long=1, one settings mode, zero retired labels, physical Ctrl+Shift+B, 21 open/close cycles, zero world/selection/camera/native leakage, confirmed execution, and exact restoration.
- Candidate discovery: native 1,722 abilities/974 candidates; Call of the Wild 9,064 abilities/5,907 candidates/2,096 optional inclusions/0 unsupported/0 planner Harmony overlap.
- The package is not cosmetically accepted by automation. Screenshots are evidence for the human checklist, not a visual verdict.

## 0.0.7 presentation qualification

Status: automated functional, compatibility, screenshot, packaging, and guarded-install acceptance PASS; human visual verdict pending.

Final release identity: source `2f125f9f1024692d83a1b2570209d1858d62eff1`; package `9feed6dffa668812ed826c75b743d72892e6e8371b0f81585fb557aea8fcf453`; DLL `bf8c72874377d56f91bcdb6daedaa8b28b340a948aee06583a32954d61b38927`; MVID `966b7d8f-bd5f-46b9-beda-62774f82ccac`; CLR 0.0.7.0; deterministic builds 2/2; publication local-only.

Exact-release repetitions: `ui-polish-0.0.7-release-animated` 71/71 (queued/started/confirmed/spent 1/1/1/1), `ui-polish-0.0.7-release-instant` 71/71 (submitted/confirmed/spent 1/1/1), `ui-polish-0.0.7-release-native` 12/12, and `ui-polish-0.0.7-release-cotw` 26/26. Every transaction restored exactly. Guarded install `ui-polish-0.0.7-install` preserved settings, verified all other mods, and installed the exact DLL/MVID.

- Branch `codex/ui-parchment-bubblebuffs`; preserved MVP source `e656812572adea8bc312419372b61ee8c4834e5a`, package `ce7492b262f01a9afb5a7666fe7e4bda9be1821395eb00244f5898b6882208e9`, DLL `6144256c6a0623e908c3d9e821a1b87ee5800195759fbfabb1e587eaf9be1d9b`, MVID `bff11809-aa53-42c2-8ab7-ef3564450e61`.
- Mechanical gates after all four phases: source 30/30, behavior/protocol 63/63, runtime filesystem 8/8, deployment WhatIf 5/5, package validation 4/4.
- Exact native resolution: `Inventory_Book_Clear`, `WeaponSets_Header`, `spellbook_frame`, `spellbook_frame_back`, `button_normal`, `button_pressed`, inventory toggle sprites, `Group_Char_Frame_Select`, Arial. Unsafe stretched frame/card use was rejected by the first screenshot and replaced by centralized fallbacks.
- `ui-polish-0.0.7-animated-1` and `ui-polish-0.0.7-instant-1`: 71/71 each, fresh processes, explicit requested engine, Bless planned/submitted/confirmed 1/1/1, five screenshots, 11 real icons/0 fallbacks, one mode control, zero retired labels, physical HUD/modal/input/lifecycle checks, immutable baseline, exact Mods restoration.
- `ui-polish-0.0.7-native-final`: 12/12. `ui-polish-0.0.7-cotw-final`: 26/26 with exact optional ownership and no planner Harmony overlap. Both restored exactly.
- Release package validation passed 4/4 and the installed primary DLL SHA-256 matches the release manifest exactly.
- Human inspection remains required for native feel, clipping, contrast, responsiveness, and workflow clarity. No merge, push, or public release is authorized.

## 0.0.6 live row-rendering recovery

Status: PASS for automated visual/runtime acceptance; guarded-installed for human handoff; local-only.

- Release source `e656812572adea8bc312419372b61ee8c4834e5a`; package `ce7492b262f01a9afb5a7666fe7e4bda9be1821395eb00244f5898b6882208e9`; DLL `6144256c6a0623e908c3d9e821a1b87ee5800195759fbfabb1e587eaf9be1d9b`; MVID `bff11809-aa53-42c2-8ab7-ef3564450e61`; CLR 0.0.6.0; deterministic builds 2/2; publication `local-only`.
- Root cause A/B: `row-render-0.0.6-canary-3` screenshot `4b3f7e05...` showed neither the same-Content canary nor real rows while their renderers were non-culled; `row-render-0.0.6-canary-fixed-1` screenshot `71a6bbf...` showed the magenta canary, real rows, and details after making the hidden Mask source opaque. The alpha-clipped 0.001 Mask source had failed to write the stencil consumed by both panes.
- The canary is absent from production. The existing programmatic Unity `Button`/`Image`/`Text`/`LayoutElement` rows use Arial, explicit readable colors/heights, and `UI/Default`; actual row height is 42 pixels. Summary text distinguishes matched view models from bound rows.
- Fresh-process physical campaign runs `row-render-0.0.6-production-3` and `row-render-0.0.6-production-4`: 71/71 each, exact Working load, physical F10/mouse input, 21 reopen cycles, and exact Mods restoration. Both captured screenshot SHA-256 `cb2343683ebc4d3dfbb066de4b030c1745c518063354a6357a331a6d53d75c19` with ten readable rows and selected Bless details.
- First five names/rectangles: Bless `63.7,441.5-475.3,469.5`; Bless `63.7,410.8-475.3,438.8`; Channel Positive Energy — Heal Living `63.7,380.2-475.3,408.2`; Guidance `63.7,349.5-475.3,377.5`; Light `63.7,318.8-475.3,346.8`. Selected row and details title are both Bless.
- Independent screenshot measurements: row luminance ranges `162,159,184,163,157`, with `190–360` distinct ARGB values; details-title range `125` with 53 colors. Runtime evidence separately records alpha 1, inherited alpha 1, `rendererCull=False`, Arial, `UI/Default`, stencil equality, opaque hidden mask source, canvas, hierarchy, sibling order, and rectangles.
- Exact native Bless contract is `require=False,item=none,count=1,hasEnough=False,consumableRequired=False`. The old unconditional sufficiency check was invalid. Both final runs planned/submitted/started/confirmed/spent `1/1/1/1/1`, observed the expected Bless effect, and then correctly reported the prepared slot exhausted.
- Local gates: source 30/30; behavior/protocol 62/62; runtime filesystem 7/7; deployment WhatIf 5/5; package 4/4. Exact-release regressions: native `row-render-0.0.6-native-final` 12/12; Call of the Wild `row-render-0.0.6-cotw-final` 26/26, 2,096 optional inclusions, zero unsupported, zero planner Harmony overlap. Every runtime transaction restored exactly; immutable baseline stayed `afca8ac5...`.
- Guarded install `row-render-0.0.6-install`: `Installed`; exact DLL/MVID/CLR; `settingsPreserved=true`; `otherModsVerified=true`; no lock or process remains.

The diagnostic attempts `production-1` and `production-2` are not counted as final passes: they exposed two copies of a stale assertion requiring spent Bless to remain available. Their transactions restored exactly, and the final exact binary passed twice after the gate was corrected.

## 0.0.5 catalog/HUD/input/tooltip repair

Status: PASS for automated acceptance and guarded-installed; human visual retest pending.

- Release source `390bb8b5f514a38edf1c553962813e29a1b526fd`; package `3eba3158aa92a6b66e249ec35aa297500eb4c5decdf73974c26992219922349c`; DLL `6999284085bd6898f6bd871900783f6f81343a6f801b2d2c95acd208c6513b56`; MVID `d2fed415-bfa2-47a7-90ba-f50fa8d1c7de`; deterministic 2/2; local-only.
- Source validation 28/28; behavior/protocol 61/61; filesystem harness 7/7; package validation 4/4; deployment WhatIf 5/5.
- Fresh-process physical campaigns `catalog-input-0.0.5-five-second-physical-1` and `catalog-input-0.0.5-five-second-physical-2`: 69/69 each, exact Working load, immutable baseline, exact Mods restoration. The second held a stable tooltip for 5,010 ms across 344 frames.
- Catalog: 11 total -> 11 search -> 11 configured -> 11 duration -> 11 source -> 11 non-hidden -> 10 available -> 10 view models/rows/active rows -> 5 viewport-visible. Content `625.4x1044.0`, viewport `625.4x500.0`, details bound, no binding failure.
- Bless: blueprint `90e59f4a4ada87243b7b3535a06d0638`, spellbook source, one provider, prepared/available, visible/active non-zero row, selected/configured. Controlled execution planned 1 but submitted/confirmed 0 and refused exactly at `material-component-unavailable`.
- Physical input: player/movement/ability/selection/target counts all zero; selection and camera unchanged; native activation zero. Tooltip: continuous five-second hover, one enter delta, four listeners, zero raycast graphics, no raycast blocking, entirely onscreen.
- Rebuild: 21 cycles; 22 screen creates and 22 destroys after close; balanced input lease and restored mode/selection/pause.
- Exact-release regressions: native `catalog-input-0.0.5-five-second-native` 12/12; Call of the Wild `catalog-input-0.0.5-five-second-cotw` 26/26.
- Guarded install `catalog-input-0.0.5-five-second-install`: installed identity exact, CLR 0.0.5.0, settings preserved, every non-planner mod unchanged.

Failed experiments remain evidence, not passes: one managed-authority staging denial was guarded-restored; one UMM overlay-dismissal flake timed out and restored; one pre-activation install caught stale CLR 0.0.4.0 and rolled back exactly. The final candidate was rebuilt and every exact-package gate above rerun.

## 0.0.4 live UI bootstrap recovery

Status: PASS and guarded-installed for human visual confirmation.

- Release source: `5b96f3b4e713489ce677db3ac5acb83a10f80f01` on `codex/kingmaker-buff-planner`.
- Release identity: package `cb3799e799f641b1a9f7d79eb71942025b5df71a8de956e17369b24fe2f14d16`; DLL `6f72c38ef7e445121291ff2f17f207d49210ea30a2e07fe1105595133b706f1c`; MVID `305a8a6c-2b49-4e3b-a365-286638cbfafa`; deterministic builds 2/2; publication `local-only`.
- Local gates: source 26/26; behavior/protocol 60/60; transaction harness 7/7; deployment WhatIf 5/5; package 4/4; warning-free exact-reference Release build 1/1.
- Fresh real-campaign runs `bootstrap-0.0.4-human-live-6` and `bootstrap-0.0.4-human-live-7`: 65/65 each. Both loaded only `KBP_AUTOMATION_WORKING` through `SaveSlot.OnButtonSaveLoad`, physically dismissed UMM's configured ShowOnStart overlay, and physically delivered F10.
- Each campaign run proved one retained UI root; one active four-button row with four listeners in Setup/Long/Important/Short order; row above the native cluster; top-hit ownership; one observed F10 keydown; one opaque/raycast-owning planner; 21 opens; 21 destroys after final close; balanced input leases; Default mode and selection restored; no duplicate objects, native activation, world command, selection/ability target, movement, ability, player, or camera input.
- Authoritative object evidence is embedded in each result as `uiHudObjectEvidence`, including `StaticCanvas/HUDLayout/Menu_Buttons48px`, root/button instance IDs, active/interactable flags, screen centers, and world corners.
- Baseline remained immutable at `afca8ac5e42219bc50f428eb334a657cbcc2e31e8f2eb39c6ab53691cbb076d3`; Working changed only through permitted load bookkeeping. Both Mods transactions report `Restored` and `restorationVerified=true`.
- Exact-release regressions: `bootstrap-0.0.4-release-native-regression` 12/12 and `bootstrap-0.0.4-release-cotw-regression` 26/26; both restored exactly.
- Guarded install `bootstrap-0.0.4-local-install`: installed DLL/MVID exact, `settingsPreserved=true`, `otherModsVerified=true`, no process or lock afterward.

Earlier 0.0.3 no-save/UI-complete implications are invalidated by the direct human failure and must not be used as live-bootstrap evidence.

## 0.0.3 HUD/modal/execution R2 correction

Status: installed for authoritative human campaign retest; no automated campaign UI or Bless success is claimed.

- release source commit `d5a20aa7ddbb2ec7d131a4bed44f1ca65ecaaa65`;
- package/DLL/MVID: `42f823d6b8454ffe4497f4f652752a07d50738d5990c5a5243d091ba92d363e0` / `5d95368ee237e658e06b4948209f805568a417ea150eb36c3023df9b155f0950` / `f3f691a4-d691-4112-90a4-7beb9f06aad2`;
- deterministic build 2/2, source validation 21/21, behavior/protocol 57/57, runtime-harness filesystem 6/6, package 4/4, deployment WhatIf 5/5, installer WhatIf 5/5;
- native exact-release runs `r2-0.0.3-release-native-1/2`: 12/12 each, identical catalog/Harmony hashes, exact restoration;
- Call of the Wild exact-release runs `r2-0.0.3-release-cotw-1/2`: 26/26 each, identical catalog/Harmony hashes, exact restoration;
- campaign gate `r2-0.0.3-release-ui-boundary`: exact identity 4/4, then structured `BLOCKED` at `campaign-ui-unavailable`; exact restoration, no save accessed;
- guarded install `r2-0.0.3-local-install`: `Installed`, installed DLL/MVID exact, `settingsPreserved=true`, `otherModsVerified=true`, lock absent;
- installed profile SHA-256 remained `3723e3181c56bff6427a15b2ba85ffd76fd40e98f3f482253b15910f038d6b48` and retains Long → Bless/target with no migration.

The blocked campaign gate intentionally does not assert retained-button hit ownership, modal visibility, world-input isolation, Long resolution, or Bless application from a main-menu object graph. Those 11 acceptance rows remain pending the human checklist.

Status: PASS for all applicable no-save gates; save-backed core remains `DEFER — EVIDENCED`

## 0.0.2 full-screen UI/input repair

Human playtesting invalidated every earlier 0.0.1 UI-complete entry below. Those entries are retained as history, not current evidence. The corrected `final-no-save-core` no longer bundles a fake main-menu UI check.

- commit: `3bd519b000f3126b19462888aefeabe29374873d`;
- package/DLL/MVID: `651d9ce3f92649f86d6e619e46fe3293ace1019e10e3b086c2a8c3617452b68f` / `f039f436fb948c7acb203e60979dec3bb500e03e85ff8f6a73ae6753b293b850` / `5f57af25-8876-400a-99b9-5971d8bfd8f4`;
- source validation 19/19, behavior/protocol 56/56, runtime-harness filesystem 6/6, deployment WhatIf 5/5, package 4/4;
- native no-save runs `ui-repair-0.0.2-final-native-1/2`: 12/12 each, identical catalog/Harmony hashes, exact restoration;
- Call of the Wild runs `ui-repair-0.0.2-cotw-1/2`: 26/26 each, identical catalog/Harmony hashes, exact restoration;
- `ui-repair-0.0.2-ui-main-menu-boundary`: main-menu `StaticCanvas` absent, so corrected UI gate failed/blocked instead of manufacturing a PASS; exact Mods restoration succeeded and no save was accessed.

The campaign gate structurally requires four HUD buttons/listeners, a stable native anchor, one opaque/raycasting full-screen root, `FullScreenUi` mode, selection suppression, consumed click/drag/scroll/cancel, no observed movement/ability/selection command, an actual group-tab change, exactly-once Long flow, explicit empty-Long feedback, 20 open/close cycles, reconstruction uniqueness, and exact pause/mode/selection restoration. It remains pending an authorized campaign fixture and the human visual/input verdict.

Final local-only release/install evidence:

- release commit `447bbd288c803a4aec609db84a4c6076cbfe94f3`;
- package `1f328e26fdf2524fc85e8482077e5330bc5f7a48ce0d8841bd372997685d652f`;
- DLL `c2598e0d31e464eaf8446e15280cbe13b3eeb4e56b0de92e20cc8f29fb458e84`;
- MVID `e43f060b-a2b7-48db-b19f-b45704ef77c4`;
- deterministic build repetition 2/2 and package validation 4/4;
- installer WhatIf 5/5;
- guarded local install `ui-repair-0.0.2-local-install`: `Installed`, settings preserved, all non-planner mods byte-identical, exact DLL verified, lock absent.
- exact installed-release native runs `ui-repair-0.0.2-release-native-1/2`: 12/12 each, exact restoration;
- exact installed-release Call of the Wild runs `ui-repair-0.0.2-release-cotw-1/2`: 26/26 each, exact restoration;
- exact installed-release UI boundary `ui-repair-0.0.2-release-ui-boundary`: structured `BLOCKED` at `campaign-ui-unavailable`, five identity/precondition assertions, exact restoration.

Current source-only Phase 1 evidence (pre-checkpoint build from HEAD `4ca6008d873577e8e6263b54658620b649f81cd1`):

- source validation: PASS 14, FAIL 0;
- strict runtime protocol tests: PASS 11, FAIL 0;
- Release compilation: PASS 1, FAIL 0, zero warnings;
- package validation: PASS 4, FAIL 0;
- deterministic package repetition: PASS 2 identical builds;
- DLL SHA-256: `3b21bec7b283ab3f760e1c7fe81de6b9d06794696d323e35c9c2d498c4a78cee`;
- DLL MVID: `e07bf29a-cd45-44bf-870c-6cfa19988822`;
- package SHA-256: `f109c51b8afcd0af931c556ad5999ea62c1b88f951178da8d563253c18de4d21`;
- package entries: 3 allowlisted project-owned files, 0 prohibited payloads.

These hashes will change after the implementation checkpoint because the exact Git commit is embedded in the DLL. No runtime claim has been made. The Phase 1 gate remains IN PROGRESS pending guarded mod-load smoke twice from fresh processes and exact live-state restoration.

First guarded runtime attempt `20260811T1931581708021Z-mod-load-smoke`: FAIL at `unhandled-exception`. UMM loaded exactly 1/1 mods and the production entry point/commit, but a trailing-separator path bug resolved the game root as `Mods`; the runtime result was atomic and reported missing `Mods\Kingmaker.exe`. The game exited, guarded recovery then restored the exact original manifest (`restorationVerified=true`, manifest equality true, lock absent). This is not a mod-load PASS. Regression tests now cover both trailing and non-trailing UMM mod paths, and the orchestrator waits for the launched process to finish before its final restore decision.

Second guarded attempt `20260811T1935262721216Z-mod-load-smoke`: in-game result PASS with 5/5 assertions and all exact hashes, MVID `16d7db94-a319-4242-b036-e303d8ca9ddf`; restoration verified in `finally`. Orchestration then failed because its validator expected display form `0.28.2` while UMM’s exact runtime API returned `0.28.2.0`. The validator now uses the exact runtime identity and reports field-specific mismatches. Because orchestration did not complete PASS, this run is retained as diagnostic evidence rather than counted toward the required two-pass gate.

Phase 1 fresh-process qualification PASS:

| Run ID | Assertions | Commit | MVID | Restoration |
|---|---:|---|---|---|
| `20260811T1938252394711Z-mod-load-smoke` | 5/5 PASS | `d3f4dc1fec9970b1c3c8eed5100052edd996c870` | `e268ff99-af2e-4def-8511-746cbd1b5106` | PASS, exact manifest |
| `20260811T1939275269790Z-mod-load-smoke` | 5/5 PASS | `d3f4dc1fec9970b1c3c8eed5100052edd996c870` | `e268ff99-af2e-4def-8511-746cbd1b5106` | PASS, exact manifest |

Both runs used package SHA-256 `885113c4f8e1bb3a271579188823ceb3704a3b1f531ddce32efe26fa5f295764` and DLL SHA-256 `1470d6f3bc45d7612f7668e87ce862ff7d89aa415e5634581e8b23924a4ba235`. Runtime identities were Kingmaker 2.1.7 (displayed 2.1.7b; executable hash already fingerprinted), UMM 0.28.2.0 with exact hash, and Harmony12 1.2.0.1 with exact hash. UMM created its normal generated DLL cache only in the transaction-owned staged tree; both states recorded the mutation and discarded it before exact restoration. Final checks: game process count 0, deployment lock absent, live/original manifest equality true.

Phase 2 structural catalog qualification PASS:

| Run ID | Assertions | Catalog SHA-256 | Counts (abilities/candidates/effects/diagnostic abilities) | Restoration |
|---|---:|---|---|---|
| `20260811T1958580589645Z-native-buff-catalog` | 8/8 PASS | `bcacbe69bc71c85c5299b8fe8254c18baa33d66e9e3ecf53f6d4aa6b37094878` | 1,722 / 1,353 / 1,095 / 755 | PASS, exact manifest |
| `20260811T2000040593873Z-native-buff-catalog` | 8/8 PASS | `bcacbe69bc71c85c5299b8fe8254c18baa33d66e9e3ecf53f6d4aa6b37094878` | 1,722 / 1,353 / 1,095 / 755 | PASS, exact manifest |

Both runs used commit `07dc2380abbac74228eed88ce73113aeeabe61db`, DLL SHA-256 `61b63cfa352ae9bc9e15b8aa10a08ff9f2fe2ac3ff2c5ee059f38d2f4e0df975`, MVID `7f7ba872-03ce-4554-ae1d-3a5942e62e8d`, and package SHA-256 `b128386d8d6b6715a4d51190faf1d971304bd0fb748c7c0d4f57d6293ec9b20c`. Source-only gates were validation 15/15, protocol/domain 20/20, harness 5/5, package 4/4, deployment WhatIf purity 5/5. No save was selected or accessed.

Rejected evidence: run `20260811T1952280447102Z-native-buff-catalog` exposed an internal-type JSON opt-in defect (`native-buff-catalog.json` was `{}`); later runs before the full expression contract showed `{}` expressions. These are defects found and repaired, not qualification passes. Runtime and PowerShell validators now reconcile the root array/counts and require an expression discriminator on every row.

Phase 6 no-save UI root qualification PASS:

- run: `20260811T2036033407315Z-ui-root-smoke`;
- commit: `c65fec1c83dd9bdae3ea5dd0b445436eff933102`;
- assertions: 9/9;
- singleton roots: 1;
- repeated open/close cycles: 2;
- rendered open frames: 12;
- observed resolution: 2560x1440;
- DLL / MVID: `ae459a27720feec946bb29efb12b3dd742b15291f835efbf575db022785032d9` / `c488303c-b96e-4346-a3fb-31ee28ddd5cc`;
- package: `ed21469ffe335f6111a477ef3bda0d4690a82ae1bf446882662726b769f6ca9f`;
- transaction: `Restored`, `restorationVerified=true`; no save selected.

This proves exact UI module load, repeated lifecycle, IMGUI render, singleton behavior, and one representative resolution. It does not prove campaign configuration interaction, scene transition, 1920x1080, or 3840x2160.

Phase 9 native classification qualification PASS; included-source runtime equivalence DEFER — EVIDENCED:

| Run ID | Commit | Catalog SHA-256 | Audited/support/exclude/unsupported | Restoration |
|---|---|---|---|---|
| `20260811T2126328544602Z-native-buff-catalog` | `fba6e24` | `1c2881de5c600c430709fac075e0f4fb223d0e050ba52d07bfa7451cf97be0fa` | 974 / 413 / 561 / 0 | PASS exact |
| `20260811T2127318144905Z-native-buff-catalog` | `fba6e24` | `1c2881de5c600c430709fac075e0f4fb223d0e050ba52d07bfa7451cf97be0fa` | 974 / 413 / 561 / 0 | PASS exact |

Both runs were byte-identical. Support classes are 396 automatic, 1 generic reflection wrapper, 13 explicit adapters, and 3 overrides. The package/DLL hashes were `766514afc64d96cd719b2237d3435156987f05ba46d6f10b0f09f0806647ca79` / `0e0423f4d33f733421a9181299b661b608b8a328e16ba479661c66c82318cb35`. Source gates were validation 15/15, behavior 43/43, harness 5/5, deployment purity 5/5, package 4/4. No save was selected. The 413 included rows remain runtime-deferred because provider/resource/effect/executor equivalence needs an authorized project-owned save.

Phase 10 executable routine workflow and first no-save HUD smoke PASS; final Phase 10 gate remains IN PROGRESS:

- source commit: `420d7a1f1f20f49706a34d953df5f0d39f67e4a8`;
- source gates: validation 15/15, behavior 45/45, harness 5/5, deployment purity 5/5, package 4/4;
- runtime run: `20260811T2144351145396Z-ui-root-smoke`, 11/11 assertions;
- observed: one UI root, 12 open frames, two open/close cycles, three routine buttons, critical controls on-screen, 2560x1440;
- package / DLL / MVID: `e544b7b2940a455c9ac886237ae5d42420c8b32b6657736af06661c369406e72` / `bd3248bb56314fa68d8ecaf16761410c114d2578df24fcdd1da4cdcd1e35bdfb` / `cfa3b527-3186-4c0a-ba07-21fbaa49bf36`;
- transaction: `Restored`, `restorationVerified=true`, no save selected or mutated.

This proves the no-save HUD controls render and fit at one representative resolution. It is smoke 1/2, not the Phase 10 gate: remaining setup polish, common scale/resolution rows, transition/reload subscription evidence, and configured campaign execution require further work or the authorized `KBP_` fixture.

Phase 10 final no-save HUD gate PASS twice:

| Run ID | Assertions | Root/reconstruction | Layout profiles | Blockers/subscriptions | Restoration |
|---|---:|---|---:|---|---|
| `20260811T2201533093853Z-ui-root-smoke` | 15/15 | 1 / 1 | 3/3 | 0 / 0 | exact PASS |
| `20260811T2202528930958Z-ui-root-smoke` | 15/15 | 1 / 1 | 3/3 | 0 / 0 | exact PASS |

Both runs used commit `cfce05c921735c33617342e9b409eeae556a16f9`, package SHA-256 `c2aaac56568366e275302650dbaecd15d984158b5af4f98a670bd90b478937ba`, and DLL SHA-256 `961ac1a3b2c5f9194fb67413e12ba0c5e034ff2d11f2e240f16d1d02cf4e75f6`. Each rendered 12 frames across two cycles, destroyed/reconstructed the owned root once, ended with one root, exposed all three routines, and validated layout bounds for 1920x1080 at 1.0, 2560x1440 at 1.25, and 3840x2160 at 1.5. Actual process resolution was 2560x1440. No save was selected. Configured campaign execution remains `DEFER — EVIDENCED`, not PASS.

Phase 11 exact compatibility qualification PASS for all locally available no-save profiles:

| Profile | Run IDs | Assertions | Catalog SHA-256 | Harmony targets/records/overlaps | Restoration |
|---|---|---:|---|---|---|
| `call-of-the-wild` | `20260811T2241040368066Z`, `20260811T2242392501436Z` | 26/26 each | `7b54f3f9f6d90d339c4cabeedb04c9d15bcb4d51d8e7d830150a18ab6eced659` | 207 / 228 / 0 | exact PASS twice |
| `native-only` | `20260811T2244134771182Z`, `20260811T2245180617144Z` | 12/12 each | `50f66299912bef24a50984d9d8398ba2bb340a4f85b551a0ad6ff97c41393f3d` | 3 / 3 / 0 | exact PASS twice |

All four used commit `57ed740d6fd6bbdf68dcdfe8c26368c744a3c91f`, package SHA-256 `29ee2ddb86f6ace36d9b9ec1cc7e75a2468f81721956836e39dbdae26752deb2`, DLL SHA-256 `2c79eefe2afc9a93ec0574a0056c7c9331ebb1add170561f6da4c72468687b13`, and MVID `18be2ec9-702a-4ba1-ae15-306765e4231d`. The optional runs proved exactly one UMM entry and assembly for both products, exact Call of the Wild version/hash, four representative owned/included abilities, 2,096 optional inclusions, and zero optional unsupported candidates. Catalog and Harmony inventory hashes repeated byte-for-byte per profile. No save was selected or written. Tabletop and combined profiles are unavailable locally; save-backed native/optional execution remains `DEFER — EVIDENCED`. See `docs/CALL-OF-THE-WILD-COMPATIBILITY.md` and `docs/TABLETOP-ADDED-RULES-COMPATIBILITY.md`.

Phase 12 composed no-save core qualification PASS twice:

| Run ID | Assertions | Catalog / Harmony SHA-256 | UI proof | Restoration |
|---|---:|---|---|---|
| `phase12-no-save-core-1` | 22/22 | `df2a48e61677723d1687b828d261ba4c103d4351b0f393d1e97276b84d7b8cb6` / `b5605e22bde458a238d63c6ffe33a99eb712bd22bf3cbc74c42d443ad479efb4` | root 1, reconstruction 1, frames 12, cycles 2, layouts 3, blockers/subscriptions 0/0 | exact PASS |
| `phase12-no-save-core-2` | 22/22 | same byte-for-byte | same | exact PASS |

Both fresh processes used clean commit `3af45f3329df300dc0616da9393480abee8547ce`, package SHA-256 `c2ea8a3dbbfb1cbe670be422b33e11a8afddfeaac4ae27902d69f8c5f6febc19`, DLL SHA-256 `a4b56a59104f5ddcac8a4184ebc4f779216f83aaeb4c0b7e199da9dcfa650413`, and MVID `193901f8-0863-4622-8885-f35880e5daf9`. Each composed identity, full catalog/expression reconciliation, ordered Harmony inventory, and UI lifecycle/layout checks in one process. This is the applicable NO-SAVE core, not executor equivalence; the save-backed core remains unmet.
