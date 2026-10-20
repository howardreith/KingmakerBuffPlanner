# Mr. Bard's Kingmaker Buff Planner 0.6.4

**Owner-authorized release.** The owner tested `v0.6.4-alpha.1` on the main
laptop and reported that controller functionality is now usable, with further
improvements still needed. This exact 0.6.4 release has not been run in the
game. The owner has not supplied a detailed acceptance trace for the stable
package, so the spellbook entry and the wider controller authoring and casting
flows remain unverified here.

This release promotes the controller entry repair from
[PR #13](https://github.com/howardreith/KingmakerBuffPlanner/pull/13).
The 0.6.3 laptop log showed native Buff Planner card selection followed by
`request-refused:root-absent`: the planner controller had never been
constructed. The 0.6.4 code installs a mod-owned Unity update driver when
enabled, allowing controller initialization if UMM's UI update host does not
appear. A frame gate avoids duplicate updates if both paths run, and unload
removes the driver. Card requests and spellbook entry creation now report
bounded reasons when their prerequisites are missing.

The exact cause of the missing UMM update host on the owner's laptop is still
unknown. The mod ID, profile identity, and 2.1.5 UMM game-version minimum are
unchanged. Native Linux 2.1.5d behavior has not been verified.

Back up the existing `Mods/KingmakerBuffPlanner` folder, including
`UserSettings`, outside `Mods` before updating. In native controller mode,
open the RT/R2 campaign menu, select Buff Planner with mapped Confirm/A,
release the controls, verify the planner opens, press Back, and repeat. Also
check the spellbook footer. If either entry fails, provide the UMM/Unity log
and the first `[KBP-CONTROLLER-CARD]` or spellbook diagnostic sequence.

See the [0.6.3 log investigation](CONTROLLER-ENTRY-0.6.3-LOG-INVESTIGATION.md)
and the [alpha test notes](RELEASE-NOTES-0.6.4-ALPHA.1.md).
