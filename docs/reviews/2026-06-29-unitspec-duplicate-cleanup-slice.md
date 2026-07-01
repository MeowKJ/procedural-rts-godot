Step: Add a UnitSpec-driven runtime definition read entrypoint.
Milestone: M1 EntityWorld Becomes Authoritative
Owner AI: Worker-M1 / Codex
Reviewer AI: Codex review pass
Integrator AI: Main thread

Scope:
- Files/folders: `scripts/core/units/UnitSpecRuntimeDescriptor.cs`, `scripts/core/units/UnitDesignDefinitionCatalog.cs`, `tools/CombatBehavior/Program.cs`, `tools/ReviewGate/Program.cs`, `docs/reviews/2026-06-29-unitspec-duplicate-cleanup-slice.md`.
- Added `UnitDesignDefinitionCatalog.ForDesign/ForSpec` and design-id keyed `RuntimeDescriptors` as the UnitSpec-side read API for runtime definition data.
- Added `UnitSpecRuntimeDescriptor` as a UnitKind-free descriptor for stats, movement, collision, primary weapon runtime data, faction accent, tech tier, and role tags.
- Kept `UnitKind`, `UnitCatalog`, `FactionCatalog`, and `GameState` compatibility APIs unchanged.
- Non-goals: deleting legacy catalogs, migrating live `GameState`, changing unit balance, changing UI, movement, pathing, construction, or production behavior.

Implementation summary:
- `UnitDesignDefinitionCatalog.ForSpec` projects directly from `UnitSpec`, `WeaponCatalog`, `AmmoDefinition`, and `SoftOldCityPalette` without reading `UnitCatalog` or `FactionCatalog`.
- `CompatibilityDefinition(string designId, UnitKind compatibilityKind)` is explicit at the boundary where a legacy `UnitDefinition` view is still needed.
- `CombatBehavior` now routes one unit-class/runtime-definition QA slice through `UnitDesignDefinitionCatalog.RuntimeDescriptors` instead of `GameState.UnitDefinitionValues`.
- `ReviewGate unitdesigndefinitioncatalog` verifies the new descriptor is UnitKind-free, the catalog avoids `UnitCatalog`/`FactionCatalog`, and CombatBehavior proves the new read path.

Automated gates:
- Command: `dotnet build ProceduralRts.csproj --no-restore`
  Result: pass
  Evidence: build completed successfully with 0 warnings and 0 errors.
- Command: `dotnet run --project tools/CombatBehavior/CombatBehavior.csproj --no-restore`
  Result: pass
  Evidence: CombatBehavior completed successfully.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore`
  Result: fail, out-of-scope existing gate
  Evidence: full ReviewGate reported `scripts/core/sim/systems/PathfindingSystem.cs` must implement `ISimSystem`; movement/pathing is outside this slice.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore unitdesigndefinitioncatalog`
  Result: pass
  Evidence: dedicated UnitDesign definition catalog gate completed with 0 errors and 0 warnings.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore review --require-record=unitspec-duplicate-cleanup-slice`
  Result: pass
  Evidence: review record gate completed with 0 errors and 0 warnings.

Manual/visual gates:
- Check: visual inspection not required.
  Result: not run.
  Evidence: this slice only adds data projection and QA gates; no rendering path was changed.

Reviewer result:
- Status: pass with warnings.
- Required fixes: none identified in the scoped review.
- Residual risks: legacy `GameState.UnitDefinitionFor`, `UnitCatalog.Definitions`, and faction rosters still exist for old UnitKind flows.

Status:
- Pass with warnings.

Residual risks:
- The broad duplicate-data cleanup remains open.
- Live gameplay callers still use old `UnitDefinition` APIs where the runtime has not yet migrated.
- The compatibility method still accepts `UnitKind`; it is intentionally isolated as a boundary shim for later deletion.

TODO update:
- Items marked done: none.
- Items left open: parent UnitSpec duplicate-data cleanup and later deletion of `UnitKind` / `UnitCatalog` / `FactionCatalog` authority.
- Reason: this is a small proof slice and the main thread owns TODO integration.
