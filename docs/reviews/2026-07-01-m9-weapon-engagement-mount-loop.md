# Review Record - M9 weapon engagement mount loop

Step: Extract shared weapon mount loop
Milestone: M9 Elegance, Decoupling, Performance
Owner AI: Codex
Reviewer AI: ReviewGate / SimReplay
Integrator AI: Codex

Scope:
- Files/folders: `scripts/core/sim/WeaponEngagementMountLoop.cs`, `scripts/core/sim/systems/combat/CombatEngagementSystem.cs`, `scripts/core/sim/systems/combat/CombatDamageSystem.cs`, `scripts/core/sim/systems/TurretCombatSystem.cs`, `scripts/core/sim/systems/BuildingTargetCombatSystem.cs`, `TODO.md`.
- Non-goals: merging combat schedulers, changing target acquisition, changing movement/chase logic, changing projectile behavior, or adding `BuildingTargetCombatSystem` to the live pipeline.

Implementation summary:
- Added `WeaponEngagementMountLoop.Tick(...)` to own the repeated mount iteration: cooldown tick, mount facing update, aimed check, target legality, min-range option, damage mode, shared fire resolution, and mount state writeback.
- Added `WeaponEngagementMountLoopOptions` so behavior differences are explicit data: mobile seeded variance, min-range enforcement, structure-aware target priority, turret one-shot-per-tick policy, and fire-anchor policy.
- Routed `CombatSystem`, `TurretCombatSystem`, and `BuildingTargetCombatSystem` through the shared loop while preserving their different scheduling and movement semantics.
- Removed the mobile-only `FireShot(...)` and local damage jitter helper from `CombatDamageSystem`; the shared loop now applies seeded variance only when the mobile combat path requests it.

Automated gates:
- Command: `dotnet build ProceduralRts.csproj --no-restore`
  Result: pass
  Evidence: main project build completed with 0 warnings and 0 errors.
- Command: `dotnet run --project tools/SimReplay/SimReplay.csproj --no-restore`
  Result: pass
  Evidence: deterministic replay suite completed with the same combat, turret, projectile, group attack, and firing-anchor scenario outputs.
- Command: `dotnet run --project tools/CombatBehavior/CombatBehavior.csproj --no-restore`
  Result: pass
  Evidence: weapon hit rules, turret states, economy, enemy AI, and outcomes passed.
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
- Residual risks: this is still not the final `WeaponEngagementSystem`; target scheduling and UnitBattlefield bridge deletion remain open.

TODO update:
- Items marked done: none.
- Items left open: `Converge the three combat systems`.
- Reason: the rotate/fire/cooldown loop is shared, but the three schedulers still exist while M1 bridge risk remains.
