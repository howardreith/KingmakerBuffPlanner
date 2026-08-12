# Manual Acceptance

## 0.0.4 installed handoff

Status: automated live-bootstrap acceptance PASS twice; 0.0.4 is guarded-installed for optional human visual confirmation.

Installed identity: source `5b96f3b4e713489ce677db3ac5acb83a10f80f01`; package `cb3799e799f641b1a9f7d79eb71942025b5df71a8de956e17369b24fe2f14d16`; DLL `6f72c38ef7e445121291ff2f17f207d49210ea30a2e07fe1105595133b706f1c`; MVID `305a8a6c-2b49-4e3b-a365-286638cbfafa`.

Runs `bootstrap-0.0.4-human-live-6` and `bootstrap-0.0.4-human-live-7` already prove, in distinct fresh processes, the exact four-button row, physical F10, visible/opaque planner, clean close/input restoration, no duplicates after 21 total cycles, and no click-through/world input. A human check should now focus on appearance and ordinary use, not re-prove bootstrap existence:

- load the same campaign and confirm one readable Setup/Long/Important/Short row above the native lower-left cluster;
- confirm Setup and F10 show the opaque planner and Escape/F10/close return cleanly to gameplay;
- confirm no visible overlap or unexpected native activation;
- report any visual scaling or content issue with the installed identity above.

Do not treat older 0.0.3 instructions or results as current acceptance evidence.

Status: 0.0.3 IS INSTALLED; REQUIRED FOR R2 VERDICT. The installed 0.0.2 verdict is FAIL.

Installed identity: DLL SHA-256 `5d95368ee237e658e06b4948209f805568a417ea150eb36c3023df9b155f0950`, MVID `f3f691a4-d691-4112-90a4-7beb9f06aad2`. The preserved profile contains Long → Bless and one target; no provider preference was stored, so the result must identify the provider selected from the live party.

With validated 0.0.3 installed, load the same campaign and verify:

- one horizontal row appears directly above the native bottom-left cluster in exact order: Setup, Long, Important, Short;
- no planner icon or tooltip overlaps a native control/tooltip region;
- clicking any planner icon never activates turn-based mode, pause/hourglass, a world command, selection, ability, camera drag, or camera zoom;
- Setup and F10 both open the same visible, opaque, full-screen `BUFF PLANNER` root;
- F10 never hides the HUD or locks gameplay unless that visible root was constructed and validated first;
- Escape, close, and F10 restore the prior gameplay/HUD/input state;
- Long visibly resolves the preserved Bless assignment, reports the chosen caster/spellbook/target and queued/submitted/started state, and only reports success if `BlessBuff` is confirmed;
- if Bless cannot apply, the visible result names the exact validation, submission, execution, resource, or unconfirmed-effect failure;
- Important and Short visibly report their configured/empty state;
- 20 close/reopen cycles leave one HUD row, no hidden modal root, and no retained input lock.

The historical 0.0.2 checklist below is superseded by this R2 checklist.

Status: REQUIRED FOR 0.0.2 UI VERDICT; save-backed execution remains deferred pending an authorized `KBP_AUTOMATION_WORKING` fixture

Human playtesting is authoritative for the visual and interaction repair. With 0.0.2 installed, load a campaign and verify:

- the lower-left HUD has one native-looking setup icon and adjacent Long, Important, and Short icons;
- no floating `Buff Planner (F10)`/routine text strip remains;
- the setup icon opens a distinct, fully opaque full-screen `BUFF PLANNER` window;
- the world is not readable or interactive behind it;
- empty-background and control clicks never move a character, change world selection, interact, or activate an ability;
- list scrolling does not zoom the world and dragging does not drag the camera;
- Long, Important, and Short group tabs visibly change selection;
- each HUD quick icon visibly reports success, refusal, or its exact unavailable reason;
- an empty Long routine reports `No Long buffs are configured.` instead of doing nothing;
- the close button, Escape, and F10 all close through the same clean lifecycle;
- repeated close/reopen works without duplicate buttons or roots;
- the layout is readable at the actual resolution and target portraits/provider controls are legible;
- tooltips identify setup/F10 and each quick routine clearly.

When an authorized `KBP_AUTOMATION_WORKING` fixture is available, also verify:

- standalone install/load and clean uninstall;
- F10/setup/HUD lifecycle through an area transition;
- search, filters, sorting, hidden and unsupported visibility;
- Long/Important/Short editing, target matrix, provider priority/ban/cap, and bounded clear confirmation;
- caster level, remaining resource, material, rejection, skip/overwrite, and unfulfilled presentation;
- animated execution, instant execution, sticky-touch fallback allowed/blocked, and combat policy;
- pre/post counts against visible effects and exact slots/resources/components;
- profile survival after party reorder and save reload;
- native-only and optional-mod source visibility without duplicate or foreign dependencies.

The no-save native-only and exact Call of the Wild load/catalog/Harmony portions above are already automated and passed twice. Manual acceptance remains limited to the save-backed rows requiring an authorized `KBP_AUTOMATION_WORKING` fixture; Tabletop Added Rules is unavailable locally.
