# Review Record - M9 weapon engagement primitives

Step: Extract shared weapon engagement primitives
Milestone: M9 Elegance, Decoupling, Performance
Owner AI: Codex
Reviewer AI: Pauli the 2nd / ReviewGate
Integrator AI: Codex

Scope:
- Files/folders: `scripts/core/sim/WeaponEngagementMath.cs`, `scripts/core/sim/WeaponEngagementQueries.cs`, `scripts/core/sim/WeaponEngagementState.cs`, `scripts/core/sim/WeaponMath.cs`, `scripts/core/sim/systems/CombatSystem.cs`, `scripts/core/sim/systems/combat/`, `scripts/core/sim/systems/TurretCombatSystem.cs`, `scripts/core/sim/systems/BuildingTargetCombatSystem.cs`, `TODO.md`.
- Non-goals: merging `CombatSystem`, `TurretCombatSystem`, and `BuildingTargetCombatSystem` into one live system, adding `BuildingTargetCombatSystem` to `SimSystemPipeline`, changing target acquisition, changing weapon balance, or removing UnitBattlefield migration bridges.

Implementation summary:
- Added `WeaponEngagementMath` for shared cooldown ticking, mount turn-rate lookup, rotate-toward, and aim tolerance.
- Added `WeaponEngagementQueries` for shared target-kind classification and any-mount targetability checks.
- Added `WeaponEngagementState` for shared mount cooldown/write-storage helpers, preserving in-place mobile combat writes and copy-based bridge writes where each system already used them.
- Extended `WeaponMath` with explicit target profile fallback modes, shared targetability, and shared target-priority multiplication.
- Replaced duplicate low-level mount/target/damage logic in `CombatSystem`, `TurretCombatSystem`, and `BuildingTargetCombatSystem` while preserving their behavior boundaries.
- Kept `SimSystemPipeline` unchanged so the transitional building-target bridge cannot double-resolve with generic combat.

Automated gates:
- Command: `dotnet build ProceduralRts.csproj --no-restore`
  Result: pass
  Evidence: main project build completed with 0 warnings and 0 errors.
- Command: `dotnet run --project tools/SimReplay/SimReplay.csproj --no-restore`
  Result: pass
  Evidence: deterministic replay suite completed, including combat, turret, projectile, group attack, and firing-anchor scenarios.
- Command: `dotnet run --project tools/CombatBehavior/CombatBehavior.csproj --no-restore`
  Result: pass
  Evidence: weapon hit rules, turret states, terrain passability, presentation descriptors, rally production, economy, enemy AI, and outcomes passed.
- Command: `dotnet run --project tools/CounterReadabilityQa/CounterReadabilityQa.csproj --no-restore`
  Result: pass
  Evidence: counter cases including AA turret pressure passed.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore -- architecture`
  Result: pass
  Evidence: architecture gate completed with 0 errors.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore -- filesize`
  Result: pass
  Evidence: file-size gate completed with 0 errors and 0 warnings.

Reviewer result:
- Status: pass.
- Required fixes: none.
- Residual risks: full convergence still requires deleting the transitional UnitBattlefield building-target combat bridge and proving one generic weapon engagement scheduler cannot double-step any entity.

TODO update:
- Items marked done: none.
- Items left open: `Converge the three combat systems`.
- Reason: the duplicate low-level rules are now shared, but the final `WeaponEngagementSystem` and bridge deletion are still future work.
