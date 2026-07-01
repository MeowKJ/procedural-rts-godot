Step: Move UnitBattlefield refinery dock state into EntityWorld state.
Milestone: M1 EntityWorld Becomes Authoritative
Owner AI: Codex
Reviewer AI: Codex review pass
Integrator AI: Codex

Scope:
- Files/folders: `scripts/core/units/runtime/UnitBattlefieldBuildingTarget.cs`, `scripts/core/units/runtime/UnitBattlefield.cs`, `scripts/core/entities/BuildingTargetEntityBridge.cs`, `scripts/BattleRoot.cs`, `tools/CombatBehavior/Program.cs`, `tools/ReviewGate/Program.cs`, `TODO.md`.
- Removed `DockReservedByHarvesterId` and `DockedHarvesterId` from `UnitBattlefieldBuildingTarget`.
- Added `UnitBattlefield.BuildingDockReservedByHarvesterId(...)` and `UnitBattlefield.BuildingDockedHarvesterId(...)` as EntityWorld-backed legacy-id accessors.
- `SyncBuildingTargetEntity` preserves `DockComponentState` while rebuilding building components.
- `ClearRefineryDockClaim` now mutates `DockComponentState` directly instead of target fields.
- BattleRoot legacy UI fallback now syncs dock state through UnitBattlefield accessors.
- Non-goals: no migration of `DeliveryPulse`, no full deletion of `BuildingModel` dock fields, no target wrapper deletion.

Automated gates:
- Command: `dotnet build ProceduralRts.csproj --no-restore`
  Result: pass
  Evidence: build completed successfully with 0 warnings and 0 errors.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj buildingtargetdockentitystate --no-restore`
  Result: pass
  Evidence: dedicated dock EntityWorld-state gate completed with 0 errors and 0 warnings.
- Command: `dotnet run --project tools/CombatBehavior/CombatBehavior.csproj --no-restore`
  Result: pass
  Evidence: CombatBehavior completed with bridge dock seeding and live harvest dock release checks intact.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore`
  Result: pass
  Evidence: full ReviewGate completed with 0 errors and 0 warnings after updating older dock/live-harvest expectations.
- Command: `dotnet run --project tools/VerifyAll/VerifyAll.csproj --no-restore`
  Result: pass
  Evidence: VerifyAll completed all 16 steps successfully, including SimReplay, CombatBehavior, PerfSmoke, BalanceReport, and Godot headless QA.

Reviewer result:
- Status: pass.
- Design note: this removes refinery dock reservation/occupancy mirrors from the second building runtime while preserving legacy UI compatibility through accessors.
- Required fixes: none identified before gates.

Status:
- Pass.

Residual risks:
- `BuildingModel.DockReservedByHarvesterId` and `BuildingModel.DockedHarvesterId` still exist for legacy GameState paths.
- `DeliveryPulse` still lives on `UnitBattlefieldBuildingTarget` as presentation feedback and should be migrated separately.
- `UnitBattlefieldBuildingTarget` still mirrors combat target and hit/delivery pulse state.

TODO update:
- Marked done: nested M1 slice `UnitBattlefieldBuildingTarget dock EntityWorld cleanup`.
