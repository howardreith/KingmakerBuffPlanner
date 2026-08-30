# Qualification

## 0.0.14 buff catalog/caster-controls maintenance

Status: SOURCE/BEHAVIOR/EXACT-ASSEMBLY/DETERMINISTIC LOCAL PACKAGE PASS;
SAVE-BACKED IN-GAME VERIFICATION BLOCKED AND NOT CLAIMED.

- Branch/implementation checkpoint/version:
  `codex/buff-catalog-caster-controls` /
  `ec718fa96ae1cbbb1feeb5b3acd1900e867b699a` / 0.0.14.
  Final qualified implementation/record HEAD:
  `ce7099b089440e40716cbbd39c4e377c4fbe21c2`; subsequent handoff record is
  documentation-only.
- `.\scripts\Inspect-KingmakerVariantContracts.ps1`: exit 0. Exact
  `Assembly-CSharp.dll` SHA-256
  `3b6450ffec440e296e586f71c711b195aed144b28d53e1cbb29406d18fef5afb`;
  MVID `07fa1e4d-8618-41b3-9b8d-faa17d3b26f7`. Verified native visibility
  selection, transient availability, parent-child construction, target
  disposition, area filter, damage, projectile, and weapon-delivery contracts.
- `.\scripts\Test-SourceOnly.ps1`: source 38/38, protocol/domain 119/119,
  runtime-harness filesystem 8/8, package 4/4, deployment WhatIf 5/5,
  aggregate 1/1.
- `.\scripts\Build.ps1 -Configuration Release`: source 38/38, build 1/1.
- `.\scripts\Build-Release.ps1`: deterministic equality 2/2, release
  builder 3/3; each internal build source 38/38, build 1/1, local package 1/1,
  with package validation 4/4.
- `git diff --check`: pass.
- Local-only release ZIP/DLL/MVID:
  `239eb9de3657de030c88dabfffeaa3fab344ec01e8d561e6649281d7a9cf0571` /
  `6fe6d6837f5155b5ad1b1cdd1e64d47974cadbab44a824efa42dc0edea48b4d6` /
  `80732098-e9da-45b2-b402-7b4ca6f52752`.

The behavioral suite proves HUD callbacks/results and no Feedback/native-log
route; direct/selectable/rejected/exhausted/stale variant semantics; five-child
expansion and one parent debit; allied/enemy/ambiguous, offensive-marker,
legitimate-hidden, conditional, heal, worn-item, and pet classification; and
automatic/split/banned/capped/ability-scoped/reset/round-trip/stale/exhausted
provider policy plus layout contracts.

The repository guard was rerun read-only and returned exactly
`Disposable save ambiguity: baseline=0; working=0.` No Kingmaker process was
launched and no live Mods staging occurred. The guard read save headers only to
count exact authorized names; no ordinary save was selected, loaded, mutated,
or substituted. The checks in `docs/MANUAL-ACCEPTANCE.md` remain required for any
in-game claim, screenshots, active-effect proof, or resource-debit proof.

## 0.0.14 published release qualification

Status: MERGED; GUARDED-PUSHED; PUBLISHED; REMOTE/DOWNLOADED ASSETS VERIFIED;
SAVE-BACKED IN-GAME RESULTS NOT CLAIMED.

- Exact release/tag commit:
  `1ad148780d801d63d7ab40e52bba94b7c4627b47`; annotated tag object:
  `d9cd66949623f66f1da3a1c902df16f8f45a9955`.
- Guarded-push WhatIf `6/6`; live guarded push verified exact remote main.
- Publisher repeated source `34/34`, protocol/domain `112/112`, runtime
  filesystem `8/8`, package `4/4`, deployment WhatIf `5/5`, aggregate `1/1`,
  and deterministic release `2/2` before publishing.
- Release is public, draft=false, prerelease=false, published
  `2026-08-28T21:52:28Z`:
  `https://github.com/howardreith/KingmakerBuffPlanner/releases/tag/v0.0.14`.
- Published ZIP is 249,439 bytes; ZIP/DLL/MVID are
  `a319a7f18aa7a20e47282fdf5b10dfee0adafd355677cf870a7fbb065028484b` /
  `4602cfe124470f5b5e5d336018ddc51a474cd931b2327e58d58eac299e4bc5bf` /
  `9f2db5b0-f61b-41c5-a3f7-54bc0296e3fe`.
- GitHub reports matching SHA-256 digests for both assets. Independently
  downloaded ZIP and checksum bytes match, and strict package validation passes
  `4/4`. Checksum-file SHA-256 is
  `4bdc2449090f506382772e5efeef4805c4ea103c0b6ad7c908cf4d18f1e19a8d`.

No install or Kingmaker launch occurred during publication. The manual
save-backed checks remain explicit limitations rather than inferred PASS rows.

## 0.0.14 spell-name and concrete-variant engineering checkpoint

Status: SOURCE/BEHAVIOR/EXACT-ASSEMBLY/DETERMINISTIC LOCAL RELEASE PASS;
OWNER AUTHORIZED DEFAULT-BRANCH MERGE, GUARDED PUSH, AND PUBLIC RELEASE;
SAVE-BACKED IN-GAME VERIFICATION BLOCKED BY ABSENT GUARDED FIXTURES AND NOT
CLAIMED.

- `./scripts/Test-SourceOnly.ps1`: source `34/34`, protocol/domain `112/112`,
  runtime-harness filesystem `8/8`, package fixture `4/4`, deployment WhatIf
  `5/5`, aggregate `1/1`.
- `./scripts/Build-Release.ps1` at clean release-source commit
  `a78869c329e39734cd77f4b587d3d97b05fede70`: source/build `34/34` and
  `1/1` twice, package validation `4/4` three times, deterministic equality
  `2/2`, release builder `3/3`.
- Local-only package:
  `artifacts/release/0.0.14/KingmakerBuffPlanner-0.0.14.zip`, 249,448 bytes;
  ZIP/DLL/MVID are
  `182a597b899875851bd4f6e125a7222018a86bdf7688d455bcf750a512f4e5cd` /
  `7c6ce2b7bf79fd24625d0d6263d36803a4ec1cd7a873be211fa569d877811fae` /
  `9896cd99-f01e-44c5-afe3-980ca1d043b9`.
- `./scripts/Inspect-KingmakerVariantContracts.ps1`: exact
  `Assembly-CSharp.dll` SHA-256
  `3b6450ffec440e296e586f71c711b195aed144b28d53e1cbb29406d18fef5afb`,
  MVID `07fa1e4d-8618-41b3-9b8d-faa17d3b26f7`; parent/child constructor,
  `ConvertedFrom`, availability, spell-level, and one-spend contracts confirmed.
- `planning/NATIVE-BUFF-CATALOG.json`: both Resist Energy parents declare five
  structurally eligible persistent-buff children, with exact child parent GUIDs
  and blueprint order; `Protection from Arrows, Communal` retains its complete
  localized name.
- `git diff --check`: pass.

The current guard reports exactly `Disposable save ambiguity: baseline=0;
working=0`. No unrelated or personal save was substituted, no game was launched,
and compilation/IL/catalog evidence is not reported as an in-game PASS.

## 0.0.13 Powerful Change qualification

Status: AUTOMATED/STATIC PASS; GUARDED INSTALL PASS; OWNER ACCEPTED PUBLICATION;
REAL-CAMPAIGN CROSS-MOD NUMERICAL RESULTS NOT CLAIMED.

Proven automatically or by exact read-only contract inspection:

- the prior end-to-end empty-option failure path and absence of a UI-only filter;
- exact installed feature/toggle/marker/spellbook/reservoir identities and native
  command transaction boundary;
- semantic classification for all six direct ability scores and supported
  polymorph carriers;
- Bull's Strength availability only with the discovered caster capability;
- absent capability, ordinary caster, wrong spellbook, unrelated spell, and
  ineligible source rejection;
- selected mass-variant identity, shared-reservoir planning, fail-closed optional
  integration, mandatory native routing, and consumed one-shot cleanup; and
- unchanged source, package-allowlist, runtime-filesystem, and deployment-WhatIf
  gates.

The final deterministic test result is source `34/34`, protocol/domain `95/95`,
runtime-harness filesystem `8/8`, package `4/4`, deployment WhatIf `5/5`, and
aggregate `1/1`. Two clean-head release builds reproduced the same DLL and ZIP.
Exact release/tag commit is
`3c329cfff3530fe8397012565c238a81d55cec1d`; published ZIP/DLL/MVID are
`67768176032d6d980f09b708a636dfa8f07e5b052530deb327d833e8e4882d96` /
`b41f31da57f9b7ee69a4e693792bf4bb1a6f7e5ea7dbff0e723c72f24d02bf86` /
`995ed895-bb45-412c-b626-692816b1f833`. Package validation passes 4/4, and a
reflection-only audit finds zero compile-time optional gameplay-mod references.
The annotated `v0.0.13` tag and public release were created on 2026-08-27; an
independent GitHub download matches the asset digest and `SHA256SUMS.txt`, then
passes strict package validation 4/4.

Final install transaction
`brown-fur-powerful-change-0.0.13-published-install-20260827` replaced the
earlier diagnostic build with the exact published DLL/MVID, preserved all
planner settings, verified every other mod unchanged, removed staging, and
retained a recovery backup. Its `install-result.json` SHA-256 is
`49e81844fd1d111b09e4c69389a5764a9692c95b7210a2b81df34268c41afa2c`.
Release: `https://github.com/howardreith/KingmakerBuffPlanner/releases/tag/v0.0.13`.

Not yet proven for this Buff Planner build: a real Brown-Fur cast invoking the
other mod's Harmony transaction, exact reservoir delta, resulting Bull's
Strength +6 (+8 with Transmutation Supremacy), an unselected +4 control, and
recast/non-stacking behavior. Those checks require the procedure in
`docs/MANUAL-ACCEPTANCE.md`; compilation or blueprint inspection does not
qualify them.

## 0.0.12 HUD lifecycle hotfix

Status: SOURCE/BEHAVIOR/DETERMINISTIC PACKAGE/PERFORMANCE/NATIVE/OPTIONAL PASS;
GUARDED-INSTALLED HUMAN CAMPAIGN PASS; V0.0.12 PUBLISHED AND EXACT PUBLISHED
BYTES INSTALLED. SAVE-BACKED AUTOMATION REMAINS UNAVAILABLE BECAUSE THE
AUTHORIZED FIXTURES ARE ABSENT.

- Start: clean fetched `origin/main` at
  `4a83aec19e0f6098e23b2965b3992c328136c576`, version 0.0.11; branch
  `codex/kbp-hud-lifecycle-hotfix-0.0.12`.
- Baseline before production changes: source 32/32, behavior/protocol 86/86,
  runtime filesystem 8/8, package 4/4, deployment WhatIf 5/5, aggregate 1/1.
- Exact qualified source is `083dfbfcf651d44bb01b302ccbbabac823e236e9`.
  `Test-SourceOnly.ps1` passes source 34/34, behavior/protocol 91/91,
  runtime filesystem 8/8, package 4/4, deployment WhatIf 5/5, and aggregate
  1/1. Release compilation is warning-free.
- `Build-Release.ps1` passes 3/3 with two deterministic builds. The accepted
  pre-integration package is SHA-256
  `eabb4785be75129cbc6cffcab030afc9e4afac32957fb7b82af2c16b0e0ac72a`;
  DLL SHA-256 is
  `6db3693e0ba38b3672bd0eac36b06df6e5965de29a3e78f9b97513c6accfe9e1`;
  MVID is `920b3246-e7f7-4818-92f4-e54294ef2db0`.
- Final integration/tag source is
  `a48bfae2185a50f1c50d9151666e0b5ce0a0bc3e`. The guarded publisher repeats
  source 34/34, protocol 91/91, runtime filesystem 8/8, package 4/4,
  deployment WhatIf 5/5, aggregate 1/1, and deterministic release 2/2. Final
  ZIP/DLL/MVID are
  `1cbb2b215a78ab4dea2af5c99ebae211fe21e1f3532eb1865af3420a90ea8494` /
  `1964b0220fd0ddd4a15009900a30ee3ec3af83c4d90b022eebb87d27cde03cac` /
  `3947e19e-fd3b-4b11-95d8-8a1b360cf9a4`.
- The guarded installer wrapper's historical `0.0.2` prior-version argument
  correctly failed against the installed published 0.0.11 identity without
  mutation. After updating it to the exact prior release, installer WhatIf
  passes 5/5. Guarded-push WhatIf passes 6/6 on the requested branch; only that
  exact branch was added to the external helper allowlist.
- Regression coverage proves absent-HUD zero dispatch, same-host bounded readiness
  retry, live-candidate zero recreation, exact 120-frame expiry notification and
  re-arm, same-host replacement installation, stable-installed zero rediscovery,
  all required stale-chain predicates, active/inactive/replaced hosts, unload/load,
  disable/re-enable, and planner-hotkey invalidation.
- Source validation rejects global `FindObjectOfType<IngameMenuController>` and
  `Resources.FindObjectsOfTypeAll` in the HUD path, requires the scoped
  `hudHost.GetComponentInChildren<IngameMenuController>(true)` lookup, and rejects
  native host/anchor/cluster destruction.
- Intake log evidence is bounded: current 0.0.11 `output_log.txt` lines 3342-3392
  show unload cancellation followed by transient candidate creation and repeated
  validation failure. That process ended before expiry and contains no
  post-disappearance hotkey attempt, so button survival is not claimed from it.

Guarded runtime results against the exact qualified artifact:

- `hud-lifecycle-0.0.12-performance-1` passes 9/9: 1,777 frames over
  20.016 seconds; 50.493/88.780/90.896 minimum/average/maximum FPS; all 18
  moving-camera samples are 90.710-90.896 FPS; zero HUD object searches; zero
  HUD-install dispatches while the campaign HUD is absent; and exact
  restoration. Steady samples spend 4.686 ms in `UiRoot.Tick` over 1,726 frames,
  or 2.715 microseconds per frame. Profile SHA-256 is
  `05f9194c31ba07f9d6b19e0b0bb002234f2022be55065cd61b2cb55501913e3a`.
- Unchanged repetition `hud-lifecycle-0.0.12-performance-2` is retained as an
  8/9 threshold failure: the first non-moving startup bucket measured 49.810
  FPS against the unchanged 50 FPS threshold. All 18 moving samples were
  88.940-90.886 FPS, average was 88.642 FPS, discovery and install dispatches
  remained zero, and restoration verified. No threshold was weakened. Profile
  SHA-256 is
  `5527a7a8f1e2021bc343292dbd6891d01131419571e6828e4ded3d8361fead98`.
- `hud-lifecycle-0.0.12-native-1` passes 12/12 with 1,722 abilities, 974
  candidates, 952 detected effects, zero scanner exceptions, zero KBP Harmony
  overlap, catalog SHA-256
  `aa3e4e966c0edc3132444335362dcfcf97307451d783eb915ed4c0b126df76fb`,
  and exact restoration.
- `hud-lifecycle-0.0.12-cotw-1` passes 26/26 with 9,064 abilities, 5,907
  candidates, 2,096 optional inclusions, zero optional unsupported candidates,
  zero KBP Harmony overlap, catalog SHA-256
  `a4e5d2206225dc5ad9dcf54e9b7a46bb0d8f5dcb58d1d617dd3534e1872356b9`,
  and exact restoration.
- `hud-lifecycle-0.0.12-ui-contract-1` is a preserved 4/5 precondition failure:
  after 600 no-save main-menu frames, no campaign native UI contract was ready.
  It loaded the exact 0.0.12 identity and restored exactly, but it is not a HUD
  qualification pass.

Evidence is under `C:\Dev\KingmakerBuffPlannerLab\runtime-evidence\<run-id>`.
Runtime-result SHA-256 values for performance-1, performance-2, native, Call of
the Wild, and UI-contract are respectively
`27017ba0e4b03d9670428b493843bb1749f7dc45fc0086246f7064a30abee645`,
`4069aa18accbccdbbd1030803d1309284b77a0c1743ec2cb29babb4ecc648da0`,
`c0d5da78c32f8fc9ab3fc5cb48817066acca9fe51e64f80487ad03b2127aecd2`,
`c9b244e37138490b50059b3e84d84280c1e6e2b0131c0bbcfb7de4ba15793fc4`,
and `3d075b99bc5b5c04f3302f780a2cafc4c06d1c0ddce59a6f94a4eb47d13cf41e`.
Native/Call-of-the-Wild Harmony inventory SHA-256 values are
`28074625d02aed66d8ebdcd6b2321b444321db636712cb7042d018ccc30b55e4`
and `5631e630366ed72758547397ee03db6cbd9ed5fcbccdab5fdaf50ef6921bf0e4`.

The guarded save resolver reports exactly `Disposable save ambiguity:
baseline=0; working=0.` Those guarded save-backed automation rows remain
unavailable, and no unrelated save was substituted. The owner instead tested
the exact guarded-installed build in a campaign and accepted the fix. The fresh
log proves unloading suspension, one candidate creation, a temporary loading-
screen raycast failure, the same candidate remaining pending, and final
`Installed` state after 57 validation frames with four buttons and four
listeners. Setup then passed both presentation phases and opened successfully.
The full log SHA-256 is
`6fec850ea76a8cf43983b20cf58a50cf84b98ef1b064181d052fadf54b055548`;
the bounded excerpt and interpretation are in
`planning/HUD-LIFECYCLE-0.0.12-HUMAN-ACCEPTANCE.md`. No Buff Planner exception
appears. This human verdict satisfies the release gate without mislabeling the
unavailable save-backed automation as executed.

Every automated runtime transaction is `Restored` with
`restorationVerified=true`; no game process or deployment lock remains.

After explicit human authorization, guarded install
`hud-lifecycle-0.0.12-human-test-install-20260824` replaced only the verified
published 0.0.11 planner. Installed DLL/MVID match the local-only release above;
`otherModsVerified=true`, `settingsPreserved=true`, and failure is null. Install
result SHA-256 is
`e1b42a8287718a2ee8aba625e6155cf9ba404e56efcf109bb16abf4798595f84`.
No process or deployment lock remains. On 2026-08-24 the owner stated that the
fix is acceptable and explicitly authorized merge to the default branch and a
new incremented release.

That authorization completed successfully. The public release is
`https://github.com/howardreith/KingmakerBuffPlanner/releases/tag/v0.0.12`;
GitHub reports draft=false, prerelease=false, and published time
`2026-08-24T18:54:29Z`. A fresh asset download matches the GitHub digest and
local deterministic artifact and passes package validation 4/4. Guarded install
`hud-lifecycle-0.0.12-published-install-20260824` installed the exact published
DLL/MVID with `otherModsVerified=true`, `settingsPreserved=true`, and failure
null; result SHA-256 is
`bc7435f972acd52b4466781f2fcb2ead515364fe5d82528d7a03be1a4adefb76`.
No process or deployment lock remains. The separate published 0.0.11 asset is
still present and unchanged. Full publication evidence is in
`planning/HUD-LIFECYCLE-0.0.12-RELEASE.md`.

## 0.0.11 published crash and release qualification

Status: PASS; HUMAN PERFORMANCE ACCEPTED; TEST PROCESS NORMAL; EXACT RELEASE PUBLISHED AND INSTALLED.

- Starting crash checkpoint: branch `codex/kbp-performance-regression-0.0.11`, clean HEAD `df003955ce7ed659bd77950be37ed4814ae42ea2`; installed 0.0.11 DLL `1bde124702d013c7f66b159963c01a23eef691104ffa5523f9e944498238c4e7`, MVID `cab52ff1-e758-4f8f-ba92-5ad3cd4eb867`.
- Reproduction: exact pre-fix test EXE `958bd34c276609d9735377471700d3ee3da68c389e4b7fef312049514b2830f6` exited `-532462766` (`0xe0434352`) after unhandled `UnauthorizedAccessException` from `Directory.CreateDirectory` in `Program.Main` line 32. System/Application Popup Event 26 records include 388139 and 388141. Application-log queries found zero paired `.NET Runtime`, `Application Error`, or WER records. No duplicate or remaining test process existed.
- Causality: pre-fix tests used the guarded production runtime-evidence root for disposable fixtures, and directory creation preceded every catch/finally. The same exact EXE passed 78/78 with intended external access. Artifact enumeration found only current identical `artifacts/tests` and `obj/Release` copies; source audit found no thread, Task, async, timer, child process, or finalizer path.
- Correction `f6bbe648e0311e8b0022ec9810b533fc66cc6502`: disposable tests use a unique temp boundary; a separate test proves production-root rejection; resolver/fixture cleanup precedes PASS; infrastructure failures produce full stderr and exit 2. Final direct repository-style invocation passed 86/86, stderr empty, exit 0, no popup/event, no process.
- Integrated/tag commit `3661f5c31a1060bca67758c2369b2ef361a339c9`. Guarded publisher results: source 32/32, protocol 86/86, runtime filesystem 8/8, package 4/4, deployment WhatIf 5/5, source aggregate 1/1, deterministic release build 2/2 and release builder 3/3. No new test popup, `.NET Runtime`, `Application Error`, or WER event was generated.
- Exact published identity: ZIP `89cbebd2a1eb594d2307c4388c19588e1d4ea9c845284d36081c3e72d492795c`; DLL `95f484907f9a1008798e3557e46212faa1e41406bccf9d109d78e1921e9d46c6`; MVID `bf949174-0601-4822-a121-9c9d9c14597f`; tag `v0.0.11`; URL `https://github.com/howardreith/KingmakerBuffPlanner/releases/tag/v0.0.11`. GitHub digest, downloaded asset, local artifact, and `SHA256SUMS.txt` agree; strict downloaded-package validation passed 4/4.
- Owner runtime verdict: severe approximately 16 FPS regression gone. Exact published-asset run `perf-0.0.11-published-final-1` had 1,776 frames, 88.730 average FPS, and all 18 moving samples at 90.693-90.914 FPS with zero global HUD searches. Only the first non-moving launch bucket missed the strict threshold at 49.600 FPS due to one 194.286 ms initialization update; the harness rejected rather than hiding it, and restoration verified true.
- Guarded install `published-0.0.11-final-install-20260823`: status Installed; version 0.0.11; published DLL/MVID exact; `settingsPreserved=true`; `otherModsVerified=true`; no process or deployment lock. Published 0.0.10 remains intact.

## 0.0.11 performance repair

Status: OPENING-CAMERA PERFORMANCE PASS; SOURCE/NATIVE/OPTIONAL PASS; SAVE-BACKED CAMPAIGN ROWS BLOCKED BY ABSENT AUTHORIZED FIXTURE.

- Unfixed normal path `perf-0.0.11-unfixed-hud-on-2`: 228 frames / 20.080 s, 7.511 minimum, 11.358 average, 11.838 maximum FPS; 228 global HUD searches; 18,874.614 ms total and 95.441 ms maximum search time; transaction restored exactly.
- Same DLL with only HUD discovery suppressed, `perf-0.0.11-unfixed-hud-off-1`: 1,787 frames / 20.026 s, 60.680 / 89.234 / 90.908 FPS; zero searches; transaction restored exactly. This is the causal A/B.
- Fixed normal path `perf-0.0.11-fixed-hud-on-1` and `-2`: 1,786 and 1,775 frames; 18 moving samples each; 59.579 / 89.236 / 90.901 and 50.739 / 88.671 / 90.935 FPS; zero searches; 5.690 and 4.892 ms total root time; both restored exactly.
- Later exact checkpoint `perf-0.0.11-exact-hud-on-1`: 1,786 frames / 20.014 s, 18 moving and 2 settled samples, 59.559 / 89.237 / 90.991 FPS, zero searches, profile SHA-256 `cd5276a85afe13370a16d5236eeedafef4fd3eeeff58f9accffe6ceb145971c8`, restoration verified.
- `perf-fix-0.0.11-exact-native-1`: 12/12, catalog SHA-256 `41acd68374e93584a88b16b3a719fbbeeb2a9a0bca088fdf30ede30d371ba614`, 1,722 abilities / 974 candidates / 952 detected effects, zero scanner exceptions and Harmony overlap, restored.
- `perf-fix-0.0.11-exact-cotw-2`: 26/26, catalog SHA-256 `2e207d41f69f5db533533d6be7ecb132512a94f14a3239f6dfab200a6627b6ad`, 9,064 abilities / 5,907 candidates / 2,096 optional included / zero unsupported / zero KBP Harmony overlap, restored.
- Full source-only suite: 32/32 source, 78/78 behavior/protocol, 8/8 runtime filesystem, 4/4 package, 5/5 deployment WhatIf, aggregate 1/1.
- Final artifact source `d3af9be4e62ab8aa796e29343ab30b75e918fb8c`, package `eac7dd50afdb8b68f9d3a6577eb7fff9863883b966df8404fed67d623d407d34`, DLL `1bde124702d013c7f66b159963c01a23eef691104ffa5523f9e944498238c4e7`, MVID `cab52ff1-e758-4f8f-ba92-5ad3cd4eb867`; deterministic builds 2/2 and release builder 3/3.
- Exact-artifact run `perf-0.0.11-final-release-1` is rejected only because its first non-moving loading bucket was 39.945 FPS; all 18 moving buckets were 90.695-90.966 FPS, searches were zero, and restoration passed. Unchanged repeat `perf-0.0.11-final-release-2` passed 9/9 with 1,783 frames / 20.020 s, 18 moving samples, 56.687 / 89.061 / 90.873 FPS, zero searches, profile SHA-256 `d3eae40417e57b08d844d84cd42825ad6d1018eab7c136075ecd70ea66daf144`, and exact restoration.

The local cap is 90 FPS; the severe KBP ceiling disappeared and motion stayed at approximately 90 FPS. This qualifies the opening-book/camera reproduction by same-scene before/after comparison, not the user's exact 60 FPS cap.

Fresh campaign HUD/setup, profile persistence, target display, Animated/Instant execution, ordinary-area, cutscene, and world-map qualification is unavailable because the current profile contains zero exact `KBP_AUTOMATION_BASELINE` and zero exact `KBP_AUTOMATION_WORKING` saves. The guard rejects this as `Disposable save ambiguity: baseline=0; working=0.` Older 0.0.10 campaign passes remain historical evidence only. No unrelated automation or ordinary save was selected.

## 0.0.10 visual-clarity qualification

Status: deterministic, exact-release physical, packaging, and guarded-install qualification PASS; human visual acceptance remains required.

Final release source/package/DLL/MVID are `14719e816c31d4efadf829733d499774c6f5e741` / `46d741b7dd16120e5687069c215a5cc270b9ec1e8430fd764dd36a2bdb05f013` / `bcd6ed91e4d6898dec74e69f501389adb15728cd03fe0a0915522e5a1a18c55e` / `8bb28075-83cf-41d2-ad3d-e883886c4961`. Deterministic release builds reproduced the exact ZIP and DLL twice.

- Candidate source/package/DLL/MVID: `73de462b885bc7b24162d8670bc2be1b806baf37` / `19683ab7a01c9e583e8d2b8cd3f790b6ce6ca03284ab324fd42e4b035cf3da1b` / `92b2dcf09937c3d277af5e5235310e8a28c9737400911baa36535e7b88d47444` / `0c5f4132-baed-4a4b-90c5-0b5764125224`.
- Deterministic gates: source 32/32; behavior/protocol 75/75; runtime filesystem 8/8; package 4/4; deployment WhatIf 5/5. Four-column calculations cover 1920x1080 and 1600x900 content widths with no horizontal scrolling.
- `ui-clarity-0.0.10-final-animated-1` and `ui-clarity-0.0.10-final-instant-1`: 77/77 each in fresh processes. Both prove physical Ctrl+Shift+B, HUD/modal/world-input isolation, tooltips, 21 lifecycle cycles, consolidated Bless, direct assignment, profile round trip, selected-buff availability/planned-use text, and confirmed effect/resource outcomes. Animated planned/confirmed 1/1; Instant planned/submitted/confirmed 1/1/1.
- Active 1280x720/UI-scale evidence: HUD left-edge alignment `true`; glyph centering `true`; hit ownership `true`; native activation count 0. Text is `UnityEngine.UI.Text`, Arial, UI/Default, fixed size, best-fit false, root scale one, scale factor 1, nested canvases 0, planner scalers 0, fractional rendered text/card transforms 0.
- Screenshot hashes from Animated: render `9e457361...`; selected details `24a63490...`; grid `8484cf45...`; target states `9c0190a2...`; settings `d503e4c9...`; HUD `4282f218...`. Full files are under `runtime-evidence/ui-clarity-0.0.10-final-animated-1/`.
- The campaign catalog retains 11 provider-backed abilities and 10 visible cards, including one two-provider/two-ability Bless card and one Resistance card. Provider selection remains implicit.
- `ui-clarity-0.0.10-final-native-1`: 12/12 with 1,722 abilities. `ui-clarity-0.0.10-final-cotw-1`: 26/26 with 9,064 abilities, 2,096 optional inclusions, zero unsupported, and zero planner Harmony overlap. Both restored exactly.
- Every candidate transaction is `Restored` with `restorationVerified=true`. The immutable baseline descriptor remains the exact `KBP_AUTOMATION_BASELINE`; only Working was loaded through the normal save action.
- The authorized campaign has one party member. It physically proves strong direct green and the new summary, but cannot honestly show a second `COVERED` portrait or a controlled invalid portrait. Deterministic plan-domain tests prove those mappings; their appearance remains human-gated in an appropriate multi-member party.
- Documentation-inclusive exact runs `ui-clarity-0.0.10-exact-animated`, `ui-clarity-0.0.10-exact-instant`, `ui-clarity-0.0.10-exact-native`, and `ui-clarity-0.0.10-exact-cotw` repeated 77/77, 77/77, 12/12, and 26/26 respectively; every transaction restored exactly.
- Guarded install `ui-clarity-0.0.10-final-install` is `Installed`; `settingsPreserved=true`; `otherModsVerified=true`; installed DLL/MVID match the release manifest. No process or deployment lock remains. No merge, push, or publication occurred.

## 0.0.9 UI polish and source-consolidation qualification

Status: deterministic, exact-release physical, packaging, and guarded-install qualification PASS; human cosmetic acceptance remains required.

Exact release source is `f026a4a9974af8e4191ff7fb104e472f11c2016f`; package `471e86e0043b47bc899322b640fb448105bfc1689f796b56611ed5d980d4bbe8`; DLL `d66edcacedcfe9d862e5cd433e2e58166abbc5a5a5404b9b7c5d6fd39ae898a1`; MVID `174e2e17-9006-4667-b06d-85d372a2bb77`. Deterministic release builds reproduced the exact runtime package twice.

- Deterministic gates: source 30/30; behavior/protocol 72/72; runtime filesystem 8/8; package 4/4; deployment WhatIf 5/5.
- `ui-polish-0.0.9-exact-animated` and `ui-polish-0.0.9-exact-instant`: 74/74 each. Both exercised physical Ctrl+Shift+B, modal/pointer isolation, tooltips, four-column vertical grid, routine-local direct portrait assignment, 21 open/close cycles, profile persistence, one-card two-source Bless, and confirmed effect execution. Animated planned/confirmed 1/1; Instant planned/submitted/confirmed 1/1/1. Both restored exactly.
- The human-reproduction catalog has 11 provider-backed abilities but 10 visible cards. Bless reports `providers=2,abilities=2` on one card with automatic provider choice. Resistance also appears once in the captured grid. Deterministic tests separately prove alternative-provider selection, shared-pool non-double-counting, legacy-assignment union/round trip, and same-name mechanically distinct non-merging.
- The direct portrait state is captured with full green tint/overlay, bright 4px frame, and `SELECTED`. Party/area expressions deterministically yield the distinct lighter `IndirectCovered` token. This disposable save has only one party target, so multi-portrait indirect visual inspection remains a stated human-runtime item rather than an automated physical claim.
- `ui-polish-0.0.9-exact-native`: 12/12, 1,722 abilities. `ui-polish-0.0.9-exact-cotw`: 26/26, 9,064 abilities, 2,096 optional inclusions, zero unsupported, zero planner Harmony overlap. Both restored exactly.
- Visual evidence: grid `c3cace3b...`, target state `7c9d594c...`, HUD integration `5dac3635...` under `runtime-evidence/ui-polish-0.0.9-release-animated/`. Screenshots inform the handoff but do not grant cosmetic acceptance.
- Guarded install `ui-polish-0.0.9-final-install` replaced exact 0.0.8 with exact 0.0.9. Its result is `Installed`, `settingsPreserved=true`, and `otherModsVerified=true`; the installed DLL hash matches the release manifest. No merge, push, or publication occurred.

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
