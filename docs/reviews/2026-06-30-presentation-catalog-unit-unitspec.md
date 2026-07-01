# Review Record - PresentationCatalog UnitSpec unit cleanup

Step: PresentationCatalog UnitSpec unit cleanup
Milestone: M1 EntityWorld Becomes Authoritative / UnitSpec duplicate-data cleanup
Owner AI: Codex
Reviewer AI: ReviewGate presentationcatalogunitunitspec / Integrator
Integrator AI: Codex

Scope:
- Files/folders: `scripts/core/PresentationCatalog.cs`, `tools/ReviewGate/Program.cs`, `TODO.md`, `docs/reviews/2026-06-30-presentation-catalog-unit-unitspec.md`.
- Non-goals: deleting `UnitKind`, deleting `UnitCatalog`, deleting `UnitPresentationCatalog`, changing relation color rules, changing faction display data, changing unit balance/stats, or changing building presentation.

Implementation summary:
- Moved `PresentationCatalog.Unit(...)` off direct `UnitPresentationCatalog.For(kind)` reads.
- Legacy `UnitKind` inputs now resolve through `UnitKindDesignBridge.TryGetSpec(...)`.
- Unit presentation metadata now comes from `UnitPresentationCatalog.ForSpec(...)`, including role glyph fallback through UnitSpec art metadata.
- Preserved existing faction, owner, relation, and minimap color policy.
- Stopped exposing legacy `UnitVisualDescriptor` data through this shared compatibility descriptor path.
- Added `ReviewGate presentationcatalogunitunitspec` to prevent this path from regressing to legacy UnitKind presentation data.

Automated gates:
- Command: `dotnet build ProceduralRts.csproj --no-restore`
  Result: pass
  Evidence: build completed successfully with 0 warnings and 0 errors.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore -- presentationcatalogunitunitspec`
  Result: pass
  Evidence: narrow ReviewGate mode completed successfully with 0 errors and 0 warnings.
- Command: `dotnet run --project tools/CombatBehavior/CombatBehavior.csproj --no-restore`
  Result: pass
  Evidence: CombatBehavior completed successfully.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore`
  Result: pass
  Evidence: full ReviewGate completed successfully with 0 errors and 0 warnings.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore -- review --require-record=presentation-catalog-unit-unitspec`
  Result: pass
  Evidence: review-record gate completed successfully with 0 errors and 0 warnings.

Manual/visual gates:
- Check: visual inspection not required.
  Result: not run.
  Evidence: this slice changes a shared descriptor read path; active unit drawing paths already use UnitSpec-specific renderers.

Reviewer result:
- Status: pass.
- Required fixes: none.
- Residual risks: `PresentationCatalog.Unit(...)` still accepts legacy `UnitKind` because callers have not been deleted yet. Broader `UnitKind`, `UnitCatalog`, and `UnitPresentationCatalog` compatibility surfaces remain open for later cleanup.

TODO update:
- Items marked done: `PresentationCatalog UnitSpec unit cleanup` under UnitSpec architecture phase 3 duplicate-data cleanup.
- Items left open: broad UnitSpec duplicate-data cleanup and final `UnitKind` / `UnitCatalog` deletion remain open.
- Reason: this removes one shared legacy presentation read path but does not delete the old compatibility types.
