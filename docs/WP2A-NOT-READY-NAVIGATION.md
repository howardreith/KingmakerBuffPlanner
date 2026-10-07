# WP2A — Not Ready problem navigation (unreleased 0.3.0 candidate)

Scope: find a blocked casting immediately. Base: current origin/main
b707c1f47859f2ecae517b2cd162d20ca18c7583, fetched 2026-10-07.
Branch: codex/kbp-not-ready-deep-linking-2026-10-07.
Worktree: private/worktrees/WP2A under the original checkout. The original
checkout and the WP1 branch are preserved; no WP1 commit is inherited.
Public version stays 0.3.0. No merge, tag, PR or release is authorized.

## Checkpoint: implementation before physical qualification

HEAD before this checkpoint: b707c1f47859f2ecae517b2cd162d20ca18c7583;
worktree dirty with the focused implementation. Remote main matches that
SHA; no open pull requests. Initial ownership audit found 398 deployment
transactions, all Restored, no game process, no deployment/foreign lease
and no dispatcher process. Steam is running. No live mutation occurred.

Previous behavior, traced in production: CastingExecutionGate exposed
sorted textual BlockingReasons only. Planner RunApply showed RunBlocked
and returned without focusing. The HUD StartCastingFirstRoutine selected
the routine and opened Setup with a generic footer; no casting identity
or viewport reveal was requested.

Implemented: CastingBlocker identities and exact reasons in compiled
execution order, propagated through CastingApplyDecision and
WorkspaceApplyResult. The shared session Apply boundary starts transient
navigation, uses FocusGraphCasting and recomputes from the existing
BuildGraph gate after edits. Previous/Next change inspection only.
Disabled/already-active omissions stay outside the blocker collection.
Ready Casts Only retains its existing gate and omission policy.
Global refusals supply no casting identity.

The view presents routine, Problem X of N, buff, caster/target and the
existing reason translation, and reveals card/catalogue/inspector through
actual RectTransform bounds. A consumed reveal is never requested by an
ordinary refresh; geometry changes may request a single new reveal.
Search/category settings are retained. Manual selection, Done and the
existing nested Escape behavior leave problem inspection.

Commands at this checkpoint:
- MSBuild tests/KingmakerBuffPlanner.Tests/KingmakerBuffPlanner.Tests.csproj
  /t:Rebuild /p:Configuration=Release /m /nologo /v:minimal
- artifacts/tests/KingmakerBuffPlanner.Tests.exe --blocked-navigation:
  PASS=11 FAIL=0.
- scripts/Build.ps1: source validation PASS=42 FAIL=0; build PASS=1 FAIL=0.
  No compiler warnings. Development DLL SHA-256
  60f973067c474bb83b48cd620344b513a1498a0d88468fce1ef0497c1980b74a
  was built at dirty baseline HEAD and is NOT a qualified candidate.

Rejected approaches: parsing refusal text; sorting identities; adding a UI
readiness predicate; permanently owning scroll; inheriting WP1. Initial
test defects corrected: an empty Draft uses the gate's existing fallback
reason; presentation state must be captured before inspecting navigation.

Uncertainty: physical Unity layout, masked visibility and automatic HUD
opening are not yet qualified. Complete mechanical gate not yet run.
Exact next action: extend the existing guarded physical-workspace scenario
with a blocked-only expectation; qualify the complete source gate, commit
and build a clean identity-bound candidate, then physically exercise the
HUD/Previous/Next/Escape paths with zero native submissions and exact
installation/save restoration.
