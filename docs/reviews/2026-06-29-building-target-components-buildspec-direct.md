Step: Read static building component data directly from BuildSpec.
Milestone: M1 EntityWorld Becomes Authoritative
Owner AI: Codex
Reviewer AI: Codex review pass
Integrator AI: Codex

Scope:
- Files/folders: `scripts/core/entities/BuildingTargetEntityBridge.cs`, `tools/ReviewGate/Program.cs`, `TODO.md`.
- EntityWorld building component generation now reads max HP, footprint, collision radius, and weapon kind directly from `BuildSpec`.
- The bridge no longer routes those static values through `UnitBattlefieldBuildingTarget` convenience properties.
- Non-goals: no removal of the remaining convenience properties, no target wrapper deletion.

Automated gates:
- Command: `dotnet build ProceduralRts.csproj --no-restore`
  Result: pass
  Evidence: build completed successfully with 0 warnings and 0 errors.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj buildingtargetcomponentsspecdirect --no-restore`
  Result: pass
  Evidence: dedicated direct BuildSpec component gate completed with 0 errors and 0 warnings.
- Command: `dotnet run --project tools/CombatBehavior/CombatBehavior.csproj --no-restore`
  Result: pass
  Evidence: CombatBehavior completed with building bridge, projection, combat, production, and AI checks intact.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore`
  Result: pass
  Evidence: full ReviewGate completed with 0 errors and 0 warnings.
- Command: `dotnet run --project tools/VerifyAll/VerifyAll.csproj --no-restore`
  Result: pass
  Evidence: VerifyAll completed all 16 steps successfully, including SimReplay, CombatBehavior, PerfSmoke, BalanceReport, and Godot headless QA.

Reviewer result:
- Status: pass.
- Design note: this makes the entity bridge less dependent on the mutable building-target wrapper and keeps static authoring data on the spec path.
- Required fixes: none identified before gates.

Status:
- Pass.

Residual risks:
- Other UnitBattlefield paths still read target convenience properties for picking, projections, and tests.
- The target wrapper still owns mutable migration fields.

TODO update:
- Marked done: nested M1 slice `BuildingTargetEntityBridge direct BuildSpec component cleanup`.
