Step: Move UnitBattlefield building rally point and rally pulse into EntityWorld state.
Milestone: M1 EntityWorld Becomes Authoritative
Owner AI: Codex
Reviewer AI: Codex review pass
Integrator AI: Codex

Scope:
- Files/folders: `scripts/core/units/runtime/UnitBattlefieldBuildingTarget.cs`, `scripts/core/units/runtime/UnitBattlefield.cs`, `scripts/core/units/runtime/UnitBattlefieldEnemyProductionAi.cs`, `scripts/core/entities/BuildingTargetEntityBridge.cs`, `scripts/BattleRoot.cs`, `tools/CombatBehavior/Program.cs`, `tools/ReviewGate/Program.cs`, `TODO.md`.
- Removed `RallyPoint` and `RallyPulse` from `UnitBattlefieldBuildingTarget`.
- Added `UnitBattlefield.BuildingRallyPoint(...)` and `UnitBattlefield.BuildingRallyPulse(...)` as EntityWorld-backed rally accessors.
- `SyncBuildingTargetEntity` preserves `RallyPointComponentState` and `PresentationPulseComponentState.CommandPulse` while rebuilding building components.
- BattleRoot legacy UI fallback now syncs rally state from `BuildingPresentationProjection`.
- Enemy production AI and CombatBehavior now read rally through UnitBattlefield EntityWorld state.
- Non-goals: no removal of legacy `BuildingModel.RallyPoint` / `BuildingModel.RallyPulse`, no dock/combat pulse migration, no target wrapper deletion.

Automated gates:
- Command: `dotnet build ProceduralRts.csproj --no-restore`
  Result: pass
  Evidence: build completed successfully with 0 warnings and 0 errors.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj buildingtargetrallyentitystate --no-restore`
  Result: pass
  Evidence: dedicated rally EntityWorld-state gate completed with 0 errors and 0 warnings.
- Command: `dotnet run --project tools/CombatBehavior/CombatBehavior.csproj --no-restore`
  Result: pass
  Evidence: CombatBehavior completed with rally production, selected-building rally input/projection, enemy production AI, and outcomes intact.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore`
  Result: pass
  Evidence: full ReviewGate completed with 0 errors and 0 warnings after updating older rally/input/projection gate expectations.
- Command: `dotnet run --project tools/VerifyAll/VerifyAll.csproj --no-restore`
  Result: pass
  Evidence: VerifyAll completed all 16 steps successfully, including SimReplay, CombatBehavior, PerfSmoke, BalanceReport, and Godot headless QA.

Reviewer result:
- Status: pass.
- Design note: this removes the rally mutable mirror from the second building runtime and keeps command feedback in the presentation pulse component.
- Required fixes: none identified before gates.

Status:
- Pass.

Residual risks:
- `BuildingModel.RallyPoint` and `BuildingModel.RallyPulse` still exist for legacy GameState paths.
- `UnitBattlefieldBuildingTarget` still mirrors powered/build progress, dock, combat target, and non-rally pulse state.

TODO update:
- Marked done: nested M1 slice `UnitBattlefieldBuildingTarget rally EntityWorld cleanup`.
