# Implementation Report

## 0.0.14 complete spell names and selectable variants

The prior variant expansion called `AbilityData.Variants`. Reflection-only IL
inspection of the exact installed Kingmaker 2.1.7b assembly proves that getter
returns `null` and `InitVariants()` is empty in this build. The parent blueprint
does expose its declared `AbilityVariants.Variants` array. The game-supported
child constructor copies caster, fact/spellbook, and metamagic context and sets
`ConvertedFrom` to the parent data; spell level, cast availability, and
`SpendFromSpellbook()` delegate through that parent. This disproved the prior
assumption that runtime variant data would be pre-expanded by the game.

The implementation adds a central game adapter for parent/child materialization
and exact resolution, a Unity-free variant catalog/formatter, and a stable
variant aggregation identity. Spellbook known/custom/prepared enumeration and
fact/resource enumeration now expand declared children in blueprint order and
run every child through the existing structural buff discovery. The parent
container is suppressed. Animated and Instant paths resolve the requested child
from the matching parent spell/fact context and never select the first sibling;
native `AbilityData.Spend()` still executes once.

The exact local native catalog establishes five useful children for each of
`Resist Energy` and `Resist Energy, Communal`, including Sonic. Their declared
orders differ and are preserved. Communal children carry
`AbilityTargetsAround`; this is normalized as area recipients and planned as
one mass cast from a valid caster anchor. No spell name or energy-name allowlist
drives inclusion.

The presentation layer now renders names such as `Protection from Arrows,
Communal` without abbreviation. Catalog cards measure wrapped localized text,
grow only the affected row, and reflow availability/configuration controls.
Selection and description names wrap and retain the same complete model text.
Search matches both complete parent text and concrete distinguishing text.

The behavior suite adds 17 regression cases covering full-name layout,
ordinary and five-child expansion, parent suppression, stable identity,
deduplication/order, Communal distinction, search, persistence, ambiguous legacy
loading, parent-backed availability, exact-child planning, single consumption,
non-variant behavior, icon fallback, and non-English formatting. Current
engineering gates are source `34/34`, protocol/domain `112/112`, runtime
filesystem `8/8`, package fixture `4/4`, deployment WhatIf `5/5`, aggregate
`1/1`, and Release build `1/1`. Feature checkpoint is
`932da35cb6633031d4077e43df65ab659bc9bd84`; clean release-source checkpoint is
`a78869c329e39734cd77f4b587d3d97b05fede70`. Two clean-head builds reproduced
the exact DLL and ZIP. The local-only release ZIP SHA-256 is
`182a597b899875851bd4f6e125a7222018a86bdf7688d455bcf750a512f4e5cd`;
DLL SHA-256 is
`7c6ce2b7bf79fd24625d0d6263d36803a4ec1cd7a873be211fa569d877811fae`;
MVID is `9896cd99-f01e-44c5-afe3-980ca1d043b9`.

## 0.0.13 Brown-Fur Powerful Change compatibility repair

The exact `Enhancement: None available` result was produced before the view:
`KingmakerCastEnhancementAdapter` discovered only `MetamagicRodMechanics`, and
`CastEnhancementSnapshot.IsApplicable` rejected every non-rod category. No
Powerful Change capability, qualified spell, option, or runtime toggle ever
entered the model. Persistence, deduplication, resource filtering, and the view
were not removing a previously discovered option.

Read-only inspection of the installed environment established that Call of the
Wild supplies the Arcanist contracts, while a separately optional module
publishes Brown-Fur and its stable blueprints. The live implementation uses one
level-3 feature, six native score activatables, marker buffs, and one shared
Arcane Reservoir. Its cast engine qualifies genuine Transmutation spells from
the exact Arcanist spellbook by positive ability-score carrier semantics and
starts its transaction at native `UnitUseAbility` construction.

The repair adds a fail-soft compatibility profile and live contract validation,
structural buff-component classification, class-feature applicability and
rejection reasons, shared usage-pool allocation, native toggle preparation,
mandatory command routing, one-shot cleanup, and focused
`[KBP][Enhancement]` diagnostics. Bull's Strength qualifies through its native
`AddStatBonus` Strength +4 Enhancement carrier; no spell name or spell GUID is
hard-coded. Other ability-score transmutations and supported polymorph carriers
follow the same policy.

Implementation/test checkpoint commit:
`650605aaf2c1c1f7272893074b5e7ad7ed9a9224`. Release version: `0.0.13`.
Exact release/tag commit:
`3c329cfff3530fe8397012565c238a81d55cec1d`. The published package
is `artifacts/release/0.0.13/KingmakerBuffPlanner-0.0.13.zip`; ZIP SHA-256 is
`67768176032d6d980f09b708a636dfa8f07e5b052530deb327d833e8e4882d96`,
DLL SHA-256 is
`b41f31da57f9b7ee69a4e693792bf4bb1a6f7e5ea7dbff0e723c72f24d02bf86`,
and MVID is `995ed895-bb45-412c-b626-692816b1f833`. Cross-mod numerical behavior
remains an explicit in-game acceptance item rather than an automated claim.
The guarded local installer replaced the earlier diagnostic UMM binary with
these exact published bytes, preserving settings and verifying every unrelated
mod unchanged. Public release:
`https://github.com/howardreith/KingmakerBuffPlanner/releases/tag/v0.0.13`.

## 0.0.12 HUD lifecycle repair

The 0.0.11 performance fix correctly replaced per-frame global discovery with
one-shot invalidation, but `ObserveHost` consumed that request before it knew the
result of `TryInstall()`. The Boolean result conflated temporary native readiness,
a newly created provisional row, a pending row, and stable installation. The
candidate's later 120-frame self-expiry could therefore remove all four buttons
without notifying the unchanged outer HUD gate.

The repair keeps invalidation-driven scoped discovery and replaces the Boolean
contract with typed attempt and candidate outcomes. Retryable inner readiness is
paced at one attempt per 30 frames while the known outer HUD remains active.
Candidate creation suppresses redispatch until validation reports installed,
expired, or stale. Expiry and stale-anchor outcomes re-arm the same-host request
after the settling interval. Unload and disable suspend host observation until an
explicit load or enable resume, closing the log-proven path that constructed a
candidate after `OnAreaBeginUnloading`.

Installed state now validates the complete held hosting chain against the current
`StaticCanvas.HUDController`: owned root existence, parent, activity and exact
native-cluster parent; inner controller and cluster existence/activity; outer HUD
existence/activity; hierarchy membership; and raycaster activity. The check is
constant-time and never calls a hierarchy search. If it fails, only Buff Planner
objects, sprites, textures, listeners, and pointer registrations are released.
The next bounded attempt locates the current inner controller beneath the already
known active outer host.

Deferred placement/glyph/raycast validation is retained. A pure 120-frame
validation gate makes expiry deterministic; the first failing predicate is no
longer overwritten by later successful checks, and changing geometry values do
not cause per-frame log spam. Root diagnostics now report lifecycle state,
request/retry/dispatch counts, host/anchor/cluster identities and activity,
attempt/candidate outcomes, hosting failure, and last validation failure.

Source-only checkpoint after implementation: source 34/34, behavior/protocol
91/91, runtime-harness filesystem 8/8, package 4/4, deployment WhatIf 5/5, and
aggregate 1/1. Exact source `083dfbfcf651d44bb01b302ccbbabac823e236e9`
passes deterministic release packaging 2/2 with ZIP
`eabb4785be75129cbc6cffcab030afc9e4afac32957fb7b82af2c16b0e0ac72a`, DLL
`6db3693e0ba38b3672bd0eac36b06df6e5965de29a3e78f9b97513c6accfe9e1`, and
MVID `920b3246-e7f7-4818-92f4-e54294ef2db0`.

Fresh no-save performance passes 9/9 at 50.493/88.780/90.896 FPS with all 18
moving-camera samples at 90.710-90.896 FPS, zero global HUD searches, zero
absent-HUD installation dispatches, and 2.715 microseconds of steady
`UiRoot.Tick` work per frame. Native and Call of the Wild catalog/Harmony runs
pass 12/12 and 26/26 and restore exactly. An unchanged repetition is retained as
an honest first-startup-bucket threshold miss at 49.810 FPS; every moving sample
remained at least 88.940 FPS and the threshold was not changed.

Guarded save-backed automation remains unavailable because the exact authorized
inventory is baseline=0 and working=0. The owner subsequently tested the exact
guarded-installed build in a campaign and accepted the repair. Its fresh log
shows one candidate staying pending while the loading screen owned the raycast,
then the same candidate installing with four buttons/four listeners after 57
validation frames; Setup presentation also validated and opened. This is the
missing human lifecycle proof, while unexecuted automation rows remain honestly
unexecuted. Exact evidence is recorded in
`planning/HUD-LIFECYCLE-0.0.12-HUMAN-ACCEPTANCE.md`.

The accepted repair was merged and released from
`a48bfae2185a50f1c50d9151666e0b5ce0a0bc3e` as `v0.0.12`. The guarded
publisher repeated every source/package gate and two deterministic builds. The
published ZIP SHA-256 is
`1cbb2b215a78ab4dea2af5c99ebae211fe21e1f3532eb1865af3420a90ea8494`;
the DLL SHA-256 and MVID are
`1964b0220fd0ddd4a15009900a30ee3ec3af83c4d90b022eebb87d27cde03cac`
and `3947e19e-fd3b-4b11-95d8-8a1b360cf9a4`. A fresh downloaded asset passes
strict package validation and the exact published bytes are guarded-installed
with settings and non-planner mods preserved. See
`planning/HUD-LIFECYCLE-0.0.12-RELEASE.md`.

## 0.0.11 test-process crash correction and release

The popup was not a production-mod failure or an orphan process. The source-only executable reused `RuntimeTestProtocol.EvidenceRoot` for disposable protocol fixtures, then called `Directory.CreateDirectory` before its test/cleanup exception boundary. A restricted direct launch therefore let `UnauthorizedAccessException` escape `Program.Main`, which Windows surfaced as CLR exception `0xe0434352`. The same exact binary passed when allowed to access that external root, and no asynchronous, child-process, finalizer, duplicate-binary, or stale-worktree path existed.

Commit `f6bbe648e0311e8b0022ec9810b533fc66cc6502` adds a test-only injected root beneath a unique temporary boundary while leaving the production root and its containment checks unchanged. It adds a production-boundary regression, completes resolver and filesystem cleanup before printing PASS, and catches only the entry-point infrastructure boundary to report stderr and exit 2. An intentional unwritable-temp check exits 2 without a Windows popup; it is neither swallowed nor converted to success.

Current remote-main work was integrated without redesigning the human-qualified performance fix. Release/tag commit `3661f5c31a1060bca67758c2369b2ef361a339c9` passed 86/86 protocol tests and the complete guarded publisher with no new crash events. Published identity is ZIP `89cbebd2a1eb594d2307c4388c19588e1d4ea9c845284d36081c3e72d492795c`, DLL `95f484907f9a1008798e3557e46212faa1e41406bccf9d109d78e1921e9d46c6`, MVID `bf949174-0601-4822-a121-9c9d9c14597f`.

## 0.0.11 runtime performance repair

Version 0.0.10's retained root called `BuffPlannerHudButtonController.TryInstall()` from every `Main.OnUpdate`, even while all planner windows were closed. With no campaign HUD, `TryInstall()` called `UnityEngine.Object.FindObjectOfType<IngameMenuController>()` globally on every frame. The exact unfixed diagnostic build spent 18,874.614 ms in 228 searches over 20.080 seconds and averaged 11.358 FPS. Suppressing only HUD discovery in the same DLL produced 1,787 frames and 89.234 average FPS.

Commit `f23a07b7560b2aa4cd7b3d1635436c3abffd575a` replaces that loop with dirty invalidation and exact-host observation, then bounds the lookup below `StaticCanvas.HUDController`. Fixed normal-path runs averaged 89.236 and 88.671 FPS under the local 90 FPS cap, with zero HUD searches during 18 moving-camera samples. A later exact checkpoint repeated 89.237 FPS average and exact restoration.

The static audit rejected frame/timing mutations, catalog/provider polling, full-screen UI construction, file/profile IO, infinite coroutines, and pointer/camera Harmony patches as causes. The runtime values stayed target 90, vSync 0, time scale 1, fixed delta 0.02, maximum delta 0.04, and capture framerate 0. Pointer/camera patch invocation counts were zero in the menu; hotkey isolation was negligible relative to the 82.78 ms average global search.

Deterministic gates pass 32/32 source, 78/78 behavior/protocol, 8/8 filesystem, 4/4 package, and 5/5 deployment WhatIf. Native discovery passes 12/12 and Call of the Wild passes 26/26 with zero KBP Harmony overlap. Exact evidence, rejected experiments, and remaining save-backed boundary are recorded in `planning/PERFORMANCE-REGRESSION-ROOT-CAUSE.md` and `docs/QUALIFICATION.md`.

No 0.0.10 artifact was changed and no public release/install action was taken.

## 0.0.10 visual clarity and plan communication — guarded-installed for human acceptance

Final release source/package/DLL/MVID are `14719e816c31d4efadf829733d499774c6f5e741` / `46d741b7dd16120e5687069c215a5cc270b9ec1e8430fd764dd36a2bdb05f013` / `bcd6ed91e4d6898dec74e69f501389adb15728cd03fe0a0915522e5a1a18c55e` / `8bb28075-83cf-41d2-ad3d-e883886c4961`. Exact-source Animated and Instant passed 77/77 each; native passed 12/12; Call of the Wild passed 26/26; all four transactions restored exactly. Deterministic release packaging reproduced both ZIP and DLL twice. Guarded install `ui-clarity-0.0.10-final-install` preserved settings, verified all non-planner mods unchanged, and installed the exact release DLL/MVID. No merge, push, or public release occurred.

The 0.0.9 mechanics remain frozen. This pass adds plan provenance and expected recipients to immutable planning results, then maps the selected consolidated card to five explicit portrait states. Strong green `SELECTED`, amber unavailable, light-green `COVERED`, red invalid, and neutral are deterministic and carry exact tooltips. Single-target and caster-centered negative cases are covered. The one-member authorized save cannot physically display a second covered portrait, so no automated multi-portrait cosmetic claim is made.

The selected-buff panel now shows `Available:` and `Planned:` for that buff, keeps both lines visible, and removes ambiguous coverage fractions/generic blocked counts. Prepared, pooled, at-will, and multiple-caster availability use existing provider/resource state. Effect-semantic consolidation and saved aggregate assignments are unchanged.

HUD row placement is derived from the native lower-left button cluster, and all glyph transforms use one centered safe-area contract plus centralized optical correction from sprite alpha bounds. Production hitboxes/listeners/input behavior were not changed. Text forensics removed the planner-only scaler and bound fixed-size legacy text to the native Arial/UI-Default path with unit transforms and post-layout pixel snapping.

Exact release source `14719e816c31d4efadf829733d499774c6f5e741` passed source 32/32, behavior 75/75, filesystem 8/8, package 4/4, and deployment WhatIf 5/5. Fresh-process exact runs passed Animated 77/77, Instant 77/77, native 12/12, and Call of the Wild 26/26; all restored exactly. Runtime diagnostics at the active 1280x720 configuration report native row alignment, all glyphs centered, zero native activations, zero nested planner scalers, zero fractional rendered text/card transforms, and Arial/UI-Default. Human visual acceptance remains mandatory.

## 0.0.9 UI polish and duplicate-source consolidation — installed for human visual acceptance

Final local-only release identity is source `f026a4a9974af8e4191ff7fb104e472f11c2016f`; package `471e86e0043b47bc899322b640fb448105bfc1689f796b56611ed5d980d4bbe8`; DLL `d66edcacedcfe9d862e5cd433e2e58166abbc5a5a5404b9b7c5d6fd39ae898a1`; MVID `174e2e17-9006-4667-b06d-85d372a2bb77`. Exact-source Animated and Instant runs passed 74/74 each; native passed 12/12; Call of the Wild passed 26/26; all four transactions restored exactly. The deterministic release reproduced the runtime ZIP byte-for-byte. Guarded install `ui-polish-0.0.9-final-install` preserved settings and verified every non-planner mod unchanged.

The accepted 0.0.8 screen and all execution/input contracts are frozen. The implementation begins with an effect-semantic aggregate card model: normalized effect GUID/kind/target/branch structure defines visible identity, while provider/caster/spellbook/resource and wrapper ability identity do not. Empty effects never merge and display names are not treated as mechanical identity. Earlier exact-source assignments will be rebound and unioned at catalog binding; the existing provider ranking will receive all member ability keys without adding provider UI.

The aggregate implementation retains every eligible ability key per card and extends only the request-to-provider match; the established provider order and shared resource ledger are unchanged. Catalog binding rekeys and unions legacy assignments once, while unsupported assignments survive untouched. Deterministic coverage proves one card for two provider-backed Resistance-style abilities, valid implicit alternate-provider choice, merged-target persistence round trip, and semantic non-merging of distinct mechanics.

Portraits now use explicit direct/indirect/neutral/invalid/unfulfillable tokens. Direct selections receive a dark-green surface, bright 4px green frame, portrait tint, full overlay, and `SELECTED` label; indirect party/area recipients use a lighter green surface/overlay and `COVERED` label. The grid uses symmetric 20px inner margins and 14px gaps. The HUD retains exact root/button geometry and actions while adding one non-raycast native-framed cluster backing, native button skins, inset antique-gold frames/accents, and more icon breathing room. Final deterministic gates are source validation 30/30, behavior 72/72, filesystem 8/8, package 4/4, and deployment WhatIf 5/5. Physical screenshots prove the direct state, balanced four-column geometry, and functional HUD treatment; the one-member fixture cannot show a multi-portrait indirect state, so that appearance and the overall cosmetic verdict remain human-gated.

## 0.0.8 four-column routine planner — implementation complete, human verdict pending

Final release source/package/DLL/MVID are `6e5d02b21e587db84f2c7e7d2a34a63bace3e942` / `22ce0c0e44c6f6b1f895199e58fe1afe5f639e6b38443e062fd6f4204ec8dbb2` / `593db3bb0ce76316840f94e52d4698c7cd2353bc2aa31610608368478bcdda4b` / `a8265c4e-e37d-4f54-a3e4-ee6578fdefa6`. Exact final Animated and Instant runs passed 72/72 each; native passed 12/12; Call of the Wild passed 26/26; all restored. The guarded final install preserved settings and every other mod.

The full-screen presentation was replaced rather than layered over 0.0.7. The production hierarchy now has top Long/Important/Short tabs, the allowed search/category/Selected only controls, a broad pooled four-column icon grid, a compact selected-buff/portrait/plan panel, one Settings surface, and one Apply action. Target portraits directly create or remove the selected buff assignment in the active routine; removing the last target removes that routine assignment. No alternate old layout, Add-to-routine listener, hidden-entry control, provider editor, technical filter, or duplicate mode control remains.

The automatic provider/planning and Animated/Instant engines were not redesigned. Schema 3 reveals legacy hidden entries and migrates F10 to configurable Ctrl+Shift+B. Exact Kingmaker input inspection identified `KeyboardAccess.Binding.InputMatched()`; the planner chord is polled by the existing update path and its B primary is suppressed from native bindings only for the exact Ctrl+Shift chord. HUD visuals return to the earlier dark tile/antique-gold treatment without geometry or lifecycle changes.

BubbleBuffs commit `f4871f763a23251284422ef0945a85e9f3fb788e` informed only the information design: icon-first multi-column browsing, selected-buff portrait targeting, routine readiness, and search/category hierarchy. No source, asset, shader, texture, or Wrath hierarchy path was copied.

Candidate physical evidence reached 72 assertions with real All=11, Spells=2, Abilities=9, Other=0 and routine-local Selected only Long=1, Important=0, Long=1. A sampled generated glyph stores the authored 0.960 red channel as the expected 8-bit 0.961. Exact release-source reruns and guarded installation are recorded in the final qualification checkpoint.

## 0.0.7 Kingmaker-native presentation refinement — automation complete, human verdict pending

Final release source is `2f125f9f1024692d83a1b2570209d1858d62eff1`; deterministic package/DLL/MVID are `9feed6dffa668812ed826c75b743d72892e6e8371b0f81585fb557aea8fcf453` / `bf8c72874377d56f91bcdb6daedaa8b28b340a948aee06583a32954d61b38927` / `966b7d8f-bd5f-46b9-beda-62774f82ccac`. The guarded 0.0.6-to-0.0.7 replacement preserved settings and verified every other mod unchanged. No merge, push, or public release occurred.

The successful 0.0.6 mechanics were frozen. Four independently regressed UI phases added: a centralized parchment theme; actual blueprint-icon cards; portrait-first target editing; and simplified filters, Settings, and Casting Source disclosure. No persistence schema/identity, discovery/classification, planning/resource, Animated/Instant, confirmed-effect, HUD isolation, modal lease, or live transaction behavior was redesigned.

The first campaign screenshot exposed unsafe stretching of otherwise correct native sprites. Large panels/cards now use centralized translucent parchment fills and restrained borders, while exact native buttons, portrait chrome, background, and font remain resolved with fallbacks. The corrected run shows 11/11 real icons, no fallback icons, one Casting mode control, no retired primary labels, readable Bless target/plan state, and advanced provider controls only after disclosure.

Versioned physical runs `ui-polish-0.0.7-animated-1` and `ui-polish-0.0.7-instant-1` each passed 71/71 in fresh processes, planned/submitted/confirmed Bless `1/1/1`, captured all five presentation states, preserved the profile assignment across reload, completed 20 reopen cycles, and restored Mods exactly. Native `ui-polish-0.0.7-native-final` passed 12/12; Call of the Wild `ui-polish-0.0.7-cotw-final` passed 26/26 with no planner Harmony overlap. Human visual acceptance remains authoritative.

## 0.0.6 live row rendering and Bless material correction — complete

The human-tested 0.0.5 screen proved discovery was populated but showed no list or details pixels. An independent same-Content canary also disappeared despite a live CanvasRenderer, valid geometry, alpha 1, Arial, the default UI shader, and correct canvas/sibling state. Both panes shared a `Mask` whose hidden source Image used alpha 0.001 with alpha clipping enabled; it did not reliably write the stencil required by child graphics. Keeping `showMaskGraphic=false` while making the source opaque restores the stencil without drawing the mask itself.

The existing row renderer already matched the mission fallback: fresh programmatic Unity `Button`, `Image`, `Text`, and `LayoutElement` components, a proven Arial font, readable opaque colors, and no custom shader. It was retained. Layout now applies the preferred height to the RectTransform and lets the vertical group control child height, yielding ten compact readable rows. The temporary canary was removed. Runtime qualification captures a real PNG and records the first five names and rectangles, selection/title, renderer cull/alpha/font/material/mask evidence, screenshot hash, and independent pixel-region measurements.

Bless exposed an unrelated generic validation bug. Kingmaker's native contract short-circuits `HasEnoughMaterialComponent` unless `RequireMaterialComponent` is true; the planner evaluated sufficiency unconditionally. Exact live Bless data is `require=False,item=none,count=1,hasEnough=False`. The new policy does not evaluate a non-required component and is covered by a deterministic callback test. In both final live runs Bless submitted, started, spent one prepared slot, and confirmed the expected effect.

Final identity and gates: source `e656812572adea8bc312419372b61ee8c4834e5a`; package `ce7492b2...`; DLL `6144256c...`; MVID `bff11809-aa53-42c2-8ab7-ef3564450e61`; source 30/30; behavior 62/62; two physical visual campaigns 71/71 each; native 12/12; Call of the Wild 26/26; deterministic release 2/2; package 4/4. Guarded installation preserved settings and all non-planner mods. No merge or public release occurred.

## 0.0.5 catalog, HUD input, quick action, and tooltip repair — complete

The authoritative human verdict was a partial pass: live bootstrap and the opaque F10 planner worked, but no catalog rows/details rendered, HUD clicks leaked into world input, quick actions lacked useful outcomes, and the tooltip flickered/offscreen. The repair preserved the bootstrap/modal and changed the four bounded failure paths only.

The populated live model was not stale: 18 raw sources produced 11 beneficial normalized entries and 11 providers. The earliest missing stage was post-binding scroll geometry. Explicit layout/content sizing plus separate filter/view-model/GameObject/active/visible diagnostics now prove 10 available rows, non-zero content and row bounds, and bound details. Binding failures are visible and logged; filtered and genuine empty states have distinct reasons and recovery actions.

The tooltip and feedback labels had participated in the HUD `HorizontalLayoutGroup`, shifting the hovered controls and causing enter/exit oscillation. They now ignore layout; the single tooltip is non-raycastable, bounded, wrapped, inward placed, and clamped. Exact 2.1.7b inspection showed world click and edge scrolling bypass Unity UI raycast consumption, so scoped pointer ownership intercepts only native pointer ticks and mouse-edge camera shift while the cursor is over registered KBP regions.

Quick actions now prove pointer receipt, one listener invocation, group/profile/configuration resolution, plan refresh, validation, execution/refusal, confirmation, and visible presentation. An unexpected coroutine boundary clears executing state and displays the full failure. The final controlled Bless run correctly reported `material-component-unavailable` before submission; it did not claim application.

Final gates: source 28/28; behavior/protocol 61/61; harness 7/7; package 4/4; deployment WhatIf 5/5; two physical campaign runs 69/69; native 12/12; Call of the Wild 26/26; deterministic release builds 2/2. Qualified source `390bb8b`; package `3eba3158...`; DLL `69992840...`; MVID `d2fed415-...`. Guarded install preserved settings and every non-planner mod.

## 0.0.4 live bootstrap recovery — complete

The installed 0.0.3 DLL was byte-identical to its release and UMM did invoke `Main.Load`, `OnToggle(true)`, and `OnUpdate`; production used zero Harmony patches. The earliest shared defect was same-frame EventSystem ownership validation immediately after constructing HUD/modal graphics. Real-campaign qualification then exposed two additional exact HUD boundaries: native layout participation overrode the row anchor, and world coordinates were supplied to a screen-space pointer hit test. Version 0.0.4 defers readiness, excludes only the KBP row from native layout, converts centers through the native raycaster event camera, and keeps failed candidates retryable with exact diagnostics.

F10 is now armed and polled directly by the UMM `Main.OnUpdate` callback before HUD work. One static `DontDestroyOnLoad` controller subscribes once to scene/area EventBus lifecycle signals. HUD failure cannot disable F10, and modal input is acquired only after two-phase visible presentation validation. The guarded live protocol accounts for UMM 0.28.2's configured ShowOnStart blocker with a physical Escape, then delivers physical F10 and exercises 20 additional open/close cycles.

Release source `5b96f3b4e713489ce677db3ac5acb83a10f80f01` passed two real-campaign fresh processes at 65/65 each plus native 12/12 and Call of the Wild 26/26 regressions. Deterministic package/DLL/MVID are `cb3799e799f641b1a9f7d79eb71942025b5df71a8de956e17369b24fe2f14d16` / `6f72c38ef7e445121291ff2f17f207d49210ea30a2e07fe1105595133b706f1c` / `305a8a6c-2b49-4e3b-a365-286638cbfafa`. The guarded installer replaced only 0.0.3, preserved settings, and verified all non-planner mods unchanged. No merge or public release occurred.

## 0.0.3 R2 correction

Direct human testing invalidated the 0.0.2 HUD and modal result. The current correction removes native `ButtonPF` cloning, requires fresh retained-mode button hitboxes to own the top EventSystem raycast, anchors the exact Setup/Long/Important/Short row above the native cluster, and validates that native sentinel listeners remain unchanged. Modal opening now constructs, lays out, renders, and validates a dedicated opaque screen before `FullScreenUi` acquisition, with complete rollback on every failure path. Execution no longer exposes “fired” as success: only confirmed expected facts count, while queued/submitted/started/resource and terminal failure states remain distinct. The exact installed profile preserved Long → Bless and its target without migration; provider choice is automatic and will be recorded from the next live party plan.

Final automated gates: source validation 21/21, behavior/protocol 57/57, runtime harness 6/6, package allowlist 4/4, deployment WhatIf 5/5, installer WhatIf 5/5, and warning-free exact-reference deterministic build 2/2. Exact release identity is package `42f823d6b8454ffe4497f4f652752a07d50738d5990c5a5243d091ba92d363e0`, DLL `5d95368ee237e658e06b4948209f805568a417ea150eb36c3023df9b155f0950`, MVID `f3f691a4-d691-4112-90a4-7beb9f06aad2`, source commit `d5a20aa7ddbb2ec7d131a4bed44f1ca65ecaaa65`. Native 12/12 and Call of the Wild 26/26 each repeated twice with exact restoration. The campaign UI gate returned explicit `BLOCKED` because no authorized campaign fixture exists. The validated 0.0.3 replacement is installed with profiles preserved and non-planner mods unchanged; authoritative human retest remains pending.

Status: 0.0.3 INSTALLED FOR HUMAN R2 RETEST; campaign/save-backed acceptance pending

The 0.0.1 and 0.0.2 UI claims below are historical and invalidated by human playtesting. Version 0.0.3 is the installed correction described above. The corrected campaign UI runtime gate no longer runs as part of `final-no-save-core` and reports `BLOCKED` when only the main menu is available. Final human visual/input acceptance is pending playtest, not installation.

Phase 9 now has an exact player-accessibility index, schema-4 generated audit, component/polarity evidence, branch-preserving exact-array adapters, explicit summoning exclusion, Magic Fang/enchantment handling, structural classifier, and strict packaged override registry. The 974 candidates reconcile to 413 included, 561 excluded, and zero unsupported/unclassified; the behavior suite is 43/43. Two guarded native-only runs from `fba6e24` produced byte-identical catalog SHA-256 `1c2881de5c600c430709fac075e0f4fb223d0e050ba52d07bfa7451cf97be0fa`. Structural classification is complete; included-source runtime equivalence remains deferred for lack of an authorized `KBP_` save.

Phase 0 intake is complete. Phase 1 now has a standalone net47/C# 7.3 classic project, UMM 0.28.2 entry point, versioned metadata, strict opt-in runtime request/result skeleton, atomic evidence writer, deterministic build metadata, source validator, project-owned protocol test runner, build script, deterministic package builder, and package allowlist validator.

The first source-only qualification produced 14/14 source assertions, 11/11 protocol tests, a warning-free Release build, and 4/4 package assertions. Two consecutive local package builds produced identical SHA-256 `f109c51b8afcd0af931c556ad5999ea62c1b88f951178da8d563253c18de4d21`. Runtime load qualification has not yet been claimed.

The guarded runtime boundary now includes whole-directory transactional staging, owned lock/token/state/sentinel records, crash-aware restoration, exact original-manifest verification, staged-mutation recording, public `-WhatIf` purity, current-session Steam offline/cloud checks, exact build-manifest linkage, and a Steam App ID 640820 launch/result orchestrator. Synthetic filesystem integration tests pass 5/5 and public deployment purity passes across five compared roots. Hashes above are superseded after this implementation is checkpointed and rebuilt.

Phase 1 is complete at clean runtime commit `d3f4dc1fec9970b1c3c8eed5100052edd996c870`: two fresh-process mod-load scenarios passed with exact package/DLL/MVID/platform identities and exact original `Mods` restoration. See `docs/QUALIFICATION.md` for run IDs and hashes.

Phase 2 is complete at runtime-qualified commit `07dc2380abbac74228eed88ce73113aeeabe61db`. The implementation now has a normalized branch-preserving effect expression, depth/reference/GUID guards, sticky-touch and variant normalization, exact adapters for required Kingmaker actions, cached deterministic exact-`ActionList` reflection, structured unknown diagnostics, action-path provenance, and a generated catalog exporter. A serialization defect discovered by inspecting the first evidence file was repaired with explicit JSON contracts and runtime reconciliation; the rejected `{}` files are retained only as diagnostic evidence. The two final runs produced byte-identical 4.41 MB catalogs with 1,722 abilities, 1,353 preliminary candidates, 1,095 effect-bearing graphs, and zero scanner exceptions.

Phases 3–5 now have source-qualified implementations. Commit `18776f1a650e311c4400c22d0f2764665cc6b046` added immutable party/provider/resource snapshots and exact Kingmaker adapters. Commit `5cbc9bb86fe9fcb79c16f8297c5a8754f34eda05` added deterministic planning, mass grouping, provider controls, resource/material reservations, and typed existing-effect evaluation. Commit `40b233a52aebf1f87d9fc671ae24d9cf86f7150e` added versioned external per-campaign profiles, atomic replacement, three valid backups, recovery, and schema migration. The source-only suite is 37/37. Save-backed runtime gates remain `DEFER — EVIDENCED` because no project-owned `KBP_` fixture exists; these phases are not mislabeled fully qualified.

Phase 6 has a persistent project-owned Unity overlay and separate setup model: search/filter, stable-ID party/pet targets, active/invalid states, Long/Important/Short assignment, provider priority/ban/cap controls, overwrite policy, hidden sources, preview, and scale. Guarded run `20260811T2036033407315Z-ui-root-smoke` proved two open/close cycles, 12 rendered frames, singleton lifecycle, and 2560x1440 resolution with exact restoration. Campaign interaction, scene-transition reconstruction, and the other resolution rows remain open.

Phases 7–8 have source implementations behind `ICastExecutor`. Animated execution queues only the mod-created native `UnitUseAbility` command and polls that scoped command. Instant execution revalidates, triggers one `RuleCastSpell`, then calls one native `AbilityData.Spend()` when UMD did not fail, in bounded batches. The 41/41 suite proves invalid casts do not fire in either engine, batching, reports, and effect/resource record separation. No save-backed execution PASS is claimed.

Phase 10 now has a composed routine service with one shared ledger across sources, structurally filtered live providers, Long/Important/Short HUD execution, full preview/result counters, provider/resource diagnostics, required filters/sorts, bounded clear confirmation, and per-step animated fallback for sticky-touch sources in instant mode. The suite is 46/46. Two consecutive guarded runs from `cfce05c` passed 15/15 assertions apiece, including forced root reconstruction, three layout profiles, zero blockers, and zero event subscriptions. Save-backed configured routine execution is still deferred rather than inferred.

Phase 11 has strict schema-1 compatibility profiles, exact directory/file identity validation, transaction-owned optional-mod staging, profile-bound request/result contracts, exact blueprint ownership, representative optional assertions, strict UMM ID/version and duplicate identity checks, and deterministic ordered Harmony patch inventories. On commit `57ed740`, native-only passed twice at 12/12 and exact Call of the Wild 1.14.4c-2.1 passed twice at 26/26. The optional catalog has 7,342 owned abilities, 4,937 candidates, 2,096 included, and zero unsupported. Tabletop Added Rules and the combined profile are explicitly unavailable locally; save-backed compatibility execution remains deferred because no authorized `KBP_` fixture exists.

Phase 12 adds clean-head deterministic release packaging, a lab-local guarded push policy with a mutation-free repository test, current install/use documentation, draft local-only release notes, and a composed no-save core scenario. Commit `3af45f3` passed that scenario twice at 22/22 with byte-identical catalog and Harmony evidence plus the complete UI lifecycle/layout gate. The exhaustive Definition-of-Done matrix retains every missing save-backed core row as unmet.
