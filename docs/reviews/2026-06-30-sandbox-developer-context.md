# Review Record - Sandbox Developer Context

Step:
Add pure-core sandbox developer context switching for owner, faction, team,
relation, environment, time scale, and debug overlays.

Milestone:
M8 Sandbox developer controls.

Owner AI:
Worker-M8.

Reviewer AI:
Integrator gate review via `ReviewGate sandboxdevelopercontext`.

Integrator AI:
Codex main thread.

Scope:
- Files/folders:
  - `scripts/core/SandboxDeveloperContext.cs`
  - `scripts/core/SandboxSpawnAuthoring.cs`
  - `tools/SandboxSpawnAuthoringQa/Program.cs`
  - `tools/ReviewGate/Program.cs`
  - `TODO.md`
- Non-goals:
  - No runtime sandbox UI buttons.
  - No one-click stress tests.
  - No mission/campaign trigger work.

Implementation summary:
- Added deterministic `SandboxDeveloperContext` and parsed request helpers.
- Added owner/faction/team/relation/environment/time/debug-overlay options for
  future sandbox UI controls.
- Kept Corruption as a locked placeholder: selectable as context metadata but
  unable to spawn content.
- Extended sandbox spawn authoring with context-filtered lists and safe request
  creation.

Automated gates:
- Command:
  `dotnet run --project tools/SandboxSpawnAuthoringQa/SandboxSpawnAuthoringQa.csproj --no-restore`
  Result:
  Pass.
  Evidence:
  QA proves entries/specs plus owner, faction, team, relation, environment, time,
  debug overlay, and locked-Corruption behavior.
- Command:
  `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore -- sandboxdevelopercontext`
  Result:
  Pass.
  Evidence:
  Gate locks the core context model, spawn filtering, QA proof, and VerifyAll hook.

Reviewer result:
Pass. The sandbox state model is UI-independent and deterministic.

Status:
Pass.

Residual risks:
- Runtime sandbox UI controls and stress-test buttons remain open.

TODO update:
- Added progress under the broad M8 sandbox item; parent remains open.
