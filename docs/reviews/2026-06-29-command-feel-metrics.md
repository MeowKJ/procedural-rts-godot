# Review Record - Command-feel metrics

Step:
Add a full command-feel metric suite to `SimMetrics` and assert movement,
attack, and firing-anchor bands in SimReplay.

Milestone:
M2 Movement Algorithms & Unit Autonomy.

Owner AI:
Codex main agent.

Reviewer AI:
Codex main-agent review with `ReviewGate commandfeelmetrics`, `SimReplay`, and
full `VerifyAll`.

Integrator AI:
Codex main agent.

Scope:
- Files/folders:
  - `scripts/core/sim/SimMetrics.cs`
  - `scripts/core/sim/systems/MovementSystem.cs`
  - `scripts/core/sim/systems/CombatSystem.cs`
  - `scripts/core/sim/systems/SeparationSystem.cs`
  - `tools/SimReplay/Program.cs`
  - `tools/ReviewGate/Program.cs`
  - `TODO.md`
  - `docs/reviews/2026-06-29-command-feel-metrics.md`
- Non-goals:
  - Do not implement flow-field/corridor pathing.
  - Do not redesign autonomy radii or stance behavior.
  - Do not change gameplay authority from metrics; metrics remain diagnostic.

Implementation summary:
- Extended `SimMetrics` with path inflation, corner count, arrival jitter,
  compactness radius, stuck seconds, repath count, target-switch count, and
  anchor-push events.
- `MovementSystem` now records movement samples, idle clears, and zero-jitter
  soft arrivals.
- `CombatSystem` records active attack targets and clears target metrics when no
  target is active.
- `SeparationSystem` records anchor-push events when firing anchors force movers
  to yield.
- SimReplay now asserts command-feel bands in existing group-move, group-attack,
  and firing-anchor scenarios.
- Added `ReviewGate commandfeelmetrics`.

Automated gates:
- Command:
  `dotnet build ProceduralRts.csproj --no-restore`
  Result:
  Pass.
  Evidence:
  Build reported 0 errors and 0 warnings.
- Command:
  `dotnet run --project tools/SimReplay/SimReplay.csproj --no-restore`
  Result:
  Pass.
  Evidence:
  SimReplay reported `OK [command-feel metrics]` with path inflation 1.00,
  corners 2, arrivals 30, and compactness 194.2px.
- Command:
  `dotnet run --project tools/ReviewGate/ReviewGate.csproj commandfeelmetrics --no-restore`
  Result:
  Pass.
  Evidence:
  ReviewGate reported 0 errors and 0 warnings.
- Command:
  `dotnet run --project tools/VerifyAll/VerifyAll.csproj --no-restore`
  Result:
  Pass.
  Evidence:
  VerifyAll passed all 14 steps: build, SimReplay, CombatBehavior,
  SimulationSmoke, FogOfWarQa, SelectionStress, AiDifficultySmoke, ReviewGate,
  PerfSmoke, BalanceReport, and Godot headless QA scenes.

Manual/visual gates:
- Check:
  Visual QA.
  Result:
  Not required.
  Evidence:
  This is a headless deterministic metrics/test slice.

Reviewer result:
- Status: pass.
- Required fixes:
  - None at record creation.
- Residual risks:
  - Flow-field/shared corridor pathing remains open.
  - Autonomy redesign remains open.
  - Metrics are aggregate diagnostics; future per-command debugging views can
    expose them more richly.

TODO update:
- Items marked done:
  - `Full command-feel metric suite in SimMetrics: path inflation, corner count, arrival jitter, compactness, stuck seconds, repath count, target switches, anchor-push events; assert bands in SimReplay`.
- Items left open:
  - Flow-field/shared corridor movement.
  - PathfindingSystem route through LOS/funnel helpers.
  - Autonomy model and deterministic autonomy tests.
- Reason:
  - Current source and replay evidence prove the named metric fields exist, are fed
    by systems, and are asserted in deterministic movement/combat feel scenarios.
