# One-cast Resistance probe

Status (2026-09-23): **run on the current candidate; PASS on the second
identified run.** `casting-probe-cast-20260923-p2-02` (frozen `320a1b6`)
cast Resistance through the reviewed one-cast boundary and confirmed a new
ResistanceBuff instance on the recipient in a fresh read, with no resource
spent, no violation, verified Mods restoration and unchanged protected
saves. The first identified run, `casting-probe-cast-20260923-p1-01`
(frozen `7664f2f`), submitted inside the open planner (FullScreenUi) and
correctly reported `TimedOutUnconfirmed`; that cause was fixed in
`320a1b6`. Exact identities and outcomes:
[`docs/evidence/casting-probe-20260923-receipt.md`](evidence/casting-probe-20260923-receipt.md).

Under the owner's delegated mission authority (owner message of
2026-09-23, sections 1 and 3), Claude writes each run's allowance
mechanically: fresh run id, exclusive creation, the frozen candidate's
exact identities, the recomputed projection, and the same Resistance
source, caster and recipient (no substitution; a different selection
would have been recorded and not run). The historical `e964d2f` checkout
and its frozen artifact are preserved unchanged; the proposal below is
that historical version, kept verbatim for review.

## Historical proposal for `e964d2f` (preserved)

The previous version of this proposal (for `7379dc8`) is kept verbatim in
`docs/probe/history/LIVE-CAST-PROBE-REQUEST-7379dc8.md`. It is
superseded: its artifact binding was insufficient (review N1). Its
binary hashes are not reproduced.

## 1. What changed since the last request

- **Frozen-artifact binding (review N1).** The allowance (schema 2)
  now binds commit, package SHA-256, DLL SHA-256 and assembly MVID. The
  launcher refuses a mismatch against the build manifest before
  deploying. Before any observation, executor construction or
  submission, the host measures the LOADED identity: the compiled-in
  commit, the verified package, the hash of the loaded DLL file and the
  loaded module's MVID. A same-commit replacement binary is refused.
- **Terminal shutdown from creation (review N2).** The probe owner
  exists from runtime-host creation. After a disable, unload or host
  failure at any point, no later update or re-enable can select, arm or
  submit.
- **Exact prepared-slot observation (review N3).** Observation no longer
  depends on a slot being spendable; it reads exactly the reserved
  tokens. This is needed for the later paid prepared-slot probe, not for
  this zero-token cantrip.

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

## 3. Build identity and freeze

The selection PASS (section 2) ran at `ea45807`. The selection code, the
converter and the projection identity have not changed since. The N1–N3
commits changed the boundary, the owner, the host shutdown path and the
observer, but not selection or the ProjectionId.

The cast would run on the **frozen artifact built at the final N-series
handoff commit**. Builds are not byte-reproducible, so the artifact is
frozen rather than rebuilt:

- The final gate's package ZIP and its build manifest are copied to
  `C:\Dev\KingmakerBuffPlannerLab\runtime-backups\probe-frozen\<commit>\`
  together with `FREEZE.json`, which records the commit, package SHA-256,
  DLL SHA-256 and MVID.
- The same four values appear in the PR #2 body and the handoff. They
  are not in this file, because a commit cannot contain its own package
  hash.
- Before a run, the package in `artifacts/local-runtime/0.1.1-rc3/`
  must hash to the frozen value. If anything was rebuilt, copy the
  frozen ZIP and manifest back first. The launcher refuses an allowance
  whose package, DLL or MVID differs from the manifest it deploys, and
  the host refuses before submission if the loaded DLL or MVID differs.

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
| Allowance scope | this run id, the frozen commit, package SHA-256, DLL SHA-256 and MVID (from `FREEZE.json`), the ProjectionId, caster, recipient and source id above, `maximumNativeSubmissions` 1 |
| Template | `docs/probe/ALLOWANCE-TEMPLATE-UNAPPROVED.json`. It sits outside the approvals directory and has an empty `approvedBy`, which the host refuses, so it cannot be used as-is. |
| Deadline | 60 s run deadline; `probe-stop.json` in the run's evidence directory stops it |
| Terminal cleanup | The owner disposes the boundary (executor cleanup), takes a fresh after-read, closes the workspace, releases the lease, records cleanup and publishes `probe-outcome.json` once. Mod disable, unload or a host exception take the same path. Then the transaction restores Mods. |

If approved, the owner would copy the template to
`C:\Dev\KingmakerBuffPlannerLab\approvals\casting-probe-cast-20260923-01.json`,
copy the four `SET-FROM-FREEZE-RECORD` values from `FREEZE.json`, set
`approvedBy` to their name, confirm the local package hash equals the
frozen one, and launch from the checkout at the frozen commit:

```powershell
& 'scripts/Invoke-KingmakerRuntimeTest.ps1' -Scenario live-cast-probe `
  -CompatibilityProfileId full-user -RunId casting-probe-cast-20260923-01 `
  -ProbeAllowancePath 'C:\Dev\KingmakerBuffPlannerLab\approvals\casting-probe-cast-20260923-01.json' `
  -TimeoutSeconds 900 -Confirm:$false
```

## 8. Casting stays disabled everywhere else

- A mismatched commit, package, DLL or MVID refuses with no observation,
  no executor construction and no submission
  (`probe-requires-frozen-artifact-identity`, launcher binding cases).
- A shutdown at any point is terminal for the request
  (`probe-shutdown-before-selection-is-terminal`,
  `probe-owner-terminal-cleanup`).

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
