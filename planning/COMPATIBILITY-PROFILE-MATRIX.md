# Compatibility Profile Matrix

Status: PASS for every locally available no-save profile; save-backed execution remains `DEFER — EVIDENCED`

| Profile ID | Exact local fixture | Compile-time gameplay-mod dependency | No-save load/catalog/Harmony status | Save-backed planning/execution |
|---|---|---|---|---|
| `native-only` | Kingmaker 2.1.7b, exact platform hashes | none | PASS twice at `57ed740` | DEFER — EVIDENCED: no `KBP_` save |
| `call-of-the-wild` | Call of the Wild 1.14.4c-2.1; DLL `4ebf8e1e…`; directory manifest `26ce134f…` | none | PASS twice at `57ed740` | DEFER — EVIDENCED: no `KBP_` save |
| `tabletop-added-rules` | no supplied immutable fixture | none | UNAVAILABLE-LOCAL-REFERENCE | UNAVAILABLE-LOCAL-REFERENCE |
| `call-of-the-wild-plus-tabletop-added-rules` | Call of the Wild available; Tabletop fixture absent | none | UNAVAILABLE-LOCAL-REFERENCE | UNAVAILABLE-LOCAL-REFERENCE |

The locally possible native-plus-Call-of-the-Wild configuration is the `call-of-the-wild` profile: the transaction stages only Kingmaker Buff Planner plus the exact optional fixture. The combined profile is reserved for the distinct case where both optional fixtures exist and can be verified together.

Final exact-profile runs:

| Profile | Run IDs | Assertions | Catalog SHA-256 | Harmony inventory SHA-256 | Restoration |
|---|---|---:|---|---|---|
| `call-of-the-wild` | `20260811T2241040368066Z`, `20260811T2242392501436Z` | 26/26 each | `7b54f3f9f6d90d339c4cabeedb04c9d15bcb4d51d8e7d830150a18ab6eced659` | `a883dd60218a1f9e989a4e6b03d99318242d401d33f914cd6c068f767b308427` | exact PASS twice |
| `native-only` | `20260811T2244134771182Z`, `20260811T2245180617144Z` | 12/12 each | `50f66299912bef24a50984d9d8398ba2bb340a4f85b551a0ad6ff97c41393f3d` | `b5605e22bde458a238d63c6ffe33a99eb712bd22bf3cbc74c42d443ad479efb4` | exact PASS twice |
