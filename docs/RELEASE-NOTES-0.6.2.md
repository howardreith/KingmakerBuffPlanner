# Mr. Bard's Kingmaker Buff Planner 0.6.2

**Owner-authorized pre-runtime full release.** This exact 0.6.2 release has
not been run in the game. Native Linux Pathfinder: Kingmaker 2.1.5d loading,
mouse use, persistence, casting, and controller behavior remain unverified.
Windows 2.1.7b gameplay was not rerun for this version and metadata change.

Unity Mod Manager's numeric minimum game version is 2.1.5, admitting the
reported native Linux 2.1.5d build past that version gate. The planner,
controller, casting, and profile logic is unchanged from the experimental
0.6.1-linux.1 prerelease. The 0.6.2 DLL changes version and source-revision
stamps. The mod ID, entry point, UMM minimum 0.28.2, dependencies, and
`UserSettings` profile identity remain unchanged. No optional gameplay mod
is required or bundled.

Back up the existing `Mods/KingmakerBuffPlanner` folder, including
`UserSettings`, outside `Mods` before updating. For the first native Linux
trial, use a disposable campaign. Confirm UMM load and mouse planner
open/close, then create and reopen a small native-buff plan across a game
restart before trying representative Instant and Animated casts. If a step
fails, stop and provide the first exception with its stack trace, UMM
`Log.txt`, Unity `Player.log` if available, game build, platform, UMM version,
and the exact ZIP checksum. The [trial procedure](https://github.com/howardreith/KingmakerBuffPlanner/blob/v0.6.2/docs/LINUX-2.1.5D-CANDIDATE.md)
has the complete small workflow.

This release makes the older build eligible to load; it is not a claim of
confirmed native Linux compatibility. The 0.6.1-linux.1 prerelease and
earlier 0.6.0 release remain available.
