# Review Record - SimEvent drain buffer

Step:
Reduce simulation hot-path allocation by draining sim events into a reusable
buffer instead of allocating snapshot arrays every tick.

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
  - `scripts/core/sim/SimEvent.cs`
  - `scripts/BattleRoot.cs`
  - `tools/PerfSmoke/Program.cs`
  - `tools/SimReplay/Program.cs`
  - `tools/ReviewGate/Program.cs`
  - `TODO.md`
  - `docs/reviews/2026-06-29-simevent-drain-buffer.md`
- Non-goals:
  - Do not remove the existing `Drain()` snapshot API; tools/tests still use it.
  - Do not claim the whole simulation allocation TODO is complete.
  - Do not change event ordering or deterministic simulation state.

Implementation summary:
- Added `SimEventSink.DrainInto(List<SimEvent>)`, which clears and fills a
  caller-owned reusable event buffer.
- `BattleRoot.StepEntityWorld` now drains EntityWorld events into a persistent
  `_simEventDrainBuffer` before feeding `SimMetrics`.
- `PerfSmoke` measures the reusable drain path.
- `SimReplay` verifies destination clearing, pending-event transfer, and sink
  emptying after drain.
- `ReviewGate simhot` now checks that the reusable event drain exists and is used
  by BattleRoot/PerfSmoke with replay coverage.

Automated gates:
- Command:
  `dotnet build ProceduralRts.csproj --no-restore`
  Result:
  Pass.
  Evidence:
  Build completed with 0 warnings and 0 errors.
- Command:
  `dotnet run --project tools/ReviewGate/ReviewGate.csproj simhot`
  Result:
  Pass.
  Evidence:
  ReviewGate reported 0 errors and 0 warnings.
- Command:
  `dotnet run --project tools/SimReplay/SimReplay.csproj --no-restore`
  Result:
  Pass.
  Evidence:
  SimReplay reported `OK [sim-events]: event drain buffer can be reused without
  snapshot arrays.` and all deterministic scenarios passed.
- Command:
  `dotnet run --project tools/PerfSmoke/PerfSmoke.csproj -c Release --no-restore`
  Result:
  Pass.
  Evidence:
  Worst average was 1.213ms at 400 units under the 16.667ms budget; allocation was
  188125 bytes/tick at 400 units.

Manual/visual gates:
- Check:
  Visual runtime.
  Result:
  Not run.
  Evidence:
  This is pure simulation/event plumbing with deterministic replay coverage.

Reviewer result:
- Status: pass-with-warnings
- Required fixes:
  - None for this bounded allocation slice.
- Residual risks:
  - Independent reviewer was not available.
  - PerfSmoke allocation did not materially drop because other hot-path
    allocations dominate; Combat mount list updates remain open.

TODO update:
- Items marked done:
  - None.
- Items left open:
  - Broad simulation hot-path allocation item.
- Reason:
  - This closes the event-drain allocation subproblem, but not the remaining
    Combat/command allocation pressure.
