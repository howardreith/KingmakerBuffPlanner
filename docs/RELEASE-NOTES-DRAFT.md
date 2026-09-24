# Kingmaker Buff Planner 0.2.0-rc5 — Casting-first planner (release candidate)

**This is a local release candidate for the owner's final review, not a
public release.** Nothing is published, tagged or permanently installed
before that review. The classic planner stays the default; the
casting-first planner is an opt-in, experimental mode.

## What changed since 0.2.0-rc4

The owner's review of the rc4 source found two defects. The
qualification on the owner's advanced test campaign, and an independent
review of the work, found more. All are fixed. For a player:

- **An effect the planner cannot fully read is never "good enough".** With
  **If the buff is already there: skip this casting**, an existing buff
  counts as already covering a recipient only when the planner could read
  that it is not suppressed, the caster level it came from, and the
  caster level of the planned casting. Otherwise the casting goes ahead
  and the card says why: "not provably as strong" or "possibly
  suppressed". rc4 treated an unreadable suppression flag as "not
  suppressed" and an unreadable caster level as good enough.
- **Group castings with mixed coverage.** Some recipients of a group
  casting may already have an adequate buff while others do not. The
  casting is then cast once for the others, at its usual cost, and the
  card says "already active on (the character) (the cast goes ahead for
  the others)". Those recipients may keep their buff unchanged, or the
  game may replace it with this cast's. In the qualification run the game
  replaced a longer buff with the shorter one, and the card now says so
  beforehand ("... may shorten it"). Every other recipient still needs
  the buff to land. Before, a kept instance made the whole cast count as
  unconfirmed.
- **Powerful Change in Instant mode.** A casting with Brown-Fur Powerful
  Change, or another enhancement that needs its provider's own cast or a
  native command, now casts that way in Instant mode, as the classic
  planner always did. Before, the casting-first planner cast it by the
  game's rule directly, a path the provider does not take part in, so the
  spell would have landed without its enhancement. Animated mode was not
  affected.
- **Share Transmutation chosen as an enhancement is refused.** It changes
  whom the spell reaches, which this version does not execute. The
  casting keeps the choice, shows it, and is not cast. Before, it could
  run with Share applied to the caster's own target.
- **The card names the character.** Notes about a buff already on a
  recipient now name the character instead of an internal id.
- **Only enhancements the casting can take are offered.** Powerful Change
  for an ability score that none of the caster's spells raises was
  offered on every spell (choosing it blocked the casting). The workspace
  now offers exactly the enhancements the planner would accept for that
  casting's source.
- **Archived plans are checked byte for byte.** When a plan is migrated,
  its original is archived. An archive already at the expected name is
  reused only when it holds exactly the original bytes. A different file
  there is kept untouched, and the original goes to a numbered name, or
  to a name made from its own content once the numbered names are taken,
  so a readable settings file can never become unreadable for want of an
  archive name. The classic planner's pre-schema archive now keeps the
  exact original bytes, byte-order mark included.

For the release checks (not visible in play):
- a separate compatibility profile for the owner's advanced test campaign
  (Gunslinger 0.0.136, staged from an exact copy);
- a frozen BagOfTricks copy whose cheats are verified off;
- variant spells (such as the four forms of Protection from Alignment)
  accepted by the qualification tooling's plain-buff check;
- two new qualifications, group buffs with mixed coverage
  (`group-mixed`) and a per-casting enhancement (`enhanced-direct`,
  Powerful Change chosen through the workspace, with the stat modifier,
  the Arcane Reservoir, the caster's toggles and the route that ran all
  read from the game);
- a capability inventory that lists the exact effects each source
  applies.

## What changed in 0.2.0-rc4 (history)

Classic routines advance only while the game runs, and **APPLY** checks
the routine at once and then casts with the planner closed. Plans saved
by 0.0.19 import through the classic planner's own reading. A cast counts
only if it landed. Every casting can be fully edited, with exact sources.
Unreadable files are announced, with a remedy that works. The test harness
gained ordinary-save and fixture protections. rc4 was superseded by rc5
before release after the owner's review of its source
(`docs/evidence/rc-0.2.0-rc4-receipt.md`).

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
.\scripts\Install-Local.ps1 -ReleaseManifestPath .\artifacts\release\0.2.0-rc5\release-manifest.json `
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
