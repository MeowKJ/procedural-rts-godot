# Review Record - UnitBattlefield production status UnitSpec cleanup

Step: UnitSpec architecture phase 3 duplicate-data cleanup UnitBattlefield production status slice
Milestone: M1 EntityWorld Becomes Authoritative
Owner AI: Worker Hegel
Reviewer AI: ReviewGate unitbattlefieldproductionspecstatus / Integrator
Integrator AI: Integrator / Codex

Scope:
- Files/folders: `scripts/core/units/runtime/UnitBattlefield.cs`, `tools/ReviewGate/Program.cs`, `TODO.md`, `docs/reviews/2026-06-30-unitbattlefield-production-spec-status.md`.
- Non-goals: deleting `ProductionKind`, changing producer selection policy, changing production queues, changing costs, or deleting legacy unit compatibility catalogs.

Implementation summary:
- Moved the `EnqueueProduction(ProductionKind, ...)` missing-producer status path to resolve the requested UnitDesign id first.
- The status path now reads the requested `UnitSpec` through `UnitDesignCatalog.Spec(...)`.
- Unit labels come from `UnitDesignDefinitionCatalog.RuntimeDescriptors` when available, falling back to `UnitSpec.Label`.
- Producer labels come from `UnitSpec.Production.ProducerKind` through `BuildSpecCatalog`.
- Added `ReviewGate unitbattlefieldproductionspecstatus` to prevent this status path from returning to legacy production definitions.

Automated gates:
- Command: `dotnet build ProceduralRts.csproj --no-restore`
  Result: pass
  Evidence: build completed successfully with 0 warnings and 0 errors.
- Command: `dotnet run --project tools/CombatBehavior/CombatBehavior.csproj --no-restore`
  Result: pass
  Evidence: CombatBehavior completed successfully.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore -- unitbattlefieldproductionspecstatus`
  Result: pass
  Evidence: ReviewGate unitbattlefieldproductionspecstatus completed with 0 errors and 0 warnings.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore -- review --require-record=unitbattlefield-production-spec-status`
  Result: pass
  Evidence: required review record check completed with 0 errors and 0 warnings.

Manual/visual gates:
- Check: visual inspection not required for this narrow status metadata-source migration.
  Result: not run.
  Evidence: UI wording keys and producer selection behavior remain unchanged; only label data sources changed.

Reviewer result:
- Status: pass
- Required fixes: none after automated gate.
- Residual risks: broader `UnitBattlefield` production UI still has compatibility paths until final legacy unit catalog deletion.

TODO update:
- Items marked done: none.
- Items left open: parent UnitSpec duplicate-data cleanup remains open.
- Reason: this is one scoped production status read-path cleanup.
