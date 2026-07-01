# Review Record - UnitSpec duplicate cleanup next

Step: UnitSpec duplicate-data cleanup production presentation slice
Milestone: M1 EntityWorld Becomes Authoritative
Owner AI: Codex implementation worker
Reviewer AI: Codex self-review
Integrator AI: Main thread

Scope:
- Files/folders: `scripts/core/ProductionOptionState.cs`, `scripts/core/ProductionPresentationDescriptor.cs`, `scripts/core/UnitPresentationCatalog.cs`, `scripts/core/GameState.cs`, `scripts/core/units/runtime/UnitBattlefield.cs`, `scripts/ui/HudLayer.cs`, `tools/CombatBehavior/Program.cs`, `tools/ReviewGate/Program.cs`, `docs/reviews/2026-06-29-unitspec-duplicate-cleanup-next.md`.
- Non-goals: deleting `UnitKind`, deleting `UnitCatalog`, changing production command semantics, changing building/guard/movement systems, or updating `TODO.md`.

Implementation summary:
- Added UnitDesign identity, short code, and accent to `ProductionOptionState` so production UI state can carry UnitSpec presentation data directly.
- Added faction-aware and UnitSpec-aware production presentation entrypoints in `UnitPresentationCatalog` while keeping old `ProductionKind` presentation as a compatibility path.
- Routed `UnitBattlefield.ProductionOptionStates` through `UnitPresentationCatalog.ForProductionSpec`, and updated `HudLayer.CommandButton` to draw `DynamicUnitIcon.DrawUnitDesignIcon` when a UnitDesign id is present.
- Added CombatBehavior assertions for dog/cat faction-specific production presentation and runtime production option state.
- Added `ReviewGate unitspeccleanup` to guard the new path against falling back to legacy production presentation data.

Automated gates:
- Command: `dotnet build ProceduralRts.csproj --no-restore`
  Result: pass
  Evidence: build completed successfully with 0 warnings and 0 errors.
- Command: `dotnet run --project tools/CombatBehavior/CombatBehavior.csproj --no-restore`
  Result: pass
  Evidence: CombatBehavior completed successfully after adding UnitSpec production presentation assertions.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj unitspeccleanup --no-restore`
  Result: pass
  Evidence: ReviewGate completed with 0 errors and 0 warnings.

Manual/visual gates:
- Check: visual inspection not required.
  Result: not run.
  Evidence: this slice changes deterministic data flow and icon selection; no scene layout or art geometry was changed.

Reviewer result:
- Status: pass-with-warnings
- Required fixes: none identified in the scoped self-review.
- Residual risks: old `ProductionKind` and `UnitKind` compatibility paths remain for legacy GameState/HUD initialization; this is a read-path cleanup slice, not a deletion slice.

TODO update:
- Items marked done: none.
- Items left open: parent UnitSpec duplicate-data cleanup and later deletion of old unit/faction catalogs.
- Reason: main thread owns TODO integration after gates and independent review.
