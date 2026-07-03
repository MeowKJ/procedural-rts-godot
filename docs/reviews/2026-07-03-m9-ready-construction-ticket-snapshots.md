# Review Record - M9 Ready Construction Ticket Snapshots

Step: #202 `[M9] Reuse ready construction ticket snapshots`
Milestone: M9 - Elegance & Decoupling
Owner AI: Remote Linux Codex
Reviewer AI: ReviewGate simhot / CommandSystemAllocationReviewGate
Integrator AI: Remote Linux Codex

Scope:
- Files/folders: `scripts/core/units/runtime/battlefield/UnitBattlefield.ConstructionTickets.cs`, `tools/ReviewGateDomains/CommandSystemAllocationReviewGate.cs`, `TODO.md`.
- Non-goals: 不改变 construction queue timing、cost reservation、placement validation、ready-ticket consume semantics、building adoption、UI、balance、story、或 visual polish。

Implementation summary:
- `ReadyConstructionTickets(...)` now returns the reusable `_constructionTicketBuffer` after `CollectReadyConstructionTickets(...)` fills it.
- Ready-ticket filtering still includes only ready-to-place tickets for the requested player slot.
- `ReviewGate simhot` locks the readout against returning to `_constructionTicketBuffer.ToArray()`.

Automated gates:
- Command: `dotnet build ProceduralRts.csproj --no-restore`
  Result: pass in batch verification.
- Command: `dotnet run --project tools/PlayerLoopQa/PlayerLoopQa.csproj --no-restore`
  Result: pass in batch verification.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore -- simhot --max-warnings=0`
  Result: pass in batch verification.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore -- review --require-record=m9-ready-construction-ticket-snapshots`
  Result: pass in batch verification.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore`
  Result: pass in batch verification.

Reviewer result:
- Status: pass.
- Required fixes: none currently known.

Residual risks:
- The public readout now follows other UnitBattlefield reusable projection/readout APIs; callers should not keep the returned list across later ticket queries.
- Parent #10 remains open for broader allocation paydown.

TODO update:
- Items marked done: none.
- Items left open: broader M9 per-tick allocation paydown remains open under #10.
