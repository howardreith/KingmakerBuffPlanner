# First live casting probe — concrete proposal, NOT approved

Status: **NOT EXECUTED and NOT APPROVED.** This document proposes one
specific run. Nothing in it grants permission. The probe cannot submit a
native cast unless the owner writes an allowance file for this exact run
into the approvals directory (section 7). Claude does not create, edit or
infer that file.

## 1. What changed since the last request

- **Zero-cost native sources (review M1).** A known Unlimited pool
  (cantrips, at-will) is now one real zero-unit native reservation,
  marked verified by the existing `ResourceLedger`, carried through the
  budget, the converter, the projection identity (`identityVersion` 3)
  and both executors. An unverified zero, a missing native line, a
  missing pool or any stripped cost is refused. A spend on a verified
  free source is an execution failure.
- **Authoritative observations (review M2).** The probe reads the
  caster's `AbilityData.GetAvailableForCastCount()` and the target's
  matching buffs directly, fresh, with a shared sequence stamped at each
  read. A failed read is recorded as failed and never replaced.
- **Owned cleanup (review M3).** One idempotent terminal path owns the
  boundary, the after-read, the workspace close and the lease release for
  completion, stop, deadline, host failure, mod disable and unload.
- **Discovery wrapper.** A reference to the cast ability itself around
  plain current-target buffs is accepted; references to any other
  ability are still refused.

## 2. Selection evidence (selection-only, no boundary constructed)

| Run | Code commit | Result |
| --- | --- | --- |
| `casting-probe-select-20260923-052640` | `2117438` (pre-M1) | FAIL: no eligible casting, no reasons recorded |
| `casting-probe-select-20260923-053136` | `2117438` (pre-M1) | FAIL: cantrips refused `native-cost-count:0` |
| `casting-probe-select-20260923-062552` | `0a4c382` (M1–M3) | FAIL: zero-cost accepted; refused `effect-shape` (self-reference wrapper) |
| `casting-probe-select-20260923-063054` | `ea45807` | **PASS**: one eligible casting selected |

Every run used zero synthetic input and constructed no dispatch
boundary. In each run the workspace closed, the input lease was released
and transaction restoration was verified. No further equivalent
selection runs are planned.

## 3. Exact build identity (from the PASS selection run)

| Item | Value |
| --- | --- |
| Source commit | `ea4580770f34ebd01103f83337337bfb9fda9a74` |
| Package ZIP SHA-256 | `fb62b3444e81a13bc350a3dbca4fbb8c902cc8281fdc070026c779365a5d74c8` |
| DLL SHA-256 | `d11b5973c48cbeaae6c03d330ac09578c4eecc9ccb5efb685c53bef156191ae3` |
| Loaded MVID (runtime result) | `cfd81eab-970d-4a3d-9a95-191f794fcb62` |
| Game / profile | 2.1.7, `full-user` |

The branch has moved on since then with documentation-only commits. The
cast must run on **exactly** `ea45807`: check it out detached, run
`Build-Local.ps1`, and confirm the three hashes above match before
launching. If any hash differs, stop.

## 4. Fixture and storage

| Item | Value |
| --- | --- |
| WORKING save | `KBP_AUTOMATION_WORKING` (`Manual_305_KBP_AUTOMATION_WORKING.zks`), gameName `Hedwirg`, gameId `df33d1ff-4ec8-4707-bfa0-5e059bf9a049`, area `JamandisMansion` |
| Baseline | `KBP_AUTOMATION_BASELINE` (`Manual_304_KBP_AUTOMATION_BASELINE.zks`), same gameId |
| Identity checks | The launcher re-derives and re-hashes the pair on every run; the host verifies gameId/name before loading. Historical numeric prefixes are never trusted. |
| Candidate storage | The staged planner folder starts with no `UserSettings`. There is no legacy plan to import and no candidate is written; the only run-time change observed was Unity Mod Manager's DLL cache file. The owner's installed planner folder is restored byte-exact and verified by manifest. |
| Save writes | None. The scenario never saves. |

## 5. Exact selection and projection

| Item | Value |
| --- | --- |
| Spell | Resistance (`7bc8e27cba24f0e43ae64ed201ad5785`), level 0 cantrip, touch, `BuffAllSavesBonus` |
| Spellbook | `bc04fc157a8801d41b877ad0d9af03dd` |
| Caster | `2b56df7d-636e-4993-af38-5d54c6217e74` |
| Recipient | `050aa19a-1cf1-40f3-b28e-59d8c2fbfebf` (a different party member) |
| Source id | `effect|fd7aa6ac895aeb318a5962d6fa358ee226c81e0cdb7dcd8d98ff163e5559df76` |
| Reservation | pool `2b56df7d-…|spellbook|bc04fc15…|unlimited`, **0 units, Unlimited = true, no tokens** |
| Expected effect | buff `df680f6687f935e408eba6fb5124930e` (ResistanceBuff) on the current target |
| Strategy | `DirectRuleCast` (instant executor) |
| ProjectionId | `ee8e76b211eb8aa6deb213a5f9fd7699eba5061e67ffc00e245186068937cb89` |

Full canonical projection (the exact text the id hashes):

```json
{"format":"kbp-explicit-projection","identityVersion":3,"scope":"SingleCastProbe","steps":[{"index":0,"castingId":"probe-cast-1","sourceId":"effect|fd7aa6ac895aeb318a5962d6fa358ee226c81e0cdb7dcd8d98ff163e5559df76","provider":{"casterUnitId":"2b56df7d-636e-4993-af38-5d54c6217e74","spellbookGuid":"bc04fc157a8801d41b877ad0d9af03dd","sourceInstanceId":"level-0|heighten-0","ability":{"sourceKind":"Spellbook","baseAbilityGuid":"7bc8e27cba24f0e43ae64ed201ad5785","variantGuid":"","metamagicMask":0,"specialSourceId":""}},"anchorUnitId":null,"targetUnitIds":["050aa19a-1cf1-40f3-b28e-59d8c2fbfebf"],"expectedRecipientUnitIds":["050aa19a-1cf1-40f3-b28e-59d8c2fbfebf"],"reservation":{"poolKey":"2b56df7d-636e-4993-af38-5d54c6217e74|spellbook|bc04fc157a8801d41b877ad0d9af03dd|unlimited","units":0,"unlimited":true,"tokenIds":[]},"material":null,"expectedEffects":{"type":"ability-reference","abilityId":"7bc8e27cba24f0e43ae64ed201ad5785","child":{"type":"sequence","children":[{"type":"sequence","children":[{"type":"leaf","kind":"Buff","effectId":"df680f6687f935e408eba6fb5124930e","target":"CurrentTarget","sourceContract":"ContextActionApplyBuff","actionPath":"7bc8e27cba24f0e43ae64ed201ad5785/0:ActionList/0:ContextActionApplyBuff"}]}]}},"massCast":false,"executionStrategy":"DirectRuleCast","executionStrategyReason":"ordinary-direct-rule-cast","enhancementIds":[],"omittedEnhancementIds":[],"enhancementUsageByPool":[]}]}
```

The other discovered cantrip, Light (`95f20656…`), carries
`UniqueBuff`, meaning casting it moves the light off other party
members. That is an unmodelled side effect. It is not selected, because
Resistance sorts first; if Resistance ever became ineligible, this
request would need revisiting rather than falling through to Light.

## 6. Observation plan and pass rule

| Phase | What is read | Required |
| --- | --- | --- |
| Before (fresh, sequence n) | caster `GetAvailableForCastCount` for this ability; ResistanceBuff instances on the recipient | Read succeeds, or nothing is submitted. The selection preview read `available=0; effects=[]` (effect absent). |
| Submission (sequence n+1) | one native submission through the one-shot boundary | — |
| After (fresh, sequence > n+1) | the same two reads | Read succeeds; a failed read is FAIL, never "no change". |

A PASS requires all of the following, each recorded separately:

- **Invocation:** the one casting is `EffectConfirmed` with no failure
  record, and the outcome reports the approved ProjectionId.
- **Effect:** a new ResistanceBuff instance on the recipient. A
  pre-existing buff counts only with a verified refresh; the target was
  chosen without it.
- **Resource:** zero change in available casts. Any decrease is
  `unexpected-paid-resource-loss`.
- **Cleanup:** the executor lifecycle settled, the boundary was disposed,
  the workspace closed and the input lease released.
- **Restoration:** the transaction is restored and the Mods manifest
  verified. This does not undo the in-process buff.

The value `available=0` is the game's own report for this cantrip, and
what it means is not verified. The rule therefore requires equality
before and after, not any particular number. If the game refuses the
cast, the result is a truthful FAIL.

A cantrip PASS does **not** prove finite resource consumption. The
later paid-slot probe needs its own request and approval.

## 7. Proposed run and one-shot allowance

| Item | Value |
| --- | --- |
| Proposed cast run id | `casting-probe-cast-20260923-01`. It is distinct from every selection run id, and the launcher refuses a reused id. |
| Allowance scope | this run id, `sourceCommit` `ea4580770f34ebd01103f83337337bfb9fda9a74`, the ProjectionId, caster, recipient and source id above, `maximumNativeSubmissions` 1 |
| Template | `docs/probe/ALLOWANCE-TEMPLATE-UNAPPROVED.json`. It sits outside the approvals directory and has an empty `approvedBy`, which the host refuses, so it cannot be used as-is. |
| Deadline | 60 s run deadline; `probe-stop.json` in the run's evidence directory stops it |
| Terminal cleanup | The owner disposes the boundary (executor cleanup), takes a fresh after-read, closes the workspace, releases the lease, records cleanup and publishes `probe-outcome.json` once. Mod disable, unload or a host exception take the same path. Then the transaction restores Mods. |

If approved, the owner would copy the template to
`C:\Dev\KingmakerBuffPlannerLab\approvals\casting-probe-cast-20260923-01.json`,
set `approvedBy`, and launch from the detached, hash-verified `ea45807`
checkout:

```powershell
& 'scripts/Invoke-KingmakerRuntimeTest.ps1' -Scenario live-cast-probe `
  -CompatibilityProfileId full-user -RunId casting-probe-cast-20260923-01 `
  -ProbeAllowancePath 'C:\Dev\KingmakerBuffPlannerLab\approvals\casting-probe-cast-20260923-01.json' `
  -TimeoutSeconds 900 -Confirm:$false
```

## 8. Casting stays disabled everywhere else

- The casting-first workspace always uses `DisabledCastingDispatchBoundary`
  (test `single-cast-probe-is-dormant-and-one-shot`).
- `SingleCastProbeBoundary` is constructed only in the runtime-test host
  (a source scan in the same test).
- The protocol accepts `probeAllowance` only on `live-cast-probe`, and
  the launcher requires the allowance file (`Test-RuntimeLauncherFileWhatIf.ps1`).
- The probe scenarios request no synthetic input: no hotkey marker and
  no physical-input requests.
- The legacy HUD routine buttons are the released pre-migration planner
  and still execute legacy routines when a user clicks them. The probe
  sends no input, so they are not triggered. They are outside the
  casting-first path and unchanged by this work.
