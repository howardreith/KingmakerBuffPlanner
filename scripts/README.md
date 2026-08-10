# Transfer and bootstrap scripts

## Laptop

Run the wrapper from the downloaded handoff kit:

```powershell
.\Invoke-KingmakerBuffPlannerLaptopHandoff.ps1 -MakeRepositoryPrivate
```

It invokes:

- `Publish-KingmakerBuffPlannerRepositoryBootstrap.ps1`
- `New-KingmakerBuffPlannerPrivateTransfer.ps1`

The first publishes only public-safe files. The second creates a private SHA-256-manifested ZIP.

## Desktop

After copying the private ZIP to the desktop:

```powershell
.\scripts\Initialize-KingmakerBuffPlannerDesktopCheckout.ps1 `
  -TransferZip "C:\Users\<you>\Downloads\KingmakerBuffPlanner-PrivateTransfer-....zip"
```

That script:

- clones or verifies the standalone repository;
- checks out `codex/kingmaker-buff-planner`;
- verifies and imports the private transfer;
- installs the isolated Codex config/rules templates;
- clones public reference source;
- refreshes the desktop environment intake.

It does not modify Steam, Kingmaker's `Mods` directory, or a save.
