# Review Record - EnemyProductionAi ProductionSpec read-path cleanup

Step: UnitSpec architecture phase 3 duplicate-data cleanup EnemyProductionAi ProductionSpec slice
Milestone: M1 EntityWorld Becomes Authoritative
Owner AI: Codex
Reviewer AI: ReviewGate enemyproductionaiunitspecproduction / Integrator
Integrator AI: Codex

Scope:
- Files/folders: `scripts/core/EnemyProductionAi.cs`, `scripts/core/units/ProductionKindDesignBridge.cs`, `tools/ReviewGate/Program.cs`, `TODO.md`, `docs/reviews/2026-06-30-enemy-production-ai-unitspec-production.md`.
- Non-goals: changing enemy pacing, changing production balance, changing production queue completion, deleting `GameState.ProductionDefinitions`, or changing the UnitBattlefield enemy AI.

Implementation summary:
- Added `ProductionKindDesignBridge.LegacySpecFor(...)` / `LegacyProductionSpecs()` so old-runtime callers can resolve legacy `ProductionKind` metadata through UnitSpec without changing old INF/TNK/HAR costs.
- Moved `EnemyProductionAi.CanQueue(...)` off `GameState.ProductionDefinitions`; it now enumerates ready producer buildings and reads cost/producer metadata from the legacy UnitSpec/ProductionSpec compatibility specs.
- Moved enemy rally-point producer detection off legacy production definitions and onto legacy UnitSpec production specs.
- Preserved the existing AI decision order and profile pacing; only the metadata source changed.
- Added `ReviewGate enemyproductionaiunitspecproduction` to prevent legacy production definition reads from returning to `EnemyProductionAi`.

Automated gates:
- Command: `dotnet build ProceduralRts.csproj --no-restore`
  Result: pass
  Evidence: build completed successfully with 0 warnings and 0 errors.
- Command: `dotnet run --project tools/CombatBehavior/CombatBehavior.csproj --no-restore`
  Result: pass
  Evidence: CombatBehavior passed weapon hit rules, turret states, terrain passability, localization fallback, presentation descriptors, shared threat propagation, rally production, economy, enemy AI, and outcomes.
- Command: `dotnet run --project tools/SimulationSmoke/SimulationSmoke.csproj --no-restore`
  Result: pass
  Evidence: SimulationSmoke passed its 300 second smoke with 10 orders, 10 completions, 2 waves, and outcome InProgress.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore -- enemyproductionaiunitspecproduction`
  Result: pass
  Evidence: ReviewGate enemyproductionaiunitspecproduction completed with 0 errors and 0 warnings.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore -- productionkinddesignbridge`
  Result: pass
  Evidence: ReviewGate productionkinddesignbridge completed with 0 errors and 0 warnings after adding the non-throwing bridge resolver.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore -- review --require-record=enemy-production-ai-unitspec-production`
  Result: pass
  Evidence: required review record check completed with 0 errors and 0 warnings.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore`
  Result: pass
  Evidence: full ReviewGate completed with 0 errors and 0 warnings.
- Command: `dotnet run --project tools/VerifyAll/VerifyAll.csproj --no-restore`
  Result: pass
  Evidence: VerifyAll completed 23/23 checks after the slice.

Manual/visual gates:
- Check: visual inspection not required for this deterministic metadata read-path migration.
  Result: not run.
  Evidence: no runtime visuals changed.

Reviewer result:
- Status: pass
- Required fixes: none after automated gates.
- Residual risks: `GameState.ProductionDefinitions` remains used by legacy queue runtime, HUD, and production-completion compatibility paths until later slices.

TODO update:
- Items marked done: none.
- Items left open: parent UnitSpec duplicate-data cleanup remains open.
- Reason: this removes the legacy AI read path only; it does not delete the old production queue compatibility table.
