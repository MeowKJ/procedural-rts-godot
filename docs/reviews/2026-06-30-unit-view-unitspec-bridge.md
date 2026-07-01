# Review Record - UnitView UnitSpec bridge

Step: UnitSpec architecture phase 3 duplicate-data cleanup UnitView read-path slice
Milestone: M1 EntityWorld Becomes Authoritative
Owner AI: Codex
Reviewer AI: ReviewGate unitviewunitspecbridge
Integrator AI: Codex

Scope:
- Files/folders: `scripts/world/UnitView.cs`, `tools/ReviewGate/Program.cs`, `TODO.md`, `docs/reviews/2026-06-30-unit-view-unitspec-bridge.md`.
- Non-goals: deleting `UnitKind`, deleting `UnitCatalog`, removing legacy `UnitView`, changing `UnitInstanceView`, changing movement/combat/fog behavior, or migrating selection/controller read paths.

Implementation summary:
- Replaced legacy `UnitView` draw-time reads from `State.Definition(Unit)` and `PresentationCatalog.Unit(Unit.Kind, ...)` with a `TryResolveUnitSpecStyle(...)` path.
- `UnitView` now resolves `UnitModel.Kind` through `UnitKindDesignBridge.TryGetSpec(...)` and `TryGetRuntimeDescriptor(...)`.
- Legacy unit rendering now uses `UnitPresentationCatalog.ForSpec(spec).Art` plus `UnitVisualRenderer.DrawUnitArtRecipe(...)`, with owner color separated through `EntityRenderPalette` and environment tone from `State.VisualTheme`.
- Cargo/status affordances now use UnitSpec role tags instead of `UnitKind.Harvester`.
- Added `ReviewGate unitviewunitspecbridge` so this live legacy-view path cannot drift back to legacy UnitDefinition / UnitVisualDescriptor reads.

Automated gates:
- Command: `dotnet build ProceduralRts.csproj --no-restore`
  Result: pass
  Evidence: build completed successfully with 0 warnings and 0 errors.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore -- unitviewunitspecbridge`
  Result: pass
  Evidence: ReviewGate unitviewunitspecbridge completed with 0 errors and 0 warnings.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore -- unitkinddesignbridge`
  Result: pass
  Evidence: ReviewGate unitkinddesignbridge completed with 0 errors and 0 warnings after reusing its UnitKind bridge helpers.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore -- review --require-record=unit-view-unitspec-bridge`
  Result: pass
  Evidence: ReviewGate review found this durable record and completed with 0 errors and 0 warnings.
- Command: `dotnet run --project tools/VerifyAll/VerifyAll.csproj --no-restore`
  Result: pass
  Evidence: VerifyAll completed 23/23 steps, including ReviewGate, Godot entity-unit and legacy-unit Battle headless runs, PerfSmoke, and ActiveBattlePerfQa.

Manual/visual gates:
- Check: Visual inspection not required for this narrow source-of-truth migration.
  Result: not run.
  Evidence: the verified change keeps legacy `UnitView` geometry/camera lifecycle intact while swapping the metadata and art recipe source; Godot headless is covered by the follow-up integration pass.

Reviewer result:
- Status: pass-with-warnings
- Required fixes: none in this scoped slice.
- Residual risks: `UnitView` still exists as a legacy compatibility view and still receives `UnitModel.Kind`; `UnitCatalog`, `UnitVisualDescriptor`, `SelectionController`, `BattleRoot` death/presentation fallbacks, and `FootprintLayer` retain additional M1 cleanup work.

TODO update:
- Items marked done: none.
- Items left open: parent UnitSpec duplicate-data cleanup remains open.
- Reason: this removes one live legacy view read path, but the broad M1 duplicate-data cleanup is not complete until the remaining compatibility layers are deleted.
