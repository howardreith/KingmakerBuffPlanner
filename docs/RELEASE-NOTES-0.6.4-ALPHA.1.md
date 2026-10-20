# Mr. Bard's Kingmaker Buff Planner 0.6.4-alpha.1

This is an owner-authorized pre-runtime alpha release for testing controller
entry. This exact release has not been run in the game. It is a GitHub
prerelease; stable 0.6.3 remains the latest stable release.

The owner's Windows Kingmaker 2.1.7b / UMM 0.33.0 output log for 0.6.3 records
three native Buff Planner card selections, each refused before a planner
handoff. The mod's first update tick and planner controller construction do not
appear in that run. The immediate failure was the absent planner controller;
the reason UMM's UI update host did not appear on that laptop remains unknown.
`Main.cs` and the controller spellbook adapter had no source changes from
`v0.5.4-controller.4` through 0.6.3, so this log does not establish a
regression in those source paths.

This alpha adds one mod-owned Unity update driver when enabled. It uses the
existing planner update path if UMM does not call it, with a frame gate to
avoid duplicate updates if both paths run. Unload removes the driver. A card
request with no controller now logs `request-refused:root-absent`, and spellbook
entry creation logs bounded reasons when registration is skipped.

The initial test is controller mode -> RT/R2 campaign menu -> focus Buff
Planner -> Confirm/A -> release controls -> planner editor or explicit mode
review -> Back -> ordinary gameplay -> repeat. Also inspect the spellbook
footer. Do not cast or edit a plan in this first test. Send the UMM/Unity log
from this exact alpha so `owned update driver installed`, `OnUpdate first
tick`, `controller constructed`, `card-selected`, `handoff-requested`, and the
terminal destination can be correlated. If the spellbook footer is still
missing, its new skip diagnostics should identify the next boundary.

This alpha was built from the PR #13 branch. It is not a native acceptance
claim or a fix to the already installed 0.6.3 copy. See the
[investigation record](CONTROLLER-ENTRY-0.6.3-LOG-INVESTIGATION.md).
