# Missing beneficial buffs — recovery ledger after review round 12

Owner instruction of 2026-11-22 (direct Claude implementation; manual review
by the owner and ChatGPT afterwards; see the mission's
[§10](MISSING-BENEFICIAL-BUFFS-MISSION.md#10-owner-workflow-addendum-of-2026-11-22-direct-implementation-and-manual-review)).
Baseline: the round-12 candidate `fb075cb3d8b5e8c793e607b5194e3bf05631d8a1`
([review](https://github.com/howardreith/KingmakerBuffPlanner/pull/25#issuecomment-7071473210)).
Corrected product commit: `75e5f41` (this record and its evidence follow in the
next commit). Developer dispositions below are **not** reviewer acceptance.

## How each finding was established

- Every reproduction runs the production paths: the adapter, scanner,
  exporter facts, classifier and live discovery (`KingmakerBuffSourceDiscovery`),
  provider admission (`PartyProviderAdmission`), targeting
  (`ProviderTargetingPlanner`, `KingmakerAreaCoverageResolver`), the explicit
  compiler and step converter, Classic `RoutinePlanService` / `CastPlanner`,
  the `SequentialForecastPlanner`, and catalogue serialization with the audit
  reader. They use the committed native 2.1.7b fixtures. Only live reads are
  injected (CanTarget, distances, relation/fact reads), as in the existing
  regressions.
- Kinds: **native** = an unchanged committed blueprint; **unread** = an
  injected failing live read; **modified** = a deliberately altered blueprint
  (a robustness test, never an observation of the installed game).
- Every finding's reproduction first asserts the reported shape (which
  cast is funded, who is or is not a beneficiary, where the row starts). It
  then asserts the observable that was wrong. A compile failure or an earlier
  assertion does not count.
- **Baseline execution.** The isolated detached worktree
  `KingmakerBuffPlanner-MBB-BASE` sits at `fb075cb`; `git diff HEAD -- src` was
  empty. The tests ran there:
  - the committed `tests/KingmakerBuffPlanner.Tests/MissingBeneficialBuffsRecoveryTests.cs`,
    byte-identical, SHA-256 `197b602f…49ee8`;
  - a baseline-only R12-F1 probe,
    [baseline-export-probe.cs.txt](../docs/evidence/missing-beneficial-buffs-recovery-2026-11-22/baseline-export-probe.cs.txt),
    `823df9a9…`.

  Command: `scripts/Test-Development.ps1 -Only missing-beneficial-buffs-recovery`.
  Result: **0 passed, 12 failed**, every one on its asserted behavior and none
  on a precondition; every control subcase passed.
  [Log](../docs/evidence/missing-beneficial-buffs-recovery-2026-11-22/baseline-fb075cb.log)
  (`a285ddbc…`; DLL `2e5f2da9…`). The baseline worktree checked out CRLF
  through autocrlf, while the active worktree is LF. That explains why this
  DLL hash differs from the round-12 receipt's `9e006fa1…` for the same git
  source. The receipt's 96/0 was itself re-run unchanged on `fb075cb` in the
  active worktree.
  The planning half was also run once in the active worktree before any
  production edit, with `src` equal to `fb075cb`: 0 passed, 7 failed, the same
  messages.
- The active worktree never held substituted baseline production files.

## Ledger

| ID | Trigger → wrong observable on `fb075cb` | Path | Origin | Kind | Requirement | Disposition |
|---|---|---|---|---|---|---|
| R8-F1 | a's Extended Firebrand active; b casts on that disk, then a casts without Extend; a's Extended skip row stayed `AlreadySatisfied`, cost 0 | `ExplicitCastingCompiler` budget pass (`RecordCasterRelative` / `ReplacedByOtherCaster`) | introduced | native | §3C, §4 group spells, §8 resources | **fixed** |
| R10-F1 | Feather Step on m removed by a funded cast; w's funded Mass Feather Step may reach m (relation unread); the later m row regained its free skip (explicit and Classic) | `ChangesFor` / Classic `StepChanges` → `RemovalConflictState.Admit` | introduced | unread (native Mass Feather Step + synthetic remover) | §3A/§3C, §9.3 | **fixed** |
| R9-F5 | a's Firebrand on m; b's centre k; distance k–m unread; a's next request on m skipped (explicit and Classic forecast) | `ProviderTargetingPlanner` disk (unread distance dropped) → invalidation sets | introduced | unread | §3C, §9.3 | **fixed** |
| R9-F4 | Prayer with an outer `HasBuff(HasteBuff)` read present; the unselected IsEnemy branch vetoed the Classic skip (also when an earlier funded assignment set the gate) | `CastPlanner.ClassicPresence` (fixed facts only) | introduced | modified | §3C, §8 branch semantics | **fixed** |
| R12-F2 | Mass Enlarge funded on m; Mass Reduce row (starts Ready, k pre-covered) narrowed to k only; was Blocked `removes-planned-effect:enlarge:…:m` | budget pass: `WithPredictedBeneficiaries` kept the opening verdict | introduced | native | §3D, §4, §8 | **fixed** (path reachable, as claimed) |
| R12-F3 | b banned, only a's effective Firebrand on the ally; b vetoed the skip (a cast; with no slot a: Unfulfilled) | `CastPlanner.PlanRoutine` presence over unbanned resolver options | introduced | native (profile ban) | §3C, §8 | **fixed** |
| R11-F2 | two equally prioritized bards; A (higher level) needs a disk per ally, B has one covering centre; two A slots spent | `CastPlanner.SelectAndReserve` ranking by reachable union | exposed (ranking from `main`) | native | §3A, §4 Good Hope, §8 | **fixed** |
| R11-F1 | Mass Feather Step + on-cast `ContextActionCastSpell` to a child whose own on-cast list deals damage: admitted, `diagnostics=[]` (claim confirmed: the empty child scan reached the path); also as a run-graph reference | `NativeCatalogExporter.OnCastDiagnostics`; adapter reads a child's run action only | introduced | modified | §3A/§3C, §4 negative controls, §9.3 | **fixed** (refused) |
| R12-F1 | damaging on-cast Feather Step: live verdict exclude; serialized diagnostics lacked the on-cast fact; replay include | `NativeCatalogExporter` (`Diagnostics = scan.Diagnostics`) | introduced | modified | §3D, §5, §8 audit | **fixed** |
| R12-F4 | Good Hope (CanTargetPoint false) with its own selector condition, or an unread one: plain 15-ft disk, Ready, 1 slot | `KingmakerAreaCoverageResolver.CollectAbility` | introduced | modified | §3A, §9.3 | **fixed** |
| R12-F5 | FeatherStepBuff + the native Daze fixture's `BuffStatusCondition(Dazed)`: admitted on a unit anchor | `NativeCandidateClassifier.IsHarmfulCondition` all-components heuristic | exposed (heuristic from `main`) | modified | §3C, §4 negative controls | **fixed** (refused) |
| R12-F6 | GoodHopeBuff + `AddFactContextActions.Activated` removing Haste: admitted (removal identity lost) | exporter `FactActionList` + ally waiver | exposed (from `main`) | modified | §3A, §8 | **fixed** (refused) |

## Repairs (shared causes)

**A. Current planning evidence and uncertainty**

- *R9-F5 / R10-F1.* A provider option now carries the units an anchor's burst
  only possibly reaches. That means an unmeasured distance
  (`ProviderPlanningOption.UncertainIdsByAnchor`, filled by
  `ProviderTargetingPlanner`), or, as before, an unread relation under an
  Ally-only selector. `ReachesDefinitely` decides certainty.
  - Possibly reached units are **affected**: invalidation and removal
    conflicts count them.
  - They are never beneficiaries.
  - Their changes are `EffectChanges.Possible()`, so an application there
    never clears a recorded removal. This holds in the explicit compiler
    (`ChangesFor`), Classic (`StepChanges`) and the sequential forecast.
  - The recipient state treats them as possible.
  - Every option copy keeps the set.
- *R8-F1.* Caster-relative invalidation is monotonic.
  - A funded application voids every earlier instance on the units it may
    reach, the same caster's included, and the touched key stays touched.
  - The only new evidence is a projected instance of that application. It
    carries the cast's caster, caster level, metamagic and expected duration,
    and is recorded only where the cast is definitely reached and the
    application is guaranteed on every path.
  - A row whose opening instance was touched is re-assessed against that
    evidence.
  - Classic keeps every applier, so a later step by the original caster
    restores nothing.
- *R9-F4.* Classic presence and kept effects bind the target's known state at
  that point of the routine. A re-evaluated option already carries the read
  and projected mutable predicates. An unread predicate stays unresolved.
- *R12-F2.* A Ready or deferred group row whose beneficiaries change is
  recompiled for them before any conflict check or reservation:
  existing-effect verdict, pre-covered units, notes, readiness and cost shape.
  Initially `AlreadySatisfied` rows already were.

**C. Classic eligibility and centres**

- *R12-F3.* Banned providers are removed before the existing-effect decision
  and before the kept-effect accounting. Resources are not permission: a
  permitted caster without a slot still satisfies the request with its
  effect.
- *R11-F2.* Each permitted candidate's best feasible single cast (enhancement
  set, plus the best conflict-free legal centre) is decided before any
  reservation. Equally prioritized candidates are then ranked by that cast's
  coverage. Pins and explicit priorities decide first, and the old
  reachable-union keys only break ties. This is not global optimization.

**B. Conservative admission and audit parity**

- *R11-F1.* An on-cast list that references an ability is unproved. So is
  every ability the cast's graph references whose own on-cast list is not
  empty or presentation-only. The adapter exposes `ReferencedAbilities`.
  Either case gives an `on-cast-action` fact with a neutral detail, which
  refuses only the three whole-cast admissions. There is no recursive
  simulation.
- *R12-F4.* The selector-condition contract applies to every allied area. A
  non-empty or unread `m_Condition` yields no disk, so Good Hope is Blocked
  with nothing reserved. In the 2.1.7b dump, 32 selectors have a condition of
  their own. None belongs to an allied-area buff the planner covers: the
  Tactical Leader feat share is a `CurrentTarget` effect, and the others are
  offensive, enemy or non-area. The reason codes were renamed to
  `area-selector-has-own-condition` and `area-selector-condition-unread`.
- *R12-F5 / R12-F6.* The three whole-cast admissions (unit-anchored burst,
  ally-disposition waiver, Personal rescue) also refuse an applied effect, or
  a buff its automatic lists apply, that directly imposes a harmful
  condition. They also refuse any automatic restorative action in an applied
  buff's own lists. No admitted native buff carries `AddCondition`,
  `BuffStatusCondition` or `AddFactContextActions` (fixtures checked).
  Modifier penalties and size changes are judged as before.
- *R12-F1.* `NativeCatalogExporter.BuildEntry` is the per-ability step of
  `Export()`. It serializes `ClassificationDiagnostics`, the same array
  `AuditFacts` consumes (scan plus on-cast facts), and marks the entry
  `onCastFactsRecorded`. The audit reader builds the contract strings exactly
  as production does and reports exports without the marker as
  `onCastFacts=absent-in-N`. Missing facts are never inferred.

## Corrected results and controls (commit `75e5f41`)

Command: `scripts/Test-Development.ps1 -Only missing-beneficial-buffs-recovery`.
Result: **12/0** ([log](../docs/evidence/missing-beneficial-buffs-recovery-2026-11-22/final-75e5f41-recovery.log)).

| ID | Corrected observable | Controls kept (pass on both) |
|---|---|---|
| R8-F1 | a's Extended row Ready, one slot + one rod use, three executable steps; neighbor: a's own weaker always-recast (b disabled) also charges | sufficient Extended replacement skips; measured disjoint b disk skips; b disabled skips |
| R10-F1 | later row Blocked `effect-removed-by-earlier-casting:remove:…` (explicit); Classic not skipped | relation read allied: reapplication confirmed, skip holds (both planners) |
| R9-F5 | a's row Ready, one slot (explicit); a's next-routine step (Classic) | measured outside: skips (both); measured inside: m covered by b |
| R9-F4 | gate read present: skip, no step; gate set by an earlier funded assignment: skip | gate read absent: no skip; gate unread: no skip |
| R12-F2 | Reduce `AlreadySatisfied`, cost 0, beneficiaries {k}, no conflict; Enlarge funded | k not yet reduced: the cast still runs into the Enlarge conflict (Blocked) |
| R12-F3 | SkippedAlreadyActive, no step, no reservation; a without a slot: still skips | a's instance absent: a casts, b never |
| R11-F2 | one B step at the midpoint, one slot, both fulfilled | pin on A, explicit priority for A, B without a slot: two A casts |
| R11-F1 | refused by discovery and provider admission (on-cast and run-graph reference) | unmodified Mass Feather Step admitted |
| R12-F1 | entry carries `on-cast-action` (contract `AbilityExecuteActionOnCast`, detail with the damage action, path `on-cast`), `onCastFactsRecorded`; replay = exporter's live and static verdicts (exclude) | unmodified Mass Feather Step exports and replays as included; an entry without the marker is reported as lacking the facts |
| R12-F4 | conditioned / unread selector: no legal anchor, Blocked, cost 0 | native Good Hope: 15-ft disk, one slot, distant ally excluded |
| R12-F5 | refused by discovery and provider admission | unmodified Feather Step; Enlarge and Reduce Person, Mass (size tradeoff) admitted |
| R12-F6 | refused by discovery and provider admission | native Good Hope admitted; its Crushing Despair removal and GoodHopeBuff application both in the discovered changes |

## Validation (commit `75e5f41`, clean `src`/`tests`, DLL `405da354…`)

| Command (`-Only`) | Result | Time | Log |
|---|---|---|---|
| `missing-beneficial-buffs` | 108/0 (96 earlier + 12 recovery) | 9 s | [focused](../docs/evidence/missing-beneficial-buffs-recovery-2026-11-22/final-75e5f41-focused.log) `f803a6a2…` |
| `missing-beneficial-buffs-recovery` | 12/0 | 8 s | [recovery](../docs/evidence/missing-beneficial-buffs-recovery-2026-11-22/final-75e5f41-recovery.log) `6e686cf5…` |
| explicit `evidence-candidate-audit-missing-beneficial-buffs` | 2/0; native-only 1,722 / 16 changed, full-user 10,443 / 38 changed, changed rows byte-identical to round 10/11 | 18 s | [audit](../docs/evidence/missing-beneficial-buffs-recovery-2026-11-22/final-75e5f41-audit.log) `286cff91…` |
| every registered prefix (complete offline program) | 857/0 (845 at `c64a34f` + 12) | 44 s | [complete](../docs/evidence/missing-beneficial-buffs-recovery-2026-11-22/final-75e5f41-complete.log) `e1306d8d…` |

Source validation was 47/0 and the Release build 1/0 on each run. Runs on
earlier working trees during the work: 103/0 and 852/0 after batch A; 108/0
and 857/0 after batch B. They are superseded by the rows above. The audit
classifier source is `45c651c4a98d…`, with `NativeEffectOverrides.json`
`c462aeb4…`. Native, purity and release coverage is deferred (mission §9).
No game, staging or save was touched.

## Limits and notes for manual review

- **Historical exports.** Both audit exports predate the on-cast facts, so
  every row reads `onCastFacts=absent`. The R11-F1 and R12-F1 classes cannot be
  reconstructed from them. A fresh guarded export is the native confirmation
  and remains outside this mission (§9; the `zhcn19` lab lock is separate).
- **Conservatism by design.**
  - An unmeasured distance can make a removal conflict, and so refuse a cast
    that might not have reached that ally.
  - A modded allied selector with its own condition now plans no disk.
  - An automatic heal in an applied buff's lists refuses the three whole-cast
    admissions just as a removal does (the model has neither).
- **R11-F2 is bounded.** It compares single casts among equally prioritized
  providers. It does not search for an optimal multi-cast partition.
- **Not in these findings (source inspection only, not executed).** In the
  explicit planner, an earlier funded weaker cast of a *non*-caster-relative
  effect (for example non-Extended Haste) followed by an Extended skip row
  still trusts the opening live instance. This is pre-existing behavior on
  `main`, outside the caster-relative mechanism R8-F1 concerns. It is recorded
  for the reviewers, not changed.
- In-game behavior remains unverified: see the owner checklist in
  [MANUAL-ACCEPTANCE.md](../docs/MANUAL-ACCEPTANCE.md#missing-beneficial-buffs--owner-in-game-acceptance-pending).
