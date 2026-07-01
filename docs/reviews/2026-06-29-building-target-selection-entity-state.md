Step: Move building target selection state fully into EntityWorld.
Milestone: M1 EntityWorld Becomes Authoritative
Owner AI: Codex
Reviewer AI: Codex review pass
Integrator AI: Codex

Scope:
- Files/folders: `scripts/core/units/runtime/UnitBattlefieldBuildingTarget.cs`, `scripts/core/units/runtime/UnitBattlefield.cs`, `scripts/core/entities/BuildingTargetEntityBridge.cs`, `scripts/BattleRoot.cs`, `tools/ReviewGate/Program.cs`, `TODO.md`.
- Removed `UnitBattlefieldBuildingTarget.Selected`.
- `SyncBuildingTargetEntity` now preserves existing `SelectableComponentState` before rebuilding components.
- BattleRoot legacy building selection fallback now reads `UnitBattlefield.BuildingProjection(...).Selected`.
- Non-goals: no removal of the target wrapper itself, no event signature migration, no UI fallback deletion.

Automated gates:
- Command: `dotnet build ProceduralRts.csproj --no-restore`
  Result: pass
  Evidence: build completed successfully with 0 warnings and 0 errors.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj buildingtargetselectionentitystate --no-restore`
  Result: pass
  Evidence: dedicated building-target selection EntityWorld-state gate completed with 0 errors and 0 warnings.
- Command: `dotnet run --project tools/CombatBehavior/CombatBehavior.csproj --no-restore`
  Result: pass
  Evidence: CombatBehavior completed with building selection, projection, production, and rally checks intact.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore`
  Result: pass
  Evidence: full ReviewGate completed with 0 errors and 0 warnings after updating older building-selection gates.
- Command: `dotnet run --project tools/VerifyAll/VerifyAll.csproj --no-restore`
  Result: pass
  Evidence: VerifyAll completed all 16 steps successfully, including SimReplay, CombatBehavior, PerfSmoke, BalanceReport, and Godot headless QA.

Reviewer result:
- Status: pass.
- Design note: this removes another mutable mirror field from the second building runtime and keeps selection authoritative in `SelectableComponentState`.
- Required fixes: none identified before gates.

Status:
- Pass.

Residual risks:
- Other mutable target fields still mirror production, rally, dock, pulses, and combat targeting during migration.
- Legacy `BuildingModel.Selected` still exists as a UI fallback value, now populated from UnitBattlefield projections.

TODO update:
- Marked done: nested M1 slice `UnitBattlefieldBuildingTarget selection EntityWorld cleanup`.
