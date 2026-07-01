Step: Move UnitBattlefield building hit and delivery pulses into EntityWorld presentation state.
Milestone: M1 EntityWorld Becomes Authoritative
Owner AI: Codex
Reviewer AI: Codex review pass
Integrator AI: Codex

Scope:
- Files/folders: `scripts/core/units/runtime/UnitBattlefieldBuildingTarget.cs`, `scripts/core/units/runtime/UnitBattlefield.cs`, `scripts/core/entities/BuildingTargetEntityBridge.cs`, `scripts/BattleRoot.cs`, `tools/CombatBehavior/Program.cs`, `tools/ReviewGate/Program.cs`, `TODO.md`.
- Removed `HitPulse` and `DeliveryPulse` from `UnitBattlefieldBuildingTarget`.
- Added `UnitBattlefield.BuildingHitPulse(...)` and `UnitBattlefield.BuildingDeliveryPulse(...)` as EntityWorld-backed accessors.
- Added `SetBuildingHitPulse(...)` and internal delivery/rally pulse helpers that update `PresentationPulseComponentState` without clobbering other pulse channels.
- `SyncBuildingTargetEntity` preserves `PresentationPulseComponentState.CommandPulse`, `.AlertPulse`, and `.HitPulse` while rebuilding building components.
- BattleRoot legacy UI fallback now syncs delivery pulse through `BuildingDeliveryPulse(...)`.
- Non-goals: no migration of unit hit pulses, no removal of legacy `BuildingModel.HitPulse` / `DeliveryPulse`, no combat target/cooldown migration.

Automated gates:
- Command: `dotnet build ProceduralRts.csproj --no-restore`
  Result: pass
  Evidence: build completed successfully with 0 warnings and 0 errors.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj buildingtargetpresentationpulseentitystate --no-restore`
  Result: pass
  Evidence: dedicated presentation pulse EntityWorld-state gate completed with 0 errors and 0 warnings.
- Command: `dotnet run --project tools/CombatBehavior/CombatBehavior.csproj --no-restore`
  Result: pass
  Evidence: CombatBehavior completed with bridge delivery pulse seeding and building hit pulse projection checks intact.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore`
  Result: pass
  Evidence: full ReviewGate completed with 0 errors and 0 warnings after updating older dock projection expectations.
- Command: `dotnet run --project tools/VerifyAll/VerifyAll.csproj --no-restore`
  Result: pass
  Evidence: VerifyAll completed all 16 steps successfully, including SimReplay, CombatBehavior, PerfSmoke, BalanceReport, and Godot headless QA.

Reviewer result:
- Status: pass.
- Design note: this removes two presentation-only mirrors from the second building runtime while keeping the existing projection/view paths intact.
- Required fixes: none identified before gates.

Status:
- Pass.

Residual risks:
- `BuildingModel.HitPulse` and `BuildingModel.DeliveryPulse` still exist for legacy GameState paths.
- `UnitBattlefieldBuildingTarget` still mirrors building combat target/cooldown state.

TODO update:
- Marked done: nested M1 slice `UnitBattlefieldBuildingTarget presentation pulse EntityWorld cleanup`.
