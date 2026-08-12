# Parchment and BubbleBuffs UI Forensics

## Mission boundary

This document records the presentation evidence used for the Kingmaker Buff Planner UI-polish branch. Discovery, classification, stable identities, planning, resource accounting, persistence schema, both executors, confirmed-effect semantics, HUD hitboxes/listeners/input isolation, the modal input lease, and guarded runtime transactions are frozen regression contracts.

Branch: `codex/ui-parchment-bubblebuffs`

Starting repository HEAD: `21c4dd702868e5cfc89963ba50bb64420f18915d`

Qualified release source: `e656812572adea8bc312419372b61ee8c4834e5a`

Starting version: `0.0.6`

## Preserved MVP identity

The preserved copy is under `artifacts/release-candidate-backups/mvp-0.0.6-21c4dd702868e5cfc89963ba50bb64420f18915d/`.

| File | SHA-256 |
|---|---|
| `KingmakerBuffPlanner-0.0.6.zip` | `ce7492b262f01a9afb5a7666fe7e4bda9be1821395eb00244f5898b6882208e9` |
| `KingmakerBuffPlanner.dll` | `6144256c6a0623e908c3d9e821a1b87ee5800195759fbfabb1e587eaf9be1d9b` |
| `release-manifest.json` | `ab5363be7a10e702996eb3fcc4d414af2a8f1da4e6e3951ee14336f103d01d85` |
| `RELEASE-NOTES-DRAFT.md` | `ddb1c4f68e1174ee32bddc4f3896f5e58e68253424341e986c9e79121bfa9e83` |

Assembly MVID: `bff11809-aa53-42c2-8ab7-ef3564450e61`.

The installed planner has the same 0.0.6 Info identity and DLL hash. No Kingmaker process, runtime deployment lock, or unresolved runtime transaction existed at intake.

## Human screenshot evidence

| Screenshot | SHA-256 | Finding |
|---|---|---|
| `01-current-working-kbp-ui.png` | `c5ace5a40ea694d7950c61fc654d6eb92c122ac8000a00a8aeae73446880d67e` | The catalog/details render and the modal works, but near-black panels, text-only rows, six implementation-shaped filters, raw provider/resource terms, and duplicate Mode controls make the screen read as a diagnostic UI. |
| `02-current-working-buffs-applied.png` | `1a98902bbe9db7f4a81bc7a22ddaf62bb7250330bff735a273a690d5aaf1c2ca` | Exact Kingmaker visual language: warm light parchment, thin burgundy rules, antique-gold ornaments, dark brown serif text, framed portraits, compact effect icon/name/time pairs, and restrained green/red state accents. It also confirms Bless, Light, Resistance, and Guidance effects after successful MVP execution. |
| `03-bubblebuffs-ui-reference.png` | `1cc8c9e223edd6472c2d390132331b96cc7a08209cfb238fcdbaa79075e18636` | Strong information hierarchy: icon-first cards, immediate requested/available text, selected detail card, portrait-first targeting, few ordinary filters, and grouped readiness. Its purple Wrath chrome, hierarchy paths, shaders, bundled icons/textures, and assets are explicitly excluded. |

## BubbleBuffs source study

Read-only source: `C:\Dev\KingmakerBuffPlannerLab\reference-source\BubbleBuffs` at commit `f4871f763a23251284422ef0945a85e9f3fb788e`.

Reviewed source and provenance:

- `BubbleBuffs/BubbleBuffer.cs`
- `BubbleBuffs/UIHelpers.cs`
- `BubbleBuffs/Utilities/Searchbar.cs`
- `BubbleBuffs/Utilities/AssetLoader.cs`
- `BubbleBuffs/SaveState.cs`
- `LICENSE` (MIT, copyright 2021 Sean Petrie/Vek17)

Adapted design ideas, not copied code:

- `CreateWindow` composes the experience from recognizable native screen pieces while preserving one controller-owned root.
- `MakeBuffsList` and `BindBuffToView` put the blueprint icon and name first and keep live availability/request state secondary.
- `PreviewReceivers` and `UpdateTargetBuffColor` make portraits the fastest way to understand requested, fulfilled, invalid, and indirect/mass-target states.
- `UpdateCasterDetails` keeps caster/resource decisions adjacent to the selected buff rather than in the catalog identity.
- `MakeSummary` expresses each routine as fulfilled/requested totals.
- `MakeFilters` demonstrates a search-first flow with ordinary toggles and category disclosure.
- `TryInstallUI`/`AddButton` demonstrate independent HUD quick actions, but KBP's already-qualified HUD hierarchy, hitboxes, listeners, and lifecycle remain unchanged.
- `SaveState` confirms that presentation state must remain a view over stable buff/caster identities rather than becoming a new mechanics store.

Deliberately not copied:

- no Wrath `ServiceWindowsPCView`, `SpellbookPCView`, character, encyclopedia, party, action-bar, or chargen hierarchy path;
- no Owlcat Wrath-specific `OwlcatButton`, TMP, UniRx, DOTween, MVVM, or workaround dependency;
- no BubbleBuffs asset bundle, shader, material, icon, texture, mesh, localization, or custom art;
- no BubbleBuffs persistence types, caster rules, execution logic, target computation, or magic-number blueprint exception;
- no source excerpt is incorporated. The MIT notice remains documented as research provenance; no third-party code has been copied at this checkpoint.

## Presentation architecture

The current safe standalone overlay and input lifecycle stay intact. Presentation will be layered as:

1. `PlannerUiTheme` resolves exact Kingmaker-native candidates at runtime and owns all fallback colors/sprites/fonts.
2. UI-only view models format player language and deterministic neutral/success/warning/failure states from `PlannerSetupModel` and planning previews.
3. `BuffPlannerScreenView` renders cards, portraits, summaries, and collapsed disclosures; callbacks continue to invoke the existing model commands.
4. Runtime diagnostics inventory native candidates and prove theme resolution, object/listener stability, and screenshot state.

## Intended theme token roles

`ParchmentBackground`, `ParchmentPanel`, `ParchmentRaised`, `DarkBrownText`, `MutedBrownText`, `BurgundyPrimary`, `GoldAccent`, `GreenSuccess`, `AmberWarning`, `RedFailure`, `DisabledGray`, native frame/button/toggle/portrait/selection sprites, and native header/body fonts are resolved in one theme object. Fallback values are permitted only there.

## Native Kingmaker inventory status

The existing runtime probe records the exact service-window root, buttons, canvas, raycasters, sprite names, and candidate anchors, but its earlier main-menu attempt could not reach campaign UI. The UI-polish branch will first extend this diagnostic-only probe to capture fonts and safe frame/panel/button/toggle/portrait candidates from the exact installed 2.1.7b campaign hierarchy, then run it through the guarded disposable working-save scenario. No presentation implementation begins until the resulting exact paths, required components, fallback, cleanup owner, and validation policy are appended here.

## Baseline regression record

On 2026-08-12 before source edits:

- source validation: `30/30`;
- protocol/behavior tests: `62/62`;
- runtime harness filesystem tests: `7/7`;
- deployment WhatIf purity: `5/5`;
- source-only aggregate: `1/1`;
- preserved release package validation: `4/4`;
- guarded installer WhatIf with actual installed prior version 0.0.6: PASS and no mutation.

`scripts/Test-InstallWhatIf.ps1` itself still passes `-ExpectedPriorVersion 0.0.2`, so it fails against the correctly installed 0.0.6 before reaching its purity assertion. That is a stale test-helper parameter, not live identity drift: installed Info is 0.0.6 and installed DLL SHA-256 is exactly `6144256c...`. The production installer and transaction framework are unchanged.
