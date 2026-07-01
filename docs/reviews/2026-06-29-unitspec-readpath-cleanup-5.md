# Review Record - UnitSpec read path cleanup 5

Step: UnitSpec duplicate-data cleanup fifth read-path slice
Milestone: M1 EntityWorld Becomes Authoritative
Owner AI: Worker F / Codex
Reviewer AI: Codex self-review
Integrator AI: Main thread

Scope:
- Files/folders: `tools/CombatBehavior/Program.cs`, `tools/ReviewGate/Program.cs`, `docs/reviews/2026-06-29-unitspec-readpath-cleanup-5.md`.
- Non-goals: deleting `UnitKind`, `UnitCatalog`, `FactionCatalog`, or `GameState` compatibility APIs; changing live combat, construction, placement, movement, replay, UI, or runtime balance behavior; updating `TODO.md`.

Implementation summary:
- Moved the CombatBehavior aircraft target-profile QA input from direct `GameState.UnitDefinitionFor(UnitKind.CatScoutAircraft)` reads to `UnitDesignDefinitionCatalog.ForDesign("cat.scout_aircraft")` plus an explicit descriptor-backed compatibility projection.
- Added a CombatBehavior assertion that the projected aircraft compatibility definition preserves aircraft movement, armor, kind, and attack-range metadata from the UnitSpec runtime descriptor.
- Updated the `unitdesigndefinitioncatalog` ReviewGate mode to require this read-path cleanup and reject renewed direct GameState aircraft definition reads in CombatBehavior.

Automated gates:
- Command: `dotnet build ProceduralRts.csproj --no-restore`
  Result: pass
  Evidence: build completed successfully with 0 warnings and 0 errors.
- Command: `dotnet run --project tools/CombatBehavior/CombatBehavior.csproj --no-restore`
  Result: pass
  Evidence: CombatBehavior completed successfully after the aircraft target-profile QA read path moved to descriptor-backed definitions.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj unitdesigndefinitioncatalog --no-restore`
  Result: pass
  Evidence: ReviewGate completed with 0 errors and 0 warnings.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj review --require-record unitspec-readpath-cleanup-5 --no-restore`
  Result: pass
  Evidence: ReviewGate review mode found the cleanup-5 record and completed with 0 errors and 0 warnings.

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
