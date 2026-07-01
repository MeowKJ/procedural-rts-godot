# Review Record - DynamicUnitIcon UnitSpec icon cleanup

Step: DynamicUnitIcon UnitSpec icon cleanup
Milestone: M1 EntityWorld Becomes Authoritative / UnitSpec duplicate-data cleanup
Owner AI: Codex
Reviewer AI: ReviewGate dynamicuniticonunitspec / Integrator
Integrator AI: Codex

Scope:
- Files/folders: `scripts/ui/DynamicUnitIcon.cs`, `tools/ReviewGate/Program.cs`, `TODO.md`, `docs/reviews/2026-06-30-dynamic-unit-icon-unitspec.md`.
- Non-goals: deleting `UnitKind`, deleting `UnitCatalog`, deleting `UnitPresentationCatalog`, changing HUD layout, changing unit balance/stats, changing production behavior, or changing the authored UnitSpec art recipes.

Implementation summary:
- Moved `DynamicUnitIcon.DrawUnitIcon(...)`, the legacy `UnitKind` icon compatibility entrypoint, onto `UnitKindDesignBridge.TryGetSpec(...)`.
- Mapped legacy `UnitKind` calls now delegate to `DrawUnitDesignIcon(...)`, so command and summary icons use UnitSpec art recipes.
- Removed the old `UnitPresentationCatalog.For(kind)` and `UnitVisualRenderer.DrawUnitSilhouette(...)` read/draw path from `DrawUnitIcon(...)`.
- Kept null/unmapped compatibility behavior explicit by drawing the supplied fallback glyph.
- Added `ReviewGate dynamicuniticonunitspec` to prevent this UI icon path from regressing to legacy presentation data.

Automated gates:
- Command: `dotnet build ProceduralRts.csproj --no-restore`
  Result: pass
  Evidence: build completed successfully with 0 warnings and 0 errors.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore -- dynamicuniticonunitspec`
  Result: pass
  Evidence: narrow ReviewGate mode completed successfully with 0 errors and 0 warnings.
- Command: `dotnet run --project tools/CombatBehavior/CombatBehavior.csproj --no-restore`
  Result: pass
  Evidence: CombatBehavior completed successfully.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore`
  Result: pass
  Evidence: full ReviewGate completed successfully with 0 errors and 0 warnings.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore -- review --require-record=dynamic-unit-icon-unitspec`
  Result: pass
  Evidence: review-record gate completed successfully with 0 errors and 0 warnings.

Manual/visual gates:
- Check: visual inspection not required for this architecture read-path slice.
  Result: not run.
  Evidence: code only changes which existing art path is used by a compatibility entrypoint.

Reviewer result:
- Status: pass.
- Required fixes: none.
- Residual risks: old `UnitKind` / `UnitPresentationCatalog` compatibility APIs still exist for other callers. This slice only removes the legacy presentation read from `DynamicUnitIcon.DrawUnitIcon(...)`.

TODO update:
- Items marked done: `DynamicUnitIcon UnitSpec icon cleanup` under UnitSpec architecture phase 3 duplicate-data cleanup.
- Items left open: broad UnitSpec duplicate-data cleanup and final `UnitKind` / `UnitCatalog` deletion remain open.
- Reason: legacy icon callers now draw through UnitSpec art, but other compatibility surfaces still remain.
