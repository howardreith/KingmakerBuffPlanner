# Native Buff Exception Matrix

Status: PHASE 9 STRUCTURAL AUDIT PASS

| Candidate set | Exception | Handling | Evidence | Status |
|---|---|---|---|---|
| 1 source | exact reflected `ActionList` wrapper | bounded cached exact-member reflection; path marked `reflected:` | catalog support class `generic-reflection-wrapper` | PASS structurally |
| 13 sources | area/pet/party/worn-enchantment/Magic Fang/dynamic enchant-pool semantics | exact typed adapters; dynamic pools retain signal-buff presence and native cast execution | catalog support class `explicit-adapter` | PASS structurally; runtime deferred |
| 3 friendly sources | harmful-enemy metadata conflicts with proven friendly persistent effect | versioned `include` overrides for Freedom of Movement, True Seeing, Strength Surge | `NativeEffectOverrides.json` exact GUID/effect/reason | PASS structurally; runtime deferred |
| 2 direct-healing sources | only persistent leaf is a cooldown marker | versioned `exclude` overrides for Heavenly Fire and Spontaneous Healing | exact component/empty-marker evidence in catalog | PASS excluded |
| 422 candidates | one or more structured unsupported-action diagnostics | persistent semantics classified independently; diagnostics remain visible per row | 25 included rows retain non-persistent adjunct diagnostics | PASS classified |
| all 974 candidates | unsupported in-scope behavior | none hidden | `unsupportedCount=0`; counts reconcile exactly | PASS |

The override file is strict schema 1, rejects duplicate/unknown/invalid members, supports include/exclude/replace/augment/unsupported dispositions, and preserves `allOf`/`anyOf` effect semantics. Overrides remain an exception layer; structural discovery is primary.
