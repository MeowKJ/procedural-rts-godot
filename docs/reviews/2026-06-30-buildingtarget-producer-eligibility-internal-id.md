# Review Record - UnitBattlefieldBuildingTarget producer eligibility internal id cleanup

Step:
- UnitBattlefieldBuildingTarget producer eligibility internal id cleanup

Milestone:
- M1 EntityWorld authority

Owner AI:
- Codex

Reviewer AI:
- Goodall the 2nd

Integrator AI:
- Codex

Scope:
- Files/folders:
  - scripts/core/units/runtime/UnitBattlefield.cs
  - tools/ReviewGate/Program.cs
  - TODO.md
  - docs/reviews/2026-06-30-buildingtarget-producer-eligibility-internal-id.md
- Non-goals:
  - Do not change production queues, costs, timings, tech tier, or producer choice
    ordering.
  - Do not change UnitDesign roster data or balance.
  - Do not migrate radius, snapshot, spec, repair, refinery, or spawn-point helpers.
  - Do not delete private building wrapper storage.

Implementation summary:
- Replaced private production design and producer eligibility helpers that accepted
  `UnitBattlefieldBuildingTarget` with `ProductionDesignIdCore(int buildingId, ...)`
  and `HasAnyProductionForCore(int buildingId)`.
- Kept faction-aware UnitDesign runtime loadout and roster lookup behavior.
- Updated production queue component sync, rally producer checks, selected-rally
  filtering, production enqueue, and candidate producer filtering to pass ids.
- Added `ReviewGate buildingtargetproducereligibilityinternalid`.

Automated gates:
- Command: `dotnet build ProceduralRts.csproj --no-restore`
  Result: pass
  Evidence: 0 warnings, 0 errors.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore -- buildingtargetproducereligibilityinternalid`
  Result: pass
  Evidence: ReviewGate passed with 0 errors, 0 warnings.
- Command: `dotnet run --project tools/PlayerLoopQa/PlayerLoopQa.csproj --no-restore`
  Result: pass
  Evidence: PlayerLoopQa PASSED.
- Command: `dotnet run --project tools/AiOpponentLoopQa/AiOpponentLoopQa.csproj --no-restore`
  Result: pass
  Evidence: AiOpponentLoopQa PASSED.
- Command: `dotnet run --project tools/CombatBehavior/CombatBehavior.csproj --no-restore`
  Result: pass
  Evidence: Combat behavior passed.
- Command: `dotnet run --project tools/SimReplay/SimReplay.csproj --no-restore`
  Result: pass
  Evidence: SimReplay PASSED.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore -- review --require-record=buildingtarget-producer-eligibility-internal-id`
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
  Evidence: This slice changes internal production helper parameters only.

Reviewer result:
- Status: pass-with-warnings
- Required fixes:
  - Goodall the 2nd noted the review record still had pending evidence while TODO
    was marked done. Fixed by recording reviewer and integrator gate evidence here.
- Residual risks:
  - Helpers still resolve the migration wrapper internally during the M1 bridge.
  - Other internal helper families still accept the migration wrapper and remain
    future M1 slices.
  - The new gate is string-based and may reject equivalent rewrites during migration.

TODO update:
- Items marked done:
  - UnitBattlefieldBuildingTarget producer eligibility internal id cleanup
- Items left open:
  - Radius, snapshot, spec, repair, refinery, spawn point, and final wrapper deletion
    migrations.
- Reason:
  - This slice only removes wrapper flow from internal production design and producer
    eligibility helper parameters.
