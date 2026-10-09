# Remaining work — 0.4.0 release train (owner-review candidate)

Program handoff for the consolidated 0.4.0 candidate. One concise record;
bulky evidence stays under the lab's `runtime-evidence\<runId>\`.

- Branch: `claude/kbp-complete-remaining-work-2026-10-09` (worktree
  `private\worktrees\KBP040`), created from `origin/main`
  `16f87ed1dc9b00b683ac7192f4ae8d95da7853fc` (WP2A merged, PR #4).
- Public release: `v0.3.0`. No 0.4.0 tag, merge, or release is authorized.
- Shell rule: run every gate/harness/launcher script through Windows
  PowerShell 5.1 as `powershell.exe -NoProfile -NonInteractive -Command
  "& .\scripts\Name.ps1 ..."`. Launched with `-File`, Test-RuntimeHarness
  fails spuriously (its script-scoped `$WhatIfPreference` reaches the 5.1
  `Get-FileHash`, which then returns nothing); the qualified WP2A head
  reproduces the same failure under `-File`.

## Package status

| Package | Implemented | Regressed | Source-qualified | Native-qualified | Owner |
|---|---|---|---|---|---|
| WP2B spellbook entry | yes | yes | focused only | physical PASS (`kbp040-wp2b-spellbook-02`) | pending |
| WP4 execution policy | in progress | | | | pending |
| WP3 direct manipulation | not started | | | | pending |
| WP5 catalogue audit | diagnosis running | | | | pending |
| WP6 Magic Circle | diagnosis running | | | | pending |
| WP7 parchment | not started | | | | pending |

The complete `Test-SourceOnly.ps1` gate runs once on the final integrated
head.

## WP2B — spellbook Buff Planner button

Root cause (from the recorded native UI contract,
`runtime-evidence\beta-0dd82937-adv-01\native-ui-contract.json`, 1920x1080):

1. The owned button was pinned 20/18 units inside the spellbook window's
   top-right corner. `SpellBook/BookBackground` is full screen and the
   service window's top-bar close button `ServiceWindow/Top/Close`
   (143x137, anchored top-right) occupies exactly that area, so a click
   reached the native Close, not the planner.
2. The handoff searched the spellbook subtree for a `Button` named
   `Close`. The only such button is `ServiceWindow/Top/Close`, outside that
   subtree (the spellbook's own "Close" nodes are per-slot containers whose
   buttons are named `Button`), so every click that did arrive was refused
   with `native-close-affordance-missing`.

Repair (`e84a892`): the button is the last child of the service-window root
(drawn and raycast above the top bar) and is placed by
`SpellbookEntryPlacement` — an ordered candidate list checked against the
live native controls. The locator resolves the native close by its exact
path. Every handoff closes the window through it and waits until both the
FullScreenUi mode and the window have released; a failure after closure
disposes any half-open planner and reopens the spellbook through the game's
`IServiceWindowUIHandler.HandleOpenSpellbook`. Strays from an earlier
controller are retired. No second entry route was added.

Guarded scenario (`e29c4ec`, corrected in `11a30db`): `live-workspace-physical
-PhysicalExpectation spellbook`. The host presses the game's own `OpenSpells`
key binding (read at runtime: `B`), clicks the button with the OS pointer and
presses Escape, three times, then repeats once with one simulated opener
refusal (`SpellbookHandoffFaults`, armable only in a locked runtime-test
session). The launcher gained a single unmodified A–Z `key` action.
`Test-SpellbookEntryEvidence.ps1` (26 cases) runs in Test-SourceOnly.

Evidence:

- `kbp040-wp2b-spellbook-01` on `e29c4ec` — FAIL by the check, not the
  product: every cycle handed off correctly, but the record required the
  FullScreenUi mode to be inactive after the click, and the planner's own
  input lease raises that mode on open (it refuses to open while another
  owner holds it). Rejected theory: "FullScreenUi still active means the
  spellbook kept ownership" — the native window was hidden and the
  workspace had opened, which its lease forbids under a foreign owner.
- `kbp040-wp2b-spellbook-02` on `11a30db` — PASS, complete, restoration
  verified, protected saves compared and clean. Package
  `a32ea34b16ac0ae436e2278455092b11c7211627104d37de6e1031be6b97e9c1`, DLL
  `a30c4e9de10bd90c7ca7d4aca70cc95602509da4979982ea16d37e66e6a7cbee`, MVID
  `b470a9d8-20c8-4a28-828b-04255484b3ec`. Each ordinary cycle: one owned
  button, one runtime listener, the first EventSystem hit at its centre is
  the button, placement `below-native-close` conflict free, one native
  release, one native close, one opener call, one workspace open, lease
  held, service window hidden, FullScreenUi owned by the planner, Escape
  closes the planner with the lease released and no native menu. Fault
  cycle: opener refused, no planner, spellbook reopened with its button.
  World input: no movement/ability commands, selection unchanged.
  Screenshots `spellbook-*-open.png` / `spellbook-*-after-click.png`.

Uncertainty: placement was observed at 1920x1080 only; other resolutions are
covered by the WP7 visual runs.
