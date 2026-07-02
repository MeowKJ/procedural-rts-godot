# Review Record - M9 FormationMath destination buffer overload

Step: Route CommandSystem group move through caller-owned FormationMath buffers
Milestone: M9 Elegance, Decoupling, Performance
Owner AI: Remote Linux Codex
Reviewer AI: SimReplay / ReviewGate simhot
Integrator AI: Remote Linux Codex

Scope:
- Issue: #66.
- Files/folders: `scripts/core/commands/FormationMath.cs`, `scripts/core/sim/systems/CommandSystem.cs`, `scripts/core/sim/systems/command/CommandSystem.MovementOrders.cs`, `tools/ReviewGateDomains/RegressionReviewGate.cs`, `TODO.md`, `docs/reviews/2026-07-02-m9-formation-destination-buffer-overload.md`.
- Non-goals: changing group move formation semantics, world clamp behavior, movement state, pathfinding, `AttackSlotMath`, damage, balance, AI strategy, or presentation behavior.

Implementation summary:
- Added `FormationMath.CreateMoveDestinationsInto(...)`, which writes destinations into caller-owned output and work buffers.
- Kept the existing `CreateMoveDestinations(...)` API as a compatibility wrapper around the new buffer overload.
- Replaced `FormationMath` LINQ max/order/nearest-slot/result materialization inside the new hot path with deterministic scan-based selection.
- Added reusable `CommandSystem` work buffers for FormationMath destination results, ordered units, slots, and remaining slots.
- Routed `CommandSystem.ApplyGroupMove` through `CreateMoveDestinationsInto(...)`, then reused the existing destination lookup dictionary from #64.
- Extended broad `ReviewGate simhot` evidence so CommandSystem cannot regress to the allocating `FormationMath.CreateMoveDestinations(...)` API.

Automated gates:
- Command: `dotnet build ProceduralRts.csproj --no-restore`
  Result: pass
  Evidence: main project build completed with 0 warnings and 0 errors.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore -- simhot --max-warnings=0`
  Result: pass
  Evidence: ReviewGate completed with 0 errors and 0 warnings.
- Command: `dotnet run --project tools/SimReplay/SimReplay.csproj --no-restore`
  Result: pass
  Evidence: SimReplay passed, including entity-shared-corridor, group-move, same-point-move, and command-feel deterministic scenarios.
- Command: `dotnet run --project tools/CombatBehavior/CombatBehavior.csproj --no-restore`
  Result: pass
  Evidence: CombatBehavior passed weapon hit rules, turret states, terrain passability, shared threat propagation, rally production, economy, enemy AI, and outcomes.
- Command: `dotnet run --project tools/PerfSmoke/PerfSmoke.csproj --no-restore`
  Result: pass
  Evidence: 400-unit run averaged 11.251ms, p99 12.033ms, max 12.779ms, and 192620 bytes/tick, under the 16.667ms active budget.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore`
  Result: pass
  Evidence: full ReviewGate completed with 0 errors and 1 existing source-directory warning for `scripts/core/sim/`.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore -- review --require-record=m9-formation-destination-buffer-overload`
  Result: pass
  Evidence: review-record gate completed with 0 errors and 0 warnings.
- Command: `dotnet run --project tools/VerifyAll/VerifyAll.csproj --no-restore`
  Result: pass
  Evidence: VerifyAll completed all 23 steps successfully, including build, SimReplay, CombatBehavior, ReviewGate, PerfSmoke, balance/counter QA, and Godot headless QA.

Reviewer result:
- Status: pass.
- Required fixes: none.
- Residual risks: the compatibility `CreateMoveDestinations(...)` API still allocates for non-hot callers; remaining M9 allocation debt includes Construction/placement lists, immutable queue/path arrays, and profiler-guided GC cleanup.

TODO update:
- Items marked done: none.
- Items left open: `Per-tick allocation paydown`.
- Reason: #66 removes one deeper group-move destination allocation family from CommandSystem's hot path, but the broad allocation-debt item remains open.
