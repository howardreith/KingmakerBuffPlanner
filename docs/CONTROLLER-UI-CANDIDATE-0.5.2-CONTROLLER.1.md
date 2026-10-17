# Local execution candidate receipt — 0.5.2-controller.1

October 17, 2026. **Implemented with named qualification gaps**. This is the
preserved execution-first candidate for source/human review; it has not run in
Kingmaker. Cold-controller startup and physical-controller execution were not
demonstrated. Full controller editing is deferred. No push, remote PR, merge,
tag, publication or permanent installation occurred.

## Exact identities

| Identity | Value |
| --- | --- |
| Branch | `codex/kingmaker-buff-planner-controller-ui` |
| Preserved mission base | `2194d0f0f597ebe8b8423f91f8e1fb6b1f65fdb7` |
| Native entry/execution source checkpoint | `afa108f1fd2cc76f49f466c62bd1572f31663d15` |
| Frozen product/compiled observer source | `d04c1a781770194966fa55df15000d627e14662d` |
| Candidate / numeric CLR version | `0.5.2-controller.1` / `0.5.2.0` |
| DLL SHA-256 | `4b5d2b0c5fbbdfd2d898e359b4d6da780c859a62358cbf6306e967c9c217e19f` |
| DLL MVID | `72d854ad-48ce-4d2d-8140-7769f2ba7ec5` |
| ZIP SHA-256 | `50c85871a64039702786fca12586cd49a643b0ad3908f565ef4f03278b882bd5` |
| Focused test EXE SHA-256 | `a15e5708abcbc26dbbea9581d77e3c572b50a73c162d5e1a914946cc623cb386` |
| Documentation provenance | Later documentation-only commit adding this receipt; distinct from frozen product HEAD. Resolve with `git log -1 -- docs/CONTROLLER-UI-CANDIDATE-0.5.2-CONTROLLER.1.md`. |

The final source-tested ZIP is:

```text
C:\Dev\KingmakerBuffPlannerLab\repo\KingmakerBuffPlanner\artifacts\release\0.5.2-controller.1\KingmakerBuffPlanner-0.5.2-controller.1.zip
```

Equivalent harness-layout artifact (identical ZIP bytes):

```text
artifacts/local-runtime/0.5.2-controller.1/KingmakerBuffPlanner-0.5.2-controller.1-local-runtime.zip
```

Manifests: `artifacts/release/0.5.2-controller.1/release-manifest.json` and the
local-runtime ZIP's `.build-local.json` sidecar. The four-entry package allowlist
is unchanged; no profiles, game/Rewired/UniRx/Harmony DLLs, game art or saves ship.
Stable release/archive paths were not touched. The product commits remain
available in local Git. There is no optional editor follow-on in this candidate.

Read-only origin/main inspection returned
`dcf718fd7b70cf453dd25aa693fefc6aa503d826`. This branch deliberately preserves
the existing 0.5.2 mission ancestry instead of replacing it with the branding
snapshot. No claim of new stable release/version promotion is made.

## Validation on the frozen product

```powershell
.\scripts\Test-Development.ps1 -Only 'controller-,execution-policy-,production-apply-end-to-end-through-host,host-stops-between-castings,host-player-stop-,autosave-,failed-flush-,casting-workspace-campaign-bound-,native-window-takes-,planner-hotkey-,input-lease-,classic-file-and-hud-,refusal-feedback-,casting-world-clock-,first-open-import-,hud-routine-buttons-,planner-escape-'
.\scripts\Build-Release.ps1
```

| Check | Exact outcome |
| --- | --- |
| Registered prefixes | All 17 matched; 13 controller tests plus 25 affected neighbors. |
| Frozen Test-Development | Source 43 PASS / 0 FAIL; production build 1/0; test compilation successful; behavior 38/0; 6.2 s reported, 6.4462946 s child wall time. |
| Build-Release | Two Build-Local passes, each source 43/0, production build 1/0, package validation 4/0, local package creation 1/0. Both DLL/ZIP hashes identical. Final package validation 4/0; release-build assertions 3/0, deterministic=2. |
| Packaging duration | 8.294 s supervised stopwatch; 8.4338511 s child wall time. |
| Documentation before freeze | Relevant link checks 15/0; inspector PowerShell parser 1/0; `git diff --check` clean. |
| Final documentation/identity checks | Relevant links 19/0; receipt fields plus unchanged ZIP/DLL identity 6/0; `git diff --check` clean. No product rebuild after documentation changes. |
| Entry/source checkpoint | Final controller selection 13/0, 7.2 s reported. Earlier reviewed affected-neighbor pass 38/0, 6.5 s. These retain their earlier identities. |

Logs under `artifacts/controller-ui/`:

- `development-frozen-product.log`: the exact frozen 38-test/build pass.
- `package-frozen-product.log` and `package-duration.txt`: deterministic package route.
- `prefix-selection.json`: every selected prefix's actual registered count.
- `development-entry-final.log`, `development-checkpoint-a.log`,
  `development-reviewed.log`, `development-focused.log`: earlier development
  receipts, not relabeled as frozen-product validation.
- `development-initial.log`: retained initial test compilation failure;
  intermediate newly introduced compiler errors were corrected. They were not
  baseline failures or runtime verdicts. No existing validator/assertion was
  weakened and no baseline failure was converted to PASS.
- `assembly-contracts.private.txt`: ignored local native IL inspection, not
  redistributable source or runtime evidence.
- `runtime-intake.json`: read-only environment/transaction audit.
- `documentation-final.log`: final link and receipt/artifact identity checks.

The ordinary development tier and the explicitly selected local deterministic
packaging boundary passed. Full-source/live-purity/layout/release-publication
campaigns were not run. None of the source/model/assembly results establishes
actual input mapping, rendered pixels or live effect/resource outcomes.

## Environment, restoration and next action

DATA; Windows PowerShell 5.1; configured C# 7.3/.NET Framework 4.7 build;
Kingmaker 2.1.7b local references; legacy Harmony12. Assembly-CSharp SHA-256
`3b6450ffec440e296e586f71c711b195aed144b28d53e1cbb29406d18fef5afb`,
MVID `07fa1e4d-8618-41b3-9b8d-faa17d3b26f7`. Tests use real compiler,
session/authoring/persistence/admission/projection/host/executor services with
owned temporary profile storage and scripted game/device boundaries. Native
fixtures were not selected or loaded; there is no live fixture version/result
for this candidate. Harness source is the frozen product checkout; its existing
mouse scenarios were neither extended nor relabeled as controller qualification.

The owner connected a USB controller. Windows PnP reported both Xbox 360
Controller for Windows and its HID game controller as OK. No virtual input,
driver installation, input-mode/settings change or game interaction was used.
Device recognition by Windows is the only demonstrated hardware level.

An active foreign game/lease was observed, briefly released and observed active
again. The 16:48:33Z intake recorded foreign game PID 29980/lease present, all
562 Buff Planner transactions Restored and no unresolved project transaction.
The foreign session was left untouched. This mission staged no package, created
no runtime transaction/allowance, and changed no live Mods, UserSettings, registry
or save. Restoration outcome: **NOT_RUN**; an untouched installation is not a
new live restoration PASS.

Review [implementation](CONTROLLER-UI-IMPLEMENTATION.md) and the explicit
[E01–E13 matrix/procedure](CONTROLLER-UI-ACCEPTANCE.md). The remaining work is a
controller-specific guarded observer/allowance extension and an exclusive,
separately budgeted live qualification session covering fresh native startup,
physical input, native buffs/resources, lifecycle isolation and two-size visual
review with exact transactional restoration. Existing manual/physical scenarios
cannot substitute. The existing full-history purity cost previously exceeded
ordinary validation limits; no receipt is reused and no old campaign is selected.

The smallest owner-dependent next action is source/candidate review and selection
of that exclusive qualification window/budget once the guarded protocol is
prepared. The owner's hardware/session offer persists; no repeated hardware
permission is requested. Before later live use, work from the frozen product
identity and its bound artifacts/manifests. Later documentation HEAD must not be
rebuilt over or silently substituted for these bytes. Full editor work remains
deferred; stable releases and the owner's normal installation remain intact.
