Step: Route live building view culling through UnitBattlefield EntityWorld presentation projections as a bounded M1 migration cleanup slice.
Milestone: M1 EntityWorld Becomes Authoritative
Owner AI: Codex
Reviewer AI: Codex review pass
Integrator AI: Codex

Scope:
- Files/folders: `scripts/BattleRoot.cs`, `tools/CombatBehavior/Program.cs`, `tools/ReviewGate/Program.cs`, `TODO.md`.
- Updated `BattleRoot.RefreshViewCulling` so UnitDesign runtime building views use `UnitBattlefield.BuildingPresentationProjection(...)` for health, position, facing, and footprint.
- Added `BuildingProjectionWorldRect(...)` to compute culling bounds from projected building footprints.
- Kept `BuildingModel` culling as the old-runtime fallback.
- Non-goals: no `BuildingView` drawing rewrite, no camera/culling interval changes, no deletion of `UnitBattlefieldBuildingTarget`.

Automated gates:
- Command: `dotnet build ProceduralRts.csproj --no-restore`
  Result: pass
  Evidence: build completed successfully with 0 warnings and 0 errors.
- Command: `dotnet run --project tools/CombatBehavior/CombatBehavior.csproj --no-restore`
  Result: pass
  Evidence: combat behavior assertions completed successfully, including projected building culling data coverage.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj buildingcullingprojection --no-restore`
  Result: pass
  Evidence: building culling projection gate completed successfully.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore`
  Result: pass
  Evidence: full ReviewGate completed successfully.
- Command: `dotnet run --project tools/VerifyAll/VerifyAll.csproj --no-restore`
  Result: pass
  Evidence: VerifyAll completed all 14 steps successfully.

Reviewer result:
- Status: pass.
- Design note: live view activation now follows EntityWorld projection geometry while preserving the old runtime fallback path.
- Required fixes: none.

Status:
- Pass.

Residual risks:
- `BuildingView` still stores a legacy `BuildingModel` fallback.
- Building view creation still starts from `_state.Buildings`; full building runtime removal remains open.
- Unit view culling is separate and unchanged.

TODO update:
- Marked done: nested M1 slice `BuildingView culling projection bridge`.
- Left open: parent migration cleanup and legacy runtime deletion.
