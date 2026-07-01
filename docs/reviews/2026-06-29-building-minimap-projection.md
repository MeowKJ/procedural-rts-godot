Step: Route live building minimap pips through UnitBattlefield EntityWorld projections as a bounded M1 migration cleanup slice.
Milestone: M1 EntityWorld Becomes Authoritative
Owner AI: Codex
Reviewer AI: Codex review pass
Integrator AI: Codex

Scope:
- Files/folders: `scripts/core/sim/BuildingPresentationProjection.cs`, `scripts/core/units/runtime/UnitBattlefield.cs`, `scripts/BattleRoot.cs`, `tools/CombatBehavior/Program.cs`, `tools/FogOfWarQa/Program.cs`, `tools/ReviewGate/Program.cs`, `TODO.md`.
- Added `BuildingMinimapProjection` for live building minimap snapshots.
- Added `UnitBattlefield.BuildingMinimapProjections(...)`, which keeps self/allied buildings visible by owner relation and accepts an explored-fog rectangle predicate for enemy buildings.
- Updated `BattleRoot.RefreshMinimap` so UnitDesign runtime building pips use UnitBattlefield projections, while the legacy `State.Buildings` path remains only as the old-runtime fallback.
- Updated FogOfWarQa to prove explored static enemy building memory works through the UnitBattlefield minimap projection path.
- Non-goals: no unit minimap migration, no HUD redraw rewrite, no deletion of `UnitBattlefieldBuildingTarget`.

Automated gates:
- Command: `dotnet build ProceduralRts.csproj --no-restore`
  Result: pass
  Evidence: build completed successfully with 0 warnings and 0 errors.
- Command: `dotnet run --project tools/CombatBehavior/CombatBehavior.csproj --no-restore`
  Result: pass
  Evidence: combat behavior assertions completed successfully, including the building minimap projection assertion.
- Command: `dotnet run --project tools/FogOfWarQa/FogOfWarQa.csproj --no-restore`
  Result: pass
  Evidence: fog QA completed successfully, including explored-memory minimap projection coverage.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj buildingminimapprojection --no-restore`
  Result: pass
  Evidence: building minimap projection gate completed successfully.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore`
  Result: pass
  Evidence: full ReviewGate completed successfully.
- Command: `dotnet run --project tools/VerifyAll/VerifyAll.csproj --no-restore`
  Result: pass
  Evidence: VerifyAll completed all 14 steps successfully.

Reviewer result:
- Status: pass.
- Design note: fog remains in `GameState.FogOfWar`; UnitBattlefield only accepts a pure explored-rect predicate and does not become fog authority.
- Required fixes: none.

Status:
- Pass.

Residual risks:
- Unit minimap pips still come from the legacy `State.Units` list in `BattleRoot`.
- Legacy `BuildingModel` minimap fallback remains for the old runtime.
- Full `UnitBattlefieldBuildingTarget` removal remains open.

TODO update:
- Marked done: nested M1 slice `UnitBattlefield building minimap projection bridge`.
- Left open: parent migration cleanup and legacy runtime deletion.
