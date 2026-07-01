Step: Move UnitBattlefield building weapon target and cooldown state into EntityWorld weapon state.
Milestone: M1 EntityWorld Becomes Authoritative
Owner AI: Codex
Reviewer AI: Codex review pass, informed by Wegener read-only audit
Integrator AI: Codex

Scope:
- Files/folders: `scripts/core/units/runtime/UnitBattlefieldBuildingTarget.cs`, `scripts/core/units/runtime/UnitBattlefield.cs`, `scripts/core/entities/BuildingTargetEntityBridge.cs`, `tools/CombatBehavior/Program.cs`, `tools/ReviewGate/Program.cs`, `TODO.md`.
- Removed `AttackTargetId`, `AttackTargetKind`, and `AttackCooldownRemaining` from `UnitBattlefieldBuildingTarget`.
- Added `UnitBattlefield.BuildingAttackTargetId(...)`, `BuildingAttackTargetKind(...)`, and `BuildingAttackCooldownRemaining(...)` as EntityWorld-backed accessors.
- `SyncBuildingTargetEntity` preserves existing `WeaponUserComponentState` while rebuilding building components.
- `BuildingTargetEntityBridge` now initializes weapon components from explicit preserved `WeaponUserComponentState` or a neutral default; it no longer reads attack target/cooldown state from the wrapper.
- Building target cleanup now clears `WeaponUserComponentState.AttackTarget` instead of writing wrapper fields.
- Non-goals: deleting `UnitBattlefieldBuildingTarget`, migrating mobile unit `UnitInstance` weapon mirrors, deleting legacy `GameState` / `BuildingModel` combat fields.

Automated gates:
- Command: `dotnet build ProceduralRts.csproj --no-restore`
  Result: pass
  Evidence: build completed successfully with 0 warnings and 0 errors.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj buildingtargetweaponuserentitystate --no-restore`
  Result: pass
  Evidence: dedicated building weapon-user EntityWorld-state gate completed with 0 errors and 0 warnings.
- Command: `dotnet run --project tools/CombatBehavior/CombatBehavior.csproj --no-restore`
  Result: pass
  Evidence: CombatBehavior completed with armed building acquisition, damage, destruction, and target clearing checks intact.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore`
  Result: pass
  Evidence: full ReviewGate completed with 0 errors and 0 warnings after updating turret bridge expectations.
- Command: `dotnet run --project tools/VerifyAll/VerifyAll.csproj --no-restore`
  Result: pass
  Evidence: VerifyAll completed all 16 steps successfully, including SimReplay, CombatBehavior, PerfSmoke, BalanceReport, and Godot headless QA.

Reviewer result:
- Status: pass.
- Design note: this removes the final combat-state mirrors from the second building runtime while leaving static BuildSpec-backed convenience properties in place for the remaining migration.
- Required fixes: none identified after gates.

Status:
- Pass.

Residual risks:
- `UnitBattlefieldBuildingTarget` still exists as a transitional wrapper for id, kind, owner, position, facing, HP, and BuildSpec-backed convenience properties.
- Legacy `GameState` / `BuildingModel` still own separate combat fields outside the UnitBattlefield entity migration.
- `BuildingPresentationCatalog`, `BuildingKind`, and other legacy catalogs remain open under the next deletion milestone.

TODO update:
- Marked done: nested M1 slice `UnitBattlefieldBuildingTarget weapon user EntityWorld cleanup`.
