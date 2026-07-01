# Review Record - SimClock backlog metrics

Step:
Expose metrics when the fixed-step clock drops backlog because of the catch-up cap.

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
  - `scripts/core/sim/SimClock.cs`
  - `scripts/core/sim/SimMetrics.cs`
  - `scripts/BattleRoot.cs`
  - `tools/SimReplay/Program.cs`
  - `tools/ReviewGate/Program.cs`
  - `TODO.md`
  - `docs/reviews/2026-06-29-simclock-backlog-metrics.md`
- Non-goals:
  - Do not change the fixed tick rate or the catch-up cap behavior.
  - Do not add an in-engine PerfHud display in this slice.
  - Do not use metrics to alter simulation authority.

Implementation summary:
- `SimClock` now tracks total and last dropped backlog events/ticks/seconds when
  `_accumulator` exceeds the per-advance catch-up cap.
- `SimMetrics.RecordClockBacklogDrop()` accumulates the metric as read-only
  diagnostic data.
- `BattleRoot.StepEntityWorld()` records the clock's last backlog drop after
  `Advance()`.
- `SimReplay` now asserts hitch-frame cap behavior and metric propagation.
- Added `ReviewGate simclock` to verify the clock/metrics/driver/test hooks.

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
  SimReplay reported `OK [sim-clock]: backlog cap metrics recorded.` and all
  deterministic scenarios passed.
- Command:
  `dotnet run --project tools/ReviewGate/ReviewGate.csproj simclock`
  Result:
  Pass.
  Evidence:
  ReviewGate reported 0 errors and 0 warnings.
- Command:
  `dotnet run --project tools/ReviewGate/ReviewGate.csproj review --require-record=simclock-backlog-metrics`
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
  In-engine PerfHud display.
  Result:
  Not run.
  Evidence:
  This slice records metrics but does not expose them in a HUD.

Reviewer result:
- Status: pass-with-warnings
- Required fixes:
  - None for this bounded source-level slice.
- Residual risks:
  - Independent reviewer was not available due to subagent limit.
  - Metrics are collected but not yet visible to the player/developer in-engine.
  - Dropped backlog seconds include fractional leftover seconds cleared by the cap;
    this is intended as diagnostic magnitude, not an authoritative tick count.

TODO update:
- Items marked done:
  - None; broad instrumentation work remains open.
- Items left open:
  - In-engine PerfHud overlay.
  - Presentation metrics and per-system step timings.
- Reason:
  - Evidence proves SimClock backlog-drop metrics, not the whole performance
    instrumentation milestone.
