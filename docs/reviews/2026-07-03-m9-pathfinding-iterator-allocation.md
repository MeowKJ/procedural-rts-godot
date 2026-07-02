# Review Record - M9 Pathfinding Iterator Allocation

Step: #157 `[M9] Remove pathfinding iterator allocation debt`
Milestone: M9 - Elegance & Decoupling
Owner AI: Remote Linux Codex
Reviewer AI: ReviewGate regression / SimReplay / CombatBehavior
Integrator AI: Remote Linux Codex

Scope:
- Replace `FindSharedCorridor(...)` member anchor `Average(...)` with an explicit indexed scan.
- Replace shared-corridor `sharedPath.Skip(...)` appends with indexed `AppendUnique(...)` overloads.
- Replace `ReconstructPath(...)` `Skip(1).Select(...).ToList()` point construction with a pre-sized list and for-loop.
- Replace legacy `GameState.AssignPath(...)` `GlobalCorridor.Skip(1)` enqueue with an indexed loop.
- Add `PathfindingAllocationReviewGate` and route it through `ReviewGate regression`.
- Non-goals: changing A*, LOS pruning, terrain passability, blocker rules, `PathfindingComponentState`, or closing parent #10.

Automated gates:
- Command: `dotnet build ProceduralRts.csproj --no-restore`
  Result: pass, 0 warnings / 0 errors.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore -- regression --max-warnings=0`
  Result: pass, Errors: 0, Warnings: 0.
- Command: `dotnet run --project tools/SimReplay/SimReplay.csproj --no-restore`
  Result: pass, including entity-pathfinding, shared-corridor, group-move, same-point-move, and deterministic replay scenarios.
- Command: `dotnet run --project tools/CombatBehavior/CombatBehavior.csproj --no-restore`
  Result: pass.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore -- filesize --max-warnings=0`
  Result: pass after syncing the validation-tool source budget evidence.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore -- review --require-record=m9-pathfinding-iterator-allocation`
  Result: pass.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore`
  Result: pass.

Reviewer result:
- Status: pass.
- Required fixes: none.

Status:
- pass

Residual risks:
- This does not remove `FindSharedCorridor(...)` blocker `ToHashSet()`, terrain `ToDictionary()`, or per-assignment path/raw-cell list allocations.
- Parent #10 remains open for broader profiler-guided GC cleanup.

TODO update:
- Added #157 follow-up evidence under the open M9 per-tick allocation paydown item.
- Items marked done: none.
- Items left open: parent #10 broader allocation paydown.
