Step: Route remaining BattleRoot building fallback definition reads through BuildSpecCatalog as a bounded M1 migration cleanup slice.
Milestone: M1 EntityWorld Becomes Authoritative
Owner AI: Codex
Reviewer AI: Codex review pass
Integrator AI: Codex

Scope:
- Files/folders: `scripts/BattleRoot.cs`, `tools/ReviewGate/Program.cs`, `TODO.md`.
- Updated removed-building alert labels, old-runtime production-complete producer labels, old-runtime minimap building footprints, old-runtime building culling footprints, selected-building average health, and old-runtime building HUD sight/max HP to read from `BuildSpecCatalog`.
- Removed `BattleRoot` building fallback calls to `_state.Definition(building)`.
- Non-goals: no deletion of old runtime branches, no `BuildingKind` removal, no Unit/UnitCatalog migration.

Automated gates:
- Command: `dotnet build ProceduralRts.csproj --no-restore`
  Result: pass
  Evidence: build completed successfully with 0 warnings and 0 errors.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj battlerootbuildspecfallbacks --no-restore`
  Result: pass
  Evidence: BattleRoot BuildSpec fallback gate completed successfully.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore`
  Result: pass
  Evidence: full ReviewGate completed successfully.
- Command: `dotnet run --project tools/VerifyAll/VerifyAll.csproj --no-restore`
  Result: pass
  Evidence: VerifyAll completed all 14 steps successfully.

Reviewer result:
- Status: pass.
- Design note: this keeps old-runtime fallback behavior alive but makes the building data source converge on the unified BuildSpec bridge.
- Required fixes: none.

Status:
- Pass.

Residual risks:
- Old-runtime branches still exist behind the migration flag and still read legacy unit definitions.
- `BuildSpecCatalog` still merges `GameState.BuildingDefinitions` during migration.
- Full `UnitBattlefieldBuildingTarget` and legacy catalog removal remains open.

TODO update:
- Marked done: nested M1 slice `BattleRoot BuildSpec fallback cleanup`.
- Left open: parent migration cleanup and legacy runtime deletion.
