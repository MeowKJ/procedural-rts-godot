# Review Record - UnitCatalog public surface deletion

Step: UnitCatalog public surface deletion
Milestone: M1 EntityWorld Becomes Authoritative / UnitSpec duplicate-data cleanup
Owner AI: Codex
Reviewer AI: ReviewGate unitcatalogpublicsurfacedeleted / Integrator
Integrator AI: Codex

Scope:
- Files/folders: `scripts/core/UnitCatalog.cs`, `tools/ReviewGate/Program.cs`, `TODO.md`, `docs/reviews/2026-06-30-unitcatalog-public-surface-deleted.md`.
- Non-goals: deleting `UnitCatalog` entirely, deleting `UnitKind`, deleting `UnitCatalogEntry`, changing legacy compatibility entry contents, changing production behavior, or changing unit balance/stats.

Implementation summary:
- Removed the unused public `UnitCatalog.Definitions` dictionary.
- Removed the unused public `UnitCatalog.Presentations` dictionary.
- Removed the unused public `UnitCatalog.UnitsForFaction(...)` helper.
- Kept temporary private compatibility entries and the `DesignDefinition(...)` / `DesignPresentation(...)` projections until a later whole-catalog deletion slice.
- Added `ReviewGate unitcatalogpublicsurfacedeleted` to prevent active source from reading the deleted public surfaces.

Automated gates:
- Command: `dotnet build ProceduralRts.csproj --no-restore`
  Result: pass
  Evidence: build completed successfully with 0 warnings and 0 errors.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore -- unitcatalogpublicsurfacedeleted`
  Result: pass
  Evidence: narrow ReviewGate mode completed successfully with 0 errors and 0 warnings.
- Command: `dotnet run --project tools/CombatBehavior/CombatBehavior.csproj --no-restore`
  Result: pass
  Evidence: CombatBehavior completed successfully.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore`
  Result: pass
  Evidence: full ReviewGate completed successfully with 0 errors and 0 warnings.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore -- review --require-record=unitcatalog-public-surface-deleted`
  Result: pass
  Evidence: review-record gate completed successfully with 0 errors and 0 warnings.

Manual/visual gates:
- Check: visual inspection not required.
  Result: not run.
  Evidence: API surface cleanup only.

Reviewer result:
- Status: pass.
- Required fixes: none.
- Residual risks: `UnitCatalog` still exists as a temporary compatibility file until the later whole-catalog deletion slice.

TODO update:
- Items marked done: `UnitCatalog public surface deletion` under UnitSpec architecture phase 3 duplicate-data cleanup.
- Items left open: broad UnitSpec duplicate-data cleanup and final `UnitKind` / `UnitCatalog` deletion remain open.
- Reason: duplicated public dictionaries are removed, but the temporary catalog file still exists.
