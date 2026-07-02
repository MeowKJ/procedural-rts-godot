# Review Record - M9 PathfindingSystem path copy paydown

Step: Avoid PathfindingSystem path array copies
Milestone: M9 Elegance, Decoupling, Performance
Owner AI: Remote Linux Codex
Reviewer AI: SimReplay / ReviewGate simhot
Integrator AI: Remote Linux Codex

Scope:
- Issue: #72.
- Files/folders: `scripts/core/sim/systems/PathfindingSystem.cs`, `tools/ReviewGateDomains/RegressionReviewGate.cs`, `TODO.md`, `docs/reviews/2026-07-02-m9-pathfinding-path-copy-paydown.md`.
- Non-goals: changing `PathfindingMath`, shared-corridor assignment semantics, LOS smoothing, terrain/blocker rules, movement behavior, `PathfindingComponentState` field types, production queue arrays, ability cooldown arrays, or closing the broad #10 allocation paydown item.

Implementation summary:
- Added `PathOrGoal(...)` to reuse non-empty `PathfindingMath` path results directly as `IReadOnlyList<PathPoint>`.
- Replaced shared-corridor assignment and single-path `ToArray()` copies before `PathfindingComponentState` creation.
- Preserved the one-point goal fallback when a path result is empty.
- Extended broad `ReviewGate simhot` evidence so `PathfindingSystem` cannot regress to `assignment.Path.ToArray()` or `result.Path.ToArray()`.

Automated gates:
- Command: `dotnet build ProceduralRts.csproj --no-restore`
  Result: pass
  Evidence: main project build completed with 0 warnings and 0 errors.
- Command: `dotnet run --project tools/SimReplay/SimReplay.csproj --no-restore`
  Result: pass
  Evidence: SimReplay passed, including entity-pathfinding, entity-shared-corridor, group-move, same-point-move, and command-feel deterministic scenarios.
- Command: `dotnet run --project tools/CombatBehavior/CombatBehavior.csproj --no-restore`
  Result: pass
  Evidence: CombatBehavior passed weapon hit rules, turret states, terrain passability, localization fallback, presentation descriptors, shared threat propagation, rally production, economy, enemy AI, and outcomes.
- Command: `dotnet run --project tools/PerfSmoke/PerfSmoke.csproj --no-restore`
  Result: pass
  Evidence: 400-unit run averaged 11.057ms, p99 11.898ms, max 13.041ms, and 192620 bytes/tick, under the 16.667ms active budget.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore -- simhot --max-warnings=0`
  Result: pass
  Evidence: ReviewGate simhot completed with 0 errors and 0 warnings, including the new PathfindingSystem no-path-copy checks.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore`
  Result: pass
  Evidence: full ReviewGate completed with 0 errors and 0 warnings after updating the validation-tool source budget lock to 17797 total tool lines.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore -- review --require-record=m9-pathfinding-path-copy-paydown`
  Result: pass
  Evidence: review-record gate completed with 0 errors and 0 warnings.
- Command: `dotnet run --project tools/VerifyAll/VerifyAll.csproj --no-restore`
  Result: pass
  Evidence: VerifyAll completed all 23 steps successfully, including build, SimReplay, CombatBehavior, ReviewGate, PerfSmoke, balance/counter QA, and Godot headless QA. Release PerfSmoke inside VerifyAll reported 400-unit avg 3.092ms and 192620 bytes/tick.

Reviewer result:
- Status: pass.
- Required fixes: none.

Status:
- Complete for #72 after integration gates.

Residual risks:
- This removes only the PathfindingSystem path-copy layer. Remaining #10 debt still includes production queue arrays, ability cooldown arrays, and broader profiler-guided GC cleanup.

TODO update:
- Items marked done: none.
- Items left open: `Per-tick allocation paydown`.
- Reason: #72 narrows one path allocation family but does not complete the broad M9 allocation-debt item.
