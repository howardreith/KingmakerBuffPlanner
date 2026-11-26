# Greater Magic Fang repair

Status: draft PR with offline evidence only. **Not run in the game.**
Version 0.6.20 is unchanged; the change is unreleased.

- Branch: `codex/kingmaker-buff-planner-greater-magic-fang`, from `main`
  `913798712f3685236c8f9d5cc1e8c04ea9fc9248` (0.6.20).
- Product commits: `3f79a8e` (tier binding), `b4ef4d7` (natural-weapon
  observation), `b4f713c` (review round 1) and `8a1a15b` (review round 2,
  the final product source). `4593639` holds the regressions and fixtures. The
  records commit also fixes the round-3 test nit; it changes no production
  source.

## Report

> "Hi. Greater Magic Fang from the Druid spellbook only apply to the Druid's
> own pet, not other pets. Could you please check this?"

Both readings of the report were checked.

- **Other pets cannot be targeted or planned.** The evidence does not support
  this. `MagicFangGreater` (`f1100650705a69c4384d3edd88ba0f52`) is a
  Close-range spell that can target friends, and it has no
  `ContextActionsOnPet`. Its effects target `CurrentTarget`, so the planner
  offers every unit that native `AbilityData.CanTarget` admits. The own-pet
  filter in `ProviderTargetingPlanner` applies only to `EffectTarget.Pet`.
  The regressions plan the spell on another character's pet.
- **The first pet receives the spell and later casts never happen.** This was
  reproduced against the unchanged 0.6.20 production code, on both planner
  paths. Discovery expected all five enhancement tiers at once, so the first
  cast was never confirmed (`TimedOutUnconfirmed`) and every later casting was
  halted. A routine that starts with the Druid's own pet therefore matches the
  report exactly. The reporter's own session was not reproduced, and no live
  run exists.

## Root cause

`KingmakerActionGraphAdapter` expanded `MagicFang.Enchantment[]` into a
`Sequence` sorted by GUID. For Greater Magic Fang the expected effect was
therefore "buff AND +1 AND +2 AND +3 AND +4 AND +5". `EffectPresenceEvaluator`
evaluates a sequence with AllOf, and confirmation needs `Complete`. The buff
plus the single tier the game applies stayed `Partial`, and the run halted
after the first casting. In Classic, `HaltingPlanRunner` halts it; in
casting-first, `ExplicitCastingRunCoordinator` does.

## Verified native contracts (Kingmaker 2.1.7b)

These were read from the installed `Assembly-CSharp.dll` (ildasm) and the
native blueprint dump. The regression `greater-magic-fang-native-contract-pinned`
pins them against the referenced assembly.

- **Tier selection** (`MagicFang.RunAction`):
  - If `Greater` is false, it applies `Enchantment[0]`.
  - Otherwise it applies
    `Enchantment[Math.Min(EnchantLevel.Calculate(context), Enchantment.Length) - 1]`.

  It applies exactly one element, so the array order is the tier order.
- **Rank** (`MagicFangGreater` blueprint):
  - `EnchantLevel` is `Rank`/`ProjectilesCount`.
  - The ability's `ContextRankConfig` of that type uses base `CasterLevel`,
    `DivStep` with step 4, `UseMin` 1 and `UseMax` 5.
  - `ContextRankConfig.GetValue` is
    `ApplyMinMax(ApplyProgression(GetBaseValue(context)))`. `DivStep` is
    `value / m_StepLevel`, and the `CasterLevel` base is
    `context.Params.CasterLevel`.
  - `MechanicsContext.RecalculateRanks` reads the rank configs of
    `AssociatedBlueprint`. A later config of the same type overwrites an
    earlier one.
  - The resulting tiers by caster level are: 1–7 → +1, 8–11 → +2, 12–15 → +3,
    16–19 → +4 and 20 or more → +5.
- **Companion buff:** `MagicFangGreaterBuff` (`ca90cc4cd9fc592419c5e58f0a9ead64`)
  is applied by `ContextActionApplyBuff`. Ordinary Magic Fang applies
  `MagicFangBuff` (`e7646b1dfdd22ce4ab340f295938ab8e`) plus
  `TemporaryEnhancement1`.
- **Enchanted weapons:**
  - `Body.EmptyHandWeapon`, if it is not null.
  - `Body.PrimaryHand`/`SecondaryHand` `MaybeWeapon`, when hands are enabled
    and the weapon is natural. While polymorphed, these getters return
    `m_PolymoprphHandsEquipmentSet` (the game's own spelling).
  - Every natural `Body.AdditionalLimbs` weapon, which is the polymorph limb
    set while polymorphed.
- **`EnchantSlot`** removes a temporary instance of the same blueprint and
  adds it again, which creates a new instance. It leaves a permanent instance
  and other tiers alone.
- **Readers before the repair:** `Body.CurrentEquipmentSlots` yields only the
  ordinary hand set and the worn slots, and an empty hand's `MaybeItem` is
  null. The vanilla animal companions keep natural weapons in their hand
  slots, so those were visible.
- **Call of the Wild** replaces both arrays with its own ordered
  `StaticTemporaryEnhancement1..5`. It keeps `Greater`, `EnchantLevel` and the
  rank config.

## Change

1. **Discovery.**
   - Ordinary Magic Fang keeps its shape.
   - Greater Magic Fang keeps the array order. Its tier is a chain of
     conditionals on `CasterLevelPredicate` keys (`caster-level-at-least:N`)
     carried as `CasterFactGate`s.
   - Each level's tier comes from the cast ability's single caster-level rank
     config, through its native `ApplyProgression`/`ApplyMinMax`. Only formula
     (non-`Custom`) progressions are used, with bounded step/start values and
     verified saturation up to caster level 10000.
   - Only the cast ability's own `MagicFang` action is bound. Anything else is
     the unknown node `MagicFang:enchant-level-unverified`, and the classifier
     then excludes the spell under the current rules.
2. **Binding.** `ProviderTargetingPlanner.CasterFacts` answers level keys
   from the provider's effective caster level
   (`AbilityData.CalculateParams().CasterLevel`).
   - Both planners therefore expect the buff and exactly that provider's tier.
     Casting-first uses `BindEffects`; Classic steps use `BindCasterLevel`,
     which binds only the tier.
   - An unknown caster level offers no targets.
3. **Confirmation.** A Magic Fang tier counts only when this attempt applied
   it and it is present at the read that judges the cast. An unbound tier
   never confirms and is never kept as coverage.
4. **Existing effects and forecast.**
   - Neither Classic presence nor casting-first assessment treats an existing
     Greater Magic Fang as already active.
   - The caster-free setup view reports such coverage as partial.
   - A one-pass structural grant names the tier its provider applied.
5. **Identity.** A caster-level selection is described by its tiers in GUID
   order, which is the identity earlier versions produced. Saved castings and
   assignments keep matching, for native and replacement arrays alike.
6. **Observation.** Both effect readers use `KingmakerEnchantedItems.Read`,
   which reads:
   - the slot items;
   - the empty-hand weapon;
   - the native hand getters, including the polymorph set;
   - the limb items, each instance once.

## Policy notes and limits

- **Existing Magic Fang always recasts.** A worn enchantment's caster level
  and source item are not read, so an existing Magic Fang is never provably
  sufficient. This is unchanged from before for ordinary Magic Fang in
  casting-first, and now also holds for Greater Magic Fang in Classic. The
  recast is native-harmless: enhancement bonuses do not stack.
- **Permanent instances halt the run.** A permanent instance of the selected
  tier is left untouched by `EnchantSlot`, so it never confirms a cast. The
  run stops instead; this is conservative.
- **Caster level is bound at planning.** If the caster level at cast time
  differs from the level read while planning, the tier differs, the cast is
  unconfirmed and the run stops.
- **Historical catalogue export.** `planning/NATIVE-BUFF-CATALOG.json` is a
  historical in-game export. It still shows the old five-leaf shape.

## Validation

All checks are offline. They were run through `env -u PSModulePath
powershell.exe` from Bash with `.\scripts\Test-Development.ps1 -Only …`.

| Check | Inputs | Result |
|---|---|---|
| Baseline reproduction | final regressions in a detached `9137987` worktree; production `src` identical (`git diff --quiet`); three baseline-behavior shims for absent APIs | PASS=4 FAIL=9 (expected) |
| Round-1 reproduction | same regressions on `4593639` | PASS=4 FAIL=9 (expected) |
| Round-2 reproduction | final regressions on `b4f713c` | PASS=10 FAIL=3; the nested-wrapper case also fails alone |
| Fixture capture | `evidence-native-fixtures-missing-beneficial-buffs-capture` | full 61 / referenced 63; adds 9 blueprints and 10 index lines; existing fixtures byte-identical, and unchanged on re-capture |
| Greater Magic Fang regressions | `greater-magic-fang` on `8a1a15b` | PASS=13 FAIL=0 |
| Complete offline program plus fixture verification | all 174 registered prefixes plus `…-verify` on `8a1a15b` | PASS=871 FAIL=0, source validation 47/0, 46 s |

**What fails at the baseline.**
- Discovery binding, replacement arrays, unproven contracts, confirmation,
  existing effects, the unknown caster level, the forecast and observation.
- The two-pet run: `halted after gmf-0 TimedOutUnconfirmed …
  expected-application-unconfirmed-after-confirmation-window`, with the
  second casting `NotProcessed:halted`.

These pass at the baseline:
- the native-contract pin;
- **catalogue identity**, which establishes that the expected strings are the
  pre-change identities;
- refusals;
- neighbours.

The shims reproduce 0.6.20 behavior: the old caster-fact read, the old
`CurrentEquipmentSlots` item loop, and no level selection.

`Build.ps1` stamps the Git state into the DLL, so the build hash depends on
the tree state. The hash of the final build is recorded in the PR
description, not here.

**Not run:**
- any in-game check;
- packaging, live purity or release qualification, none of which is selected
  for a repair checkpoint (docs/VALIDATION-POLICY.md).

## Live acceptance

Not performed. No approved fixture can stage one Druid with pets of two
masters: the approved automation party is a fighter, a bard and a sorcerer.
AGENTS.md forbids editing protected saves to build one. The checks are listed
in the [owner checklist](MANUAL-ACCEPTANCE.md#greater-magic-fang--owner-in-game-acceptance-pending).

## Independent review (Codex CLI 0.170.0, `codex exec -s read-only`)

- **Round 1** on `3f79a8e..4593639`: nine findings. Eight were accepted and
  fixed in `b4f713c`, each reproduced first:
  - Classic steps were unbound;
  - an old tier was accepted under a fresh buff;
  - an unprovable contract was admitted with its buff alone;
  - the saved identity changed;
  - the forecast reused a weaker grant;
  - the polymorph hand set was missed;
  - an unknown caster level was handled inconsistently;
  - custom progressions were bound.

  The ninth, that an adequate existing Magic Fang always recasts, was kept as
  existing policy (follow-up 1).
- **Round 2** on `b4f713c`: ten findings. Seven were fixed in `8a1a15b`, with
  every product finding reproduced first:
  - a vanished tier seen only by an earlier progress read;
  - a Classic skip over an Oil of Enhancement;
  - the grant key's role and path;
  - nested-cast binding;
  - overflow bounds;
  - the Pre040 audit verdict;
  - prepared-token coverage in the two-pet regression.

  One became moot: an unreadable second provider in Classic presence. Two are
  follow-ups (2 and 3).
- **Round 3** on `8a1a15b`: no blocking issue for the native Druid two-pet case; Codex ran all 13 regressions and they passed. It raised one test nit, now fixed: the prepared-slot fixture used non-native token IDs. Its four should-fix findings need modified or mod-only blueprints, and are follow-ups 4–7.

## Follow-ups (not in this PR)

1. Read a worn enchantment's own context (caster level, temporary, end
   time) and its item, so that an adequate existing Magic Fang can skip and
   tiers are matched against natural weapons only.
2. Two spell blueprints that share an aggregate but order their tier arrays
   differently: Classic binds the representative's array. Bind the selected
   ability's own graph. This occurs only with mods.
3. A constant `EnchantLevel` with unreachable array elements changes the
   saved identity. Keep the full array for identity. This occurs only with
   modified blueprints.
4. A Greater Magic Fang with a constant `EnchantLevel` has no caster-level
   selection, so Classic can still skip it over a matching marker, such as
   an oil on a manufactured weapon. Ordinary Magic Fang already had the same
   ambiguity before this branch. This needs a modified blueprint; follow-up 1
   (item provenance) resolves it.
5. A caster-targeted companion effect leaves the caster out of the one-pass
   tier grant key. The generic structural key also ignores roles. This needs
   a modified blueprint.
6. An animated sticky-touch delivery of a constant Magic Fang tier can keep
   a pending-poll observation. No native or Call of the Wild spell has this
   shape.
7. The rank-field bounds also apply to `m_StartLevel` for progressions that
   never read it. This only refuses modified blueprints, which is the
   conservative direction.
8. Approve a Druid fixture with a second pet owner and run the guarded
   two-pet acceptance.
9. Regenerate `planning/NATIVE-BUFF-CATALOG.json` at the next guarded
   catalogue run.
10. `CHANGELOG.md` already headed the 0.6.20 content "Unreleased - missing
    beneficial buffs" before this branch.

## Next action

Owner review of the draft PR. After that, either approve a Druid fixture for
the live two-pet check, or accept the change on offline evidence plus the
owner checklist.
