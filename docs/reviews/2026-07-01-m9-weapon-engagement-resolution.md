# Review Record - M9 weapon engagement resolution

Step: Extract shared weapon fire resolution
Milestone: M9 Elegance, Decoupling, Performance
Owner AI: Codex
Reviewer AI: ReviewGate / SimReplay
Integrator AI: Codex

Scope:
- Files/folders: `scripts/core/sim/WeaponEngagementResolution.cs`, `scripts/core/sim/systems/combat/CombatDamageSystem.cs`, `scripts/core/sim/systems/TurretCombatSystem.cs`, `scripts/core/sim/systems/BuildingTargetCombatSystem.cs`, `scripts/core/sim/systems/ProjectileSystem.cs`, `TODO.md`.
- Non-goals: changing target acquisition, changing fire timing, changing projectile behavior, moving shield/retaliation/veterancy damage semantics out of CombatSystem, or merging the three combat schedulers.

Implementation summary:
- Added `WeaponEngagementResolution.Fire(...)` as the shared path for `WeaponFiredEvent` emission, tracking projectile spawn, and immediate damage dispatch.
- Routed `CombatSystem`, `TurretCombatSystem`, and `BuildingTargetCombatSystem` fire paths through `WeaponEngagementResolution`.
- Added `WeaponEngagementResolution.ApplyProjectileImpact(...)` so `ProjectileSystem` no longer calls a projectile-specific CombatSystem wrapper.
- Removed the old projectile spawn helpers from `CombatSystem` while keeping its authoritative shield absorption, retaliation recording, queued destruction, and veterancy award logic intact.
- Preserved per-system behavior boundaries: CombatSystem still applies seeded damage jitter and mobile fire anchors; turret combat still fires at most one mount per tick; building-target combat still does bridge standoff and fire anchoring.

Automated gates:
- Command: `dotnet build ProceduralRts.csproj --no-restore`
  Result: pass
  Evidence: main project build completed with 0 warnings and 0 errors.
- Command: `dotnet run --project tools/SimReplay/SimReplay.csproj --no-restore`
  Result: pass
  Evidence: deterministic replay suite completed, including projectile tracking, turret, combat, group attack, and firing-anchor scenarios.
- Command: `dotnet run --project tools/CombatBehavior/CombatBehavior.csproj --no-restore`
  Result: pass
  Evidence: weapon hit rules, turret states, economy, enemy AI, and outcomes passed.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore -- architecture`
  Result: pass
  Evidence: architecture gate completed with 0 errors.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore -- filesize`
  Result: pass
  Evidence: file-size gate completed with 0 errors and 0 warnings.

Reviewer result:
- Status: pass.
- Required fixes: none.
- Residual risks: this is still a convergence step, not the final unified `WeaponEngagementSystem`; the UnitBattlefield bridge systems remain until double-resolution risk is removed.

TODO update:
- Items marked done: none.
- Items left open: `Converge the three combat systems`.
- Reason: fire resolution is shared, but target scheduling and bridge deletion are still open.
