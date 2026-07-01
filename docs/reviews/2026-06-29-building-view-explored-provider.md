Step: Route BuildingView explored-memory checks through an injected provider as a bounded M1 migration cleanup slice.
Milestone: M1 EntityWorld Becomes Authoritative
Owner AI: Codex
Reviewer AI: Codex review pass
Integrator AI: Codex

Scope:
- Files/folders: `scripts/world/BuildingView.cs`, `scripts/BattleRoot.cs`, `tools/ReviewGate/Program.cs`, `TODO.md`.
- Added `BuildingView.ExploredProvider` so draw visibility can consume explored-memory checks supplied by the live root.
- Updated projected building draw visibility to use projected owner plus the injected explored-memory provider.
- Updated legacy building draw visibility to preserve friendly visibility while sharing the same explored-memory provider.
- Removed `BuildingView` calls to `State.IsExploredByPlayer(Building)`.
- Non-goals: no fog authority rewrite, no `GameState.FogOfWar` deletion, no removal of `BuildingModel`.

Automated gates:
- Command: `dotnet build ProceduralRts.csproj --no-restore`
  Result: pass
  Evidence: build completed successfully with 0 warnings and 0 errors.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj buildingviewexploredprovider --no-restore`
  Result: pass
  Evidence: building view explored-provider gate completed successfully.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore`
  Result: pass
  Evidence: full ReviewGate completed successfully.
- Command: `dotnet run --project tools/VerifyAll/VerifyAll.csproj --no-restore`
  Result: pass
  Evidence: VerifyAll completed all 14 steps successfully.

Reviewer result:
- Status: pass.
- Design note: the view still has a legacy `GameState.FogOfWar` fallback, but the live path now depends on an injected read function instead of the old building exploration helper.
- Required fixes: none.

Status:
- Pass.

Residual risks:
- `BuildingView` still requires `GameState` and `BuildingModel` for old-runtime fallback state.
- Building creation still iterates `_state.Buildings`.
- Full `UnitBattlefieldBuildingTarget` and legacy catalog removal remains open.

TODO update:
- Marked done: nested M1 slice `BuildingView explored-provider bridge`.
- Left open: parent migration cleanup and legacy runtime deletion.
