# Review Record - M9 Legacy Production Enqueue Scans

Step: #161 `[M9] Reuse legacy production enqueue scans`
Milestone: M9 - Elegance & Decoupling
Owner AI: Remote Linux Codex
Reviewer AI: ReviewGate regression / CombatBehavior / PlayerLoopQa
Integrator AI: Remote Linux Codex

Scope:
- Replace legacy `GameState.EnqueueProduction(...)` producer `OrderBy(...).ThenBy(...)` selection with an explicit least-queued producer scan.
- Replace legacy `GameState.CancelFirstProduction(...)` owner/filter/order query with an explicit earliest queue-item scan.
- Preserve least queue count, producer id tie-break, first queued item cancel behavior, production status text, costs, and refunds.
- Extend `GameStateAllocationReviewGate` so `ReviewGate regression` forbids the old production enqueue/cancel LINQ sorting paths.
- Non-goals: changing UnitBattlefield production, production balance, HUD text, production completion, or closing parent #10.

Automated gates:
- Command: `dotnet build ProceduralRts.csproj --no-restore`
  Result: pass.
- Command: `dotnet run --project tools/CombatBehavior/CombatBehavior.csproj --no-restore`
  Result: pass.
- Command: `dotnet run --project tools/PlayerLoopQa/PlayerLoopQa.csproj --no-restore`
  Result: pass.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore -- regression --max-warnings=0`
  Result: pass.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore -- review --require-record=m9-legacy-production-enqueue-scans`
  Result: pass.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore`
  Result: pass.

Reviewer result:
- Status: pass.
- Required fixes: none known.

Status:
- pass

Residual risks:
- This removes production enqueue/cancel query allocation only; production completion building snapshots and produced-unit spawn obstacle list construction remain separate possible slices.
- Parent #10 remains open for broader allocation paydown.

TODO update:
- Added #161 follow-up evidence under the open M9 per-tick allocation paydown item.
- Items marked done: none.
- Items left open: parent #10 broader allocation paydown.
