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

### 2026-10-03 — private preview delivered (Claude takeover)

Delivered candidate 8d7681d0 (full gate PASS, r15 41/41 native jobs done and
verified on it), uploaded to the private draft release as
`KingmakerBuffPlanner-0.2.0-rc6-everyday-use-v1.2+8d7681d0.zip` and verified
after download; prior beta asset untouched. Defects D1-D13 and harness
findings H1-H5 found and repaired along the way (journal checkpoint
2026-10-03). Row verdicts: `docs/E01-E27-ACCEPTANCE-MATRIX.md` (original row
definitions). Owner acceptance of the everyday-use changes is pending the
self-paced trial.

### Session 5 (2026-09-29 night) — review F1–F5 + E1 of f0483fc repaired

Source: `KBP_Z_Review_f0483fc_2026-09-29.md` (request changes; §1–§5 of
the earlier addendum preserved, not redone).

- **F1** — Failed-flush intent now has an explicit production owner:
  `CastingSessionOwner` (pure, no Unity) performs every controlled
  transition the root makes (`Ensure` for campaign resolve/switch,
  `Release` for teardown); a session whose discard-time flush fails is
  CAPTURED (`PendingSessionRecovery`: latest document + settings, bound to
  its original mod path and campaign) and REGISTERED with the
  process-wide `CastingWorkspaceRecoveryStore` BEFORE ownership passes.
  The root's session factory adopts a pending recovery for the exact
  campaign (`Take`), and the adopting session immediately retries the
  save (clean on success; honestly dirty + retryable on failure). Root
  teardown registers the same way, so a replacement root in the same
  process recovers it. Regression runs the PRODUCTION owner with ONE
  shared mod dir and campaign-keyed files, drops every old-session
  reference, and recovers through the owner/factory alone: switch-to-B
  under lock (B uncontaminated, A's file untouched, failure reported with
  its campaign) → return to A recovers the exact latest intent durably;
  root teardown under lock → a REPLACEMENT owner adopts the intent;
  storage healed before the discard → flush succeeds, nothing registered.
  Mutant (register dropped): caught.
- **F2** — ShareCapability now carries the integration's immutable
  EXACT-SOURCE contract (the snapshot's ability + spellbook whitelists)
  and its own verified legal recipients; Apply refuses a non-genuine
  spellbook source, an unverified spellbook, an unverified ability, and
  keeps the verified-variant identity rule — the same applicability
  semantics as `CastEnhancementSnapshot.ApplicabilityFailure`. The graph
  paths (Add, next-casting lane, focused retarget) build the COMPLETE
  prospective casting and pass it through the same resolver the compiler
  uses (`ApplyGraphTargetingModifiers` no longer passes a null casting).
  Reordered capability/party records change nothing. Mutant (contract
  dropped): caught.
- **F3** — `IsSupportedSpell` is now IMMUTABLE (genuine spellbook +
  personal range + transmutation school from blueprint data; no live
  toggle anywhere in ordinary `Discover`/`ForCast`). The native
  TargetAnchor probe is isolated in `TryProbeShareTargeting`, which arms
  the exact toggle and REPORTS failed restoration as probe failure
  (overrides a passing observation; never swallowed). Boundary regression
  is an exact assembly-backed IL scan of the BUILT assembly: zero
  ActivatableAbility state-mutation calls (set_IsOn/TurnOn/TurnOff/Stop/
  set_ResourceCount) in every ordinary discovery method of the
  compatibility type and the enhancement adapter; the probe is the ONLY
  method that arms, and doubles as the scan's positive control.
- **F4** — Legality is separated from affordability: a verified-zero (or
  unknown) remaining balance no longer refuses Apply; the capability and
  its cost shape survive any balance, and the compiler/ledger decides
  (AlreadySatisfied for a sufficiently active effect; atomic resource
  block otherwise). Compiler regressions: active effect + zero reservoir
  → AlreadySatisfied with zero reservation; missing effect + zero →
  `enhancement-pool-exhausted`; Overwrite + zero → resource block; mixed
  routine (skipped shared + ordinary fundable) keeps the ordinary
  casting's full funding. The direct modifier test now asserts zero
  balance APPLIES with an unchanged cost shape. Mutant (old refusal
  re-added): caught.
- **F5** — `TryReserveAtomically` fails CLOSED on enhancement pools it
  has no verified balance for: a demanded pool absent from the ledger
  (`enhancement-pool-unknown`) or reporting an unknown balance
  (`enhancement-balance-unknown`) can never fund — the commit pass can no
  longer record a demanded-but-unknown cost as allocated. Integrated
  compile/budget regressions (real enhancement + resource snapshots): two
  allies = two invocations with combined reservoir accounting; 1-use
  reservoir funds the first and blocks the second with the real shortage,
  no partial reservation anywhere; Share + Powerful Change on ONE shared
  pool validate as combined demand (agreeing snapshots; conservative MIN)
  and a shortage blocks atomically while a later ordinary casting still
  funds; unknown/missing balances block. Mutant (skip restored): caught.
- **E1** — R3's submitted assertions now read the boundary's own
  `LastProjection`: exact ordered casting ids and executor steps, each
  step's target/provider/caster/source identities and reserved native
  cost, projection identity present, the returned projection is the
  boundary's object, durability from an actual fresh read, and the
  submitted projection is IMMUTABLE after a later editor mutation (which
  is itself durable).
- The converter's targeting-modifier refusal and the compiler's
  `enhancement-changes-targeting` block are INTENTIONALLY untouched
  (fail-closed until the §6 execution contract exists).
- State: protocol 379/379; full source-only gate at the exact candidate;
  commits c1..c3 + plan; guarded push.

### Session 4 (2026-09-29 late) — review addendum §1–§5 all repaired and pushed (b1d1aef, 375/375)

- §1 (`3f5c2b9`): PushHistory trim keeps the NEWEST 64 in LIFO order (the
  old code kept the oldest and inverted the stack — 65 edits' first Undo
  restored the near-empty start and autosaved it). Regression: exact
  contents after every Undo through the 65-edit floor, a 66-edit repeat,
  honest refusal below the floor, fresh-session durable read.
- §2 (`3f5c2b9`): AcknowledgeImportNotices announces (autosaves +
  survives fresh load); teardown and campaign-switch attempt durability via
  FlushSessionForDiscard/RetryFailedSave, keep the retained session on
  failure (recoverable under its ORIGINAL campaign), never relabel.
  Regressions: locked-save retry refused/healed; A-failure → switch → B
  clean → A recovers; acknowledgement persistence.
- §3 (`c973c1c`): Share is ONE registration resolving the exact casting's
  caster from VERIFIED capability facts (the targeting-affecting
  enhancement snapshots the installed-provider integration produced —
  feature ownership, own reservoir identity, per-use cost and remaining
  from the installed contract). Caster-exact costs regardless of party
  order; incapable/exhausted refused honestly; unverified never free.
- §4 (`c973c1c`+`b01a49b`): fixture identity fixed (one expression
  instance under both catalogue keys — production matching unchanged); the
  production defect it exposed repaired — AddGraphCasting, the
  next-casting target lane and focused retarget all resolve the
  draft's/focused casting's selected modifiers through the same pure
  semantics as the compiler BEFORE eligibility. The graph-gesture
  regression runs the production path: caster/source → Share before any
  connection → ally → exact persisted per-casting modifier intent; second
  ally = second record; Share-off keeps the ally target blocked/
  repairable through reload.
- §5 (`b1d1aef`): R1 verifies durable state after EVERY step; R2 edits
  after the second reload replacement and proves genuine Undo, live-vs-
  durable comparison; R3 holds the boundary by direct reference, asserts
  both castings, the gate's exact ordered executable set, and the
  submitted castings' exact targets/caster/source.
- §6 REMAINS: Share UI control (Next-casting + inspector toggle — session
  command surface exists via Draft.TargetingModifiers +
  UpdateFocusedCasting); execution-only native bridge (arm exact verified
  toggle → cast once → restore on ALL paths); converter currently refuses
  targeting-modifier castings fail-closed — extend it to represent/execute
  the complete contract (do NOT just remove the restriction); combined
  Share+PowerfulChange reservoir live proof; Classic route-map completion
  (HUD/hotkey/spellbook/settings all → graph); gates at candidate;
  unattended batch (also judges scroll frames + right-click live);
  E01–E27 matrix; private delivery.

### Session 3 (2026-09-29 night) — R1–R3 repaired; Share planning pipeline in

- **R1–R3 REPAIRED** (`dd8fb79`, protocol 370/370 then 371/371): ONE
  persistence entry point (PersistNow) for document + settings changes
  (R1: no cross-counter suppression possible); ReplaceAuthoring detaches/
  reattaches the autosave subscription at EVERY authoring-instance change
  — ctor load/import paths and both Reload() branches (R2); Apply now
  flushes once and refuses with `persistence-failed:<reason>` when the
  current revision cannot persist, empty-plan no-op exempt, zero
  submissions while undurable (R3). Regressions in
  `tests/KingmakerBuffPlanner.Tests/AutosaveLifecycleTests.cs`: real
  authoring+repository services, interleaved settings/edits/Undo with
  fresh-session file verification, reload-then-edit x2 + Undo, and a
  genuine OS file-lock failure → Run refused (0 submissions) → recovery →
  one deliberate Run submits the latest intent. No manual Save anywhere.
- **Share planning pipeline IN** (uncommitted-to-pushed flow, 371/371):
  pure `GameAdapters/ShareCastingModifier.cs` (personal-spell expansion to
  verified legal allies; deterministic; refuses share-not-needed and
  share-feature-unavailable with reasons; declares the verified reservoir
  demand 1/use in the atomic cost vector — same pool as Powerful Change);
  wired into `BuffPlannerUiRoot.CurrentCastingInputs` (one registration
  per party unit from the snapshot); `AddGraphCasting` now carries
  `Draft.TargetingModifiers` (Share selected BEFORE target survives into
  the record). Source regressions in
  `tests/KingmakerBuffPlanner.Tests/SharePlanningTests.cs`:
  `share-expands-personal-targets-purely` PASSES (expansion, purity,
  not-needed refusal, demand, no-allies refusal).
- **EXACT RESUME POINT**: `share-graph-gesture-carries-and-persists`
  (complete test body in SharePlanningTests.cs, Run line commented with
  the reason) fails because the synthetic party fixture yields ZERO
  caster nodes from `session.BuildGraph` — the fixture's effect
  expression / provider option must satisfy the live catalogue scan
  (see `BuildSourceOptions`/`OptionServesExpression` serving rule; likely
  the AbilityKey Canonical format or EffectLeafExpression target/kind
  must match what `GraphCapableCasters` accepts — copy the shape from
  CastingGraphTests' `GraphAbilityA` fixtures which DO serve). After it
  passes, the remaining Share work: UI toggle (Next-casting Share control
  after caster/source before targets + inspector toggle via
  UpdateFocusedCasting), execution-phase native bridge (arm exact
  verified toggle → cast once → restore on ALL paths; never swallow
  restoration), combined Share+PowerfulChange reservoir verification
  against the installed build, then E16–E20 live.
- Still open: Share UI + native execution + combined-cost live proof;
  Classic cutover route map completion; gates + unattended batch +
  E01–E27 matrix + private delivery (slice 4).

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
