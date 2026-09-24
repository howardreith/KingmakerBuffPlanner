# Advanced seed: the one missing input and its compatibility decision

Status (2026-09-24): **seed designated and the advanced profile
registered.** The owner created `KBP_ADVANCED_SEED` as a new, disposable
*Beneath the Stolen Lands* save (a cleric, a brown-fur transmuter, an
alchemist with Transfusion and a fighter, prepared for this fixture) under
Gunslinger 0.0.136, and approved option 1 below. See "Decision" at the end.
The 2026-09-23 request is kept below unchanged.

## The input

In the game, load the advanced campaign of your choice and save it through
the ordinary save dialog as a **new** manual save named exactly
`KBP_ADVANCED_SEED`. No script ever reads or touches the original save.

## The compatibility decision it implies

The automation profile `full-user` now stages the sealed Gunslinger
**0.0.133** (exact external copy). Your installed Gunslinger is **0.0.136**.
A seed saved now is saved under 0.0.136, and it will not be loaded under
the 0.0.133 configuration without establishing compatibility. The options:

1. **A separate advanced profile sealed at your installed 0.0.136**
   (recommended if the seed is saved now). It would equal `full-user` in
   every other entry, with Gunslinger sealed at exactly these measured
   identities of your installation (read-only, 2026-09-23):

   | Field | Value |
   | --- | --- |
   | version | 0.0.136 |
   | directoryManifestSha256 | `d08f0d5a2b3c9d9ef4fdc715caee1b76833d4128d97b9de8f8adb622c420e21f` |
   | fileCount | 238 |
   | totalBytes | 47128988 |
   | KingmakerGunslinger.dll SHA-256 | `c6cccdac914ed59fa4d85d020108588d7d12cfb4ac38cf5a162772bacc9b465c` |
   | Info.json SHA-256 | `f66de05d5c6282eece8218b6c4f31d49dfc8ef9efa27dfeceeda712034717e17` |

   This is a new seal, so it needs your explicit approval; nothing is
   resealed automatically, and `full-user` keeps 0.0.133.
2. Save the seed while 0.0.133 is installed. This would require you to
   downgrade temporarily, which the automation never does.

The Steam Cloud state needs no answer: every guarded run records it and
refuses unsafe states. The latest runs observed Steam offline with Cloud
sync disabled for Kingmaker (`orchestration.json`, `steamSafety`:
"Sync Disabled; offlineMode=true").

This is asked once (owner mission section 9); all other work continues
meanwhile.

## Decision (2026-09-24)

- **Profile `advanced-gunslinger-0136`** (`compatibility/profiles/`): equal
  to `full-user` in every entry except Gunslinger, which carries exactly
  the six values in the table above. A test pins both: the advanced
  Gunslinger entry to those values, and `full-user` to its exact 0.0.133
  copy.
- **Staged from exact copies.** The Gunslinger development session now
  installs its own builds (0.0.139 on 2026-09-24), so the approved 0.0.136
  bytes come from an exact external copy
  (`examples\KingmakerGunslinger-0.0.136`, taken from that lab's
  pre-deploy snapshot of 08:31 local and verified against all six values
  before and after copying). Each run stages it in place of the installed
  Gunslinger and restores the installed directory byte-exact.
- **BagOfTricks resealed in both profiles** (owner-approved). It rewrites
  its own `Settings.xml` whenever the game exits, so its identity moved
  after the owner's preparation and again at the other session's exits.
  Both profiles now stage one frozen copy (`examples\BagOfTricks-20260924`,
  `bc3e790b...`, 1805726 bytes; version, info and assembly unchanged). Its
  provenance records the resource-altering toggles verified off (unlimited
  casting, metamagic, material components, infinite abilities, instant
  cooldown, restore after combat, no resource cost, among others) and the
  spells-per-day multiplier at 1.
- **One pairing, both ways.** The launcher refuses the advanced copy under
  any other profile, and the advanced profile with any other fixture,
  before any save lookup or deployment; the host re-checks the same pairing
  from the request (`live-save-family-profile`,
  `advanced-profile-without-advanced-copy`).
- The two sessions share the installation by lock: this lab's
  `runtime-state\deployment.lock` and the Gunslinger lab's
  `compatibility.lock` exclude each other; neither starts while the other's
  lock or a game is present.

## What happens next (under the existing mission authority)

The guarded bootstrap seals the advanced pair; `live-advanced-inspect`
runs non-casting; after it completes cleanly (game PASS, owned exit,
verified restoration, clean protected saves), the finite-resource
selection and a bounded, allowance-bound qualification run follow.
