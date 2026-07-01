# Review Record - UnitBattlefieldBuildingTarget power/construction internal id cleanup

Step:
- UnitBattlefieldBuildingTarget power/construction internal id cleanup

Milestone:
- M1 EntityWorld authority

Owner AI:
- Codex

Reviewer AI:
- Pending

Integrator AI:
- Codex

Scope:
- Files/folders:
  - scripts/core/units/runtime/UnitBattlefield.cs
  - tools/ReviewGate/Program.cs
  - TODO.md
  - docs/reviews/2026-06-30-buildingtarget-power-construction-internal-id.md
- Non-goals:
  - Do not change power or construction simulation rules.
  - Do not change producer/refinery eligibility semantics.
  - Do not migrate dock, weapon, repair, or producer-candidate helpers.
  - Do not delete private building wrapper storage.

Implementation summary:
- Replaced private power/build-progress read helpers that accepted
  `UnitBattlefieldBuildingTarget` with `BuildingPoweredCore(int buildingId)` and
  `BuildingBuildProgressCore(int buildingId)`.
- Kept public `BuildingPowered(int buildingId)` and
  `BuildingBuildProgress(int buildingId)` APIs stable, including missing-building
  migration defaults.
- Updated internal eligibility reads to pass `building.Id`.
- Updated power/construction ReviewGate checks and added
  `ReviewGate buildingtargetpowerconstructioninternalid`.

Automated gates:
- Command: `dotnet build ProceduralRts.csproj --no-restore`
  Result: pass
  Evidence: 0 warnings, 0 errors.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore -- buildingtargetpowerconstructionentitystate`
  Result: pass
  Evidence: ReviewGate passed with 0 errors, 0 warnings.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore -- buildingtargetpowerconstructionobjectdeleted`
  Result: pass
  Evidence: ReviewGate passed with 0 errors, 0 warnings.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore -- buildingtargetpowerconstructioninternalid`
  Result: pass
  Evidence: ReviewGate passed with 0 errors, 0 warnings.
- Command: `dotnet run --project tools/CombatBehavior/CombatBehavior.csproj --no-restore`
  Result: pass
  Evidence: Combat behavior passed.
- Command: `dotnet run --project tools/SimReplay/SimReplay.csproj --no-restore`
  Result: pass
  Evidence: SimReplay PASSED.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore -- review --require-record=buildingtarget-power-construction-internal-id`
  Result: pass
  Evidence: ReviewGate passed with 0 errors, 0 warnings.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore`
  Result: pass
  Evidence: ReviewGate passed with 0 errors, 0 warnings.
- Command: `dotnet run --project tools/VerifyAll/VerifyAll.csproj --no-restore`
  Result: pass
  Evidence: VerifyAll PASSED, 23/23 steps.

Manual/visual gates:
- Check: Visual/UI review
  Result: not applicable
  Evidence: This slice changes internal power/construction read parameters only.

Reviewer result:
- Status: pass
- Required fixes:
  - None.
- Residual risks:
  - Public power/build reads still preserve migration wrapper existence checks.
  - Other internal helper families still accept the migration wrapper and remain
    future M1 slices.
  - The new gate is string-based and may reject equivalent rewrites during migration.

TODO update:
- Items marked done:
  - UnitBattlefieldBuildingTarget power/construction internal id cleanup
- Items left open:
  - Dock, weapon, pulse write, repair, producer-candidate, and refinery helper
    migrations.
- Reason:
  - This slice only removes wrapper flow from internal power/construction read
    helpers.
