# Review Record - M9 Pathfinding Passability Materialization

Step: #158 `[M9] Remove PathfindingMath passability LINQ materialization`
Milestone: M9 - Elegance & Decoupling
Owner AI: Remote Linux Codex
Reviewer AI: ReviewGate regression / SimReplay / CombatBehavior
Integrator AI: Remote Linux Codex

Scope:
- Replace `PathfindingMath.FindPathWithDebug(...)` blocker/terrain `ToHashSet()` / `ToDictionary(...)` setup with explicit fill helpers.
- Replace `PathfindingMath.FindSharedCorridor(...)` blocker/terrain `ToHashSet()` / `ToDictionary(...)` setup with the same explicit fill helper.
- Add `PathfindingMath.Passability.cs` as a small partial to keep `PathfindingMath.cs` below the file-size warning threshold.
- Extend `PathfindingAllocationReviewGate` so `ReviewGate regression` forbids the old passability materialization paths.
- Non-goals: changing A*, line-of-sight pruning, terrain passability semantics, shared-corridor routing, path result, or closing parent #10.

Automated gates:
- Command: `dotnet build ProceduralRts.csproj --no-restore`
  Result: pass.
- Command: `dotnet run --project tools/SimReplay/SimReplay.csproj --no-restore`
  Result: pass.
- Command: `dotnet run --project tools/CombatBehavior/CombatBehavior.csproj --no-restore`
  Result: pass.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore -- regression --max-warnings=0`
  Result: pass.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore -- review --require-record=m9-pathfinding-passability-materialization`
  Result: pass.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore`
  Result: pass.

Reviewer result:
- Status: pass.
- Required fixes: none.

Status:
- pass

Residual risks:
- This removes LINQ materialization in passability setup but still allocates per-call passability collections; caller-owned pathfinding scratch state remains a separate future slice if profiling justifies it.
- Parent #10 remains open for broader profiler-guided GC cleanup.

TODO update:
- Added #158 follow-up evidence under the open M9 per-tick allocation paydown item.
- Items marked done: none.
- Items left open: parent #10 broader allocation paydown.
