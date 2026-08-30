# Manual Acceptance

## 0.0.14 buff catalog and caster controls

Status: REQUIRED FOR IN-GAME CLAIM; NOT RUN. The guarded resolver returned
`Disposable save ambiguity: baseline=0; working=0.` Use only an explicitly
authorized `KBP_AUTOMATION_BASELINE` / `KBP_AUTOMATION_WORKING` pair. Never
substitute an ordinary campaign save.

Exact qualified implementation/record HEAD is
`ce7099b089440e40716cbbd39c4e377c4fbe21c2`. Local-only ZIP/DLL/MVID are
`239eb9de3657de030c88dabfffeaa3fab344ec01e8d561e6649281d7a9cf0571` /
`6fe6d6837f5155b5ad1b1cdd1e64d47974cadbab44a824efa42dc0edea48b4d6` /
`80732098-e9da-45b2-b402-7b4ca6f52752`.

1. Load the representative party and run Long when all twelve configured buffs
   are active. Confirm no lower-left floating result, no bottom-right/common/
   combat/event-log message, and a UMM result with `skipped=12` and
   `unfulfilled=0`. Confirm Setup, Long, Important, Short, hover tooltips, and
   running-state button disabling still work.
2. Open the full planner and press APPLY. The planner footer may show the
   complete result. Close the planner and confirm no floating result remains.
3. With no investigator and no learned Flying Kick, confirm Effortless Aid and
   Flying Kick concrete options are absent. Capture the UMM variant-eligibility
   reasons and exact parent/child keys.
4. Inspect the catalog. Confirm Alchemist's Fire, Channel Positive Energy -
   Damage Undead, and its quick/damage sibling are absent while genuine
   personal, friend, party, pet, communal, worn-item, and substantive hidden
   buffs remain.
5. Verify valid Resist Energy choices. Select Fire and prove Fire, not the
   first sibling, is applied with one slot/resource debit. Verify Communal uses
   actual allied recipients and one debit. Record parent/child/provider keys,
   active buff, and before/after resources.
6. Configure Blur with Felix enabled/first/max 1 and Akasa enabled/second/
   Unlimited. Select enough targets for multiple casts. Confirm preview shows
   Felix 1 and Akasa the remainder; execution matches; Felix casts Blur once;
   and Felix's unrelated level-two Bulls Strength allocation remains governed
   by its own exact provider/resource state.
7. Close/reopen, return through the main menu where safe, and reload the
   campaign. Confirm order, enabled states, and caps persist, including for a
   temporarily depleted provider that remains visible/configured.
8. Disable every Blur provider. Confirm the preview marks casts unfulfilled,
   execution uses none, `[KBP-PLAN-DIAGNOSTIC]` reports
   `provider-policy-refusal`, and no floating/native-log result appears.

Capture full-resolution screenshots, relevant UMM slices, exact source/provider
keys, profile JSON before/after reload, active-effect evidence, and resource
deltas. Restore the working fixture through the guarded transaction and verify
the immutable baseline hash.

## 0.0.14 complete names and concrete variants

Status: REQUIRED FOR IN-GAME CLAIM; NOT RUN because the guarded resolver reports
`baseline=0; working=0`. Use only a repository-authorized
`KBP_AUTOMATION_BASELINE` / `KBP_AUTOMATION_WORKING` pair. Do not substitute an
ordinary campaign save.

Exact published test artifact is
`artifacts/release/0.0.14/KingmakerBuffPlanner-0.0.14.zip`, released from
`1ad148780d801d63d7ab40e52bba94b7c4627b47`; ZIP SHA-256 is
`a319a7f18aa7a20e47282fdf5b10dfee0adafd355677cf870a7fbb065028484b`.
Release URL is
`https://github.com/howardreith/KingmakerBuffPlanner/releases/tag/v0.0.14`.

1. Open Setup and verify `Protection from Arrows, Communal` is present in full
   on its selectable card, selected detail, and description view. Check one
   ordinary short name remains compact and adjacent controls do not overlap.
2. Verify Resist Energy exposes exactly its blueprint-supported concrete
   choices and no unresolved parent card. Repeat for Resist Energy, Communal.
   Confirm parent siblings are adjacent and in declared blueprint order.
3. Search the complete parent name and verify every child remains visible;
   search Fire and Cold independently and verify the corresponding concrete
   entries.
4. With a prepared caster, select Fire, execute once, and confirm the Fire buff
   rather than the first child. Record the exact prepared slot before/after and
   confirm one slot is consumed. Repeat Cold from a restored working fixture.
5. Repeat one choice with a spontaneous caster and any locally supported
   innate/resource source. Confirm parent ownership establishes availability,
   the concrete child effect is submitted, and the pool/charge changes once.
6. Execute a Communal child on a representative party configuration. Confirm
   actual `AbilityTargetsAround` recipients, one cast/resource debit, and no
   unresolved variant-selection window or stall.
7. Save a plan containing a concrete child, close/reopen Setup, return through
   the main menu, and restart/reload where supported. Confirm the same parent
   and child GUIDs and visible name return.
8. Load a copied legacy parent-only plan. Confirm no child is invented; the UI
   reports that the complete parent requires reselection. Also execute one
   ordinary non-variant buff as a regression control.

Capture the planner log, exact parent/child GUIDs, before/after slot/resource
state, active buff GUID, and screenshots for every executed choice.

## 0.0.13 Powerful Change post-release verification

Status: RECOMMENDED DIAGNOSTIC FOLLOW-UP. The owner accepted the exact installed
candidate and authorized publication on 2026-08-27. That acceptance authorizes
the release but does not manufacture numerical in-game evidence; use this
checklist to verify the remaining cross-mod runtime boundary. It never
authorizes modification of another mod.

Exact artifact: `artifacts/release/0.0.13/KingmakerBuffPlanner-0.0.13.zip`;
release/tag commit `3c329cfff3530fe8397012565c238a81d55cec1d`;
ZIP SHA-256
`67768176032d6d980f09b708a636dfa8f07e5b052530deb327d833e8e4882d96`;
DLL SHA-256
`b41f31da57f9b7ee69a4e693792bf4bb1a6f7e5ea7dbff0e723c72f24d02bf86`;
MVID `995ed895-bb45-412c-b626-692816b1f833`. These are the exact published and
guarded-installed bytes.

1. Load a campaign containing a Brown-Fur Transmuter who owns Powerful Change,
   knows Bull's Strength in the CotW Arcanist casting spellbook, and has at least
   four Arcane Reservoir points. Record caster level and whether Transmutation
   Supremacy is present.
2. Open Setup, select Bull's Strength, and inspect Enhancement. Confirm
   `Powerful Change: Strength` is available. Capture the corresponding
   `[KBP][Enhancement]` line; it must report the expected caster, ability/buff,
   `School=Transmutation`, feature detected, `MatchedScores=[Strength]`,
   qualification true, no rejection reason, and the option in the available
   list.
3. With no enhancement selected, cast Bull's Strength on a clean target. Confirm
   the native Enhancement modifier is +4 and the reservoir does not change.
4. Remove/expire that buff. Record the reservoir, select Powerful Change:
   Strength, and execute the routine. Confirm exactly one point is spent, the
   toggle is consumed, the resulting Enhancement modifier is +6 (or +8 at level
   20 with Transmutation Supremacy), and the execution log names the selected
   enhancement and a successful native command.
5. Recast with Powerful Change after removing/expiring the prior buff. Confirm
   one point is spent per selected successful cast and there is one normal buff
   instance/modifier rather than duplicated or stacked enhancement entries.
6. Execute once with Powerful Change unselected after an enhanced cast. Confirm
   the result returns to +4 and no prior score toggle was silently rearmed.
7. Repeat a representative related spell, preferably Cat's Grace or Bear's
   Endurance, using its matching score. Confirm the matching option appears and
   a nonmatching score does not. Select an unrelated nonqualifying spell and
   confirm no Powerful Change option appears.
8. Where practical, inspect an ordinary wizard and a multiclass spell copy from
   a non-Arcanist spellbook. Neither may receive the option. Restart without the
   optional Brown-Fur provider and confirm Buff Planner initializes normally
   with no Powerful Change entries.

Report the full `[KBP][Enhancement]`, `[KBP-ENHANCEMENT-OPTION]`, routine-plan,
routine-outcome, and optional provider transaction lines for any failure, plus
before/after reservoir values and the target's modifier breakdown.

## 0.0.12 HUD lifecycle hotfix gate

Status: PASS. The owner accepted the guarded-installed 0.0.12 repair on
2026-08-24. The accepted log proves a loading-screen-blocked candidate remained
pending and then installed as four buttons/four listeners after 57 validation
frames; Setup presentation also validated and opened. Exact evidence is in
`planning/HUD-LIFECYCLE-0.0.12-HUMAN-ACCEPTANCE.md`.

The authorized save inventory remains baseline=0 and working=0, so the guarded
harness did not independently execute the campaign checklist. The checklist is
retained for future regression testing; do not substitute another automation
save.

1. Start a fresh process and load an existing campaign. Watch the lower-left
   Setup/Long/Important/Short row from first appearance through full HUD settlement
   and for at least 10 seconds—materially longer than the former 120-frame window.
   Record the final `[KBP-BOOT]` snapshot and confirm `hudState=Installed`.
2. Start or enter a new campaign area where practical. Repeat the settlement wait
   and verify exactly four controls remain visible and clickable.
3. Click all four controls. Setup must open the planner; Long/Important/Short must
   report their real configured result. No click may activate a native button
   underneath, and native lower-left controls must still work independently.
4. Open and close Setup through both its HUD button and Ctrl+Shift+B. After closing,
   camera, selection, game mode, and native input must be restored.
5. Perform an area transition. During unload no candidate should be created; after
   load, the row should create once, validate, and remain installed. Capture host,
   anchor, candidate, dispatch, and retry identities from the transition log.
6. Exercise a reproducible opening-camera or cutscene segment, world-map
   presentation, and settled gameplay. Compare with the accepted 0.0.11 behavior:
   there must be no severe approximately 11–17 FPS ceiling, no continuous
   absent-HUD discovery, and no continuous stable-installed discovery.
7. If a candidate expires or an inner anchor becomes stale, confirm the log reports
   `Expired` or `Stale`, a bounded retry re-arm, a later candidate creation, and
   final `Installed` without an outer-HUD identity change.

Report game area/save category, screen resolution, UI scale, configured FPS cap,
wait duration, relevant `[KBP-BOOT]` lines, and full-resolution evidence for any
future failure. The owner separately authorized merge, tag, and publication of
0.0.12 after this acceptance.

## 0.0.11 performance-repair completion gate

Status: PASS. The repository owner confirmed through human runtime testing that the severe approximately 16 FPS regression is gone. The checklist below records the accepted coverage and remains useful for later compatibility retesting:

1. With only the declared comparison mod set, record the configured cap and FPS throughout the opening book/camera approach and after settlement. The former approximately 16-17 FPS KBP-specific ceiling must be absent.
2. Load an ordinary gameplay area. Confirm the Setup/Long/Important/Short HUD row appears once, is aligned and clickable, and ordinary gameplay remains at the same-scene no-KBP frame rate.
3. Open/close Setup with the HUD and Ctrl+Shift+B. Confirm full-screen input isolation, target-state display, profile round trip, and restoration of camera/selection/mode.
4. Execute one configured Long routine in Animated mode and one in Instant mode. Confirm expected effects and exact resource outcomes; include metamagic-rod/enhancement behavior if configured.
5. Record a safely reproducible cutscene and world-map movement segment with and without KBP. Severe KBP-specific judder must be absent.

Exact published identity: tag commit `3661f5c31a1060bca67758c2369b2ef361a339c9`; ZIP `89cbebd2a1eb594d2307c4388c19588e1d4ea9c845284d36081c3e72d492795c`; DLL `95f484907f9a1008798e3557e46212faa1e41406bccf9d109d78e1921e9d46c6`; MVID `bf949174-0601-4822-a121-9c9d9c14597f`. Release publication was separately authorized and completed at `https://github.com/howardreith/KingmakerBuffPlanner/releases/tag/v0.0.11`.

## 0.0.10 clarity and alignment visual gate

Status: REQUIRED. Exact 0.0.10 is guarded-installed. Automation proves mechanics and captures rendered evidence; it does not grant cosmetic acceptance.

Installed identity: source `14719e816c31d4efadf829733d499774c6f5e741`; package `46d741b7dd16120e5687069c215a5cc270b9ec1e8430fd764dd36a2bdb05f013`; DLL `bcd6ed91e4d6898dec74e69f501389adb15728cd03fe0a0915522e5a1a18c55e`; MVID `8bb28075-83cf-41d2-ad3d-e883886c4961`.

Compare the installed build with `runtime-evidence/ui-clarity-0.0.10-final-animated-1/`:

1. At the active UI scale, confirm the four-button row's left edge aligns with the native lower-left grid and every antique-gold glyph looks centered inside its unchanged hitbox. Recheck at 1920x1080 and 1600x900.
2. Confirm planner text is as sharp as nearby native service-window text. There must be no soft rescaled appearance, clipping, or fuzzy fractional placement.
3. Select Bless or another configured buff. A fulfilled explicit portrait must be unmistakably full green with a strong frame and `SELECTED`; selected-but-unavailable must be amber; invalid must be red and explain why.
4. In a multi-member party, select one recipient for a normalized party/area effect. Additional expected recipients must be lighter green with `COVERED` and the tooltip `Also affected by the planned cast.` Single-target spells must not mark unrelated portraits.
5. The selected-buff panel must visibly show `Available:` and `Planned:` lines. No unexplained coverage fraction or generic blocked count may appear.
6. Bless, Resistance, Light, and similar entries must remain consolidated. Animated, Instant, automatic provider selection, routine persistence, Ctrl+Shift+B isolation, HUD clicks/tooltips, and modal close/restore must remain unchanged.

Report resolution, UI scale, selected buff, party composition, and a full-resolution screenshot for any failed item. No merge, push, or public release is authorized by this checklist.

## 0.0.9 polish and consolidation visual gate

Status: REQUIRED. Exact 0.0.9 is guarded-installed; automation proves behavior and rendered evidence, not the final cosmetic verdict.

Installed identity: source `f026a4a9974af8e4191ff7fb104e472f11c2016f`; package `471e86e0043b47bc899322b640fb448105bfc1689f796b56611ed5d980d4bbe8`; DLL `d66edcacedcfe9d862e5cd433e2e58166abbc5a5a5404b9b7c5d6fd39ae898a1`; MVID `174e2e17-9006-4667-b06d-85d372a2bb77`.

Compare the installed build against `runtime-evidence/ui-polish-0.0.9-release-animated/`:

1. The four dark/gold HUD buttons read as one framed cluster immediately above the native lower-left block. Confirm their screen-edge spacing, native skin, inset frame/accent, and inward-padded glyphs feel integrated; clicks, tooltips, anchors, and quick actions must remain unchanged.
2. The grid remains exactly four columns without horizontal scrolling. Symmetric inner margins and wider gaps should feel balanced; Bless and Resistance should each appear once.
3. Bless should say `At will · multiple sources` in the fixture while retaining automatic provider choice. No provider-management interface or provider-specific duplicate card should appear.
4. Clicking a valid portrait must produce the strong full green tint/overlay, bright frame, and `SELECTED` label. Amber and invalid red/muted states must remain distinct.
5. In a multi-member party with Remove Fear or another normalized Party/AreaRecipients effect, directly select one target. Other legal beneficiaries should receive the lighter green `COVERED` state, clearly distinct from direct selection. The one-member automation save cannot visually demonstrate this multi-portrait case.
6. Long/Important/Short, Search/categories/Selected only, direct target toggles, one Apply action, Animated, Instant, Ctrl+Shift+B isolation, close/restore, and persistence should behave exactly as in 0.0.8.

Report the exact resolution and screenshots for any failed item. No merge or public release is authorized by this checklist.

## 0.0.8 four-column planner visual gate

Status: REQUIRED after guarded installation. Automation has not granted cosmetic acceptance.

Installed final identity: source `6e5d02b21e587db84f2c7e7d2a34a63bace3e942`; package `22ce0c0e44c6f6b1f895199e58fe1afe5f639e6b38443e062fd6f4204ec8dbb2`; DLL `593db3bb0ce76316840f94e52d4698c7cd2353bc2aa31610608368478bcdda4b`; MVID `a8265c4e-e37d-4f54-a3e4-ee6578fdefa6`. Compare against screenshots under `runtime-evidence/ui-grid-0.0.8-final-animated/`.

1. The four HUD buttons retain their established anchors/hitboxes/tooltips but use dark brown tiles with antique-gold glyphs; no bright white HUD treatment remains.
2. Ctrl+Shift+B opens/closes the planner without triggering native B. F10 does nothing for the planner. Settings can toggle the hotkey to Ctrl+Shift+P and back.
3. Long, Important, and Short clearly define edit context. The broad catalog is exactly four cards across at 1920x1080, vertically scrolls, and has readable actual icons, names, availability, badges, and restrained status accents.
4. The only normal catalog controls are Search, All, Spells, Abilities, Other, and Selected only. Selected only follows the active routine. There is no Hide/Show hidden, Add to routine, Casting Source/provider editor, technical source/resource text, Advanced Filters, or duplicate Mode.
5. Select Bless, click a portrait under Long, switch to Important, and return to Long. The click itself adds/removes the target; routines remain independent; Select All Valid and Clear Targets behave immediately.
6. The lower area keeps icon/name/source/duration/description/portraits/plan compact. Settings contains one casting mode. Apply Long/Important/Short is the single planner action.
7. Animated and Instant each confirm the expected effect/resource outcome; close/Escape/hotkey restores HUD, selection, camera, and mode without duplicate UI.
8. Judge native feel, contrast, text clipping, scroll response, portrait-state clarity, and information density. Report the exact resolution and a screenshot for any failed item.

No merge or public release is authorized by this checklist.

## 0.0.7 parchment UI visual gate

Status: exact 0.0.7 is guarded-installed; automated mechanics/presentation evidence PASS; human visual acceptance REQUIRED. Do not treat installation as cosmetic acceptance.

Installed identity: source `2f125f9f1024692d83a1b2570209d1858d62eff1`; package `9feed6dffa668812ed826c75b743d72892e6e8371b0f81585fb557aea8fcf453`; DLL `bf8c72874377d56f91bcdb6daedaa8b28b340a948aee06583a32954d61b38927`; MVID `966b7d8f-bd5f-46b9-beda-62774f82ccac`; CLR 0.0.7.0.

Load a campaign and inspect Setup/F10 against the final automated screenshots under `runtime-evidence/ui-polish-0.0.7-release-instant/`:

1. The screen reads as a Kingmaker service window rather than a debug tool: warm parchment, burgundy rules, antique gold, brown text, restrained borders, and readable contrast.
2. Every buff card has a recognizable spell/ability icon; the name, availability, routine badge, selected state, and neutral/ready/partial/blocked state are legible without crowding.
3. Portrait targeting is immediately understandable: neutral means unselected, green/check means selected and fulfillable, amber means partial/unavailable, red/cross means illegal, and the lighter secondary mark means indirect benefit.
4. Search, Configured only, Show hidden, Reset, and Advanced Filters require no guessing; the old CONFIG/DURATION/SOURCE/SORT/HIDDEN/AVAIL row is absent.
5. Long/Important/Short readiness means fulfilled requested targets / all requested targets.
6. Casting Source is understandable while collapsed. Advanced Casting Source exposes priority, disablement, spellbook, availability, and caps without dominating normal setup.
7. Settings contains the only Animated/Instant control. Footer actions are Refresh, Close, and Apply Current Routine.
8. No text is clipped, cramped, blurred, or too low-contrast at the actual resolution; card and portrait clicks feel responsive.
9. Animated and Instant still produce the already-confirmed result; HUD icons/tooltips, click isolation, F10, close, and restored input remain unchanged.

Human verdict is authoritative. Report a screenshot and exact resolution for any failed item. No merge or public release is authorized.

## 0.0.6 installed handoff

Status: automated production screenshot/runtime acceptance PASS twice; 0.0.6 is guarded-installed for final human confirmation.

Installed identity: source `e656812572adea8bc312419372b61ee8c4834e5a`; package `ce7492b262f01a9afb5a7666fe7e4bda9be1821395eb00244f5898b6882208e9`; DLL `6144256c6a0623e908c3d9e821a1b87ee5800195759fbfabb1e587eaf9be1d9b`; MVID `bff11809-aa53-42c2-8ab7-ef3564450e61`; CLR 0.0.6.0.

Please load the same campaign and confirm:

1. The accepted four HUD icons/tooltips/pointer isolation remain unchanged.
2. Setup or F10 opens the opaque planner with ten readable list rows and selected Bless details, matching screenshot SHA-256 `cb234368...` in both final automated runs.
3. Search/filter/reset and row selection update the visible list/details normally; the header says `10 matched | 10 rows bound`, not that internal models were “shown.”
4. No magenta diagnostic canary appears.
5. Long applies the configured prepared Bless and reports a confirmed effect; it must not refuse for a material component. A later attempt may correctly report the spent resource pool exhausted.
6. Close/Escape/F10 restores HUD, selection, camera, pause, and mode without duplicates.

If anything differs, provide a screenshot plus the exact installed identity above. No public release has been made.

## 0.0.5 installed handoff

Status: automated catalog/HUD/input/tooltip acceptance PASS twice; 0.0.5 is guarded-installed for authoritative human visual confirmation.

Installed identity: source `390bb8b5f514a38edf1c553962813e29a1b526fd`; package `3eba3158aa92a6b66e249ec35aa297500eb4c5decdf73974c26992219922349c`; DLL `6999284085bd6898f6bd871900783f6f81343a6f801b2d2c95acd208c6513b56`; MVID `d2fed415-bfa2-47a7-90ba-f50fa8d1c7de`; CLR 0.0.5.0.

Please retest in a real campaign:

1. Confirm exactly one visible Setup/Long/Important/Short row remains in the accepted position.
2. Hover every icon for at least five seconds. Tooltip text should remain steady, wrapped, entirely onscreen, and should not move the row.
3. Click each HUD icon. The party must not move, selection/camera must not change, and no underlying native control should activate.
4. Open Setup and confirm the default catalog contains rows and selected details. Bless should be visible for the tested prepared Cleric state.
5. Exercise search/filter controls, confirm their labels show active state, force an empty result, then use Reset Filters.
6. Confirm Long/Important/Short always show an exact result. A Bless refusal for missing material is expected for the current disposable configuration and must not be reported as applied.
7. Close and reopen through F10 and the normal close control; HUD, selection, camera, pause, and game mode should restore cleanly with no duplicate UI.

Report the exact text and a screenshot for any remaining failure. Automated runs prove the underlying row geometry, physical input isolation, continuous five-second tooltip stability, explicit quick outcomes, and 21-cycle cleanup; human visual smoothness remains authoritative.

## 0.0.4 installed handoff

Status: automated live-bootstrap acceptance PASS twice; 0.0.4 is guarded-installed for optional human visual confirmation.

Installed identity: source `5b96f3b4e713489ce677db3ac5acb83a10f80f01`; package `cb3799e799f641b1a9f7d79eb71942025b5df71a8de956e17369b24fe2f14d16`; DLL `6f72c38ef7e445121291ff2f17f207d49210ea30a2e07fe1105595133b706f1c`; MVID `305a8a6c-2b49-4e3b-a365-286638cbfafa`.

Runs `bootstrap-0.0.4-human-live-6` and `bootstrap-0.0.4-human-live-7` already prove, in distinct fresh processes, the exact four-button row, physical F10, visible/opaque planner, clean close/input restoration, no duplicates after 21 total cycles, and no click-through/world input. A human check should now focus on appearance and ordinary use, not re-prove bootstrap existence:

- load the same campaign and confirm one readable Setup/Long/Important/Short row above the native lower-left cluster;
- confirm Setup and F10 show the opaque planner and Escape/F10/close return cleanly to gameplay;
- confirm no visible overlap or unexpected native activation;
- report any visual scaling or content issue with the installed identity above.

Do not treat older 0.0.3 instructions or results as current acceptance evidence.

Status: 0.0.3 IS INSTALLED; REQUIRED FOR R2 VERDICT. The installed 0.0.2 verdict is FAIL.

Installed identity: DLL SHA-256 `5d95368ee237e658e06b4948209f805568a417ea150eb36c3023df9b155f0950`, MVID `f3f691a4-d691-4112-90a4-7beb9f06aad2`. The preserved profile contains Long → Bless and one target; no provider preference was stored, so the result must identify the provider selected from the live party.

With validated 0.0.3 installed, load the same campaign and verify:

- one horizontal row appears directly above the native bottom-left cluster in exact order: Setup, Long, Important, Short;
- no planner icon or tooltip overlaps a native control/tooltip region;
- clicking any planner icon never activates turn-based mode, pause/hourglass, a world command, selection, ability, camera drag, or camera zoom;
- Setup and F10 both open the same visible, opaque, full-screen `BUFF PLANNER` root;
- F10 never hides the HUD or locks gameplay unless that visible root was constructed and validated first;
- Escape, close, and F10 restore the prior gameplay/HUD/input state;
- Long visibly resolves the preserved Bless assignment, reports the chosen caster/spellbook/target and queued/submitted/started state, and only reports success if `BlessBuff` is confirmed;
- if Bless cannot apply, the visible result names the exact validation, submission, execution, resource, or unconfirmed-effect failure;
- Important and Short visibly report their configured/empty state;
- 20 close/reopen cycles leave one HUD row, no hidden modal root, and no retained input lock.

The historical 0.0.2 checklist below is superseded by this R2 checklist.

Status: REQUIRED FOR 0.0.2 UI VERDICT; save-backed execution remains deferred pending an authorized `KBP_AUTOMATION_WORKING` fixture

Human playtesting is authoritative for the visual and interaction repair. With 0.0.2 installed, load a campaign and verify:

- the lower-left HUD has one native-looking setup icon and adjacent Long, Important, and Short icons;
- no floating `Buff Planner (F10)`/routine text strip remains;
- the setup icon opens a distinct, fully opaque full-screen `BUFF PLANNER` window;
- the world is not readable or interactive behind it;
- empty-background and control clicks never move a character, change world selection, interact, or activate an ability;
- list scrolling does not zoom the world and dragging does not drag the camera;
- Long, Important, and Short group tabs visibly change selection;
- each HUD quick icon visibly reports success, refusal, or its exact unavailable reason;
- an empty Long routine reports `No Long buffs are configured.` instead of doing nothing;
- the close button, Escape, and F10 all close through the same clean lifecycle;
- repeated close/reopen works without duplicate buttons or roots;
- the layout is readable at the actual resolution and target portraits/provider controls are legible;
- tooltips identify setup/F10 and each quick routine clearly.

When an authorized `KBP_AUTOMATION_WORKING` fixture is available, also verify:

- standalone install/load and clean uninstall;
- F10/setup/HUD lifecycle through an area transition;
- search, filters, sorting, hidden and unsupported visibility;
- Long/Important/Short editing, target matrix, provider priority/ban/cap, and bounded clear confirmation;
- caster level, remaining resource, material, rejection, skip/overwrite, and unfulfilled presentation;
- animated execution, instant execution, sticky-touch fallback allowed/blocked, and combat policy;
- pre/post counts against visible effects and exact slots/resources/components;
- profile survival after party reorder and save reload;
- native-only and optional-mod source visibility without duplicate or foreign dependencies.

The no-save native-only and exact Call of the Wild load/catalog/Harmony portions above are already automated and passed twice. Manual acceptance remains limited to the save-backed rows requiring an authorized `KBP_AUTOMATION_WORKING` fixture; Tabletop Added Rules is unavailable locally.
