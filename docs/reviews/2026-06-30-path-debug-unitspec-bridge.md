# Review Record - PathDebug UnitSpec bridge

Step: UnitSpec architecture phase 3 duplicate-data cleanup PathDebug read-path slice
Milestone: M1 EntityWorld Becomes Authoritative
Owner AI: Worker A / Codex
Reviewer AI: ReviewGate pathdebugunitspecbridge
Integrator AI: Worker A / Codex

Scope:
- Files/folders: `scripts/core/units/UnitKindDesignBridge.cs`, `scripts/world/PathDebugLayer.cs`, `tools/ReviewGate/Program.cs`, `TODO.md`, `docs/reviews/2026-06-30-path-debug-unitspec-bridge.md`.
- Non-goals: deleting `UnitKind`, deleting `UnitCatalog`, adding live `UnitDesignId` to `UnitModel`, changing pathfinding/movement behavior, changing combat/fog/sandbox UI, or migrating unit silhouettes.

Implementation summary:
- Added `UnitKindDesignBridge.TryGetSpec(...)` as a narrow compatibility resolver from legacy `UnitKind` to authored `UnitSpec`.
- Moved `PathDebugLayer` path coloring away from `GameState.UnitDefinitionFor(unit.Kind)` and legacy `UnitKind.Harvester` checks.
- Path debug colors now read UnitSpec presentation accents through `UnitPresentationCatalog.ForSpec(...)`, while economy/worker special coloring uses UnitSpec role tags.
- Added `ReviewGate pathdebugunitspecbridge` and advanced the older GameState definition cleanup gate so this runtime debug path cannot regress to direct legacy definition reads.

Automated gates:
- Command: `dotnet build ProceduralRts.csproj --no-restore`
  Result: pass
  Evidence: build completed successfully with 0 warnings and 0 errors.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj pathdebugunitspecbridge --no-restore`
  Result: pass
  Evidence: ReviewGate pathdebugunitspecbridge completed with 0 errors and 0 warnings.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj gamestatedefinitionspubliccleanup --no-restore`
  Result: pass
  Evidence: ReviewGate gamestatedefinitionspubliccleanup completed with 0 errors and 0 warnings after its PathDebug expectation was advanced.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj review --require-record=path-debug-unitspec-bridge --no-restore`
  Result: pass
  Evidence: ReviewGate review found this durable record and completed with 0 errors and 0 warnings.

Manual/visual gates:
- Check: visual inspection not required.
  Result: not run.
  Evidence: this slice only changes debug path color metadata authority; path geometry and rendering commands are unchanged.

Reviewer result:
- Status: pass
- Required fixes: none.
- Residual risks: `UnitModel` still exposes legacy `UnitKind`, so this slice still needs `UnitKindDesignBridge` until live unit models carry UnitDesign identity directly. `UnitCatalog` and `UnitVisualDescriptor` remain compatibility layers for later M1 cleanup.

TODO update:
- Items marked done: none.
- Items left open: parent UnitSpec duplicate-data cleanup remains open.
- Reason: this is one verified runtime read-path cleanup slice, not full deletion of legacy unit compatibility data.
