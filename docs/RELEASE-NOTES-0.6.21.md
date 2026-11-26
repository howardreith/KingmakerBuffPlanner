# Kingmaker Buff Planner 0.6.21

This release moves the mod to the Unity Mod Manager 0.31.1 baseline. Planner behavior is unchanged from 0.6.20.

- **Declared UMM minimum: 0.31.1.** `Info.json` now declares `ManagerVersion` 0.31.1 (0.6.20 and earlier declared 0.28.2). Use Unity Mod Manager 0.31.1 or later.
- **Pinned compile baseline.** The DLL is compiled against the pinned UnityModManager 0.31.1.0 (from the author's 0.31.1 NuGet package) and 0Harmony12 1.2.0.1, not against whichever loader is installed on the build machine. The published 0.6.20 DLL referenced UnityModManager 0.33.0.0. The build and the package validator refuse any other UnityModManager or Harmony reference.
- The runtime test host can report which loader assemblies it ran under (diagnostics only).

This is an owner-authorized pre-runtime release, published on the owner's authorization of November 26, 2026 for version 0.6.21. This exact release has not been run in the game. Native gameplay testing and combined-stack testing with other mods were NOT RUN, under any loader version.

Checks actually performed are static and managed only: the publisher's offline validation, two deterministic clean release builds and strict package validation (listed under Verification below). A static metadata audit of the published DLL's loader API references against UMM 0.31.1, 0.32.4, the owner's 0.33.0 and 0.33.4 is recorded in [the 0.6.21 release record](https://github.com/howardreith/KingmakerBuffPlanner/blob/main/docs/RELEASE-0.6.21-RECORD.md), bound to the published DLL's SHA-256. A static audit shows that the referenced loader types and members resolve; it is not a game run.

[UMM compatibility details](https://github.com/howardreith/KingmakerBuffPlanner/blob/v0.6.21/docs/UMM-COMPATIBILITY.md)
