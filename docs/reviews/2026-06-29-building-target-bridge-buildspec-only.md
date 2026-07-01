Step: Remove split-definition overloads from BuildingTargetEntityBridge.
Milestone: M1 EntityWorld Becomes Authoritative
Owner AI: Codex
Reviewer AI: Codex review pass
Integrator AI: Codex

Scope:
- Files/folders: `scripts/core/entities/BuildingTargetEntityBridge.cs`, `tools/CombatBehavior/Program.cs`, `tools/ReviewGate/Program.cs`, `TODO.md`.
- Removed `BuildingDefinition` / `BuildDefinition` adapter overloads for `ToEntitySpec`, `SpawnBuildingTarget`, and `ToEntityComponents`.
- Updated bridge coverage in `CombatBehavior` to use `BuildSpec` directly for entity specs and building target spawning.
- Non-goals: no deletion of `BuildingDefinition`, `BuildDefinition`, or their compatibility projection catalogs yet.

Automated gates:
- Command: `dotnet build ProceduralRts.csproj --no-restore`
  Result: pass
  Evidence: build completed successfully with 0 warnings and 0 errors.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj buildingtargetbridgebuildspeconly --no-restore`
  Result: pass
  Evidence: dedicated BuildSpec-only building target bridge gate completed with 0 errors and 0 warnings.
- Command: `dotnet run --project tools/CombatBehavior/CombatBehavior.csproj --no-restore`
  Result: pass
  Evidence: CombatBehavior completed after moving bridge coverage to direct BuildSpec calls.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore`
  Result: pass
  Evidence: full ReviewGate completed with 0 errors and 0 warnings.
- Command: `dotnet run --project tools/VerifyAll/VerifyAll.csproj --no-restore`
  Result: pass
  Evidence: VerifyAll completed all 16 steps successfully, including SimReplay, CombatBehavior, PerfSmoke, BalanceReport, and Godot headless QA.

Reviewer result:
- Status: pass.
- Design note: the EntityWorld building bridge now has one authoring surface: `BuildSpec`.
- Required fixes: none identified before gates.

Status:
- Pass.

Residual risks:
- Split legacy definitions still exist as compatibility projections for older call sites outside this bridge.
- `UnitBattlefieldBuildingTarget` still exists as a mutable migration wrapper.

TODO update:
- Marked done: nested M1 slice `BuildingTargetEntityBridge BuildSpec-only cleanup`.
