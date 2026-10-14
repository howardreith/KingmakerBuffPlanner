# Human-feedback refinement 0.5.1-rc5 decision receipt

## Decision

`0.5.1-rc5` is the validated local successor to rc4. It preserves all prior
human-feedback corrections and closes the two preparation findings from the
latest review:

- the native planning observer now performs its meaningful reorder only after
  independent castings exist, explicitly focuses the intended casting, proves
  the real `CanMoveLater` precondition, and undoes the change; and
- `windowed-1920x1200` is an explicit guarded display request whose Unity
  dimensions, native record, PNG dimensions, game settings and unchanged
  physical-display state are correlated and restored or failed closed.

The source, focused regressions, package and freeze pass. The selected live
milestone did **not** pass its first prerequisite. During the first five-target
purity inventory, DATA's physical display changed from the supervised
preflight's 1920x1200 to 1920x1080. The first selected WhatIf call therefore
refused explicit-size admission before deployment or launch. Per the owner's
no-retry instruction, the campaign stopped. There is no rc5 runtime result,
screenshot, timing observation or policy-cast evidence to promote.

No merge, tag, release, upload, permanent installation or protected-save
change occurred.

## Exact candidate identities

| Item | Identity |
| --- | --- |
| Branch | `codex/kingmaker-buff-planner-human-feedback` |
| Reviewed starting feature HEAD | `5699babbdb533de2902d4911a3f1fc322ed4cff0` |
| rc5 product/observer/runtime-harness source | `ec3d5f865707e540b00fccabd3f1a98f04aa3095` |
| Version | `0.5.1-rc5` (CLR assembly/file version `0.5.1.0`) |
| ZIP SHA-256 | `63c12d519dd49d514f6ddd31ea0514b9a8af5644680aa1dde82ddc77e63bfb03` |
| DLL SHA-256 | `8bbf4f1a515cdbd186f8cdd515250fd12c864d2f643a38945f84d786b61e9f92` |
| DLL MVID | `95f36b3c-7e36-4005-9880-898841e1fd72` |
| Build-manifest SHA-256 | `f190a8cac63438520a49b7672b40f8c9ac9155ed5b8c77e66df9934270d7a7f5` |
| Freeze-record SHA-256 | `6db2544b9694a111359a6f9be062c7d9b05f89a26616fd73c2cacf59f2fa1ee2` |

Installable ZIP:

```text
C:\Dev\KingmakerBuffPlannerLab\repo\KingmakerBuffPlanner\artifacts\local-runtime\0.5.1-rc5\KingmakerBuffPlanner-0.5.1-rc5-local-runtime.zip
```

Exclusive qualification freeze:

```text
C:\Dev\KingmakerBuffPlannerLab\runtime-backups\qualification-frozen\ec3d5f865707e540b00fccabd3f1a98f04aa3095\FREEZE.json
```

The freezer verified the original path-bound build receipt, copied the archive
and receipt to a new exclusive directory, independently extracted the packaged
DLL, and rechecked its SHA-256 and MVID. rc1 through rc4 and the retained rc3
failure remain unchanged. The product commit was guarded-pushed before the live
attempt; the final documentation commit is deliberately separate from the
frozen product identity. Any future runtime attempt must use a clean checkout
whose `HEAD` is exactly `ec3d5f865707e540b00fccabd3f1a98f04aa3095`, because
the original build receipt is path-bound and the launcher requires its commit
to equal checkout `HEAD`. The later documentation-only branch HEAD is not a
runtime input and does not require a rebuild.

## Corrected preparation behavior

### Complete native planning sequence

The live observer no longer tries `Cast later` while the seeded casting is the
only casting. It first performs two successive ordinary recipient assignments,
then explicitly selects the seeded casting, reads the real graph inspector's
`CanMoveLater`, reorders exactly that casting, and undoes the change. The
sequence still proves:

- source selection configures the next assignment without changing earlier
  castings;
- adding inspects but does not arm an implicit edit;
- successive ordinary recipient clicks create independent castings;
- explicit retarget and provider edits are one-shot;
- explicit and ordinary removal both work; and
- Undo and the final authored-plan signature restore the initial intent.

The shared regression executes the sequence against both the minimum real
shape (three recipients and one seeded casting) and the larger supported shape
(four recipients and two seeded castings), checking intermediate counts,
eligibility, ordering, preserved assignments and final signature. Native
evidence schema 8 requires `focus-reorder:ok`; older records retain their
original schema meaning.

### Explicit 1920x1200 transaction

The existing launcher now accepts `windowed-1920x1200` as a distinct windowed
mode. It does not change the Windows display mode. While holding the runtime
lock it snapshots the relevant physical geometry and all game-registry values,
sets only the guarded game window values, binds `expectedScreen=1920x1200`, and
then correlates:

- the Unity `screen-size` assertion;
- `native-ui.json` width and height; and
- every named full-screen PNG's IHDR dimensions.

After success or failure it restores and byte-compares the game settings and
verifies the physical display still matches the lock-time snapshot. A changed
physical display is treated as a potentially newer user change: the harness
does not overwrite it, leaves a durable pending record, and blocks later work.
The restore route verifies display state before releasing the Mods
transaction. The frame/PNG correlation is size and expected-state evidence,
not a claim of pixel correctness.

## Focused validation

| Check | Result |
| --- | --- |
| `Test-DisplayTransaction.ps1` | PASS 10, FAIL 0 |
| `Test-RuntimeLauncherArguments.ps1` | PASS 28, FAIL 0 |
| `Test-RuntimeLauncherFileArguments.ps1` | PASS 8, FAIL 0 |
| `Test-CommonHashing.ps1` | PASS 8, FAIL 0 |
| selected `Test-Development.ps1` human-feedback/native/direct-manipulation checks | source 43/0, build 1/0, behavior 32/0 |
| `Build-Local.ps1` | source 43/0, build 1/0, package 4/0, creation 1/0 |
| qualification-freeze package validation | PASS 4, FAIL 0 |
| guarded-push helper test | PASS 6, FAIL 0 |
| PowerShell parse / `git diff --check` | 9 parsed / clean |

The broad `Test-RuntimeLauncherFileWhatIf.ps1`, `Test-SourceOnly.ps1`, publisher
and unrelated aggregates were not run. Their inexpensive launcher checks were
run through the direct bounded suites as instructed. No candidate rebuild was
performed after the one coherent compiled change/package freeze.

The representative offline 67-casting calculation probe reported:

| Calculation-only boundary | Result |
| --- | ---: |
| cold calculation | 3.762 ms |
| warm median / slowest | 1.237 / 2.742 ms |
| buff-switch median / slowest | 1.064 / 1.826 ms |
| recipient-toggle median / slowest | 42.356 / 51.576 ms |

These are calculation/authoring timings, not input-to-correct-rendered-frame
latency. They do not rule out the owner's one-to-two-second delay in a real
67-casting campaign.

## Selected live milestone: retained failure

The one owned operation was:

```text
kbphf-rc5-20261014-01
```

It ran from `2026-10-14T12:44:54.0940617Z` through
`2026-10-14T13:47:50.5161990Z` and exited numerically 1. Its exact supervisor
source SHA-256 is
`bd67e34ed51c3c3bbc4496dcf4b31dcba92f381da00e8d6cdaf462a13b1d08b7`.
The preflight revalidated the frozen package, a clean exact checkout, Steam
offline state, absent runtime/save blockers, and the retained Advanced
inspection:

| Advanced prerequisite | Observed identity |
| --- | --- |
| inspection RunId | `advanced-inspect-20260924-01` |
| game ID | `cb1f405d-0b0c-4281-8b76-3b4f3b690abb` |
| WORKING SHA-256 | `9787613753f38abf08dceeac8f7a66a6b0b1a5cb28ee0a7cd8805f0f265f1e7d` |
| profile | `advanced-gunslinger-0136` |
| compatibility identity | `088553867688dd3c09a5c5ad593d551682ed0977a5ed5db3f0163d6305fc02b7` |

At `2026-10-14T12:45:36.3595190Z`, the durable preflight measured one
1920x1200 primary display. The first five-target purity helper then completed
its expensive before-snapshot and invoked its first selected action with the
required `-Attempts 1 -LeaseWaitSeconds 120`. Source validation passed 43/0
and package validation passed 4/0. The launcher re-read the display and found
1920x1080, so it refused:

```text
DisplayMode windowed-1920x1200 is unsupported on this session's display
(1920x1080); nothing was changed.
```

This is an environment/display change during the prerequisite, not a product
runtime result. It is also not a purity PASS: because the action threw, the
purity helper did not reach its post-action snapshot. The operation was not
retried and the remaining WhatIf calls and every game run were skipped.

No proposed runtime RunId has evidence, state, staging or backup content. No
allowance was created. There are therefore:

- no actual rc5 game RunIds;
- no actual rc5 rendered resolution;
- no rc5 screenshots;
- no rc5 opening, Resources, buff-switch or recipient-toggle rendered latency;
- no rc5 strict/permissive execution observation; and
- no restoration claim to make, because no runtime transaction was entered.

The owner-context post-failure audit measured the current display as 1920x1080,
found no Kingmaker process, foreign lease, unresolved transaction or
unacknowledged save violation, and found no proposed RunId path. The game
registry remained its pre-existing 1920x1080 fullscreen values. The harness
did not overwrite the newer physical display state.

Durable evidence root:

```text
C:\Dev\KingmakerBuffPlannerLab\handoff\runtime-supervision\kbphf-rc5-20261014-01
```

| Evidence | SHA-256 |
| --- | --- |
| preflight | `6598828726a4ea31e6e607a6bde0c69df4cd468342259e696d154b82b3f2de96` |
| supervisor progress/failure phase | `2bff5de101958336e1161af59d05692f0f069c291dbe4abb77eec12e10da3561` |
| numeric result | `5b107665e748f54bbbb03ef50f1fdfb7a364d4de81a4856d229b3f90da540c6f` |
| transcript | `c237e0d39c156413c74598ffd80c0617be3e11d3edb82cca2b2c18defa4fa24b` |
| exact supervisor source | `bd67e34ed51c3c3bbc4496dcf4b31dcba92f381da00e8d6cdaf462a13b1d08b7` |
| separate post-failure audit | `e25684f30981adeab42f22aec5b7b731e181581437ea854e971f6427d5bba8ab` |

## UX-01 through UX-06 status

| ID | rc5 status | Remaining limitation |
| --- | --- | --- |
| UX-01 Resources | Source/focused PASS: native-layer cells, opaque full-dialog backing, scoped tables, reopen/input contracts preserved. | rc5 actual cells, opacity, clipping, scrolling and focus at both sizes are **NOT_RUN**. |
| UX-02 responsiveness | Open-request, targeted Resources and correlated frame-end/expected-state instrumentation preserved; 67-casting calculation numbers above are offline. | No rc5 input-to-correct-rendered-frame result; the real 67-casting UI delay remains unbounded. |
| UX-03 assignment | Complete minimum/larger sequence regressions PASS, including independent adds, explicit one-shot edits, reorder/Undo and restoration. | Actual in-game actions and hints are **NOT_RUN** on rc5. |
| UX-04 draft choices | Same-caster/source cleanup and unchanged-context batching regressions remain passing. | Actual in-game reset/preservation observation is **NOT_RUN** on rc5. |
| UX-05 policy | Planning/execution/reader checks preserve strict and ReadyCastsOnly semantics, shared costs, order, omissions and resolver integration. | No rc5 native dispatch occurred; neither permissive execution nor physical HUD input is claimed. |
| UX-06 labels | Ordinary `Run Long`, `Run Important`, `Run Short` casing and native skin are preserved in source checks. | Actual two-size rendering is **NOT_RUN** on rc5. |

The supplied policy postcheck remains an injected combat-active validation, not
an observed real combat transition. The HUD coverage is shared-resolver
integration, not a physical HUD press.

## Exact remaining batch and cost

A fresh batch must not reuse `kbphf-rc5-20261014-01` or call its failed purity
window a receipt. Before another authorization, the owner must return the
physical primary display to 1920x1200 and keep it stable through the first
purity window, first game run, restoration, and screenshot review. The harness
must re-read that state; it must not change the physical display itself.

With fresh RunIds and an absent operation root:

1. enter a clean, owned runtime checkout at exact product/harness commit
   `ec3d5f865707e540b00fccabd3f1a98f04aa3095`, revalidate the original
   path-bound rc5 receipt and freeze, then revalidate Steam/offline,
   transaction/save guards, 1920x1200 physical state, and the existing Advanced
   inspection binding; restore the feature branch after the owned operation;
2. run one five-target purity window with `-Attempts 1` and
   `-LeaseWaitSeconds 120` around exactly these three WhatIf calls:
   `live-workspace-native-ui` at `windowed-1920x1200`, the same scenario at
   `windowed-1280x720`, and Advanced `live-cast-qual-select` with
   `policy-shared-powerful`/instant;
3. run the 1920x1200 UI scenario and stop for the mandatory machine,
   restoration and original-resolution visual checkpoint;
4. only if it passes, run the 1280x720 UI scenario and Advanced policy
   selection serially, checking restoration after each;
5. mechanically create the run-bound qualification allowance from that fresh
   selection and the rc5 qualification freeze;
6. run the second five-target purity window, again with `-Attempts 1` and
   `-LeaseWaitSeconds 120`, around the exact allowance-bound policy-cast
   WhatIf; then run its actual cast once; and
7. review raw policy evidence, screenshots, correlated timings and every
   restoration record.

The full remaining batch is still approximately **7 hours 30 minutes**: both
complete five-target windows, four serial game runs, restoration, and visual/
evidence review. The failed 62-minute-56-second attempt reduces none of that
cost because it produced neither a completed first window nor a reusable
receipt. The currently matching Advanced inspection avoids a separate
inspection run; if its binding changes, the established guard must refuse and
the prescribed replacement inspection plus its prerequisite becomes a newly
costed addition.

The smallest current human action is therefore one environment decision:
restore and hold the owner-supported physical 1920x1200 mode, then explicitly
authorize one fresh no-retry batch. After that batch, the remaining subjective
review is the two original-resolution Resources/planner image sets; all policy
execution claims must come from the machine-readable cast evidence rather than
a caption or screenshot.
