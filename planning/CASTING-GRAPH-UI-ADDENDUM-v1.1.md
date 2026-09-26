# Kingmaker Buff Planner
## Casting-Graph Workspace and Global Budget Addendum

**Version:** 1.1  
**Prepared:** September 25, 2026  
**Owner:** Howie  
**Status:** Adopted owner correction to the Casting-First Migration and Native Scroll UI Charter v1.0

## 1. Purpose and authority

This addendum refines the existing casting-first charter. It does not replace the canonical casting model, compiler, resource ledger, execution coordinator, persistence safeguards, compatibility contracts, or guarded runtime procedures already implemented.

It supersedes any implementation interpretation in which the target casting-first experience is primarily:

- a full-screen multi-column buff grid;
- an `Automatic` caster policy;
- target toggles attached directly to a selected buff;
- source-level enhancement controls;
- a separate `Assignments & Resources` dialog required for ordinary casting authoring; or
- a close visual/workflow imitation of Bubble Buffs.

The owner’s manual observation at 1920×1200 rejected that experience as confusing, visually weak, and contrary to the intended product. That observation is a product decision, not merely a cosmetic preference.

The screenshot that prompted this addendum must not be assumed to show the new casting-first workspace until the loaded DLL, version, commit identity, planner mode, and active view class are proved. The last known release-candidate procedure left the normal installation on an earlier build and kept casting-first opt-in. Build/mode identity is therefore the first gate of the correction mission.

## 2. Corrected primary interaction

The ordinary direct-target workflow is:

1. **Select a buff.**
2. **Select a caster and, where materially necessary, the exact source.**
3. **Select a legal target.**
4. The system creates **one explicit planned casting** and displays it as a connection from that caster/source to that target.
5. **Select the casting connection** to configure enhancements and other per-casting settings.

The workspace must make this sequence visually obvious without requiring a tutorial, a secondary assignment-management screen, or cycling through unnamed casters.

Selecting a buff, caster, source, or existing casting changes editing focus only. It does not mutate the saved plan until the player performs an explicit authoring action such as selecting a target, applying an enhancement, moving a casting, or deleting a casting.

## 3. Workspace composition

For the selected buff only, the primary parchment uses three fixed conceptual lanes:

- **Caster/source lane**
- **Casting-connection lane**
- **Target/beneficiary lane**

The catalogue remains available as a narrow navigation surface. It must not dominate the screen after a buff is selected.

### 3.1 Caster/source lane

Each capable caster remains visible even when currently exhausted or blocked.

Each caster entry shows:

- portrait and display name;
- capability/readiness reason;
- exact materially distinct source rows when more than one exists;
- the number of additional castings of the selected buff currently fundable from that source after global plan reservations;
- the pool that controls that count, such as a spontaneous spell level, prepared token, ability pool, item charge pool, or unlimited source; and
- blocked or unresolved reasons without removing the caster.

Do not display a mathematically false aggregate count by summing sources that share a pool. When one reliable aggregate cannot be computed without double-counting, show source-specific counts and require source selection.

When a caster has exactly one materially distinct usable source, selecting the caster may select that source. When several materially distinct sources exist, the source must be chosen explicitly before a new casting is authored.

`Automatic` may remain as migration provenance or an unresolved draft state. It is not the normal primary authoring choice for new casting-first records.

### 3.2 Target lane

After a caster/source is selected:

- legal targets are visibly enabled;
- illegal or unavailable targets remain visible with reasons;
- an already assigned target shows its existing casting connection rather than being silently stolen;
- clicking a legal unassigned target creates one direct-target casting;
- duplicate, move, replace, and remove are explicit operations.

For a single-target ability, three selected recipients produce three casting records, three connections, and three actual cast costs.

### 3.3 Casting connections

One direct casting is represented by:

- a restrained ink-like visible line;
- a midpoint casting chip/card or other substantial hit target;
- the casting’s order or stable short identifier;
- source/cost/readiness information sufficient to distinguish parallel castings; and
- selected enhancement indicators.

The visible line may be narrow, but interaction must never require precise thin-line clicking. Use a wide invisible hit corridor and/or the midpoint casting chip as the primary pointer target.

Clicking either the connection’s hit corridor or its casting chip selects exactly that casting and opens its inspector.

Multiple castings between the same caster and target remain distinct. They may use parallel offset lines or stacked chips but must never collapse into one ambiguous edge.

The graph is derived from canonical casting records. Do not persist freeform node positions.

### 3.4 Per-casting inspector

The selected casting inspector contains:

- exact caster and source;
- direct target or group origin;
- routine and explicit order;
- existing-effect policy;
- applicable targeting modifiers;
- enhancements;
- complete cost vector;
- readiness and refusal reasons;
- Undo, remove, move, and duplicate operations where supported.

Enhancements are configured on the casting, not globally on the buff and not merely on the caster.

Each enhancement entry shows:

- name;
- mechanism/source;
- applicability;
- actual resource demand;
- global allocated/available/remaining state;
- expected effect such as duration or strength change; and
- why it is unavailable or blocked.

Examples include Extend Spell/rod use, Powerful Change, and any other verified caster/source-specific enhancement.

## 4. Global resource contract

All caster capacity and enhancement availability shown in the workspace must come from the same authoritative compiler/resource services used by preflight and execution.

The UI must not maintain an independent counter.

The global plan budget includes all enabled planned castings in the explicitly displayed one-pass routine order. At minimum:

- spontaneous spell-level pools;
- prepared slots and linked prepared tokens;
- unlimited sources;
- ability pools;
- metamagic/item charge pools;
- class-resource pools such as Arcane Reservoir;
- materials; and
- combined demands from multiple enhancements sharing one pool.

Creating, deleting, moving, enabling, disabling, or enhancing any casting refreshes every affected caster/source count and budget line across the plan.

For a spontaneous caster, planning a level-2 cast for one buff reduces the displayed remaining level-2 capacity for every other level-2 buff using that spellbook.

For a shared enhancement pool, spending it on one casting reduces the displayed remaining enhancement capacity everywhere else.

A selected-routine preview and a global one-pass preview may both exist, but they must be clearly labelled and consume the same authoritative resolved-plan representation. Incidental previews never approve execution.

## 5. Group effects

A group ability remains one authored casting.

Represent:

- caster or target-anchored origin;
- one casting connection/card;
- required coverage;
- predicted beneficiaries; and
- missed coverage.

Derived beneficiary branches use a distinct dashed or patterned treatment and are not independently clickable castings or additional costs.

Never silently create another group cast to fill coverage.

## 6. Visual and input corrections

### 6.1 Hover ownership

The owner observed one correct target hover plus a second offset hover up and left of the pointer.

The correction must establish, not guess, the cause by inspecting:

- active planner mode and view class;
- UI hierarchy and duplicate portrait/overlay instances;
- `Selectable` transition targets;
- pointer-enter/exit listeners;
- event-system raycasts;
- canvas/camera ownership;
- screen-to-local coordinate conversion;
- scale factor and anchors; and
- stale objects or listeners left from rebuilds.

Acceptance requires:

- exactly one visible hover response for the actual hovered control;
- no offset or phantom highlight;
- no duplicate event owner;
- no lingering highlight after exit, close, reload, or scene change; and
- correct behavior at 1920×1200 and 1920×1080.

### 6.2 Text contrast

Normal, hovered, pressed, selected, and disabled button states must remain readable.

For normal actionable text, target a contrast ratio of at least 4.5:1 against the actual button background state. Large text may use 3:1. Measure intended colors rather than relying only on subjective screenshots.

Use a crisp high-contrast native-compatible text treatment. Do not retain muddy low-contrast gold-on-gray merely because it came from a donor. A donor that produces unreadable text is not an acceptable complete style contract.

Disabled controls must look disabled while remaining legible enough to explain their state.

### 6.3 Native appearance

The target is a coherent Kingmaker planning ledger, not Bubble Buffs reproduced on beige paper.

Use restrained native parchment, burgundy emphasis, readable typography, native portraits/icons, gray action buttons only where readable, and ink-like connections.

Do not introduce a new UI framework or external asset package.

## 7. Mode clarity

The screen must state which planner is active: `Classic` or `Casting-first`.

If Classic remains available during development, the user must not be able to mistake it for the new workspace. Expose a clear mode label and a deliberate route to the casting-first workspace.

A screenshot or manual verdict is not attributed to casting-first until the loaded binary identity and active mode/view are recorded.

## 8. Persistence and architecture

Reuse the canonical schema-6 casting records, authoring service, compiler, budget ledger, persistence, review gate, and execution host.

The Unity view renders read models and sends commands. It must not:

- choose substitute casters;
- calculate independent resource balances;
- infer targeting legality;
- silently expand cast counts;
- mutate sibling castings during focus changes; or
- bypass review/preflight.

No schema change is required merely to draw connections. Add persistence fields only if a new product decision truly cannot be represented by the existing canonical model. Never persist graph coordinates.

## 9. Required correction acceptance scenarios

| ID | Scenario | Required result |
|---|---|---|
| G01 | Prove current screenshot identity | Loaded version/DLL/MVID, planner mode, and active view class recorded before calling it a regression. |
| G02 | Select buff → caster/source → target | One explicit direct casting and one connection created; no unrelated mutation. |
| G03 | Same buff, two casters, three targets | Three distinct connections; editing one changes only it. |
| G04 | Multiple sources on one caster | Exact source rows and non-double-counted remaining capacity; explicit source choice where required. |
| G05 | Spontaneous global capacity | Planning one level-pool cast reduces remaining capacity for every other casting sharing that pool; deletion restores it. |
| G06 | Shared enhancement resource | Two enhancements consuming one class-resource pool show combined demand and block atomically when insufficient. |
| G07 | Enhancement editing | Clicking a casting line/chip opens its inspector; Extend/Powerful Change choices affect only that casting and update global budgets. |
| G08 | Parallel same caster-target castings | Distinct records, lines/chips, costs, and selection; no collapsed ambiguous edge. |
| G09 | Group casting | One origin/casting/cost, derived beneficiary branches, visible missed coverage, no automatic extra cast. |
| G10 | Hover sweep | One and only one highlight follows the hovered portrait/control; no offset ghost at both supported resolutions. |
| G11 | Button states and contrast | Normal/hover/pressed/selected/disabled states readable; normal actionable text meets the contrast target. |
| G12 | Save/reload | Exact casting connections and enhancement selections reconstruct from canonical records; no graph-position persistence. |
| G13 | Long names and crowded plan | Essential source, target, enhancement, count, and action text remains reachable and readable. |
| G14 | Input/lifecycle | Search, lines, portraits, inspector, Escape, close/reopen, reload, and scene change leave no world-input leak, stale highlight, or duplicate listener. |
| G15 | Performance stability | No per-frame global discovery or full graph reconstruction; object/listener counts remain stable through repeated edits. |

## 10. Milestone boundary

The prior rc6 evidence remains valuable for canonical-model, execution, resource, migration, install, and rollback behavior. The owner has not accepted its currently observed UI as the product milestone.

Do not publish or merge a casting-first release on the strength of earlier automated UI evidence alone.

The next milestone is a focused, reviewable graph-workspace correction followed by one owner visual/usability review. Full gameplay requalification is required only after the corrected UI is accepted and only to the extent the correction changed production planning or execution contracts.
