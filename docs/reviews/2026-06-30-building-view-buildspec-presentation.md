# Review Record - BuildingView BuildSpec presentation cleanup

Step: Migration cleanup BuildingView BuildSpec presentation slice
Milestone: M1 EntityWorld Becomes Authoritative
Owner AI: Integrator / Codex
Reviewer AI: ReviewGate buildingviewbuildspecpresentation
Integrator AI: Integrator / Codex

Scope:
- Files/folders: `scripts/world/BuildingView.cs`, `tools/ReviewGate/Program.cs`, `TODO.md`, `docs/reviews/2026-06-30-building-view-buildspec-presentation.md`.
- Non-goals: deleting `PresentationCatalog.Building`, changing building gameplay authority, changing BuildingView layout/art geometry, or changing selection/health overlay behavior.

Implementation summary:
- Replaced `BuildingView`'s final `PresentationCatalog.Building(...)` read with direct `BuildSpecCatalog.For(kind)` presentation color inputs.
- Body accent now comes from `BuildSpec.Accent` through `FactionVisualPolicy.EntityAccent(...)`.
- Relation color now comes from `FactionVisualPolicy.RelationOverlay(...)` and remains selection/health overlay-only.
- Owner color remains separate through `EntityRenderPalette.SoftOldCity(ownerColor, bodyAccent)`.
- Added `ReviewGate buildingviewbuildspecpresentation` and advanced `buildingviewviewerfaction` so this view cannot regress to building presentation descriptors.

Automated gates:
- Command: `dotnet build ProceduralRts.csproj --no-restore`
  Result: pass
  Evidence: build completed successfully with 0 warnings and 0 errors.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore -- buildingviewbuildspecpresentation`
  Result: pass
  Evidence: ReviewGate buildingviewbuildspecpresentation completed with 0 errors and 0 warnings.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore -- review --require-record=building-view-buildspec-presentation`
  Result: pass
  Evidence: required review record check completed with 0 errors and 0 warnings.

Manual/visual gates:
- Check: visual inspection not required for this narrow metadata-source migration.
  Result: not run.
  Evidence: drawing calls and overlay placement remain unchanged; only body/relation color data sources changed.

Reviewer result:
- Status: pass
- Required fixes: none expected after automated gate.
- Residual risks: `PresentationCatalog.Building` still exists as a compatibility helper until all remaining non-view call sites are gone and its deletion gate is updated.

TODO update:
- Items marked done: `BuildingView BuildSpec presentation cleanup`.
- Items left open: broader Migration cleanup remains open.
- Reason: this slice removes BuildingView's presentation descriptor dependency, not the full secondary building runtime.
