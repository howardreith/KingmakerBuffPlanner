# Controller entry investigation after 0.6.3

## Evidence and comparison

The owner's Windows `output_log_10_20_26_205PM.txt` is from Kingmaker 2.1.7b,
UMM 0.33.0, and the published 0.6.3 assembly stamped
`f82555c2c52d2580b187be7006555c855a8862fe` (lines 14, 36, 98-108).
The supplied file's SHA-256 is
`8427BCF6FC3AFEA45746EAC53140D85C959FAA57688980882051FFEC78DA71E8`.
It is retained outside the repository at
`C:\Dev\KingmakerBuffPlannerLab\handoff\output_log_10_20_26_205PM.txt`.

The native menu captured its input layer and invoked the selected-card action
three times (lines 6146-6158). Every action ended in
`request-refused:admission`; none reached `handoff-requested`. There is no
`[KBP-BOOT] OnUpdate first tick` or `controller constructed` in this run, nor
the UMM `Spawning` event seen in the lab's earlier UMM 0.33 log. The card's
admission method returned silently when the controller singleton was absent.
This is the observed failure stage. Why UMM's UI update host did not spawn on
that machine is not established by this log.

`Main.cs` and `ControllerSpellbookIntegration.cs` have no source difference
between `v0.5.4-controller.4` and `v0.6.3`. The missing controller singleton
also makes the spellbook footer's readiness test false, even if its native
binding was created. This log contains no spellbook registration or skip event,
so a separate spellbook creation issue remains possible. The layer capture
records an ordinary `InGameMainInputContext` predecessor; the earlier
null/BaseLayer theory does not explain these refusals.

## Repair checkpoint

Branch: `codex/kingmaker-buff-planner-controller-entry-investigation`.
Base HEAD: `aeec51a0993eb1d2416c0fc569ab9d5ab2fdc215`.
Version: 0.6.3 source after the published release; no replacement release.

The mod now creates one owned Unity update driver when enabled. It drives the
existing `OnUpdateCore` when UMM's UI callback is absent. A frame gate prevents
both update paths from pumping the planner twice in one Unity frame. Unload
destroys the owned driver. Card refusal reports `root-absent` explicitly, and
spellbook creation records bounded skip reasons.

Selected check:

```text
powershell -NoProfile -ExecutionPolicy Bypass -File .\scripts\Test-Development.ps1 -Only 'controller-,qualification-on-the-planner-pumped-host,qualification-disable-step-rules'
Source validation: PASS=47 FAIL=0
Build: PASS=1 FAIL=0
Protocol tests: PASS=40 FAIL=0
DEVELOPMENT PASS=40 FAIL=0
```

This is offline evidence. No native 0.6.3 replacement build has been run on
the owner's laptop. Next: review the diff, then install a guarded test package
on that laptop and observe cold controller mode -> RT/R2 card -> A -> editor
or review screen -> Back -> reopen, plus the spellbook footer. Record
`owned update driver installed`, `OnUpdate first tick`, `controller constructed`,
`card-selected`, `handoff-requested`, and the terminal destination. Keep casting
and plan edits out of this first observation.
