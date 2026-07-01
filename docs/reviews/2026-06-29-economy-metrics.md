# Review Record - Economy metrics

Step:
Add ResourceSystem-driven economy metrics to `SimMetrics`.

Milestone:
M4 Production & Economy System / Design Reference - Resource, Mining &
Environment Regeneration.

Owner AI:
Codex main agent.

Reviewer AI:
Codex main-agent review with `ReviewGate economymetrics`, `SimReplay`, and
full `VerifyAll`.

Integrator AI:
Codex main agent.

Scope:
- Files/folders:
  - `scripts/core/sim/SimMetrics.cs`
  - `scripts/core/sim/systems/ResourceSystem.cs`
  - `tools/SimReplay/Program.cs`
  - `tools/ReviewGate/Program.cs`
  - `TODO.md`
  - `docs/reviews/2026-06-29-economy-metrics.md`
- Non-goals:
  - Do not implement environment regeneration.
  - Do not migrate live UI to display economy metrics.
  - Do not claim all economy tuning data is centralized.

Implementation summary:
- Added read-only economy counters to `SimMetrics`: banked credits,
  credits-per-minute, harvester idle time, active trip time, average trip time,
  dock wait time, refinery congestion events, and completed resource trips.
- `ResourceSystem` now records economy elapsed time, idle/active harvester time,
  dock wait/congestion, banked credits, and completed delivery trips.
- `SimReplay` now asserts resource-loop throughput metrics and a separate
  one-dock/two-harvester congestion scenario.
- Added `ReviewGate economymetrics`.

Automated gates:
- Command:
  `dotnet build ProceduralRts.csproj --no-restore`
  Result:
  Pass.
  Evidence:
  Build completed with 0 warnings and 0 errors.
- Command:
  `dotnet run --project tools/SimReplay/SimReplay.csproj --no-restore`
  Result:
  Pass.
  Evidence:
  SimReplay asserted credits-per-minute, idle time, average resource trip time,
  dock wait time, refinery congestion events, and banked credits.
- Command:
  `dotnet run --project tools/ReviewGate/ReviewGate.csproj economymetrics --no-restore`
  Result:
  Pass.
  Evidence:
  ReviewGate reported 0 errors and 0 warnings.
- Command:
  `dotnet run --project tools/ReviewGate/ReviewGate.csproj review --require-record=economy-metrics --no-restore`
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
  HUD display of economy metrics.
  Result:
  Not required.
  Evidence:
  This slice adds headless sim metrics only; UI presentation remains future work.

Reviewer result:
- Status: pass.
- Required fixes:
  - None at record creation.
- Residual risks:
  - Metrics are global read-only counters today; owner-specific dashboards may need
    owner breakdowns later.
  - Environment regeneration and full economy tuning data remain open.

TODO update:
- Items marked done:
  - `Economy metrics in SimMetrics: credits-per-minute, harvester idle time, dock wait time, resource trip time, refinery congestion`.
- Items left open:
  - Economy regeneration.
  - Centralized economy tuning/balance config.
  - Full deterministic economy/production tests covering regeneration.
- Reason:
  - Current code and replay evidence prove all named economy metrics are recorded
    from the pure EntityWorld resource path.
