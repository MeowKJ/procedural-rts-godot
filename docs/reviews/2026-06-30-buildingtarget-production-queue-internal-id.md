# Review Record - UnitBattlefieldBuildingTarget production queue internal id cleanup

Step:
- UnitBattlefieldBuildingTarget production queue internal id cleanup

Milestone:
- M1 EntityWorld authority

Owner AI:
- Codex

Reviewer AI:
- Harvey

Integrator AI:
- Codex

Scope:
- Files/folders:
  - scripts/core/units/runtime/UnitBattlefield.cs
  - tools/ReviewGate/Program.cs
  - TODO.md
  - docs/reviews/2026-06-30-buildingtarget-production-queue-internal-id.md
- Non-goals:
  - Do not change production queue rules, costs, timings, or output choices.
  - Do not migrate rally, dock, power, weapon, or repair helpers in this slice.
  - Do not delete private building wrapper storage.
  - Do not touch UI art or balance tuning.

Implementation summary:
- Replaced the private `BuildingProductionQueue(UnitBattlefieldBuildingTarget building)`
  helper with `BuildingProductionQueueCore(int buildingId)`.
- Updated internal `UnitBattlefield` production queue reads to pass building ids
  (`building.Id` / `producer.Id`) while still reading
  `ProductionQueueComponentState.Items` from EntityWorld.
- Updated production queue ReviewGate checks and added
  `ReviewGate buildingtargetproductionqueueinternalid`.

Automated gates:
- Command: `dotnet build ProceduralRts.csproj --no-restore`
  Result: pass
  Evidence: 0 warnings, 0 errors.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore -- buildingtargetproductionqueueentitystate`
  Result: pass
  Evidence: ReviewGate passed with 0 errors, 0 warnings.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore -- buildingtargetproductionqueueobjectdeleted`
  Result: pass
  Evidence: ReviewGate passed with 0 errors, 0 warnings.
- Command: `dotnet run --project tools/CombatBehavior/CombatBehavior.csproj --no-restore`
  Result: pass
  Evidence: Combat behavior passed.
- Command: `dotnet run --project tools/SimReplay/SimReplay.csproj --no-restore`
  Result: pass
  Evidence: SimReplay PASSED.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore -- buildingtargetproductionqueueinternalid`
  Result: pass
  Evidence: ReviewGate passed with 0 errors, 0 warnings.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore -- review --require-record=buildingtarget-production-queue-internal-id`
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
  Evidence: This slice changes internal production queue read parameters only.

Reviewer result:
- Status: pass-with-warnings
- Required fixes:
  - None.
- Residual risks:
  - The new gate is intentionally string-based and may reject equivalent rewrites
    until the wrapper migration is complete.
  - `BuildingProductionQueue(int buildingId)` still checks the migration wrapper
    for existence before reading EntityWorld queue state, preserving migration
    behavior until wrapper storage is removed.
  - Other internal state helpers still accept the migration wrapper and remain
    future M1 slices.

TODO update:
- Items marked done:
  - UnitBattlefieldBuildingTarget production queue internal id cleanup
- Items left open:
  - Rally, dock, power, weapon, pulse write, repair, producer-candidate, and
    refinery helper migrations.
- Reason:
  - This slice only removes wrapper flow from internal production queue reads.
