# Kingmaker Buff Planner 0.2.0-rc4 — Casting-first planner (release candidate)

**This is a local release candidate for the owner's final review, not a
public release.** Nothing is published, tagged or permanently installed
before that review. The classic planner stays the default; the
casting-first planner is an opt-in, experimental mode.

## What changed since 0.2.0-rc3

rc3's final independent review found defects, and rc4 fixes them; a
second independent review of those fixes found more, fixed here too. For
a player:

- **Classic routines no longer give up while the game is paused.** A
  classic routine now advances only while the game runs. **APPLY** in the
  open classic planner checks the routine at once (a refusal is shown
  there), then closes the planner and casts; the result is shown when the
  planner opens again. In rc3 a paused game made the first cast look
  unconfirmed, and the rest of the routine was abandoned.
- **Plans saved by 0.0.19 import.** The first time the casting-first
  planner opens a campaign, a classic plan saved by the released 0.0.19
  (or older) is imported through the classic planner's own reading. Its
  file is not rewritten. rc3 refused such plans.
- **A cast counts only if it landed.** A cast is confirmed only when this
  cast put the effect there: a new effect, or the old one renewed to a
  later end, of the buff the spell grants (another effect of the same
  name does not count). An effect that was already there, or one the
  game suppresses, never confirms a cast. The exception is a group
  casting cast for recipients that lack the buff while others already
  have a provably good-enough one: those may keep theirs unchanged, or
  the game may replace it with this cast's (the card warns when theirs
  lasts longer than this cast). A group casting that would
  reach nobody, or whose caster cannot be its origin, is blocked with a
  reason instead of running; a party buff that also reaches pets is
  planned as a party buff.
- **Every casting can be fully edited.** Editing one casting now changes
  its caster and exact source, its routine and its place in it, whether
  it is cast again when the buff is already there, and whether it is a
  single-target or a group casting. Sources are named exactly: the
  spellbook, its spell level and the kind of slot, or the item or
  ability. The same caster keeps its enhancements when it switches
  source; another caster keeps only the ones it has, and the planner
  names the ones it dropped (Undo restores them). An imported casting
  whose classic plan let the planner pick any caster, or did not say
  single target or group, can be completed and marked Ready in place. A
  caster who can cast a buff in more than one way picks the exact one
  when adding it; a spell known at two levels of one spellbook is refused
  with the reason (this version cannot pin one). The animated fallback
  and the out-of-combat rule have switches.
- **Unreadable files are announced, and the remedy works.** Both
  planners say when their saved file could not be read or comes from a
  newer version, when a backup was loaded instead (and whether saving is
  refused because of it), and when a change was not saved; the
  casting-first header says so for as long as saving is blocked. Moving
  the unreadable casting plan and its backups aside and pressing Reload
  lets it save again, keeping castings added meanwhile. Only the files
  the planner names are to be moved; a reload never discards castings
  added in the session without a second press, and a classic plan
  repaired after a failed import is imported (never an empty stand-in).
- **Smaller fixes.** The casting-first planner never rewrites the classic
  plan file. The spellbook button opens the casting-first planner
  properly. The HUD tooltip never describes another campaign. A new
  casting never reuses the number of a casting it has shown, even after
  a reload. The footer counts the castings of the selected routine that
  would run short of a resource if every routine ran in one pass. A
  linked opposition-school slot pair counts as two slots in the budget,
  and a slot whose cost could not be read counts as one. The first
  import uses the classic planner's own copy of the plan, matched to the
  party's current abilities.
- **Test harness (not player-facing).** A changed ordinary save is
  reported with every other failure and blocks later test runs until the
  owner has reviewed it. The save comparison survives a launcher that
  stops early. A live test run stops itself before its time limit and on
  the launcher's abort signal, checked on every frame. Finite-resource
  test approvals can be written. A rehearsal of the manual session is
  labelled as one. More of the game's own checks run before a test
  launches. After the second review: the saves are read and compared
  while the test run holds its lock, which is kept until the comparison
  is made; fixture changes and test runs cannot overlap; the owner's
  acknowledgement of a save change is typed by the owner and bound to
  that exact record; an incomplete run never reports success. After a
  focused review of those fixes: a comparison that can never be made has
  a recorded way out for the owner's review, a comparison is made only
  while the run still holds its lock, and every scenario obeys the run's
  stop rule.

## What changed in 0.2.0-rc3 (history)

The classic planner was checked in the game in both casting modes; a
classic routine stops at the first unconfirmed cast; free sources stay
free and paid ones stay paid (the plan's reservation decides the route,
an unreadable count is uncertainty); files the planner cannot read are
never overwritten; test approvals name their exact mod set, save and
purpose; builds are byte-identical across checkouts. rc3 was superseded
before release (`docs/evidence/rc-0.2.0-rc3-receipt.md`).

## What it lets a player do

- **Choose the planner mode** in the Unity Mod Manager panel: Classic
  (default) or Casting-first (experimental). The choice is saved in
  `UserSettings/planner-mode.json`.
- **Plan buffs one casting at a time.** The workspace follows the Bubble
  Buffs layout on Kingmaker's own book and parchment:
  - a searchable icon grid of buffs, with Spells / Abilities / Other tabs;
  - routine tabs (Long, Important, Short) with counts;
  - the selected buff's castings as cards, and an inspector.

  Each card is exactly one cast and can be edited on its own. It shows
  the caster and the recipient or group origin, its source, enhancements,
  cost, readiness and routine. Nothing is expanded, merged or substituted.
- **Edit any one casting.** Change who casts it and from which spellbook
  level, item or ability, its routine and its place in it, whether it is
  cast again when the buff is already there, its target or group origin,
  and its enhancements. An imported casting gets its caster in place.
- **See costs and refusals in plain words.** A cost names whose resource
  it uses and what kind ("Linzi: level 2 spell slot", "Hedwirg: free").
  The header shows routine-wide readiness. A refusal says what to do next
  ("choose who receives this casting first").
- **Review, accept, then run.** Accept Plan records the routine's
  contents. Review & Apply, the HUD routine buttons, the hotkey and the
  spellbook button all run through one production host. That host:
  - re-reads the party first;
  - skips buffs already active, and recasts weaker, expiring or
    unprovable ones;
  - stops at the first failed or uncertain cast and never retries.

  Pressing a routine button while a routine runs stops it after the cast
  in progress.
- **Keep the classic plan.** On first open, a campaign's classic plan is
  imported into a separate casting-first plan. The classic file is never
  modified, and a byte-exact copy is archived.

See `docs/CASTING-FIRST-PLAYER-GUIDE.md` for the full guide.

## What was checked, by evidence layer

**Source tests** (Windows PowerShell 5.1, non-interactive, at the
candidate commit; the release-candidate receipt has the counts) cover:
- source validation and protocol tests;
- the runtime-harness filesystem;
- deployment WhatIf and launcher WhatIf;
- fixture inventory;
- install rollback in isolated state;
- the guarded publisher gate.

Every guard added since rc2 has a mutant that the tests catch. Mutants
show that the tests would notice a broken guard; they are not evidence
of behavior in the game.

**Native gameplay.** These are guarded runs on the disposable automation
campaign, never an ordinary save. Each run checked in game that it had
loaded the build it staged (commit, package, DLL and MVID). The receipt
(`docs/evidence/rc-0.2.0-rc4-receipt.md`, added after the freeze) lists
the runs made on the frozen rc4 candidate itself: the Classic Long
routine in both casting modes, the casting-first zero-cost qualification
in both modes (stop, complete, repeat, recast, disable, recover), the
first-open import of a genuine 0.0.19 (schema 4) plan, and the finite
selection on this party. The rc3 receipt keeps the runs made on rc3.

**Restoration:** every run moved the owner's Mods folder aside, staged
only the candidate and the approved mod set, and restored the folder
byte-exact afterwards, including the installed KingmakerGunslinger.
Every save was compared before and after.

**Manual acceptance:** not yet. The consolidated supervised session is
described in `docs/MANUAL-USABILITY-HANDOFF.md`.

**Not yet checked in the game:**
- spells that spend prepared slots or spontaneous levels;
- ability pools;
- group spells, metamagic variants and rods;
- an area change during a run (the test campaign's only exit autosaves
  and ends the prologue);
- pets;
- keyboard and mouse input from a person, and any check that judges game
  frames (the workspace layout at 1920×1200 and 1920×1080, the in-game
  reload, the manual-session rehearsal): game frames render black while
  the owner's remote session is disconnected, so these wait for a
  connected session.

The finite-resource and group qualification is prepared and waits for an
owner-designated advanced test save (`docs/ADVANCED-SEED-COMPATIBILITY.md`):
the automation party's finite spell levels hold no buff.

## Known limitations

- A cast counts only when it put its effect there. When the game keeps an
  existing instance instead (a buff that does not replace itself, a
  longer-lasting instance already present, a permanent effect), the cast
  is reported as not confirmed and the routine stops there. The default
  **If the buff is already there: skip this casting** avoids that only
  when every intended recipient's existing effect is provably at least as
  good, because the casting is then not cast at all. It does not avoid it
  whenever the casting still casts over an existing instance: one that is
  weaker, about to expire or not provably as good (for example when the
  game does not let the planner read its caster level), or any instance
  under **Always recast**. The one case handled is a group casting that
  casts because some recipients lack the buff: a recipient whose existing
  effect was provably good enough may keep it unchanged, and only the
  others need the effect to land. The game may also replace that
  recipient's longer effect with this cast's shorter one (it did in the
  qualification run); the card warns when a recipient's effect outlasts
  the cast, and a casting that gave the longer effect may then be due
  again the next time the routine runs.
- An enchantment a spell puts on a worn item (Magic Weapon, Magic Fang)
  is cast again even when it is already there: this version does not
  read that enchantment's caster level, and a strength the planner cannot
  read never proves an existing effect good enough. If the game then
  keeps the enchantment unchanged, the cast is reported as not confirmed
  and the routine stops there.
- Within one routine, a group casting after a casting that gives one of
  its recipients the same buff: that recipient counts as already covered
  only if its buff was there before the routine started. Otherwise the
  group cast must reach it too, and if the game keeps the earlier cast's
  longer buff, the group cast is reported as not confirmed and the
  routine stops there.
- In Instant mode, a casting whose enhancement needs the game's own cast
  (for example Powerful Change when the Brown-Fur provider offers no
  direct transaction) is cast that way, with its animation. The planner
  records the route in its log, but the card does not show it and the
  accepted plan does not include it.
- The classic planner has no press-again-to-stop: a started classic
  routine runs to its end or to its first unconfirmed cast.
- The wait for a cast's confirmation is counted in game frames.
- A spell known at two levels of one spellbook cannot be pinned to one
  level; the planner refuses that choice with the reason.
- A linked opposition-school slot pair that cannot be funded shows one
  requested slot in the budget.
- Pets: a buff that reaches the party and pets is planned for every unit
  its party effect names; whether the game's party action reaches other
  members' pets is not proven (no test party has pets), so such a cast
  can be reported unconfirmed and stop the routine. A buff that reaches a
  chosen target or an area and also the caster's pet is planned for the
  pet only.
- APPLY in the open classic planner now closes the planner so the party
  casts at once (the planner pauses the game). This changes the default
  planner's behaviour and is for the owner to confirm.

## Not supported in this version (shown on the card, refused by Apply)

- Share Transmutation and other ways of changing whom a spell reaches,
  whether chosen as a targeting modifier or as an enhancement.
- A specific physical rod or item.
- Group castings whose required recipients are outside the predicted
  area.

## Install and roll back (local, guarded)

Exit Kingmaker and Unity Mod Manager first.

```powershell
# From the candidate's clean checkout:
.\scripts\Build-Release.ps1
.\scripts\Install-Local.ps1 -ReleaseManifestPath .\artifacts\release\0.2.0-rc4\release-manifest.json `
    -InstallId <id> -ExpectedPriorVersion 0.1.1-rc3
# To return to the prior version, keeping settings edited since:
.\scripts\Restore-InstallLocal.ps1 -InstallId <id>
```

The installer:
- verifies the package, DLL and manifest;
- keeps `UserSettings` exactly;
- refuses when any other mod would change;
- rolls itself back if a step fails.

`Restore-InstallLocal` restores the prior planner and keeps newer
settings. A casting-first plan the older version cannot read is moved to
the rollback evidence folder.
