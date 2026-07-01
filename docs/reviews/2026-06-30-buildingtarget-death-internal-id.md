# Review Record - UnitBattlefieldBuildingTarget death internal id cleanup

Step:
- UnitBattlefieldBuildingTarget death internal id cleanup

Milestone:
- M1 EntityWorld authority

Owner AI:
- Codex

Reviewer AI:
- Gauss the 2nd

Integrator AI:
- Codex

Scope:
- Files/folders:
  - scripts/core/units/runtime/UnitBattlefield.cs
  - tools/ReviewGate/Program.cs
  - TODO.md
  - docs/reviews/2026-06-30-buildingtarget-death-internal-id.md
- Non-goals:
  - Do not change building damage values, death timing, combat targeting, or outcome
    rules.
  - Do not change public `BuildingsRemoved` event payloads.
  - Do not migrate combat target legality, snapshot helpers, BuildSpec helpers, or
    final wrapper storage.

Implementation summary:
- Changed building combat event cleanup to keep destroyed-building candidates as ids
  instead of materializing a `UnitBattlefieldBuildingTarget` list.
- Added `BuildingDeathInfo(int buildingId)` to generate immutable
  `UnitBattlefieldBuildingDeathInfo` records by id.
- Changed `RemoveDeadBuildingTargets(IReadOnlyList<int> deadBuildingIds)` to
  accept building ids.
- Preserved migration target removal, EntityWorld mirror removal, unit attack-target
  clearing, `BuildingsRemoved` publication, and outcome updates.
- Added `ReviewGate buildingtargetdeathinternalid`.

Automated gates:
- Command: `dotnet build ProceduralRts.csproj --no-restore`
  Result: pass
  Evidence: 0 warnings, 0 errors.
- Command: `dotnet build tools/ReviewGate/ReviewGate.csproj --no-restore`
  Result: pass
  Evidence: 0 warnings, 0 errors.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore -- buildingtargetdeathinternalid`
  Result: pass
  Evidence: ReviewGate passed with 0 errors, 0 warnings.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore -- buildingtargeteventobjectdeleted`
  Result: pass
  Evidence: ReviewGate passed with 0 errors, 0 warnings.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore -- buildingtargethealthinternalid`
  Result: pass
  Evidence: ReviewGate passed with 0 errors, 0 warnings.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore -- buildingtargetcombatsystembridge`
  Result: pass
  Evidence: ReviewGate passed with 0 errors, 0 warnings.
- Command: `dotnet run --project tools/SimReplay/SimReplay.csproj --no-restore`
  Result: pass
  Evidence: SimReplay PASSED, including combat, group-attack, outcome, and building-target combat coverage.
- Command: `dotnet run --project tools/CombatBehavior/CombatBehavior.csproj --no-restore`
  Result: pass
  Evidence: Combat behavior passed.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore -- review --require-record=buildingtarget-death-internal-id`
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
  Evidence: This slice changes internal building death cleanup plumbing only.

Reviewer result:
- Status: pass on implementation shape; no required code fixes.
- Required fixes:
  - None.
- Reviewer notes:
  - Gauss the 2nd confirmed that death records remain self-contained, EntityWorld
    mirror removal still runs by id, unit attack targets are cleared, outcome
    updates still use death records, and `BattleRoot` consumes death info without
    relying on the removed wrapper.
- Residual risks:
  - `BuildingDeathInfo(int buildingId)` still resolves the temporary migration
    wrapper internally by id during M1.
  - The helper still uses `BuildingSpec(building)` until the remaining snapshot/spec
    helper cleanup slices retire wrapper-based private helpers.
  - ReviewGate is string/regex-based rather than semantic type analysis.

TODO update:
- Items marked done:
  - UnitBattlefieldBuildingTarget death internal id cleanup
- Items left open:
  - Combat targeting helpers, snapshot/build-spec helper cleanup, and final wrapper
    deletion migrations.
- Reason:
  - This slice removes wrapper-list flow from internal building death cleanup while
    preserving building death event and outcome behavior.
