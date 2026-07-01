Step: Route BuildingView footprint and max HP fallbacks through BuildSpec as a bounded M1 migration cleanup slice.
Milestone: M1 EntityWorld Becomes Authoritative
Owner AI: Codex
Reviewer AI: Codex review pass
Integrator AI: Codex

Scope:
- Files/folders: `scripts/world/BuildingView.cs`, `tools/ReviewGate/Program.cs`, `TODO.md`.
- Updated `BuildingView._Draw` so projected live building data falls back to `BuildSpecCatalog.For(kind)` instead of `State.Definition(Building)` for footprint and max HP.
- Kept EntityWorld projections preferred for health, identity, footprint, production, rally, and visibility data.
- Non-goals: no removal of `BuildingModel`, no deletion of `UnitBattlefieldBuildingTarget`, no building creation source rewrite.

Automated gates:
- Command: `dotnet build ProceduralRts.csproj --no-restore`
  Result: pass
  Evidence: build completed successfully with 0 warnings and 0 errors.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj buildingviewbuildspecfallback --no-restore`
  Result: pass
  Evidence: building view BuildSpec fallback gate completed successfully.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore`
  Result: pass
  Evidence: full ReviewGate completed successfully.
- Command: `dotnet run --project tools/VerifyAll/VerifyAll.csproj --no-restore`
  Result: pass
  Evidence: VerifyAll completed all 14 steps successfully.

Reviewer result:
- Status: pass.
- Design note: this removes another `BuildingDefinition` read from `BuildingView` while preserving the migration fallback shell.
- Required fixes: none.

Status:
- Pass.

Residual risks:
- `BuildingView` still requires `BuildingModel` as a fallback shell.
- Building creation still iterates `_state.Buildings`.
- Full `UnitBattlefieldBuildingTarget` and legacy catalog removal remains open.

TODO update:
- Marked done: nested M1 slice `BuildingView BuildSpec fallback bridge`.
- Left open: parent migration cleanup and legacy runtime deletion.
