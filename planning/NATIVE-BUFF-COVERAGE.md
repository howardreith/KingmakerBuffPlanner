# Native Buff Coverage

Status: IN PROGRESS — deterministic structural catalog generated; provider/resource and runtime disposition audit pending

The authoritative preliminary catalog is generated structurally from the exact installed native blueprint cache by the guarded `native-buff-catalog` scenario. Static name/GUID lists are not the primary discovery mechanism. The generated file is `planning/NATIVE-BUFF-CATALOG.json`, SHA-256 `bcacbe69bc71c85c5299b8fe8254c18baa33d66e9e3ecf53f6d4aa6b37094878`.

| Classification | Count | Evidence |
|---|---:|---|
| all native ability blueprints inventoried | 1,722 | exact `ResourcesLibrary.GetBlueprints<BlueprintAbility>()` export |
| preliminary candidates | 1,353 | spell or structurally detected effect |
| effect-bearing graphs | 1,095 | branch-preserving expression contains a leaf |
| preliminary candidates without a detected effect | 258 | requires scope/exclusion audit |
| candidates with structured unknown-action diagnostics | 455 | explicit diagnostic, not silently discarded |
| candidates without scanner diagnostics | 898 | exact generated catalog |
| spells among preliminary candidates | 1,060 | exact blueprint flag |
| non-spell effect candidates | 293 | structural discovery |
| variant parents / sticky-touch candidates | 78 / 54 | exact blueprint components/properties |
| final PASS | 0 | final execution/resource qualification not yet run |
| final FAIL | 0 | audit remains open rather than misclassifying candidates |
| DEFER — EVIDENCED | 1,353 | final candidate disposition requires Phases 3–10 |

Current catalog fields cover source/parent/variant GUIDs, names, assembly, spell/candidate flags, targeting, sticky-touch, resource IDs, branch-preserving effect expressions, provenance paths, diagnostics, and preliminary disposition. Provider class, exact spell-list level, normalized resource/slot semantics, planner eligibility, and final runtime disposition are Phase 3+ obligations and are not claimed complete.

Determinism proof: runs `20260811T1958580589645Z-native-buff-catalog` and `20260811T2000040593873Z-native-buff-catalog`, both from commit `07dc2380abbac74228eed88ce73113aeeabe61db`, produced the same catalog hash and counts. Both transactions restored the original whole `Mods` tree with `restorationVerified=true`; no save was selected.
