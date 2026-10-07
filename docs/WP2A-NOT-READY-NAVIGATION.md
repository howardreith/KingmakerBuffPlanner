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

## Checkpoint: guarded scenario, before complete source gate

Branch unchanged; exact HEAD d339811c44c6a8ae74c0d76ece8642f84b68d104,
version 0.3.0, dirty with the scenario/lifecycle changes. No runtime mutation.
Focused C# regression command now PASS=14 FAIL=0. The complete C# runner
at the preceding scenario revision passed 411/411. Production validation
42/42 and compilation 1/1 pass with no compiler warnings. Independent
scripts/Test-ProblemNavigationEvidence.ps1 under the absolute Windows
PowerShell 5.1 host: PASS=18 FAIL=0. Actual complete source gate remains next.

The existing live-workspace-physical launcher now accepts only the additional
physicalExpectation "problems", with no allowance permitted. The scenario
authors a disposable Long plan in the transactional mod directory, with
twenty early castings and two later Drafts. It requests physical moon,
Next, Previous and two Escape presses. Read-only seams measure masked
chip visibility before reveal, actual screen points and visible card
fraction afterward, catalogue/routine points, visible inspector text and
boundary controls. An independent record judge and launcher verifier
require unchanged document/profile/native state, zero dispatch/run,
unchanged Undo and no authorization, and a clean nested Escape close.
Existing cast/select expectations and allowance guards remain intact.

Supporting lifecycle repair: a global party-refresh refusal after problem
inspection clears stale problem focus and remains a global explanation.
Regression proves no document, profile or Undo change.

Push helper's authorized branch set was extended only for the owner's exact
requested branch; repository, payload, clean-tree and fast-forward guards
remain mandatory. Helper SHA-256:
55b7d87057655e61881b7acfcac0858789fdce198343ceab955433c2bbb9e3e2.
scripts/Test-GuardedPush.ps1 now validates the actual focused worktree.

Uncertainty: no physical claim yet. A charter annotation was rejected by
the authoritative-copy validator and removed; both mission copies remain
byte-identical. Development C# scope/string-literal defects were corrected,
then the affected tests/build rerun. Exact next action: commit this coherent
scenario checkpoint, execute scripts/Test-SourceOnly.ps1 to zero failures,
Build-Local on the clean commit, then bind the guarded physical run to the
resulting ZIP/DLL hashes and MVID.
## Checkpoint: unavailable-source reachability regression

Branch codex/kbp-not-ready-deep-linking-2026-10-07; exact pre-commit HEAD
8ec8cbf00c2030e4b86b8b0424795a8f816a37ce; version 0.3.0; dirty only with
this focused repair and evidence. The previously running complete source
gate was stopped at its archival manifest comparison so this change can
be qualified on a new clean candidate. Its completed components were C#
412/412, runtime filesystem 38/38 and problem-evidence 18/18. It did not
reach a complete source-qualified verdict or enter live runtime.

A real-service regression removed the saved buff from current discovery.
Ordinary Apply correctly identified its authored casting, but the graph
lacked the selected catalogue row required to finish reveal. The focused
runner reproduced PASS=14 FAIL=1 at artifacts/wp2a-missing-source-red.log.
The repair supplies an explicitly unavailable selected row only during
problem inspection, without a provider or readiness judgment. Reveal now
waits for all graph, catalogue and inspector rectangles before moving any
scroll. Actual imported Draft provenance is also exercised without
guessing its caster.

After repair: affected MSBuild test rebuild and the --blocked-navigation
runner PASS=15 FAIL=0 (artifacts/wp2a-focused-15.log). scripts/Build.ps1
source validation 42/42 and build 1/1 pass, with no compiler warnings
(artifacts/wp2a-unavailable-source-build.log). Development DLL SHA-256
1f315543524edbd48af18b531445166a47dc07d7f59e17ac2a6fc34699be87e5 is not
yet a qualified candidate.

Rejected theory: an unavailable saved source always has a catalogue row
because selected entries bypass filters. That rule applies only to entries
discovery actually supplied. No source catalogue audit or readiness
implementation was added.

Exact next action: commit; Build-Local for the harness's required ZIP
prerequisite; run the complete Test-SourceOnly gate to zero failures;
rebuild the final clean candidate; run the candidate-bound blocked-only
physical scenario; verify exact restoration and protected saves.
Physical visibility remains uncertain until that run passes.