# Review Record - M9 Pathfinding Neighbor Buffer

Step: #159 `[M9] Replace PathfindingMath neighbor iterator buffer`
Milestone: M9 - Elegance & Decoupling
Owner AI: Remote Linux Codex
Reviewer AI: ReviewGate regression / SimReplay / CombatBehavior
Integrator AI: Remote Linux Codex

Scope:
- Replace `PathfindingMath.ValidNeighbors(...)` `IEnumerable` / `yield return` helper with `CollectValidNeighbors(...)`.
- Allocate one neighbor buffer per `FindPathWithDebug(...)` call and reuse it for every A* expanded cell.
- Preserve neighbor offset order, diagonal corner-cutting checks, blocker checks, terrain passability checks, and movement-domain semantics.
- Extend `PathfindingAllocationReviewGate` so `ReviewGate regression` forbids the old iterator/yield path.
- Non-goals: changing A* heuristic, clearance penalty, smoothing, `PathfindingSystem` grouping, replay hash, or closing parent #10.

Automated gates:
- Command: `dotnet build ProceduralRts.csproj --no-restore`
  Result: pass.
- Command: `dotnet run --project tools/SimReplay/SimReplay.csproj --no-restore`
  Result: pass.
- Command: `dotnet run --project tools/CombatBehavior/CombatBehavior.csproj --no-restore`
  Result: pass.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore -- regression --max-warnings=0`
  Result: pass.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore -- review --require-record=m9-pathfinding-neighbor-buffer`
  Result: pass.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore`
  Result: pass.

Reviewer result:
- Status: pass.
- Required fixes: none.

Status:
- pass

Residual risks:
- This removes neighbor iterator state but still leaves the A* dictionaries, priority queue, reconstructed path list, and per-call passability collections for future profiler-guided slices.
- Parent #10 remains open for broader allocation paydown.

TODO update:
- Added #159 follow-up evidence under the open M9 per-tick allocation paydown item.
- Items marked done: none.
- Items left open: parent #10 broader allocation paydown.
