# Mr. Bard's Kingmaker Buff Planner 0.6.1 — experimental local candidate

Lowers Unity Mod Manager's minimum numeric game version from 2.1.7 to 2.1.5
for a native Linux 2.1.5d compatibility trial. No planner, controller,
casting, or persistence logic changed. The 0.6.1 DLL differs from 0.6.0 only
because of the version and source-revision stamps; a separate diagnostic ZIP
uses the exact published 0.6.0 DLL with only the manifest edited.

Native Linux 2.1.5d loading, mouse use, saved plans, and casting are **NOT RUN /
unverified**. The installed Windows 2.1.7b API references remain the build
source. This is a local review candidate, not a published release or a
compatibility certification. See the [candidate record and test steps](LINUX-2.1.5D-CANDIDATE.md).

---

# Mr. Bard's Kingmaker Buff Planner 0.6.0

Owner-authorized full release of the controller planner implementation. The exact
release has not been run in the game; native F01-F10 remain NOT_RUN. New native
card, semantic authoring/search, workspace text repair and shared entry/run
handoffs do not inherit Alpha4's qualification. The established pre-runtime
publisher retains complete offline C# tests, publisher checks, reproducible
builds and strict package validation. No unrelated mod, saves, settings or
permanent installation is changed. See the
[0.6.0 release notes](RELEASE-NOTES-0.6.0.md) and
[release record](RELEASE-0.6.0-RECORD.md).

---

# Historical local source candidate 0.5.4-controller.4 — full controller planner (unqualified)

Implements semantic controller focus over the existing workspace's authoring
controls, controller search/text modal, exact source/recipient/options/order/Undo/
recovery paths, a genuine native campaign-menu card and direct spellbook access.
Run hands off safely to selected-routine confirmation; shared execution remains
canonical. Adds owned transition input protection, root-canvas host selection,
inactive-parent-safe TMP initialization, owned controller font materials and
bounded first/settled workspace diagnostics. Mouse navigation/highlight behavior
is retained. Alpha 3's text-cancel and viewport-scroll corrections remain.

This assignment is NOT complete/native-qualified. Actual new-byte card, controller-
only authoring, rendering at 1920x1200/compact, finite-resource execution and
lifecycle/restoration still require guarded observations. [Inventory](CONTROLLER-AUTHORING-CAPABILITIES.md).
No new public release/tag, push/PR edit, main merge, installation or gameplay-mod
change selected. Existing published Alpha 3 is untouched.

## Historical controller test alpha 0.5.4-controller.3 — focused review corrections

Includes the .2 same-session Configure/editor access, console-book entry and
text-container initialization repair. Fixes two reviewed presentation/input cases:
owned TMP Escape cancellation is captured before focus is lost, even when the
planner observes the key later; controller result scrolling now advances by
three-quarters of the viewport rather than a fraction of the whole report.
No extra editor layer is dismissed by the text-cancel press; long reports retain
overlap between views. Existing shared Back latch and execution path remain.

Owner requested an alpha for testing and PR update, not native qualification or
stable release. Full graph editing/native fan card remain deferred. See
[test instructions and limits](CONTROLLER-ALPHA-0.5.4-CONTROLLER.3-NOTES.md).

## Historical local review candidate 0.5.4-controller.2 — controller editor access/layout repair

Not published, installed or native-qualified. Adds Configure plans into the
ACTUAL editor in a controller-started campaign, with mouse/keyboard authoring,
canonical autosave and a neutral-gated return to quick access in the same session.
Fixes text creation order against installed TMP default-container initialization;
essential messages/controls are unpaged and longer results get a scrollable view.
Adds a context-scoped console-book footer into that editor, preserving native Y
book switching/Metamagic and retaining the accepted campaign-menu shortcut.
Full controller graph editing, native fan card and original-book reconstruction
remain deferred. Native rendering (1920x1200/compact), pointer/text interaction,
physical input and full author/save/return/run qualification are NOT_RUN.
No gameplay-mod changes/dependencies, release/tag/push, merge or permanent install.
The 0.5.4-controller.1 published frozen alpha below remains unchanged.

## Historical local candidate 0.5.4-controller.1 — controller execution

Not published or permanently installed. Integrates the controller mission with
0.5.3 branding/licensing and publisher changes, plus the three PR review fixes.
The version suffix identifies a local candidate, not a new stable release.
The original 0.5.2-controller.1 candidate and evidence remain historical.

Create/edit plans with mouse and keyboard; run saved routines and request Stop
with a controller in native controller mode.

- Adds a Buff Planner entry with a native mapped-button hint to the campaign
  controller menu, plus Long / Important / Short and explicit Stop.
- Uses the same campaign session, latest-edit durability barrier, strict/subset
  policy, exact casting projection and Instant/Animated host as the mouse routes.
- Adds visible focus, current mode/policy/save state and paged per-casting results
  and refusal reasons; controller refusals do not open the mouse editor.
- Uses the native input stack and scoped binding ownership, with neutral latching
  and lifecycle cleanup. Retained Classic choices direct the player to mouse
  setup; controller Classic execution and full controller editing are deferred.

Source-tested with named qualification gaps: cold-controller startup, physical
mapping/isolation, game rendering, actual effects/resources and live restoration
are pending. Windows device detection is not gameplay qualification. See
[implementation](CONTROLLER-UI-IMPLEMENTATION.md) and
[E01–E13 acceptance](CONTROLLER-UI-ACCEPTANCE.md). No controller-support claim for
Steam Deck, Linux, consoles, other device families or remappings.

---

# Mr. Bard's Kingmaker Buff Planner 0.5.3

Owner-authorized stable release, 2026-10-15.

0.5.3 renames the mod and adds an explicit license. It was published as
**Kingmaker Buff Planner** through 0.5.2; Unity Mod Manager now lists it as
**Mr. Bard's Kingmaker Buff Planner** by **Mr. Bard**. The planner itself is
the 0.5.2 planner.

## What changed

- **Name and author.** The UMM display name and author, the DLL's
  title/product/company, and the bootstrap diagnostics label on the mod's UMM
  page. The planner window, HUD buttons, spellbook button and game-log lines
  keep their "Buff Planner" labels.
- **MIT license.** The mod is released under the MIT License
  (`Copyright (c) 2026 Mr. Bard`). The ZIP now includes `LICENSE` next to
  `THIRD-PARTY-NOTICES.md`, which keeps its design-reference credits.
- **Same mod underneath.** The UMM ID `KingmakerBuffPlanner`,
  `KingmakerBuffPlanner.dll`, its entry method, the
  `Mods\KingmakerBuffPlanner` folder and the `UserSettings` profile files are
  unchanged. The only new package entry is `LICENSE`.

## Upgrading

Back up the whole `Mods\KingmakerBuffPlanner` folder, including
`UserSettings`, outside `Mods` before installing. 0.5.3 installs over 0.5.2
or an earlier version as the same Unity Mod Manager entry, now shown under
the new name. 0.5.3 reads the same plan format as 0.5.2, so 0.5.2 plans are
expected to load unchanged; this follows from the unchanged persistence code
and was not exercised in a live upgrade. To roll back, restore the complete
backup.

## Validation boundary

The owner authorized this release without a further human playtest. Source
validation, the complete offline C# behavior suite, the publisher gate tests,
two deterministic release builds and strict package validation ran for this
release. 0.5.3 has not been run in the game, and the broad source-only
aggregate (runtime-harness, launcher, deployment, restore and filesystem-purity
checks) was not run. Its planner code is unchanged from 0.5.2, whose
validation boundary below applies unchanged.

---

# Kingmaker Buff Planner 0.5.2

0.5.2 publishes the human-feedback refinement while preserving the approved
native appearance and HUD buttons.

## What changed

- **Readable Resources.** Resources opens as an opaque native parchment dialog
  with visible grouped accounting, fixed columns, bounded scrolling, selected
  routine/all-routines scopes, reliable reopening and proper focus/close input.
- **Continuous assignment.** A normal caster or exact source configures the next
  assignment. Normal recipient clicks add or remove independent castings without
  silently editing the casting being inspected. Existing castings change only
  through the explicit one-shot recipient or caster/source edit controls.
- **Scoped draft choices.** Share Transmutation and other draft enhancements are
  preserved while batching recipients in an unchanged context and cleared when
  the buff, caster, exact provider or routine changes. Saved castings are never
  stripped merely by navigation.
- **Strict or available casting.** Each profile remembers **Require full plan**
  or **Cast available**. Planner and HUD resolution share the same fresh
  validation and execution path; permissive runs preserve authored order,
  complete casting costs, shared pools, assignments and omission reasons.
- **Responsive, observable UI.** Revision-specific work is cached where safe,
  full rebuild/theme/layout work is reduced, and opening, switching, recipient
  and Resources interactions record separately attributed timings.
- **Ordinary labels.** The controls say **Run Long**, **Run Important** and
  **Run Short** while retaining the native skin.

## Upgrading

Back up the whole `Mods\KingmakerBuffPlanner` folder, including
`UserSettings`, outside `Mods` before installing. Existing profiles remain
strict until you explicitly choose **Cast available**. Saved assignments load
unchanged. To roll back, restore the complete backup.

## Validation boundary

Focused source, behavior, display-transaction and package checks passed for the
human-feedback candidate. The selected rc5 live milestone stopped before game
launch when the physical display changed during its guarded purity prerequisite,
so it produced no fresh rendered UI, input-to-frame timing or permissive-casting
observation for these new paths. Historical 0.5.0 evidence still covers the
unchanged native appearance and HUD. The exact retained record is in
`docs/HUMAN-FEEDBACK-REFINEMENT-0.5.1-RC5.md`.

The calculation-only 67-casting probe was fast, but it does not rule out the
reported one-to-two-second delay in a real populated campaign. This stable
release is therefore suitable for the owner's requested full ordinary testing,
with these limits stated rather than promoted to runtime PASS.

---

# Kingmaker Buff Planner 0.5.0

0.5.0 makes the planner look and behave like one of the game's own windows.
The planning model, routines, saved plans, Undo, resource accounting,
enhancements and the HUD buttons' icons, order, actions and tooltips are
unchanged.

## What changed

- **HUD.** The four buttons (gear, moon, diamond, sun) are a third row of
  the game's own button panel, directly above its first four columns. The
  panel's frame rises over them and steps down after the fourth. Each button
  is the game's own button art with its symbol engraved in the game's gold,
  and it lights up on hover like its neighbours.
- **Spellbook.** **BUFF PLANNER** now sits beside **METAMAGIC** in the
  spellbook's footer, as the same kind of button. It is there for every
  character, including one without Metamagic. The old button at the window's
  top right is gone.
- **Spell descriptions.** Right-click a buff, the selected buff's header or a
  casting card to open the game's own spell description window, the one the
  spellbook opens. A casting shows its caster's own spell; an entry with no
  caster chosen shows the base spell. **Escape** closes it first.
- **Other windows.** Opening the inventory, character sheet, spellbook or
  another game window (by its key, its HUD button or its tab) closes the
  planner and shows that window. Reopening the planner brings back the same
  buff and plan. Typing in the planner's search never triggers the game's
  keys.
- **Game look.** All planner text uses the game's own fonts, with its
  ornamented title, small-capital headings and button captions. Buffs,
  sources, castings and recipients sit on paper slips with thin frames and
  framed portraits; the selected one is marked in burgundy. Scrollbars,
  rules, buttons and checkboxes are the game's own.
- **Shorter wording.** The title is just "Buff Planner"; the columns are
  **Caster & source**, **Castings** and **Recipients**. A casting card
  shows its state in a few words, for example "Active · Will skip" or
  "Active · Will recast". A buff shows a count only when it has castings.
  Recipient portraits say what a click does ("Click to add a casting",
  "Click again to remove #1").
- **Casting options.** The panel on the right follows one order: **Done**,
  who casts on whom and from which source, the state, **Recast if active**,
  **Enhancements**, the casting's resources ("If cast" when the run will
  skip it), Cast earlier / Cast later, and **Remove casting**. While adding,
  it shows only the next step and the default for new castings. Moving a
  casting to another routine is no longer in this panel: select that routine
  and add it there.
- **Footer and game log.** The footer shows the save state, one line saying
  what **Run** will do or exactly why it cannot, and **Resources**, **Mode**,
  **Undo** and **Run**. **Resources** opens the full accounting: the
  selected routine on its own and the whole plan without rest. Each run, and
  each refused HUD press, now adds one line to the game's own log, for
  example "Buff Planner: Long done - 3 of 3 cast, 1 already active."
  Hovering the line shows the full result. The game still logs each cast
  itself.

## Upgrading

Back up the whole `Mods\KingmakerBuffPlanner` folder (with `UserSettings`)
outside `Mods` before installing. 0.4.3 plans load unchanged. To roll back,
restore the backup.

## Checked in the game, and not

The automated in-game checks use the disposable automation campaign only.
The exact runs, builds and results are in `docs/QUALIFICATION.md`.

- **Checked:** the HUD row, frame, tooltips, idle, hover, pressed and genuine
  execution-disabled states; the spellbook pair, Metamagic entry and Return,
  and unavailable planner action; native and casting-card description
  windows and their Escape/close order; normal and paused window switching;
  physical mouse and keyboard search, selection, authoring, Undo and blocked
  navigation; catalogue filters; active skip/recast; Advanced groups,
  enhancements and finite prepared resources; casts, refusals and results in
  the game log; and the planner/HUD/spellbook at 1920x1080 and 1280x720.
- **Not checked:** an organic long or localized spell description (the
  in-game bounded layout probe was used), a custom remapped binding, and UI
  scale above the default with a genuinely long campaign catalogue.

---

# Kingmaker Buff Planner 0.4.3 (published)

0.4.3 is the published release of the owner-accepted 0.4.2 build (commit
`7a0842f`, including the fixes from its review), with the version advanced so
Unity Mod Manager sees it as newer than that build. It follows up the owner's
feedback on 0.4.1 with three fixes.

## What changed

- **Shadow Clone.** The Ninja's Shadow Clone (Call of the Wild) now has its own
  card under Abilities and can be planned for the ninja who has it
  (Personal: the ninja only). It used to be folded into Mirror Image's card
  because both apply the same buff, so a ninja without the spell had nothing
  to plan. Every class ability now has its own card. Spells
  with the same effect still share one. A class-ability casting saved by
  0.4.1 under a spell's card moves to the ability's own card once, and the
  0.4.1 plan file is kept beside it first (`.pre-0.4.2-source-identity`).
- **Spells removed from a spellbook.** When you remove a spell from a
  caster's spellbook, the planner removes the castings that depended on it.
  Removing means retraining it away for a spontaneous caster, or un-preparing
  its last prepared copy. This happens the next time you open the planner or
  press a routine, with the spellbook closed. The planner saves the change
  and tells you what it did, for example "Removed 2 Mind Blank castings: no
  longer prepared in Felix's and Leinna's spellbooks. Undo available." Undo
  brings the castings back, and they then stay until that spellbook changes
  again. The plan file from before the first such removal is kept beside it
  (`.pre-spellbook-removal`). Nothing is removed when:
  - a slot is spent or a caster has no uses left;
  - a caster is dead, unconscious or out of the party;
  - the game is loading;
  - a spellbook could not be read with certainty;
  - the party is in combat or a routine is running.
  Stale castings such as the earlier Mind Blank ones are cleared on the first
  open with 0.4.3, provided the spell is no longer in those spellbooks and
  their casters are in the party, alive and conscious.
- **Native paper and opening sound.** The planner and the spell description
  are now drawn on the same aged page the game uses for its inventory,
  character sheet and journal, over the game's own table. The page has
  subdued colour, worn, irregular and layered edges, and shading from top to
  bottom. The ink is darkened to match. Opening the planner now plays the
  game's own paper sound, the one the character screen, spellbook and journal
  play. The layout and controls are unchanged. All of this is borrowed from
  the running game: no game art or sound ships with the mod. If any of it
  cannot be found, the planner falls back to the 0.4.1 paper, then to the
  plain look.

## Upgrading

Back up the whole `Mods\KingmakerBuffPlanner` folder (with `UserSettings`)
outside `Mods` before installing. 0.4.1 plans load unchanged apart from the
two one-time changes above, each of which keeps the earlier file. To roll
back, restore the backup.

## Checked in the game, and not

The automated in-game checks use disposable test campaigns only.
- **Checked:** removing a spell from a spontaneous caster's book removed
  exactly its castings, with the notice. Spending all of another caster's
  slots removed nothing. The new paper, table and opening sound were checked
  on the same build.
- **Not checked:**
  - No test party has a ninja, so a real Shadow Clone cast has not been made
    in the game. The ability's card, legality and plan are proven against its
    native blueprint.
  - No mutable test party has a prepared caster. Un-preparing, spending the
    last prepared slot and pending rest are proven against the game's own
    spellbook code, not in the game.
  - The automation proves that the opening sound is posted exactly once per
    open through the game's own sound system. Hearing it is the owner's
    check.

The exact record is in `docs/OWNER-FEEDBACK-0.4.2-HANDOFF.md`.

---

# Kingmaker Buff Planner 0.4.1 (published)

0.4.1 is the published release of the 0.4 work: the owner-accepted 0.4.0
review build with the version advanced so Unity Mod Manager sees it as newer
than that build. It completes the casting-first planner's remaining work: the spellbook
button works, routines never run in combat, Instant mode is strict, castings
are edited directly in the graph, the beneficial-buff catalogue is audited,
Magic Circle against Alignment casts instantly, and the planner and the
spell description are drawn on the game's own scroll paper.

## What changed

- **Spellbook button.** The Buff Planner button in the native spellbook now
  opens the planner (it used to sit under the window's close button). The
  spellbook closes first; if the planner cannot open, the spellbook comes
  back.
- **Never during combat.** A routine pressed while the party is in combat is
  refused with "Buff routines cannot run during combat." - in the
  casting-first planner and the Classic planner alike, before anything is
  planned, saved or spent. This is no longer a setting.
- **Strict Instant.** In Instant mode a casting that cannot be cast
  instantly is Not Ready with its reason (the planner shows it); choose
  **Animated** to cast it normally. The "animate buffs that cannot be
  instant" checkbox is gone.
- **Direct graph editing.** With a casting selected, click its recipient
  again to remove it, another portrait to move it (a group casting is
  re-centred), another caster or source row to change who casts it. Done or
  Escape clears the selection. Disable, Duplicate and the Cast By and
  required-recipient menus are gone; a legacy Disabled casting still loads
  and can be removed. Share Transmutation is offered only where it applies.
- **Group castings** affect whoever the area reaches; nobody is "required"
  and a member outside the area never blocks the cast. Required-recipient
  choices from 0.3.0 are archived beside the plan the first time it saves.
- **Catalogue audit.** Attacks with caster riders (Hideous Laughter), combat
  maneuvers, harmful conditions (Dazing Touch), save-gated effects, heals and
  restorations with their cooldowns (the Heal skill's Treat Affliction / Treat
  Deadly Wounds, Counter Curse, Kinetic Healer) and revivals (Inspiring
  Recovery) no longer appear as buffs. Buffs whose effect lives in the
  abilities that read them stay (Targeted Bomb Admixture, Venomous Strike,
  Elemental Bastion, School Understanding), as do Light and Daylight. With
  Call of the Wild, the shaman's Battle / Bone / Wind Ward and Draconic
  Resilience hexes are planned by their ward buff, and 20 more revelations and
  hexes appear (Air Barrier, Spirit Shield, Ice Armor, Armor of Bones, Time
  Sight, Gift of Claw and Horn, Mythmaker).
- **Magic Circle against Alignment** (KingmakerGunslinger) casts instantly
  on an ally in Instant mode.
- **Scroll paper.** The planner and the right-click spell description are
  drawn on the game's own parchment (borrowed from the game at runtime; no
  game art ships with the mod), falling back to the previous look if the
  paper cannot be found. Clicking outside the description closes it.

## Upgrading

Back up the whole `Mods\KingmakerBuffPlanner` folder (with `UserSettings`)
outside `Mods` before installing. 0.3.0 plans load unchanged; their stored
combat/animation preferences are read but no longer honoured, and the next
save writes the enforced values. To roll back, restore the backup.

## Requirements and compatibility

- Pathfinder: Kingmaker Enhanced Plus Edition 2.1.7b with Unity Mod Manager.
- Checked alongside the same mod set as 0.3.0 (BagOfTricks, BetterVendors,
  CallOfTheWild, CheatMenu, CraftMagicItems, EddicKingmakerRespec,
  KingmakerBugfixes, KingmakerDiceRoller, KingmakerGunslinger 0.0.133 and
  0.0.136, KingmakerLastAzlantiPreserver, ProperFlanking2, RacesUnleashed,
  SkipIntro, TweakOrTreat, ZFavoredClass). None is required; Share
  Transmutation needs KingmakerGunslinger's Brown-Fur Transmuter and Magic
  Circle against Alignment comes from KingmakerGunslinger.

## Known limitations

- The exact in-game qualification (of the 0.4.0 candidate rc4 `009b6dd`,
  which 0.4.1 repeats with only the version number and documentation
  advanced) and its limits are in `docs/REMAINING-WORK-0.4.0-HANDOFF.md`.
- Instant mode's "Not Ready" for a casting that can only be cast animated has
  no in-game check: after the Magic Circle repair no test party has such a
  source (it is proven by the planner's own compiler and navigation tests).
- Group re-centring by portrait click is checked in code, not in the game
  (the standard test party has no group spell).
- At 1280x720 the catalogue's Spells / Abilities tab captions wrap.
- Light and Daylight stay in the catalogue as utility buffs (they give
  light and have no other mechanics).
