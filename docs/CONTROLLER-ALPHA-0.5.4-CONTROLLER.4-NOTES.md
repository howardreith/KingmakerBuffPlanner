# Controller Alpha 4 — 0.5.4-controller.4

Owner-authorized experimental test release, **not native-qualified or controller-
support complete**. This build has not been run in the game. Stable0.5.3 stays
Latest; PR #10 stays draft. The owner requested this alpha before guarded checks.

## What changed

- A real native campaign-menu Buff Planner card, intended to open the actual
  planner directly; existing mapped X/Func01 quick access remains.
- Semantic controller focus over the existing authoring controls: routine,
  catalogue/filter, exact caster/source, recipient, casting inspection/options,
  order/removal/Undo, policy, Resources and recovery. Search has an owned
  controller character grid rather than requiring physical typing.
- Workspace text initialization and controller-owned material lifetime repair,
  including initially inactive panels. The owner's spellbook-origin rendering
  problem informed these changes; actual pixels remain unverified.
- Shared spellbook/editor/run handoffs with neutral-edge protection. Run releases
  editor ownership and requires a separate focused confirmation in quick access.
  Existing canonical discovery, autosave, admission, execution and Stop remain.
- Preserves Alpha3's Escape cancellation-order and overlapping report-scroll fixes.

These are implemented source paths, not demonstrated controller usability. All
new F01-F10 native acceptance items remain NOT_RUN, including cold controller
entry, native card selection, rendered text, authoring/search and effects/Stop.

## Manual testing — no casting first

Back up saves and the complete Buff Planner folder/settings first. Close the
game and install **KingmakerBuffPlanner-0.5.4-controller.4.zip** through Unity Mod
Manager. Do not install GitHub's Source code archive. Test a disposable campaign;
do not edit saves or remove unrelated mods to make it load. No accompanying
Gunslinger/CotW update is required by this standalone UI build.

Cold controller session -> hold RT/R2 -> select Buff Planner card -> readable
planner -> choose routine -> buff -> exact caster/source -> recipient -> observe
canonical autosave -> close/reopen and verify the same plan. Also try the native
spellbook entry and retained menu shortcut. The principal path should not require
mouse/keyboard; optional pointer use is not controller-only acceptance.

On the recorded mapping: D-pad/left stick moves focus; Left/Right changes regions;
LB/RB chooses routines; Confirm selects the focused action; X/Func01 inspects;
right stick scrolls; Back cancels one inner layer. Read the mapped glyphs for your
device. Search opens the character grid with Space/Delete/Clear/Apply/Cancel.
Classic, if retained, requires explicit review and consent before switching.

Please capture initial and settled workspace screens at1920x1200, then a compact
supported size, from BOTH menu and spellbook origins. Include long-name rows,
inspector/footer, descriptions/Resources, keyboard cancel and long results. Report
controller model/mapping, exact entry route, game/mod versions and complete log.
If rendering/input fails, stop and report it rather than relying on invisible
controls. Only after no-cast editing/return works, test actual buffs/resources and
Stop in a disposable plan. This release does not establish those outcomes.

## Exact artifact and evidence

Product: `69a48a4d4c6484abf84549ab8bb062195cedb380`, not later documentation HEAD.
Version/CLR: `0.5.4-controller.4` / `0.5.4.0`.

ZIP SHA-256: `b307b18950f9254b4df68b13a7aa028abf0d012fa3352d347132c3de6b3b0f58`

DLL SHA-256: `3d2199e8b736bbc63034d15d571a8cddf0788e884512e631b1213cf145b8fe2d`

MVID: `ad20a159-f896-4316-8de2-f3935a848851`

Frozen focused64/0 (7.3s), source47/build1/test compile PASS, two identical builds
and package validation6/0. Tests exercise production algorithms/services and
assembly contracts with native/device substitutions, not physical input/pixels.
This artifact-only alpha reuses those unchanged validated bytes. No broad
historical source/purity/game/release campaign is claimed for publication.

Previous alphas/tags/archives remain untouched. No main merge, permanent lab
installation, game launch or save modification accompanies publication. Guarded
controller-specific loader/observer support and native acceptance remain pending.
