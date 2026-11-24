# UMM 0.31.1 baseline evidence

Machine-generated outputs exported 2026-11-24 15:16 UTC for candidate commit `0e26eeda0881f9a9a73376d18d12bd30c99a8187`. Each row names what produced that output (its
source commit, or the reused input it describes); outputs produced at another commit are identified there.
Only local path prefixes were replaced: `<repo>` (this checkout or its temporary clone root), `<temp>`, `<game>`
(the Kingmaker installation, read-only), `<buildtools>`, `<reference-assemblies>`, `<dotnet>`, `<controller>` (the agent-review controller), `<dev>` (the owner's development folder), `<steam>`, `<user-profile>`, `<fleet>` (the owner's fleet mission folder,
outside this repository). Line endings were normalized to LF and a UTF-8 byte-order mark was dropped; nothing else was
edited. Raw SHA-256 is of the original output bytes. Exported SHA-256 is of the committed Git blob of each file in this
folder (LF bytes; a Windows checkout with `core.autocrlf` may show CRLF in the working tree, so hash the blob, not the
checked-out file). To verify one, from the repository root (Git Bash or any POSIX shell with sha256sum; the second
command, with Python, works in Windows PowerShell and cmd.exe):

```
git cat-file blob HEAD:docs/evidence/umm-0311/<file> | sha256sum
python -c "import hashlib,subprocess,sys; print(hashlib.sha256(subprocess.check_output(['git','cat-file','blob','HEAD:docs/evidence/umm-0311/'+sys.argv[1]])).hexdigest())" <file>
```

| File | Produced by | Raw SHA-256 | Exported SHA-256 (Git blob) |
| --- | --- | --- | --- |
| `retained-frozen-product.json` | retained pair built in the frozen worktree at 8ac5738; stamp comparison with 0e26eeda | `1ebf72e1d082d429e6d8aaff6b3c05dc08509d69687884c8546ea097cc2cac0a` | `193a6beae5d5eb40b64e165ca9c444e6e2bdf47e8fe5420c9ae9d84f705f96f7` |
| `retained-build-receipt.json` | `8ac57387509f13840659365c9f7d01762e80bcf5` | `4e317a36eb66e5204d5949aa2caeba89a1f4d3da7a8660035c6338f0a8586f43` | `85e4bba4c6d088856ac3707ed17d103b19f231c1cf05c3a05796baeaf1de2ed1` |
| `retained-validation.json` | read-back of the retained pair at 8ac5738 | `f29785fb5dac2148b01e0514ae9c0bbcbd9927430f5b4848383cfb295d2b445e` | `bc1560f66d43af4d4228d8b955c87d6d4cfc1f689d3bda2242a6b1b39b25fa8d` |
| `loader-baseline.json` | `8ac57387509f13840659365c9f7d01762e80bcf5` | `d04e2c21f0aa587c449ba1611e6241d9f5c3e301eabe1dc614d62ed019730503` | `1b2b7e373c6f918ab281f9a5ce140462127d192202242dfe79be387db387a9b9` |
| `runtime-loader-identity.json` | 74a9efc7d066f193c84c7e3554f41515b358c309 (only Test-LoaderBaseline.ps1 changed before 8ac5738) | `284051b126d8ea7788210a7115f421dab8759b5d5060123d570e73e6910332ee` | `fcbda97c7a05ea8f5fc269675ffdaecb61a1e8ac2965fbe3a85bf355378d7420` |
| `api-audit.json` | static audit of the retained DLL 80e0a1fb | `b3ffc0f604cdefebb50be0c6b73061dca6ccbdd21010cd805498afdc77188be1` | `9f80b823595f101d0e40635dbc5fe60bd0718ad56583eb51eb3fca4dc401cad5` |
| `loader-profile-inventory.json` | profile inventory with the retained DLL 80e0a1fb | `60e54635be7cc3365e1b65847d9d84681532366c15182b42ab6da0234ab53c09` | `73d7767200baff222d614a881826604ea136467b9ccd7000ccc81501032c38cf` |
| `docs-link-check.json` | `0e26eeda0881f9a9a73376d18d12bd30c99a8187` | `cf9d2da2099228dd763523438486752b30118f2b75f412764c95317c6d0f78d2` | `247856e0ffe4d9486c4104c063b010ba5c579ac0cbef5144d875cc4a32ad45c8` |
| `newest-stable-selection.json` | reused fleet record measured 2026-11-23 | `3b3afcb904f00b33006d74c481fbce4bf6e4992e7f9001d28cdf28d589f9f56f` (original output, from its first export) | `150806fc23701c046ae9980c73c91d12c3badb671d7e30c126b61060784dd51e` |
| `loader-profiles.md` | fleet loader-profile record (reused) | `3b2883677f537286a0d6f210327fae8ac8734b40ec7cc5850e92819eaeb03d5e` (original output, from its first export) | `3b2883677f537286a0d6f210327fae8ac8734b40ec7cc5850e92819eaeb03d5e` |
| `loader-api-audit-Program.cs.txt` | audit tool source (reused) | `be870827024fd41f63235ebb522c489e8d49c7991de7091ad0e14a1a0e935ea3` (original output, from its first export) | `be870827024fd41f63235ebb522c489e8d49c7991de7091ad0e14a1a0e935ea3` |
| `loader-api-audit-csproj.txt` | audit tool project (reused) | `93989652d8252ff54a60ec92c13c1fc41bf94bfa3cf6c9f7f13c877bac761909` (original output, from its first export) | `93989652d8252ff54a60ec92c13c1fc41bf94bfa3cf6c9f7f13c877bac761909` |
