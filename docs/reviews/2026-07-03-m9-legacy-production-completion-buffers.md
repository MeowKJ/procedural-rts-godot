# Review Record - M9 Legacy Production Completion Buffers

Step: #162 `[M9] Reuse legacy production completion buffers`
Milestone: M9 - Elegance & Decoupling
Owner AI: Remote Linux Codex
Reviewer AI: ReviewGate regression / CombatBehavior / PlayerLoopQa
Integrator AI: Remote Linux Codex

Scope:
- Replace legacy `GameState.UpdateProductionQueues(...)` `Buildings.ToList()` tick snapshot with an indexed building scan.
- Add reusable produced-unit spawn obstacle storage to legacy `GameState`.
- Replace `UnitObstacles()` `Units.Select(...).ToList()` with a caller-owned `CollectUnitSpawnObstacles(...)` helper.
- Preserve production completion, spawn placement scoring, rally assignment, production status text, and runtime `UnitBattlefield` production behavior.
- Extend `GameStateAllocationReviewGate` so `ReviewGate regression` forbids the old production completion snapshot and spawn-obstacle LINQ paths.
- Non-goals: changing production balance, spawn scoring, rally behavior, UnitBattlefield production, enqueue/cancel selection, or closing parent #10.

Automated gates:
- Command: `dotnet build ProceduralRts.csproj --no-restore`
  Result: pass.
- Command: `dotnet run --project tools/CombatBehavior/CombatBehavior.csproj --no-restore`
  Result: pass.
- Command: `dotnet run --project tools/PlayerLoopQa/PlayerLoopQa.csproj --no-restore`
  Result: pass.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore -- regression --max-warnings=0`
  Result: pass.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore -- review --require-record=m9-legacy-production-completion-buffers`
  Result: pass.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore`
  Result: pass.

Reviewer result:
- Status: pass.
- Required fixes: none known.

Status:
- pass

Residual risks:
- The indexed producer scan assumes production-completion subscribers do not mutate the `Buildings` list during the legacy update; current subscribers only update HUD, alerts, audio, and command cards.
- Parent #10 remains open for broader allocation paydown.

TODO update:
- Added #162 follow-up evidence under the open M9 per-tick allocation paydown item.
- Items marked done: none.
- Items left open: parent #10 broader allocation paydown.
