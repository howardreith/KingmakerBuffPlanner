# Kingmaker Buff Planner 0.4.0 — owner-review candidate (draft)

**Draft for owner review. 0.4.0 is not tagged or released.**

0.4.0 completes the casting-first planner's remaining work: the spellbook
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
  refused with "Buff routines cannot run during combat." Nothing is spent.
  This is no longer a setting.
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
- **Catalogue audit.** Attacks with caster riders (Hideous Laughter), the
  Heal skill's Treat Affliction / Treat Deadly Wounds cooldowns, harmful
  conditions (Dazing Touch), save-gated effects, activation markers and
  restoration trackers no longer appear as buffs. Light, Daylight and
  Elemental Bastion also leave the catalogue (their buffs have no mechanics
  of their own).
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

- The exact in-game qualification of this candidate and its limits are in
  `docs/REMAINING-WORK-0.4.0-HANDOFF.md`.
- Instant mode's "Not Ready" for a casting that can only be cast animated has
  no in-game check: after the Magic Circle repair no test party has such a
  source (it is proven by the planner's own compiler and navigation tests).
- Group re-centring by portrait click is checked in code, not in the game
  (the standard test party has no group spell).
- At 1280x720 the catalogue's Spells / Abilities tab captions wrap.
- The visual look (scroll paper) is the owner's judgement to accept.
