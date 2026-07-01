Step: Move UnitBattlefield building production queues into EntityWorld state.
Milestone: M1 EntityWorld Becomes Authoritative
Owner AI: Codex
Reviewer AI: Codex review pass
Integrator AI: Codex

Scope:
- Files/folders: `scripts/core/units/runtime/UnitBattlefieldBuildingTarget.cs`, `scripts/core/units/runtime/UnitBattlefield.cs`, `scripts/core/units/runtime/UnitBattlefieldEnemyProductionAi.cs`, `scripts/core/entities/BuildingTargetEntityBridge.cs`, `tools/CombatBehavior/Program.cs`, `tools/ReviewGate/Program.cs`, `TODO.md`.
- Removed `ProductionQueue` from `UnitBattlefieldBuildingTarget`.
- Added `UnitBattlefield.BuildingProductionQueue(...)` as the read-only EntityWorld-backed queue accessor.
- `SyncBuildingTargetEntity` preserves existing `ProductionQueueComponentState` while rebuilding building components.
- Enemy production AI and CombatBehavior now read UnitBattlefield building queues through EntityWorld state.
- Non-goals: no removal of legacy `BuildingModel.ProductionQueue`, no production event signature migration, no target wrapper deletion.

Automated gates:
- Command: `dotnet build ProceduralRts.csproj --no-restore`
  Result: pass
  Evidence: build completed successfully with 0 warnings and 0 errors.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj buildingtargetproductionqueueentitystate --no-restore`
  Result: pass
  Evidence: dedicated production queue EntityWorld-state gate completed with 0 errors and 0 warnings.
- Command: `dotnet run --project tools/CombatBehavior/CombatBehavior.csproj --no-restore`
  Result: pass
  Evidence: CombatBehavior completed with production enqueue, cancel, completion, selected-building HUD, and enemy production AI checks intact.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore`
  Result: pass
  Evidence: full ReviewGate completed with 0 errors and 0 warnings after updating older production bridge expectations.
- Command: `dotnet run --project tools/VerifyAll/VerifyAll.csproj --no-restore`
  Result: pass
  Evidence: VerifyAll completed all 16 steps successfully, including SimReplay, CombatBehavior, PerfSmoke, BalanceReport, and Godot headless QA.

Reviewer result:
- Status: pass.
- Design note: this removes a high-churn mutable queue mirror from the second building runtime and makes producer queue state component-authoritative.
- Required fixes: none identified before gates.

Status:
- Pass.

Residual risks:
- `BuildingModel.ProductionQueue` still exists for legacy GameState paths.
- `UnitBattlefieldBuildingTarget` still mirrors rally, powered/build progress, dock, pulses, and combat target state.

TODO update:
- Marked done: nested M1 slice `UnitBattlefieldBuildingTarget production queue EntityWorld cleanup`.
