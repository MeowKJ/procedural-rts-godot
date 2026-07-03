# Review Record - M9 BattleRoot Debug Design Id Scan

Step: #201 `[M9] Replace BattleRoot debug design id LINQ`
Milestone: M9 - Elegance & Decoupling
Owner AI: Remote Linux Codex
Reviewer AI: ReviewGate presentation / BattleRootHudAllocationReviewGate
Integrator AI: Remote Linux Codex

Scope:
- Files/folders: `scripts/BattleRoot.cs`, `scripts/SkirmishFlowQaRoot.cs`, `tools/ReviewGatePresentation/BattleRootHudAllocationReviewGate.cs`, `TODO.md`.
- Non-goals: 不改变 skirmish loadout、faction selection、AI difficulty、runtime spawning、UI、balance、story、或 visual polish。

Implementation summary:
- `DebugUnitBattlefieldDesignIds(...)` now counts matching live units and fills a stable string array with an explicit scan.
- Player and enemy debug readouts remain independent snapshots, preserving the `SkirmishFlowQaRoot` two-call assertion pattern.
- `ReviewGate presentation` locks the debug readout against returning to the LINQ `Select(unit => unit.Spec.Id)` projection chain.

Automated gates:
- Command: `dotnet build ProceduralRts.csproj --no-restore`
  Result: pass in batch verification.
- Command: `godot-dotnet --headless --path . --scene res://scenes/SkirmishFlowQa.tscn`
  Result: pass in batch verification.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore -- presentation --max-warnings=0`
  Result: pass in batch verification.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore -- review --require-record=m9-battleroot-debug-design-id-scan`
  Result: pass in batch verification.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore`
  Result: pass in batch verification.

Reviewer result:
- Status: pass.
- Required fixes: none currently known.

Residual risks:
- The returned array allocation remains intentional to preserve independent debug snapshot ownership across consecutive player/enemy reads.
- Parent #10 remains open for broader allocation paydown.

TODO update:
- Items marked done: none.
- Items left open: broader M9 per-tick allocation paydown remains open under #10.
