# Review Record - Resource Rally Production

Step: Implement EntityWorld resource-rally auto-harvest core.
Milestone: Command Vocabulary Completeness
Owner AI: Codex
Reviewer AI: Codex reviewer pass
Integrator AI: Codex

Scope:
- Files/folders: `scripts/core/entities/EntityCommand.cs`, `scripts/core/entities/EntityComponentState.cs`, `scripts/core/entities/EntityStateHash.cs`, `scripts/core/sim/SimInvariants.cs`, `scripts/core/sim/systems/ProductionSystem.cs`, `tools/SimReplay/Program.cs`, `tools/ReviewGate/Program.cs`, `TODO.md`.
- Non-goals: UI/right-click rally wiring, repeat production toggles, rally onto moving units, legacy `GameState` rally path migration, production command queue modifiers.

Implementation summary:
- Added `SetRallyPointEntityCommand` so producer rally intent can flow through the EntityWorld command buffer.
- Extended `RallyPointComponentState` with optional `TargetEntityId`; hash and invariants now include that entity reference.
- `ProductionSystem` now applies rally commands to producer entities, preserves existing point-rally behavior, and treats resource-node rally targets specially for produced harvesters.
- Produced harvesters from a resource rally enter `HarvesterMode.MovingToField`, keep the resource as `FieldId`, and move/visualize intent at the resource point.
- Added deterministic replay coverage for rallying a factory to a resource, producing a harvester, and proving it starts the harvest loop.

Automated gates:
- Command: `dotnet build ProceduralRts.csproj --no-restore`
  Result: pass
  Evidence: build completed with 0 warnings and 0 errors.
- Command: `dotnet run --project tools/SimReplay/SimReplay.csproj --no-restore`
  Result: pass
  Evidence: `OK [resource-rally-production]: mode ReturningToRefinery, field 2, resource 0, cargo 100.` and `SimReplay PASSED.`
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj resourcerally --no-restore`
  Result: pass
  Evidence: `Errors: 0`, `Warnings: 0`, `ReviewGate passed.`
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore`
  Result: pass
  Evidence: `Errors: 0`, `Warnings: 0`, `ReviewGate passed.`
- Command: `dotnet run --project tools/VerifyAll/VerifyAll.csproj --no-restore`
  Result: pass
  Evidence: all steps passed after this slice.

Manual/visual gates:
- Check: Visual QA
  Result: not applicable
  Evidence: this slice is deterministic simulation behavior only; UI/right-click rally wiring remains open.

Reviewer result:
- Status: pass
- Required fixes: none
- Residual risks: Resource rally currently targets static resource nodes. Rally onto moving units, repeat/loop production, and UI command affordances remain open.

TODO update:
- Items marked done: `EntityWorld resource-rally auto-harvest core`.
- Items left open: repeat production, queued command modifiers, moving-unit rally, and UI/smart-right-click rally wiring.
- Reason: replay and ReviewGate prove the bounded producer-resource-rally-to-harvester-autoharvest path; adjacent UI and repeat-production behavior remains separate.
