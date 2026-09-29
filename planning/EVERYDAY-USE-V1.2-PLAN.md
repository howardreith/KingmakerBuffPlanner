# Everyday-use v1.2 — implementation map and status

Branch: `codex/kingmaker-buff-planner-everyday-use` from `dd50ed1`
(records head; beta source `ec34705c`, package `214b795b…` preserved
immutable). Owner verdict recorded correctly: **graph direction accepted
positively ("Overall I am very happy. This is very close to my vision.");
the everyday-use changes below are outstanding work, and final acceptance
of these changes is pending the next trial.**

Source documents: `KBP_Everyday_Use_v1_2_Handoff` (01 addendum, 02 Z
mission, 03 acceptance matrix E01–E27, 04 owner feedback verbatim).
The v1.2 addendum supersedes: Classic default, Animated default, manual
Save/Accept ceremony, Share-out-of-scope, book artwork acceptance,
any-rod enhancement lists. It does NOT waive resource checks, exact
targeting, profile protection, protected saves, or native safeguards.

## Requirement → source → change → observable outcome

| # | Requirement (matrix) | Source starting points | Planned change | Outcome |
| --- | --- | --- | --- | --- |
| 1 | Casting-first default + sole normal route (E01, E03) | `Persistence/PlannerModeStore.cs`, `Main.cs`, `UI/BuffPlannerUiRoot.cs` (SwitchPlannerFromScreen, mode button), settings panel | Default mode casting-first on all creation paths; remove prominent Classic switch; keep importer + rollback + read-only legacy types | Every entry opens the graph; no competing writer |
| 2 | Instant default, Animated preference retained (E01, E02) | `Persistence/CastingPlanProfileModels.cs`, execution-mode default sites, `CastingExecutionHost` | Explicit Instant default on genuine new/unset paths; never reset an explicit Animated choice | New profile runs Instant; old Animated kept |
| 3 | Continuous scroll, no book binding (E04) | `UI/CastingWorkspaceScreenView.cs` (page art locator `ServiceWindow/SpellBook/BookBackground`), `PlannerNativeTheme*.cs` | Borrow a verified native parchment/paper donor at runtime into mod-owned visuals; one uninterrupted surface under connections | No gutter in presented frames |
| 4 | Right-click full spell description (E05, E06) | `CastingWorkspaceScreenView.cs`, native spellbook/tooltip owners, `NativeUiContractProbe` | Verify the installed Kingmaker inspect/tooltip contract; native route or mod-owned scrollable panel fed by native localized data; lifecycle isolation | Physical right-click opens/closes; no mutation |
| 5 | Autosave = persistence (E07–E11, E26) | `Planning/CastingAuthoringService.cs`, `CastingWorkspaceSession*.cs`, `Persistence/CastingPlanRepository.cs`, `AtomicFile.cs` | One mutation→persistence boundary with monotonic revisions, campaign-bound single writer, flush-before-run/close, no save storms from browsing, failure keeps in-memory + last good | Edits persist with no Save; restart reconstructs |
| 6 | One-click run, no ceremony (E12–E15) | `Planning/CastingReviewCoordinator.cs`, `CastingExecutionGate.cs`, `UI/BuffPlannerHudButtonController.cs`, `CastingExecutionHost`, hotkey/spellbook entries | One run service: flush latest revision, fresh snapshot, validate exact intent, immutable snapshot, revalidate per cast; moon left-click = Long always, editor stays closed; remove Save/Accept/Review&Apply; one active-run guard | Cold moon click runs Long only |
| 7 | Share Transmutation end to end (E16–E20) | `Planning/CastingTargetingModifiers.cs`, `GameAdapters/KingmakerShareTargetingModifier.cs`, `Compatibility/BrownFurShareTransmutationCompatibility.cs`, converter, both executors | Deliberate adapter: pure `ICastingTargetingModifier` for planning (no live toggles), verified native bridge at execution only; per-casting persisted intent; combined cost vector; cleanup verified | Beast Shape shared to a legal ally, real costs |
| 8 | Meaningful enhancements (E21, E22) | `GameAdapters/KingmakerCastEnhancementAdapter.cs`, `Domain/Planning/CastEnhancements.cs`, inspector build | Service-level relevance policy (native semantics, not name lists); irrelevant rods hidden for beneficial spells; already-selected odd options stay visible/removable | Good Hope shows only meaningful choices |
| 9 | Beneficial catalogue (E23, E24) | `Discovery/NativeCandidateClassifier.cs`, `ActionGraphScanner.cs`, `EffectOverrideRegistry.cs`/`NativeEffectOverrides.json` | Trace the LIVE catalogue path; fix classification at the shared boundary (disposition/carrier/variants); narrow verified identity override if needed; excluded saved intent stays visible + blocked | Irresistible Dance absent; Fire Shield stays |
| 10 | Global accounting preserved (E25) | existing ledger (proved at `0dd82937`) | Carry through all changes; combined Share+PC pool verified from installed implementation | Advanced live proof still passes |

## Execution order (slices from 02_Z mission §C)

1. Slice 1 = rows 1–4 + 8–9 (defaults, scroll, inspect, relevance).
2. Slice 2 = rows 5–6 (autosave + run service — one integrated change).
3. Slice 3 = row 7 (Share).
4. Slice 4 = cutover, gates at candidate, dispatcher batch (the six
   evidence groups of 02 §E), private delivery with the E-matrix.

## Evidence rules for this mission

- Matrix E01–E27 filled only from actual runs/tests; labels
  source/native-UI/FAIL/NOT-RUN/BLOCKED with run IDs + paths.
- Historical evidence keeps its own commit (`ec34705c`, `0dd82937` etc.);
  unchanged paths documented, never relabelled.
- Dispatcher + reusable console task reused; no new infrastructure.
- Windowed mode and the unreproduced historical hover remain separate
  known limitations.

## Status (updated as slices complete)

### Session 2026-09-29 (later) — slices 1 and 2 complete; Share next

- Slice 1 COMPLETE: right-click native spell descriptions (`fb51138`);
  rod relevance policy (`33476c7`); harmful-ally-disposition catalogue
  fix (`e3e1130`) + excluded-source persistence regression (`89241a9`).
- Slice 2 COMPLETE (`2d342e0`, protocol 367/367): autosave through the
  authoring service's revision-announced mutation boundary (settings and
  Undo included; blocked/unreadable data never overwritten; MAX_PATH
  robustness fix in AtomicFile temp names); run-authorization
  (`CastingReviewCoordinator.AuthorizeRun`) — the deliberate Run/Apply
  authorizes the exact current revision (digest-exact, run-path only);
  moon left-click runs Long in casting-first (hold opens the editor);
  everyday footer = passive save status + Undo + Run <routine>.
- NEXT (slice 3, Share): the pure pipeline is READY —
  `TargetingModifierSelection` persists,
  `ExplicitCastingCompiler.ApplyTargetingModifiers` applies registered
  pure modifiers with unresolved/unknown/unavailable blocks and folds
  `ModifierUsageDemand` into the atomic cost vector. Remaining work:
  (1) pure `ShareTransmutationCastingModifier` (planning-side; expands
  the option's reachable targets to verified legal allies via the
  Brown-Fur snapshot facts; distinct refusal reasons: self-only,
  feature-missing, source-unsupported, reservoir-short; reservoir
  demands); (2) wire into `CurrentCastingWorkspaceInputs`'
  `targetingModifiers` argument (currently null) from the verified
  Brown-Fur compatibility snapshot; (3) UI: Share control in "Next
  casting" AFTER caster/source but BEFORE target selection, and on an
  existing casting's inspector (persisted per-casting intent; disabling
  keeps an ally target visibly blocked); (4) execution-phase native
  bridge adapting the verified `KingmakerShareTargetingModifier`
  pattern (arm exact toggle → cast once → restore on ALL terminal
  paths, never swallow restoration) behind the pure modifier's
  execution hook; (5) combined Share+PowerfulChange reservoir demand
  verified against the installed implementation (both draw the arcane
  reservoir — verify actual pool identity once, reserve once).
  Reference: docs/INSTANT-SHARE-FAILED-VALIDATION.md (the prior
  validation failure and what the bridge proved).

- [x] Package read; branch `codex/kingmaker-buff-planner-everyday-use`
      created from `dd50ed1`; state reconciled.
- [x] Map + milestone record committed `8a8a996`.
- [x] Slice 1a — casting-first default, Classic switch retired
      (`f94e560`: PlannerModeStore defaults casting-first; explicit
      classic honored + changeable; toggle never replaces refused files;
      in-planner 'Use the Classic planner' button removed).
- [x] Slice 1b-1 — Instant default (`4ff89c4`: ExecutionProfile.Default
      now instant, single default site; explicit animated preserved;
      regressions updated: migration lands instant, new plan starts
      instant, single toggle lands animated).
- [x] Slice 1b-2 — continuous scroll (`867558f`: book sprite retired,
      page=continuous-scroll;book-art-retired evidence; no donor read).
- Protocol suite 365/365 at each step. Gates NOT yet rerun for the
  branch (no game run staged yet — gate before the first live batch).
- [ ] Slice 1b-3 — right-click full spell description (verify the
      installed Kingmaker inspect/tooltip contract: right-click on a
      catalogue row / selected-spell header / casting spell; native
      route or mod-owned scrollable panel fed by native localized data;
      lifecycle isolation; no authoring side effects).
- [ ] Slice 1b-4 — enhancement relevance service (hide irrelevant
      Piercing/Persistent/Selective for beneficial spells like Good
      Hope; keep Extend/Powerful Change where eligible; selected
      irrelevant options stay visible/removable; backend validation
      agrees with offers).
- [ ] Slice 1b-5 — beneficial catalogue: trace the LIVE catalogue path
      for Irresistible Dance (audit/export classifier vs live graph
      catalogue); fix at the shared structural boundary (disposition /
      carrier/marker / variants); narrow verified identity override via
      EffectOverrideRegistry only with documented exception + regression;
      excluded saved intent stays visible + blocked (E23/E24).
- [ ] Slice 2 — autosave + one-run-intent (see map row 5-6; the
      acceptance-contract removal in RuntimeTestHost's
      workspace-budget-evidence is already scenario-scoped).
- [ ] Slice 3 — Share Transmutation (adapter over the pure
      ICastingTargetingModifier contract; never copy the legacy
      toggle-swallowing pattern into planning; execution-time native
      bridge only; combined Share+PowerfulChange reservoir demand from
      the installed implementation).
- [ ] Slice 4 — cutover, full gate at candidate, dispatcher batch (six
      evidence groups of 02 §E), private delivery + E01-E27 matrix.

Continuation note: work tree must stay clean for the gate; the
dispatcher/build-jobs/freeze flow is proven (see
kbp-operator-note-casting-graph-20260926.txt and dispatch\history).
