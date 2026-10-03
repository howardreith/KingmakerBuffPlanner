# Release candidate 0.2.0-rc3 — receipt (2026-09-24, superseded)

**Superseded by 0.2.0-rc4.** rc3 was built, gated, frozen and run in the
game, and then its final independent review found real defects (below).
They were fixed for rc4. rc3 must not be installed; this receipt and its
frozen package stay unchanged as history, like rc2 (`ae0181d`) and rc1.

## Identity

| Item | Value |
| --- | --- |
| Commit | `ea2a02793122425a0774f5f8fe6e34aa7ab3e5a4` |
| Version | 0.2.0-rc3 (assembly 0.2.0.0) |
| Package | `3a6e5769fb1ba078308c9880dbc71fdefd6a42ba7238c05dd15f51be4093e1ab` (release ZIP and harness package byte-identical) |
| DLL SHA-256 | `13f6372ee5ee6a20be1fef857e05fd83a5af9b5f721e66746e64c44568e47817` |
| MVID | `f4965315-abcf-4a3b-85bc-7699af58c1b1` |
| Frozen copy | `C:\Dev\KingmakerBuffPlannerLab\runtime-backups\rc-frozen\ea2a02793122425a0774f5f8fe6e34aa7ab3e5a4\` (read-only, `FREEZE.json`) |
| Clean checkout | `C:\Dev\KingmakerBuffPlannerLab\repo\KingmakerBuffPlanner-RC3` |

It was the first candidate whose build did not embed the checkout path:
RC3, RC3-repro and G2 (three checkouts with different names) built the
same package, DLL and MVID.

## Source tests and mutation at the candidate

Full gate at `ea2a027` (Windows PowerShell 5.1, non-interactive, clean
tree): source 42/42, protocol 311/311, runtime harness 36/36, package 4/4,
deployment WhatIf 5/5, launcher WhatIf 12/12, fixture inventory 3/3,
Restore-InstallLocal 16/16, guarded publisher 3/3. Every guard added
since rc2 had a mutant the tests caught (review round 32 C# and 12
PowerShell; re-review 23 C# and 12 PowerShell).

## Game runs on the frozen build

All on the Automation WORKING fixture (campaign `df33d1ff…`, party
Hedwirg, Linzi, Tartuccio), each run restored the Mods folder byte-exact
and compared every save clean.

| Run | Result | What it showed |
| --- | --- | --- |
| `classic-select-20260924-rc3-inst-01` | PASS | Classic Long plan authored with the Classic screen's controls: one step (Linzi casts Resistance on Hedwirg), digest `ec085180…`, no grant used, no Classic routine run |
| `classic-cast-20260924-rc3-inst-01` | PASS | That exact plan cast once through the HUD routine entry under a single-use grant, Instant mode: EffectConfirmed (new instance), at-will count -1 before and after, finite pools unchanged (Linzi level 1: 2>2, Tartuccio level 1: 5>5) |
| `classic-select-20260924-rc3-anim-01` | PASS | The same plan in Animated mode |
| `classic-cast-20260924-rc3-anim-01` | PASS | The same cast in Animated mode (native command), same observations |
| `casting-qual-select-20260924-rc3-01` | PASS | Zero-cost casting-first selection: three castings (Linzi, Tartuccio), forecast projections recorded |
| `casting-qual-cast-20260924-rc3-anim-01` | PASS | Animated: stop, complete, repeat, recast, held disable, recover; 8 planned submissions within the budget of 8; zero violations |
| `casting-qual-cast-20260924-rc3-inst-01` | PASS | The same in Instant mode (disable before the first cast, as labelled) |
| `casting-ws-import-20260924-rc3-01` | PASS | First-open import (seeded, at rc3, with a schema-5 file) |
| `casting-ws-reload-20260924-rc3-01` | FAIL (`workspace-visual-validation`) | Every frame black (30 recaptures). The owner's RDP session was disconnected. Session logs show this is the cause: the s3 reload (black) also ran while disconnected, and the rc1 and rc2 reloads (both passing) ran while connected. Restoration and saves were clean. Not a product defect; not retried. |
| manual rehearsal | not run | The chain stopped at the reload. |

## Why rc3 was superseded

Three independent read-only reviews of `fd0e6dc..ea2a027` found:
- **A1 (high, Classic):** a Classic routine run while the game was paused,
  or from the open Classic screen, expired its casts' frame-counted
  confirmation windows, and the new halting runner then abandoned the
  rest of the routine.
- **B1 (high):** the first-open import refused every Classic plan saved by
  the released 0.0.19 (schema 4). The rc3 import run passed only because
  its seed was written in rc3's own schema 5.
- **B2 (high, usability):** an imported casting whose old plan let the
  planner pick any caster could never become Ready in place. The focused
  editor had no caster, source, routine, order or recast controls.
- Medium: empty recipient sets confirmed vacuously (A2); a cast was
  confirmed by the effect's presence rather than by an instance this
  cast applied (A3); multi-provider casters had no source picker (B3);
  load and save failures were never shown (B4); the harness dropped a
  protected-save violation when the run also failed (C1), kept the
  protected-save baseline only in memory (C2), could abandon a live run
  inside the host's own limits (C3), and could not write a
  finite-resource allowance (C4).
- Low: B5 to B7 and C5 to C8.

Every finding and its fix is listed in
`docs/CASTING-FIRST-REVIEW-INDEX.md`. The rc4 receipt
(`docs/evidence/rc-0.2.0-rc4-receipt.md`) supersedes this one.
