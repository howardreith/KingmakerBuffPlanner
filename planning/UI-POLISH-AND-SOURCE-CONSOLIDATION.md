# UI polish and source consolidation

## Mission boundary

This pass refines the qualified 0.0.8 four-column planner. Discovery, effect interpretation, routine semantics, target identity, provider/resource planning, Animated and Instant execution, modal ownership, HUD input ownership, hotkey behavior, persistence durability, and guarded deployment remain authoritative. The production UI continues to expose one planner and no provider-management surface.

## Consolidation rule

The visible-card identity is an effect-semantic identity, not a provider or caster identity and never a display-name-only match. A deterministic fingerprint walks the normalized `EffectExpression` tree and includes:

- each leaf's effect kind, stable effect GUID, and target semantic;
- sequence structure and child order;
- conditional contract plus distinct true/false branches;
- targeted-wrapper target semantics; and
- referenced-ability child semantics while deliberately excluding the wrapper ability GUID.

The fingerprint deliberately excludes provider key, caster, spellbook, resource pool, prepared slot, action path, discovery source contract, and reference-wrapper ability ID. Consequently, two providers that deliver the same normalized effect tree produce one player-facing card, while same-name abilities with different effect GUIDs, targets, conditions, or branch structures remain separate. Empty/unresolved expressions retain their exact ability identity and are never consolidated speculatively.

An aggregate retains every member `AbilityKey` and every matching provider. The existing planner ranks the resulting provider options with its current priority, coverage, caster-level, duration, resource-kind, and stable-key rules. The assignment stores the stable aggregate source ID and representative ability only for compatibility/readability; planning accepts all member ability keys. No provider selector is added to the normal UI.

## Persistence migration

At catalog binding, exact ability-key assignments from earlier profiles are mapped to the matching aggregate identity. Assignments that collapse into the same aggregate are merged routine-by-routine, preserving the union of target IDs, ignored-presence markers, and the safest existing-effect policy. Unsupported assignments remain untouched. The rebound profile is saved once only when a change occurred. The same aggregate ID is then used for new edits and round trips.

## Presentation states

The portrait adapter emits one deterministic state token: `DirectSelected`, `IndirectCovered`, `ValidUnselected`, `Invalid`, or `SelectedButUnfulfillable`. Direct selection receives the strongest green full-portrait treatment. Indirect coverage receives a lighter green treatment and is derived from normalized party/area recipient semantics plus the existing legal-target/provider model, never spell names. Warnings remain amber and invalid targets remain muted red.

## Visual-only contracts

Grid tuning may change inner margins, column gaps, card width, and lower-panel balance while retaining exactly four columns at 1920x1080 and vertical scrolling. HUD tuning may change only owned backing/frame/padding/spacing visuals; native anchor discovery, button rectangles, hitboxes, listeners, click actions, tooltips, pointer capture, quick actions, and lifecycle are frozen and regression-tested.

## Baseline

- Branch point: `47d1777` (`codex/ui-grid-rebuild`)
- Qualified source: `6e5d02b21e587db84f2c7e7d2a34a63bace3e942`
- Version: `0.0.8`
- Package SHA-256: `22ce0c0e44c6f6b1f895199e58fe1afe5f639e6b38443e062fd6f4204ec8dbb2`
- DLL SHA-256: `593db3bb0ce76316840f94e52d4698c7cd2353bc2aa31610608368478bcdda4b`
- DLL MVID: `a8265c4e-e37d-4f54-a3e4-ee6578fdefa6`
- Rollback copy: `artifacts/release-candidate-backups/ui-polish-start-0.0.8-47d1777/KingmakerBuffPlanner-0.0.8.zip`
