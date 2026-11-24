# UMM 0.31.1 baseline evidence

Machine-generated outputs for the frozen product commit `5cd32d080416718706d94e539c92f000a66fb7b6`, exported 2026-11-24 02:12 UTC.
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

| File | Raw SHA-256 | Exported SHA-256 (Git blob) |
| --- | --- | --- |
| `retained-frozen-product.json` | `d353f50abe18b128a1959cdd459bc7e0f51bb4731650cb22475f86d47a4b19e4` | `792ac2cbdee06e5c8e33040970f65f8f9fe57751d2cd6896a9a32221e9d4f5f5` |
| `retained-build-receipt.json` | `a59cc6e44066aca9204c5b6a2e4680a547e6ab24274e9eee68b4dff1e3582945` | `747b331e9c882f853a69edb6d3308941dfb9a4f3619d63d586258ee1de2954ac` |
| `retained-validation.json` | `64e0d02610d30cad7a76490294ae4b8e9216e900474a9752cd9e96d87f9fe0a5` | `4c07c42ea457cc9f1a6346d3fd40144b14ecfee1e75a4c8861ac69ece224cfdd` |
| `loader-baseline.json` | `193e4440a8d68640ebc26df46d0733077db781cfb3d09d959fb28f02f7361441` | `62b4e578e0d5d5f86cbfd1805c011434217a52bc8a4e5ebddf66da252f2c1abf` |
| `runtime-loader-identity.json` | `308d982cdff4f1519df4be909fb64934c7ede35d8e77363aed568f29651db167` | `c32cd8055bb12bef37a55155538684a704e3038941fbaaae9f7a817b83f34998` |
| `direct-build-record.json` | `72355379f6417e0419293c013c5214369b24d73e09db62297cc1ddce5b8c93bd` | `0d60ac47036d8614c06525f264682a411d91aae5643bbbcbaac843c8952d3596` |
| `api-audit.json` | `3988f286ad96696e085d49316e5a4a55611230ee0e4e6c24b8ae7ad437178ad5` | `75bceac84ec91eb6c2a3bbba99b7c27a6370d636552bd612e593eb00ddcee394` |
| `loader-profile-inventory.json` | `20d1dc252f8ed225f1bf9c8eb1605ce6d0ed8dc65a3c9d65420079a7405a3392` | `a6e8aab8f0897966db4d57ca40913e1e11cd8b0643130bbf59b8728901b33e6d` |
| `docs-link-check.json` | `377980615c0847e5f2bdfd01892ebc85c1551f0de46cfb9cff06eb66f88a48d3` | `8e180829325fb959482e33fe2364ac41aad0304f8012d39d101c3780283447af` |
| `newest-stable-selection.json` | `3b3afcb904f00b33006d74c481fbce4bf6e4992e7f9001d28cdf28d589f9f56f` | `150806fc23701c046ae9980c73c91d12c3badb671d7e30c126b61060784dd51e` |
| `loader-profiles.md` | `3b2883677f537286a0d6f210327fae8ac8734b40ec7cc5850e92819eaeb03d5e` | `3b2883677f537286a0d6f210327fae8ac8734b40ec7cc5850e92819eaeb03d5e` |
| `loader-api-audit-Program.cs.txt` | `be870827024fd41f63235ebb522c489e8d49c7991de7091ad0e14a1a0e935ea3` | `be870827024fd41f63235ebb522c489e8d49c7991de7091ad0e14a1a0e935ea3` |
| `loader-api-audit-csproj.txt` | `93989652d8252ff54a60ec92c13c1fc41bf94bfa3cf6c9f7f13c877bac761909` | `93989652d8252ff54a60ec92c13c1fc41bf94bfa3cf6c9f7f13c877bac761909` |
