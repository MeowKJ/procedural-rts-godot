# Review Record - M9 CommandSystem scalar order subject buffer

Step:
M9 CommandSystem scalar order subject buffer (#73)

Milestone:
M9 - Elegance & Decoupling

Owner AI:
Remote Linux Codex

Reviewer AI:
Remote Linux Codex

Integrator AI:
Remote Linux Codex

Scope:
- Files/folders: `scripts/core/sim/systems/CommandSystem.cs`, `scripts/core/sim/systems/command/CommandSystem.MovementOrders.cs`, `scripts/core/sim/systems/command/CommandSystem.CombatOrders.cs`, `tools/ReviewGateDomains/CommandSystemAllocationReviewGate.cs`, `tools/ReviewGateDomains/RegressionReviewGate.cs`, `TODO.md`.
- Non-goals: no changes to group order buffers, harvest/repair orders, selection sets, command semantics, combat balance, UI, or visuals.

Implementation summary:
- Added `_scalarOrderMembers` to `CommandSystem`.
- Routed Move, Patrol, Guard, Attack, Stop, and Stance command paths through `CollectOwnedSubjects(...)` with the reusable scalar buffer.
- Added `CommandSystemAllocationReviewGate` and wired it into `RegressionReviewGate` so `ReviewGate simhot` rejects scalar order paths that return to `OwnedSubjects(...)` iterator allocation.

Automated gates:
- Command: `dotnet build ProceduralRts.csproj --no-restore`
  Result: pass
  Evidence: Debug build succeeded with 0 warnings and 0 errors.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore -- simhot --max-warnings=0`
  Result: pass
  Evidence: ReviewGate passed with 0 errors and 0 warnings.
- Command: `dotnet run --project tools/SimReplay/SimReplay.csproj --no-restore`
  Result: pass
  Evidence: SimReplay PASSED; group move, group attack, patrol, guard, combat, resource, construction, and outcome hashes stayed deterministic.
- Command: `dotnet run --project tools/CombatBehavior/CombatBehavior.csproj --no-restore`
  Result: pass
  Evidence: Combat behavior passed.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore`
  Result: pass
  Evidence: Full ReviewGate passed after this record was added.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore -- review --require-record=m9-command-scalar-order-buffer`
  Result: pass
  Evidence: ReviewGate found this durable review record.

Manual/visual gates:
- Check: Visual QA
  Result: not applicable
  Evidence: Simulation command allocation refactor only; no rendering or UI behavior changed.

Reviewer result:
- Status: pass
- Required fixes: none
- Residual risks: Harvest/AutoHarvest/Repair and selection allocation cleanup remain separate #74/#75 slices; broad #10 remains open.

TODO update:
- Items marked done: none; #10 remains open.
- Items left open: M9 per-tick allocation paydown.
- Reason: This is one bounded allocation paydown slice, not the full closeout for all remaining CommandSystem allocation debt.
