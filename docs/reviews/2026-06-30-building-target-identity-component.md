# Review Record - UnitBattlefield building identity component cleanup

Step: Migration cleanup building identity component slice
Milestone: M1 EntityWorld Becomes Authoritative
Owner AI: Codex
Reviewer AI: ReviewGate buildingtargetidentitycomponent / Integrator
Integrator AI: Codex

Scope:
- Files/folders: `scripts/core/entities/EntityComponentState.cs`, `scripts/core/entities/EntityStateHash.cs`, `scripts/core/entities/BuildingTargetEntityBridge.cs`, `scripts/core/units/runtime/UnitBattlefield.cs`, `tools/CombatBehavior/Program.cs`, `tools/ReviewGate/Program.cs`, `TODO.md`, `docs/reviews/2026-06-30-building-target-identity-component.md`.
- Non-goals: deleting `UnitBattlefieldBuildingTarget`, removing public target APIs, changing building balance, changing construction command shape, or changing visual style.

Implementation summary:
- Added `BuildingIdentityComponentState` carrying legacy building id, `BuildingKind`, `PlayerSlotId`, and `UnitFactionId`.
- Added deterministic hashing for building identity fields.
- `BuildingTargetEntityBridge` now writes identity from `BuildingEntitySeed`.
- `UnitBattlefield` adds identity fallback lookup, stamps identity onto adopted constructed buildings, and uses identity for building view, selection, hover, hit-pulse, and minimap projections.
- `CombatBehavior` now proves bridge-created and live mirrored building entities contain the expected identity component.

Automated gates:
- Command: `dotnet build ProceduralRts.csproj --no-restore`
  Result: pass
  Evidence: build completed successfully with 0 warnings and 0 errors.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore -- buildingtargetidentitycomponent`
  Result: pass
  Evidence: ReviewGate buildingtargetidentitycomponent completed with 0 errors and 0 warnings.
- Command: `dotnet run --project tools/CombatBehavior/CombatBehavior.csproj --no-restore`
  Result: pass
  Evidence: CombatBehavior passed weapon hit rules, turret states, terrain passability, localization fallback, presentation descriptors, shared threat propagation, rally production, economy, enemy AI, and outcomes.
- Command: `dotnet run --project tools/SimReplay/SimReplay.csproj --no-restore`
  Result: pass
  Evidence: SimReplay passed; `m5-turret-entities` remained deterministic with identity in the state hash.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore -- review --require-record=building-target-identity-component`
  Result: pass
  Evidence: review record gate completed with 0 errors and 0 warnings.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore`
  Result: pass
  Evidence: full ReviewGate completed with 0 errors and 0 warnings.
- Command: `dotnet run --project tools/VerifyAll/VerifyAll.csproj --no-restore`
  Result: pass
  Evidence: VerifyAll passed all 23 steps after the identity and public-surface building migration slices.

Manual/visual gates:
- Check: visual inspection not required for this identity/projection data cleanup.
  Result: not run.
  Evidence: no drawing code, palette, or layout behavior changed.

Reviewer result:
- Status: pass
- Required fixes: none before automated verification.
- Residual risks: `UnitBattlefieldBuildingTarget` remains a public runtime handle; this slice only moves identity data into EntityWorld so later public-surface cleanup can use projections/components.

TODO update:
- Items marked done: `UnitBattlefield building identity component cleanup` subitem under Migration cleanup.
- Items left open: parent Migration cleanup remains open.
- Reason: identity is component-owned, but external callers still accept/return `UnitBattlefieldBuildingTarget`.
