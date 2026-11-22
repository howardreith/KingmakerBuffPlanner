# Missing beneficial buffs mission

Owner mission of 2026-11-21. This file is the technical contract for the
`codex/kingmaker-buff-planner-missing-beneficial-buffs` branch and its draft PR.
It is executed under the installed agent-review controller (`single` plan): the
developer implements, a Codex reviewer independently reviews, and the
controller manages the exchange. This file contains no enrollment or controller
launch instructions; the developer never starts, enrolls or restarts a
controller.

Fix the missing-beneficial-buffs problem in **Kingmaker Buff Planner**,
including the related exclusions described below.

## 1. Owner authorization and limits

Authorized: the routine work needed to implement and validate these changes,
commit them, push the assigned branch through the repository's guarded push
helper, and (through the controller) update the draft PR with automated review
requests, findings, dispositions and status reports.

Not authorized: merging, tagging, publishing a release, enabling automatic
merge, requesting human reviewers, or replacing the owner's normal-play
installation. The PR remains a draft. Final owner acceptance, merging, release
publication and normal-install deployment remain with the owner.

## 2. Claude/Codex review contract

- **Developer:** Claude Opus with high effort.
- **Independent reviewer:** Codex using **`gpt-6.2-astra`**, with high reasoning
  effort and the configured read-only review restrictions.

Model, permission, budget and completion settings are not changed to obtain a
successful run.

The established exchange:

1. Claude completes the assigned checkpoint and required focused validation.
2. Changes are committed and pushed through Buff Planner's required push path.
3. The controller captures the exact candidate, comparison commits, mission
   identity, and evidence.
4. Codex independently reviews that fixed candidate against the mission and
   actual evidence.
5. The controller validates and publishes the complete review.
6. Claude gives a disposition for every actionable finding, makes supported
   corrections, and validates affected behavior.
7. Codex reviews the corrected candidate or evaluates a substantive
   evidence-backed rebuttal.
8. The loop continues under the configured plan and persistent budgets.

The final successful state is **`AWAITING_OWNER_REVIEW`**.

Keep these distinctions explicit:

- Codex is the reviewer; Claude owns implementation and fixes.
- "No reviewers assigned" means **no human reviewer assignments**. Automated
  Codex reviews and developer replies are required.
- A prior clean review does not cover later material changes.
- A rebuttal is not accepted merely because Claude labels it accepted.
- Agent agreement does not replace required test or native-game evidence.
- Missing required evidence, an unavailable model, exhausted limits, or a
  genuine unresolved disagreement must produce the appropriate **`BLOCKED`**
  or **`PAUSED`** outcome (developer status `blocked`, `needs_owner_decision`
  or `paused`), not a manually asserted pass.

## 3. Technical mission and evidence baseline

The owner's bard reached level 7 and learned Good Hope and Haste. Haste appears
in the planner's castable buffs, but Good Hope does not, even after resting and
saving/reloading.

The preceding investigation examined:

`dd3e997100916576a268b24e46d49b4db7c34481`

That commit is an evidence baseline (it is also this branch's starting point,
`origin/main` on 2026-11-21). Work from current source and reconcile relevant
changes before implementing.

Read the relevant discovery, architecture, and execution documentation. Follow
the existing C# 7.3 / .NET Framework 4.7 conventions. Keep discovery, planning,
execution, and UI responsibilities separate. Avoid new dependencies.

### A. Fix Good Hope and Firebelly

`NativeCandidateClassifier.IsSafeRecipient` currently rejects every effect when:

`EffectOnAlly == "Harmful"`

The native Kingmaker 2.1.7b blueprints carry that misleading metadata on two
legitimate buffs:

| Spell | Ability GUID | Applied buff GUID |
|---|---|---|
| Good Hope | `a5e23522eda32dc45801e32c05dc9f96` | `85af9f0c5d29e5e4fa2e75ca70442487` |
| Firebelly | `b065231094a21d14dbf1c3832f776871` | `7c33de68880aa444bbb916271b653016` |

**Good Hope** targets allies through `AbilityTargetsAround` and applies a
substantive beneficial morale buff. Its native radius is 15 feet. The
harmful-ally veto causes the classifier to return `harmful-ally-disposition`.

**Firebelly** applies a beneficial caster buff granting fire resistance and a
separate fire-breath ability. The initial cast does not perform the damaging
breath attack. It is rejected by the same rule.

**Haste** provides a useful comparison: its ally disposition is `None`, so it
survives this filter. Good Hope and Haste both use a conditional
remove-opposing-debuff/otherwise-apply-buff structure; that structure is not
the distinguishing cause.

The harmful-ally veto was introduced to exclude Irresistible Dance. Its
assumption that this metadata is always authoritative is incorrect.

Correct the shared eligibility logic so the actual Good Hope and Firebelly
blueprints are admitted while genuinely harmful abilities remain excluded.

Keep the catalog primarily structural. Evaluate actual effects, recipients, and
action semantics. If a narrow metadata exception is necessary, make it explicit
and evidence-backed.

**Preserve the entire discovered effect expression.** The current
`NativeEffectOverrides.json` `"include"` mechanism replaces the discovered
graph. A normal unit-buff override could erase Good Hope's allied-area
targeting and conditional branch. Do not use that shortcut unless those
semantics are preserved.

Do not broadly remove harmful/hostile checks or modify native spell behavior to
accommodate the planner.

### B. Fix the Abjuration resistance selections

The investigation also found erroneous exclusions affecting:

- Resistance — Acid
- Resistance — Cold
- Resistance — Electricity
- Resistance — Fire
- Resistance — Sonic

Their internal names follow:

`AbjurationResistance{Element}Ability`

These are class abilities. Their native blueprints have `Range = Personal` and
`CanTargetSelf = true`, but also carry `CanTargetEnemies = true` and
harmful-enemy metadata.

The current hostile-current-target rule rejects them despite their substantive
personal resistance effects. A later hostile-target audit rule can also reject
them, so changing only the first gate is insufficient.

Correct classification using the actual personal targeting contract.

These are **mutually exclusive permanent selections**. Each applies its
resistance and removes the other four. Preserve native resource costs and
switching behavior. Do not advertise five stackable resistances, invent finite
durations, or repeatedly recast an already satisfied choice.

### C. Resolve four beneficial spells blocked by point-target support

| Spell | Ability GUID | Native ally radius |
|---|---|---|
| Blessing of Luck and Resolve, Mass | `462c21cebf7820c40a87f5e4d03e17cf` | 30 feet |
| Feather Step, Mass | `d219494150ac1f24f9ce14a3d4f66d26` | 15 feet |
| Firebrand | `98734a2665c18cd4db71878b0532024a` | 30 feet |
| Heroic Invocation | `43740dab07286fe4aa00a6ee104ce7c1` | 30 feet |

These hit the blanket `CanTargetPoint` rejection. This is a separate support
restriction.

All four inspected native blueprints also allow self/friend targeting,
disallow enemy targeting, and have an explicit allied `AbilityTargetsAround`
selector. The current rule appears to treat "can target a point" as "requires
arbitrary ground placement."

Verify each against the current loaded blueprint and exact native targeting
contract. Implement deterministic caster/ally-anchor support wherever the
native spell permits it, reusing existing targeting and coverage
infrastructure.

Preserve native target validation, the selected center, radius, recipients,
and resource spending. Do not invent ground coordinates or broadly admit
unrelated point-target spells.

Resolve all four individually. If a native contract prevents safe support,
document the precise limitation and complete the other repairs. An unresolved
targeting extension must not hold up the confirmed Good Hope and Firebelly fix.

### D. Audit related false negatives

Review the complete candidate inventory for other legitimate beneficial spells
or abilities rejected by these rules.

Include candidates excluded under **both** `Current` and `Pre040` classifier
modes. The existing audit report can omit those entries; reviewing only
included or newly removed rows is insufficient.

For each credible candidate, retain its stable identity, relevant
targeting/effect facts, current exclusion reason, post-repair disposition, and
supporting evidence.

`planning/NATIVE-BUFF-CATALOG.json` is historical. Its underlying facts are
useful, but its old inclusion/exclusion labels are not current runtime results.

Preserve existing behavior for already handled metadata inconsistencies,
including Freedom of Movement and True Seeing.

Inspect available modded blueprints when their profiles and assemblies are
present. Identify the exact scope inspected; do not claim full installed-stack
coverage from a native-only audit. Keep unrelated findings separate from this
repair.

## 4. Required regression coverage

Start with reproductions that fail on the relevant baseline. Retain the
misleading native flags in the fixtures. Do not normalize test inputs simply to
make the baseline pass.

The existing `TestCommunalStructuralCanaries` includes Good Hope but does not
prove its real metadata survives classification into the castable catalog.

Add meaningful coverage through the relevant **adapter → scanner → classifier
→ provider/catalog** path, using existing infrastructure, faithful blueprint
fixtures, and exact assembly-backed contracts where available.

### Good Hope and Firebelly

Verify:

- A bard that knows Good Hope receives a castable entry.
- Haste remains available.
- Good Hope retains its native allied-area semantics and radius.
- Resource spending matches native casting; area coverage does not cause
  unnecessary per-recipient casts.
- Out-of-radius units are not reported as covered.
- Existing-buff and recast decisions use the real persistent effects.
- Firebelly is included, while its separately granted damaging breath remains
  excluded.
- Reopening or refreshing the planner retains correct discovery.

Test Good Hope's **Crushing Despair** branch. In the inspected native graph, an
affected recipient has Despair removed instead of receiving Good Hope on that
cast. Preserve that behavior and truthful confirmation. Do not report that Good
Hope was applied, silently cast again, or change the native spell to satisfy an
assertion.

### Abjuration resistance selections

Verify:

- All five valid selections are discoverable for an eligible character.
- Personal targeting and native costs are honored.
- Switching selections removes the previous resistance.
- Re-selecting the current choice behaves correctly.
- The planner does not claim simultaneous coverage from mutually exclusive
  choices.

### Supported group spells

For each point-capable group spell admitted through the verified unit-anchor
path, establish:

- Castable catalog availability.
- A legal native target.
- Correct area center, radius, and recipients.
- Correct resource expenditure.
- No coverage claims for out-of-area units.

### Negative controls

Use representative real action/targeting shapes for:

- Irresistible Dance.
- Hideous Laughter.
- Call Lightning.
- Firebelly's damaging breath.
- Harmful-condition-only abilities.
- Restoration-only abilities and cooldown markers.
- Unsafe or offensive point-target effects.

Do not replace the old blanket harmful-ally test with another synthetic test
that merely restates the replacement rule.

## 5. Focused validation and native checks

Use `scripts/Test-Development.ps1` with explicit relevant prefixes and affected
dependencies, following `docs/VALIDATION-POLICY.md`.

Relevant existing families include native-candidate classification,
communal/area semantics, catalog audit, catalog adjudication, and
persistent-beneficial classification. Add focused regressions for the repaired
behavior.

Compile affected production code, run the selected tests, and supply the
results to Codex with the candidate. Corrections should first receive the
failing reproduction and affected-neighbor checks.

Do not automatically resume historical qualification campaigns, repeatedly
rebuild unchanged candidates, or run the full release suite after every
correction.

Perform focused native checks when the configured environment and repository
safeguards permit them. Verify actual listing, targeting, casting, resource
use, and persistent effects. Catalog appearance alone does not establish that
the targeting changes work.

Use only repository-owned guarded runtime procedures:

- Preserve the immutable baseline and protected saves.
- Only the designated automation working save may be mutable.
- Transactional test staging must restore the installation exactly.
- Do not interrupt an active game session.
- The independent reviewer must not run a competing game/staging transaction.

If a required native check cannot run, preserve the implementation and
completed offline work, and report the exact missing evidence (developer status
`blocked` or `needs_owner_decision` as appropriate). Do not convert unavailable
verification into a pass.

Compilation, detached reflection, a main-menu load, or agreement between the
agents does not establish gameplay validation.

## 6. Source references

Useful production files, relative to `src/KingmakerBuffPlanner/`, include:

- `Discovery/NativeCandidateClassifier.cs`
- `GameAdapters/KingmakerBuffSourceDiscovery.cs`
- `GameAdapters/KingmakerPartySnapshotBuilder.cs`
- `GameAdapters/KingmakerActionGraphAdapter.cs`
- `Discovery/AreaRecipientSemantics.cs`
- `GameAdapters/KingmakerProviderOptionBuilder.cs`
- `GameAdapters/KingmakerAreaCoverageResolver.cs`
- `Discovery/EffectOverrideRegistry.cs`

Native reference data is the **Blueprints2.1.7b.zip** release asset of
https://github.com/xADDBx/KingmakerDataminer/releases/tag/v1.0.0. A local
read-only copy was staged outside the repository on 2026-11-21:

- Archive: `C:/Dev/KingmakerBuffPlannerLab/native-blueprints-2.1.7b/download/Blueprints2.1.7b.zip`
  (96,717,702 bytes, sha256
  `726a58e28583cc0a2cb5e0d3ff7c9f20bc0b7c791f5a9cc200fcd1338f5b4834`)
- Extracted (85,316 JSON files, one folder per blueprint type):
  `C:/Dev/KingmakerBuffPlannerLab/native-blueprints-2.1.7b/extracted/`
  e.g. `Kingmaker.UnitLogic.Abilities.Blueprints.BlueprintAbility/GoodHope.a5e23522eda32dc45801e32c05dc9f96.json`

This dump is reference data for facts and fixtures; it does not replace the
loaded-assembly/loaded-blueprint contract checks required above. Do not commit
it.

The investigation scanned 1,722 native abilities and ran the unmodified
classifier through 19 focused native-fact and diagnostic cases. It reproduced
the exclusions described above. That was investigation evidence, not an
implemented repair or native-game qualification.

## 7. Environment facts for this run

- The controller's `focused` gate runs `scripts/Test-Development.ps1` and its
  extra-argument pattern is `-Only [A-Za-z0-9._-]+`: one prefix per gate
  receipt (commas are not accepted there). Choose the gate prefix so that it
  actually exercises the repaired behavior (for example a shared prefix for the
  new regressions), and record any broader multi-prefix `Test-Development.ps1`
  selections with their exact commands and pass/fail counts in the report.
- Prior lab observations to re-verify, not assume: the automation fixture party
  historically had only cantrip spellbook buffs and no mutable prepared caster;
  physical runtime runs have needed an idle desktop and a non-console
  foreground window. If the fixture cannot exercise a required native check,
  report that precisely rather than substituting weaker evidence.

## 8. Review emphasis and final handoff

The reviewer receives the full mission, applicable repository instructions,
candidate diff and surrounding code, regression results, candidate audit, and
available native evidence. A developer success summary alone is insufficient.

In addition to its independent review, the reviewer examines these concrete
risks:

- Relaxed metadata handling admits harmful abilities.
- An override or normalization discards conditional or area semantics.
- Eligibility changes are not reflected consistently in provider creation or
  execution.
- Area coverage or resource accounting becomes incorrect.
- Resistance selections are treated as stackable.
- Historical catalog results are presented as current evidence.
- A required acceptance gap is hidden as optional follow-up.
- The review or validation results refer to an earlier candidate.

Update relevant project records (see `AGENTS.md` durable records) and preserve
the substantive exchange according to the controller protocol.

The final report states:

- Root causes and changes.
- Each named spell or ability family: fixed, verified, limited, or blocked,
  with reasons.
- Actual focused test results and candidate-audit scope.
- Native checks completed and remaining limitations.
- Outstanding findings and their dispositions, if any.

The endpoint is a reviewed draft PR awaiting owner review, or an accurately
reported blocker with the work preserved.

## 9. Owner addendum of 2026-11-21: in-game checks move to owner acceptance

This addendum is an explicit owner decision. Where it conflicts with sections
2, 5 or 8, this section governs.

Background: the first enrollment of this mission stopped `BLOCKED` before any
review because (1) the lab deployment lock is still held by transaction
`zhcn19-zh-auto-01` in `ModsRestoredPendingDisplay` (display geometry
mismatch), so every guarded launch refuses, and (2) the automation fixture
party has none of the repaired spells, and the casting scenarios cannot select
them. Offline work was committed at `9220f904def438ed809f9eeab3d78eb2b0d74e3c`.

Decision (owner option B):

1. **Automated native/in-game checks are not required for this mission.** The
   owner will test the change in their own game after the independent reviewer
   judges the code ready. Do not start guarded runtime launches, create
   fixtures, extend the runtime harness, or touch the `zhcn19-zh-auto-01`
   transaction, display recovery or launcher safeguards for this mission.
2. **Everything else stays required.** The offline regression coverage of
   section 4 (adapter → scanner → classifier → provider/catalog, faithful
   blueprint fixtures with the misleading native flags retained, baseline
   reproductions, negative controls), the candidate audit of section 3D, and
   assembly-backed contract checks where available remain required in full.
3. **Honesty about native behavior.** No document, report or test name may
   claim in-game verification. Native behavior that only the game can confirm
   (for example whether native `CanTarget` admits a unit target for the
   point-anchored group spells) must be stated as unverified. Where the code
   depends on such behavior, it must fail closed (refuse to plan or cast, with
   a truthful reason) rather than assume success, and that fail-closed path
   needs offline coverage.
4. **Owner acceptance checklist.** Add a concise, specific in-game checklist
   for this change to `docs/MANUAL-ACCEPTANCE.md`, covering at least: Good Hope
   listed for a bard who knows it, cast on a legal target, one slot spent, the
   buff only on in-radius allies, Crushing Despair branch behavior if the owner
   can arrange it; Haste still listed; Firebelly listed and its breath not
   listed; Abjuration resistance select, re-select (no cast) and switch (one
   use, previous resistance removed; the native resource is 1 use per day);
   Mass Feather Step (or another admitted point-capable group spell) planned on
   a unit anchor with only in-radius allies covered; reopening the planner
   keeps the entries. State what each check proves and what a failure would
   mean.
5. **Review bar.** The reviewer judges the code and offline evidence against
   this amended contract. The absence of automated native evidence is, by this
   owner decision, not by itself a readiness blocker; it is recorded as
   remaining owner review. Unsupported claims of native verification, missing
   fail-closed handling for unverified native contracts, or gaps in the
   required offline coverage remain blocking. Agent agreement still does not
   establish gameplay behavior; `AWAITING_OWNER_REVIEW` means "code reviewed,
   in-game acceptance pending with the owner".
6. Update the existing blocker/resume records so the native-check blocker for
   this mission reads as superseded by this owner decision (the `zhcn19` lab
   lock itself remains a separate, still-open lab item).

## 10. Owner workflow addendum of 2026-11-22: direct implementation and manual review

This addendum records an explicit owner decision. Where it conflicts with §2
(and with the controller-workflow wording in §1, §5 and §8), this section
governs. The technical requirements of §3–§8 and owner addendum §9 are
unchanged.

1. **Workflow.** After review round 12 (of 15) the automated controller
   stopped `BLOCKED`. The owner switched PR #25 to direct Claude
   implementation. The owner and ChatGPT then perform an independent manual
   review. No controller is enrolled, resumed or restarted for this ticket. The
   remaining three automated rounds are historical controller budget, not a
   limit on this work. `AWAITING_OWNER_REVIEW` and Astra approval are not
   claimed. The endpoint is **"Ready for manual code review; in-game
   acceptance pending."**
2. **Ownership handoff (2026-11-22).** The enrollment
   `kbp-codex-kingmaker-buff-planner-missing-beneficial-buffs-20261121094703-350e`
   was idle: `BLOCKED`, no lock holder, no developer running, and no process
   referencing it. It was pinned to the installed package `34fb8529…`
   (fingerprint `17b66fac…`). It was cancelled through that package's
   supported `cancel` command, with the reason "owner has switched PR #25 to
   direct Claude implementation with manual Howie/ChatGPT review". Status is
   now `CANCELLED`; its registry claim was released. Its records, transitions,
   findings, review comments and budget history are preserved unchanged. The
   existing branch, worktree `KingmakerBuffPlanner-MBB` and draft PR #25
   continue; no continuation branch was needed.
3. **Findings.** The twelve open findings (R8-F1, R9-F4, R9-F5, R10-F1, R11-F1,
   R11-F2, R12-F1 to R12-F6) keep their IDs. Their reproductions, repairs and
   evidence are in
   [MISSING-BENEFICIAL-BUFFS-RECOVERY-LEDGER.md](MISSING-BENEFICIAL-BUFFS-RECOVERY-LEDGER.md).
   A developer disposition is not reviewer acceptance.
4. **Publication.** Commits are pushed through the guarded standalone helper
   `codex-policy/Push-KingmakerBuffPlanner.ps1` (no force push), and the draft
   PR is updated. No merge, tag, release, package, deployment or reviewer
   request is made. A controller gate receipt is not required for this
   workflow; ordinary `scripts/Test-Development.ps1` evidence bound to the
   candidate replaces it.
