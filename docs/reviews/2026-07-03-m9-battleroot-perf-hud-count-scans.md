# Review Record - M9 BattleRoot Perf HUD Count Scans

Step: #165 `[M9] Replace BattleRoot perf HUD count LINQ`
Milestone: M9 - Elegance & Decoupling
Owner AI: Remote Linux Codex
Reviewer AI: ReviewGate presentation / DesktopHudQa
Integrator AI: Remote Linux Codex

Scope:
- Replace `BattleRoot.Process.PerfHudCounts()` live unit, live building, and visible unit view `Count(...)` LINQ paths with explicit scans.
- Preserve perf HUD count semantics and keep `UnitBattlefield.LiveBuildingCount(...)` unchanged.
- Extend `BattleRootHudAllocationReviewGate` so `ReviewGate presentation` forbids the old per-frame count LINQ paths.
- Non-goals: changing perf HUD display, sampling cadence, culling, minimap, selection HUD, alerts, sandbox helpers, or closing parent #10.

Automated gates:
- Command: `dotnet build ProceduralRts.csproj --no-restore`
  Result: pass.
- Command: `dotnet run --project tools/DesktopHudQa/DesktopHudQa.csproj --no-restore`
  Result: pass.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore -- presentation --max-warnings=0`
  Result: pass.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore -- review --require-record=m9-battleroot-perf-hud-count-scans`
  Result: pass.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore`
  Result: pass.

Reviewer result:
- Status: pass.
- Required fixes: none known.

Status:
- pass

Residual risks:
- Alerts and sandbox helper LINQ paths remain separate candidates; this slice only covers per-frame perf HUD counts.
- Parent #10 remains open for broader allocation paydown.

TODO update:
- Added #165 follow-up evidence under the open M9 per-tick allocation paydown item.
- Items marked done: none.
- Items left open: parent #10 broader allocation paydown.
