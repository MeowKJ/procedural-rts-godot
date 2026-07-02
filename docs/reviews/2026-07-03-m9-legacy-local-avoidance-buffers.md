# Review Record - M9 Legacy Local Avoidance Buffers

Step: #160 `[M9] Reuse legacy local avoidance hash buffers`
Milestone: M9 - Elegance & Decoupling
Owner AI: Remote Linux Codex
Reviewer AI: ReviewGate regression / SimReplay / CombatBehavior
Integrator AI: Remote Linux Codex

Scope:
- Add reusable local-avoidance body and hash bucket storage to legacy `GameState`.
- Replace `GameState.BuildLocalAvoidanceHash()` `Units.Where(...).Select(...).ToList()` with an explicit alive-unit scan.
- Route legacy local avoidance through `LocalAvoidanceMath.BuildHashInto(...)` instead of the allocating `BuildHash(...)` dictionary-copy path.
- Adjust legacy movement/local-avoidance method signatures to consume the reusable bucket dictionary.
- Extend `GameStateAllocationReviewGate` so `ReviewGate regression` forbids the old local-avoidance materialization path.
- Non-goals: changing avoidance force, anchor bias, cell size, displacement rules, EntityWorld movement/separation systems, or closing parent #10.

Automated gates:
- Command: `dotnet build ProceduralRts.csproj --no-restore`
  Result: pass.
- Command: `dotnet run --project tools/SimReplay/SimReplay.csproj --no-restore`
  Result: pass.
- Command: `dotnet run --project tools/CombatBehavior/CombatBehavior.csproj --no-restore`
  Result: pass.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore -- regression --max-warnings=0`
  Result: pass.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore -- review --require-record=m9-legacy-local-avoidance-buffers`
  Result: pass.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore`
  Result: pass.

Reviewer result:
- Status: pass.
- Required fixes: none.

Status:
- pass

Residual risks:
- This keeps the reusable hash dictionary keys alive with empty buckets, matching existing `BuildHashInto(...)` reuse behavior; a future compaction policy can be added only if profiling shows stale buckets matter.
- Parent #10 remains open for broader allocation paydown.

TODO update:
- Added #160 follow-up evidence under the open M9 per-tick allocation paydown item.
- Items marked done: none.
- Items left open: parent #10 broader allocation paydown.
