Step: Move UnitBattlefield building powered state and build progress into EntityWorld state.
Milestone: M1 EntityWorld Becomes Authoritative
Owner AI: Codex
Reviewer AI: Codex review pass
Integrator AI: Codex

Scope:
- Files/folders: `scripts/core/units/runtime/UnitBattlefieldBuildingTarget.cs`, `scripts/core/units/runtime/UnitBattlefield.cs`, `scripts/core/entities/BuildingTargetEntityBridge.cs`, `tools/CombatBehavior/Program.cs`, `tools/ReviewGate/Program.cs`, `TODO.md`.
- Removed `Powered` and `BuildProgress` from `UnitBattlefieldBuildingTarget`.
- Added `UnitBattlefield.BuildingPowered(...)` and `UnitBattlefield.BuildingBuildProgress(...)` as EntityWorld-backed accessors.
- `SyncBuildingTargetEntity` preserves `PowerComponentState` and `ConstructionComponentState` while rebuilding building components, unless the live BattleRoot upsert supplies migration seed values.
- Producer and refinery eligibility now read powered/construction state through UnitBattlefield EntityWorld accessors.
- Non-goals: no removal of legacy `BuildingModel.Powered` / `BuildingModel.BuildProgress`, no full construction system migration, no target wrapper deletion.

Automated gates:
- Command: `dotnet build ProceduralRts.csproj --no-restore`
  Result: pass
  Evidence: build completed successfully with 0 warnings and 0 errors.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj buildingtargetpowerconstructionentitystate --no-restore`
  Result: pass
  Evidence: dedicated power/construction EntityWorld-state gate completed with 0 errors and 0 warnings.
- Command: `dotnet run --project tools/CombatBehavior/CombatBehavior.csproj --no-restore`
  Result: pass
  Evidence: CombatBehavior completed with building power/construction projection and production behavior checks intact.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore`
  Result: pass
  Evidence: full ReviewGate completed with 0 errors and 0 warnings after updating older component-rebuild expectations.
- Command: `dotnet run --project tools/VerifyAll/VerifyAll.csproj --no-restore`
  Result: pass
  Evidence: VerifyAll completed all 16 steps successfully, including SimReplay, CombatBehavior, PerfSmoke, BalanceReport, and Godot headless QA.

Reviewer result:
- Status: pass.
- Design note: this removes two more mutable mirrors from the second building runtime and leaves power/construction as component-owned runtime state.
- Required fixes: none identified before gates.

Status:
- Pass.

Residual risks:
- `BuildingModel.Powered` and `BuildingModel.BuildProgress` still exist for legacy GameState paths.
- `UnitBattlefieldBuildingTarget` still mirrors dock, combat target, health, and presentation pulse state.

TODO update:
- Marked done: nested M1 slice `UnitBattlefieldBuildingTarget power/construction EntityWorld cleanup`.
