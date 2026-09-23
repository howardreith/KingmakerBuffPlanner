# Supervised manual session — `casting-ws-claude-manual-20260922-200244`

First human session on the casting-first workspace (2026-09-22,
20:02–20:15 local). Operator: Howie. Supervising agent: Claude.
Non-casting UI test on the disposable WORKING campaign; native casting
disabled throughout.

## Lifecycle (harness result — NOT a usability verdict)

| Item | Value |
|---|---|
| Build | commit `5781301c6823ff424bb7a9b61377f03de7c81b9e`, DLL `22a936d3…7129`, MVID `f55909e8-0245-4b2d-b643-ffba4c5b9f3d`, package `67e395ce…6fc6` |
| Profile / save | `full-user` (15 mods) / `KBP_AUTOMATION_WORKING` |
| Game | PID 32580, RDP session 2 |
| manual-ready | hold 900 s, `syntheticInputRequested=false`, workspace open, legacy closed; frame checked before handover |
| Terminal | Howie: "You can end the hold and move on." → Claude wrote `manual-done.json` (`by=operator`, origin recorded) at 20:15:41 via `Write-KbpJsonAtomic`, before the 20:19:32 deadline |
| Result | PASS: ready, no automatic authoring, `manual-completed;by=done-marker`, final capture (nonBlack), camera restoration clean, workspace closed + input lease released |
| Restoration | transaction `Restored`, `restorationVerified=true`, 00:15:48Z UTC; no lock, no process |

## Operator observations (separate from the lifecycle)

- "Focus" buttons in the Casters lane: no visible effect.
- Routine tabs (long / important / short): no visible change.
- No cards at first; the workflow was not self-evident until
  exploring ("Oh wait, I think I understand now").
- Added **Light**, caster **Linzi**, recipient **Hedwirg**, single
  target: Add Casting worked. Undo worked.
- "Everything looks to be working. I really hate this UI, but I assume
  this is a WIP."
- Direction given afterwards: stay broadly similar to Bubble Buffs;
  the individual casting is the atomic unit (see `docs/UI-END-GOAL.md`).

**Usability verdict: FAIL (disliked).** Functional Add/Undo observed.

## Not Run

Edit/retarget/Done, group ↔ direct targeting, unsaved close/reopen,
Save/Reload, scrolling to last rows, long labels, hover/pressed/disabled
states, tooltips, sounds, world-input isolation.
