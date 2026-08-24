# 0.0.12 HUD Lifecycle Human Acceptance

Date: 2026-08-24

Owner verdict: `This fix is acceptable.`

Publication authorization: merge the accepted repair to the default branch and
publish a new incremented release. The existing public version is 0.0.11, so the
prepared and authorized new version is 0.0.12. Version 0.0.11 must not be
overwritten.

## Installed identity

- Installation transaction: `hud-lifecycle-0.0.12-human-test-install-20260824`
- Artifact source: `083dfbfcf651d44bb01b302ccbbabac823e236e9`
- Package SHA-256: `eabb4785be75129cbc6cffcab030afc9e4afac32957fb7b82af2c16b0e0ac72a`
- DLL SHA-256: `6db3693e0ba38b3672bd0eac36b06df6e5965de29a3e78f9b97513c6accfe9e1`
- MVID: `920b3246-e7f7-4818-92f4-e54294ef2db0`
- Install result SHA-256: `e1b42a8287718a2ee8aba625e6155cf9ba404e56efcf109bb16abf4798595f84`
- Install guards: `otherModsVerified=true`, `settingsPreserved=true`, failure null

## Accepted campaign log

Source log: `%USERPROFILE%\AppData\LocalLow\Owlcat Games\Pathfinder Kingmaker\output_log.txt`

- Last write: `2026-08-24T14:43:01.9133622-04:00`
- Length: 595,152 bytes
- Full-log SHA-256: `6fec850ea76a8cf43983b20cf58a50cf84b98ef1b064181d052fadf54b055548`

Relevant `[KBP-BOOT]` sequence, preserving original log line numbers:

```text
59  Main.Load entered; assembly=KingmakerBuffPlanner, Version=0.0.12.0
65  Main.Load exited; version=0.0.12; commit=083dfbfc...; result=true
3343 HUD installation suspended; reason=OnAreaBeginUnloading; state=Suspended
3359 active HUD detected; current=1036920; active=True; suspended=True
3373 HUD installation dispatch requested; reason=OnAreaScenesLoaded; request=1
3379 HUD install attempted; attempt=1; candidateCreate=1; candidate=-485022
3380 scoped HUD attempt result; result=CandidateCreated; state=CandidatePending
3384 HUD install failed; reason=button-raycast-not-owned; top=Canvas/LoadingScreen/Window/Background; retryable=true
3385 HUD installation dispatch requested; reason=OnAreaLoadingComplete; request=2
3388 scoped HUD attempt result; result=CandidatePending; candidate=-485022
3390 HUD candidate installed; attempt=2; candidateCreate=1; candidate=-485022; buttons=4; listeners=4; active=True
3391 HUD candidate transition; result=Installed; state=Installed; validationFrames=57
3398 full-screen presentation phase A; valid=True; coverage=1.0000; ownsCenter=True
3400 full-screen presentation phase B; valid=True; coverage=1.0000; ownsCenter=True
3402 full-screen install succeeded; inputLease=true; active=True
```

This is the intended repaired lifecycle: unloading remains suspended, one owned
candidate is created after a load signal, the loading screen temporarily owns
the raycast, the same candidate remains pending rather than being recreated,
and it becomes an installed four-button/four-listener row when the HUD settles.
Setup then constructs and validates the full-screen planner.

The log contains no Kingmaker Buff Planner exception. A nearby
`NullReferenceException` originates from Owlcat
`Kingmaker.UI.BugReportCanvas.OnEnable`; it precedes Buff Planner's first update
and is not attributed to this mod.

The authorized save-pair automation remains unavailable at baseline=0 and
working=0, so it did not independently repeat every campaign row. Deterministic
tests retain exact candidate-expiry/re-arm, stale-anchor, bounded-retry, and
steady-state performance coverage. The owner's accepted installed-campaign
verdict is the human release gate.

Completed result: the hotfix was merged and published from
`a48bfae2185a50f1c50d9151666e0b5ce0a0bc3e` as the new `v0.0.12` release.
Version 0.0.11 remains a separate intact release. See
`planning/HUD-LIFECYCLE-0.0.12-RELEASE.md`.
