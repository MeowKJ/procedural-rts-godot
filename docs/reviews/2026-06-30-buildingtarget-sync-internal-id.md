# Review Record - UnitBattlefieldBuildingTarget sync internal id cleanup

Step:
- UnitBattlefieldBuildingTarget sync internal id cleanup

Milestone:
- M1 EntityWorld authority

Owner AI:
- Codex

Reviewer AI:
- Codex

Integrator AI:
- Codex

Scope:
- Files/folders:
  - scripts/core/units/runtime/UnitBattlefield.cs
  - tools/ReviewGate/Program.cs
  - TODO.md
  - docs/reviews/2026-06-30-buildingtarget-sync-internal-id.md
- Non-goals:
  - Do not remove the private migration wrapper list.
  - Do not change snapshot payloads, combat legality, production semantics, or
    building construction rules.
  - Do not migrate `BuildingTargetById` or final wrapper creation/deletion.

Implementation summary:
- Replaced the private wrapper-shaped sync entrypoint with
  `SyncBuildingTargetEntity(int buildingId, ...)`.
- Kept upsert seed overrides for rally point, powered state, and construction
  progress through the id-based sync helper.
- Updated selected building repair, selected building attack, explicit building
  attack, visibility fallback, rally, production cancellation, full sync, and
  construction subject enumeration to sync building entities by id.
- Added `ReviewGate buildingtargetsyncinternalid` to prevent wrapper-shaped sync
  helpers or direct wrapper sync calls from returning.

Automated gates:
- Command: `dotnet build ProceduralRts.csproj --no-restore`
  Result: pass
  Evidence: 0 warnings, 0 errors.
- Command: `dotnet build tools/ReviewGate/ReviewGate.csproj --no-restore`
  Result: pass
  Evidence: 0 warnings, 0 errors.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore -- buildingtargetsyncinternalid`
  Result: pass
  Evidence: ReviewGate passed with 0 errors, 0 warnings.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore -- buildingtargetselectionentitystate`
  Result: pass
  Evidence: ReviewGate passed with 0 errors, 0 warnings.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore -- buildingtargetproductionqueueentitystate`
  Result: pass
  Evidence: ReviewGate passed with 0 errors, 0 warnings.
- Command: `dotnet run --project tools/CombatBehavior/CombatBehavior.csproj --no-restore`
  Result: pass
  Evidence: Combat behavior passed.
- Command: `dotnet run --project tools/SimReplay/SimReplay.csproj --no-restore`
  Result: pass
  Evidence: SimReplay passed.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore -- review --require-record=buildingtarget-sync-internal-id`
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
  Evidence: This slice changes internal building sync plumbing only.

Reviewer result:
- Status: pass after integration review.
- Required fixes:
  - None.
- Reviewer notes:
  - `SyncBuildingTargetEntity(int buildingId, ...)` resolves the temporary
    wrapper internally by id, preserves existing EntityWorld component state, and
    returns false for missing ids.
  - Upsert, repair, attack, visibility, rally, production, and construction paths
    now call the sync helper with ids instead of wrapper variables.
- Residual risks:
  - The private `Buildings` wrapper list and `BuildingTargetById(int)` lookup
    remain until final M1 wrapper deletion.
  - ReviewGate is string/regex-based rather than semantic type analysis.

TODO update:
- Items marked done:
  - UnitBattlefieldBuildingTarget sync internal id cleanup
- Items left open:
  - Private wrapper storage, wrapper creation/adoption, and final legacy
    `BuildingKind` deletion.
- Reason:
  - The building EntityWorld sync helper no longer accepts
    `UnitBattlefieldBuildingTarget` parameters.
