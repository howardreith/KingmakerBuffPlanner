# Manual Acceptance

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
