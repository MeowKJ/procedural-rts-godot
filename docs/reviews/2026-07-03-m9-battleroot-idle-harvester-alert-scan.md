# Review Record - M9 BattleRoot Idle Harvester Alert Scan

Step: #166 `[M9] Replace BattleRoot idle harvester alert Count LINQ`
Milestone: M9 - Elegance & Decoupling
Owner AI: Remote Linux Codex
Reviewer AI: ReviewGate presentation / DesktopHudQa
Integrator AI: Remote Linux Codex

Scope:
- Replace `BattleRoot.UpdateIdleHarvesterAlert()` legacy `_state.Units.Count(...)` with an explicit scan helper.
- Preserve alert cooldown, one/many text selection, and `IsHarvestWorker(...)` semantics.
- Extend `BattleRootHudAllocationReviewGate` so `ReviewGate presentation` forbids the old idle harvester count LINQ path.
- Non-goals: changing alert text, cooldowns, runtime `UnitBattlefield` behavior, harvester behavior, selection HUD, perf HUD, minimap, sandbox helpers, or closing parent #10.

Automated gates:
- Command: `dotnet build ProceduralRts.csproj --no-restore`
  Result: pass.
- Command: `dotnet run --project tools/DesktopHudQa/DesktopHudQa.csproj --no-restore`
  Result: pass.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore -- presentation --max-warnings=0`
  Result: pass.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore -- review --require-record=m9-battleroot-idle-harvester-alert-scan`
  Result: pass.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore`
  Result: pass.

Reviewer result:
- Status: pass.
- Required fixes: none known.

Status:
- pass

Residual risks:
- Runtime `UnitBattlefield` path bypasses this legacy alert; this slice only removes the legacy fallback count allocation.
- Parent #10 remains open for broader allocation paydown.

TODO update:
- Added #166 follow-up evidence under the open M9 per-tick allocation paydown item.
- Items marked done: none.
- Items left open: parent #10 broader allocation paydown.
