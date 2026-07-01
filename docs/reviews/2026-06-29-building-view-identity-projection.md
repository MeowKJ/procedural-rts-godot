Step: Route BuildingView art identity through UnitBattlefield building view projections as a bounded M1 migration cleanup slice.
Milestone: M1 EntityWorld Becomes Authoritative
Owner AI: Codex
Reviewer AI: Codex review pass
Integrator AI: Codex

Scope:
- Files/folders: `scripts/core/sim/BuildingPresentationProjection.cs`, `scripts/core/units/runtime/UnitBattlefield.cs`, `scripts/world/BuildingView.cs`, `scripts/BattleRoot.cs`, `tools/CombatBehavior/Program.cs`, `tools/ReviewGate/Program.cs`, `TODO.md`.
- Added `BuildingViewProjection` to carry live building kind, player slot, faction, and presentation data together.
- Added `UnitBattlefield.BuildingViewProjection(...)`.
- Updated `BuildingView` to prefer projected kind, owner slot, faction, and owner color for live art identity.
- Updated `BattleRoot.CreateBuildingView(...)` to wire the new identity projection provider.
- Non-goals: no removal of legacy `BuildingModel`, no PresentationCatalog rewrite, no faction art redesign.

Automated gates:
- Command: `dotnet build ProceduralRts.csproj --no-restore`
  Result: pass
  Evidence: build completed successfully with 0 warnings and 0 errors.
- Command: `dotnet run --project tools/CombatBehavior/CombatBehavior.csproj --no-restore`
  Result: pass
  Evidence: combat behavior assertions completed successfully, including building view identity projection coverage.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj buildingviewidentityprojection --no-restore`
  Result: pass
  Evidence: building view identity projection gate completed successfully.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore`
  Result: pass
  Evidence: full ReviewGate completed successfully.
- Command: `dotnet run --project tools/VerifyAll/VerifyAll.csproj --no-restore`
  Result: pass
  Evidence: VerifyAll completed all 14 steps successfully.

Reviewer result:
- Status: pass.
- Design note: identity stays in UnitBattlefield for now because building faction is runtime/faction-specific and should not be baked into shared immutable `EntitySpec`.
- Required fixes: none.

Status:
- Pass.

Residual risks:
- `BuildingView` still requires `BuildingModel` as a fallback shell.
- Building creation still iterates `_state.Buildings`.
- Full `UnitBattlefieldBuildingTarget` and legacy catalog removal remains open.

TODO update:
- Marked done: nested M1 slice `BuildingView identity projection bridge`.
- Left open: parent migration cleanup and legacy runtime deletion.
