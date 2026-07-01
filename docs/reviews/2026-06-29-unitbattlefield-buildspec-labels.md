Step: Route UnitBattlefield runtime status labels through BuildSpec as a bounded M1 migration cleanup slice.
Milestone: M1 EntityWorld Becomes Authoritative
Owner AI: Codex
Reviewer AI: Codex review pass
Integrator AI: Codex

Scope:
- Files/folders: `scripts/core/units/runtime/UnitBattlefield.cs`, `tools/ReviewGate/Program.cs`, `TODO.md`.
- Updated live selected-building rally status to derive producer building labels from `BuildSpecCatalog`.
- Updated production queued and missing-producer status text to derive producer labels from `BuildSpecCatalog`.
- Removed the hard-coded `UnitBattlefield.BuildingLabel(...)` helper.
- Updated legacy structures/turrets ReviewGate checks so coverage is proven through `BuildSpecCatalog` instead of requiring a runtime label switch.
- Non-goals: no old `BuildingKind` deletion, no localization rewrite, no production system behavior change.

Automated gates:
- Command: `dotnet build ProceduralRts.csproj --no-restore`
  Result: pass
  Evidence: build completed successfully with 0 warnings and 0 errors.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj unitbattlefieldbuildspeclabels --no-restore`
  Result: pass
  Evidence: UnitBattlefield BuildSpec label gate completed successfully.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore`
  Result: pass
  Evidence: full ReviewGate completed successfully after migrating structures/turrets label checks to BuildSpecCatalog.
- Command: `dotnet run --project tools/VerifyAll/VerifyAll.csproj --no-restore`
  Result: pass
  Evidence: VerifyAll completed all 14 steps successfully.

Reviewer result:
- Status: pass.
- Design note: status text now follows the same unified build spec labels used by building projections and alerts, eliminating one more parallel building-label source.
- Required fixes: none.

Status:
- Pass.

Residual risks:
- `BuildingKind` remains as the migration key until the full EntitySpec authoring path replaces it.
- `BuildSpecCatalog` still merges legacy `GameState.BuildingDefinitions` during migration.
- Full `UnitBattlefieldBuildingTarget` and legacy catalog removal remains open.

TODO update:
- Marked done: nested M1 slice `UnitBattlefield BuildSpec status label bridge`.
- Left open: parent migration cleanup and legacy runtime deletion.
