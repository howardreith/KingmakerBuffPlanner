# Desktop Codex Operating Contract

This Codex home is dedicated to the standalone **Kingmaker Buff Planner** project.

## Machine and project isolation

- Work only inside `C:\Dev\KingmakerBuffPlannerLab` unless a project-owned guarded script explicitly performs a reversible operation against the installed Kingmaker environment.
- The Tabletop Added Rules / Gunslinger project is a different product on a different machine. Never mutate, commit, reset, clean, rebase, push, package, or deploy it from this desktop.
- Read-only copies under `reference-source`, `examples`, and `harness-reference` are evidence and design references, not writable worktrees.
- Never copy or use laptop Git credential stores, Codex authentication state, secret files, active worktrees, runtime locks, or mutable backup directories.

## Autonomy

- Continue through all safe, independently actionable work until the active mission's definition of done is met or a documented critical hard stop is proven.
- Do not stop for ordinary implementation choices, one failing experiment, context compaction, an absent optional mod, or a need for better diagnostics.
- Prefer exact local assembly inspection and runtime evidence over assumptions.
- Keep durable journal, blocker, resume, architecture, matrix, and qualification records current.
- Before context compaction or a long transition, record the exact current commit, state, evidence, and next command in `AUTONOMOUS-RESUME.md`.

## Safety

- Never use `danger-full-access` or equivalent unrestricted authority for this project.
- Never touch valued saves. Use only project-owned `KBP_` fixtures through the guarded runtime harness.
- Never directly merge, clean, or overwrite the live Kingmaker `Mods` directory. The harness must stage transactionally and restore byte-for-byte in `finally`.
- Stop on Steam credentials, Steam Guard, purchases, cloud conflicts, updates, account state, or other unexpected dialogs.
- Do not modify third-party source or binaries. Optional mods are read-only fixtures.
- Do not commit game DLLs, proprietary assets, credentials, runtime evidence, backups, live mod payloads, or generated packages unless an explicit allowlist says otherwise.
- Do not publish a public release without separate user authorization.

## Engineering posture

- Target the exact installed Pathfinder: Kingmaker 2.1.7b environment, legacy Harmony12, .NET Framework 4.7, and C# 7.3 unless exact evidence proves otherwise.
- Keep game discovery, normalized domain logic, planning, persistence, execution, UI, and compatibility adapters separated.
- Routes or UI event handlers orchestrate only; business logic belongs in services/domain code.
- Prefer behavior-focused integration-style tests over implementation-detail mocks.
- Add no dependency unless it replaces substantial code, is actively maintained, works under the target framework, and is documented with a concrete justification.
- Preserve license notices and provenance for all reused MIT code. Do not reuse restricted Kingmaker Buff Bot code or assets.
