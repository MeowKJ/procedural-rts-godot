# Review Record - UnitCatalog legacy deletion

Step: UnitCatalog legacy deletion
Milestone: M1 EntityWorld Becomes Authoritative / UnitSpec duplicate-data cleanup
Owner AI: Codex
Reviewer AI: ReviewGate unitcataloglegacydeleted / Integrator
Integrator AI: Codex

Scope:
- Files/folders: `scripts/core/UnitCatalog.cs`, `scripts/core/UnitCatalogEntry.cs`, `scripts/core/UnitPresentationDescriptor.cs`, `scripts/core/UnitVisualDescriptor.cs`, `scripts/core/UnitTurretVisualKind.cs`, `scripts/core/UnitVisualRenderer.cs`, `scripts/core/EntityPresentationDescriptor.cs`, `scripts/core/PresentationCatalog.cs`, `scripts/core/units/UnitKindDesignBridge.cs`, `tools/ReviewGate/Program.cs`, `TODO.md`, `docs/reviews/2026-06-30-unitcatalog-legacy-deleted.md`.
- Non-goals: deleting `UnitKind`, deleting `UnitDefinition`, changing unit balance/stats, changing production behavior, changing UnitSpec art recipes, or migrating building/runtime architecture.

Implementation summary:
- Deleted the remaining unused `UnitCatalog` compatibility catalog and its legacy presentation/visual descriptor files.
- Removed the empty `UnitVisual` payload from `EntityPresentationDescriptor` and the corresponding null writes in `PresentationCatalog`.
- Removed the legacy `UnitKindDesignBridge.CompatibilityPresentation(...)` projection so the bridge only maps old `UnitKind` values to UnitSpec runtime data.
- Removed legacy silhouette/turret/color rendering helpers from `UnitVisualRenderer`; active drawing remains through `DrawUnitArtRecipe(...)`.
- Migrated ReviewGate checks that previously required UnitCatalog compatibility entries so they now validate UnitDesign/UnitSpec evidence and the UnitKindDesignBridge mapping.
- Added `ReviewGate unitcataloglegacydeleted` to keep the deleted files and symbols from returning.

Automated gates:
- Command: `dotnet build ProceduralRts.csproj --no-restore`
  Result: pass
  Evidence: build completed successfully with 0 warnings and 0 errors.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore -- unitcataloglegacydeleted`
  Result: pass
  Evidence: narrow ReviewGate mode completed successfully with 0 errors and 0 warnings.
- Command: `dotnet run --project tools/CombatBehavior/CombatBehavior.csproj --no-restore`
  Result: pass
  Evidence: CombatBehavior completed successfully.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore`
  Result: pass
  Evidence: full ReviewGate completed successfully with 0 errors and 0 warnings.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore -- review --require-record=unitcatalog-legacy-deleted`
  Result: pass
  Evidence: review-record gate completed successfully with 0 errors and 0 warnings.
- Command: `dotnet run --project tools/VerifyAll/VerifyAll.csproj --no-restore`
  Result: pass
  Evidence: VerifyAll completed 23/23 steps successfully, including build, SimReplay, CombatBehavior, FogOfWarQa, PerfSmoke, ReviewGate, and Godot headless QA.

Manual/visual gates:
- Check: visual inspection not required.
  Result: not run.
  Evidence: architecture deletion only; active rendering still uses existing UnitSpec art recipes.

Reviewer result:
- Status: pass.
- Required fixes: none.
- Residual risks: old `UnitKind` and `UnitDefinition` still remain as runtime compatibility surfaces until the final entity-path deletion milestone.

TODO update:
- Items marked done: `UnitCatalog legacy deletion` under UnitSpec architecture phase 3 duplicate-data cleanup.
- Items left open: broad UnitSpec duplicate-data cleanup, building migration cleanup, and final `UnitKind` / `BuildingKind` / `UnitCatalog` legacy deletion milestone remain open.
- Reason: the dead catalog/presentation/visual descriptor chain is removed, but broader legacy enum/runtime compatibility still exists.
