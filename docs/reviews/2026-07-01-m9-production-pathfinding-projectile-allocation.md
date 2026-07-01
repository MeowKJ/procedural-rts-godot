# Review Record - M9 production/pathfinding/projectile allocation paydown

Step: Reuse hot-path production, pathfinding, and projectile buffers
Milestone: M9 Elegance, Decoupling, Performance
Owner AI: Codex
Reviewer AI: ReviewGate simhot / SimReplay
Integrator AI: Codex

Scope:
- Files/folders: `scripts/core/sim/systems/ProductionSystem.cs`, `scripts/core/sim/systems/production/ProductionSystem.Spawning.cs`, `scripts/core/production/ProductionSpawnMath.cs`, `scripts/core/sim/systems/PathfindingSystem.cs`, `scripts/core/sim/systems/ProjectileSystem.cs`, `tools/ReviewGateDomains/RegressionReviewGate.cs`, `TODO.md`.
- Non-goals: changing production timing, changing spawn-point selection order, changing pathfinding algorithms, changing projectile hit rules, closing all remaining allocation debt, or adding GUI-only QA.

Implementation summary:
- Replaced `ProductionSystem`'s per-tick `world.OrderedEntities.ToList()` producer snapshot with `_producerStepBuffer`, preserving the existing spawn-and-rally semantics without mutating the entity collection during iteration.
- Replaced production spawn obstacle LINQ allocation with `_spawnObstacles`, and moved spawn/rally placement logic into `ProductionSystem.Spawning.cs` to keep file-size governance green.
- Changed `ProductionSpawnMath` candidate direction and ring scale data to static arrays instead of allocating a direction list per spawn.
- Reused `PathfindingSystem` shared-corridor planned/group/member/assignment and blocker de-duplication buffers instead of rebuilding those collections per tick.
- Removed `ProjectileSystem`'s per-tick `OrderedEntities.ToArray()` snapshot; projectile stepping only queues removals and does not mutate the live entity collection during iteration.
- Added `ReviewGate simhot` evidence in the regression domain for these hooks.

Automated gates:
- Command: `dotnet build ProceduralRts.csproj --no-restore`
  Result: pass
  Evidence: main project build completed with 0 warnings and 0 errors under `DOTNET_ROLL_FORWARD=Major`.
- Command: `dotnet run --project tools/SimReplay/SimReplay.csproj --no-restore`
  Result: pass
  Evidence: deterministic replay suite passed under `DOTNET_ROLL_FORWARD=Major`, including production-loop, entity-shared-corridor, projectile-tracking, group movement, combat, and outcome scenarios.
- Command: `dotnet run --project tools/CombatBehavior/CombatBehavior.csproj --no-restore`
  Result: pass
  Evidence: weapon hit rules, turret states, rally production, economy, enemy AI, and outcomes passed under `DOTNET_ROLL_FORWARD=Major`.
- Command: `dotnet run --project tools/PerfSmoke/PerfSmoke.csproj --no-restore`
  Result: pass
  Evidence: 400-unit run under `DOTNET_ROLL_FORWARD=Major` averaged 11.173ms with p99 11.670ms and 192620 bytes/tick, under the 16.667ms active budget.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore -- simhot --max-warnings=0`
  Result: pass
  Evidence: ReviewGate completed with 0 errors and 0 warnings under `DOTNET_ROLL_FORWARD=Major`.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore -- regression --max-warnings=0`
  Result: pass
  Evidence: ReviewGate completed with 0 errors and 0 warnings under `DOTNET_ROLL_FORWARD=Major`.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore -- filesize --max-warnings=0`
  Result: pass
  Evidence: file-size gate passed under `DOTNET_ROLL_FORWARD=Major` after splitting `ProductionSystem.Spawning.cs` and syncing the validation-tools source budget string.
- Command: `sh tools/verify-all.sh --skip-perf`
  Result: pass
  Evidence: full grouped verification passed 22/22 under `GODOT_BIN=/usr/local/bin/godot-dotnet` and `DOTNET_ROLL_FORWARD=Major`, including Godot battle, display settings, skirmish flow, active battle perf, and pause headless QA.

Manual/visual gates:
- Check: GUI visual QA
  Result: not run
  Evidence: this was a Godot-free simulation allocation slice; no GUI rendering path changed.

Reviewer result:
- Status: pass.
- Required fixes: none.
- Residual risks: remaining allocation debt still includes Construction/Command paths, immutable queue/path array replacement, placement-list construction, and profiler-guided GC cleanup. PerfSmoke remains time-green but still reports 192620 bytes/tick at 400 units.

TODO update:
- Items marked done: none.
- Items left open: `Per-tick allocation paydown`.
- Reason: this removes and locks one production/pathfinding/projectile allocation family, but the broad M9 allocation item is not fully paid down.
