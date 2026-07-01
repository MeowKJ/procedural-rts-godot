# Review Record - UnitBattlefieldBuildingTarget dock internal id cleanup

Step:
- UnitBattlefieldBuildingTarget dock internal id cleanup

Milestone:
- M1 EntityWorld authority

Owner AI:
- Codex

Reviewer AI:
- Planck

Integrator AI:
- Codex

Scope:
- Files/folders:
  - scripts/core/units/runtime/UnitBattlefield.cs
  - tools/ReviewGate/Program.cs
  - TODO.md
  - docs/reviews/2026-06-30-buildingtarget-dock-internal-id.md
- Non-goals:
  - Do not change harvester docking, unloading, reservation, or release rules.
  - Do not migrate weapon, repair, pulse write, or producer-candidate helpers.
  - Do not delete private building wrapper storage.

Implementation summary:
- Replaced private dock read helpers that accepted `UnitBattlefieldBuildingTarget`
  with `BuildingDockReservedByHarvesterIdCore(int buildingId)`,
  `BuildingDockedHarvesterIdCore(int buildingId)`, and
  `BuildingDockStateCore(int buildingId)`.
- Kept public dock read APIs stable and still converting EntityWorld dock entity ids
  to legacy unit ids for migration callers.
- Updated dock ReviewGate checks and added `ReviewGate buildingtargetdockinternalid`.

Automated gates:
- Command: `dotnet build ProceduralRts.csproj --no-restore`
  Result: pass
  Evidence: 0 warnings, 0 errors.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore -- buildingtargetdockentitystate`
  Result: pass
  Evidence: ReviewGate passed with 0 errors, 0 warnings.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore -- buildingtargetdockobjectdeleted`
  Result: pass
  Evidence: ReviewGate passed with 0 errors, 0 warnings.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore -- buildingtargetdockinternalid`
  Result: pass
  Evidence: ReviewGate passed with 0 errors, 0 warnings.
- Command: `dotnet run --project tools/CombatBehavior/CombatBehavior.csproj --no-restore`
  Result: pass
  Evidence: Combat behavior passed.
- Command: `dotnet run --project tools/SimReplay/SimReplay.csproj --no-restore`
  Result: pass
  Evidence: SimReplay PASSED.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore -- review --require-record=buildingtarget-dock-internal-id`
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
  Evidence: This slice changes internal dock read parameters only.

Reviewer result:
- Status: pass-with-warnings
- Required fixes:
  - None.
- Residual risks:
  - Reviewer was read-only and did not rerun gates; automated evidence was supplied
    by the integrator commands above.
  - Public dock reads still preserve migration wrapper existence checks.
  - Other internal helper families still accept the migration wrapper and remain
    future M1 slices.
  - The new gate is string-based and may reject equivalent rewrites during migration.

TODO update:
- Items marked done:
  - UnitBattlefieldBuildingTarget dock internal id cleanup
- Items left open:
  - Weapon, pulse write, repair, producer-candidate, and refinery helper migrations.
- Reason:
  - This slice only removes wrapper flow from internal dock read helpers.
