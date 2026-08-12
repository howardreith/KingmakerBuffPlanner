# Codex Fresh-Session Mission: Recover the Missing 0.0.3 Live UI Bootstrap

Work in:

```text
C:\Dev\KingmakerBuffPlannerLab\repo\KingmakerBuffPlanner
```

Use **High reasoning**.

This must be treated as a fresh independent audit. Do not defend or assume the correctness of the prior UI implementation, automated UI claims, journal conclusions, or resume file when they conflict with direct human evidence, installed identities, runtime logs, or exact source.

## Human verdict

The locally installed **Kingmaker Buff Planner 0.0.3** is shown by Unity Mod Manager as active with a green status indicator.

In an actual loaded campaign:

- no Buff Planner Setup/Long/Important/Short controls exist anywhere;
- pressing F10 does nothing;
- there is no visible planner;
- there is no invisible modal/input lock either;
- therefore the live campaign received neither the HUD UI nor the fallback hotkey.

This is not a positioning or click-through defect. The entire live UI/hotkey bootstrap is absent or aborting before installation.

Human evidence is read-only under:

```text
C:\Dev\KingmakerBuffPlannerLab\incoming\ui-bootstrap-0.0.3-failure\
```

Expected files:

```text
01-umm-0.0.3-active.png
02-live-campaign-no-kbp-controls.png
```

A diagnostic capture from a clean reproduction should also be present in a timestamped subdirectory created by:

```text
C:\Dev\KingmakerBuffPlannerLab\repo\KingmakerBuffPlanner\scripts\Collect-KbpLiveBootstrapDiagnostics.ps1
```

## Critical methodological correction

The prior handoff stated that:

```text
the campaign UI gate correctly reported BLOCKED because there is no authorized automation save
```

That means the actual campaign UI was never mechanically qualified. Passing detached/source tests and catalog/runtime profiles did not prove the live HUD bootstrap, F10 registration, or full-screen root.

Correct every documentation/matrix claim that conflated:

- mod assembly loaded;
- source fixture constructed;
- catalog scenario passed;
- UI bootstrap installed in an actual campaign.

Do not present another package for human testing until a real campaign UI scenario passes on an explicitly authorized disposable save.

## First actions: read-only forensic intake

Before changing source:

1. Verify:
   - current branch;
   - exact HEAD;
   - remote relationship;
   - clean/dirty status;
   - source version;
   - installed `Info.json`;
   - installed DLL SHA-256 and MVID;
   - release manifest/package SHA-256;
   - no unresolved deployment lock or transaction.
2. Read:
   - `AGENTS.md`;
   - all active mission files;
   - `AUTONOMOUS-RESUME.md`;
   - `AUTONOMOUS-BLOCKERS.md`;
   - `KINGMAKER-BUFF-PLANNER-JOURNAL.md`;
   - UI architecture and implementation reports;
   - the current UI/bootstrap source;
   - runtime scenario catalog and UI tests.
3. Read the clean reproduction logs and exact diagnostic capture.
4. Inspect the installed DLL—not merely source—to confirm the expected UI types/methods exist and match the source commit.
5. Write an evidence table of every bootstrap stage and whether it actually occurred.

Do not modify code until the earliest missing or faulted stage has been identified.

## Primary hypothesis to test

The simultaneous absence of both HUD controls and F10 strongly indicates a **common bootstrap/controller failure**, not a rendering-only defect.

Investigate at least:

```text
Main.Load entry
Load return value
modEntry.Enabled
Main.Enabled/current enabled flag
OnToggle registration and startup invocation semantics in UMM 0.28.2
OnUpdate registration
global F10 polling
Harmony patch application
EventBus subscription
scene/area subscription
UI controller construction
UI controller retention/garbage collection
OnAreaDidLoad/scene callback
late-load into an already active campaign
Game.Instance/UI/Common readiness predicates
HUD host lookup
native hierarchy lookup
feature/settings gates
static singleton state
exception swallowing
disabled callbacks after a failed install
mod disable/unload cleanup
```

A particularly important possibility is that UI/hotkey initialization was moved behind `OnToggle`, an area callback, or a HUD installation success path that is not invoked on initial startup under the installed UMM 0.28.2 lifecycle. Prove or disprove it from exact logs and assembly behavior.

Also inspect whether one exception during HUD installation aborts both:

- retained-mode HUD installation;
- global F10 handler registration.

The fallback hotkey must not depend on successful HUD construction.

## Mandatory instrumentation

Add structured, release-safe diagnostics for the exact live lifecycle, with one stable prefix such as:

```text
[KBP-BOOT]
```

At minimum record:

```text
assembly loaded
mod version
commit/build identity
Main.Load entered/exited
modEntry.Enabled
OnToggle assigned
OnToggle invoked and value
OnUpdate assigned
OnUpdate first tick
Harmony patch count/result
EventBus subscription result
scene/area callback registration
scene/area callback invocation
game mode
UI root readiness
HUD host lookup result
button-row install attempted/succeeded/failed
full-screen root install attempted/succeeded/failed
F10 handler armed
F10 keydown observed
controller instance identity
controller enabled/disposed state
complete exceptions and stack traces
```

Do not swallow bootstrap exceptions behind generic catch blocks. Log full exception type, message, and stack while still failing safely.

Add a bounded diagnostics snapshot callable from the UMM options panel or log command, reporting current UI/bootstrap state without requiring the HUD controls to exist.

## Required bootstrap architecture

Repair the smallest proven root cause. Do not perform another speculative visual redesign.

The live architecture must guarantee:

1. `Main.Load` always installs the minimum safe root lifecycle:
   - release-safe logging;
   - enabled state;
   - global update/hotkey polling;
   - scene/UI lifecycle observer.
2. The F10 fallback is registered independently of HUD button installation.
3. HUD installation is retried safely when the actual campaign HUD becomes ready.
4. Late load, save load, area transition, UI rebuild, mod enable, and startup all converge on one idempotent `EnsureInstalled()` path.
5. A failed HUD lookup/installation:
   - logs the exact reason;
   - does not mark installation successful;
   - does not permanently suppress retries;
   - does not disable F10 diagnostics;
   - does not acquire a modal/input lock.
6. A successful install records exact host hierarchy, instance IDs, button count, listener count, and active state.
7. Exactly one controller remains strongly referenced for the mod lifetime.
8. Disable/unload disposes listeners, subscriptions, and owned objects exactly once.
9. The old and new UI implementations cannot both be active.

F10 behavior while the visible planner cannot be constructed must be safe and explicit:

- do not hide the HUD;
- do not lock gameplay input;
- log/show `Buff Planner UI is unavailable: <exact reason>`;
- trigger a safe reinstall attempt;
- never silently do nothing.

## Disposable campaign save requirement

Do not fabricate another campaign UI pass.

Before final live qualification, require the explicit save pair:

```text
KBP_AUTOMATION_BASELINE
KBP_AUTOMATION_WORKING
```

Rules:

- Both must belong to a deliberately disposable test campaign.
- Baseline is immutable.
- Only Working may be loaded/modified by the guarded harness.
- Verify exact save descriptors and reject ambiguity.
- Never select or write another existing save.

If the pair is not present, complete source/fixture/log repair and stop only at the precise human gate requesting creation of the pair. Do not claim campaign UI qualification.

## Required live campaign scenario

Once the disposable pair exists, add/run a fresh-process guarded scenario that proves in the actual campaign:

```text
KBP assembly identity exact
Main.Load reached
OnUpdate ticks
scene/UI lifecycle callback reached
HUD host found
exactly one Setup button
exactly one Long button
exactly one Important button
exactly one Short button
buttons active in hierarchy
visible bounds non-zero
listeners exactly one
F10 handler armed
F10 keydown observed
visible planner root opens
input lease acquired only after visible validation
close restores input
no duplicate objects after 20 cycles
```

The scenario must fail if only detached objects or synthetic roots were constructed.

Record authoritative live object paths, instance IDs, active states, canvas identities, and world corners.

## Human evidence and package sequence

Use the next repository-consistent version; do not reuse `0.0.3`.

Before guarded local replacement:

1. focused bootstrap tests;
2. complete behavior/protocol tests;
3. transaction/deployment tests;
4. exact-reference Release build;
5. package validation;
6. disposable-save live UI scenario;
7. repeated fresh-process live UI pass;
8. native and Call of the Wild regression profiles materially affected;
9. exact `Mods` restoration;
10. clean HEAD and hashes.

Only then replace the installed 0.0.3 using the guarded installer.

The next human package should be presented with:

```text
source commit
version
package SHA-256
DLL SHA-256
MVID
live campaign run IDs
exact HUD object evidence
exact F10 evidence
```

## Documentation correction

Update:

```text
KINGMAKER-BUFF-PLANNER-JOURNAL.md
AUTONOMOUS-RESUME.md
AUTONOMOUS-BLOCKERS.md
planning/DEFINITION-OF-DONE-MATRIX.md
docs/ARCHITECTURE.md
docs/IMPLEMENTATION-REPORT.md
docs/MANUAL-ACCEPTANCE.md
CHANGELOG.md
```

Mark 0.0.3 as human-failed:

```text
UMM active, but live campaign HUD controls absent and F10 unregistered/nonfunctional.
```

Preserve historical evidence but remove unsupported claims of live UI completion.

## Continuation policy

Work autonomously through diagnosis, implementation, tests, live disposable-save qualification, packaging, and guarded installation.

Do not stop because:

- the prior implementation must be partially reverted;
- one lifecycle theory is wrong;
- another context compaction occurs;
- a test or runtime attempt fails;
- a commit is complete.

Stop only for a true critical boundary:

- no safe/authorized disposable save pair;
- unresolved or unrestorable `Mods` transaction;
- protected-save risk;
- unsupported exact game/UMM environment;
- required credential/dialog;
- licensing barrier;
- irreducible product decision not answered here.

Begin with the diagnostic capture and the exact installed/source bootstrap call graph. Do not begin by moving icons again.
