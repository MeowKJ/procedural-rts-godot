# Review Record - SelectionController UnitSpec bridge

Step: UnitSpec architecture phase 3 duplicate-data cleanup SelectionController read-path slice
Milestone: M1 EntityWorld Becomes Authoritative
Owner AI: Codex
Reviewer AI: ReviewGate selectionunitspecbridge
Integrator AI: Codex

Scope:
- Files/folders: `scripts/controllers/SelectionController.cs`, `tools/ReviewGate/Program.cs`, `TODO.md`, `docs/reviews/2026-06-30-selection-unitspec-bridge.md`.
- Non-goals: editing `FootprintLayer`, editing `BattleRoot`, deleting `UnitKind`, deleting `UnitCatalog`, changing selection box behavior, changing command preview branch ordering, or moving relation colors into unit body art.

Implementation summary:
- Replaced old-runtime SelectionController unit command-line feedback reads from `State.Definition(unit)` with `UnitKindDesignBridge.TryGetRuntimeDescriptor(...)`.
- Replaced old-runtime unit hover radius reads from `State.Definition(hoveredUnit)` with UnitSpec runtime descriptor radius data while keeping hover color on `State.RelationOverlay(...)`.
- Migrated legacy selected-harvester checks from `UnitKind.Harvester` to `UnitKindDesignBridge.TryGetSpec(...)` plus UnitSpec economy/worker role tags and authored harvest ability metadata.
- Added `ReviewGate selectionunitspecbridge` so this controller path cannot regress to direct legacy `GameState` unit-definition reads or legacy harvester kind checks.

Automated gates:
- Command: `dotnet build ProceduralRts.csproj --no-restore`
  Result: pass
  Evidence: build completed successfully.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore -- selectionunitspecbridge`
  Result: pass
  Evidence: ReviewGate selectionunitspecbridge completed with 0 errors and 0 warnings.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore -- review --require-record=selection-unitspec-bridge`
  Result: pass
  Evidence: ReviewGate review found this durable record and completed with 0 errors and 0 warnings.

Manual/visual gates:
- Check: visual inspection not required for this narrow metadata-source migration.
  Result: not run.
  Evidence: selection box, hover outline, and command preview branch structure remain in SelectionController; only the old-unit metadata source changed.

Reviewer result:
- Status: pass
- Required fixes: none.
- Residual risks: `SelectionController` still receives legacy `UnitModel.Kind` on the old-runtime fallback path, so this slice still depends on `UnitKindDesignBridge` until legacy unit compatibility is deleted.

TODO update:
- Items marked done: none.
- Items left open: parent UnitSpec duplicate-data cleanup remains open.
- Reason: this is one scoped SelectionController read-path cleanup, not full deletion of legacy unit compatibility data.
