# Architecture and Source Matrix

Status: IN PROGRESS

| Concern | Planned project boundary | Evidence/reference authority | Copy policy |
|---|---|---|---|
| Composition and UMM lifecycle | `Integration` composition root | exact local UMM 0.28.2 API | original implementation |
| Game contracts | narrow `GameAdapters` interfaces | exact installed Assembly-CSharp IL/reflection | original implementation |
| Action-graph discovery | `Discovery` scanner and diagnostics | exact action contracts; MIT references for comparison | original implementation |
| Normalized catalog | immutable `Domain` types | mission model and behavioral requirements | original implementation |
| Planning/allocation | deterministic pure `Planning` services | exact Spellbook/AbilityData contracts | original implementation |
| Persistence | versioned external JSON with atomic replace | mission requirements; reference failure modes | original implementation |
| Animated execution | executor interface over native commands | exact `UnitUseAbility` contract | original implementation |
| Instant execution | executor interface over `RuleCastSpell` and native spend | exact installed IL | original implementation |
| UI | standalone Unity UI controller/view | exact Unity 2018 and UMM lifecycle | original implementation; no copied assets |
| Compatibility | reflection adapters and versioned overrides | optional local fixtures | original implementation, fail-soft |
| Runtime qualification | guarded request/result runner | read-only Gunslinger harness architecture | adapted architecture with standalone identities |

## Rejected reference behaviors

- Flattening conditional branches into one effect set loses branch semantics.
- Treating maximum resources or spells-per-day as current availability over-allocates.
- Direct persistence writes are not crash-safe.
- Global command hooks or blueprint mutation leak state across casts.
- Direct `AddBuff` does not preserve casting, resource, event, or material semantics.
- Name/index persistence is unstable; stable blueprint GUIDs and unit IDs are required.
