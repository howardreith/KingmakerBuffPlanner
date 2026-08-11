# Architecture

Status: IN PROGRESS

Kingmaker Buff Planner is one standalone Unity Mod Manager mod. The assembly, namespace, UMM ID, persistence, package, runtime runner, and compatibility profiles are all owned by this repository.

The dependency direction is:

```text
Integration/UI/RuntimeTesting -> Services -> Domain
GameAdapters/Discovery/Execution/Persistence -> Domain contracts
Planning -> Domain only
Compatibility -> Discovery contracts through bounded reflection
```

Static Kingmaker, Unity, UMM, and Harmony state is confined to narrow adapters and the composition root. Discovery emits normalized immutable effect expressions; planning consumes catalog/provider snapshots without Unity dependencies; execution consumes an immutable plan through animated or instant engines. UI controllers orchestrate services and never implement scanning, allocation, persistence, or casting rules.

Runtime automation is a separate, opt-in request/result boundary. It cannot deploy itself, select arbitrary saves, or mutate the live mod directory outside the guarded PowerShell transaction.
