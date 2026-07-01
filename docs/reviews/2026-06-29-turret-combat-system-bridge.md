Step: Move armed building target combat onto an EntityWorld turret-combat bridge.
Milestone: M1 EntityWorld Becomes Authoritative
Owner AI: Codex
Reviewer AI: Codex review pass
Integrator AI: Codex

Scope:
- Files/folders: `scripts/core/sim/systems/TurretCombatSystem.cs`, `scripts/core/units/runtime/UnitBattlefield.cs`, `scripts/core/entities/BuildingTargetEntityBridge.cs`, `tools/CombatBehavior/Program.cs`, `tools/ReviewGate/Program.cs`, `TODO.md`.
- Added a transitional pure `TurretCombatSystem` that processes only `EntityKind.Turret` entities, auto-acquires hostile targets, advances weapon cooldown/facing, applies damage, and emits sim combat events.
- Routed armed `UnitBattlefieldBuildingTarget` combat through `TurretCombatSystem`, then synced target/cooldown/damage state back to legacy presentation fields during migration.
- Fixed live building weapon target id translation so legacy unit/building ids are mapped to `EntityId` inside `UnitBattlefield`, where the mapping tables exist, while direct EntityWorld bridge tests can still pass already-entity ids.
- Non-goals: no mobile unit combat migration, no projectile presentation rewrite, no balance changes.

Automated gates:
- Command: `dotnet build ProceduralRts.csproj --no-restore`
  Result: pass
  Evidence: build completed successfully with 0 warnings and 0 errors.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj turretcombatsystembridge --no-restore`
  Result: pass
  Evidence: dedicated turret combat bridge gate completed with 0 errors and 0 warnings.
- Command: `dotnet run --project tools/CombatBehavior/CombatBehavior.csproj --no-restore`
  Result: pass
  Evidence: CombatBehavior completed with turret state, economy, enemy AI, and outcome scenarios intact.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore`
  Result: pass
  Evidence: full ReviewGate completed with 0 errors and 0 warnings.
- Command: `dotnet run --project tools/VerifyAll/VerifyAll.csproj --no-restore`
  Result: pass
  Evidence: VerifyAll completed all 15 steps successfully, including SimReplay, CombatBehavior, PerfSmoke, BalanceReport, and Godot headless QA.

Reviewer result:
- Status: pass.
- Design note: the bridge intentionally filters to `EntityKind.Turret` so live mobile unit combat is not double-stepped while M1 migration is incomplete.
- Required fixes: `UnitBattlefield` now performs building attack-target id mapping before stepping turret combat, preventing legacy ids from overwriting EntityWorld weapon state in the live migration path.

Status:
- Pass.

Residual risks:
- `TurretCombatSystem` is transitional and duplicates part of `CombatSystem`; it should be folded back into generic combat once mobile units are fully authoritative in EntityWorld.
- `ApplyTurretCombatEvents` currently drains the shared EntityWorld event sink immediately after turret combat. This is acceptable for the current bridge order, but future systems that emit events in the same update phase should filter or partition event draining.

TODO update:
- Marked done: nested M1 slice `UnitBattlefield building turret CombatSystem bridge`.
- Left open: parent M1 behavior deletion until remaining live `UnitBattlefield` behavior methods are retired.
