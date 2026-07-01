Step: Add a building-specific presentation projection for the next bounded M1 migration cleanup slice.
Milestone: M1 EntityWorld Becomes Authoritative
Owner AI: Codex
Reviewer AI: Codex review pass
Integrator AI: Codex

Scope:
- Files/folders: `scripts/core/sim/BuildingPresentationProjection.cs`, `scripts/core/units/runtime/UnitBattlefield.cs`, `scripts/world/BuildingView.cs`, `scripts/BattleRoot.cs`, `tools/CombatBehavior/Program.cs`, `tools/ReviewGate/Program.cs`, `TODO.md`.
- Added `BuildingPresentationProjection` so building views can read footprint, production queue, rally point, powered state, and construction progress from EntityWorld components.
- Wired `UnitBattlefield.BuildingPresentationProjection` and `BattleRoot` building view providers.
- Updated `BuildingView` to prefer projected building presentation state while preserving legacy `BuildingModel` fallbacks during migration.
- Added focused CombatBehavior and ReviewGate proof.
- Non-goals: no building selection command migration, no deletion of `UnitBattlefieldBuildingTarget`, no HUD/AI production summary migration, no removal of `BuildingDefinition`/`BuildingKind`.

Automated gates:
- Command: `dotnet build ProceduralRts.csproj --no-restore`
  Result: pass
  Evidence: build completed with 0 warnings and 0 errors.
- Command: `dotnet run --project tools/CombatBehavior/CombatBehavior.csproj --no-restore`
  Result: pass
  Evidence: CombatBehavior passed with the building presentation projection assertion.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj buildingpresentationprojection --no-restore`
  Result: pass
  Evidence: `Errors: 0`, `Warnings: 0`, `ReviewGate passed.`
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj review --require-record=building-presentation-projection --no-restore`
  Result: pass
  Evidence: `Errors: 0`, `Warnings: 0`, `ReviewGate passed.`
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore`
  Result: pass
  Evidence: `Errors: 0`, `Warnings: 0`, `ReviewGate passed.`
- Command: `dotnet run --project tools/VerifyAll/VerifyAll.csproj --no-restore`
  Result: pass
  Evidence: all 14 VerifyAll steps passed.

Reviewer result:
- Status: pass
- Design note: building selection intentionally remains on the legacy `GameState` path because building selection is not yet mirrored through EntityWorld commands; forcing it into the projection now would hide selection outlines.
- The projection clones queue items so the view receives a snapshot instead of mutable queue object references.

Status:
- Pass.

Residual risks:
- `BuildingView` still falls back to `BuildingModel` and still uses legacy selection, delivery/rally pulses, dock pulses, and exploration checks.
- HUD production details and building selection/rally command overlays still read legacy building collections.
- `UnitBattlefieldBuildingTarget` remains until all gameplay-facing building state is EntityWorld-owned.

TODO update:
- Marked done: nested M1 slice `BuildingView building presentation projection bridge`.
- Left open: parent migration cleanup, legacy runtime deletion, building selection command migration, and broader EntityWorld authority work.
