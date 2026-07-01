# Review Record - Naming structure gate

Step: Add automated naming/structure checks for systems, component states, commands, and math helpers.
Milestone: Engineering Conventions.
Owner AI: Codex.
Reviewer AI: Codex self-review (subagents unavailable in this continuation turn; ReviewGate provides durable source checks).
Integrator AI: Codex.

Scope:
- Files/folders: `tools/ReviewGate/Program.cs`, `scripts/core/entities/EntityCommandBuffer.cs`, `scripts/core/entities/EntityWorld.cs`, `scripts/core/sim/ISimSystem.cs`, `tools/SimReplay/Program.cs`, `tools/PerfSmoke/Program.cs`, `TODO.md`, `docs/reviews/2026-06-29-naming-structure-gate.md`.
- Non-goals: no gameplay behavior changes, no EntityWorld live-authority migration, no data-authoring migration.

Implementation summary:
- Added `ReviewGate naming`.
- The gate verifies `scripts/core/sim/systems/*System.cs` files define matching `*System : ISimSystem` classes.
- The gate verifies `*Math.cs` files expose matching static helper classes.
- The gate verifies records named `*ComponentState` inherit `EntityComponentState`.
- The gate verifies gameplay command records named `*EntityCommand` inherit `EntityCommand`.
- Renamed the command-buffer ordering wrapper from `SequencedEntityCommand` to `SequencedCommandEnvelope`, because it is not a gameplay command.

Automated gates:
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj naming --no-restore`
  Result: pass
  Evidence: ReviewGate reported 0 errors and 0 warnings.
- Command: `dotnet build ProceduralRts.csproj --no-restore`
  Result: pass
  Evidence: build completed with 0 warnings and 0 errors.
- Command: `dotnet run --project tools/SimReplay/SimReplay.csproj --no-restore`
  Result: pass
  Evidence: full deterministic SimReplay suite passed after the command-envelope rename.

Manual/visual gates:
- Check: visual QA
  Result: not applicable
  Evidence: naming/structure-only change; no presentation output changed.

Reviewer result:
- Status: pass
- Required fixes: none.
- Residual risks: this gate proves naming and structure conventions for the current core, but it does not prove broader content-authoring or view-authority TODOs.

TODO update:
- Items marked done: `Naming/structure`.
- Items left open: `New content = new data`; `Components hold state only; behavior lives in systems; views hold no authority`.
- Reason: the exact naming convention now has automated source checks plus build and deterministic replay evidence.
