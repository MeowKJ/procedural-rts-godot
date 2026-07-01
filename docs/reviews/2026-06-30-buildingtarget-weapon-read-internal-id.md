# Review Record - UnitBattlefieldBuildingTarget weapon read internal id cleanup

Step:
- UnitBattlefieldBuildingTarget weapon read internal id cleanup

Milestone:
- M1 EntityWorld authority

Owner AI:
- Codex

Reviewer AI:
- Singer

Integrator AI:
- Codex

Scope:
- Files/folders:
  - scripts/core/units/runtime/UnitBattlefield.cs
  - tools/ReviewGate/Program.cs
  - TODO.md
  - docs/reviews/2026-06-30-buildingtarget-weapon-read-internal-id.md
- Non-goals:
  - Do not change combat targeting, cooldown, or turret firing semantics.
  - Do not change weapon balance or unit/building stats.
  - Do not migrate pulse write, repair, producer-candidate, or refinery helpers.
  - Do not delete private building wrapper storage.

Implementation summary:
- Replaced private building weapon read helpers that accepted
  `UnitBattlefieldBuildingTarget` with `BuildingAttackTargetIdCore(int buildingId)`,
  `BuildingAttackTargetKindCore(int buildingId)`,
  `BuildingAttackCooldownRemainingCore(int buildingId)`, and
  `BuildingWeaponStateCore(int buildingId)`.
- Kept public id-based weapon read APIs stable and preserving missing-building
  migration defaults.
- Updated internal dead-unit target cleanup to pass `building.Id` into the id-based
  core helpers.
- Updated weapon ReviewGate checks and added
  `ReviewGate buildingtargetweaponreadinternalid`.

Automated gates:
- Command: `dotnet build ProceduralRts.csproj --no-restore`
  Result: pass
  Evidence: 0 warnings, 0 errors.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore -- buildingtargetweaponreadinternalid`
  Result: pass
  Evidence: ReviewGate passed with 0 errors, 0 warnings.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore -- buildingtargetweaponuserentitystate`
  Result: pass
  Evidence: ReviewGate passed with 0 errors, 0 warnings.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore -- buildingtargetweaponreadobjectdeleted`
  Result: pass
  Evidence: ReviewGate passed with 0 errors, 0 warnings.
- Command: `dotnet run --project tools/CombatBehavior/CombatBehavior.csproj --no-restore`
  Result: pass
  Evidence: Combat behavior passed.
- Command: `dotnet run --project tools/SimReplay/SimReplay.csproj --no-restore`
  Result: pass
  Evidence: SimReplay PASSED.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore -- review --require-record=buildingtarget-weapon-read-internal-id`
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
  Evidence: This slice changes internal building weapon read parameters only.

Reviewer result:
- Status: pass-with-warnings
- Required fixes:
  - Singer requested hardening against future `*Core(UnitBattlefieldBuildingTarget)`
    overloads. Fixed by adding explicit ReviewGate forbids for target-wrapper core
    overloads, then reran the narrow gate successfully.
- Residual risks:
  - Public weapon reads still preserve migration wrapper existence checks.
  - Other internal helper families still accept the migration wrapper and remain
    future M1 slices.
  - The new gate is string-based and may reject equivalent rewrites during migration.

TODO update:
- Items marked done:
  - UnitBattlefieldBuildingTarget weapon read internal id cleanup
- Items left open:
  - Pulse write, repair, producer-candidate, and refinery helper migrations.
- Reason:
  - This slice only removes wrapper flow from internal building weapon read helpers.
