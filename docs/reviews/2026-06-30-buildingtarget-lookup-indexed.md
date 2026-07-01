# Review Record - UnitBattlefieldBuildingTarget lookup index cleanup

Step:
- UnitBattlefieldBuildingTarget lookup index cleanup

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
  - docs/reviews/2026-06-30-buildingtarget-lookup-indexed.md
- Non-goals:
  - Do not remove the private migration wrapper list.
  - Do not change public building snapshot/list APIs, construction behavior, or
    death/outcome behavior.
  - Do not migrate remaining wrapper creation sites.

Implementation summary:
- Added `_buildingTargetsById` as the private id index for temporary building
  target migration state.
- Changed `BuildingTargetById(int)` to read through `_buildingTargetsById`
  instead of linearly scanning `Buildings`.
- Added `AddBuildingTarget(...)` and `RemoveBuildingTargetState(...)` so list and
  index updates stay paired.
- Updated upsert, adoption, explicit removal, dead-building cleanup, and id
  allocation to use the indexed helpers.
- Added `ReviewGate buildingtargetlookupindexed`.

Automated gates:
- Command: `dotnet build ProceduralRts.csproj --no-restore`
  Result: pass
  Evidence: 0 warnings, 0 errors.
- Command: `dotnet build tools/ReviewGate/ReviewGate.csproj --no-restore`
  Result: pass
  Evidence: 0 warnings, 0 errors.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore -- buildingtargetlookupindexed`
  Result: pass
  Evidence: ReviewGate passed with 0 errors, 0 warnings.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore -- buildingtargetinternalwrapper`
  Result: pass
  Evidence: ReviewGate passed with 0 errors, 0 warnings.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore -- buildingtargetadoptinternalid`
  Result: pass
  Evidence: ReviewGate passed with 0 errors, 0 warnings.
- Command: `dotnet run --project tools/CombatBehavior/CombatBehavior.csproj --no-restore`
  Result: pass
  Evidence: Combat behavior passed.
- Command: `dotnet run --project tools/SimReplay/SimReplay.csproj --no-restore`
  Result: pass
  Evidence: SimReplay passed.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore -- review --require-record=buildingtarget-lookup-indexed`
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
  Evidence: This slice changes private building target lookup plumbing only.

Reviewer result:
- Status: pass after integration review.
- Required fixes:
  - None.
- Reviewer notes:
  - Ordered `Buildings` enumeration is preserved for migration loops.
  - Id lookups, duplicate detection, removal, and id allocation now share the
    `_buildingTargetsById` index.
- Residual risks:
  - `UnitBattlefieldBuildingTarget` is still the private migration storage type.
  - The ordered `Buildings` list remains until final M1 wrapper deletion.
  - ReviewGate is string/regex-based rather than semantic type analysis.

TODO update:
- Items marked done:
  - UnitBattlefieldBuildingTarget lookup index cleanup
- Items left open:
  - Private wrapper creation/storage removal and final legacy `BuildingKind`
    deletion.
- Reason:
  - Id-based building target access no longer scans the ordered wrapper list.
