# Native Buff Coverage

Status: PHASE 9 STRUCTURAL AUDIT PASS; INCLUDED-SOURCE RUNTIME QUALIFICATION DEFERRED — EVIDENCED

The authoritative row-level audit is generated from the exact initialized native blueprint cache. Player accessibility is derived from `BlueprintRoot` player classes/races/feats, class and archetype progressions, native spellbooks/special lists, feature fact grants, and variants. Names are recorded for review but are not the primary classifier.

Generated catalog: `planning/NATIVE-BUFF-CATALOG.json`, schema 4, SHA-256 `1c2881de5c600c430709fac075e0f4fb223d0e050ba52d07bfa7451cf97be0fa`.

| Classification | Count | Evidence |
|---|---:|---|
| all native ability blueprints inventoried | 1,722 | `ResourcesLibrary.GetBlueprints<BlueprintAbility>()` |
| audited player-accessible candidates | 974 | exact player class/race/feat reachability graph |
| supported automatically | 396 | structural persistent effect and safe target semantics |
| supported by generic reflection wrapper | 1 | exact `ActionList` wrapper with retained provenance |
| supported by explicit adapter | 13 | area/pet/party/enchantment/Magic Fang/dynamic-pool contracts |
| supported by override | 3 | exact friendly exceptions with misleading target metadata |
| excluded by definition | 561 | exact row reason in generated catalog |
| unsupported with reason | 0 | count reconciles; none hidden |
| runtime-qualified direct cases | 0 | no authorized `KBP_` save exists |
| runtime-qualified equivalence classes | 0 | no authorized `KBP_` save exists |

The candidate equation reconciles exactly: `396 + 1 + 13 + 3 + 561 + 0 = 974`. The 413 included sources are `DEFER-runtime-qualification`; the 561 exclusions are `PASS-excluded-by-definition`.

Exclusion reasons reconcile to 561: point target without safe placement 159; hostile-only 147; no persistent effect 126; summoning 113; sticky-touch delivery carrier only 11; hostile weapon carrier 2; direct healing cooldown overrides 2; non-castable variant container 1. Three otherwise-hostile structural rows are separately re-included by exact friendly-effect overrides.

Catalog fields include source/parent/variant GUIDs, spell lists and levels, native ownership, ability/effect component contracts, target flags, sticky/mass/area shape, resource and material facts, action paths, branch-preserving expressions, exact diagnostics, disposition, support class, override, runtime evidence, and qualification status.

Determinism proof: guarded native-only runs `20260811T2126328544602Z-native-buff-catalog` and `20260811T2127318144905Z-native-buff-catalog`, from commit `fba6e24`, produced byte-identical catalogs with the hash above. Both passed runtime validation and restored the whole original `Mods` tree exactly; no save was selected.

Phase 9's classification/reconciliation gate is PASS. Candidate-level final PASS remains deferred because the mission also requires resource/effect/executor runtime equivalence, which cannot be inferred from a no-save catalog run.
