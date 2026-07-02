# Review Record - M9 CommandSystem economy order subject buffer

Step:
M9 CommandSystem economy order subject buffer (#75)

Milestone:
M9 - Elegance & Decoupling

Owner AI:
Remote Linux Codex

Reviewer AI:
Remote Linux Codex

Integrator AI:
Remote Linux Codex

Scope:
- Files/folders: `scripts/core/sim/systems/command/CommandSystem.EconomyOrders.cs`, `scripts/core/sim/systems/command/CommandSystem.SubjectsSelection.cs`, `tools/ReviewGateDomains/CommandSystemAllocationReviewGate.cs`, `TODO.md`.
- Non-goals: no changes to resource choice, cargo/dock flow, repair legality, restart capture, movement intent semantics, UI, visuals, balance, or the full #10 closeout.

Implementation summary:
- Routed Harvest, AutoHarvest, and Repair through the reusable `_scalarOrderMembers` subject buffer.
- Removed the old `OwnedSubjects(...)` yield iterator helper after all command paths stopped using it.
- Extracted `ApplyHarvestIntent(...)` so AutoHarvest applies harvest intent directly instead of allocating a nested `HarvestEntityCommand` and one-entity subject array.
- Extended `CommandSystemAllocationReviewGate` to lock economy orders against iterator and nested-command allocation regressions.

Automated gates:
- Command: `dotnet build ProceduralRts.csproj --no-restore`
  Result: pass
  Evidence: Debug build succeeded with 0 warnings and 0 errors.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore -- simhot --max-warnings=0`
  Result: pass
  Evidence: ReviewGate passed with 0 errors and 0 warnings.
- Command: `dotnet run --project tools/SimReplay/SimReplay.csproj --no-restore`
  Result: pass
  Evidence: SimReplay PASSED; resource-loop, auto-harvest, repair-field, targeted-repair, and full deterministic replay suite stayed green.
- Command: `dotnet run --project tools/CombatBehavior/CombatBehavior.csproj --no-restore`
  Result: pass
  Evidence: Combat behavior passed.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore`
  Result: pass
  Evidence: Full ReviewGate passed after this record was added.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore -- review --require-record=m9-command-economy-order-buffer`
  Result: pass
  Evidence: ReviewGate found this durable review record.
- Command: `dotnet run --project tools/VerifyAll/VerifyAll.csproj --no-restore`
  Result: pass
  Evidence: Concentrated VerifyAll passed for #73/#74/#75 before final closeout.

Manual/visual gates:
- Check: Visual QA
  Result: not applicable
  Evidence: Simulation command allocation refactor only; no rendering or UI layout changed.

Reviewer result:
- Status: pass
- Required fixes: none
- Residual risks: #10 remains open for broader profiler-guided allocation cleanup beyond CommandSystem.

TODO update:
- Items marked done: none; #10 remains open.
- Items left open: M9 per-tick allocation paydown.
- Reason: This closes the economy-order allocation child slice, not all remaining allocation work.
