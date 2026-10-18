# Local access/layout repair candidate — 0.5.4-controller.2

**Source-tested with named native qualification gaps. Not published or installed.**
The owner correction requires real same-session editor access, not merely a
run-only selector. Implementation is preserved, but rendered/mixed-input
acceptance is NOT_RUN. No claim of full controller graph editing.

## Identities

- Branch: `codex/kingmaker-buff-planner-controller-ui`.
- Preserved intake / current remote PR #10: `2a04579a460064d53f4f87d34defb22a96dbc227`.
- Access/layout implementation checkpoint: `b64e86c`.
- Frozen reviewed product: `32f6385206694b7cdb102d1b938f7b2b1acb2d1b`.
- Version / CLR: `0.5.4-controller.2` / `0.5.4.0`.
- ZIP SHA-256: `0a79c3b10dd2f1adfe8ba85888c9099b1885002f5b7c77d10a429f7c743930ef`.
- DLL SHA-256: `fd37060969c1cc4d0fb092b14f410bc6f585feb269a2dd45a38353e9e8bfb15f`.
- DLL MVID: `9eae87dc-ce34-4e79-af0d-da49a3747dcb`.
- Documentation HEAD: later documentation-only commit adding this receipt;
  never rebuild that HEAD over this frozen candidate.

Exact clean detached source/build worktree:
`private/worktrees/KBP-CONTROLLER-ACCESS02-VERIFIED` under the normal repository root.
Installable local ZIP:

```text
C:/Dev/KingmakerBuffPlannerLab/repo/KingmakerBuffPlanner/private/worktrees/KBP-CONTROLLER-ACCESS02-VERIFIED/artifacts/release/0.5.4-controller.2/KingmakerBuffPlanner-0.5.4-controller.2.zip
```

Release manifest is adjacent. Identical harness-layout ZIP/manifest is under
that worktree's `artifacts/local-runtime/0.5.4-controller.2/`. A verified ZIP
copy and this uniquely named receipt are delivered in
`C:/Dev/KingmakerBuffPlannerLab/handoff/`; no installation is performed.
Stable 0.5.3, all prior alphas/receipts/tags and product 9aec44e are untouched.
Package retains the existing five-file standalone allowlist, MIT license,
stable UMM/DLL/folder identity, and contains no game/input/gameplay-mod binaries,
saves, profiles or private inspection evidence.

## Actual available behavior and limits

Source implements Configure (default for empty plans), actual workspace hosted
from the controller scene, scoped existing-EventSystem physical-mouse authoring,
native Back/top-layer behavior, canonical autosave and same-session quick-menu
return. No input-mode switch, copied plan or second executor. The controller-opened
editor's Run instructs returning to routines; it does not submit behind fullscreen.
Three routines and explicit Stop remain shared canonical execution requests.
Physical Escape and native Back share one neutral-edge latch. An input-field
cancel retains its original text-only intent even if TMP handles Escape first;
one press cannot dismiss text and then close a second layer.

Installed TMP default sizing is applied in Awake. Controller labels establish
their final rects afterwards and log bounded diagnostic samples; basic status,
focus and contextual hints are unpaged. View results is a separate scrollable
list. Source tests do NOT prove the reported 1920x1200 clipping is gone.

Console-book footer targets the actual editor. Y/Func02 stays reserved for
native book switching. A conflict-free RightStickButton or FuncAdditional
uses its native glyph; prepared/spontaneous/substate/foreign binding behavior
still requires native checks. Native VM.Back releases the source before editor
opening; timeouts never bypass ownership. Editor Close returns to routines,
not an unverified recreated book/selection. Original-book return and genuine
native fan card are DEFERRED behind the primary authoring/layout qualification.
Full controller-only graph authoring/text entry is DEFERRED.

## Frozen focused validation

Executed in the unchanged frozen worktree:

```powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass -File scripts/Test-Development.ps1 -Only 'controller-,execution-policy-,production-apply-end-to-end-through-host,host-stops-between-castings,host-player-stop-,autosave-,failed-flush-,casting-workspace-campaign-bound-,native-window-takes-,planner-hotkey-,input-lease-,classic-file-and-hud-,refusal-feedback-,casting-world-clock-,first-open-import-,hud-routine-buttons-,planner-escape-,footer-save-status,focused-casting-routine-order-and-recast,production-execution-wiring-and-session-lock,spellbook-entry-,native-text-material-,native-source-tabs-'
powershell.exe -NoProfile -ExecutionPolicy Bypass -File scripts/Build-Release.ps1
```

- **60 PASS / 0 FAIL**, 7.0 s reported; production/source/test build successful.
- Source validation 47/0; production build 1/0 per build.
- Existing package validator 6/0; two deterministic builds, identical ZIP/DLL;
  release-build checks 3/0. Combined validation/packaging stopwatch **15.873 s**.
- Logs in frozen worktree: `artifacts/controller-access/frozen-development.log`
  and `frozen-package.log`. Prior iteration/failure/inspection logs remain in
  the normal checkout's `artifacts/controller-access/`, private/ignored.
- Real owned profile files, authoring, persistence, gate, projection and host
  exercise configure intent -> same-session author/save -> reconstructed
  selector -> finite scripted execution. Device/native/UI boundaries remain
  substituted, so this is not real buff/resource or pointer evidence.
- Added result-focus isolation, native queue identity/foreign preservation,
  handoff neutral/timeout/cancel, native book close/conflict selection and TMP
  initialization/mouse-fallback contract checks, and single-edge mixed-input
  text-only Back behavior. Existing persistence, refusal,
  Stop, session-lock, mouse entry/hotkey/lease and material neighbors retained.
- No full source/purity/live/release-publication campaign. Intermediate compiler
  errors and changed focus expectations are retained in iteration logs; no
  baseline native result was promoted.

### Preserved intermediate results, not selected artifacts

The earlier pre-delivery .2 worktree `KBP-CONTROLLER-ACCESS02`, product
`c75b03082adcfc748b47576d8e477797db145973`, had 59/0 and a deterministic
package (ZIP `e76c23ee036276616dd905a82d22c772e957e4da2d78dd608b912121d08de34e`).
It remains unchanged and was never delivered, published or installed. Review
then found competing native/keyboard Back handling and corrected it at
`63b908234aab6aea682e1b45ee5129722acfb099`. That worktree
`KBP-CONTROLLER-ACCESS02-FINAL` retains its 59/1 result: the new behavior test
passed, but an existing source-wiring assertion required the obsolete one-line
Escape dispatch. Commit `32f6385` aligned that assertion with the single-edge
route, retaining Escape ordering/suppression checks. Narrow follow-up 2/0
(6.7 s, normal checkout `artifacts/controller-access/escape-regression.log`),
then the selected clean-worktree 60/0 and new deterministic package above.
Do not relabel any intermediate bytes/results as the selected product.

## Owner photos inspected (baseline only)

All three privately supplied `ChatGPT Image Oct 17, 2026, ... PM.png` files
in the handoff folder are 1920x1200. They were visually inspected at original
resolution; no new-candidate screenshot or binary identity is present.

| Filename time suffix | Observation | SHA-256 |
| --- | --- | --- |
| `08_02_34` | Quick panel has unused width while title/settings/routine/details text truncate in a narrow left strip; footer overlaps; basic explanation is behind Details 1 / 7. Zero counts do not prove profile absence. | `072cff5d0e5aa60ca50f6b84b829f122feb9eeffadf5a78e64eb89d5b4c7e7e9` |
| `08_02_38` | Native spellbook shows level, Context Menu, Close and Metamagic hints; no planner footer. An absent Y hint does not prove a free action. | `79f5b714a3c18c8385d6dea69e25e44a023738d5448284aab4e6cbb4d7b120c7` |
| `08_02_42` | Native card fan and existing X planner shortcut are visible; no planner card. Image alone does not prove callback/input or execution. | `81672c22301140fc4956e39710e6d2c4bf52524288d44d1c8753801e0025abea` |

The narrow strip is consistent with the installed TMP default-size reset path,
but the photos do not measure rects or distinguish every masking cause. Final
geometry/material diagnostics and rendered checks on the new bytes remain
necessary. Images/game assets stay private and out of Git/packages.

## Native boundary / smallest next action

DATA read-only audit at 2026-10-17T23:50Z found foreign Kingmaker PID 22596 and
active KMG compatibility lease. They were left untouched. Lab-installed KBP
is 0.1.1.0, SHA-256 78407dd4c7240ce25c7654715854e83061da744a18d8eb4a2198fdffec5e3080,
MVID c0cc9cbc-8075-4d98-a32e-29972aafa7fa, not this or the owner's alpha.
Game 2.1.7b MVID remains 07fa1e4d-8618-41b3-9b8d-faa17d3b26f7.

No automated desktop use, launch/staging transaction, game/settings/profile/save
mutation or permanent install occurred. Restoration outcome: NOT_RUN, not PASS.
The previous guarded [controller entry protocol gap](CONTROLLER-ENTRY-EXIT-PLAN.md)
remains; mouse scenarios cannot stand in for cold-controller authoring. A fresh
exclusive guarded no-cast session needs its narrow request/observer route and
existing full safety/transaction prerequisites. First capture empty menu at
1920x1200 -> Configure -> actual mouse click/scroll/type -> autosave -> Close ->
updated counts/Back and native controls/restoration. Then compact rendering;
finite-resource controller execution/refusal/Stop requires a separate allowance.
All new native A01–A08 observations remain NOT_RUN in the
[acceptance record](CONTROLLER-UI-ACCEPTANCE.md).

Do not publish, merge, push or install from this receipt without the appropriate
separate owner authority. Working KMG/CotW loading repairs are not modified or
required by this standalone candidate.
