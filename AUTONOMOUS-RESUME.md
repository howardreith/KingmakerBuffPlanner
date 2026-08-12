# Autonomous Resume

Status: IN PROGRESS — 0.0.1 UI acceptance invalidated by human playtesting

- Repository/branch: standalone Kingmaker Buff Planner / `codex/kingmaker-buff-planner`
- Starting repair HEAD: `ec153837401c8815b1909cb15e85ab658a1ee26a`; current committed HEAD: `3bd519b000f3126b19462888aefeabe29374873d`
- Development version: `0.0.2`; installed version remains `0.0.1` with DLL SHA-256 `3d356e6b1dbf422b4aa8e721fb4d71051dc10c2097547473bbfdd135b8c11be8`
- Worktree: documentation truth correction plus failure-envelope BLOCKED classification; final checkpoint pending
- Authoritative repair mission: `planning/FULLSCREEN-UI-INPUT-ISOLATION-REPAIR-MISSION.md`
- Confirmed root cause: the production UI is fixed-coordinate IMGUI, while exact Kingmaker `PointerController.InGui` only recognizes EventSystem pointer-over-GameObject state. No native full-screen mode or raycast surface is acquired.
- Exact native evidence: `Assembly-CSharp.dll` SHA-256 `3b6450ffec440e296e586f71c711b195aed144b28d53e1cbb29406d18fef5afb`, MVID `07fa1e4d-8618-41b3-9b8d-faa17d3b26f7`; full-screen event enters/stops `GameModeType.FullScreenUi`; native service canvas blocks raycasts and hides/restores HUD.
- Invalid prior gate: it hardcoded routine count/layout and required zero blockers/subscriptions without dispatching real UI/world input.
- Long finding: an empty/zero-step routine reports only inside the closed setup window, so HUD execution can appear silent even if its coroutine ran.
- Runtime state: Kingmaker closed; no deployment lock; live `Mods\KingmakerBuffPlanner` remains validated 0.0.1; guarded native probe proved `StaticCanvas` unavailable at the main menu and restored `Mods` exactly; no save accessed.
- Separate existing hard stop: no authorized `KBP_AUTOMATION_BASELINE` / `KBP_AUTOMATION_WORKING` pair; this does not block UI-only no-save repair qualification.
- Last runtime gates: native `ui-repair-0.0.2-final-native-1/2` 12/12 each and Call of the Wild `ui-repair-0.0.2-cotw-1/2` 26/26 each, exact restoration. Corrected UI gate rejected main-menu-only evidence and restored exactly.
- Current exact local package (will change after final commit): package `651d9ce3f92649f86d6e619e46fe3293ace1019e10e3b086c2a8c3617452b68f`, DLL `f039f436fb948c7acb203e60979dec3bb500e03e85ff8f6a73ae6753b293b850`, MVID `5f57af25-8876-400a-99b9-5971d8bfd8f4`.
- Exact next command: `scripts\Test-SourceOnly.ps1`, commit docs/failure-envelope changes, build clean final release, validate/install via `scripts\Install-Local.ps1`, then guarded push and handoff.
