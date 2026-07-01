# Review Record - EntityWorld system timing

Step:
Add debug-only per-system step timing inside `EntityWorld.Step`.

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
  - `scripts/core/entities/EntityWorld.cs`
  - `scripts/core/sim/SimMetrics.cs`
  - `scripts/core/sim/SimSystemTiming.cs`
  - `tools/SimReplay/Program.cs`
  - `tools/ReviewGate/Program.cs`
  - `TODO.md`
  - `docs/reviews/2026-06-29-entityworld-system-timing.md`
- Non-goals:
  - Do not enable timing by default.
  - Do not affect deterministic state hash or gameplay authority.
  - Do not build the in-engine PerfHud in this slice.

Implementation summary:
- Added `SimSystemTiming` samples/total/last/max/average data.
- `SimMetrics` now records per-system timing samples.
- `EntityWorld.SystemTimingEnabled` defaults from `PROCEDURAL_RTS_SIM_TIMING=1`
  and wraps each system with `Stopwatch` only when enabled.
- `SimReplay` asserts timing is off by default and records a `CommandSystem` sample
  when enabled.
- Added `ReviewGate timing` to verify the debug flag, metrics, and test hooks.

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
  SimReplay reported `OK [system-timing]: debug per-system metrics recorded only
  when enabled.` and all deterministic scenarios passed.
- Command:
  `dotnet run --project tools/PerfSmoke/PerfSmoke.csproj -c Release`
  Result:
  Pass.
  Evidence:
  Worst average was 1.203ms at 400 units under the 16.667ms budget; allocation was
  188125 bytes/tick at 400 units.
- Command:
  `dotnet run --project tools/ReviewGate/ReviewGate.csproj timing`
  Result:
  Pass.
  Evidence:
  ReviewGate reported 0 errors and 0 warnings.
- Command:
  `dotnet run --project tools/ReviewGate/ReviewGate.csproj review --require-record=entityworld-system-timing`
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
  This slice records metrics but does not expose them in the UI.

Reviewer result:
- Status: pass-with-warnings
- Required fixes:
  - None for this bounded source-level slice.
- Residual risks:
  - Independent reviewer was not available due to subagent limit.
  - Timing is diagnostic and non-deterministic by nature; it stays out of the state
    hash and must remain debug-only.

TODO update:
- Items marked done:
  - None; PerfHud and presentation metrics remain open.
- Items left open:
  - In-engine PerfHud.
  - PresentationMetrics rolling averages / 1%-low.
- Reason:
  - Evidence proves per-system timing instrumentation, not all instrumentation work.
