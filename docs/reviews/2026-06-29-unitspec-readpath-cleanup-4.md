# Review Record - UnitSpec read path cleanup 4

Step: UnitSpec duplicate-data cleanup fourth read-path slice
Milestone: M1 EntityWorld Becomes Authoritative
Owner AI: Worker-M1C / Codex
Reviewer AI: Codex self-review
Integrator AI: Main thread

Scope:
- Files/folders: `scripts/core/units/UnitDesignDefinitionCatalog.cs`, `tools/CombatBehavior/Program.cs`, `tools/ReviewGate/Program.cs`, `docs/reviews/2026-06-29-unitspec-readpath-cleanup-4.md`.
- Non-goals: deleting `UnitKind`, `UnitCatalog`, `FactionCatalog`, or `GameState` compatibility APIs; changing live combat, construction, placement, movement, replay, or runtime balance behavior; updating `TODO.md`.

Implementation summary:
- Added a `UnitDesignDefinitionCatalog.CompatibilityDefinition(UnitSpecRuntimeDescriptor, UnitKind)` overload so compatibility `UnitDefinition` views can be projected from UnitSpec runtime descriptors at the explicit legacy boundary.
- Moved the CombatBehavior footprint weight-class seed definitions from direct `GameState.UnitDefinitionFor` reads to `UnitDesignDefinitionCatalog.ForDesign` plus descriptor-backed compatibility projections.
- Updated the `unitdesigndefinitioncatalog` ReviewGate mode to preserve this cleanup slice and reject renewed legacy seeding for the footprint tank QA definition.

Automated gates:
- Command: `dotnet build ProceduralRts.csproj`
  Result: pass
  Evidence: build completed successfully with 0 warnings and 0 errors.
- Command: `dotnet run --project tools/CombatBehavior/CombatBehavior.csproj`
  Result: pass
  Evidence: CombatBehavior completed successfully after the footprint QA read path moved to descriptor-backed definitions.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj unitdesigndefinitioncatalog`
  Result: pass
  Evidence: dedicated UnitDesign definition catalog gate completed with 0 errors and 0 warnings.

Manual/visual gates:
- Check: visual inspection not required.
  Result: not run.
  Evidence: this slice only changes read-only tool QA data access and static ReviewGate checks.

Reviewer result:
- Status: pass-with-warnings
- Required fixes: none identified in the scoped self-review.
- Residual risks: legacy `UnitKind`, `UnitCatalog`, `FactionCatalog`, and remaining `GameState.UnitDefinitionFor` reads still exist for live compatibility and later migration slices.

TODO update:
- Items marked done: none.
- Items left open: parent UnitSpec duplicate-data cleanup and later deletion of legacy unit/faction catalogs.
- Reason: this is a narrow read-path cleanup slice; the main thread owns TODO integration.
