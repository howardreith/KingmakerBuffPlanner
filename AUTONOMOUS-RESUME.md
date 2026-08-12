# Autonomous Resume

Status: IN PROGRESS — 0.0.1 UI acceptance invalidated by human playtesting

- Repository/branch: standalone Kingmaker Buff Planner / `codex/kingmaker-buff-planner`
- Starting/current committed HEAD: `ec153837401c8815b1909cb15e85ab658a1ee26a`
- Active/installed version: `0.0.1`; installed DLL SHA-256 `3d356e6b1dbf422b4aa8e721fb4d71051dc10c2097547473bbfdd135b8c11be8`
- Worktree: user-supplied untracked repair mission plus root-cause inspection changes
- Authoritative repair mission: `planning/FULLSCREEN-UI-INPUT-ISOLATION-REPAIR-MISSION.md`
- Confirmed root cause: the production UI is fixed-coordinate IMGUI, while exact Kingmaker `PointerController.InGui` only recognizes EventSystem pointer-over-GameObject state. No native full-screen mode or raycast surface is acquired.
- Exact native evidence: `Assembly-CSharp.dll` SHA-256 `3b6450ffec440e296e586f71c711b195aed144b28d53e1cbb29406d18fef5afb`, MVID `07fa1e4d-8618-41b3-9b8d-faa17d3b26f7`; full-screen event enters/stops `GameModeType.FullScreenUi`; native service canvas blocks raycasts and hides/restores HUD.
- Invalid prior gate: it hardcoded routine count/layout and required zero blockers/subscriptions without dispatching real UI/world input.
- Long finding: an empty/zero-step routine reports only inside the closed setup window, so HUD execution can appear silent even if its coroutine ran.
- Runtime state: Kingmaker closed; no deployment lock; live `Mods\KingmakerBuffPlanner` remains validated 0.0.1; no save accessed.
- Separate existing hard stop: no authorized `KBP_AUTOMATION_BASELINE` / `KBP_AUTOMATION_WORKING` pair; this does not block UI-only no-save repair qualification.
- Exact next command: implement and run a guarded no-save native UI hierarchy/input-contract probe, record the stable bottom-left anchor and service canvas state, then build the production uGUI controllers and scoped input lease.
