# Review Record - BuildingView viewer-faction fallback cleanup

Step: Migration cleanup BuildingView viewer-faction fallback slice
Milestone: M1 EntityWorld Becomes Authoritative
Owner AI: BuildingView worker / Codex
Reviewer AI: ReviewGate buildingviewviewerfaction
Integrator AI: Integrator / Codex

Scope:
- Files/folders: `scripts/world/BuildingView.cs`, `scripts/BattleRoot.cs`, `tools/ReviewGate/Program.cs`, `TODO.md`, `docs/reviews/2026-06-30-building-view-viewer-faction.md`.
- Non-goals: deleting `PresentationCatalog.Building`, changing building gameplay authority, changing BuildingView art recipes, or changing building selection behavior.

Implementation summary:
- Added an explicit nullable `ViewerFaction` input to `BuildingView`.
- Removed the `FactionCatalog.DefaultFactionForOwner(CoreOwner.Player)` viewer-faction guess from `BuildingView`.
- `BattleRoot` now injects `_state.MatchConfig.PlayerFaction` into live building views.
- BuildingView now uses the explicit viewer faction to resolve relation overlays directly; the later BuildSpec presentation cleanup removed the temporary `PresentationCatalog.Building(...)` call from this path.
- The redraw signature now includes the viewer faction key so faction injection changes invalidate cached drawing.

Automated gates:
- Command: `dotnet build ProceduralRts.csproj --no-restore`
  Result: pass
  Evidence: build completed successfully.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore -- buildingviewviewerfaction`
  Result: pass
  Evidence: ReviewGate buildingviewviewerfaction completed with 0 errors and 0 warnings after the expectation was advanced to direct viewer-faction relation resolution.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore -- review --require-record=building-view-viewer-faction`
  Result: pass
  Evidence: required review record check completed with 0 errors and 0 warnings.

Manual/visual gates:
- Check: visual inspection not required for this narrow data-source cleanup.
  Result: not run.
  Evidence: drawing code and palette use remain in `BuildingView`; only viewer-faction source changed.

Reviewer result:
- Status: pass
- Required fixes: none expected after automated gate.
- Residual risks: `PresentationCatalog.Building` remains as a compatibility presentation path until the broader building presentation catalog cleanup is complete.

TODO update:
- Items marked done: `BuildingView viewer-faction fallback cleanup`.
- Items left open: broader Migration cleanup remains open.
- Reason: this slice removes one fallback inference but does not delete the secondary building runtime.
