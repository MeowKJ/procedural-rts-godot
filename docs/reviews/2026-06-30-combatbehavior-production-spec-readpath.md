# Review Record - CombatBehavior ProductionSpec read-path cleanup

Step: UnitSpec architecture phase 3 duplicate-data cleanup CombatBehavior ProductionSpec read-path slice
Milestone: M1 EntityWorld Becomes Authoritative
Owner AI: Codex
Reviewer AI: ReviewGate combatbehaviorproductionspecreadpath / Integrator
Integrator AI: Codex

Scope:
- Files/folders: `tools/CombatBehavior/Program.cs`, `tools/ReviewGate/Program.cs`, `TODO.md`, `docs/reviews/2026-06-30-combatbehavior-production-spec-readpath.md`.
- Non-goals: deleting `GameState.ProductionDefinitions`, changing GameState production runtime behavior, changing unit costs/durations, changing AI production choices, or expanding `ProductionKind`.

Implementation summary:
- Added `ProductionDesignSpecFor(...)` so CombatBehavior resolves legacy `ProductionKind` checks through `UnitDesignRuntimeLoadouts.ProductionDesignId(...)` and `UnitDesignCatalog.Spec(...)`.
- Added `PlayableProductionSpecs(...)` so CombatBehavior can enumerate production-capable Dog/Cat UnitSpecs without reading `GameState.ProductionDefinitions`.
- Replaced production presentation/lane metadata QA with UnitSpec/ProductionSpec reads.
- Replaced production lane refund, rally production cost, cancel refund, and enemy producer-kind QA reads with UnitSpec cost/producer metadata.
- Added `ReviewGate combatbehaviorproductionspecreadpath` to keep `GameState.ProductionDefinitions` out of CombatBehavior.

Automated gates:
- Command: `dotnet build ProceduralRts.csproj --no-restore`
  Result: pass
  Evidence: build completed successfully with 0 warnings and 0 errors.
- Command: `dotnet run --project tools/CombatBehavior/CombatBehavior.csproj --no-restore`
  Result: pass
  Evidence: CombatBehavior passed weapon hit rules, turret states, terrain passability, localization fallback, presentation descriptors, shared threat propagation, rally production, economy, enemy AI, and outcomes.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore -- combatbehaviorproductionspecreadpath`
  Result: pass
  Evidence: ReviewGate combatbehaviorproductionspecreadpath completed with 0 errors and 0 warnings.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore -- review --require-record=combatbehavior-production-spec-readpath`
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
  Evidence: no runtime visuals changed; only CombatBehavior QA sources changed.

Reviewer result:
- Status: pass
- Required fixes: none after automated gates.
- Residual risks: `GameState.ProductionDefinitions` still exists and remains used by legacy runtime compatibility paths until a later production-runtime migration slice.

TODO update:
- Items marked done: none.
- Items left open: parent UnitSpec duplicate-data cleanup remains open.
- Reason: this removes CombatBehavior's read path, not the legacy runtime production definitions.
