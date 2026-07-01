# Review Record - GameState ProductionLaneSnapshots UnitSpec cleanup

Step: UnitSpec architecture phase 3 duplicate-data cleanup GameState production lanes slice
Milestone: M1 EntityWorld Becomes Authoritative
Owner AI: Codex
Reviewer AI: ReviewGate gamestateproductionlanesunitspec / Integrator
Integrator AI: Codex

Scope:
- Files/folders: `scripts/core/GameState.cs`, `tools/ReviewGate/Program.cs`, `TODO.md`, `docs/reviews/2026-06-30-gamestate-production-lanes-unitspec.md`.
- Non-goals: changing queue completion, changing enqueue/cancel behavior, changing production balance, deleting `GameState.ProductionDefinitions`, or changing HUD layout.

Implementation summary:
- Moved `GameState.ProductionLaneSnapshots(...)` off legacy `ProductionDefinitions` reads.
- Queue snapshots now resolve each queued legacy `ProductionKind` through `ProductionKindDesignBridge.LegacySpecFor(...)`.
- Queue cost and refund math now read from old-compatible `UnitSpec.Stats.Cost`; queue progress reads duration from legacy `ProductionSpec`.
- Legacy `OutputUnit` compatibility for snapshots now comes from `UnitPresentationCatalog.ForProductionSpec(...)`, keeping the old snapshot shape while avoiding GameState's production table.
- `IsProductionBuilding(...)` now identifies producers from UnitSpec production metadata.
- Added `ReviewGate gamestateproductionlanesunitspec` to keep this queue snapshot path UnitSpec-backed.

Automated gates:
- Command: `dotnet build ProceduralRts.csproj --no-restore`
  Result: pass
  Evidence: build completed successfully with 0 warnings and 0 errors.
- Command: `dotnet run --project tools/CombatBehavior/CombatBehavior.csproj --no-restore`
  Result: pass
  Evidence: CombatBehavior passed weapon hit rules, turret states, terrain passability, localization fallback, presentation descriptors, shared threat propagation, rally production, economy, enemy AI, and outcomes.
- Command: `dotnet run --project tools/PlayerLoopQa/PlayerLoopQa.csproj --no-restore`
  Result: pass
  Evidence: PlayerLoopQa passed build radius, harvest/bank, T1-T3 production, rally, selection, move/attack/stance, victory and defeat.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore -- gamestateproductionlanesunitspec`
  Result: pass
  Evidence: ReviewGate gamestateproductionlanesunitspec completed with 0 errors and 0 warnings.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore -- review --require-record=gamestate-production-lanes-unitspec`
  Result: pass
  Evidence: required review record check completed with 0 errors and 0 warnings.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore`
  Result: pass
  Evidence: full ReviewGate completed with 0 errors and 0 warnings.
- Command: `dotnet run --project tools/VerifyAll/VerifyAll.csproj --no-restore`
  Result: pass
  Evidence: VerifyAll completed 23/23 checks after the slice.

Manual/visual gates:
- Check: visual inspection not required for this deterministic production queue snapshot migration.
  Result: not run.
  Evidence: no rendering code changed.

Reviewer result:
- Status: pass
- Required fixes: none after automated gates.
- Residual risks: `GameState.ProductionDefinitions` remains used by legacy enqueue/cancel/production-completion compatibility paths until later slices.

TODO update:
- Items marked done: none.
- Items left open: parent UnitSpec duplicate-data cleanup remains open.
- Reason: this removes queue snapshot metadata reads only, not the whole legacy production runtime.
