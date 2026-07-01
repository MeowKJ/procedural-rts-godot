# Review Record - Sim hot-path scratch buffers

Step:
Reuse scratch buffers in hot simulation systems and expose allocation pressure in
PerfSmoke.

Milestone:
M6 Performance.

Owner AI:
Codex main agent.

Reviewer AI:
Codex main-agent self-review; independent reviewer was not spawned because the
current thread reached the subagent limit.

Integrator AI:
Codex main agent.

Scope:
- Files/folders:
  - `scripts/core/SpatialHashAvoidanceMath.cs`
  - `scripts/core/sim/systems/MovementSystem.cs`
  - `scripts/core/sim/systems/VisionSystem.cs`
  - `tools/PerfSmoke/Program.cs`
  - `tools/ReviewGate/Program.cs`
  - `TODO.md`
  - `docs/reviews/2026-06-29-sim-hot-scratch-buffers.md`
- Non-goals:
  - Do not rewrite `CombatSystem` target acquisition in this slice.
  - Do not change authoritative simulation behavior or command semantics.
  - Do not mark the broad sim-step allocation TODO complete.

Implementation summary:
- Added `SpatialHashAvoidanceMath.BuildHashInto()` so callers can reuse a hash and
  avoid the old `BuildHash()` dictionary-copy path.
- `MovementSystem` now reuses `_avoidanceBodies` and `_avoidanceHash` across ticks.
- `VisionSystem` now reuses `_owners`, `_viewers`, and `_viewerGrid` across ticks
  while preserving deterministic sorted owner iteration.
- `PerfSmoke` now reports `alloc/tick` alongside timing metrics.
- Added `ReviewGate simhot` to verify scratch-buffer and allocation-metric hooks.

Automated gates:
- Command:
  `dotnet build ProceduralRts.csproj`
  Result:
  Pass.
  Evidence:
  Build completed with 0 warnings and 0 errors.
- Command:
  `dotnet run --project tools/SimReplay/SimReplay.csproj`
  Result:
  Pass.
  Evidence:
  Movement, combat, authored, group-move, group-attack, and outcome scenarios were
  deterministic with existing metrics intact.
- Command:
  `dotnet run --project tools/PerfSmoke/PerfSmoke.csproj -c Release`
  Result:
  Pass.
  Evidence:
  Worst average was 9.362ms at 400 units under the 16.667ms budget; worst
  allocation was 283829 bytes/tick at 400 units.
- Command:
  `dotnet run --project tools/ReviewGate/ReviewGate.csproj simhot`
  Result:
  Pass.
  Evidence:
  ReviewGate reported 0 errors and 0 warnings.
- Command:
  `dotnet run --project tools/ReviewGate/ReviewGate.csproj review --require-record=sim-hot-scratch-buffers`
  Result:
  Pass.
  Evidence:
  ReviewGate reported 0 errors and 0 warnings.
- Command:
  `dotnet run --project tools/ReviewGate/ReviewGate.csproj`
  Result:
  Pass.
  Evidence:
  ReviewGate reported 0 errors and 0 warnings.

Manual/visual gates:
- Check:
  Runtime battle stress QA.
  Result:
  Not run.
  Evidence:
  This slice changes headless simulation hot paths; runtime visual behavior should
  still be checked when running the game.

Reviewer result:
- Status: pass-with-warnings
- Required fixes:
  - None for this bounded source-level slice.
- Residual risks:
  - Independent reviewer was not available due to subagent limit.
  - `PerfSmoke` still reports high allocation pressure at 400 units, so Combat mount
    list updates, event drain, and other hot-path allocations remain open.
  - Average 400-unit step time remains under budget but did not materially improve
    versus the previous TODO baseline.

TODO update:
- Items marked done:
  - None; the broad sim-step cost item remains open.
- Items left open:
  - `CombatSystem.NearestHostile` broadphase.
  - Combat/event hot-path allocations.
  - Further `OrderedEntities`/`StableEntities` cleanup proof.
- Reason:
  - Evidence covers Movement/Vision scratch-buffer reuse and allocation reporting,
    but not the full simulation-step cost TODO.
