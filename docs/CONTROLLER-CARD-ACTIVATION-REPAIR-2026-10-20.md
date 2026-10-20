# Controller card activation repair — 2026-10-20

## PR #12 review correction

Product commit `69f114caae5dd80d410e7dfdebd5d0b810628c6c`, version `0.6.2`,
restores the active-run route. A card request is now admitted while the casting
host runs. After the native menu releases its layer and controls are neutral,
the destination checks the current running state: it opens quick controls with
Stop for an active run, or begins authoring otherwise. Authoring remains blocked
during execution. The terminal card diagnostic distinguishes
`destination-opened:editor`, `destination-opened:running-controls`,
`destination-opened:classic-review`, `destination-opened:editor-recovery`, and
`handoff-canceled:destination-unavailable`.

The new detached regression starts an animated two-cast routine, keeps its first
cast in flight while the original controls are abandoned, completes the native
release/neutral handoff, reopens quick controls through production destination
routing, selects Stop, and verifies cancellation before the second cast. It also
checks the distinct editor, Classic review, recovery, and unavailable outcomes.
The native Back UI and actual RT/R2 menu are not driven by this detached test.

At that committed product, `powershell -NoProfile -ExecutionPolicy Bypass -File
scripts/Test-Development.ps1 -Only 'controller-'` completed in 6.2 seconds:
source validation `47/0`, production build `1/0`, controller tests `37/0`.
The built DLL SHA-256 was
`5E8CB0D5A428D6F481E87E22D1FFE0A8FCC3DEB14D6085433638CB78A1DDE4AD`.
`git diff --check` passed. No live game, installation, Mods staging, save edit,
package, or release was performed. F02 remains **NOT_RUN**. The exact next
action remains the guarded, no-cast native card → editor → Back → repeat
observation below, with an added active-run card → quick controls → Stop case
once the basic entry route is observed.

## Scope and source identity

- Branch: `codex/kingmaker-buff-planner-controller-card-activation`.
- Product commit: `86414032510d5302326dadbb9800b83522ded43a`, based on `474d944c8e2b93343f5b9b0fa52469f5b5dcecea`.
- Source version: `0.6.2`. No release, push, install, game launch, save edit, or Mods staging was performed.

## Observations and rejected theory

The machine-local `GamePath.props` identifies the Windows Steam installation under
`C:\Program Files (x86)\Steam\steamapps\common\Pathfinder Kingmaker`. Its
`Assembly-CSharp.dll` has MVID `07fa1e4d-8618-41b3-9b8d-faa17d3b26f7` and
SHA-256 `3B6450FFEC440E296E586F71C711B195AED144B28D53E1CBB29406D18FEF5AFB`.
This is the repository's pinned 2.1.7b managed contract. The local installed
`Mods\KingmakerBuffPlanner\Info.json` instead reports version `0.1.1-rc3` and
game version `2.1.7`; its DLL SHA-256 is
`78407DD4C7240CE25C7654715854E83061DA744A18D8EB4A2198FDFFEC5E3080`.
The available `output_log.txt` was last written October 17 and has no current
controller-card activation attempt. The owner's installed copy, platform, and
exact failure stage therefore remain unestablished by this lab.

Detached inspection of that exact native assembly establishes:

- `GamePad.CurrentInputLayer` returns `BaseLayer` when `m_Layers` is empty.
  The proposed null-versus-base mismatch is **not** the cause on this build;
  the neighboring editor mode-release comparison uses the matching raw owner.
- `BaseGameMenuView.Bind` binds entities, then gets and pushes its input layer.
  `DestroyViewImplementation` pops that layer before deactivating the menu root.
- Native Confirm calls the parent menu VM's close action. During parent VM
  disposal, the first selected entity starts its action. The owned card view's
  verified serialized fields are `m_Rect` and `m_Title`.

Source review identified a real admission gap: the card's layer observation
previously depended on the card already existing and on the separate Func01
entry's Default-mode gate. A selected callback could then pass a missing capture
to a silently refusing request. Repeated layer observations could also make the
source its own predecessor. The handoff's context, source, and timeout exits had
no terminal reason. Which of these occurred in the owner's A press is still
unknown without a current native trace.

## Repair

The card now retains a menu-owned layer observation before or after construction,
uses the actual stack predecessor when its source is already pushed, and clears
stale observations on rebind, disable, replacement, and destruction. Layer capture
runs independently of the optional Func01 entry gate. The selected callback
reports its attempt, checks capture identity, and catches request failures without
interrupting native disposal. It passes the root canvas resolved during card
construction, avoiding a new canvas lookup after native hide.

The request admits only its exact source or recorded predecessor and refuses a
foreign owner. It records menu generation, layer identities, stack membership,
mode, and native visibility. The neutral-gated handoff reports source release and
one terminal outcome: editor opened, specific cancellation, or timeout. It checks
for the actual casting workspace before reporting success. It does not synthesize
another native Confirm or open over a different native window.

## Focused validation and limit

At product commit `8641403`, the command
`powershell -NoProfile -ExecutionPolicy Bypass -File scripts/Test-Development.ps1 -Only 'controller-'`
completed in 6.5 seconds: source validation `47/0`, production build `1/0`,
controller protocol tests `36/0`. The built DLL SHA-256 was
`AB35DA10AAB3104E8C19D5896788AAF803F6C58650258DDDB03462587FE3B3C3`.
The tests cover empty-stack native getter behavior, menu bind/release ordering,
capture before/after construction, pushed/repeated observations, rebind cleanup,
neutral/held input, one opening, unavailable destination, takeover, context loss,
source loss, cancellation, and timeout. `git diff --check` passed.

These are compilation, detached native-contract, and offline behavior results.
F02 and the other live controller acceptance cases remain **NOT_RUN**. No claim
is made that the owner's installed copy is fixed.

## Exact next action

Use the repository-owned guarded runtime workflow only after its current source,
launcher-safety, `-WhatIf` purity, package, installation, save, and transaction
requirements pass. Stage this exact product through that workflow and observe:
cold controller mode → RT/R2 campaign menu → focus Buff Planner → mapped A →
release controls → real editor visible and usable → Back → gameplay → repeat.
First record highlight/Back, held/repeated A, another native card, the first and
settled visible frames, and the `[KBP-CONTROLLER-CARD]` stage sequence. Do no
casting or plan edits in this observation. Stop at the first failed stage and
use its recorded reason for the next correction. No public release is authorized
by this checkpoint.
