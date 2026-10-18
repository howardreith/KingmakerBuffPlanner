# Controller Alpha 3 candidate and review corrections

Owner requested fixing the two PR #10 findings, updating the draft PR, and
publishing a new test alpha. This authorizes a non-Latest prerelease, not a main
merge, permanent installation, unrelated mod change or native qualification claim.

## Frozen identities

- Branch: `codex/kingmaker-buff-planner-controller-ui`; preserved intake/review
  HEAD `2144f2c470fc34084c016c79f9558811c6abce69`.
- Product: `7a7faec9a0824ff192271d12d1a9cde4199452cf`.
- Version / CLR: `0.5.4-controller.3` / `0.5.4.0`.
- ZIP SHA-256: `730025732759e5a43ce3f09643cdec604f948fcd58b8910cde89713ca038ad41`.
- DLL SHA-256: `0f0ffb2562eecfea5ce1ae5ea4abab5c2dcc254029bf8a083644abdfd32a1f98`.
- DLL MVID: `ec7f6d2c-8fe1-45f9-8752-9396b70fad56`.
- Clean detached worktree: `private/worktrees/KBP-CONTROLLER-ALPHA03`.
- ZIP: that worktree's
  `artifacts/release/0.5.4-controller.3/KingmakerBuffPlanner-0.5.4-controller.3.zip`;
  exact build/package identity is in adjacent `release-manifest.json`.
- Documentation HEAD is separate; never rebuild it over this frozen artifact.
  Original .1 public and .2 local artifacts remain unchanged.

## Corrections and limits

The .2 receipt overstated its pre-request Escape coverage: its test recorded a
focused field first. .3 observes the installed TMP cancellation-before-deactivate
contract on the exact owned editor root, preserving that context even when focus
or selection is already cleared at the common production request call. Cancellation
after Request is also retained. Native and keyboard requests still share one
neutral-edge latch; the second deliberate Back is not consumed by stale state.
The Harmony12 prefix only observes text, never suppresses TMP behavior, is inert
outside the owned editor, and is removed with the exact controller patch owner.

Results scroll distance is now 75% of the actual viewport height, normalized
over content minus viewport height, with overlap independent of report length.
Short/empty/equal-height reports avoid division by zero; inertia is stopped before
the discrete movement. No execution/domain/persistence logic was duplicated.

Behavior regressions execute the shared production Back request/completion and
scroll functions with substituted native field/device/window boundaries, not
just a source string. They cover cancellation before/after request, duplicate
dispatch, lost selection, no nested surface (otherwise erroneous editor Close),
second press and complete line visibility both ways through 100,000-unit reports.
Native TMP/production adapter wiring is assembly-inspected separately. Neither
substituted fields nor rect math establishes an actual native UI interaction.

## Frozen focused evidence

In the clean frozen worktree:

```powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass -File scripts/Test-Development.ps1 -Only 'controller-,execution-policy-,production-apply-end-to-end-through-host,host-stops-between-castings,host-player-stop-,autosave-,failed-flush-,casting-workspace-campaign-bound-,native-window-takes-,planner-hotkey-,input-lease-,classic-file-and-hud-,refusal-feedback-,casting-world-clock-,first-open-import-,hud-routine-buttons-,planner-escape-,footer-save-status,focused-casting-routine-order-and-recast,production-execution-wiring-and-session-lock,spellbook-entry-,native-text-material-,native-source-tabs-'
powershell.exe -NoProfile -ExecutionPolicy Bypass -File scripts/Build-Release.ps1
```

**62 PASS / 0 FAIL**, 6.9 s reported. Source validation 47/0, production build
1/0, test compile successful. Package validator 6/0; two deterministic builds
with identical DLL/ZIP; release assertions 3/0. Combined stopwatch **14.924 s**.
All prefixes registered. Logs in frozen worktree:
`artifacts/controller-alpha3/frozen-development.log` and `frozen-package.log`.
Earlier affected-neighbor pass 38/0 (6.0 s) is separate, not an extra 38 tests
added to the frozen total; log `artifacts/controller-access/review-fixes-development.log`
in the normal checkout. Exact installed TMP IL remains private/ignored in
`artifacts/controller-access/review-tmp-cancel.private.txt`.

No full compatibility/source/purity/gameplay campaign selected. Current native
[A01-A08 gates](CONTROLLER-UI-ACCEPTANCE.md) remain NOT_RUN. No runtime staging,
game launch, installation/settings/profile/save changes or mission-owned runtime
transaction: restoration **NOT_RUN**, not PASS. Working KMG/CotW repairs untouched.

## Alpha publication and next check

Selected route is the established artifact-only alpha workflow: guarded
fast-forward branch push, draft prerelease upload of these unchanged bytes and
checksum, download/hash verification, then publish with prerelease=true and
latest=false. No historical full publisher/rebuild campaign and no weakened
validator. Exact publication result is recorded after remote verification.

First owner check is **no casting**: cold controller -> menu/shortcut -> readable
initial/settled quick menu -> Configure -> real pointer/click/scroll/text -> one
casting/autosave -> Close -> correct counts/save status -> Back/gameplay. Include
text Escape with no nested layer, long results, native takeover, 1920x1200 and
a compact supported configuration. Only then test finite-resource routines/Stop.
See [alpha instructions](CONTROLLER-ALPHA-0.5.4-CONTROLLER.3-NOTES.md). Full
gamepad graph editing, native fan card and original-book reconstruction stay deferred.
