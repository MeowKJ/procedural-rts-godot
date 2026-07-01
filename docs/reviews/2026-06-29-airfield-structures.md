# Review Record - Airfield structures

Step: Complete the vertical-slice structure roster by adding Airfield.
Milestone: Playable 1v1 Skirmish - structure scope.
Owner AI: Codex.
Reviewer AI: Codex self-review (CombatBehavior and ReviewGate provide durable checks).
Integrator AI: Codex.

Scope:
- Files/folders: `scripts/core/BuildingKind.cs`, `scripts/core/BuildCatalog.cs`, `scripts/core/BuildingPresentationCatalog.cs`, `scripts/core/GameState.cs`, `scripts/core/GameText.cs`, `scripts/core/units/runtime/UnitBattlefield.cs`, `tools/CombatBehavior/Program.cs`, `tools/ReviewGate/Program.cs`, `TODO.md`, `docs/reviews/2026-06-29-airfield-structures.md`.
- Non-goals: no air production queue yet, no new aircraft roster, no turret implementation, no claim that the full player gameplay loop is complete.

Implementation summary:
- Added `BuildingKind.Airfield`.
- Added Airfield to `BuildCatalog` as an Air-category structure requiring HQ, PowerPlant, and VehicleFactory.
- Added Airfield runtime building data to `GameState.BuildingDefinitions`.
- Added Airfield presentation data and text key.
- Added Airfield label support to `UnitBattlefield` building target labels.
- Added CombatBehavior coverage proving all six vertical-slice structures exist across build/runtime/presentation catalogs.
- Added build-option coverage proving Airfield is locked before VehicleFactory and unlocks once prerequisites are ready.
- Added `ReviewGate structures`.

Automated gates:
- Command: `dotnet build ProceduralRts.csproj --no-restore`
  Result: pass
  Evidence: build completed with 0 warnings and 0 errors.
- Command: `dotnet run --project tools/CombatBehavior/CombatBehavior.csproj --no-restore`
  Result: pass
  Evidence: CombatBehavior passed, including structure catalog completeness and Airfield prerequisite checks.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj structures --no-restore`
  Result: pass
  Evidence: ReviewGate reported 0 errors and 0 warnings for structure coverage.

Manual/visual gates:
- Check: visual QA
  Result: not run
  Evidence: this slice is data/catalog/build-option coverage; visual building style remains part of the art/UI TODO.

Reviewer result:
- Status: pass
- Required fixes: none.
- Residual risks: Airfield is now a buildable structure entry, but air-unit production and full air tech progression remain separate open work.

TODO update:
- Items marked done: `Structures: HQ, Power, Refinery, Barracks (light), Factory (tank), Airfield (air).`
- Items left open: turrets, T1-T3 production completeness, player gameplay loop, AI command-buffer-only behavior, counter readability.
- Reason: all named structures now exist in the enum, build catalog, runtime definitions, presentation catalog, UI build options, and tests.
