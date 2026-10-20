# Mr. Bard's Kingmaker Buff Planner 0.6.1-linux.1

**Owner-authorized pre-runtime experimental prerelease.** This exact release
has **not been run in the game**. It is offered for a user with native Linux
Pathfinder: Kingmaker **2.1.5d** to try and report results. Linux loading,
mouse use, persistence, casting, and controller behavior are unverified.
Windows 2.1.7b gameplay was not rerun for this metadata change.

The only compatibility edit lowers Unity Mod Manager's numeric minimum game
version from `2.1.7` to `2.1.5`. No planner, controller, casting, or profile
logic changed. The mod ID, entry point, UMM minimum `0.28.2`, dependencies,
and `UserSettings` identity remain the same. A version/source-revision stamp
changes this prerelease DLL's bytes; the separate manifest-only diagnostic
uses the exact published 0.6.0 DLL and is described in the
[candidate record](LINUX-2.1.5D-CANDIDATE.md).

Before installing, back up the entire existing `Mods/KingmakerBuffPlanner`
folder, including `UserSettings`, outside `Mods`. Try the asset on a disposable
campaign. First confirm UMM load and mouse planner open/close; then create and
reopen a small native-buff plan, check it across restart, and only then try
representative Instant and Animated casts. If it fails, stop and report the
first exception with stack trace, UMM `Log.txt`, Unity `Player.log` if present,
game build, platform, UMM version, and exact ZIP checksum. Avoid personal saves.

This prerelease is an invitation to test, not a claim of confirmed native
Linux support. The stable 0.6.0 release remains unchanged.
