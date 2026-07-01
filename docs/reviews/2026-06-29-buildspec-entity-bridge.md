# Review Record - BuildSpec Entity Bridge

Step: Add a unified `BuildSpec` bridge for building entity/spec generation as the first migration cleanup slice.
Milestone: M1 EntityWorld Becomes Authoritative
Owner AI: Codex
Reviewer AI: Codex reviewer pass
Integrator AI: Codex

Scope:
- Files/folders: `scripts/core/BuildSpec.cs`, `scripts/core/BuildSpecCatalog.cs`, `scripts/core/entities/BuildingTargetEntityBridge.cs`, `scripts/core/units/runtime/UnitBattlefield.cs`, `tools/CombatBehavior/Program.cs`, `tools/ReviewGate/Program.cs`, `TODO.md`.
- Non-goals: deleting `BuildingDefinition`, deleting `BuildDefinition`, replacing all build UI/status calls, removing `UnitBattlefieldBuildingTarget`.

Implementation summary:
- Added `BuildSpec`, a single bridge record that merges current building runtime data (`BuildingDefinition`) and build/economy data (`BuildDefinition`).
- Added `BuildSpecCatalog`, which covers all current `BuildCatalog.Definitions` and merges them with `GameState.BuildingDefinitions` during migration.
- `BuildingTargetEntityBridge` now generates `EntitySpec` and component state from `BuildSpec`, while retaining old overloads for compatibility.
- `UnitBattlefield.SyncBuildingTargetEntity` now reads `BuildSpecCatalog.For(target.Kind)` instead of directly joining `GameState.BuildingDefinitions` and `BuildCatalog`.
- `CombatBehavior` proves BuildSpecCatalog coverage and BuildSpec-driven entity/component bridging for turret, producer, construction, power, dock, and deterministic hash behavior.

Automated gates:
- Command: `dotnet build ProceduralRts.csproj --no-restore`
  Result: pass
  Evidence: build completed with 0 warnings and 0 errors.
- Command: `dotnet run --project tools/CombatBehavior/CombatBehavior.csproj --no-restore`
  Result: pass
  Evidence: Combat behavior passed with BuildSpecCatalog building bridge assertions.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj buildspecbridge --no-restore`
  Result: pass
  Evidence: `Errors: 0`, `Warnings: 0`, `ReviewGate passed.`
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj review --require-record=buildspec-entity-bridge --no-restore`
  Result: pass
  Evidence: `Errors: 0`, `Warnings: 0`, `ReviewGate passed.`
- Command: `dotnet run --project tools/VerifyAll/VerifyAll.csproj --no-restore`
  Result: pass
  Evidence: all 14 steps passed after this slice.

Manual/visual gates:
- Check: Visual QA
  Result: not run
  Evidence: this slice changes authoring/spec plumbing only; current presentation-facing fields remain compatible.

Reviewer result:
- Status: pass
- Required fixes: none
- Residual risks: Legacy building/build records still exist and are still used by other UI/build surfaces. This is a bridge slice, not the final deletion of duplicate authoring.

TODO update:
- Items marked done: nested M1 slice `UnitBattlefield BuildSpec entity bridge`.
- Items left open: parent migration cleanup, `UnitBattlefieldBuildingTarget` removal, construction/build placement migration, legacy catalog deletion.
- Reason: tests and ReviewGate prove the first unified spec bridge without claiming the full migration cleanup is complete.
