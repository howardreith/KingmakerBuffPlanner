# Native Linux Kingmaker 2.1.5d trial

## Scope and evidence

Branch: `codex/kingmaker-buff-planner/linux-2.1.5d`, based on
`2bc6cc19e3e4a1c5feb1c8cd511d48dfb568606e`. The isolated manifest
commit is `bc00dbd`. Published 0.6.0 remains unchanged. UMM `Id`, minimum
manager version `0.28.2`, assembly name, entry point, requirements, and
external `UserSettings` profile identity are unchanged. The only product
behavior change is UMM's minimum numeric game version: `2.1.7` to `2.1.5`.
UMM discards the letter suffix when detecting a game version; the lab's
Windows log reports `2.1.7b` as `2.1.7`. This metadata admits the requested
`2.1.5d` build through that gate; it does not establish API or gameplay
compatibility.

The available lab game is **Windows Kingmaker 2.1.7b**, with UMM **0.33.0.0**
(`Kingmaker_Data/Managed/UnityModManager/Log.txt`, lines 2-12). No native
Linux 2.1.5d installation or exact managed assemblies were found in the
lab's reference directories. WSL distro enumeration was denied. No Linux
game launch, profile edit, casting run, or Windows game regression run was
performed for these candidates. Native Linux acceptance is **NOT RUN /
unverified**. The available Windows game assemblies remain the build
references for the versioned package.

The controller adapters use exact 2.1.7b native UI contracts. In particular,
the shared root calls controller activation and tick methods even for mouse
use. Whether those types and methods exist in Linux 2.1.5d is unknown.
No controller isolation or casting code was changed without a reproduced
failure. The first Linux exception, if any, should drive a focused repair.

## Packages

| Purpose | File | Identity |
| --- | --- | --- |
| First compatibility trial | `artifacts/linux-2.1.5d/KingmakerBuffPlanner-0.6.0-linux-2.1.5d-manifest-only-diagnostic.zip` | SHA256 `f7386a8408ce8592414100056a90832e4b6cf1451517757df55b96644240da07` |
| Separately versioned review candidate | `artifacts/release/0.6.1/KingmakerBuffPlanner-0.6.1.zip` | Recorded after build below |

The diagnostic ZIP is the published 0.6.0 ZIP with only `Info.json` replaced.
Its DLL SHA256 is the unchanged published
`240aeaa8c699a4ca2f1c651943619233815256dbbab97522c8edfe9bd8125da2`
(MVID `a6339ffd-16b2-4126-a7e3-43eda0fd32d4`). The other four packaged
files were byte compared to the published ZIP. The original release ZIP remains
at `artifacts/release060-download/KingmakerBuffPlanner-0.6.0.zip`, SHA256
`54a7b98535e2127e09200fc7ea330de7bfc7569e05834ab3e210066619921dfa`.
The versioned candidate rebuild changes DLL version and source-revision
stamps; no C# planner code changed.

## Manual native Linux trial

1. On an actual native Linux **2.1.5d** installation, record the game version,
   Linux distribution, UMM version, and installation path. Back up the entire
   existing `Mods/KingmakerBuffPlanner` directory, including `UserSettings`,
   outside `Mods`. Use a disposable campaign, not a personal save. Install the
   exact **manifest-only diagnostic ZIP** above through UMM and confirm its
   checksum before launching. Keep the public release ZIP separate.
2. Load the disposable campaign. Confirm UMM loads and enables the mod without
   relevant exceptions. Open and close the planner through the HUD gear,
   spellbook **BUFF PLANNER** button, and Ctrl+Shift+B. Confirm controls render,
   click and keyboard input work, and the mouse planner stays open normally.
3. With native buffs, create a small plan with a named caster, exact source,
   and recipient; edit it, close/reopen, then quit and restart the game and
   reopen. Confirm the saved choice and recipient persist. Record the plan and
   `UserSettings` file identities without sharing personal save data.
4. On the disposable campaign, run one Instant and one Animated native buff
   cast. Record the expected and observed recipient, applied buff, spell slot
   or resource cost, and game log result. Check already-active handling and
   Stop where applicable. Confirm ordinary mouse use produces no recurring
   exception or unexpected workspace closure.
5. If claiming controller support on this build, also check the native menu
   card and spellbook footer, input/focus, open/close, and a no-cast edit. No
   Linux controller support is claimed by this candidate yet.

If anything fails, stop before further casts and provide the **first relevant
exception with its full stack trace**, the UMM `Log.txt` under
`Kingmaker_Data/Managed/UnityModManager`, and the Unity `Player.log` if
present. Include the exact candidate checksum and the point of failure
(load, enable, mouse entry, edit/save, restart, Instant, Animated, or Stop).
Check for a platform-specific log location rather than assuming the Windows
path layout. Do not use Windows via Proton as native Linux evidence.

After the diagnostic succeeds, repeat a small load/open/save/cast smoke test
on the exact 0.6.1 ZIP before calling that final package compatible. The
versioned DLL is a different binary even though planner code is unchanged.

## Focused checkpoint

At manifest commit `bc00dbd`, strict package validation on the diagnostic
reported **PASS=6 FAIL=0**. Archive comparison reported **5 entries checked,
0 unexpected differences**; only `Info.json` differs, and the DLL hash and
MVID match the published 0.6.0 release. No C# tests or product rebuild were
selected for this metadata-only diagnostic, per
[`VALIDATION-POLICY.md`](VALIDATION-POLICY.md). No theory about missing Linux
controller APIs has been accepted as a defect. Exact next action: build and
validate the 0.6.1 review package, then run the manual native Linux trial
above when that environment becomes available.
