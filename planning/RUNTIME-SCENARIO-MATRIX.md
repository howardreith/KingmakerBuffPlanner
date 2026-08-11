# Runtime Scenario Matrix

Status: PASS for every applicable no-save scenario; save-backed rows are `DEFER — EVIDENCED`

| Phase | Scenario | Save policy | Required proof | Current status |
|---|---|---|---|---|
| 1 | mod-load-smoke (two fresh processes) | NO-SAVE | identity, hashes, clean load/unload | PASS |
| 2 | native-buff-catalog (two identical fresh processes) | NO-SAVE | exact candidate/effect export and deterministic hash | PASS |
| 6 | ui-root-smoke | NO-SAVE | singleton root, repeated open/close, rendered frames, exact resolution | PASS at 2560x1440 |
| 3 | party-provider-scan | KBP working fixture | providers/sources/resources | DEFER — EVIDENCED |
| 4 | prepared-resource-plan | KBP working fixture | exact prepared allocation | DEFER — EVIDENCED |
| 4 | spontaneous-resource-plan | KBP working fixture | shared pool allocation | DEFER — EVIDENCED |
| 4 | existing-effect skip/overwrite | KBP working fixture | policy behavior | DEFER — EVIDENCED |
| 5 | profile-persistence | KBP working fixture | round-trip/reorder/reload | DEFER — EVIDENCED |
| 7 | animated-execution | KBP working fixture | real command/effect/spend | DEFER — EVIDENCED |
| 8 | instant prepared/spontaneous | KBP working fixture | effect and exact spend | DEFER — EVIDENCED |
| 8 | mass-single-cost | KBP working fixture | one source cost, multi-target effects | DEFER — EVIDENCED |
| 10 | ui-routine-buttons-smoke (two fresh processes) | NO-SAVE | three HUD controls, fit, reconstruction lifecycle | PASS twice (`20260811T2201533093853Z`, `20260811T2202528930958Z`) |
| 10 | configured-routine execution | KBP working fixture | preview, real execution, result panel | DEFER — EVIDENCED |
| 11 | native-only profile (two fresh processes) | NO-SAVE | exact identities, full catalog, ordered Harmony inventory, restoration | PASS twice (`20260811T2244134771182Z`, `20260811T2245180617144Z`) |
| 11 | Call of the Wild profile (two fresh processes) | NO-SAVE | exact UMM/assembly/fixture identities, representative dynamic discovery, ordered Harmony inventory, restoration | PASS twice (`20260811T2241040368066Z`, `20260811T2242392501436Z`) |
| 11 | Tabletop Added Rules profile | KBP working fixture | exact fixture hash and dynamic discovery | UNAVAILABLE-LOCAL-REFERENCE |
| 11 | native/COTW save-backed planning and execution | KBP working fixture | provider/resource/effect/execution equivalence | DEFER — EVIDENCED |

The save-backed rows are evidentially deferred while source-only work and NO-SAVE runtime work remain independently actionable. They must not be relabeled PASS without a project-owned `KBP_` fixture.
