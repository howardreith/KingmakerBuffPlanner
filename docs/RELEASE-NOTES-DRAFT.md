# Kingmaker Buff Planner 0.0.11

Version 0.0.11 fixes a severe closed-window performance regression in Pathfinder: Kingmaker Enhanced Plus Edition 2.1.7b.

## Performance repair

Earlier builds attempted to discover the campaign HUD on every Unity Mod Manager update. When no campaign HUD existed, that path called Unity's global `FindObjectOfType<IngameMenuController>()` once per frame. Exact-build profiling measured 18.875 seconds inside 228 searches during a 20.080-second opening-camera interval and only 11.358 average FPS.

HUD discovery is now invalidation-driven. KBP observes the known `StaticCanvas.HUDController` identity and active state, reacts to real area lifecycle signals, and performs one bounded lookup below that HUD host only when invalidated. Unchanged frames with no HUD perform no hierarchy discovery.

The same-DLL diagnostic A/B increased the opening-camera average from 11.358 to 89.234 FPS when only the pathological discovery path was disabled. Fixed exact-package runs hold moving-camera samples near the configured 90 FPS cap with zero global HUD searches. The repository owner also confirmed through human runtime testing that the severe approximately 16 FPS cutscene and world-map regression is gone.

## Test workflow reliability

The source-only protocol tests no longer create disposable fixtures in the guarded live runtime-evidence tree. They use a unique temporary test boundary while separately proving that the production evidence root remains enforced. Infrastructure faults are reported on stderr with a nonzero exit instead of escaping the test entry point as an unhandled CLR exception.

The direct test executable and complete source/build/release workflow now finish with exit code 0, no residual test process, and no new Windows `.NET Runtime`, `Application Error`, Windows Error Reporting, or `Application Popup` crash event.

## Preserved functionality

This release retains the complete owner-accepted 0.0.10 feature set, including:

- structural native and optional-mod buff discovery;
- Long, Important, and Short routines;
- automatic provider and resource selection;
- Animated and Instant execution;
- material-component and resource accounting;
- metamagic rod and cast-enhancement selection;
- full spell details on right-click and personal-spell target eligibility;
- the four-column planner, target-state display, HUD controls, hotkey, input isolation, and external profile persistence;
- fail-soft Call of the Wild compatibility with no compile-time gameplay-mod dependency.

## Installation

1. Download `KingmakerBuffPlanner-0.0.11.zip` from **Assets** below.
2. In Unity Mod Manager, select Pathfinder: Kingmaker.
3. Drag the ZIP into the **Mods** tab.
4. Launch the game and enable **Kingmaker Buff Planner**.
5. Load a campaign and open the planner with Ctrl+Shift+B or the lower-left HUD controls.

Do not download GitHub's automatically generated **Source code** archives; they are not the Unity Mod Manager package.

## Qualification

The release publisher rebuilds from the fully pushed default branch, runs the complete source-only suite, performs two deterministic clean release builds, validates the strict package allowlist, and publishes the exact ZIP together with `SHA256SUMS.txt`.

The current repository is private, so the release is visible only to GitHub users authorized for this repository unless repository visibility changes.

## Release policy

The existing 0.0.10 tag, release, and assets remain immutable. This 0.0.11 artifact receives its own tag and release; any later code or presentation change must advance the version instead of replacing published bytes.
