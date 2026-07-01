# Review Record - UnitBattlefieldBuildingTarget ensure production queue component internal id cleanup

Step:
- UnitBattlefieldBuildingTarget ensure production queue component internal id cleanup

Milestone:
- M1 EntityWorld authority

Owner AI:
- Codex

Reviewer AI:
- Boyle the 2nd

Integrator AI:
- Codex

Scope:
- Files/folders:
  - scripts/core/units/runtime/UnitBattlefield.cs
  - tools/ReviewGate/Program.cs
  - TODO.md
  - docs/reviews/2026-06-30-buildingtarget-ensure-queue-internal-id.md
- Non-goals:
  - Do not change production queue contents, costs, timings, tech tier, or producer
    selection ordering.
  - Do not change UnitDesign roster data or production UI.
  - Do not migrate producer candidate lists, completion matching, repair helpers, or
    final building wrapper storage.

Implementation summary:
- Changed `EnsureProductionQueueComponent(int buildingId, EntityInstance entity)` to
  accept `int buildingId` instead of `UnitBattlefieldBuildingTarget`.
- Updated building sync and constructed-building adoption call sites to pass
  `target.Id`.
- Preserved `HasAnyProductionForCore(buildingId)`, the existing queue guard, and the
  empty `ProductionQueueComponentState` creation behavior.
- Added `ReviewGate buildingtargetensurequeueinternalid`.

Automated gates:
- Command: `dotnet build ProceduralRts.csproj --no-restore`
  Result: pass
  Evidence: 0 warnings, 0 errors.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore -- buildingtargetensurequeueinternalid`
  Result: pass
  Evidence: ReviewGate passed with 0 errors, 0 warnings.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore -- playertierproduction`
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
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore -- review --require-record=buildingtarget-ensure-queue-internal-id`
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
  Evidence: This slice changes internal helper parameters only.

Reviewer result:
- Status: pass on static implementation shape; fail-for-completion until evidence
  was recorded.
- Required fixes:
  - Boyle the 2nd noted the review record still had pending evidence and TODO was
    still open before final gates were recorded. Fixed by recording reviewer,
    integrator gate evidence, and the completed TODO update.
- Residual risks:
  - The helper still resolves producer capability through wrapper-backed
    `HasAnyProductionForCore` during the M1 migration window.
  - `HasAnyProductionForCore(int)` still resolves through `BuildingTargetById`,
    which is linear over the migration wrapper list; acceptable for this narrow
    migration slice, but not the final architecture.
  - Producer candidate and production completion matching still use the migration
    building wrapper and remain future M1 cleanup slices.
  - ReviewGate is string/regex-based rather than semantic type analysis.

TODO update:
- Items marked done:
  - UnitBattlefieldBuildingTarget ensure production queue component internal id cleanup
- Items left open:
  - Producer candidate lists, production completion matching, repair helpers, and
    final wrapper deletion migrations.
- Reason:
  - This slice only removes wrapper flow from the internal queue-component ensure
    helper parameters while preserving queue creation behavior.
