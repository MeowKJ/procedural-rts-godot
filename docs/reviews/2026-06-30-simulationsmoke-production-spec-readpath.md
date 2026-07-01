# Review Record - SimulationSmoke ProductionSpec read-path cleanup

Step: UnitSpec architecture phase 3 duplicate-data cleanup SimulationSmoke ProductionSpec read-path slice
Milestone: M1 EntityWorld Becomes Authoritative
Owner AI: Codex
Reviewer AI: ReviewGate simulationsmokeproductionspecreadpath / Integrator
Integrator AI: Codex

Scope:
- Files/folders: `tools/SimulationSmoke/Program.cs`, `tools/ReviewGate/Program.cs`, `TODO.md`, `docs/reviews/2026-06-30-simulationsmoke-production-spec-readpath.md`.
- Non-goals: deleting `GameState.ProductionDefinitions`, changing GameState production runtime behavior, changing production durations/costs, changing smoke duration, or changing enemy AI behavior.

Implementation summary:
- SimulationSmoke production validation now uses `ProductionKindDesignBridge.LegacySpecFor(...)` so old-runtime queue validation remains UnitSpec-driven without changing legacy INF/TNK/HAR costs and durations.
- Moved queued production validation off `GameState.ProductionDefinitions`.
- Production queue items now validate that faction + legacy `ProductionKind` resolves through `UnitDesignRuntimeLoadouts.ProductionDesignId(...)`.
- Queue progress is bounded by the current faction's UnitSpec production lane duration, preserving old-runtime compatibility where one legacy kind can represent multiple authored UnitSpecs.
- Added `ReviewGate simulationsmokeproductionspecreadpath` to prevent `GameState.ProductionDefinitions` from returning to SimulationSmoke.

Automated gates:
- Command: `dotnet build ProceduralRts.csproj --no-restore`
  Result: pass
  Evidence: build completed successfully with 0 warnings and 0 errors.
- Command: `dotnet run --project tools/SimulationSmoke/SimulationSmoke.csproj --no-restore`
  Result: pass
  Evidence: SimulationSmoke passed its 300 second smoke with 10 orders, 10 completions, 2 waves, and outcome InProgress.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore -- simulationsmokeproductionspecreadpath`
  Result: pass
  Evidence: ReviewGate simulationsmokeproductionspecreadpath completed with 0 errors and 0 warnings.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore -- review --require-record=simulationsmoke-production-spec-readpath`
  Result: pass
  Evidence: required review record check completed with 0 errors and 0 warnings.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore`
  Result: pass
  Evidence: full ReviewGate completed with 0 errors and 0 warnings.
- Command: `dotnet run --project tools/VerifyAll/VerifyAll.csproj --no-restore`
  Result: pass
  Evidence: VerifyAll completed 23/23 checks after the slice.

Manual/visual gates:
- Check: visual inspection not required for this deterministic tool read-path migration.
  Result: not run.
  Evidence: no runtime visuals changed; only SimulationSmoke validation sources changed.

Reviewer result:
- Status: pass
- Required fixes: none after automated gates.
- Residual risks: `GameState.ProductionDefinitions` still exists and remains used by legacy runtime compatibility paths until later production-runtime migration slices.

TODO update:
- Items marked done: none.
- Items left open: parent UnitSpec duplicate-data cleanup remains open.
- Reason: this removes SimulationSmoke's read path, not the legacy runtime production definitions.
