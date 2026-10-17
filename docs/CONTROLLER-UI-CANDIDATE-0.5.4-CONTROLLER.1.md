# Integrated local controller candidate: 0.5.4-controller.1

Subsequent owner authorization published these exact bytes as
[Controller Alpha 1](https://github.com/howardreith/KingmakerBuffPlanner/releases/tag/v0.5.4-controller.1),
without a rebuild/new tests. [Publication receipt](evidence/controller-alpha-0.5.4-controller.1.md).
The original freeze snapshot below retains its identities and qualification gaps.

October 17, 2026. **Implemented with named qualification gaps.** PR review
corrections and current main integration are complete in this candidate. Native
startup, handoff, physical input, effects/resources and lifecycle remain NOT_RUN.
Full controller editor: deferred. No release/default-branch merge or permanent
installation is authorized or performed.

| Identity | Value |
| --- | --- |
| Branch | `codex/kingmaker-buff-planner-controller-ui` |
| Review corrections | `8e4677ba2bb92969a30037875d3bf041f380ab9c` |
| Integrated frozen product | `9aec44e374b26b3561fa4402912c77a23a4e08ac` |
| Main parent | `dcf718fd7b70cf453dd25aa693fefc6aa503d826` |
| Candidate / CLR version | `0.5.4-controller.1` / `0.5.4.0` |
| ZIP SHA-256 | `6e5c3f1cc067f72d92558fa474b9fe5a0c4929e7b4503d7068e0912b9daa9d59` |
| DLL SHA-256 | `0d4e7895c9235a540f3da9f98f10de75248c3b65e7547ff2538258b2dadc3eeb` |
| DLL MVID | `60d7ee7b-0541-45de-83c4-308311d91331` |
| Documentation | Later documentation-only commit adding this receipt; not the product source identity. |

Local installable ZIP (not staged or uploaded as a release):

```text
C:\Dev\KingmakerBuffPlannerLab\repo\KingmakerBuffPlanner\artifacts\release\0.5.4-controller.1\KingmakerBuffPlanner-0.5.4-controller.1.zip
```

Identical harness-layout ZIP:
`artifacts/local-runtime/0.5.4-controller.1/KingmakerBuffPlanner-0.5.4-controller.1-local-runtime.zip`.
Manifests: release directory `release-manifest.json`, local-runtime ZIP's
`.build-local.json` sidecar. The main-inherited five-entry allowlist includes
the verbatim MIT LICENSE; no game, mod, input-library, save or profile is bundled.
Mr. Bard branding, stable UMM/DLL/folder identity, main's publisher/build-order
fixes and release history are preserved. Public stable release remains 0.5.3.

## Frozen validation

```powershell
.\scripts\Test-Development.ps1 -Only 'controller-,execution-policy-,production-apply-end-to-end-through-host,host-stops-between-castings,host-player-stop-,autosave-,failed-flush-,casting-workspace-campaign-bound-,native-window-takes-,planner-hotkey-,input-lease-,classic-file-and-hud-,refusal-feedback-,casting-world-clock-,first-open-import-,hud-routine-buttons-,planner-escape-,footer-save-status,focused-casting-routine-order-and-recast,production-execution-wiring-and-session-lock'
.\scripts\Build-Release.ps1
```

- All 20 prefixes matched. **45 PASS / 0 FAIL** (17 controller + 28 neighbors),
  source validation **47/0**, production build **1/0**, test build successful;
  6.2 s reported / 6.4519714 s child wall.
- Two deterministic builds: identical ZIP/DLL hashes; each source47/build1/
  package6/creation1 passed. Final package **6/0**, release-build assertions
  **3/0**; 8.0408864 s stopwatch / 8.1444404 s child wall.
- Logs: `artifacts/controller-ui/review-frozen-product.log`, `review-package.log`.
  Failed reproductions/intermediate results remain in the
  [review correction record](CONTROLLER-UI-REVIEW-FIXES.md), not reclassified.

No full-source, live-purity, native, physical or release-publication campaign was
run. Actual native **unbound** collection/callback cleanup is tested; the device
registration boundary and native-stack behavior are not live-qualified. Header
tests cover corrupt/newer/blocked-import/backup recovery without inspection
writes; reconstruction tests cover success -> refusal -> reopen -> later success.

The original frozen product `d04c1a781770194966fa55df15000d627e14662d` /
0.5.2-controller.1 and ZIP SHA-256
`50c85871a64039702786fca12586cd49a643b0ad3908f565ef4f03278b882bd5`
remain unchanged. Its old evidence is not transferred to these bytes. Later
documentation HEAD must not be rebuilt over either frozen artifact.

## Runtime handoff

Environment remains DATA / Windows PowerShell 5.1 / C#7.3 / .NET4.7 / Kingmaker
2.1.7b / legacy Harmony12. Assembly-CSharp MVID
`07fa1e4d-8618-41b3-9b8d-faa17d3b26f7`. Windows previously recognized the
owner's USB Xbox 360 controller; game recognition remains unobserved. See the
[entry/exit-only preparation and remaining protocol/budget](CONTROLLER-ENTRY-EXIT-PLAN.md).
This is the first native check, before any casting; the current harness does not
yet implement that scenario. Existing mouse manual/physical routes cannot be
used as substitutes. No runtime transaction, game launch, settings/profile/save
mutation or staging occurred. Restoration: **NOT_RUN**. E01-E13 live gaps remain
in the [acceptance matrix](CONTROLLER-UI-ACCEPTANCE.md); PR stays draft.

Follow-up read-only environment audit: `review-runtime-intake.json`,
2026-10-17T19:51:18Z, 562 transactions, no unresolved transaction or protected-save
review, no running Kingmaker or foreign lease. This point-in-time check is not
exclusive desktop authorization, Steam preflight or live restoration evidence.
Final documentation checks: local links/conflict markers 74/0, artifact identity
checks 4/0, `git diff --check` clean. No source/script/test/version changes or
artifact rebuild follow the frozen product in the documentation handoff.
