Step: Route live production-complete producer labels through BuildSpec as a bounded M1 migration cleanup slice.
Milestone: M1 EntityWorld Becomes Authoritative
Owner AI: Codex
Reviewer AI: Codex review pass
Integrator AI: Codex

Scope:
- Files/folders: `scripts/BattleRoot.cs`, `tools/ReviewGate/Program.cs`, `TODO.md`.
- Updated live `OnUnitBattlefieldProductionCompleted(...)` status text to read producer labels from `BuildSpecCatalog.For(building.Kind).Label`.
- Removed the hard-coded `BattleRoot.BuildingLabel(...)` helper.
- Non-goals: no localization rewrite, no old-runtime production-complete handler change, no building kind enum removal.

Automated gates:
- Command: `dotnet build ProceduralRts.csproj --no-restore`
  Result: pass
  Evidence: build completed successfully with 0 warnings and 0 errors.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj buildingproductionlabel --no-restore`
  Result: pass
  Evidence: building production label gate completed successfully.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore`
  Result: pass
  Evidence: full ReviewGate completed successfully.
- Command: `dotnet run --project tools/VerifyAll/VerifyAll.csproj --no-restore`
  Result: pass
  Evidence: VerifyAll completed all 14 steps successfully.

Reviewer result:
- Status: pass.
- Design note: live production status now follows the unified BuildSpec data source instead of a parallel label switch.
- Required fixes: none.

Status:
- Pass.

Residual risks:
- The old-runtime production-complete path still reads legacy `GameState.ProductionDefinitions` and `_state.Definition(building)`.
- `BuildingKind` remains as the migration key until the full EntitySpec authoring path replaces it.
- Full `UnitBattlefieldBuildingTarget` and legacy catalog removal remains open.

TODO update:
- Marked done: nested M1 slice `UnitBattlefield production-complete BuildSpec label bridge`.
- Left open: parent migration cleanup and legacy runtime deletion.
