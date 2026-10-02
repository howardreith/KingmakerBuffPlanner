# AUTONOMOUS RESUME — updated 2026-10-02 (owner-requested stopping point)

## Where the work stands

Everything is committed and pushed on branch
`codex/kingmaker-buff-planner-everyday-use`; HEAD `3c1c5d4a5193d3d84aa205e234c0a9b09b4face7` (full gate
PASS at this exact tree: `artifacts/gate-3c1c5d4.log`). The delivery
candidate is frozen with verified bytes under
`runtime-backups/qualification-frozen/3c1c5d4a5193d3d84aa205e234c0a9b09b4face7/FREEZE.json` (package
1b6d67c0f7fd5ba8ba8d4a9047073500e0a5c3e01a76851c6e13ae410a2d5665, DLL
c727c5e2565adff320d9c53a313e815f57c479e9d198dcb6188542214462f7a3, MVID
6fb77f64-c7f7-4fbb-8340-5e6ca6c839f6).

## The exact next action

1. Dispatch the already-built r13 evidence batch (41 jobs, run IDs
   `beta-3c1c5d4ar13-*`, bound to the frozen candidate):
   `powershell -NoProfile -ExecutionPolicy Bypass -File
   C:\Dev\KingmakerBuffPlannerLab\dispatch\dispatcher.ps1`
   (in `C:\Dev\KingmakerBuffPlannerLab\dispatch`). The dispatcher now
   runs on any Active session of the owner account; it waits only for
   3 minutes of owner input-idle (self-clearing) and honors an
   `owner-pause` flag file in that directory. The launcher's foreground
   activation works under RDP as of 3c1c5d4a5193d3d84aa205e234c0a9b09b4face7 (owner-authorized
   AttachThreadInput route).
2. As runs complete, fill the NOT-RUN cells in
   `docs/E01-E27-ACCEPTANCE-MATRIX.md` with the actual run IDs.
3. Private delivery (draft GitHub release, prior beta asset untouched):
   unique everyday-use filename, upload, DOWNLOAD and hash-verify the
   bytes, then the final report with the retrieval location, exact
   identity, qualification evidence, honest limitations and rollback
   notes. Follow `artifacts/tmp_release_notes.md` (staged) for the
   release body.

## Rules that bind the resume

- The repo tree must stay clean and HEAD must equal 3c1c5d4a5193d3d84aa205e234c0a9b09b4face7 while r13
  jobs run; documentation commits happen AFTER the batch.
- A failed job: preserve evidence, restore through
  `Restore-Local.ps1 -RunId <run>`, diagnose from the record (the
  physical record now publishes every field + notes), fix, full gate,
  freeze, rebuild jobs with a fresh suffix; retry with a fresh run ID.
- The two hover-dependent jobs (ui-windowed, ui-advanced) are queued at
  the END of the batch; under RDP they may still fail their hover phase
  even with the new activation route - if they do, run them when the
  session is back on the physical console (the failure is exactly and
  only the hover mouse-move delivery).
