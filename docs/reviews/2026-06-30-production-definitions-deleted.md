# Review Record - ProductionDefinitions deletion cleanup

Step: UnitSpec architecture phase 3 duplicate-data cleanup ProductionDefinitions deletion slice
Milestone: M1 EntityWorld Becomes Authoritative
Owner AI: Codex
Reviewer AI: ReviewGate productiondefinitionsdeleted / Integrator
Integrator AI: Codex

Scope:
- Files/folders: `scripts/core/GameState.cs`, `scripts/core/ProductionDefinition.cs`, `scripts/core/units/ProductionKindDesignBridge.cs`, `tools/ReviewGate/Program.cs`, `TODO.md`, `docs/reviews/2026-06-30-production-definitions-deleted.md`.
- Non-goals: deleting `ProductionKind`, changing old-runtime production queue shape, changing authored faction UnitSpecs, or changing production balance.

Implementation summary:
- Deleted the unused `GameState.ProductionDefinitions` table.
- Deleted `scripts/core/ProductionDefinition.cs`.
- Kept old-runtime INF/TNK/HAR compatibility metadata in `ProductionKindDesignBridge.LegacySpecFor(...)` / `LegacyProductionSpecs()`.
- Legacy compatibility specs reuse generic UnitSpec stats for old costs/labels and explicit legacy `ProductionSpec` values for producer/duration/lane metadata.
- Added `ReviewGate productiondefinitionsdeleted` to keep the deleted table/type from returning and scan `scripts/**/*.cs` for removed symbols.

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
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore -- productiondefinitionsdeleted`
  Result: pass
  Evidence: ReviewGate productiondefinitionsdeleted completed with 0 errors and 0 warnings.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore -- resourcescope`
  Result: pass
  Evidence: ReviewGate resourcescope completed with 0 errors and 0 warnings after checking UnitSpec single-cost fields and ProductionKindDesignBridge legacy specs.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore -- review --require-record=production-definitions-deleted`
  Result: pass
  Evidence: required review record check completed with 0 errors and 0 warnings.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore`
  Result: pass
  Evidence: full ReviewGate completed with 0 errors and 0 warnings.
- Command: `dotnet run --project tools/VerifyAll/VerifyAll.csproj --no-restore`
  Result: pass
  Evidence: VerifyAll completed 23/23 checks after the slice.

Manual/visual gates:
- Check: visual inspection not required for this deterministic deleted-symbol cleanup.
  Result: not run.
  Evidence: no visual behavior changed.

Reviewer result:
- Status: pass
- Required fixes: none after automated gates.
- Residual risks: legacy `ProductionKind` remains as the compatibility queue id until a future broader production API deletion/migration.

TODO update:
- Items marked done: none.
- Items left open: parent UnitSpec duplicate-data cleanup remains open.
- Reason: this deletes the production definition table, but broader UnitSpec duplicate-data cleanup still has other legacy compatibility surfaces.
