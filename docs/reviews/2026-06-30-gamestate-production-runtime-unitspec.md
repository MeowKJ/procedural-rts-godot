# Review Record - GameState production runtime UnitSpec cleanup

Step: UnitSpec architecture phase 3 duplicate-data cleanup GameState production runtime slice
Milestone: M1 EntityWorld Becomes Authoritative
Owner AI: Codex
Reviewer AI: ReviewGate gamestateproductionruntimeunitspec / Integrator
Integrator AI: Codex

Scope:
- Files/folders: `scripts/core/GameState.cs`, `tools/ReviewGate/Program.cs`, `TODO.md`, `docs/reviews/2026-06-30-gamestate-production-runtime-unitspec.md`.
- Non-goals: deleting `GameState.ProductionDefinitions`, changing legacy `ProductionKind` queue shape, changing HUD callers, changing production balance, or migrating external compatibility display paths.

Implementation summary:
- Moved old-runtime `EnqueueProduction(...)`, `CancelFirstProduction(...)`, `UpdateProductionQueues(...)`, and `SpawnProducedUnit(...)` off direct `ProductionDefinitions` reads.
- Added `CandidateProductionProducers(...)` so enqueue checks ready producer buildings through legacy UnitSpec production compatibility data.
- Queue costs and refunds now read from old-compatible `UnitSpec.Stats.Cost`; queue duration reads from legacy `ProductionSpec.Duration`.
- Completed legacy `OutputUnit` compatibility now resolves through `UnitPresentationCatalog.ForProductionSpec(...)`, preserving old `CompletedProductionItem` shape without reading GameState's production table.
- `SpawnProducedUnit(...)` now trusts `CompletedProductionItem.OutputUnit` instead of rereading production metadata.
- Added `ReviewGate gamestateproductionruntimeunitspec` to keep these runtime methods UnitSpec-backed.

Automated gates:
- Command: `dotnet build ProceduralRts.csproj --no-restore`
  Result: pass
  Evidence: build completed successfully with 0 warnings and 0 errors.
- Command: `dotnet run --project tools/CombatBehavior/CombatBehavior.csproj --no-restore`
  Result: pass
  Evidence: CombatBehavior passed weapon hit rules, turret states, terrain passability, localization fallback, presentation descriptors, shared threat propagation, rally production, economy, enemy AI, and outcomes.
- Command: `dotnet run --project tools/SimulationSmoke/SimulationSmoke.csproj --no-restore`
  Result: pass
  Evidence: SimulationSmoke passed its 300 second smoke with 10 orders, 10 completions, 2 waves, and outcome InProgress, preserving old-runtime pacing after switching to legacy UnitSpec compatibility specs.
- Command: `dotnet run --project tools/PlayerLoopQa/PlayerLoopQa.csproj --no-restore`
  Result: pass
  Evidence: PlayerLoopQa passed build radius, harvest/bank, T1-T3 production, rally, selection, move/attack/stance, victory and defeat.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore -- gamestateproductionruntimeunitspec`
  Result: pass
  Evidence: ReviewGate gamestateproductionruntimeunitspec completed with 0 errors and 0 warnings.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore -- review --require-record=gamestate-production-runtime-unitspec`
  Result: pass
  Evidence: required review record check completed with 0 errors and 0 warnings.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore`
  Result: pass
  Evidence: full ReviewGate completed with 0 errors and 0 warnings.
- Command: `dotnet run --project tools/VerifyAll/VerifyAll.csproj --no-restore`
  Result: pass
  Evidence: VerifyAll completed 23/23 checks after the slice.

Manual/visual gates:
- Check: visual inspection not required for this deterministic production runtime metadata migration.
  Result: not run.
  Evidence: no rendering code changed.

Reviewer result:
- Status: pass
- Required fixes: none after automated gates.
- Residual risks: external HUD/BuildingView/BattleRoot compatibility display paths still read `GameState.ProductionDefinitions` until later slices; the static table itself remains.

TODO update:
- Items marked done: none.
- Items left open: parent UnitSpec duplicate-data cleanup remains open.
- Reason: this removes GameState runtime method reads, not the final external compatibility callers or the table definition.
