# Buff Catalog and Native HUD Maintenance Evidence — 0.0.16 candidate

## Intake checkpoint — 2026-08-31

- Intake branch/SHA: `main` / `91f198f53733b0fa63bfbc6c93ee133360b9b194`
  (`merge: release 0.0.15 buff catalog caster controls`). The worktree was
  clean and `main...origin/main` was synchronized before the dedicated branch
  `codex/kingmaker-buff-planner-0.0.16-catalog-native-hud` was created.
- Intake version: `0.0.15`; persistence schema remains `4` pending evidence
  that a persisted-data change is genuinely needed.
- No push, merge, tag, publication, game launch, Mods staging, or save mutation
  occurred during intake.

## Exact local assembly evidence

| Assembly | SHA-256 | MVID |
| --- | --- | --- |
| `Assembly-CSharp.dll` | `3b6450ffec440e296e586f71c711b195aed144b28d53e1cbb29406d18fef5afb` | `07fa1e4d-8618-41b3-9b8d-faa17d3b26f7` |
| `UnityEngine.UI.dll` | `4dee942e8974492511733e5482f89fde39e3964008f072ec9c12d05e40a9080d` | `2d028325-c478-4385-91de-2ab708b546bc` |
| `UnityEngine.CoreModule.dll` | `3a76df7f709d465e3273502e08edbffb536b1c2f78c3a132b8668e59fddd2803` | `bd5ffe06-494e-4588-a068-c8443cc48c47` |
| `Mods/CallOfTheWild/CallOfTheWild.dll` | `4ebf8e1ed3e66ffed72ea33ea325595629423dacd5bffa23e3c9109144b26915` | `8caab254-aacf-4811-8093-44b9184e6e53` |

## Inspected contracts and initial findings

- The documented read-only environment/source inspection command passed:
  `./scripts/Inspect-KingmakerVariantContracts.ps1 -TypePattern 'AbilityData$' -MethodPattern '^(\\.ctor|get_Variants|GetAvailableForCastCount|SpendFromSpellbook)$'`.
  It reconfirmed `TargetType.Enemy=0`, `Ally=1`, and `Any=2`.
- Exact Call of the Wild components are all direct `BlueprintComponent`
  subclasses. `CallOfTheWild.SpellbookMechanics.CompanionSpellbook` has field
  `BlueprintSpellbook spellbook`; `GetKnownSpellsFromMemorizationSpellbook`
  has the same field; `CanNotUseSpells` has no declared fields. These contracts
  will be consumed only through cached, fail-soft reflection.
- The current snapshot builder enumerates every non-null spellbook and does
  not distinguish a preparation companion from a casting book. The execution
  resolver already binds by the exact `ProviderKey.SpellbookGuid`.
- `AbilityTargetsAround.Select` obtains targets around the cast target and
  filters only `Enemy` and `Ally`; its `Any` case intentionally has no native
  faction filter. The existing planner treats `Any` as unusable even when the
  surrounding blueprint declares friends allowed and enemies impossible. The
  repair will preserve ambiguous/enemy cases and refine only that proven
  friend-only form.
- Exact UI inspection found `Kingmaker.UI.Constructor.ButtonPF` derives from
  `UnityEngine.UI.Button`; the formation control is
  `IngameMenuController.m_FormationButton`. Native tooltip support is
  `Kingmaker.UI.Tooltip.TooltipTrigger.SetNameAndDescription(title, body)`.
  The native UI sound API is `Kingmaker.UI.UISoundManager.Play(UISoundType)`;
  the exact opening call path remains to be recorded before implementation.
- Existing HUD code copies only the native tile sprite, then applies a custom
  brown color block, `KBP.InnerFrame`, `KBP.LowerAccent`, bright baked glyph
  ink, and a custom local tooltip. These will be removed/replaced by captured
  native values and native tooltip use.
- Existing card presentation supplies an opaque `RoutineBadge` string. The
  persisted routine assignments remain the source of truth; presentation will
  expose structured membership chips without a schema change.

## Rejected approaches at intake

- Display-name/class/GUID filtering for Arcanist, Lay on Hands, or the two
  reported communal spells.
- Hiding only a duplicate UI caster row while allowing the wrong provider into
  planning/execution.
- Treating all area effects or every `TargetType.Any` effect as allied.
- Retaining custom brown HUD chrome or a black tooltip as normal presentation.
- Cloning native GameObjects or retaining their listeners.

## Runtime boundary

The current documented guarded inventory is `baseline=0; working=0`. No
ordinary save will be substituted. Source, assembly, deterministic, package,
and guarded-WhatIf qualification will proceed; save-backed visual/cast/audio
acceptance remains blocked until the authorized fixture pair exists.

## Implementation checkpoint

- `SpellbookRoleResolver` operates on spellbook GUIDs and exact capability /
  relationship facts only. A `CanNotUseSpells` book is excluded only when an
  owned same-unit companion or casting-side memorization reference resolves to
  a distinct book. Ambiguous missing contracts are retained. Snapshot logging
  records GUID, spontaneous state, resolved role, relationship target,
  inclusion, and reason; animated execution repeats the inclusion check.
- `DiscoveryNodeKind.RestorativeAction` carries exact installed action-type
  semantics through branch paths. `NativeCandidateClassifier` adds deterministic
  `instantaneous-restoration-without-substantive-buff`,
  `reactive-restoration-marker-only`, and
  `restorative-action-without-substantive-buff` outcomes without a display-name
  condition.
- `AreaRecipientSemantics` resolves `Any` to allied only with
  `CanTargetFriends=true`, `CanTargetEnemies=false`, and
  `CanTargetPoint=false`. `KingmakerAreaCoverageResolver` recursively finds a
  unique structurally allied `AbilityTargetsAround` radius through sticky
  delivery, cast-spell references, conditionals, party/pet wrappers, and
  bounded reflected `ActionList` members. It deliberately returns no geometry
  on disagreement/read failure. Provider options expose
  `RecipientIdsByAnchor`; planning and card coverage consume the same map.
- Routine chips derive entirely from persisted assignments. No routine overlap
  is removed, deduplicated, or serialized separately.
- The HUD captures the active formation `ButtonPF` target sprite/type/material/
  color, transition, complete color/sprite state, navigation, click/hover sound
  flags, and child icon material/tint. Owned controls use white alpha-mask
  textures. `TooltipTrigger` replaces the custom message object, and
  `SetupOpenSoundGate` invokes `UISoundType.CharacterScreenOpen` after phase-B
  presentation validation.

## Action-graph fixtures and rejected theories

- Archived exact catalog evidence identifies Lay on Hands Self
  `8d6073201e5395d458b8251386d72df1` and Others
  `caae1dc6fcf7b37408686971ee27db13` as `AbilityEffectRunAction` graphs with
  `ContextActionHealTarget` and conditional recovery branches, no persistent
  effect leaves, and player-friendly/enemy-capable targeting. The prior export
  recorded them as unsupported action diagnostics; the new exact action adapter
  records the restorative semantics and excludes them under the general
  no-substantive-payload rule.
- Protection from Arrows, Communal
  `96c9d98b6a9a7c249b6c4572e4977157` is a direct
  `AbilityEffectRunAction` + `AbilityTargetsAround` graph with a persistent
  `ProtectionFromArrowsCommunalBuff` current-target leaf. It allows friends,
  excludes enemies and points, and has no branch or diagnostic leaf.
- Good Hope `a5e23522eda32dc45801e32c05dc9f96` is likewise direct
  `AbilityEffectRunAction` + `AbilityTargetsAround`, allows friends and
  excludes enemies/points, and contains a `ContextConditionHasBuff` conditional:
  its false branch applies `GoodHopeBuff`; its other reachable adjunct is
  `ContextActionRemoveBuff`. The substantive false-branch buff keeps it in the
  catalog.
- Resist Energy, Communal `7bb0c402f7f789d4d9fae8ca87b4c7e2` is the existing
  positive control: allied, non-point `AbilityTargetsAround` variant container
  with five current-target persistent child leaves.
- The archived runtime catalog did not serialize the `TargetType` numeric value
  or area radius. Those exact live-object values remain an authorized-runtime
  evidence row; no unobserved numeric value is claimed here. The exact
  `AbilityTargetsAround.Select` IL establishes that
  only `Enemy` and `Ally` filter natively and `Any` needs surrounding-metadata
  refinement.

## Deterministic checkpoint

- `./scripts/Validate-Source.ps1`: PASS=39 FAIL=0.
- `./scripts/Inspect-KingmakerSpellbookContracts.ps1`: PASS; exact optional
  component fields above.
- The source-only protocol executable: PASS=127 FAIL=0. The preceding aggregate
  source-only run passed source 39/39, protocol/domain 125/125, runtime
  filesystem 8/8, package fixture 4/4, deployment WhatIf 5/5, aggregate 1/1;
  its fresh 0.0.16 package-dependent portion is pending the local package.
- `./scripts/Build.ps1 -Configuration Release`: PASS=1 FAIL=0.
- `./scripts/Test-RuntimeHarness.ps1`: PASS=8 FAIL=0;
  `./scripts/Test-DeploymentWhatIf.ps1`: PASS=5 FAIL=0; `git diff --check`:
  pass. The old install-WhatIf pin failed safely against the valid local 0.0.13
  installation and was tightened to that exact predecessor; it must be rerun
  after clean-head 0.0.16 packaging.

## Exact next action

Commit the release source, run the clean-worktree deterministic package builder,
rerun guarded installer WhatIf, and install only the manifest-verified 0.0.16
package. Record exact ZIP/DLL/MVID/transaction hashes. Do not launch the game
without the required guarded save pair.
