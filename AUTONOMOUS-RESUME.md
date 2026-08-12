# Autonomous Resume

Status: INSTALLED FOR HUMAN UI RETEST — campaign UI and save-backed qualification pending

- Repository/branch: standalone Kingmaker Buff Planner / `codex/kingmaker-buff-planner`
- Starting repair HEAD: `ec153837401c8815b1909cb15e85ab658a1ee26a`; release commit: `447bbd288c803a4aec609db84a4c6076cbfe94f3`; final records checkpoint pending
- Development/installed version: `0.0.2`; installed DLL SHA-256 `c2598e0d31e464eaf8446e15280cbe13b3eeb4e56b0de92e20cc8f29fb458e84`
- Worktree: final install/release records only
- Authoritative repair mission: `planning/FULLSCREEN-UI-INPUT-ISOLATION-REPAIR-MISSION.md`
- Confirmed root cause: the production UI is fixed-coordinate IMGUI, while exact Kingmaker `PointerController.InGui` only recognizes EventSystem pointer-over-GameObject state. No native full-screen mode or raycast surface is acquired.
- Exact native evidence: `Assembly-CSharp.dll` SHA-256 `3b6450ffec440e296e586f71c711b195aed144b28d53e1cbb29406d18fef5afb`, MVID `07fa1e4d-8618-41b3-9b8d-faa17d3b26f7`; full-screen event enters/stops `GameModeType.FullScreenUi`; native service canvas blocks raycasts and hides/restores HUD.
- Invalid prior gate: it hardcoded routine count/layout and required zero blockers/subscriptions without dispatching real UI/world input.
- Long finding: an empty/zero-step routine reports only inside the closed setup window, so HUD execution can appear silent even if its coroutine ran.
- Runtime state: Kingmaker/UMM closed; no deployment lock; live `Mods\KingmakerBuffPlanner` is guarded-installed validated 0.0.2; all non-planner mods verified byte-identical; no save accessed.
- Separate existing hard stop: no authorized `KBP_AUTOMATION_BASELINE` / `KBP_AUTOMATION_WORKING` pair; this does not block UI-only no-save repair qualification.
- Last runtime gates: native `ui-repair-0.0.2-final-native-1/2` 12/12 each and Call of the Wild `ui-repair-0.0.2-cotw-1/2` 26/26 each, exact restoration. Corrected UI gate rejected main-menu-only evidence and restored exactly.
- Exact release: package `1f328e26fdf2524fc85e8482077e5330bc5f7a48ce0d8841bd372997685d652f`, DLL `c2598e0d31e464eaf8446e15280cbe13b3eeb4e56b0de92e20cc8f29fb458e84`, MVID `e43f060b-a2b7-48db-b19f-b45704ef77c4`, commit `447bbd288c803a4aec609db84a4c6076cbfe94f3`.
- Install evidence: `C:\Dev\KingmakerBuffPlannerLab\runtime-evidence\install-ui-repair-0.0.2-local-install\install-result.json`.
- Exact-release runtime: native `ui-repair-0.0.2-release-native-1/2` 12/12; Call of the Wild `ui-repair-0.0.2-release-cotw-1/2` 26/26; UI boundary `ui-repair-0.0.2-release-ui-boundary` structured `BLOCKED` because no campaign fixture; all restored exactly.
- Exact next command: commit these final evidence records, rerun guarded push, then hand off the installed build for authoritative human playtesting.
