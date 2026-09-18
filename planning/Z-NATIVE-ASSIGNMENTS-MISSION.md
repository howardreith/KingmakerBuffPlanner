# Z Mission — Kingmaker Buff Planner: Native UI and Precise Casting Assignments

## Mission

Implement the Buff Planner overhaul described below. This is an implementation mission, not a request to produce another high-level proposal. Inspect the current repository and installed contracts, make the changes, test them, create coherent commits, and leave a reproducible candidate package plus a precise handoff.

The player must be able to configure **which caster casts a spell on which targets, with which enhancements**, inspect how limited resources are allocated across spells, reach every enhancement option, and open the planner from the spellbook. The planner must look and behave like Kingmaker's native parchment/book UI.

Keep the automatic workflow easy. Advanced control must not require duplicating the spell catalog or maintaining a second configuration that can disagree with the simple interface.

Work through Checkpoints A–F. Continue between checkpoints without requesting routine approval. When a concrete prerequisite blocks one lane, record the blocker and continue independent work; do not bypass safeguards or label blocked acceptance as complete.

## 1. Baseline, authority, and operating boundaries

### Establish the actual baseline

Primary repository: `howardreith/KingmakerBuffPlanner`.

Reference snapshot used for the approved plan: `fd0e6dc1c32dfc929a56dbc575163e641b150746`, manifest version `0.0.19`.

Styling reference: `howardreith/KingmakerDiceRoller`, reviewed at `f18560337973e05510e4f747109a6bff6f90e66a`, including native theme recovery and full-caption sizing.

These are historical review anchors, not assertions about current HEAD, the installed DLL, or unpushed work. Inspect remotes, branches, worktrees, local modifications, version metadata, and installed artifacts before choosing the implementation base. Use the existing Buff Planner lab/checkout. `C:\Dev\KingmakerBuffPlannerLab` is a discovery hint only; verify the actual machine and path.

Read `AGENTS.md`, applicable parent/local instructions, the current architecture, execution semantics, build/release instructions, runtime harness documentation, qualification records, `AUTONOMOUS-RESUME.md`, and `AUTONOMOUS-BLOCKERS.md`. Read the attached implementation plan when available. This mission contains the required product decisions and is sufficient to begin without that attachment.

Use a dedicated branch consistent with repository rules, such as `codex/kingmaker-buff-planner-z-native-assignments`. Preserve unknown work. Do not reset, clean, force-push, or overwrite another agent's branch/worktree. Reconcile relevant newer work before coding; do not blindly revert to the review anchor.

### Scope of work

Implement and test within this repository, create reviewable commits, and prepare a local candidate release package. Push only through the repository's maintained guarded helper when its authorization and preflight requirements are satisfied. **Do not merge to main, create release tags, or publish a release without separate explicit authorization.** Historical permission for a previous release does not authorize this release.

Keep the standalone .NET Framework 4.7 / C# 7.3 / installed legacy Harmony12 boundary, subject to verified current repository contracts. No new gameplay-mod dependency, UI framework, downloaded replacement Unity/TMP package, or generic orchestration framework is needed. Do not change Dice Roller or another gameplay mod as a side effect of this mission.

Preserve the existing discovery, effect-presence, slot, material, metamagic, targeting, duration, and native resource rules. This is not permission to rewrite buff eligibility or implement direct buff injection.

### Runtime safety

Use only project-owned guarded build, staging, deployment, and runtime scripts. Do not replace the live Mods tree manually, overwrite unrelated settings/mods, kill unrelated processes, or disrupt a user-controlled game session. Respect the current harness's desktop, locking, rollback, and account-UI boundaries.

Only an existing designated `KBP_AUTOMATION_WORKING` fixture may be mutated; `KBP_AUTOMATION_BASELINE` stays immutable and other campaign saves stay protected. Do not substitute or relabel an ordinary player save. If an authorized fixture is absent, continue source/build/assembly work and mark the affected live lanes BLOCKED or NOT RUN. Do not invent acceptance evidence from a launch, compilation, or detached assembly test.

## 2. Product contract

### A. One catalog entry, multiple casting assignments

Keep one catalog entry per existing consolidated spell/source. Beneath that source, introduce child casting assignments with:

- A stable assignment identity and routine-wide allocation order.
- Automatic or pinned caster, and optional exact spellbook/provider constraints.
- An ordered set of explicit target identities.
- Per-assignment enhancement selections, including required/optional behavior.
- A preview of resolved casts, costs, coverage, and specific unavailable reasons.

Default interaction remains one Automatic assignment with selected portraits. Add Assignment and Split Assignment expose finer control. All interfaces edit the same underlying assignments; source-level portrait/membership summaries are derived, not separately writable copies.

Canonical acceptance example:

| Caster | Explicit targets | Requested enhancements |
| --- | --- | --- |
| Leinna | Leinna | None |
| Felix | Felix | None |
| Felix | Tias, Raine | Share Transmutation |

In a fixture where the actual installed contracts permit these casts and resources are sufficient, the plan contains four casts: one by Leinna and three by Felix. Share is requested only for Felix's two non-self casts. Use the example names only in fixtures/documentation, never as production routing logic. The installed provider determines eligibility and real costs.

A pin is a hard eligibility constraint, not a preference. An unavailable pinned caster/provider/item remains visible and unresolved. Do not silently replace it. In Automatic mode, show the actual chosen caster, spellbook/source, target, enhancement source, and cost in the resolved preview. Preserve existing bans, priorities, and cap semantics as defaults; diagnose conflicts rather than silently bypassing them.

Selecting a target explicitly assigned elsewhere offers an explicit move, performed atomically, instead of silently duplicating or removing it. Retain native mass/communal coverage: several portraits can be covered by one actual cast. Do not merge casts across assignments whose caster, enhancement, or other meaningful constraints differ. Incidental native area coverage is not itself a duplicate explicit assignment.

Do not silently prune targets or delete assignments when an enhancement changes, a caster disappears, equipment moves, or an optional mod becomes unavailable. Preserve intent and surface repairable validation errors. Invalid selected targets must still be removable.

### B. Enhancement source identity and shortage behavior

Support both explicit compatible-family selection and exact physical-rod selection when the installed native contract supports a durable instance identity. An exact item pin must survive save/reload; display ordinal, equipment slot, or Unity instance number alone is not an identity.

An enhancement must be usable by the resolved caster. When caster is Automatic, an owner-specific selection narrows eligible casters and the UI must explain that restriction. Contextual choices should show owner, source, applicability, live charges, and planned usage. Exhaustion must not hide existing configuration; unavailable saved choices remain visible and removable.

Do not automatically transfer/equip rods. Missing, moved, unequipped, sold, or exhausted pinned items produce specific diagnostics. Native item identity and native usage-pool identity are distinct: preserve genuinely independent charges and represent any proven shared pool only once.

Legacy same-blueprint pooled rod selections migrate as **any matching rod for the original caster**, not as a guessed exact-item pin. If durable instance binding cannot be proven, document the concrete contract blocker and preserve accurate pooled behavior; do not present enumeration-order selection as exact control or claim that requirement complete.

Requested enhancements are **required by default**. Offer an explicit “Cast without this enhancement when unavailable” policy only where omission is safe. This permission allows omission, not silent substitution of a pinned item or caster. Recompute legality for the omitted-enhancement candidate. Never drop Share or another targeting modifier when it is needed to make the recipient legal.

Where several optional enhancements interact, use a documented deterministic fallback policy, validate the whole resulting set, and show exactly what is omitted. Do not arbitrarily strip enhancements in view code. Distinguish fully enhanced casts, explicitly permitted unenhanced casts, blocked casts, and already-active skips.

### C. Visible order and one authoritative allocation result

Add a Casting Order view with numbered assignments and Earlier/Later controls. Persist routine-wide assignment order and target order. Catalog alphabetical sorting, filtering, and search must not change resource priority. Drag-and-drop is optional.

Preserve legacy source-ID ordering during migration by making it explicit; new assignments may use insertion order. Use stable final tie-breakers. Pins constrain eligibility but must not secretly jump ahead of the displayed allocation order. A global optimizer is not required.

Extend the existing resource/enhancement accounting rather than adding a UI-only counter. Produce one resolved plan that names the exact provider and enhancement sources, actual cast order, recipients, cost vector, allocation decisions, and unmet reasons. UI summaries and execution preparation consume that result.

For each native limited pool, expose available now, requested usage, allocated usage, unmet demand, and forecast remaining, with traces back to assignments/casts. Separate resource shortfalls from unavailable-caster, illegal-target, and incompatible-enhancement failures. A compatible-family request is alternative demand, not demand against every candidate rod simultaneously.

The canonical shortage fixture has nine otherwise legal enhanced single-target casts, three available charges, and no already-active skips. Show **requested 9 / available 3 / allocated 3 / unmet 6**. The first three eligible casts in the displayed order receive charges; the other six are blocked or explicitly planned without the optional enhancement. Expansion identifies the exact spell, caster, and recipient for each result.

Budget per actual cast, not per portrait. Do not reserve resources for already-active skips. Share and Powerful Change must still sum their costs when they share a reservoir. If a base ability and an enhancement spend the same actual native pool, unify their budget identity; do not independently spend the same balance twice. Reserve a cast's combined requirements atomically so a rejected candidate cannot leak reservations.

### D. Cross-routine visibility without imaginary inventories

Long, Important, and Short are templates, not separate charge pools or rest boundaries. All views read the same live native resource state.

Default to current-routine resource usage. Also expose which assignments in other routines request a given enhancement/pool. That is competing configured demand, not an already committed reservation.

Provide an explicit read-only combined forecast where the player selects and orders routines. The first implementation may support one occurrence of each selected routine, clearly labeled **one run per selected routine**. Repeated Short runs and combined execution are not required.

A sequence forecast begins with one snapshot and carries resource balances through the selected order. Carry supported projected successful effects forward so later already-active skips are not falsely charged. State success and elapsed-time assumptions, such as successful casts and no intervening combat/expiration. Unknown conditional effects must not be invented. Unsupported projections must be labeled conservative demand estimates rather than exact forecasts.

Do not sum independently full-budget previews, assume every saved routine will run, or create persistent cross-routine reservations. “Reserve N charges for Short” quotas are outside this mission. Respect existing per-run provider-cap semantics rather than silently changing them into daily caps.

### E. Preview and execution must agree

Refresh native state and resolve again immediately before execution. If the confirmed plan changes materially in caster, item, enhancement, cost, order, or coverage, present the revised result instead of silently executing a different plan. Revalidate the selected native source and costs at each cast.

Incomplete requested coverage must be visible before applying. Default Apply must not quietly execute the ready subset of an incomplete routine. Provide an explicit **Apply Ready Casts Only** action/confirmation or clearly saved equivalent policy; the HUD quick-run path must honor the same rule. Distinguish requested-target coverage from success of the subset that was actually cast.

Preserve Animated/Instant/Hybrid execution, qualified Share/provider-direct fallback, activation leases, one-shot restoration, and native debit ownership. Do not manually debit resources already owned by the game/provider transaction. Do not count speculative reservations as actual consumption.

Cancellation before spending releases only forecast reservations; cancellation or failure after native spending must not manufacture a refund. Observe effects and native resource state, report partial results accurately, and safely rebuild or stop the remaining plan without substituting user pins. Unresolved transaction cleanup must not allow a later cast to begin.

Resource displays must recover after manual casting, rest, equipment changes, party changes, and loading an older save. Persist intent, not charge counts or speculative reservations. Invalidate at meaningful events or bounded polling where no reliable event exists; no per-frame global scans.

## 3. Native presentation and spellbook integration

### Theme

Use native parchment/manila book surfaces, torn/sliced paper edges, restrained burgundy headers and trim, dark readable text, native spell icons/portraits, and genuine gray action buttons. Copy the actual normal/hover/pressed/disabled state set and transition behavior, not approximate tints or unrelated bookmark sprites.

Create a small Buff-Planner-owned theme resolver/adapter patterned after Dice Roller's capability separation, donor validation, recovery, and caption sizing. Its character-creation paths are references, not campaign locators. Inventory the actual installed campaign/spellbook donors and verify component types, owner identities, materials, typography, sprite borders, and transitions.

Borrow visual properties into owned controls at runtime. Do not clone native controller/event trees, change shared assets, destroy donors, bundle proprietary fonts/textures, or introduce a runtime dependency on Dice Roller. Use one verified native click-sound route; avoid duplicate click/open sounds.

Resolve paper, buttons, text roles, input, scrollbars, ornaments, and sound independently. Missing cosmetics may fall back readably without disabling the planner. Functional fallback is not native visual acceptance. Cache by owner identity, discard stale references, and bound retries. Handle Unity object lifetimes and control initialization ordering explicitly.

Fit controls to their fully styled captions plus padding, using verified native typography/TMP where appropriate. Reflow or wrap instead of abbreviating, ellipsizing primary controls, or shrinking below readable size. Re-measure on text/style/scale changes, not every frame. Apply this to long caster names, rod names, errors, and multiple-line enhancement rows.

Keep the catalog and assignments primary. Use a compact persistent resource summary with a larger Resource Usage view on demand. Preserve the opaque full-screen input-isolation contract independently of decorative paper edges. Do not weaken presentation/input validation to accommodate a broken theme.

### Enhancement overflow

The reviewed chooser already creates a ScrollRect. The suspected defect is content sizing and absent visible scrollbar wiring. Reproduce actual content/viewport bounds and input behavior; do not claim “add ScrollRect” as the fix.

Give content height exactly one owner: measured explicit sizing or an appropriate preferred-size fitter. Audit affected shared-factory consumers before changing their layout contract. Avoid competing fitters, mask regressions, off-screen modals, and recurring full-tree rebuilds.

Keep title, selected-caster context, and close/action controls fixed while choices scroll. Provide native scrollbar dragging, wheel input, and supported focus/keyboard navigation. Preserve position after toggles/refreshes and reveal focused rows. Group/search options as useful, but neither grouping nor search substitutes for reachable content.

### Spellbook entry

Install an owned **Buff Planner** gray button in verified free spellbook header/footer space. Inspect real layouts at supported resolutions; the user's reference image is character creation and does not establish spellbook coordinates.

Use one button per actual spellbook owner, attached at verified lifecycle boundaries with bounded recovery. Clean up only owned controls/listeners. No per-frame global hierarchy search and no accidental duplicate events.

Preserve the existing guard against another active FullScreenUi. Implement an explicit handoff: capture valid spellbook character/page context, request native closure, wait a bounded time for ownership release, then enter the existing planner opening lifecycle. Roll back to a usable interface on failure; do not forcibly seize the mode or disable the guard.

Close-without-casting returns to the same spellbook context when it is still valid. Apply uses the existing gameplay execution path and must not reopen the spellbook over animated casting. Preserve pause, selection, focus, input ownership, and usable navigation across Escape, failure, scene changes, disable, and repeated opens.

Opening from a caster's spellbook may highlight that caster for orientation; it must not rewrite saved assignments or silently hide the rest of the party. Existing HUD/hotkey entry remains intact.

## 4. Domain and persistence changes

Retain unique source/catalog identity and nest child assignments beneath it. Suggested concepts are `CastingAssignmentProfile`, `EnhancementSelection`, `ResourceAllocationSummary`, and a sequence-forecast request. These are design suggestions, not claims of existing class names; match current repository conventions.

Carry `AssignmentId` through requests, cast steps, target outcomes, preview bindings, diagnostics, and execution reports. Audit every source-keyed dictionary and filter so multiple assignments for the same source cannot overwrite or conflate outcomes. Sequence traces also identify the routine occurrence. Keep display labels separate from stable identifiers.

UI sends edit commands and renders results. Planning, eligibility, allocation, and persistence belong in appropriate existing services/domain layers, not large view classes. Preserve existing provider preferences as defaults and document assignment override precedence. Keep immutable plan snapshots where practical.

Define the schema before migration, then bump the actual current version as appropriate. From schema 4, each source becomes one legacy-equivalent automatic assignment with its existing targets, enhancements, effect policy, provider defaults, and former allocation order. Do not regenerate assignment IDs on every load or change pooled rod intent.

Use existing atomic persistence and real temporary-file tests. Create an archival pre-migration backup that rotating saves cannot immediately erase. A failed migration, unsupported newer schema, corrupt file, or unresolved identity must not autosave an empty default over valid configuration. Make migration idempotent and ensure errors leave a recoverable original.

Profiles remain external to game saves. Missing party members, sources, items, or optional mods remain represented as unavailable intent. Do not write Unity object references or current balances into profiles. Successful migration is not proof that every referenced gameplay capability is currently available.

## 5. Execution checkpoints

### A — Baseline and immediate overflow repair

Establish the real branch/version/installed baseline, existing test results, available fixtures, and runtime authority. Save a brief implementation map. Reproduce the overflowing chooser, repair height ownership and scrollbar behavior, retain position, and audit the caster-policy chooser sharing the factory.

**Gate:** baseline tests preserved; overflowing content reaches the final selectable row by wheel and scrollbar, without world input leakage. Record content/viewport measurements and actual runtime evidence where available. Keep the repair in a separately reviewable commit. A blocked live gate does not prevent independent offline work but remains open.

### B — Native theme foundation

Inventory campaign donors. Implement independent theme capabilities, bounded recovery, native button states, sound routing, and measured captions. Theme representative buttons, paper, an input, and the enhancement chooser before reskinning everything.

**Gate:** working normal/hover/held/disabled states, readable native text, complete long captions, scrollbar, partial-donor fallback, and lifecycle recovery. If live qualification is blocked, mark the theme unqualified and avoid propagating an unverified donor assumption indiscriminately.

### C — Assignment model, identities, migration, and planner

Implement stable child assignments; exact/pooled enhancement choices; strict pin constraints; visible ordering; cost vectors; allocation traces; and safe migration. Extend the actual planner and existing ledgers. Prove the mixed-caster example before full UI wiring.

**Gate:** four correctly routed example casts; no cross-assignment enhancement leakage; strict pins; correct shared/dependent budgets; no failed-reservation leakage; correct mass-cast counts; preserved migrated profiles/order. Durable exact-item binding is proven or explicitly blocked, never guessed.

### D — Assignment editor and resource UX

Build native assignment rows, add/split/move-target actions, contextual enhancements, caster/item controls, resource inspector, and order controls. Preserve simple automatic portrait assignment. Add the explicit sequence forecast, required/optional behavior, preflight revision handling, and explicit partial execution across planner/HUD entry points.

**Gate:** 9/3/3/6 demand fixture is navigable and explained; reordering changes allocated targets predictably; catalog sorting does not; missing pins survive refresh; actual chosen items are visible; independent routine previews do not invent extra charges. Requested coverage cannot be confused with successful cast count.

### E — Spellbook lifecycle integration

Add the native-styled owned button and guarded handoff/return flow. Reuse existing screen/input ownership machinery, with bounded readiness and rollback.

**Gate:** live spellbook opening, same-context return, Apply navigation, repeated reconstruction, disable, and failed handoff all preserve usable input. No duplicate controls/listeners. HUD/hotkey still work. A button present in the hierarchy is not sufficient acceptance.

### F — Complete reskin, regression qualification, and candidate

Finish remaining planner surfaces with the verified theme. Run the full behavior, migration, exact-assembly, runtime, visual, and performance matrix. Correct regressions rather than weakening validators. Update documentation and stale version/use guidance to match the candidate.

**Gate:** reproducible build/package identities, coherent committed changes, actual evidence for acceptance claims, and explicit outstanding lanes. Choose the candidate version from actual current repository metadata, not a guessed increment from this document. Do not publish.

## 6. Minimum acceptance matrix

Exercise the production planner and services with realistic snapshots and real temporary profile files. Mock only actual external/game boundaries unavailable to the source harness. Avoid tautological tests that duplicate a desired algorithm without invoking production code. Verify pure policies, real Unity layout/input where available, and installed-assembly contracts at their appropriate layers.

| ID | Required behavior |
| --- | --- |
| T01 | Mixed Echolocation assignments produce four correct casts with Share only on the two configured non-self targets; vary IDs and party ordering. |
| T02 | Two casters with the same spell and different rods use the resolved owner's resources; pins do not fall back. |
| T03 | Two identical rods on one caster retain separate physical identity/charges where proven; exact pin survives reload; legacy pooled selection stays pooled. |
| T04 | Nine eligible enhanced casts against three charges yield requested 9, allocated 3, unmet 6; explicit optional fallback is labeled correctly. |
| T05 | Assignment/target order controls allocation; catalog sorting/filtering/reopening does not; legacy order is preserved. |
| T06 | Shared Share/Powerful Change costs aggregate; base/enhancement references to one native pool cannot double-spend it; failed candidates leak no reservations. |
| T07 | Already-active skips reserve no charge; one mass/communal cast costs once; incompatible assignment constraints are not silently merged. |
| T08 | Missing caster/mod/source or moved/unequipped/sold/exhausted item remains repairable intent; invalid targets are removable; updates do not silently prune selections. |
| T09 | Target moves are atomic and do not create accidental explicit duplicates; source-level summaries match child assignments. |
| T10 | Old/current/newer/corrupt profiles, migration retry, atomic-write failure, backup recovery, and unresolved IDs do not destroy the original configuration. |
| T11 | Current-routine views share one native balance; a selected sequence carries balances and supported projected effects forward; estimates/assumptions are labeled. |
| T12 | Manual depletion, rest, older-save reload, and equipment/party changes invalidate stale previews; material plan changes require renewed review. |
| T13 | Required shortage blocks implicit partial Apply, including quick-run; explicit ready-only execution reports full requested coverage and unmet demand honestly. |
| T14 | Both execution modes preserve native effects/spending; test qualified and unavailable optional provider contracts, Share plus Powerful Change where legal, and ordinary unenhanced casts. |
| T15 | Cancellation/rejection before and after native spending neither double-spends nor invents refunds; activation cleanup and one-shot restoration survive failures. |
| T16 | Lists of 0, 1, 30, and 100 options have correct bounds; final row is reachable/selectable; position survives toggles; input does not reach the world. |
| T17 | Native button states, full captions, long names/reasons, tooltips, scrollbars, focus, missing/stale donors, and theme recovery work on real rendered controls. |
| T18 | Spellbook open/return/Apply/failure, repeated opens, reconstruction, scene change, and mod disable restore valid navigation without duplicate listeners. |
| T19 | Existing catalog/variant/provider policy, sticky-touch, HUD/hotkey, and input-isolation regressions remain green. |
| T20 | Compare closed/open planner performance to baseline; no new idle global discovery, per-frame text measurement, or repeated reconstruction is introduced. |

Use at least 1280×720, 1920×1080, and a higher-resolution or scaled configuration supported by the project for visual qualification. Restore any temporary test configuration through the maintained harness.

For every test/lane record PASS, FAIL, BLOCKED, or NOT RUN, exact command/fixture, expected versus observed behavior, and evidence location. Source, assembly, package, and runtime evidence are separate categories. A candidate can be prepared with disclosed blocked lanes, but the overall mission must not be labeled fully complete while mandatory acceptance is unproven.

## 7. Source map and reusable references

Verify locations against the actual checkout. Buff Planner paths below are relative to `src/KingmakerBuffPlanner/`:

| Concern | Reviewed files |
| --- | --- |
| Theme/factory and modal layouts | `UI/KingmakerUiFactory.cs`, `UI/PlannerViews.cs` |
| Screen binding/presentation | `UI/BuffPlannerScreenView.cs`, `UI/PlannerPresentationModels.cs`, `UI/PlannerScreenViewModel.cs` |
| Configuration/session composition | `UI/PlannerSetupModel.cs`, `UI/PlannerUiSession.cs` |
| Screen/input/HUD lifecycle | `UI/BuffPlannerScreenController.cs`, `UI/BuffPlannerUiRoot.cs`, `UI/KingmakerPlannerInputBoundary.cs`, `UI/BuffPlannerHudButtonController.cs` |
| Profile models, validation, migration | `Persistence/ProfileModels.cs`, `Persistence/ProfileRepository.cs` |
| Request compilation/allocation | `Planning/RoutinePlanService.cs`, `Planning/CastPlanner.cs`, `Planning/ResourceLedger.cs` |
| Requests, steps, enhancement applicability/cost | `Domain/Planning/PlanningModels.cs`, `Domain/Planning/CastEnhancements.cs`, `Execution/ExecutionModels.cs` |
| Native enhancement discovery/activation | `GameAdapters/KingmakerCastEnhancementAdapter.cs` |
| Effective targeting/Share | `Domain/Planning/EffectiveTargeting.cs`, `GameAdapters/KingmakerShareTargetingModifier.cs`, `Compatibility/BrownFurShareTransmutationCompatibility.cs` |
| Preserve native execution routes | `Execution/AnimatedCastExecutor.cs`, `Execution/InstantCastExecutor.cs`, `Execution/HybridCastExecutor.cs`, corresponding game adapters |

Dice Roller references, relative to `src/KingmakerDiceRoller/`: `UI/NativeBookTheme.cs`, `UI/NativeThemeResolver.cs`, `UI/NativeUiDonorLookup.cs`, `UI/NativeThemeRecovery.cs`, `UI/ButtonCaptionFit.cs`, and relevant caption measurement/application in `UI/NativeRollPanelHost.cs`. Reuse patterns with independent campaign verification, not its owner assumptions, gameplay bindings, or entire host.

## 8. Durable handoff and completion report

At the first checkpoint, persist this mission as `planning/Z-NATIVE-ASSIGNMENTS-MISSION.md` or the repository's established equivalent. Maintain one compact checkpoint status document, such as `planning/Z-NATIVE-ASSIGNMENTS-STATUS.md`, linked from existing resume records. Do not create competing source-of-truth trackers.

After each checkpoint, record branch, exact HEAD, dirty/untracked state, implemented requirements, test commands/results, evidence paths, blockers, any deviation and its reason, and the exact next safe action. Update the existing journal/resume/blocker files and relevant architecture, migration, use, qualification, and release documents.

If tokens, context, or execution time become limited, stop at a safe boundary: terminate only owned test activity through the guarded path, restore transient deployment/state, commit coherent work where safe, and leave the resume record sufficient for another Z/Codex session. Do not discard work or call a partially implemented feature complete.

Before finishing, review the diff for migration data loss, silent fallback/pruning, resource identity collisions, source-keyed outcome collisions, duplicated native debit, UI-only budgeting, input ownership leaks, and per-frame scans. Resolve findings or record them explicitly.

The final report must state:

1. What was implemented, by checkpoint, and what remains blocked or incomplete.
2. Exact branch/commit/candidate identities and whether anything was pushed; confirm main/tag/release publication state accurately.
3. Commands and actual test counts, separating deterministic, assembly, build/package, and live runtime results.
4. How the mixed-caster example, three-charge shortage, identical rods, migration, and spellbook handoff behaved.
5. Candidate package path, SHA-256, evidence locations, compatibility/qualification limits, rollback instructions, and the next safe action.

Begin by inspecting the repository and environment, then execute Checkpoint A. Do not stop after restating this mission.
