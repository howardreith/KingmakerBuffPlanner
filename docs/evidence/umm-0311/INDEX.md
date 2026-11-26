# UMM 0.31.1 baseline evidence

Machine-generated outputs exported 2026-11-26 16:50 UTC for candidate commit `a76174b9767bdc131840a90027fbe080e8334420`. Each row names what produced that output (its
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
| `loader-baseline-r7.json` | a76174b (round-7 code; 117/117 on a temporary clone) | `aab54fb3a4e25416b21df62575ec8fea9004223d8e541b9012e88fec24e40dd2` | `b2b9273a9bae48c4850e35302f7a4886cfa2848ac3705c4f60babcc0bb3924b0` |
| `build-receipt-r7.json` | a76174b (Build-Local in this clean checkout) | `bcf131fe7c15e5ddb146c7ddc997193513f75ee158f710a871c5e88354c63f80` | `779ebc1d1a25d5fa4cd64f4d539859ed79fa894c217683cacfb33ceef16c63ff` |
| `package-validation-r7.json` | a76174b (validate-package on the Build-Local ZIP 23048581) | `a4a0de82fa293f050debb1536be254410134c1972f6389dae90e183768b4f126` | `2d739bcc2043dbcaa525b3fe987aa08b9563045911dbd97802ac0fd269c4f195` |
| `api-audit-r7.json` | static audit of the a76174b packaged DLL 684b28ef (MVID 5a94f398) | `16b05f3aa8987a317976c4ba3a1456a4bd1ce3e62cd1e86720e1d5d92eebeffd` | `99d11f8bdd3a2199e62203ce1f627ba7f8d4a0e608697ea5819f25e82754a911` |
| `loader-baseline-r6.json` | aebe2ed (round-6 code 51e8776; 111/111 on a temporary clone) | `1016cc8423425acabde292b5af649fcf89bf47d64ab049b06285e0adf99da2b0` (original output, from its first export) | `9401d3e0ec11c281aa6375da7f7a45b0b5f88357d8e8f5e68336a9e228751703` |
| `retained-frozen-product.json` | retained pair built in the frozen worktree at c2a594c; stamp comparison with 1c3671ff | `be30c9b227c1cd2dc3e2ab7b16da7417e778ec81e8bb69135e5e64c900e00672` (original output, from its first export) | `081c3904b1b9ce94a928e89b8ddac13409491e96882716542eab7e9294840984` |
| `retained-build-receipt.json` | `c2a594ce7124cba35c63523b6ad4a039c84cdbc3` | `fa40acdb2dfb0e6a30cb3bbde4a604e2cfd1cbb59858823b546cb030cbe1299b` (original output, from its first export) | `7110deff0b7ce8916174ec1eb50795436b2bfcffe730bf26f81d0ed5e5f11e8d` |
| `retained-validation.json` | read-back of the retained pair at c2a594c | `f0e365ef2d549225628604f8e701281e90cbc13c9a62da799c69122679f875b3` (original output, from its first export) | `6df6d5361b5ca45e504131544c41579fbcfd8f1ddbb5af63db2a8c6726528c4c` |
| `loader-baseline.json` | `c2a594ce7124cba35c63523b6ad4a039c84cdbc3` | `22a96ebed2e6c326e963db8c95d510710d1aff8c95c728ec661348a6ca5b82d0` (original output, from its first export) | `4cbd9d550f1ecf2e6b5d5f76f82a3858474c0d15fc0369e5c57ba36a2093d663` |
| `runtime-loader-identity.json` | c2a594ce7124cba35c63523b6ad4a039c84cdbc3 (run from the checkout with a relative -CandidatePackage) | `365b033bcc119f0892c93ac83bc657bdf0b379c34091f8c70c5badd985341f7c` (original output, from its first export) | `c2abe55897e5ed01539465d2d7db1ee839024c3450f2f1b0ded03c7cbd539207` |
| `api-audit.json` | static audit of the retained DLL a384dc44 | `7e0f46934fb5c28c29f66faaff7109e559098b1aa64966278c6aa989fbeaba55` (original output, from its first export) | `7004f27de64172bd1bb767b8bedf3243277e59161314abe7b3ceddae736ff5f5` |
| `loader-profile-inventory.json` | profile inventory with the retained DLL a384dc44 | `0e168675c780795b052152aa2c95b9dc8270c7747c40491aaf826dd93cca474b` (original output, from its first export) | `081f3360acdb7f88f894eb6db05061d5528e137dc5d561080e5798f236c53230` |
| `docs-link-check.json` | `1c3671ff168758276d8f829f063c049d86f9571f` | `4f098de913f082eaa31924311ec6d9e1f27ada59b2ee0a7d6d1de7cfa3044279` (original output, from its first export) | `e34a7bd7e2dc4120413c5243f5a401374644f2801f923e678caac02ce8572752` |
| `newest-stable-selection.json` | reused fleet record measured 2026-11-23 | `3b3afcb904f00b33006d74c481fbce4bf6e4992e7f9001d28cdf28d589f9f56f` (original output, from its first export) | `150806fc23701c046ae9980c73c91d12c3badb671d7e30c126b61060784dd51e` |
| `loader-profiles.md` | fleet loader-profile record (reused) | `3b2883677f537286a0d6f210327fae8ac8734b40ec7cc5850e92819eaeb03d5e` (original output, from its first export) | `3b2883677f537286a0d6f210327fae8ac8734b40ec7cc5850e92819eaeb03d5e` |
| `loader-api-audit-Program.cs.txt` | audit tool source (reused) | `be870827024fd41f63235ebb522c489e8d49c7991de7091ad0e14a1a0e935ea3` (original output, from its first export) | `be870827024fd41f63235ebb522c489e8d49c7991de7091ad0e14a1a0e935ea3` |
| `loader-api-audit-csproj.txt` | audit tool project (reused) | `93989652d8252ff54a60ec92c13c1fc41bf94bfa3cf6c9f7f13c877bac761909` (original output, from its first export) | `93989652d8252ff54a60ec92c13c1fc41bf94bfa3cf6c9f7f13c877bac761909` |
