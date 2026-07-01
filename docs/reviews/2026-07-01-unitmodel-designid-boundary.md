# Review Record - UnitModel DesignId boundary

Step: M1 UnitModel presentation read-path cleanup
Milestone: M1 EntityWorld Becomes Authoritative
Owner AI: Codex
Reviewer AI: ReviewGate
Integrator AI: Codex

Scope:
- Files/folders: `scripts/core/units/UnitModel.cs`, `scripts/core/units/UnitDeathInfo.cs`, `scripts/world/UnitView.cs`, `scripts/world/FootprintLayer.SpecStyle.cs`, `scripts/ui/DynamicUnitIcon.cs`, `scripts/BattleRoot.Selection.cs`, `scripts/BattleRoot.HudSync.cs`, `scripts/BattleRoot.Process.cs`, `tools/ReviewGate/checks/unit-spec/`, `TODO.md`.
- Non-goals: deleting `UnitKind`, changing old GameState simulation identity, or migrating SelectionController/ControlGroupController/PathDebugLayer in this slice.

Implementation summary:
- Added `DesignId`, `Spec`, and `RuntimeDescriptor` compatibility-boundary properties to legacy `UnitModel`.
- Added `DesignId` to `UnitDeathInfo` without changing the existing constructor shape.
- Migrated `UnitView` and `FootprintLayer` to consume `UnitModel.Spec` and `UnitModel.RuntimeDescriptor`.
- Migrated BattleRoot selection, culling, and unit death VFX read paths to consume `UnitModel`/`UnitDeathInfo.DesignId`.
- Migrated `DynamicUnitIcon` legacy `UnitKind` fallback to resolve a design id first, then draw the UnitSpec art path.
- Updated ReviewGate so these presentation paths are locked to the UnitModel/UnitDesign-id boundary instead of scattered `UnitKindDesignBridge.TryGetSpec/TryGetRuntimeDescriptor` calls.

Automated gates:
- Command: `dotnet build ProceduralRts.csproj --no-restore`
  Result: pass
  Evidence: local run completed with 0 warnings and 0 errors.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore -- unitviewunitspecbridge`
  Result: pass
  Evidence: local run completed with 0 errors and 0 warnings.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore -- dynamicuniticonunitspec`
  Result: pass
  Evidence: local run completed with 0 errors and 0 warnings.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore -- battlerootunitspecreadpath`
  Result: pass
  Evidence: local run completed with 0 errors and 0 warnings.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore -- footprintunitspecbridge`
  Result: pass
  Evidence: local run completed with 0 errors and 0 warnings.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore -- battlerootunitcullingunitspec`
  Result: pass
  Evidence: local run completed with 0 errors and 0 warnings.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore -- battlerootidleharvestunitspec`
  Result: pass
  Evidence: local run completed with 0 errors and 0 warnings.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore`
  Result: pass
  Evidence: full ReviewGate completed with 0 errors and 0 warnings.
- Command: `dotnet run --project tools/VerifyAll/VerifyAll.csproj --no-restore`
  Result: pass
  Evidence: VerifyAll completed all 23 steps successfully, including build, SimReplay, CombatBehavior, SimulationSmoke, ReviewGate, perf smoke, and Godot headless QA.

Reviewer result:
- Status: pass
- Required fixes: none from narrow gates.
- Residual risks: other legacy UnitKind read paths still exist in controllers, debug layers, QA fixtures, and old GameState simulation compatibility.

TODO update:
- Items marked done: none; this is a progress slice under the broader UnitSpec phase-3 cleanup.
- Items left open: final UnitSpec duplicate-data cleanup and `UnitKind`/`BuildingKind`/`UnitCatalog` deletion.
- Reason: presentation read paths are narrower now, but deleting the identity enums requires moving the remaining gameplay/controller compatibility edges.
