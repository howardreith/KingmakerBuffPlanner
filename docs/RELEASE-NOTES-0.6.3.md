# Mr. Bard's Kingmaker Buff Planner 0.6.3

**Owner-authorized pre-runtime full release.** This exact 0.6.3 release has
not been run in the game. Native controller acceptance, including the actual
RT/R2 campaign menu → Buff Planner card → mapped Confirm/A → editor → Back
sequence, remains **NOT_RUN**. The owner's reported A-press failure stage has
not been observed in a current in-game trace.

This release contains the controller card repair merged in
[PR #12](https://github.com/howardreith/KingmakerBuffPlanner/pull/12).
The card retains its native input-layer observation across binding and card
construction order, derives the correct predecessor for an already-pushed
layer, waits for native release and neutral controls, and logs the admission
and handoff outcome. If a buff routine is still running when the destination
opens, the card shows quick controls with **Stop**; editing remains unavailable
during that run. Diagnostics distinguish the editor, running controls, Classic
review, recovery, and canceled handoffs.

The repository's complete offline C# suite, source checks, publisher gate,
deterministic package builds, and package validation are release gates. They
do not establish in-game controller behavior. The 2.1.5 UMM minimum from
0.6.2 remains unchanged; native Linux 2.1.5d loading and gameplay are also
unverified. The mod ID and existing `UserSettings` profile identity are
unchanged.

Back up the existing `Mods/KingmakerBuffPlanner` folder, including
`UserSettings`, outside `Mods` before updating. Begin with a disposable
campaign and no casting or plan edits: in controller mode open the RT/R2
campaign menu, focus Buff Planner, press mapped Confirm/A, release controls,
verify the editor, press Back, and repeat. Focus and Back without confirming
must not open the planner. After that works, test a running routine's card
route to quick controls and Stop. If a step fails, provide the first
`[KBP-CONTROLLER-CARD]` stage sequence, UMM/Unity logs, game build, platform,
UMM version, and ZIP checksum.

The [repair record](CONTROLLER-CARD-ACTIVATION-REPAIR-2026-10-20.md) documents
the exact native assembly contracts, rejected null/base-layer theory, offline
regressions, and remaining acceptance steps.
