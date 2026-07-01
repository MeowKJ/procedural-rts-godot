# Review Record - UnitModel runtime boundary cleanup

Step: M1 UnitModel controller/runtime read-path cleanup
Milestone: M1 EntityWorld Becomes Authoritative
Owner AI: Codex
Reviewer AI: ReviewGate
Integrator AI: Codex

Scope:
- Files/folders: `scripts/controllers/SelectionController.Utilities.cs`, `scripts/controllers/ControlGroupController.cs`, `scripts/world/PathDebugLayer.cs`, `scripts/world/CombatEffectsLayer.Pulses.cs`, `scripts/core/GameState.cs`, `scripts/core/game-state/`, `scripts/core/ai/`, `scripts/core/units/UnitDeathInfo.cs`, `scripts/BattleRoot.Selection.cs`, `scripts/BattleRoot.HudSync.cs`, `tools/SimulationSmoke/Program.cs`, `tools/ReviewGate/checks/unit-spec/`, `TODO.md`.
- Non-goals: deleting `UnitKind`, deleting `BuildingKind`, changing balance values, changing movement/combat behavior, or replacing the remaining old runtime compatibility entry points.

Implementation summary:
- Migrated SelectionController, ControlGroupController, PathDebugLayer, CombatEffectsLayer, and SimulationSmoke old-runtime unit metadata reads to the `UnitModel` compatibility boundary via `unit.Spec` or `unit.RuntimeDescriptor`.
- Added `GameState.IsHarvesterUnit(UnitModel)` and moved internal AI, selection, threat, production, and harvest callers away from `IsHarvesterUnit(unit.Kind)`.
- Removed the internal `GameState.UnitRuntimeDescriptorFor(UnitModel)` overload; runtime paths now read `unit.RuntimeDescriptor`, while the remaining `UnitRuntimeDescriptorFor(UnitKind)` helper is kept only for true legacy UnitKind inputs.
- Changed `UnitDeathInfo` to store `DesignId`, with GameState removal/death paths passing `unit.DesignId`.
- Migrated HUD selection summary and single-unit portrait paths to carry `DesignId` instead of regrouping by legacy `UnitKind`.
- Updated ReviewGate UnitSpec checks so these paths are locked to the UnitModel/UnitDesign-id boundary and cannot silently reintroduce scattered UnitKind bridge reads.

Automated gates:
- Command: `dotnet build ProceduralRts.csproj --no-restore`
  Result: pass
  Evidence: local run completed with 0 warnings and 0 errors.
- Command: `dotnet build tools/ReviewGate/ReviewGate.csproj --no-restore`
  Result: pass
  Evidence: local run completed with 0 warnings and 0 errors.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore`
  Result: pass
  Evidence: full ReviewGate completed with 0 errors and 0 warnings.
- Command: `dotnet run --project tools/CombatBehavior/CombatBehavior.csproj --no-restore`
  Result: pass
  Evidence: local run completed with all behavior checks passing.
- Command: `dotnet run --project tools/SimulationSmoke/SimulationSmoke.csproj --no-restore`
  Result: pass
  Evidence: local run completed with all smoke checks passing.
- Command: `dotnet run --project tools/VerifyAll/VerifyAll.csproj --no-restore`
  Result: pass
  Evidence: VerifyAll completed all 23 steps successfully, including build, SimReplay, CombatBehavior, SimulationSmoke, ReviewGate, perf smoke, and Godot headless QA.

Reviewer result:
- Status: pass
- Required fixes: none.
- Residual risks: `UnitKind` and `BuildingKind` still remain as compatibility/identity edges; deleting them is a separate M1 final-deletion item.

TODO update:
- Items marked done: none; this is a progress slice under the broader UnitSpec phase-3 duplicate-data cleanup.
- Items left open: final UnitSpec duplicate-data cleanup and final legacy `UnitKind` / `BuildingKind` / `UnitCatalog` deletion.
- Reason: controller/runtime reads are narrower now, but the old enum identity cannot be removed until seeding, production output, and building identity boundaries are migrated.
