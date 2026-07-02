# Review Record - M9 UnitBattlefield Placement Query Buffers

Step:
M9 UnitBattlefield placement query buffer reuse (#99)

Milestone:
M9 - Elegance & Decoupling

Owner AI:
Remote Linux Codex

Reviewer AI:
Remote Linux Codex

Integrator AI:
Remote Linux Codex

Scope:
- Files/folders: `scripts/core/units/runtime/UnitBattlefield.cs`, `scripts/core/units/runtime/battlefield/UnitBattlefield.BuildingLifecycle.cs`, `scripts/core/units/runtime/battlefield/UnitBattlefield.EntityWorldSystems.cs`, `tools/ReviewGateRuntime/UnitBattlefieldRuntimeAllocationReviewGate.cs`, `TODO.md`.
- Non-goals: no placement math, build radius, power, terrain, obstacle semantics, ConstructionSystem, ProductionSpawnMath, AI, balance, UI, or visual changes.

Implementation summary:
- Added reusable `PlacementBuildAnchor` and `PlacementObstacle` buffers to `UnitBattlefield`.
- Routed `ValidateBuildingPlacement(...)` through caller-owned placement query fills before calling `PlacementMath.ValidateBuildableArea(...)`.
- Replaced build-anchor and obstacle `Select/Where/ToList` materialization with explicit loops.
- Deleted the unused `SpawnObstacles()` helper from `UnitBattlefield.EntityWorldSystems`.
- Added `ReviewGate simhot` evidence so the old placement list materialization path cannot return.

Automated gates:
- Command: `dotnet build ProceduralRts.csproj --no-restore`
  Result: pass
  Evidence: Debug build succeeded with 0 warnings and 0 errors.
- Command: `dotnet run --project tools/PlayerLoopQa/PlayerLoopQa.csproj --no-restore`
  Result: pass
  Evidence: PlayerLoopQa passed build radius, cat ready-ticket placement, harvest/bank, T1-T3 production, rally, selection, commands, victory, and defeat.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore -- simhot --max-warnings=0`
  Result: pass
  Evidence: ReviewGate simhot passed with 0 errors and 0 warnings.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore`
  Result: pass
  Evidence: Full ReviewGate passed with 0 errors and 0 warnings after syncing exact validation-suite budget evidence.
- Command: `dotnet run --project tools/VerifyAll/VerifyAll.csproj --no-restore`
  Result: pass
  Evidence: VerifyAll passed 23/23, including build, SimReplay, CombatBehavior, ReviewGate, PerfSmoke, and Godot headless QA. `godot-active-battle-perf-qa` emitted a non-failing 2 ObjectDB leaked instances warning.

Manual/visual gates:
- Check: Visual QA
  Result: not applicable
  Evidence: Runtime allocation refactor only; no rendering or UI changed.

Reviewer result:
- Status: pass.
- Required fixes: synced exact validation-suite source-budget evidence after adding the runtime allocation gate checks.
- Residual risks: static `ReviewGate` checks are string-based; `PlayerLoopQa` and `VerifyAll` cover placement behavior. Broader M9 allocation debt remains open under #10.

TODO update:
- Items marked done: none; M9 per-tick allocation paydown remains open.
- Items left open: broader profiler-guided UnitBattlefield and projection allocation cleanup.
- Reason: This closes only the UnitBattlefield placement query buffer child slice.
