# Review Record - M9 UnitBattlefield Spawn Roster Loop

Step: #200 `[M9] Replace UnitBattlefield SpawnRoster LINQ`
Milestone: M9 - Elegance & Decoupling
Owner AI: Remote Linux Codex
Reviewer AI: ReviewGate simhot / UnitBattlefieldAllocationReviewGate
Integrator AI: Remote Linux Codex

Scope:
- Files/folders: `scripts/core/units/runtime/UnitBattlefield.cs`, `tools/ReviewGateDomains/UnitBattlefieldAllocationReviewGate.cs`, `TODO.md`.
- Non-goals: 不改变 roster definitions、UnitDesignCatalog ordering、spawn stats、facing、weapons、EntityWorld mirror contract、sandbox composition、balance、UI、或 visual polish。

Implementation summary:
- `SpawnRoster(...)` now pre-sizes a result list from the roster design count and fills it with an indexed loop.
- Spawn order and spacing still follow `UnitDesignCatalog.ForRoster(roster)` order.
- `ReviewGate simhot` locks the method against returning to the LINQ `Select(...).ToList()` projection chain.

Automated gates:
- Command: `dotnet build ProceduralRts.csproj --no-restore`
  Result: pass in batch verification.
- Command: `dotnet run --project tools/CombatBehavior/CombatBehavior.csproj --no-restore`
  Result: pass in batch verification.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore -- simhot --max-warnings=0`
  Result: pass in batch verification.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore -- review --require-record=m9-unitbattlefield-spawn-roster-loop`
  Result: pass in batch verification.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore`
  Result: pass in batch verification.

Reviewer result:
- Status: pass.
- Required fixes: none currently known.

Residual risks:
- `SpawnRoster(...)` still returns an owned result list by design because callers inspect the spawned units after setup.
- Parent #10 remains open for broader allocation paydown.

TODO update:
- Items marked done: none.
- Items left open: broader M9 per-tick allocation paydown remains open under #10.
