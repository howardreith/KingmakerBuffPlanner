> Owner-selected continuous completion milestone, October 11: the original
> four-hour estimate is now eleven hours after two observed probe defects.
> Current clean source is `2c40f0633079248072c2f23a754905d665ef41d6`; its
> version 0.5.0 package remains NOT_RUN while the selected full purity window
> runs. Prior 743b1dc and dc60a37 game attempts FAILED and restored exactly.
> Follow the [current milestone](NATIVE-UI-FINISH-0.5.0-COMPLETION.md)
> and current validation policy. Earlier budget deferrals are historical.

The receipt and manifest below retain the earlier candidate chronology.
Their PASS observations apply to their named original bytes. The frozen
4dbfa33 aggregate FAILED at its decision-refusal assertion; the numeric
exit and underlying child output were not preserved. It did not pass
the complete gate. The four original game PASS receipts remain valid
for their original inputs.

> Current screenshot repair: clean source 7410f92/version 0.5.0 has a validated
> local package and focused offline capture checks; new game observations remain
> NOT_RUN and receipt stays PARTIAL. Old frozen gate is now recorded FAILED;
> required live-purity cost exceeds the ordinary budget. Exact source/artifact
> provenance, history and next selected observation:
> [capture checkpoint](NATIVE-UI-FINISH-0.5.0-CAPTURE-CHECKPOINT.md).

> October 11 Codex development checkpoint: workflow merged at `0463f553a859ecc195f862fba9632ee91586e08c`;
> native UI observer source `673d1e3a62356af3d1b4d5dcbd19f233fd521734`, version 0.5.0.
> Focused checks pass; new observations are NOT_RUN in game. The fallback below
> remains built from 4dbfa33 with its original PARTIAL receipt. See the
> [current review checkpoint](NATIVE-UI-FINISH-0.5.0-REVIEW-CHECKPOINT.md).

# Native UI finish 0.5.0 — evidence manifest

Original captures are the guarded runs' own frames, untouched, under
`C:\Dev\KingmakerBuffPlannerLab\runtime-evidence\<run>\`. Crops are derived comparison
crops (nearest-neighbour when enlarged) under
`C:\Dev\KingmakerBuffPlannerLab\native-ui-finish-0.5.0-evidence\crops\`; the complete
list with every hash is that folder's `manifest.json`. A capture proves appearance in its
state only; behaviour is proven by each run's record (`native-ui.json`, `qual-outcome.json`).

## Runs

| Run | Role | Result | Commit | Version | DLL SHA-256 | MVID | Screen |
|---|---|---|---|---|---|---|---|
| `kbpnui-facts-03` | before: 0.4.3 baseline captures (owner display 1920x1200) | PASS | `3fd5fe9` | 0.4.3 | `a6a379c876cb09a3984529e0cd8b5b96efe9e2427dba96198be01901dac6af2d` | `5ffa2a58-f419-44ce-872c-0d607660329e` | 1920x1200 |
| `kbpnui-final-qual-07` | after: final qualify, 1920x1080 | PASS | `4dbfa33` | 0.5.0 | `b32f320ecebd1288eb409cec4893953f2e685a1acad0898de1b8f03164e58e69` | `526e6663-b368-4e64-9afa-08f979dbcfe0` | 1920x1080 |
| `kbpnui-final-qual-720-05` | after: final qualify, 1280x720 (case F) | PASS | `4dbfa33` | 0.5.0 | `b32f320ecebd1288eb409cec4893953f2e685a1acad0898de1b8f03164e58e69` | `526e6663-b368-4e64-9afa-08f979dbcfe0` | 1280x720 |
| `kbpnui-final-cast-select-04` | final casting selection (non-casting) | PASS | `4dbfa33` | 0.5.0 | `b32f320ecebd1288eb409cec4893953f2e685a1acad0898de1b8f03164e58e69` | `526e6663-b368-4e64-9afa-08f979dbcfe0` | - |
| `kbpnui-final-cast-03` | final casting run (cases D/E) | PASS | `4dbfa33` | 0.5.0 | `b32f320ecebd1288eb409cec4893953f2e685a1acad0898de1b8f03164e58e69` | `526e6663-b368-4e64-9afa-08f979dbcfe0` | - |
| `kbpnui-facts-02` | facts stage, failed (probe) | FAIL | `c8a5a21` | 0.4.3 | `5f900661872d506d67c9633f555a269418e222e4a8399d1f84fd885ff70dda84` | `87e3884a-2df1-46b8-ab96-bd3fcbeede9b` | 1920x1200 |
| `kbpnui-facts-04` | facts: UI-09 implementation | PASS | `fa0bfc4` | 0.4.3 | `a6c3e141e86b50a7ba309e3fedc8cd07aca133392e2b53863b4949441b5746a7` | `339ad506-2e34-49dd-90de-1f1ed182ad01` | 1920x1200 |
| `kbpnui-facts-05` | facts: UI-10 implementation | PASS | `bbe8426` | 0.4.3 | `bf10718663d1fc412ed4d78e651e7d3f3641aae0f08c2043dba6235e06210cc2` | `3e92babc-daf9-4780-b40f-be12d92ff527` | 1920x1200 |
| `kbpnui-facts-06` | facts: UI-01 implementation | PASS | `d5a1d99` | 0.4.3 | `639accd43a13df27ecd213a804dd916e34f08f84641b807e07feda957401b172` | `ee6b289a-0955-439f-a920-9496e335e404` | 1920x1200 |
| `kbpnui-facts-07` | facts: UI-03..08, UI-11 implementation | PASS | `aad17d1` | 0.4.3 | `1ba4ecbc6cf07bc95a50ed16ca4c8d0346c550b57935e95baa5305c80a568958` | `e198b338-8126-4893-9609-6c766ce5d1ad` | 1920x1080 |
| `kbpnui-qual-01` | qualify, failed (fixed in 545a5cf) | FAIL | `16f749c` | 0.4.3 | `df682a06904cae3521620d71618b9e485d90d70f5220d0168f410e02eb57f41c` | `f85591a4-5ae9-4af4-8cde-4e5573b6d16f` | 1920x1080 |
| `kbpnui-qual-02` | qualify (0.4.3 version), superseded | PASS | `545a5cf` | 0.4.3 | `4c1da7096c8dc8e58d06e7954f382f6e107d7017dfee0722b197472cab79c512` | `b71976c1-252d-4f7b-8fa1-bad862c64a4d` | 1920x1080 |
| `kbpnui-qual-720-01` | qualify 1280x720 (0.4.3 version), superseded | PASS | `545a5cf` | 0.4.3 | `4c1da7096c8dc8e58d06e7954f382f6e107d7017dfee0722b197472cab79c512` | `b71976c1-252d-4f7b-8fa1-bad862c64a4d` | 1280x720 |
| `kbpnui-final-qual-01` | qualify 0.5.0, superseded | PASS | `8ae985d` | 0.5.0 | `b354bb9e5346cf1fdfdc0e0b64105881cd6c11f8582c5ea3d24473fe2f98bfb1` | `6d2a08ea-9b88-4724-92f5-2f8a8c8457f9` | 1920x1080 |
| `kbpnui-final-qual-720-01` | qualify 0.5.0 1280x720, superseded | PASS | `8ae985d` | 0.5.0 | `b354bb9e5346cf1fdfdc0e0b64105881cd6c11f8582c5ea3d24473fe2f98bfb1` | `6d2a08ea-9b88-4724-92f5-2f8a8c8457f9` | 1280x720 |
| `kbpnui-final-qual-02` | qualify (same-spell popups), superseded | PASS | `93b29b1` | 0.5.0 | `c9b4ddcca8251334e2b8fe6a8af87b16b470fea64ebb8e8e14474be1788286cc` | `fff52247-95c3-4a02-9972-88b1fca1b74c` | 1920x1080 |
| `kbpnui-final-qual-03` | qualify (key routes), superseded | PASS | `60d6611` | 0.5.0 | `83653ab58232ffc10f42bab5062de030a84c34ba8d7da470f4348ff285460045` | `59b5935f-95be-46b8-abf5-e8dc1ff8de41` | 1920x1080 |
| `kbpnui-final-qual-720-02` | qualify 1280x720 (key routes), superseded | PASS | `60d6611` | 0.5.0 | `83653ab58232ffc10f42bab5062de030a84c34ba8d7da470f4348ff285460045` | `59b5935f-95be-46b8-abf5-e8dc1ff8de41` | 1280x720 |
| `kbpnui-final-cast-select-01` | casting selection, superseded | PASS | `60d6611` | 0.5.0 | `83653ab58232ffc10f42bab5062de030a84c34ba8d7da470f4348ff285460045` | `59b5935f-95be-46b8-abf5-e8dc1ff8de41` | - |
| `kbpnui-final-cast-01` | casting run, superseded | PASS | `60d6611` | 0.5.0 | `83653ab58232ffc10f42bab5062de030a84c34ba8d7da470f4348ff285460045` | `59b5935f-95be-46b8-abf5-e8dc1ff8de41` | - |
| `kbpnui-final-qual-04` | qualify, failed (rejected disabled-state probe) | FAIL | `f31fea9` | 0.5.0 | `595ce75a7de1b7197746420de156ea29a4b5781b2df7f72ffa0c3117eebce1e3` | `0c4a92f8-a8a5-46f6-8c8e-25ecbee601e1` | 1920x1080 |
| `kbpnui-final-qual-05` | qualify, superseded (pressed frame showed idle) | PASS | `1938aaf` | 0.5.0 | `ed11adbe976fb2fdc41c7a32d5809d6eb2fbf6442d40167f534b639ad483e2c1` | `300351a9-97ff-4373-8ca9-01a234e57881` | 1920x1080 |
| `kbpnui-final-qual-720-03` | qualify 1280x720, superseded | PASS | `1938aaf` | 0.5.0 | `ed11adbe976fb2fdc41c7a32d5809d6eb2fbf6442d40167f534b639ad483e2c1` | `300351a9-97ff-4373-8ca9-01a234e57881` | 1280x720 |
| `kbpnui-final-cast-select-02` | casting selection, superseded | PASS | `1938aaf` | 0.5.0 | `ed11adbe976fb2fdc41c7a32d5809d6eb2fbf6442d40167f534b639ad483e2c1` | `300351a9-97ff-4373-8ca9-01a234e57881` | - |
| `kbpnui-final-qual-06` | qualify (state tints), superseded by the gate fix | PASS | `cd1b554` | 0.5.0 | `a8db3a16a36905e1c2f0889bd87ed6e8bf2730a14587cbf6f5265e2800fe8e41` | `6e0ed7cd-3d0b-41da-af95-d820bfec8a09` | 1920x1080 |
| `kbpnui-final-qual-720-04` | qualify 1280x720 (state tints), superseded by the gate fix | PASS | `cd1b554` | 0.5.0 | `a8db3a16a36905e1c2f0889bd87ed6e8bf2730a14587cbf6f5265e2800fe8e41` | `6e0ed7cd-3d0b-41da-af95-d820bfec8a09` | 1280x720 |
| `kbpnui-final-cast-select-03` | casting selection, superseded by the gate fix | PASS | `cd1b554` | 0.5.0 | `a8db3a16a36905e1c2f0889bd87ed6e8bf2730a14587cbf6f5265e2800fe8e41` | `6e0ed7cd-3d0b-41da-af95-d820bfec8a09` | - |
| `kbpnui-final-cast-02` | casting run, superseded by the gate fix | PASS | `cd1b554` | 0.5.0 | `a8db3a16a36905e1c2f0889bd87ed6e8bf2730a14587cbf6f5265e2800fe8e41` | `6e0ed7cd-3d0b-41da-af95-d820bfec8a09` | - |

## Before and after

### UI-03..08, UI-11 full planner

Before: 0.4.3 planner (legacy sans-serif text, development title, ledger footer), its own scroll popup open. After: native fonts, paper slips, Casting options, compact footer.

- Before (0.4.3, 1920x1200): `kbpnui-facts-03/nui-planner-casting.png` `bbda89d19dc0eb0937fe3dbb55e9190c89971cacc2ef3be1635c4ec90315dba4`
- After: `kbpnui-final-qual-07/nui-planner-casting.png` `9d8eb6e908f41ff275bbb84d19f960ef4c5c99fb94929ad5111fa0fa0ee9acc6`
- After: `kbpnui-final-qual-720-05/nui-planner-casting.png` `5d802af54053a589d50ecb5d0c4a3fd4a0473e006b725b1076b7e1cd63d7e166`

### UI-01 description

Before: the planner's own parchment scroll. After: the same spell (Resistance) in the game's DescriptionWindow from the spellbook and from the planner, same scale and position.

- Before (0.4.3, 1920x1200): `kbpnui-facts-03/nui-planner-popup.png` `060fd0589ee2921104347d5e078670308c951c5c629ea6e62408a8d47f94b0d8`
- After: `kbpnui-final-qual-07/nui-native-popup.png` `c1cfa9a1cb1469b5ffdd54e527e00dddf94159079438bdde5387f62533aca5b1`
- After: `kbpnui-final-qual-07/nui-planner-popup.png` `58ce5b3521e0606ad6c5fc1fdf3688e0e7db7e65426cd698898af793295948c6`
- After: `kbpnui-final-qual-720-05/nui-native-popup.png` `de0f9502fbc4d5119c677c56a2d315df89e93dc1e64ed25ac3ace4c664d60cb4`
- After: `kbpnui-final-qual-720-05/nui-planner-popup.png` `3cb7226553a3672d9844eb355e8b129557f9f27f45d35507ddb08b251a033f0e`
- After: `crops/UI-01_after_1920x1080_native-spellbook-popup-Resistance.png` `d6818ef67656e322b2a0756f53fa1a713c1ea63f236bf6b222d4ef85a4284ed0`
- After: `crops/UI-01_after_1920x1080_planner-popup-Resistance.png` `61c2aca4895f55c350e286b206cd81964d6bc40d60fcd0c60f064eaa8b4dfa17`
- After: `crops/UI-01_after_1280x720_native-spellbook-popup-Resistance.png` `69def173f6d6a540696f32d2bbde8fae464e03bdacbdd2bda7bc943303156780`
- After: `crops/UI-01_after_1280x720_planner-popup-Resistance.png` `b786afefdfe657a95435da4763f9128c40ec4d4c65a8d55c4c12ce887f9e602c`

### UI-09 HUD

Before: bare white glyphs above the old rail. After: four native cells in the stepped frame; hover (Setup) and pressed (Long) beside the native Formation cell's hover.

- Before (0.4.3, 1920x1200): `kbpnui-facts-03/nui-hud.png` `c3941d2ff7eca08a931493dcc91672f07bf84bb09b11faff6b568919812a126d`
- After: `kbpnui-final-qual-07/nui-hud.png` `3c1cee0d436cb0213d9e2bb7918a332d7fcafeef6f3594eec508030131f3b711`
- After: `kbpnui-final-qual-07/nui-hud-hover-planner.png` `65eb9f10ffade5a7ca4eb1e54c06fd16f7446871c6484bdb0181bb98f051218c`
- After: `kbpnui-final-qual-07/nui-hud-pressed-planner.png` `e13bf726557c60e4331c2fa0a18b5157704d9680c63339d692ee8794d929aba1`
- After: `kbpnui-final-qual-07/nui-hud-hover-native.png` `2d1915f52c661b66ca9257b039e941a56111575d730dbc6daed02355dc079914`
- After: `kbpnui-final-qual-720-05/nui-hud.png` `cefdc28de794a500a6efa445e493658d1e45a2b3015c3e14a978004a2ba49ee9`
- After: `crops/UI-09_before_1920x1200_hud.png` `76215253dec07e71c856323a9f1d3179814992953399966128525ffa700c54c1`
- After: `crops/UI-09_after_1920x1080_hud-idle.png` `69453f573577e6b42df34750fb05cf2b3dcbd83f9ee43b875acce7559849356c`
- After: `crops/UI-09_after_1920x1080_cells-idle_x3.png` `12169113205cb06577d1558f6f702608f95af1503d10dcc9fc03a9e58a66c892`
- After: `crops/UI-09_after_1920x1080_cells-hover-setup_x3.png` `5488c15921c9dd68d1b9238a4d1256f553c9517cac70d0e33f5490e7411b9ab2`
- After: `crops/UI-09_after_1920x1080_cells-pressed-long_x3.png` `12dd3c7c66d951851687a1db7e1d9065c07333ca24a78f9d0e13452424439a02`
- After: `crops/UI-09_after_1920x1080_native-hover_x2.png` `4ae2d7bddc485b338f4abdf6ab59f207a1f5b80ad16570c17199555e8a93bdf4`
- After: `crops/UI-09_after_1280x720_hud-idle_x2.png` `4c6e827f7c3581c3bc2b52e8108c539dca39d7ef55ecab14fc19deaf8af00124`

### UI-10 spellbook pair

Before: METAMAGIC alone in the footer (the planner entry sat at the window's top right). After: the pair for a caster; BUFF PLANNER alone for a character without a spellbook.

- Before (0.4.3, 1920x1200): `kbpnui-facts-03/nui-spellbook-0.png` `f84d8ac0afceedb3343b705ee2c390d0f3153f7511d0caf45713d43b9472997a`
- After: `kbpnui-final-qual-07/nui-spellbook-0.png` `4717a6729725e5973a93f115310bddf8fa78cc4ef5304555b9b74f9586ae0dee`
- After: `kbpnui-final-qual-07/nui-spellbook-2.png` `847000532e93dfce5ffa8d7145f0ce09a469ffe82f46df56ead932ff37410e63`
- After: `kbpnui-final-qual-720-05/nui-spellbook-0.png` `be69d457a4abb7fcd22c442d8f1b79406fec99bcb231a8b748a79860d62a7743`
- After: `crops/UI-10_before_1920x1200_spellbook-footer.png` `39762b5a9d591c449848d6d3476d1b135d158bb79d9fa2888849a460d5b715dc`
- After: `crops/UI-10_after_1920x1080_pair-caster-Linzi_x2.png` `078b6a6de8bf2d8d213fb5a61528e5b7f63358f779419a46b203af1a150bedb7`
- After: `crops/UI-10_after_1920x1080_pair-noncaster-Hedwirg_x2.png` `f386279954e455c3f493e113ca2e58162e753be4816d8f09dc0642bf0b670d72`
- After: `crops/UI-10_after_1280x720_pair-caster-Linzi_x2.png` `ac4308ef5a8621a70925392845cb4a0dd12c6e9e85fff67987e3219f6353503b`
- After: `crops/UI-10_after_1280x720_pair-noncaster-Hedwirg_x2.png` `39f38fd1b176c469b4eae865817cf6b72ab9ce2f610f03824500a67ceaa57de5`

### UI-02 window switching

The native window shown after each route, the planner closed.

- Before (0.4.3, 1920x1200): `kbpnui-facts-03/nui-switch-inventory.png` `060fd0589ee2921104347d5e078670308c951c5c629ea6e62408a8d47f94b0d8`
- After: `kbpnui-final-qual-07/nui-switch-inventory.png` `c13bfba416dcaa149edcf1bb2734cf009bf46afa4a2cf42446887aa32b683007`
- After: `kbpnui-final-qual-07/nui-switch-character.png` `cf5c804ed51ff80e4c2deb1651e20ea3a220ea67f052dd61463caecade5f76ed`
- After: `kbpnui-final-qual-07/nui-switch-spellbook.png` `f4818ffe9f4fdc6687516cdad8b03874e775888e0be22e6562724e87b13a7e76`
- After: `kbpnui-final-qual-07/nui-switch-key-OpenInventory.png` `e4bbcefe5e31597951269d500478e88bf543755c0271d3ead0ce17d1162e3712`
- After: `kbpnui-final-qual-07/nui-switch-key-OpenChracterScreen.png` `bc186fcecdc8290f7b2c16df3aff88dc1740317ca8547b2ad2b7c0c9df1637aa`
- After: `kbpnui-final-qual-07/nui-switch-key-OpenSpells.png` `f4818ffe9f4fdc6687516cdad8b03874e775888e0be22e6562724e87b13a7e76`
- After: `kbpnui-final-qual-07/nui-switch-key-OpenJournal.png` `08bc656dd855eddbc0a1eb76e01a234cb2e81a012da81b22e7b7c7d39b81f1a7`

### UI-05 sidebar

A new casting selected; nothing selected after Done (only the next step).

- Before (0.4.3, 1920x1200): none
- After: `kbpnui-final-qual-07/nui-planning-added.png` `10f7559fd660fab663d5a8a3754b3fb490c99318e0ccf5515ee192c6e155b096`
- After: `kbpnui-final-qual-07/nui-planning-no-selection.png` `b885a6bc1652c4ea575dccb4db6537dacf967960bff5a97026b26c9ac983aa82`
- After: `kbpnui-final-qual-720-05/nui-planning-no-selection.png` `8f66200383b1554f179ec050da86bd2c3fd9b8ee421e4ec5221b22122ad70ad4`

### UI-08 footer and game log

The refused press's line in the game log; "Not saved" with Retry save, then saved.

- Before (0.4.3, 1920x1200): `kbpnui-facts-03/nui-gamelog.png` `f624c5a2a626798b3eb35ab7d616cf67dcea642340f08202cfebcbbbe3a3974c`
- After: `kbpnui-final-qual-07/nui-gamelog.png` `8566502a11d8023f9133c84b790de8ee8548d94a33a542493847b38f6dab8a62`
- After: `kbpnui-final-qual-07/nui-save-failed.png` `aa91b75e3f05d981fc55d5b776e8467b93b3d14c892c83b3b96122d89eb74b56`
- After: `kbpnui-final-qual-07/nui-save-recovered.png` `d570cdbb7fd5920e71bb24e539503847104fc2c24f486cc9b07c9c8f37a62288`
