# Review Record - Presentation metrics

Step:
Add rolling presentation frame metrics and 1% low frame-time instrumentation.

Milestone:
M6 Performance.

Owner AI:
Codex main agent.

Reviewer AI:
Codex main-agent self-review; independent reviewer was not spawned because the
current thread has been operating at the subagent limit.

Integrator AI:
Codex main agent.

Scope:
- Files/folders:
  - `scripts/core/PresentationMetrics.cs`
  - `scripts/BattleRoot.cs`
  - `tools/SimReplay/Program.cs`
  - `tools/ReviewGate/Program.cs`
  - `TODO.md`
  - `docs/reviews/2026-06-29-presentation-metrics.md`
- Non-goals:
  - Do not build the visible in-engine PerfHud in this slice.
  - Do not estimate true GPU render time; this slice records frame delta,
    process-frame cost, and EntityWorld shadow-step cost.
  - Do not change gameplay simulation authority.

Implementation summary:
- Added `PresentationMetrics` with fixed rolling capacity, average frame/process/
  sim-step milliseconds, and worst-1% frame-time / FPS snapshot values.
- `BattleRoot` now records `_Process` frame delta, measured process cost, and
  measured EntityWorld shadow-step cost into presentation metrics every frame.
- `SimReplay` now asserts rolling-window eviction and 1% low spike detection.
- Added `ReviewGate presentationmetrics` to verify metrics, BattleRoot hooks, and
  replay coverage.

Automated gates:
- Command:
  `dotnet build ProceduralRts.csproj`
  Result:
  Pass.
  Evidence:
  Build completed with 0 warnings and 0 errors after rerunning sequentially; the
  earlier parallel build attempt hit a temporary Godot DLL write lock.
- Command:
  `dotnet run --project tools/SimReplay/SimReplay.csproj`
  Result:
  Pass.
  Evidence:
  SimReplay reported `OK [presentation-metrics]: rolling averages and 1% low frame
  time recorded.` and all deterministic scenarios passed.
- Command:
  `dotnet run --project tools/ReviewGate/ReviewGate.csproj presentationmetrics`
  Result:
  Pass.
  Evidence:
  ReviewGate reported 0 errors and 0 warnings.
- Command:
  `dotnet run --project tools/PerfSmoke/PerfSmoke.csproj -c Release`
  Result:
  Pass.
  Evidence:
  Worst average was 1.170ms at 400 units under the 16.667ms budget; allocation was
  188125 bytes/tick at 400 units.
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
  This slice creates the metrics source; visible HUD rendering remains a separate
  TODO item.

Reviewer result:
- Status: pass-with-warnings
- Required fixes:
  - None for this bounded instrumentation slice.
- Residual risks:
  - Independent reviewer was not available.
  - GPU render timing is not measured yet; Godot frame delta and `_Process`
    Stopwatch timings are enough for the next PerfHud slice but not a full renderer
    profiler.

TODO update:
- Items marked done:
  - Per-system step ms inside `EntityWorld.Step` behind a debug flag.
  - `PresentationMetrics`: rolling averages + 1%-low frame time.
- Items left open:
  - In-engine `PerfHud` overlay.
  - Render/GPU timing and fog update display in that HUD.
- Reason:
  - Automated gates prove the data source and spike-sensitive rolling metric, but
    no visible HUD has been implemented in this slice.
