# Native Buff Coverage Matrix

0.0.13 Powerful Change checkpoint: native catalog inclusion counts are
unchanged. Optional enhancement qualification consumes the existing
branch-preserving action graph and inspects resulting buff component semantics;
it does not add Bull's Strength or the other score spells to a name/GUID
allowlist. Deterministic classifier coverage includes Strength, Dexterity,
Constitution, Intelligence, Wisdom, Charisma, supported polymorph carriers, and
wrong-school/spellbook/unrelated rejection. Real-campaign modifier equivalence
for the diagnostic artifact remains pending.

0.0.11 performance-repair regression: exact no-save run `perf-fix-0.0.11-exact-native-1` passes 12/12 with 1,722 abilities, 974 candidates, 952 detected effects, zero scanner exceptions, zero KBP Harmony overlap, catalog SHA-256 `41acd68374e93584a88b16b3a719fbbeeb2a9a0bca088fdf30ede30d371ba614`, and exact restoration. The HUD-lifecycle fix does not change structural classification. Save-backed effect/resource equivalence remains deferred because no authorized KBP save pair is present.

0.0.8 UI replacement checkpoint: the catalog/discovery contract is unchanged. Exact candidate qualification still reports 1,722 native abilities and 974 candidates; the final UI branch reruns native 12/12 before packaging.

Status: PHASE 9 STRUCTURAL AUDIT PASS; RUNTIME EQUIVALENCE DEFERRED — EVIDENCED

The generated row-level matrix is `planning/NATIVE-BUFF-CATALOG.json`: 1,722 blueprint rows, 974 player-accessible audited candidates, schema 4, SHA-256 `1c2881de5c600c430709fac075e0f4fb223d0e050ba52d07bfa7451cf97be0fa`.

Every candidate has exactly one `include`, `exclude`, or `unsupported-with-reason` disposition. Counts reconcile to 413 included, 561 excluded, and 0 unsupported/unclassified. Each row records its structural evidence, support class, exact reason, override if any, and qualification status.

The native classification gate is complete. The 413 included rows remain `DEFER-runtime-qualification` until an authorized project-owned `KBP_` save permits provider/resource, active-effect, animated/instant, and equivalence scenarios. This matrix does not convert no-save blueprint evidence into a save-backed runtime claim.
