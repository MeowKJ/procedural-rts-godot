# Review Record - UnitBattlefieldBuildingTarget health sync internal id cleanup

Step:
- UnitBattlefieldBuildingTarget health sync internal id cleanup

Milestone:
- M1 EntityWorld authority

Owner AI:
- Codex

Reviewer AI:
- Hypatia the 2nd

Integrator AI:
- Codex

Scope:
- Files/folders:
  - scripts/core/units/runtime/UnitBattlefield.cs
  - tools/ReviewGate/Program.cs
  - TODO.md
  - docs/reviews/2026-06-30-buildingtarget-health-internal-id.md
- Non-goals:
  - Do not change building damage, death, or outcome semantics.
  - Do not change combat balance or visual feedback.
  - Do not migrate repair, producer-candidate, refinery, radius, snapshot, or spec
    helpers.
  - Do not delete private building wrapper storage.

Implementation summary:
- Replaced private building health sync helper accepting
  `UnitBattlefieldBuildingTarget` with `SyncBuildingHealthFromEntityCore(int buildingId)`.
- Kept the migration behavior that copies EntityWorld `HealthComponentState.Hp` back
  to the legacy building target record for existing view/event code.
- Updated building damage feedback to pass `target.Id`.
- Added `ReviewGate buildingtargethealthinternalid`.

Automated gates:
- Command: `dotnet build ProceduralRts.csproj --no-restore`
  Result: pass
  Evidence: 0 warnings, 0 errors.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore -- buildingtargethealthinternalid`
  Result: pass
  Evidence: ReviewGate passed with 0 errors, 0 warnings.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore -- buildingtargetcombatsystembridge`
  Result: pass
  Evidence: ReviewGate passed with 0 errors, 0 warnings.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore -- m1behaviorparentcomplete`
  Result: pass
  Evidence: ReviewGate passed with 0 errors, 0 warnings.
- Command: `dotnet run --project tools/CombatBehavior/CombatBehavior.csproj --no-restore`
  Result: pass
  Evidence: Combat behavior passed.
- Command: `dotnet run --project tools/SimReplay/SimReplay.csproj --no-restore`
  Result: pass
  Evidence: SimReplay PASSED.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore -- review --require-record=buildingtarget-health-internal-id`
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
  Evidence: This slice changes internal building health sync helper parameters only.

Reviewer result:
- Status: pass-with-warnings
- Required fixes:
  - Hypatia the 2nd noted that the review record still had pending gate/reviewer
    evidence while TODO was marked done. Fixed by recording the gates and review
    result here, then running final record/full/VerifyAll gates.
- Residual risks:
  - Health still syncs back into the legacy wrapper during migration.
  - Other internal helper families still accept the migration wrapper and remain
    future M1 slices.
  - The new gate is string-based and may reject equivalent rewrites during migration.

TODO update:
- Items marked done:
  - UnitBattlefieldBuildingTarget health sync internal id cleanup
- Items left open:
  - Repair, producer-candidate, refinery, radius, snapshot, spec, and final wrapper
    deletion migrations.
- Reason:
  - This slice only removes wrapper flow from internal building health sync.
