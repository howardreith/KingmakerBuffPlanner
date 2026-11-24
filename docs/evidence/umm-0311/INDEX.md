# UMM 0.31.1 baseline evidence

Machine-generated outputs for the frozen product commit `75aeeff68670913994a3caa0fb6a0d5e1edd49ba`, exported 2026-11-24 00:45 UTC.
Only local path prefixes were replaced: `<repo>` (this checkout or its temporary clone root), `<temp>`, `<game>`
(the Kingmaker installation, read-only), `<buildtools>`, `<reference-assemblies>`, `<dotnet>`, `<user-profile>`, `<fleet>` (the owner's fleet mission folder,
outside this repository). Line endings were normalized to LF and a UTF-8 byte-order mark was dropped; nothing else was
edited. Raw SHA-256 is of the original output bytes. Exported SHA-256 is of the committed Git blob of each file in this
folder (LF bytes; a Windows checkout with `core.autocrlf` may show CRLF in the working tree, so hash the blob, not the
checked-out file). To verify one, from the repository root:

```
git cat-file blob HEAD:docs/evidence/umm-0311/<file> | sha256sum
python -c "import hashlib,subprocess,sys; print(hashlib.sha256(subprocess.check_output(['git','cat-file','blob','HEAD:docs/evidence/umm-0311/'+sys.argv[1]])).hexdigest())" <file>
```

| File | Raw SHA-256 | Exported SHA-256 (Git blob) |
| --- | --- | --- |
| `frozen-product.json` | `228b4a4deb7c7742dd7842b029ad720e2a0d2aa0fe4a7e49af831720d88eddf0` | `590efc63a1ae55c8cf07e9be7a497165c35558f3d2dd8fafa77dcf50bee613b6` |
| `frozen-build-receipt.json` | `8d5d2e6cc510029c54d5bd2327dc29721e19cd3b8bbec73f6cc3820030652f20` | `09a0771f7639cf4f96ab123059da909eba3b5315dc3300fa775307059d098c42` |
| `frozen-package-validation.json` | `c6fbfc414e53c7fbd1f720f2e3919d2904987106aed6792f01120a5bb6517368` | `6cf59601fc7df1fae057b1973a0bc21f79839b91cace9f2b2cd40947e880cec7` |
| `loader-baseline.json` | `f82730c8814edb55f6de25726c3336cd9ac660ff3dd33360ffa61442c36c7dbc` | `20ccd5a65e52d67af748e27b73a1675540082ea8e09be251cbaa84c188916483` |
| `runtime-loader-identity.json` | `c9b2d180ab19ac8fe5ef974eac6190a7b09f4aadd5c87f26a4d1747469304c16` | `9d887a211dca750aacdde88cd170b3fda5e8124b7f2a60bfa7bd96618b853180` |
| `direct-build-record.json` | `39fa1ea47fe27ca6c9bb6d47ca58aeeaa19ecb6dff1787c8c39bde5c171be158` | `69d9c3d626980e6c643108485205a9173a9aa78a882a13ff06ba7fd4c6fc95a0` |
| `api-audit.json` | `06d256728d034548f5335aa57683b318d5e3d69e7407ff9c85e8ec1a0761ed8d` | `bc14ec3130166db4fca6c6275c628709a75a2a23651d15ce054ff0a65fe87ea2` |
| `loader-profile-inventory.json` | `05cdfed204947cf1d432bc9f011531a4b8b502fa8b58c9fac8acee2a439859cf` | `ae2df9a60ab7ae059c3b270259aab3e16077caf9ba747001b6a1d116785ee63c` |
| `docs-link-check.json` | `377980615c0847e5f2bdfd01892ebc85c1551f0de46cfb9cff06eb66f88a48d3` | `8e180829325fb959482e33fe2364ac41aad0304f8012d39d101c3780283447af` |
| `newest-stable-selection.json` | `3b3afcb904f00b33006d74c481fbce4bf6e4992e7f9001d28cdf28d589f9f56f` | `150806fc23701c046ae9980c73c91d12c3badb671d7e30c126b61060784dd51e` |
| `loader-profiles.md` | `3b2883677f537286a0d6f210327fae8ac8734b40ec7cc5850e92819eaeb03d5e` | `3b2883677f537286a0d6f210327fae8ac8734b40ec7cc5850e92819eaeb03d5e` |
| `loader-api-audit-Program.cs.txt` | `be870827024fd41f63235ebb522c489e8d49c7991de7091ad0e14a1a0e935ea3` | `be870827024fd41f63235ebb522c489e8d49c7991de7091ad0e14a1a0e935ea3` |
| `loader-api-audit-csproj.txt` | `93989652d8252ff54a60ec92c13c1fc41bf94bfa3cf6c9f7f13c877bac761909` | `93989652d8252ff54a60ec92c13c1fc41bf94bfa3cf6c9f7f13c877bac761909` |
