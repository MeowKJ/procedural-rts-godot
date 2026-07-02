# Review Record - M9 LocalAvoidance BuildHash Removal

Step: #171 `[M9] Remove LocalAvoidanceMath allocating hash wrapper`
Milestone: M9 - Elegance & Decoupling
Owner AI: Remote Linux Codex
Reviewer AI: ReviewGate regression / SimReplay / CombatBehavior
Integrator AI: Remote Linux Codex

Scope:
- Remove the unused `LocalAvoidanceMath.BuildHash(...)` wrapper that allocated a dictionary and copied it with `ToDictionary(...)`.
- Keep caller-owned `BuildHashInto(...)` and both typed `ResolveVector(...)` hash overloads intact.
- Extend `GameStateAllocationReviewGate` so `ReviewGate regression` forbids the retired wrapper from returning.
- Non-goals: changing avoidance math, `GameState` pathing behavior, `SpatialGrid`, or path-quality tooling.

Automated gates:
- Command: `dotnet build ProceduralRts.csproj --no-restore`
  Result: pass.
- Command: `dotnet run --project tools/SimReplay/SimReplay.csproj --no-restore`
  Result: pass.
- Command: `dotnet run --project tools/CombatBehavior/CombatBehavior.csproj --no-restore`
  Result: pass.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore -- regression --max-warnings=0`
  Result: pass.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore -- review --require-record=m9-local-avoidance-buildhash-removal`
  Result: pass.

Reviewer result:
- Status: pass.
- Required fixes: none known.

Status:
- pass

Residual risks:
- `ResolveVector(...)` overloads still support existing typed hash callers; removing them is outside this slice.
- Parent #10 remains open for broader allocation paydown.

TODO update:
- Added #171 follow-up evidence under the open M9 per-tick allocation paydown item.
- Items marked done: none.
- Items left open: parent #10 broader allocation paydown.
