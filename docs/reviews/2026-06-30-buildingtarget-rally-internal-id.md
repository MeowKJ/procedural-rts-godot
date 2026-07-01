# Review Record - UnitBattlefieldBuildingTarget rally internal id cleanup

Step:
- UnitBattlefieldBuildingTarget rally internal id cleanup

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
  - docs/reviews/2026-06-30-buildingtarget-rally-internal-id.md
- Non-goals:
  - Do not change rally command behavior or rally target validation.
  - Do not migrate rally pulse writes in this slice.
  - Do not migrate power, dock, weapon, repair, or producer-candidate helpers.
  - Do not delete private building wrapper storage.

Implementation summary:
- Replaced private rally read helpers that accepted
  `UnitBattlefieldBuildingTarget` with `BuildingRallyPointCore(int buildingId)`
  and `BuildingRallyPulseCore(int buildingId)`.
- Kept public `BuildingRallyPoint(int buildingId)` and
  `BuildingRallyPulse(int buildingId)` APIs stable while preserving migration
  wrapper existence checks.
- Updated rally ReviewGate expectations and added
  `ReviewGate buildingtargetrallyinternalid`.

Automated gates:
- Command: `dotnet build ProceduralRts.csproj --no-restore`
  Result: pass
  Evidence: 0 warnings, 0 errors.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore -- buildingtargetrallyentitystate`
  Result: pass
  Evidence: ReviewGate passed with 0 errors, 0 warnings.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore -- buildingtargetrallyobjectdeleted`
  Result: pass
  Evidence: ReviewGate passed with 0 errors, 0 warnings.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore -- buildingtargetrallyinternalid`
  Result: pass
  Evidence: ReviewGate passed with 0 errors, 0 warnings.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore -- review --require-record=buildingtarget-rally-internal-id`
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
  Evidence: This slice changes internal rally read parameters only.

Reviewer result:
- Status: pass-with-warnings
- Required fixes:
  - None.
- Residual risks:
  - Rally pulse writes still use the migration wrapper and remain a later M1 slice.
  - Public rally reads still preserve migration wrapper existence checks.
  - The new gate is string-based and may reject equivalent rewrites during migration.

TODO update:
- Items marked done:
  - UnitBattlefieldBuildingTarget rally internal id cleanup
- Items left open:
  - Rally pulse write helper migration and broader wrapper storage deletion.
- Reason:
  - This slice only removes wrapper flow from internal rally read helpers.
