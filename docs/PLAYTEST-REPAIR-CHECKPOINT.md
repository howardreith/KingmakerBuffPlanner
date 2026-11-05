# Playtest repair checkpoint

Branch: `codex/kingmaker-buff-planner-playtest-repairs`.
Baseline: `29c25260824558dabcb06723c02cec1932383781` (current main after one fetch).
Frozen product candidate: `78ae896f3de28eed57a8ecf8efe70679986158d2`.
Final test source: `a384ba827fc14037b51f8536a4574ea86fae47b4`; the test-only commit changes no product source.
Version: 0.6.10, unreleased repair changes. No merge or release.
Draft PR: https://github.com/howardreith/KingmakerBuffPlanner/pull/20.

The handoff ZIP SHA-256 is
`603da7dc3f8a96ebcc9becced11c9c65cedeba4df58d8ab320961a1c3f398400`.
All 17 SHA256SUMS entries verified. Originals remain private and unchanged.

## Evidence and repairs

**H:** The supplied 0.6.10 runs show a new/replaced unsuppressed Heroism
instance and one available cast consumed, followed by failed confirmation and
nine unattempted castings. They do not include the expression or every poll.
The earlier completed full-user native catalogue `kbp041-rel-catalog-full`,
generated at `85e7410c816cf02271436523ad82b0777c4e443c`, supplies the actual
three-leaf expression: mandatory target HeroismBuff; a caster-fact condition
whose true branch applies InfectiousCharmsHeroismBuff to the caster and a
reflected nearby-target wrapper containing InfectiousCharmsHeroismTargetBuff;
empty false branch. Catalogue SHA-256:
`d9c7e5bb2eb666d45ef5f18c4068d3a3631b02cffd77624315a1165e0921954d`.
This is historical loaded-mod evidence, separate from this candidate's native
acceptance and from the owner's reported character state.

The previous presence evaluator drops the empty alternative, making the gated
additions mandatory. The repair preserves the structural graph and its stable
catalogue identity, captures only the exact native ContextConditionCasterHasFact
contract (And/Or and Not), reads those facts per selected provider, and binds
proven branches for both active-effect assessment and execution projection.
Unreadable or unsupported predicates remain unknown. Applicable additions and
mandatory sequences remain requirements; an empty selected branch supplies no
positive confirmation evidence. Unchanged expressions retain object identity.
No spell-name/GUID exception, resource-accounting change, retry or timeout change.
Condition metadata is exported for future native inspection but is excluded from
the catalogue identity calculation. Source-expression lookup still prefers the
selected ability over its catalogue representative. Strategy/Share option copies
retain the caster-fact snapshot.

The regression preserves the captured nested/conditional/target structure and
real Heroism IDs through scanner, compiler, gate, converter, both executors,
coordinator, session and reporting. Its feature identity and external native
world are fixtures, not new native observations. It exercises fresh application,
replacement with another remaining prepared slot, a distinct affordable sentinel,
active skipping, unknown/applicable gates, missing mandatory effect and negative
controls for unchanged/missing/suppressed/unreadable/wrong-kind/wrong-unit effects.
The first/repeat/skip progression never refills either resource pool. The sentinel
is explicitly a simulated distinct spell, not native Mirror Image acceptance.

**T:** The supplied log has ten Material.HasProperty -> GenerateTextMesh
exceptions and the screenshots lose several body/title/heading labels, not only
the policy caption. Installed TMP IL at `GenerateTextMesh` IL_0cda reads
`m_currentMaterial` and then calls HasProperty, consistent with an invalid material.
The supplied evidence does not identify that instance or prove donor-cache
lifetime is the only cause. The mouse kit previously cached borrowed materials
across workspace lifetimes and validated only its font/canvas cache key.

Both input presentations now own bounded material copies per workspace and role.
Live atlas-compatible donors are preferred, with the native font's material as
fallback; a loaded native text font is the fallback if the preferred face is
unavailable. Native/shared donors are never changed or destroyed. Only owned
copies have inherited clip/stencil context reset; normal local masking remains.
Owned materials are released at workspace teardown, destroyed Unity objects are
detected through native liveness, and disposed owners cannot allocate again.
Existing unparented-Awake/controller typography ordering remains. The bounded
label probe now also covers the first two mouse refreshes and identifies the label,
font, material, shader and atlas before attempting mesh generation. A property or
frame-end receipt is not glyph-rendering proof. This repair is provisional until
the specified rendered lifecycle observations pass.

**M:** Both buttons keep the ordinary native factory and state sprites. Explicit
red/bold override and WarningMark are removed. The modal clears Unity selection
instead of selecting Cancel (installed Selectable selection pins Highlighted),
while semantic focus still starts at Cancel and resumes on keyboard/controller
input. Mouse opening does not force a cyan semantic focus overlay. Confirmation,
Undo/autosave, run ownership, description blocking, input neutral/release guards
and focus return remain on their existing paths. Real pointer/focus transitions
still require native observation.

**W:** Source rows show Item charge without the group suffix. The banner says
Area buff only for discovered AlliedAreaRecipients; party/group and direct
sources retain their own targeting names. Selected group origins say Centred on
the character without repeating group and caster/centre text. Chips say Predicted
reach and retain the coverage disclosure. The two-owner item test checks exact
source identity, one charge from each independent pool, origins, coverage and
unchanged saved intent. Loading/reselection disclosures remain unchanged.

## Validation

Commands run through Windows PowerShell 5.1 with `-NoProfile -ExecutionPolicy
Bypass -File`, unless noted. Full logs are local under `artifacts/playtest-*`;
only minimal redacted review evidence belongs in the repository.

- `artifacts/playtest-baseline.ps1`: controlled ablation restoring the old
  unbound compiler/converter behavior, same new captured-expression regressions:
  **0 PASS / 2 FAIL**, source 47/0, build 1/0, 7.9 seconds. Both modes apply the
  expected buff but stop the sentinel. This is an old-behavior reproduction on
  the working repair source, not a claim that the newly added runner existed at
  pristine baseline HEAD. Exact original files restored in finally.
- `scripts/Test-Development.ps1 -Only 'playtest,heroism-repeat,clear-plan,cast-available,duplicate-items,stale-item,graph-group-casting,controller-workspace-spatial-modal,native-text-material,group-mixed-coverage-keeps-adequate-recipients,direct-manipulation-group-confirmation-needs-one-reached,confirmation-needs-an-instance-this-attempt-applied,existing-effect-sufficiency-rules'`:
  **77/0**, source **47/0**, build **1/0**, 11.1 seconds (pre-freeze focused pass).
- First native prerequisite attempt at ee3bbfb: **675/1**, with failure
  `probe-catalogue-distinct-spells-keep-own-expression`. Binding copied unchanged
  expression objects. Corrected source, retained the assertion and original log
  (`artifacts/playtest-native-prerequisites-first.log`). No native launch followed.
- Correction: `scripts/Test-Development.ps1 -Only 'probe-catalogue-distinct-spells-keep-own-expression,playtest-heroism,probe-catalogue,ability-expression'`:
  **4/0**, 7.5 seconds. Preserve identity when binding changes nothing.
- `scripts/Build-Local.ps1` on the final product: source **47/0**, build **1/0**,
  package **6/0**, local package **1/0**, 6.5 seconds.
- `scripts/Test-SourceOnly.ps1`: explicitly selected once for actual native-launch
  prerequisites after the frozen candidate; not the development default.
  Completed full offline C# **676/0** and bounded harness/readers; aggregate **INCOMPLETE** at the deployment history window (details below).
- `scripts/Test-GuardedPush.ps1`: **6/0**, 4.3 seconds. Project-owned guarded push
  verified remote candidate SHA and opened draft PR #20; no merge/release.

Candidate DLL SHA-256:
`c9e6af303161d2efa64fcc3e89626cb76b81dc6d3475fe5c088da66d94454b93`.
Local runtime ZIP SHA-256:
`32913b9f193f499dc369faa81535cf1e3f4e81dbabc185aa1d2f303f7ed0d456`.
MVID: `7475c189-a819-41ba-a581-0d94105c66e4`.
The local package is an unreleased test artifact, not an instruction to install
or replace the public release. Documentation commits do not rebuild these bytes.

Rejected theories: unchanged old Heroism does not explain the supplied terminal
reads; rule success/spending alone is not effect confirmation; the flat GUID list
does not supply branch semantics; two reservation units versus one available cast
does not prove double spending; checked strict policy does not prove a migration
failure; populated label properties do not prove rendering. No unrelated mod
disable sweep or blanket continue-on-failure was introduced.

## Native acceptance and next action

Selected native scope: required safety/source/WhatIf prerequisites, one current
full-user loaded graph export, one 1920x1200 native workspace capture using the
same verified candidate. Separate 45-minute planning budget; any transaction must
finish cleanup/restoration even if it crosses that boundary. No live casting
allowance is fabricated or reused. Only guarded disposable workflows may run.

The full-user read-only compatibility preflight was **REFUSED** with
`Compatibility fixture identity mismatch: ZFavoredClass`. That installed
directory no longer matches the sealed profile. No profile, mod or guard was
changed to obtain a pass. Separate native-only and call-of-the-wild read-only
preflights passed; the primary display is 1920x1200. These checks are not launches.

The final prerequisite invocation began at 2026-11-04 20:49:32 -05:00. Completed:
launcher arguments **28/0**, source **47/0**, full C# **676/0**, filesystem harness
**38/0**, physical-search reader **20/0**, problem-navigation reader **35/0**,
spellbook-entry reader **50/0**. At 21:07:37 -05:00 (**1084.8 seconds**) it was
still reading the first deployment-purity history window. Several further full
history comparisons would exceed the selected 45-minute native budget. Only
that read-only process was stopped after verifying its exact command identity,
zero children, no Kingmaker process, and no own or foreign deployment lock.
The aggregate exit was -1 (stopped), not PASS. No runtime transaction was killed
or left pending. Deployment WhatIf, launcher -File WhatIf purity, fixture-inventory,
restore-install and publisher aggregate stages were not completed. No native
launch was attempted, including on the narrower passing profiles.

Final focused command repeats the 77-test selector above and adds
`probe-catalogue-distinct-spells-keep-own-expression`; the `playtest` prefix now
also contains the new installed-assembly caster-fact contract regression:
**79 PASS / 0 FAIL**, source **47/0**, build **1/0**, **15.3 seconds**.
The DLL hash is still exactly the frozen candidate hash above. Reader executable
SHA-256: `bbc4b2c5759009d8f2a2c14d9239a4f63558b66edd5f46c175d83da066ec3bb8`.
The additional test uses the compiled native adapter with installed Condition,
ConditionsChecker and blueprint types, preserving Not/Or and refusing an unknown
predicate or a destroyed native fact. It substitutes native construction/liveness;
it never invokes Condition.Check, Rulebook or a game context. The earlier 676-test
receipt predates that one test; no new full-suite count is inferred.

Minimal review receipts are in [the evidence directory](evidence/playtest-repair/focused.txt),
including [old-behavior failures](evidence/playtest-repair/baseline.txt),
[the captured historical expression](evidence/playtest-repair/historical-heroism-expression.json),
[the retained first prerequisite failure](evidence/playtest-repair/first-prerequisite-attempt.txt),
and [completed prerequisite stages](evidence/playtest-repair/completed-prerequisite-stages.txt).
Private full logs, original owner images/log and local packages remain untracked.

| Workstream | Source repair | Automated evidence | Native acceptance |
| --- | --- | --- | --- |
| H | Proven caster-fact branch binding implemented | Both-mode captured-expression continuation, skipping, negative controls and installed-contract checks pass | BLOCKED/NOT RUN for current reported environment; no fresh loaded graph, first/repeat Heroism, next spell or actual native spending on this candidate |
| T | Material lifetime repair implemented provisionally | Atlas policy and modeled ownership/liveness/teardown checks pass | NOT RUN: no rendered cold/reopen/modal/load frames; failed material's exact owner still unknown |
| M | Ordinary buttons and semantic/Unity selection separation implemented | Existing compiled input/description/lifecycle and modal focus neighbors pass | NOT RUN: pointer normal/hover/exit, physical focus and press/cancel not observed |
| W | Area/resource/centre copy implemented | Two-owner exact-source, charge, coverage, saved-intent and non-area distinctions pass | NOT RUN on candidate; owner's previous working Signet mechanics remain historical evidence |

The mission's source changes and draft PR are delivered; its native acceptance
criteria remain open. No claim of native gameplay or visual repair qualification
is made from compilation, fixtures, the historical catalogue, or preflight checks.

Exact next actions:

1. Review draft PR #20. Resolve the sealed ZFavoredClass profile drift through
   the existing approved profile/fixture workflow before attempting the reported
   full-user configuration. Do not silently reseal or update dependencies.
2. Select a separately budgeted completion of the outstanding deployment and
   launcher purity gates. Keep the frozen product checkout/package at the exact
   recorded HEAD; later documentation/test commits are separate provenance.
   `Read-KbpBuildManifest` requires matching HEAD and package path, so do not run
   the old package from a newer documentation HEAD or edit its identity manifest.
3. For gameplay acceptance, implement the narrow real-Heroism-plus-distinct-spell
   guarded recipe that the previous investigation still lacks. Its selection must
   capture current post-mod graph, actual caster-fact result, recipient roles and
   confirmation predicate/polls. Use completed frozen-build selection, the existing
   `New-KbpRunAllowance.ps1` writer and genuine remaining resources for both modes.
   Never reuse an old allowance or substitute the same-spell finite recipe as proof.
   Any observer/recipe product change needs a new frozen binary and affected tests.
4. Under guarded disposable testing at 1920x1200, inspect all labels at cold open,
   close/reopen, ordinary modal open/close and load/reopen. Check Require full plan
   both checked and unchecked without changing its persisted meaning. Observe both
   modal buttons normal -> hover -> exit and press/cancel, then one safe keyboard/
   controller sequence and held/opening edge. No frame was obtained in this turn.

Owner test checklist, once a candidate is staged through the guarded workflow:

- In Instant and Animated, Heroism first and repeat/recast must confirm and a
  legal distinct Mirror Image or Heightened Awareness must run afterward.
- Reopen and load/reopen: title, selected buff, headings, search placeholder,
  feedback and Require full plan caption remain visible; no associated TMP errors.
- Cancel and Clear plan look ordinary, visibly hover/unhover, and Cancel remains
  the safe keyboard/controller action. Confirm clear once; Undo restores the plan.
- Two Blessed Signet owners show Item charge, Area buff and their own centres;
  exact item choices, independent charges and predicted coverage remain correct.
