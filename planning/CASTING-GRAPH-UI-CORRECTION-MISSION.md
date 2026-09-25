# Claude Mission
## Prove the Active Build/Mode, Then Correct the Casting-First Graph Workspace

Repository:

`https://github.com/howardreith/KingmakerBuffPlanner`

Governing documents:

1. `Kingmaker-Buff-Planner-Casting-First-Migration-Charter.md`
2. `Kingmaker-Buff-Planner-Casting-Graph-UI-Addendum-v1.1.md`

Owner: Howie

## 1. Owner decision and immediate release status

Howie manually opened the planner in his normal game at 1920×1200 and rejected the observed experience.

Observed screen characteristics:

- a dominant four-column buff grid;
- Long / Important / Short tabs;
- `Casters: Automatic`;
- target portrait toggles;
- `Enhancement: None`;
- `Edit Assignments`;
- a separate `Assignments & Resources` button;
- low-contrast text on gray stone buttons.

Observed defects and product feedback:

1. Hovering a target creates the correct hover plus a second visible hover effect offset up and left of the cursor.
2. Gray stone button text is too low contrast and visually unattractive.
3. The workflow is unclear, too similar to Bubble Buffs, and worse than the intended product.
4. The desired primary flow is:
   - select a buff;
   - select a caster/source;
   - select a target;
   - create a visible caster-to-target casting connection;
   - select that connection to configure enhancements such as Extend or Powerful Change.
5. Caster capacity and enhancement resources must be tracked globally across the complete plan, including shared spontaneous spell-level pools.

The currently observed UI is not owner-accepted. Do not merge, publish, tag, promote, or permanently install a release candidate on the basis of the prior automated UI evidence.

However, do not yet assert that the screenshot proves a regression in the casting-first workspace.

The last recorded candidate procedure left the normal installation on an earlier version and kept casting-first opt-in. The screenshot’s labels strongly resemble the Classic/legacy screen. The first duty is to prove exactly what binary and mode Howie is viewing.

## 2. Mission outcome

Deliver a focused, reviewable correction that:

- proves the active build and planner mode;
- preserves the valuable casting-first domain, compiler, persistence, budget, and execution work;
- replaces or repairs the casting-first primary composition so it follows the owner-adopted graph contract;
- fixes the duplicate/offset hover defect at its cause;
- fixes button-text contrast across states;
- proves authoritative global capacity/resource updates;
- produces a manual-review development build;
- stops for Howie’s visual/usability verdict before broad release qualification.

This is not a clean-room rewrite and not a new test-harness program.

## 3. Branch and PR strategy

After reconciliation, create a new focused branch from the current remote head of:

`codex/kingmaker-buff-planner-casting-first`

Use:

`claude/casting-graph-ui-correction`

Open a new draft stacked PR with base:

`codex/kingmaker-buff-planner-casting-first`

Suggested title:

`Casting-first graph workspace: caster → casting → target authoring`

Do not put another large UI rewrite directly onto the already oversized integration PR without a separate review surface.

Do not close or merge PR #2 during this mission.

PR #1 may be closed as superseded only if ancestry is reverified and that housekeeping has not already been done.

Do not create a release candidate version or tag until the owner accepts the corrected workspace.

## 4. Phase 0 — Reconcile and prove what Howie is viewing

Before modifying code:

1. Fetch remote branches and inspect all worktrees.
2. Preserve unknown local changes.
3. Record:
   - current `main`;
   - PR #2 head;
   - new branch base;
   - installed KingmakerBuffPlanner version;
   - loaded DLL SHA-256;
   - loaded MVID;
   - package/commit identity if embedded;
   - current `planner-mode.json`;
   - effective planner mode;
   - active view/controller class when the screenshot-like screen is open.
4. Determine whether the observed screen is:
   - the old installed 0.1.1-rc3 preview;
   - Classic mode from rc6/current source;
   - casting-first mode misrouted to the legacy view; or
   - the actual casting-first workspace.
5. Record the conclusion with direct evidence. Do not infer it from labels alone.

Add a visible mode label to development builds if one does not already exist:

- `Planner: Classic`
- `Planner: Casting-first`

This label must come from the effective route, not merely the saved setting.

### Conditional result

If the screenshot is an old build or Classic mode:

- say so plainly;
- do not call that alone a casting-first regression;
- safely stage the appropriate development build through the existing guarded transaction;
- activate casting-first deliberately;
- capture the actual casting-first workspace at 1920×1200;
- compare it against the addendum.

If the actual casting-first workspace already satisfies any requirements, preserve them. If it does not satisfy the addendum, continue with the correction below.

Do not ask Howie to spend more time reviewing the wrong build or mode.

## 5. Preserve the backend; correct the primary composition

Start from the existing production contracts, including where applicable:

- `CastingPlanDocument` and schema-6 profiles;
- `CastingAuthoringService`;
- `CastingWorkspaceSession`;
- `ExplicitCastingCompiler`;
- `CastingBudgetLedger` / existing sequence forecast;
- provider and enhancement discovery;
- `CastingExecutionHost`;
- review/preflight;
- guarded persistence and rollback.

Do not fork a second writable casting model.

Do not put resource allocation, targeting legality, or caster substitution into the view.

A UI change must remain a command/read-model client of the canonical services.

Before replacing a backend contract, demonstrate a concrete addendum requirement that the current contract cannot express.

## 6. Required corrected workspace

### 6.1 Overall layout

At 1920×1200, when a buff is selected:

- keep a narrow searchable buff catalogue on the left or in another restrained navigation region;
- display the selected buff prominently;
- display routine selection without dominating the page;
- use fixed lanes for:
  - casters/sources;
  - explicit casting connections/cards;
  - targets/beneficiaries;
- keep a contextual casting inspector at the right or as a parchment side sheet;
- keep the global budget/conflict summary and primary actions reachable in the footer.

The selected-buff graph is the primary authoring surface.

The old four-column catalogue with a bottom selected-buff panel is not the accepted casting-first composition.

Do not delete Classic mode merely to simplify this mission, but make mode identity unmistakable.

### 6.2 Buff → caster/source → target

Implement the owner’s ordinary direct-target sequence:

1. Select a buff.
2. Select a caster.
3. If the caster has several materially distinct sources, select the exact source.
4. Legal targets become available.
5. Clicking a legal target creates one `PlannedCasting`.
6. A visible connection appears from caster/source to target.
7. Focus moves to that casting without changing sibling castings.

Selection/focus before step 5 must not mutate the document.

`Automatic` is not the primary new-casting choice. Preserve it only as unresolved migrated intent if required.

### 6.3 Caster/source capacity

For the selected buff, every capable caster remains listed.

Under each caster, show exact source rows and current additional fundable casts after global reservations.

Examples:

- `Bard level 2 — 3 casts remaining`
- `Wizard prepared slot — 1 exact slot ready`
- `Mutagen — 0 / 1 remaining`
- `Unlimited`
- `Blocked: material unavailable`

Do not sum sources that share a pool unless the aggregate is proven non-overlapping.

Use the existing authoritative planning budget. No view-local counters.

A count must refresh when any relevant casting anywhere in the plan is added, removed, moved, enabled, disabled, or enhanced.

### 6.4 Casting connections

For each direct casting:

- render a restrained visible ink line from caster/source anchor to target anchor;
- render a midpoint casting chip/card containing enough identity/status to distinguish it;
- provide a wide invisible pointer hit corridor, approximately 20–28 logical pixels where layout permits;
- make both the corridor and chip select the same casting;
- highlight the complete route on selection;
- preserve other castings visibly.

Do not require pixel-perfect thin-line clicks.

Do not add an external UI package. Use mod-owned Unity UI objects in the existing canvas. A rotated/sized `Image` segment or another project-consistent Unity UI implementation is acceptable. The visible segment and hit corridor should be separate graphics so readability and pointer usability are independently controlled.

Do not update line geometry through global discovery every frame. Recompute only when relevant layout, scrolling, selection, or document state changes.

Parallel castings between one caster and one target remain separately selectable through offset lines, stacked chips, or another unambiguous treatment.

### 6.5 Clicking a casting opens enhancements

Selecting the line/chip opens that exact casting’s inspector.

Show only verified enhancements applicable to its caster, source, ability, and targeting shape.

For each enhancement show:

- full name;
- mechanism/source;
- required/optional semantics where applicable;
- actual cost;
- globally allocated and remaining resource;
- expected strength/duration/targeting change;
- unavailable reason.

Changing an enhancement:

- edits one casting only;
- preserves sibling casting identity;
- recompiles through production services;
- updates all affected global counts;
- supports Undo;
- never silently substitutes another enhancement/source;
- never leaves a native toggle active during mere authoring.

At minimum exercise the installed fixture’s verified enhancements, including Extend and Powerful Change where available.

### 6.6 Global budget behavior

The authoritative displayed plan budget spans all enabled castings in the explicit one-pass routine order already supported by the project.

Prove at least:

1. A spontaneous caster has a shared spell-level pool.
2. Adding one casting for Buff A reduces remaining capacity displayed for Buff B using the same spellbook level.
3. Deleting or disabling that casting restores capacity.
4. An enhancement sharing a class-resource pool updates every affected enhancement choice.
5. A casting requiring several resources reserves atomically or not at all.
6. A preview refresh never serves as acceptance.
7. The selected run and global one-pass views are labelled distinctly if both are exposed.

Do not claim the global one-pass forecast models a whole day, rests, or repeated Short routines.

### 6.7 Group casts

Keep one explicit casting card/connection for a group ability.

Represent origin and required coverage separately.

Use dashed/patterned derived beneficiary branches. These branches do not represent extra records or costs.

Show missed intended coverage and never silently author a second group cast.

## 7. Hover bug — root-cause mission

Reproduce the owner-observed target hover defect before fixing it.

Capture:

- active build/mode/view;
- target portrait hierarchy;
- all active `Selectable`, `EventTrigger`, pointer handlers, and transition graphics;
- event-system raycast results under the cursor;
- canvas render mode, camera, scaler, and scale factor;
- pointer screen position;
- local position calculations;
- active hover/highlight object count and anchored positions;
- stale objects/listeners across close/reopen and refresh.

Plausible causes may include duplicate overlays/listeners, a native transition plus a custom transition, incorrect canvas camera, coordinate conversion against the wrong rect, or stale rebuilt controls. These are hypotheses only. Prove the actual cause.

Fix the cause rather than hiding the second graphic.

Add regression coverage that fails when:

- more than one hover owner is active;
- a highlight is not aligned with its target;
- pointer exit leaves a highlight;
- close/reopen increases listener or highlight counts.

Run a live pointer sweep over every visible target at 1920×1200 and 1920×1080. Exactly one highlight may appear, on the hovered target.

## 8. Button contrast and state contract

Inventory the actual normal/hover/pressed/selected/disabled background and text colors used by the gray stone controls.

Add a small pure contrast utility/test if the project has no equivalent.

Acceptance:

- normal actionable text contrast at least 4.5:1;
- large actionable text may use 3:1;
- hover and pressed remain readable;
- selected state is clear;
- disabled state is visibly disabled but legible;
- captions do not disappear into gray/brown stone;
- full labels remain intact.

Prefer a crisp warm-ivory or other native-compatible high-contrast treatment with a restrained shadow/outline only when it improves readability. Do not preserve low-contrast donor text merely to claim native styling.

Capture the five states in the actual game.

## 9. Persistence and migration behavior

The graph reconstructs entirely from canonical casting records.

Do not persist:

- line coordinates;
- portrait positions;
- current hover state;
- resource balances;
- predicted beneficiaries;
- Unity object references.

Save/reload must preserve:

- casting IDs;
- caster/source;
- direct target or origin;
- required coverage;
- routine/order;
- enhancements;
- existing-effect policy;
- enabled/draft state;
- migration provenance.

Legacy automatic or pooled choices remain visible unresolved intent. Do not silently resolve them through the new graph.

## 10. Testing style and bounded evidence

Match the project’s existing C# 7.3/.NET Framework style.

Prefer production-service integration tests with real serialization and real authoring/compiler objects. Avoid mock-heavy tests. Use fakes only at genuine Unity/game-native boundaries.

Required source/integration scenarios:

- add direct casting through real session command path;
- edit one connection without changing siblings;
- remove and undo;
- multiple sources and non-double-counted capacity;
- spontaneous global pool propagation across two buffs;
- shared enhancement pool propagation;
- atomic multi-resource refusal;
- parallel same caster-target castings;
- group origin and beneficiary branches;
- save/reload reconstruction;
- hover ownership/alignment policy;
- contrast thresholds;
- listener/object count stability.

Required guarded live UI evidence before owner handoff:

- 1920×1200 selected buff with at least two casters and several connections;
- 1920×1080 equivalent;
- one selected connection with enhancement inspector open;
- global budget change before/after adding a spontaneous cast;
- normal/hover/pressed/selected/disabled button states;
- pointer sweep with no ghost hover;
- final rows/long labels reachable;
- close/reopen and save/reload.

Use an approved disposable fixture for automated runs. Do not automate a valued normal save.

Do not rerun the complete historical finite/group/Mutagen/rod gameplay chain during UI iteration unless production planning or execution semantics changed.

## 11. Owner checkpoint

When the corrected development build is safely staged and the manual workspace is ready, stop automated interaction and invite Howie to review it.

His checkpoint is specifically:

- Does buff → caster/source → target feel obvious?
- Does each visible line clearly mean one casting?
- Is selecting a line/chip easy?
- Are enhancements understandable and scoped to one casting?
- Do caster counts and global resource changes make sense?
- Is the interface materially better and clearer than Bubble Buffs?
- Are button captions readable?
- Is the phantom hover gone?
- Does the page look like a coherent Kingmaker planning ledger?

Do not infer acceptance from automated callbacks or screenshots.

If Howie rejects the composition, record his exact feedback and stop. Do not freeze another RC.

If Howie accepts it, report that the UI checkpoint passed. Release qualification remains a separate bounded mission.

## 12. Safety and authority

Authorized in this mission:

- read-only reconciliation;
- a new focused branch and stacked draft PR;
- product/UI code and tests necessary for this addendum;
- guarded development package staging;
- approved disposable-save runtime tests;
- temporary planner-mode changes with restoration;
- documentation updates accurately recording the owner rejection of the prior observed UI.

Not authorized:

- merge to `main`;
- publication, tagging, release, or stable promotion;
- permanent installation;
- deletion of Classic;
- destructive history changes;
- testing automation on a valued normal save;
- weakening locks, transactions, rollback, or save protections;
- expanding into Share Transmutation, exact physical rod identity, pets, or unrelated charter gaps;
- another generalized harness redesign.

On every guarded run:

- acquire the shared installation lock atomically;
- recheck other project locks and running Kingmaker;
- prove staged artifact identity;
- preserve and compare Mods and approved saves;
- restore on all terminal paths;
- stop immediately on incomplete restoration.

## 13. Documentation

Add the adopted addendum to the repository under an appropriate planning/docs path.

Update the current migration status so it says:

- the prior rc6 backend/execution evidence remains historical evidence;
- the owner rejected the observed UI as the release experience;
- exact screenshot build/mode identity was [proved result];
- a focused graph-workspace correction is underway;
- no release or merge is authorized.

Do not rewrite historical receipts to pretend they observed the new UI.

Do not commit transient watcher/preflight status repeatedly.

## 14. Final report

Lead with one of:

- `IDENTITY PROVED — CLASSIC/OLD UI; GRAPH CORRECTION IMPLEMENTED`
- `IDENTITY PROVED — CASTING-FIRST UI; GRAPH CORRECTION IMPLEMENTED`
- `IDENTITY PROVED — CORRECTION PARTIAL, BLOCKED`
- `NO SAFE RUNTIME ACCESS — SOURCE WORK COMPLETE, LIVE CHECK BLOCKED`
- `OWNER ACCEPTED GRAPH WORKSPACE`
- `OWNER REJECTED GRAPH WORKSPACE`

Then report:

1. Exact original screenshot identity:
   - loaded version;
   - DLL SHA-256;
   - MVID;
   - planner mode;
   - active view/controller.

2. Branch and PR:
   - base;
   - head;
   - commits;
   - changed files;
   - draft PR URL.

3. Product changes:
   - primary workflow;
   - caster/source capacity;
   - connection/card behavior;
   - enhancement inspector;
   - global budgets;
   - group representation;
   - hover fix;
   - contrast fix.

4. Tests:
   - exact source/integration counts;
   - exact guarded live run IDs;
   - pass/fail/not-run;
   - no inflated claim from synthetic evidence.

5. Screens/evidence:
   - 1920×1200;
   - 1920×1080;
   - selected connection;
   - global count update;
   - button states;
   - hover sweep;
   - save/reload.

6. Owner verdict, quoted exactly if provided.

7. Safety:
   - final installed version;
   - Mods comparison;
   - save comparison;
   - transaction state;
   - locks/processes.

8. Remaining limitations:
   - distinguish addendum blockers from deferred full-charter work.

## Operating principle

The prior migration produced valuable explicit-casting, budgeting, execution, and safety foundations. Preserve them.

The owner’s rejected screen is not repaired by more qualification of the same composition. Prove which screen he saw, then build the intended product:

**buff → caster/source → target → one visible casting connection → per-casting enhancements, with one global authoritative budget.**
