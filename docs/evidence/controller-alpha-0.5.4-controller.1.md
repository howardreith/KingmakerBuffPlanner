# Controller Alpha 1 publication receipt

October 17, 2026. The owner accepted the source-review checkpoint, requested an
alpha to test on another computer, then explicitly selected publication without
a full release workflow or new tests. This supersedes the earlier no-publication
boundary for this **artifact-only alpha**, not for a stable release, PR merge,
local game launch or installation.

Published: [Controller Alpha 1](https://github.com/howardreith/KingmakerBuffPlanner/releases/tag/v0.5.4-controller.1)
at `2026-10-17T20:06:32Z`. GitHub reports draft=false, prerelease=true,
Latest=false. Stable v0.5.3 remains Latest; main remains
`dcf718fd7b70cf453dd25aa693fefc6aa503d826`. PR #10 remains draft/unmerged.

| Identity | Value |
| --- | --- |
| Tag and exact product target | `v0.5.4-controller.1` -> `9aec44e374b26b3561fa4402912c77a23a4e08ac` |
| Packaged version / CLR | `0.5.4-controller.1` / `0.5.4.0` |
| Installable asset | `KingmakerBuffPlanner-0.5.4-controller.1.zip` (898208 bytes) |
| ZIP SHA-256 | `6e5c3f1cc067f72d92558fa474b9fe5a0c4929e7b4503d7068e0912b9daa9d59` |
| DLL SHA-256 | `0d4e7895c9235a540f3da9f98f10de75248c3b65e7547ff2538258b2dadc3eeb` |
| DLL MVID | `60d7ee7b-0541-45de-83c4-308311d91331` |
| SHA256SUMS.txt SHA-256 | `26dfdf163ebef5482c584767b6d81de592192acc9aaf60668aef3c4efb542129` |

Reused the exact existing [frozen candidate](../CONTROLLER-UI-CANDIDATE-0.5.4-CONTROLLER.1.md)
ZIP, without rebuilding, renaming its internal version or relabeling its prior
45/0 focused tests and deterministic-build evidence. The older d04c1a7 candidate
also remains unchanged. No new test suite, full publisher, offline aggregate,
runtime, purity or acceptance campaign ran. The guarded publisher's normal
rebuild/all-offline-test route was not selected; this uses the owner-selected
artifact-only prerelease approach, as with the historical v0.5.0-rc.1 review
artifact. No script, validator or assertion was weakened.

Publication checks only: verified frozen manifest/ZIP hash, pushed product
ancestry and origin; strict package integrity 6/0 (0.54 s child wall); uploaded
ZIP/checksum into a draft, downloaded both into a fresh local folder and verified
exact bytes/checksum content; then published with explicit prerelease and
latest=false. Post-publication API asset digests match; ls-remote verifies the
tag points at the exact product and main is unchanged. No test or build rerun.

Commands: `gh release create ... --target 9aec44e... --prerelease --latest=false
--draft`; `gh release download`; `gh release edit ... --draft=false --prerelease
--latest=false`; read-only release/list/tag verification. Local publication notes,
checksum and downloaded bytes are under
`artifacts/controller-alpha-0.5.4-controller.1/`. Source worktree at publication:
`codex/kingmaker-buff-planner-controller-ui`, clean documentation HEAD
`b0d92ce0bffd5a01da7a37414c66bf517fc34b02`; release tag deliberately targets the
earlier compiled product, not this later documentation.

Native/physical controller behavior remains unqualified. Release notes tell the
owner to back up the complete mod folder/UserSettings and saves, use a disposable
test campaign, and test native entry/handoff/navigation/Back **before casting**.
Full controller editing is deferred. No changes were made to DATA's game,
installation, saves, profiles, settings or another mod; restoration remains
NOT_RUN. Next is owner alpha feedback from the other PC, not an automatic lab
qualification campaign or permanent installation.
