# Advanced-save copy inspection — request, NOT approved

Status: **REQUEST ONLY.** Nothing here selects, copies, overwrites,
reseals or loads any ordinary save. Casting stays disabled for this
whole activity. It needs the owner's separate approval and one action
only the owner can take (step 1).

## Why

The WORKING fixture is a tiny early party (two cantrips, a few feat
abilities). It proved the zero-cost path and the probe mechanics, but it
cannot show how the casting-first workspace behaves with a real
mid- or late-game party. A private, non-casting inspection of an advanced
party is the fastest way to find the product gaps that matter. It does
not need perfect rods, every class adapter, a public release or finished
aesthetics.

## What the owner would do (only step that touches saves)

1. In the game, load the advanced campaign of the owner's choosing and
   save it through the ordinary save dialog as a **new** manual save
   named exactly `KBP_ADVANCED_SEED`. The original save file is never
   read, copied or touched by any script; only this new seed is used.
2. Tell Claude which compatibility profile that campaign was played
   with (`full-user` or another), and whether Steam Cloud sync is on for
   Kingmaker.

## Tooling status

| Piece | Status |
| --- | --- |
| Sealed advanced pair from `KBP_ADVANCED_SEED` | **implemented**: `New-KbpAutomationFixture.ps1 -Family Advanced` (same staged, journaled, recoverable transaction as the automation pair; seed archived; every pre-existing save re-verified byte-identical). Isolated test in `Test-RuntimeHarness.ps1` (advanced pair produced under its own names, automation pair/seed and ordinary saves unchanged, refusal without an advanced seed). Not run against the real save folder. |
| Refusing a same-ID retry without deleting prior history (review O1) | **implemented** (`3c784c1`), isolated regressions A–E |
| Advanced-pair load identity | **implemented** as a lookup: `Get-KbpDisposableSavePair -Family Advanced` returns only an exact, campaign-correlated advanced pair. Isolated tests cover no advanced pair, both families present, and a mixed-campaign pair. No scenario uses it yet. |
| Protected-save comparison | **implemented** as helpers: `Get-KbpSaveFolderSnapshot` and `Compare-KbpSaveFolderSnapshot` report new files (autosave, cloud), removed files and changed files other than the allowed WORKING copy. Isolated test only; not yet wired into a launcher run. |
| Launcher/host scenario that loads the advanced WORKING copy | not implemented (the host's live-save contract still names the automation pair) |
| Compatibility inventory | existing profile machinery (`-CompatibilityProfileId`) applies; the owner must name the profile |
| Party-roster verification | not implemented (needs an in-game read) |

## What the tooling does and will do

This extends the existing guarded fixture design (`New-KbpAutomationFixture.ps1`)
to a second, separate family. Nothing is shared with the current
`KBP_AUTOMATION_*` pair.

| Safeguard | How |
| --- | --- |
| Immutable original | Only the `KBP_ADVANCED_SEED` file is accepted, matched by header name. Its bytes are hashed and archived outside the save folder before anything else; the seed itself is never modified. |
| Disposable copy | A sealed `KBP_ADVANCED_BASELINE` and one mutable `KBP_ADVANCED_WORKING` are published by the same staged, journaled, recoverable transaction the current fixture uses. Only the WORKING copy is ever loaded. |
| Identity | gameId, game name, area and the party roster (unit ids, classes, levels) are recorded from the header and verified at load. A mismatch refuses. |
| Mod compatibility | The run uses the exact compatibility profile the owner names. The loaded optional mods (id, version, assembly hash) are inventoried and compared; any mismatch refuses before loading. |
| Candidate isolation | The staged planner folder starts with no `UserSettings`. Any candidate the workspace writes stays inside run-owned storage and is discarded by the transaction restore. The owner's installed planner and settings are restored byte-exact and verified by manifest. |
| Autosave / cloud | The scenario never saves and never changes area. The save folder's manifest (all files, hashes) is compared before and after; any new or changed file other than the WORKING copy is a FAIL. If Steam Cloud is on, the owner is asked to pause it for the session, or the session does not run. |
| Restoration | Standard transaction restore with Mods manifest verification, plus the save-folder comparison above. |

## The first session: inspection and authoring only

It runs as a supervised manual session (zero synthetic input, owner at
the keyboard) or as an automatic read-only pass. It has no dispatch
boundary, no Apply to game, and no save. It checks:

- Opening reaches the **casting-first workspace**, not the legacy
  screen. The existing `workspaceRoot=active; legacyScreen=closed` check
  applies.
- Catalogue size, search, and scrolling with a large buff list.
- Multiple spellbooks, variants, spontaneous and prepared casters.
- Portraits, including pets and animal companions.
- Existing imported intentions, if the planner's legacy plan for that
  campaign is supplied. This is a separate owner choice, and without it
  the session starts empty.
- Long and group buffs, coverage and resource forecasts per routine.
- Input isolation (clicks and keys do not reach the game).
- Remaining unsupported options, recorded explicitly as gaps.

Nothing is manufactured: no equipment, features or spells are added to
the party. Anything the discovery cannot represent is written down as
a gap, not worked around.

## What it is not

It is not advanced-party qualification. Paid-slot, multi-cast, group,
interruption and reload gameplay tests each need their own gameplay
authority later.
