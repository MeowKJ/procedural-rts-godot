# Review Record - UnitBattlefieldBuildingTarget visibility internal id cleanup

Step:
- UnitBattlefieldBuildingTarget visibility internal id cleanup

Milestone:
- M1 EntityWorld authority

Owner AI:
- Codex

Reviewer AI:
- Archimedes the 2nd

Integrator AI:
- Codex

Scope:
- Files/folders:
  - scripts/core/units/runtime/UnitBattlefield.cs
  - tools/ReviewGate/Program.cs
  - TODO.md
  - docs/reviews/2026-06-30-buildingtarget-visibility-internal-id.md
- Non-goals:
  - Do not change fog, vision, reveal radius, or attack-wave targeting semantics.
  - Do not change enemy AI visibility rules.
  - Do not migrate radius, snapshot, spec, repair, producer-candidate, or refinery
    helpers.
  - Do not delete private building wrapper storage.

Implementation summary:
- Replaced private building visibility helper accepting
  `UnitBattlefieldBuildingTarget` with `IsVisibleToCore(PlayerSlotId viewer, int buildingId)`.
- Kept public `IsVisibleTo(PlayerSlotId viewer, int buildingId)` stable, including
  its missing-building false result.
- Preserved the migration fallback that syncs a building entity mirror before reading
  EntityWorld `VisibilityIndex`.
- Added `ReviewGate buildingtargetvisibilityinternalid` and updated the historical
  visibility object-deletion gate to require the id-based core helper.

Automated gates:
- Command: `dotnet build ProceduralRts.csproj --no-restore`
  Result: pass
  Evidence: 0 warnings, 0 errors.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore -- buildingtargetvisibilityinternalid`
  Result: pass
  Evidence: ReviewGate passed with 0 errors, 0 warnings.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore -- buildingtargetvisibilityobjectdeleted`
  Result: pass
  Evidence: ReviewGate passed with 0 errors, 0 warnings.
- Command: `dotnet run --project tools/FogOfWarQa/FogOfWarQa.csproj --no-restore`
  Result: pass
  Evidence: Fog-of-war QA passed.
- Command: `dotnet run --project tools/AiOpponentLoopQa/AiOpponentLoopQa.csproj --no-restore`
  Result: pass
  Evidence: AiOpponentLoopQa PASSED.
- Command: `dotnet run --project tools/CombatBehavior/CombatBehavior.csproj --no-restore`
  Result: pass
  Evidence: Combat behavior passed.
- Command: `dotnet run --project tools/SimReplay/SimReplay.csproj --no-restore`
  Result: pass
  Evidence: SimReplay PASSED.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore -- review --require-record=buildingtarget-visibility-internal-id`
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
  Evidence: This slice changes internal building visibility helper parameters only.

Reviewer result:
- Status: pass-with-warnings
- Required fixes:
  - None.
- Residual risks:
  - Public visibility reads still preserve migration wrapper existence checks.
  - Other internal helper families still accept the migration wrapper and remain
    future M1 slices.
  - The new gate is string-based and may reject equivalent rewrites during migration.

TODO update:
- Items marked done:
  - UnitBattlefieldBuildingTarget visibility internal id cleanup
- Items left open:
  - Radius, snapshot, spec, repair, producer-candidate, refinery, and final wrapper
    deletion migrations.
- Reason:
  - This slice only removes wrapper flow from internal building visibility reads.
