# Review Record - BuildingTargetEntityBridge seed bridge cleanup

Step: Migration cleanup BuildingTargetEntityBridge seed bridge slice
Milestone: M1 EntityWorld Becomes Authoritative
Owner AI: Codex
Reviewer AI: ReviewGate buildingtargetseedbridge / Integrator
Integrator AI: Codex

Scope:
- Files/folders: `scripts/core/entities/BuildingEntitySeed.cs`, `scripts/core/entities/BuildingTargetEntityBridge.cs`, `scripts/core/units/runtime/UnitBattlefieldBuildingTarget.cs`, `scripts/core/units/runtime/UnitBattlefield.cs`, `tools/CombatBehavior/Program.cs`, `tools/SimReplay/Program.cs`, `tools/ReviewGate/Program.cs`, `TODO.md`, `docs/reviews/2026-06-30-building-target-seed-bridge.md`.
- Non-goals: deleting `UnitBattlefieldBuildingTarget`, changing building HP/position/facing behavior, changing construction methods, changing building balance, or rewriting selection/repair APIs.

Implementation summary:
- Added `BuildingEntitySeed` as the immutable runtime seed for building entity creation.
- Added `UnitBattlefieldBuildingTarget.ToEntitySeed()` so the migration wrapper exports seed data explicitly.
- Changed `BuildingTargetEntityBridge` spawn/component helpers to accept `BuildingEntitySeed` instead of `UnitBattlefieldBuildingTarget`.
- Updated `UnitBattlefield`, `CombatBehavior`, and `SimReplay` fixtures to call the seed-backed bridge.
- Added `ReviewGate buildingtargetseedbridge` and updated existing BuildSpec bridge gates to prevent bridge dependencies on the second building runtime wrapper from returning.

Automated gates:
- Command: `dotnet build ProceduralRts.csproj --no-restore`
  Result: pass
  Evidence: build completed successfully with 0 warnings and 0 errors.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore -- buildingtargetseedbridge`
  Result: pass
  Evidence: ReviewGate buildingtargetseedbridge completed with 0 errors and 0 warnings.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore -- buildingtargetbridgebuildspeconly`
  Result: pass
  Evidence: ReviewGate buildingtargetbridgebuildspeconly completed with 0 errors and 0 warnings.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore -- buildingtargetcomponentsspecdirect`
  Result: pass
  Evidence: ReviewGate buildingtargetcomponentsspecdirect completed with 0 errors and 0 warnings.
- Command: `dotnet run --project tools/CombatBehavior/CombatBehavior.csproj --no-restore`
  Result: pass
  Evidence: CombatBehavior passed weapon hit rules, turret states, terrain passability, localization fallback, presentation descriptors, shared threat propagation, rally production, economy, enemy AI, and outcomes.
- Command: `dotnet run --project tools/SimReplay/SimReplay.csproj --no-restore`
  Result: pass
  Evidence: SimReplay passed, including `m5-turret-entities` deterministic coverage.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore -- review --require-record=building-target-seed-bridge`
  Result: pass
  Evidence: required review record check completed with 0 errors and 0 warnings.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore`
  Result: pass
  Evidence: full ReviewGate completed with 0 errors and 0 warnings.
- Command: `dotnet run --project tools/VerifyAll/VerifyAll.csproj --no-restore`
  Result: pass
  Evidence: VerifyAll completed 23/23 checks, including build, SimReplay, CombatBehavior, SimulationSmoke, FogOfWarQa, ReviewGate, PerfSmoke, Godot battle headless, active battle perf, skirmish flow, and pause QA.

Manual/visual gates:
- Check: visual inspection not required for this bridge API cleanup.
  Result: not run.
  Evidence: no drawing, palette, or layout behavior changed.

Reviewer result:
- Status: pass
- Required fixes: none before automated verification.
- Residual risks: `UnitBattlefieldBuildingTarget` still exists as a migration wrapper for building identity, transform, and HP; this slice only removes it from the entity creation bridge.

TODO update:
- Items marked done: `BuildingTargetEntityBridge seed bridge cleanup` subitem under Migration cleanup.
- Items left open: parent Migration cleanup remains open.
- Reason: the bridge is wrapper-independent, but the live building runtime wrapper and its public API still exist.
