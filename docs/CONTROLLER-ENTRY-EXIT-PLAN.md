# First guarded controller observation: entry and exit only

Status: **BLOCKED**. This is a preparation/design record, not an implemented
launcher scenario, a run command, or native acceptance evidence. The source fixes
and integrated candidate are preserved separately. No casting or full-roster
campaign is selected.

## Smallest protocol extension

Add a distinct no-cast, owner-driven controller-entry scenario through the
existing launcher/request/observer/result chain. Do not reuse
`live-workspace-manual`: `RuntimeTestHost` reaches its manual hold only after
constructing/capturing the mouse workspace. Do not reuse physical mouse/hotkey
automation, call `OpenControllerRoutines` from the observer, or inject commands
after native mapping. Keep `NativeCastingSessionPolicy` locked; this first run
requires **zero** native submissions and no cast allowance.

Before implementation, read the selected launcher and every reached deployment,
Steam, save fixture/profile/settings, observer and restoration child in full.
Current inspection establishes the boundary but does not claim that full live
route audit is complete. The minimal implementation touches:

- `scripts/Invoke-KingmakerRuntimeTest.ps1`: explicit scenario/timeout and owned
  Automation save selection; no synthetic keyboard/mouse delivery. Keep exact
  package, branch/HEAD, Steam, locks, WhatIf, save and restoration checks.
- `RuntimeTestProtocol`: strict scenario/parameters and no submission grant;
  reject unknown fields, wrong fixture/family, allowances and ambiguous markers.
- `RuntimeTestHost`: load only the authorized WORKING fixture, bind the existing
  saved profile without opening either mouse editor, wait for physical operator
  input, and observe state rather than invoking entry/Run callbacks. Use the
  normal deadline/abort and restoration result path. Terminal human markers
  alone cannot produce PASS without corresponding observed state.
- Narrow read-only controller diagnostics: native/menu/owned layer identities,
  focus transitions, entry/open/close counts and the existing host's started-run
  count. Timestamp native `OnUnbind` versus older menu removal and current-layer
  identity, without retaining a second owner or altering dispatch ordering.

Add positive and negative protocol/observer tests before selecting any live use:
correct progression; no-input timeout/abort; missing controller mode; editor
opened; planner lost after native close; wrong/lost ownership; no navigation;
unreturned gameplay input; any native submission; wrong identity or marker.
Harness statuses remain PASS/FAIL/BLOCKED; do not introduce a new machine status.
Compiled observer changes require a new product/DLL/MVID/package identity and
their own focused checks, not a relabeling of the preserved candidate below.

## Physical sequence and required evidence

1. Fresh process starts in native controller mode. Observe Main.Load/update,
   enabled mod, recognized mapped controller, correct campaign and existing
   profile; mouse editor open count must remain zero. Verify the game's actual
   supported startup-mode mechanism before any permitted transactional setting
   changes; no guessed settings key, loader change or global mode switch.
2. Operator uses the controller to open the native campaign card menu and its
   visible Buff Planner entry. Correlate mapped event, timestamps and frame.
3. Closing the older native menu must leave the planner visible and its native
   layer current. Record whether the planner receives `OnUnbind`, when, and
   whether `_yielded` causes closure. Do not turn a transient screenshot into
   proof of stable input ownership.
4. Navigate Long / Important / Short and Stop without confirming Run. Verify
   focus/hints, no gameplay movement/camera/radial action, zero host starts and
   unchanged profile bytes/timestamps after controlled session initialization.
5. Back once, return to neutral, observe the owned layer/hints close and normal
   native gameplay input return. One press must not also open a game menu.
6. End through the guarded route, verify every save, profile/settings and Mods
   restoration assertion and lock release. A failure or incomplete restoration
   blocks further live work. Only a successful entry/exit observation permits
   selecting finite-resource execution/refusal/Stop/held-input/lifecycle tests.

Collect controller-driven event observations, before/after UI frames and world
state together. Human input remains labeled physical only if the operator
actually uses the USB device. Detached native types and source tests do not
establish this sequence.

## Budget and session boundary

The existing required `Test-SourceOnly.ps1` -> deployment WhatIf and
`Test-RuntimeLauncherFileWhatIf.ps1` routes include repeated
`Invoke-KbpLivePurityWindow` snapshots over runtime-state/staging/backups/evidence
and live Mods. `Get-KbpDirectoryManifest` recursively hashes every file. There
is no established narrow purity-reuse receipt for this product or new observer.
Do not run those wrappers as an ordinary quick check or skip their guards.

Historical single five-target purity took 2 h 06 m; a later two-window estimate
was 5 h 10 m. These are historical costs, not measurements of the new candidate.
Even an entry-only session therefore exceeds the ordinary 30-minute ceiling
through its prerequisites. After the protocol is implemented/reviewed, select a
separate budget for the exact required safety/source/purity route plus a bounded
entry/exit run, restoration and human review; do not select the old seven-hour
spell campaign. Confirm exclusive DATA desktop use at that actual live boundary.
The owner's USB-controller offer persists; no repeated hardware permission is
needed. No game, global settings, driver, binding, loader or other mod was changed
while preparing this record.
