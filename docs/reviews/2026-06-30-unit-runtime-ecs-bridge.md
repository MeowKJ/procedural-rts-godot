# Review Record - UnitBattlefield unit runtime movement ECS bridge

Step: UnitBattlefield unit runtime movement ECS bridge
Milestone: M1 EntityWorld Becomes Authoritative
Owner AI: Codex
Reviewer AI: ReviewGate unitruntimeecsbridge / Integrator
Integrator AI: Codex

Scope:
- Files/folders: `scripts/core/units/runtime/UnitBattlefield.cs`, `scripts/core/sim/systems/MovementSystem.cs`, `scripts/core/sim/systems/ResourceSystem.cs`, `tools/CombatBehavior/Program.cs`, `tools/ReviewGate/Program.cs`, `TODO.md`, `docs/reviews/2026-06-30-unit-runtime-ecs-bridge.md`.
- Non-goals: changing balance values, replacing unit-vs-unit combat, merging `BattleRoot._entityWorld` with `UnitBattlefield.EntityWorld`, changing construction/economy rules, changing art/UI style, or deleting legacy compatibility methods.

Implementation summary:
- `UnitBattlefield.Update` no longer calls legacy per-unit `UpdateMovementIntent(unit, dt)` or `ResolveSoftCollisions(dt)` for live unit runtime motion.
- Added a bounded `UpdateUnitRuntimeMotionFromEntityWorld(dt)` bridge that syncs live units into EntityWorld, steps `MovementSystem` and `SeparationSystem`, then syncs transform/movement/runtime state back to `UnitInstance`.
- Preserved ECS-only `MovementComponentState.FireAnchorRemaining` when legacy compatibility sync writes unit state into EntityWorld, so firing anchors remain stable for local avoidance and separation.
- Updated `MovementSystem` local avoidance to ignore non-blocking collision helpers such as resource nodes after live harvesting proved harvesters could otherwise orbit their target resource.
- Updated `ResourceSystem` to send harvesters toward collidable refinery dock approach points and to include refinery collision radius in dock-arrival checks, keeping buildings blocking while allowing unload.
- `CombatBehavior` proves UnitBattlefield movement and separation advance through EntityWorld and that `UnitInstance`/projection positions match the post-step entity transform.

Automated gates:
- Command: `dotnet build ProceduralRts.csproj --no-restore`
  Result: pass
  Evidence: build completed successfully with 0 warnings and 0 errors.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore -- unitruntimeecsbridge`
  Result: pass
  Evidence: ReviewGate unitruntimeecsbridge completed with 0 errors and 0 warnings.
- Command: `dotnet run --project tools/CombatBehavior/CombatBehavior.csproj --no-restore`
  Result: pass
  Evidence: CombatBehavior passed weapon hit rules, terrain, production/economy, enemy AI, outcomes, and the new EntityWorld movement/separation bridge checks.
- Command: `dotnet run --project tools/SimReplay/SimReplay.csproj --no-restore`
  Result: pass
  Evidence: SimReplay passed deterministic resource, production, construction, ability, movement, combat, group-move, group-attack, and outcome scenarios.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore -- review --require-record=unit-runtime-ecs-bridge`
  Result: pass
  Evidence: ReviewGate review record check completed with 0 errors and 0 warnings.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore`
  Result: pass
  Evidence: full ReviewGate completed with 0 errors and 0 warnings.
- Command: `dotnet run --project tools/VerifyAll/VerifyAll.csproj --no-restore`
  Result: pass
  Evidence: VerifyAll passed all 23 steps, including build, SimReplay, CombatBehavior, SimulationSmoke, FogOfWarQa, SelectionStress, AI/player/sandbox/HUD QA, full ReviewGate, PerfSmoke, BalanceReport, CounterReadabilityQa, and Godot headless scene checks.

Manual/visual gates:
- Check: visual inspection not required for this architecture bridge.
  Result: not run.
  Evidence: no drawing, palette, HUD layout, or camera behavior changed.

Reviewer result:
- Status: pass.
- Required fixes: none known.
- Residual risks: mobile unit-vs-unit combat still runs through the legacy `UnitInstance` path. A later combat convergence slice must prevent double-stepping before switching generic `CombatSystem` on for all live mobile combat.

TODO update:
- Items marked done: `UnitBattlefield unit runtime movement ECS bridge` subitem under M1.
- Items left open: `UnitSpec architecture phase 3 duplicate-data cleanup`, `Migration cleanup`, and final legacy deletion remain open.
- Reason: live movement/separation authority moved to EntityWorld, but combat and several compatibility surfaces remain intentionally staged for later slices.
