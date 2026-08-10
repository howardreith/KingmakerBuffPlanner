# Kingmaker Buff Planner — Desktop Automation Setup

## Purpose

This guide prepares a Windows desktop to develop a **new standalone Pathfinder: Kingmaker buff-planning mod** while a separate laptop continues work on Tabletop Added Rules / Gunslinger.

The desktop project must be isolated from the laptop project in every important respect:

- separate repository;
- separate Git remote;
- separate Codex home and policy;
- separate lab root, runtime state, backups, evidence, and package outputs;
- separate disposable save names;
- no shared Git worktree;
- no desktop writes to the Tabletop Added Rules / Gunslinger repository.

The proposed provisional identity is:

```text
Product:        Kingmaker Buff Planner
Repository:     KingmakerBuffPlanner
Assembly:       KingmakerBuffPlanner.dll
UMM ID:         KingmakerBuffPlanner
Namespace:      KingmakerBuffPlanner
Lab root:       C:\Dev\KingmakerBuffPlannerLab
Repository:     C:\Dev\KingmakerBuffPlannerLab\repo\KingmakerBuffPlanner
Codex home:     C:\Dev\KingmakerBuffPlannerLab\codex-home
```

These names can be changed later, but changing them after persistence and package publication begins is more expensive. Use them unless a collision is proven.

---

# 1. Important correction about mod-added buffs

A manually maintained list of every buff spell should **not** be the primary architecture.

The preferred design is dynamic structural discovery:

1. inspect the live party's spellbooks and abilities after all mods have loaded;
2. inspect each ability blueprint's action graph;
3. recognize actions that apply beneficial persistent effects;
4. normalize those effects into unit buffs, pet buffs, area buffs, and worn-item enchantments;
5. expose supported abilities automatically;
6. use a small override/adaptor layer only for exceptional mechanics.

This should automatically cover many Call of the Wild and Tabletop Added Rules spells—including Shield Other—provided they use ordinary Kingmaker blueprint actions. Custom action types can be handled by optional reflection adapters or a GUID/type override registry without adding a compile-time dependency on another gameplay mod.

The final architecture therefore has four discovery layers:

```text
1. Generic native action-graph discovery
2. Generic reflection discovery for unknown ActionList wrappers
3. Optional assembly/type adapters, loaded only when present
4. Explicit include/exclude/effect overrides for proven exceptions
```

The native Kingmaker catalog still needs a complete audit. Dynamic discovery reduces manual work; it does not remove the need to prove coverage and correct false positives.

---

# 2. Parallel-machine constraint: Steam and saves

Both Codex agents can edit, build, test pure code, package, and commit simultaneously. **Do not assume the same Steam account can safely run Kingmaker online on both machines at once.**

Use this desktop arrangement:

1. Install and fully update Kingmaker while Steam is online.
2. Launch Kingmaker once and reach the main menu.
3. Disable Steam Cloud specifically for Pathfinder: Kingmaker on the desktop.
4. Verify the game launches in Steam Offline Mode.
5. Leave the desktop Steam client in Offline Mode while autonomous runtime qualification is running.
6. Keep the laptop online if needed for the other project.
7. Never re-enable Cloud until the desktop's disposable saves and local data have been reviewed or removed.

The runtime harness must stop rather than interact with:

- login or Steam Guard prompts;
- purchases;
- update dialogs;
- cloud-conflict dialogs;
- credential prompts;
- Remote Play prompts;
- unexpected account state.

The desktop uses only disposable save fixtures named with the `KBP_` prefix. It must never load or overwrite the laptop's protected `KMG_` fixtures or valued campaign saves.

Recommended save names:

```text
KBP_AUTOMATION_BASELINE   immutable source fixture; never written
KBP_AUTOMATION_WORKING    disposable runtime target; may be recopied from baseline
```

---

# 3. Install and verify the base software

## 3.1 Windows

Windows 11 is the preferred Codex Windows baseline. A fully updated Windows 10 1809-or-newer system can still work, but native sandbox behavior is less reliable.

Run Windows Update before installing the development stack.

## 3.2 ChatGPT desktop app and Codex

Install the official Windows app with the exact Microsoft Store product ID:

```powershell
winget install --id 9PLM9XGG6VKS -s msstore
```

Do not use a name-only command such as `winget install Codex`; exact product identity matters.

After installation:

1. Sign in normally with the ChatGPT account that has Codex access.
2. In Settings, select the **Windows-native** agent rather than WSL.
3. Select **PowerShell** as the integrated terminal.
4. Use **Ask for approval**, not Full Access.
5. Complete the elevated Windows sandbox setup when prompted.
6. Restart the app after configuration changes.

The CLI is optional but useful for policy checks. If `codex` is not already available after app installation, install the official standalone CLI from an ordinary PowerShell terminal, outside the Codex sandbox:

```powershell
powershell -ExecutionPolicy Bypass -c "irm https://chatgpt.com/codex/install.ps1 | iex"
```

Then verify:

```powershell
codex --version
```

Do not copy Codex authentication files or sandbox secrets from the laptop. Sign in normally on the desktop.

## 3.3 Git and GitHub CLI

Install and verify:

```powershell
winget install --id Git.Git -e
winget install --id GitHub.cli -e

git --version
gh --version
gh auth login
gh auth status
```

Use the same Git author name/email as the laptop if desired, but do not copy credential stores manually.

Suggested global Git settings:

```powershell
git config --global core.autocrlf true
git config --global fetch.prune true
git config --global pull.ff only
git config --global init.defaultBranch main
```

## 3.4 Visual Studio Build Tools and .NET Framework

Install Visual Studio 2022 Build Tools or full Visual Studio 2022 with:

- MSBuild;
- .NET desktop build tools;
- NuGet build support;
- C# compiler;
- optional C++ build tools if the Codex native extension or sandbox reports a missing runtime dependency.

Install the **.NET Framework 4.7 Developer Pack**. A newer .NET SDK or the .NET Framework 4.8 Developer Pack does not replace the reference assemblies for an exact `net47` build.

Verify the targeting pack exists:

```powershell
Test-Path 'C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\.NETFramework\v4.7'
```

Find MSBuild:

```powershell
$vswhere = 'C:\Program Files (x86)\Microsoft Visual Studio\Installer\vswhere.exe'
& $vswhere -latest -products * -requires Microsoft.Component.MSBuild -find MSBuild\**\Bin\MSBuild.exe
```

## 3.5 PowerShell and supporting tools

Windows PowerShell 5.1 is sufficient for the initial scripts. PowerShell 7 is useful and may be installed separately:

```powershell
winget install --id Microsoft.PowerShell -e
pwsh --version
```

Python is optional for report generation and fixture tooling. Git, PowerShell, MSBuild, and the installed game assemblies are the actual essentials.

## 3.6 Pathfinder: Kingmaker

Install Pathfinder: Kingmaker through Steam on the desktop and pin the development target to the exact locally installed build.

Expected baseline from the existing lab:

```text
Game:       Pathfinder: Kingmaker Enhanced Plus Edition
Version:    2.1.7b
Steam ID:   640820
Framework:  .NET Framework 4.7
Language:   C# 7.3
UMM:        exact known-good 0.32.4 / 0.32.x installation
Harmony:    1.2.0.1, Harmony12 namespace, 0Harmony12.dll
```

Do not assume these identities. The desktop intake script and Codex mission must hash and record the actual installed files.

Typical install path:

```text
C:\Program Files (x86)\Steam\steamapps\common\Pathfinder Kingmaker
```

Launch the unmodded game once and close it.

## 3.7 Unity Mod Manager

Use the exact known-good Unity Mod Manager installer/archive already used by the laptop. Copy that installer package to the desktop rather than silently adopting a different release.

Install UMM for Kingmaker and verify these installed references exist:

```text
<Kingmaker>\Kingmaker_Data\Managed\UnityModManager\UnityModManager.dll
<Kingmaker>\Kingmaker_Data\Managed\UnityModManager\0Harmony12.dll
```

Record SHA-256 hashes of both. Do not place these DLLs in Git or the release ZIP.

---

# 4. Create the isolated desktop lab

Extract the supplied launch bundle somewhere temporary and run:

```powershell
Set-ExecutionPolicy -Scope Process Bypass
.\Initialize-KingmakerBuffPlannerDesktop.ps1
```

The script only creates folders and records an environment intake. It does not install software, edit saves, deploy mods, launch Steam, or alter the game.

Expected layout:

```text
C:\Dev\KingmakerBuffPlannerLab\
  codex-home\
    AGENTS.md
    config.toml
    rules\
      default.rules
  codex-policy\
  examples\
  harness-reference\
  incoming\
  reference-source\
  repo\
    KingmakerBuffPlanner\
  runtime-backups\
  runtime-evidence\
  runtime-state\
  runtime-staging\
  environment-intake.json
```

Set an isolated Codex home before opening the app:

```powershell
$env:CODEX_HOME = 'C:\Dev\KingmakerBuffPlannerLab\codex-home'
setx CODEX_HOME 'C:\Dev\KingmakerBuffPlannerLab\codex-home'
```

Close and reopen ChatGPT after `setx`.

Activate the supplied templates:

```powershell
Copy-Item .\codex-config.template.toml `
  C:\Dev\KingmakerBuffPlannerLab\codex-home\config.toml

Copy-Item .\CODEX-HOME-AGENTS.template.md `
  C:\Dev\KingmakerBuffPlannerLab\codex-home\AGENTS.md

New-Item -ItemType Directory -Force `
  C:\Dev\KingmakerBuffPlannerLab\codex-home\rules | Out-Null

Copy-Item .\codex-rules.template.rules `
  C:\Dev\KingmakerBuffPlannerLab\codex-home\rules\default.rules
```

The supplied config deliberately uses:

```text
approval_policy = on-request
approvals_reviewer = auto_review
sandbox_mode = workspace-write
Windows sandbox = elevated
```

This matches the successful laptop pattern: the coding agent remains sandboxed while an automatic reviewer evaluates exact deployment, Steam-launch, and push requests. It does **not** grant Full Access.

The repository is writable as the active workspace. The supplied config adds only the project-owned policy, runtime-state, staging, evidence, backup, artifact, and log directories as extra writable roots. `reference-source`, `examples`, `harness-reference`, `incoming`, the game directory, and the save directory are not general writable roots. Game deployment and Steam launching must occur through exact guarded scripts and approval rules.

---

# 5. Create the new Git repository

Create a new private GitHub repository named `KingmakerBuffPlanner`. Do not fork or clone the Gunslinger repository.

From the desktop, replace `<YOUR_GITHUB_ACCOUNT>` and `<BUNDLE_PATH>`, then run:

```powershell
Set-Location C:\Dev\KingmakerBuffPlannerLab\repo

gh repo create <YOUR_GITHUB_ACCOUNT>/KingmakerBuffPlanner `
  --private `
  --clone `
  --description "Standalone buff planning and execution mod for Pathfinder: Kingmaker"

Set-Location .\KingmakerBuffPlanner
Copy-Item <BUNDLE_PATH>\PROJECT-AGENTS.template.md .\AGENTS.md
Copy-Item <BUNDLE_PATH>\CODEX-KINGMAKER-BUFF-PLANNER-AUTONOMOUS-MISSION.md `
  C:\Dev\KingmakerBuffPlannerLab\incoming\

git add AGENTS.md
git commit -m "chore: establish standalone buff planner repository guidance"
git push -u origin main
```

If the private repository was already created through the GitHub website, use this instead of `gh repo create`:

```powershell
gh repo clone <YOUR_GITHUB_ACCOUNT>/KingmakerBuffPlanner `
  C:\Dev\KingmakerBuffPlannerLab\repo\KingmakerBuffPlanner
```

The autonomous mission will create its own dedicated branch, normally:

```text
codex/kingmaker-buff-planner
```

Do not point the desktop Codex project at the Tabletop Added Rules or Gunslinger repository.

---

# 6. Materials to copy from the laptop

Copy these as **read-only reference material**, not as a shared worktree.

## 6.1 Existing runtime-harness patterns

From the laptop's Kingmaker project, copy into:

```text
C:\Dev\KingmakerBuffPlannerLab\harness-reference\
```

The bundle includes a safe export helper. Run it on the laptop from the extracted bundle directory:

```powershell
Set-ExecutionPolicy -Scope Process Bypass
.\Export-KingmakerAutomationReferenceFromLaptop.ps1
```

By default it reads:

```text
C:\Dev\KingmakerGunslingerLab\repo\KingmakerGunslinger
```

and creates, without overwriting an existing file:

```text
%USERPROFILE%\Desktop\KingmakerAutomationReference.zip
```

The archive records the source branch, commit, dirty state, missing optional files, and SHA-256 inventory. It contains selected copies of:

```text
docs\WIN10-AUTONOMOUS-RUNTIME-TESTING.md
docs\WORKING-SAVE-SMOKE.md
scripts\Invoke-KingmakerRuntimeTest.ps1
scripts\RuntimeAutomation.Common.ps1
scripts\RuntimeHarness.Common.ps1
scripts\Test-RuntimeRequest.ps1
scripts\Test-RuntimeResult.ps1
src\KingmakerGunslinger\RuntimeTesting\   (entire folder)
scripts\compatibility\                    (entire folder, if present)
compatibility\                             (schemas/manifests only)
codex-policy\Push-KingmakerGunslinger.ps1 (reference pattern only)
```

Transfer that ZIP to the desktop and extract its inner `KingmakerAutomationReference` directory into:

```text
C:\Dev\KingmakerBuffPlannerLab\harness-reference\
```

The desktop Codex must adapt and rename these patterns. It must never execute the Gunslinger push helper or deploy a Gunslinger build from this reference directory.

## 6.2 Open-source buff-mod references

Copy or clone pinned source snapshots into:

```text
C:\Dev\KingmakerBuffPlannerLab\reference-source\
```

Recommended sources:

```text
BubbleBuffs\             factubsio/BubbleBuffs
PathfinderAutoBuff\      ilkar399/PathfinderAutoBuff
BuffIt2TheLimit\         Gh05d/wrath-epic-buffing
KingmakerRebalance\      Holic75/KingmakerRebalance / Call of the Wild source
```

For each source, preserve:

- exact commit SHA;
- license file;
- repository identity;
- acquisition date;
- a SHA-256 inventory of relevant files.

These are reference authorities. Do not modify them in place. Do not ship their source, binaries, or UI assets in the new mod package unless the MIT notice and substantial-copy obligations are correctly satisfied.

Do not copy code or assets from Kingmaker Buff Bot / the Nexus mod with restrictive permissions.

## 6.3 Optional-mod compatibility fixtures

Copy exact loadable packages into immediate child directories under:

```text
C:\Dev\KingmakerBuffPlannerLab\examples\
```

Recommended entries:

```text
CallOfTheWild\
TabletopAddedRules\
```

For Tabletop Added Rules, copy a **built package or immutable source snapshot**, not the laptop's active worktree. The desktop may inspect it and test against it but may never modify or push it.

If Shield Other is not yet present in the copied snapshot, the buff-planner mission should still implement generic discovery and mark the exact profile `UNAVAILABLE-LOCAL-REFERENCE` or `FEATURE-NOT-PRESENT-IN-SNAPSHOT`. That is not a reason to stop the native mod.

## 6.4 Exact UMM package and environment notes

Copy:

- the exact UMM installer/archive used on the laptop;
- laptop hashes for `UnityModManager.dll` and `0Harmony12.dll`;
- the laptop's technical baseline document;
- any exact Kingmaker assembly hash inventory.

The desktop must compare rather than blindly assume equality.

## 6.5 Disposable save fixture

Copy only a disposable automation save. Do not copy valued campaigns unless separately backed up and deliberately authorized.

A practical route is:

1. duplicate the laptop's known-safe automation save archive;
2. import it on the desktop with Cloud disabled;
3. rename the in-game saves to `KBP_AUTOMATION_BASELINE` and `KBP_AUTOMATION_WORKING`;
4. preserve an offline byte-for-byte backup of baseline outside the live save directory;
5. mark baseline immutable by policy;
6. permit runtime tests to load only working.

If no suitable save exists, create a small dedicated test campaign manually before launching Codex. The mission can develop and build without it, but final runtime qualification cannot be truthful without controlled fixtures.

---

# 7. Materials not to copy

Do **not** copy any of the following from the laptop:

- the active Tabletop Added Rules / Gunslinger Git worktree;
- `.git` internals from that worktree;
- unresolved Git lock files;
- Codex `auth.json`, sandbox secrets, session databases, or encrypted credentials;
- the laptop's live `Mods` directory as a development repository;
- `bin`, `obj`, `.vs`, NuGet caches, runtime evidence, or stale build artifacts;
- an unedited `GamePath.props` that points at the laptop's install;
- protected saves or the immutable laptop baseline;
- Steam credential files;
- third-party binaries inside the new repository or release package.

The desktop should obtain its own local game references from its own legally installed Kingmaker files.

---

# 8. Game reference inventory

The new project should reference the desktop's installed copies, with `Private=false` / Copy Local disabled, of the exact assemblies it truly needs. The starting inventory should inspect at least:

```text
Kingmaker_Data\Managed\Assembly-CSharp.dll
Kingmaker_Data\Managed\Assembly-CSharp-firstpass.dll
Kingmaker_Data\Managed\Newtonsoft.Json.dll
Kingmaker_Data\Managed\UnityEngine.dll
Kingmaker_Data\Managed\UnityEngine.CoreModule.dll
Kingmaker_Data\Managed\UnityEngine.AnimationModule.dll
Kingmaker_Data\Managed\UnityEngine.AssetBundleModule.dll
Kingmaker_Data\Managed\UnityEngine.UI.dll
Kingmaker_Data\Managed\UnityModManager\UnityModManager.dll
Kingmaker_Data\Managed\UnityModManager\0Harmony12.dll
```

Codex must add other Unity modules only when source usage proves the reference is required.

Use an ignored local `GamePath.props`, for example:

```xml
<Project xmlns="http://schemas.microsoft.com/developer/msbuild/2003">
  <PropertyGroup>
    <KingmakerInstallDir>C:\Program Files (x86)\Steam\steamapps\common\Pathfinder Kingmaker</KingmakerInstallDir>
  </PropertyGroup>
</Project>
```

Never commit this local path file.

---

# 9. Environment fingerprint

Before production work, the repository must generate and retain an ignored local environment fingerprint containing:

- Windows edition/build;
- PowerShell version;
- Git version;
- MSBuild path/version;
- .NET Framework 4.7 reference path;
- Steam storefront and app ID;
- Kingmaker displayed version and executable file version;
- SHA-256 of `Kingmaker.exe`;
- SHA-256 of every referenced managed DLL;
- UMM version and hashes;
- Harmony version/hash;
- enabled live mod identities at intake;
- exact source-reference commits/hashes;
- exact optional-mod package hashes.

The repository may commit the schema and a redacted example, but machine-specific paths and third-party payloads remain ignored.

---

# 10. Codex policy validation

After activating the templates and reopening the app, verify the project uses the intended guidance:

```powershell
$env:CODEX_HOME
codex --version
codex --cd C:\Dev\KingmakerBuffPlannerLab\repo\KingmakerBuffPlanner `
  --ask-for-approval never `
  "List the instruction files you loaded and summarize the active safety boundaries. Do not modify files."
```

If the CLI is unavailable, open the repository as a project in the desktop app and ask the same read-only question.

If `codex execpolicy` is available, test the supplied rules:

```powershell
codex execpolicy check --pretty `
  --rules C:\Dev\KingmakerBuffPlannerLab\codex-home\rules\default.rules `
  -- git reset --hard HEAD

codex execpolicy check --pretty `
  --rules C:\Dev\KingmakerBuffPlannerLab\codex-home\rules\default.rules `
  -- powershell.exe -NoProfile -ExecutionPolicy Bypass -File `
     C:/Dev/KingmakerBuffPlannerLab/repo/KingmakerBuffPlanner/scripts/Invoke-KingmakerRuntimeTest.ps1 `
     -Scenario mod-load-smoke
```

Expected:

- destructive Git reset is forbidden;
- the exact future runtime harness receives a prompt/auto-review path;
- direct arbitrary `git push` is forbidden in favor of a guarded helper.

---

# 11. Final preflight before giving Codex the mission

Confirm all applicable rows:

```text
[ ] ChatGPT desktop app installed from exact Microsoft Store product ID
[ ] Windows-native elevated sandbox configured
[ ] PowerShell selected
[ ] CODEX_HOME points to the isolated buff-planner codex-home
[ ] config.toml, AGENTS.md, and default.rules active
[ ] Git and gh authenticated
[ ] private KingmakerBuffPlanner remote exists
[ ] repository cloned at the exact lab path
[ ] project AGENTS.md committed
[ ] Visual Studio/MSBuild available
[ ] .NET Framework 4.7 Developer Pack available
[ ] Kingmaker installed and verified as exact local target
[ ] UMM installed from the known-good package
[ ] UMM/Harmony hashes recorded
[ ] Steam Cloud disabled for Kingmaker on desktop
[ ] desktop Steam Offline Mode verified
[ ] disposable KBP save fixtures prepared or explicitly unavailable
[ ] reference-source snapshots copied and read-only
[ ] optional mod packages copied under examples and read-only
[ ] laptop runtime harness copied only as reference
[ ] no Tabletop/Gunslinger active worktree copied
[ ] no secrets or protected saves copied
[ ] Initialize-KingmakerBuffPlannerDesktop.ps1 completed
[ ] environment-intake.json reviewed
```

---

# 12. Launching the autonomous mission

1. Open ChatGPT desktop.
2. Add this project:

```text
C:\Dev\KingmakerBuffPlannerLab\repo\KingmakerBuffPlanner
```

3. Select the strongest available coding model and high or xhigh reasoning where available.
4. Confirm **Ask for approval** is active and Full Access is not.
5. Paste the entire contents of:

```text
CODEX-KINGMAKER-BUFF-PLANNER-AUTONOMOUS-MISSION.md
```

6. Send once.

The mission tells Codex not to ask ordinary implementation questions. It must continue through investigation, implementation, tests, runtime qualification, documentation, commits, and packaging until the definition of done is met or a listed critical hard stop is proven.

---

# 13. What “completed” means

A build that reaches the main menu is not completion.

The autonomous mission is complete only when it has produced a standalone release package that:

- loads under exact Kingmaker 2.1.7b + exact UMM/Harmony;
- inventories and supports the audited native persistent beneficial spell/ability catalog;
- dynamically discovers ordinary mod-added buffs;
- safely allocates prepared, spontaneous, variant, metamagic, domain, cantrip, and resource-bound casts;
- lets the player assign targets, groups, caster preferences, bans, and caps;
- skips or overwrites active effects according to settings;
- persists per-campaign configuration externally and safely;
- has a normal animated executor and a proven instant executor;
- spends slots, resources, and components exactly once;
- proves mass-spell single-cost semantics;
- has guarded runtime evidence from fresh game processes;
- has exact-hash qualification for available Call of the Wild and Tabletop Added Rules snapshots;
- leaves the live Mods directory and protected saves restored;
- includes no third-party payloads or game DLLs;
- has coherent Git history, documentation, and a validated release ZIP.

A missing optional-mod snapshot does not block the native release. A failure to prove the core native planner or resource-correct instant execution does block completion.
