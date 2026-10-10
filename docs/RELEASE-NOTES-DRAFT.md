# Kingmaker Buff Planner 0.4.2 (owner-test candidate)

0.4.2 is a local owner-test build. It follows up the owner's feedback on 0.4.1
with three fixes. It is not published: 0.4.1 stays the published release
until the owner accepts this build.

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
  open with 0.4.2, provided the spell is no longer in those spellbooks and
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

## Not checked in the game

The automated in-game checks use disposable test campaigns only. These are
not covered yet:
- No test party has a ninja, so a real Shadow Clone cast has not been made in
  the game. The ability's card, legality and plan are proven against its
  native blueprint.
- No mutable test party has a prepared caster, so un-preparing and spending a
  prepared slot are proven against the game's own spellbook code, not in the
  game.
- The automation proves that the opening sound is posted exactly once per open
  through the game's own sound system. Hearing it is the owner's check.

The exact record is in the 0.4.2 handoff.

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
