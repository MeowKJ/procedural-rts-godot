# Review Record - M9 production queue mutation buffers

Step: Reuse production queue mutation storage
Milestone: M9 Elegance, Decoupling, Performance
Owner AI: Codex
Reviewer AI: ReviewGate regression / SimReplay
Integrator AI: Codex

Scope:
- Files/folders: `scripts/core/sim/systems/ProductionSystem.cs`, `tools/SimReplayEconomyProduction/ProductionScenarios.cs`, `tools/ReviewGateDomains/RegressionReviewGate.cs`, `TODO.md`.
- Non-goals: changing production timing, costs, repeat behavior, rally behavior, refunds, queue ordering, or closing the broad M9 allocation parent issue.

Implementation summary:
- Replaced `ProductionSystem` enqueue/remove array-copy mutations with `MutableQueueItems(...)`, which converts existing array-backed queue storage to `List<UnitProductionQueueItem>` once and reuses that list for later queue changes.
- Kept `ProductionQueueComponentState.Items` as `IReadOnlyList<UnitProductionQueueItem>` so projections and existing read paths remain unchanged.
- Extended `SimReplay` production assertions so completed, cancelled, unpowered, and repeat producer queues keep reusable storage while deterministic replay hashes remain unchanged.
- Extended `ReviewGate regression` to require the reusable queue mutation path and forbid `new UnitProductionQueueItem[...]` copied queue arrays from returning.

Automated gates:
- Command: `dotnet build ProceduralRts.csproj --no-restore`
  Result: pass
  Evidence: main project build completed with 0 warnings and 0 errors.
- Command: `dotnet run --project tools/SimReplay/SimReplay.csproj --no-restore`
  Result: pass
  Evidence: deterministic replay suite passed; `production-loop` final hash stayed `427000301860631748` and `repeat-production` final hash stayed `18255291344743571417`.
- Command: `dotnet run --project tools/CombatBehavior/CombatBehavior.csproj --no-restore`
  Result: pass
  Evidence: combat behavior, rally production, economy, enemy AI, and outcomes passed.
- Command: `dotnet run --project tools/PerfSmoke/PerfSmoke.csproj --configuration Release --no-restore`
  Result: pass
  Evidence: 400-unit run averaged 3.089ms, p99 3.357ms, and 192620 bytes/tick; the combat smoke does not exercise queue mutations, so allocation/tick is unchanged.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore -- regression --max-warnings=0`
  Result: pass
  Evidence: ReviewGate regression completed with 0 errors and 0 warnings.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore -- filesize --max-warnings=0`
  Result: pass
  Evidence: file-size gate completed with 0 errors and 0 warnings; `RegressionReviewGate.cs` remains at 200 lines.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore -- review --require-record=m9-production-queue-mutation-buffers`
  Result: pass
  Evidence: required review record gate completed with 0 errors and 0 warnings.
- Command: `GODOT_BIN=$(command -v godot-dotnet) DOTNET_ROLL_FORWARD=Major sh tools/verify-all.sh`
  Result: pass
  Evidence: full grouped verification completed 23/23.

Manual/visual gates:
- Check: GUI visual QA
  Result: not run
  Evidence: this was a Godot-free simulation allocation slice; no presentation rendering behavior changed.

Reviewer result:
- Status: pass.
- Required fixes: none.
- Residual risks: the broad M9 allocation item remains open. Remaining debt still includes path/queue storage families outside production queue mutation and broader profiler-guided GC cleanup.

TODO update:
- Items marked done: none.
- Items left open: `Per-tick allocation paydown`.
- Reason: #123 closes one concrete child allocation slice, but issue #10 tracks a broader allocation paydown theme.
