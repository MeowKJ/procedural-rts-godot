Step: Centralize BuildingView creation through a UnitBattlefield projection-wired factory as a bounded M1 migration cleanup slice.
Milestone: M1 EntityWorld Becomes Authoritative
Owner AI: Codex
Reviewer AI: Codex review pass
Integrator AI: Codex

Scope:
- Files/folders: `scripts/BattleRoot.cs`, `tools/ReviewGate/Program.cs`, `TODO.md`.
- Added `BattleRoot.CreateBuildingView(...)` so initial and added building views share one projection-wired path.
- Ensured every created `BuildingView` receives both `EntityProjection` and `BuildingPresentationProjection` providers from `UnitBattlefield`.
- Updated building-online alert labels to use `BuildSpecCatalog` instead of `GameState.Definition(building)`.
- Non-goals: no `BuildingView` constructor rewrite, no removal of `BuildingModel`, no building placement migration.

Automated gates:
- Command: `dotnet build ProceduralRts.csproj --no-restore`
  Result: pass
  Evidence: build completed successfully with 0 warnings and 0 errors.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj buildingviewfactoryprojection --no-restore`
  Result: pass
  Evidence: building view factory projection gate completed successfully.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore`
  Result: pass
  Evidence: full ReviewGate completed successfully.
- Command: `dotnet run --project tools/VerifyAll/VerifyAll.csproj --no-restore`
  Result: pass
  Evidence: VerifyAll completed all 14 steps successfully.

Reviewer result:
- Status: pass.
- Design note: this does not remove `BuildingModel`, but it narrows the live view creation path to one projection-wired location for the next migration step.
- Required fixes: none.

Status:
- Pass.

Residual risks:
- `BuildingView` still requires a legacy `BuildingModel` fallback.
- Initial building source still comes from `_state.Buildings`.
- Full building authoring/runtime split cleanup remains open.

TODO update:
- Marked done: nested M1 slice `BuildingView factory projection bridge`.
- Left open: parent migration cleanup and legacy runtime deletion.
