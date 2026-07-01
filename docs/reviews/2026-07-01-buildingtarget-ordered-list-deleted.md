# Review Record - UnitBattlefieldBuildingTarget ordered wrapper list deletion

Step:
- UnitBattlefieldBuildingTarget ordered wrapper list deletion

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
  - docs/reviews/2026-07-01-buildingtarget-ordered-list-deleted.md
- Non-goals:
  - Do not remove `UnitBattlefieldBuildingTarget` or `_buildingTargetsById` yet.
  - Do not change building identity, health sync, construction adoption, combat,
    production, harvesting, selection, or removal semantics.
  - Do not change unit balance, movement feel, UI, fog, art, or roster data.

Implementation summary:
- Removed the private ordered `Buildings` list from `UnitBattlefield`.
- Changed `BuildingTargetIds()` to keep EntityWorld ordered identity enumeration as
  the primary source and use deterministic `_buildingTargetsById.Keys.OrderBy(...)`
  as the remaining migration fallback.
- Removed `Buildings.Add(...)` and `Buildings.RemoveAll(...)` maintenance from
  add/remove target-state helpers.
- Updated historical ReviewGate checks that previously allowed the private wrapper
  list so they now reject it and require id-index storage/fallback instead.
- Added `ReviewGate buildingtargetorderedlistdeleted`.

Automated gates:
- Command: `dotnet build ProceduralRts.csproj --no-restore`
  Result: pass
  Evidence: 0 warnings, 0 errors.
- Command: `dotnet build tools/ReviewGate/ReviewGate.csproj --no-restore`
  Result: pass
  Evidence: 0 warnings, 0 errors.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore -- buildingtargetorderedlistdeleted`
  Result: pass
  Evidence: ReviewGate passed with 0 errors, 0 warnings.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore -- buildingtargetlistobjectdeleted`
  Result: pass
  Evidence: ReviewGate passed with 0 errors, 0 warnings.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore -- buildingtargetidprojectionreads`
  Result: pass
  Evidence: ReviewGate passed with 0 errors, 0 warnings.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore -- buildingtargetlookupindexed`
  Result: pass
  Evidence: ReviewGate passed with 0 errors, 0 warnings.
- Command: `dotnet run --project tools/SimReplay/SimReplay.csproj --no-restore`
  Result: pass
  Evidence: SimReplay PASSED.
- Command: `dotnet run --project tools/CombatBehavior/CombatBehavior.csproj --no-restore`
  Result: pass
  Evidence: Combat behavior passed.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore -- review --require-record=buildingtarget-ordered-list-deleted`
  Result: pass
  Evidence: ReviewGate passed with 0 errors, 0 warnings after evidence backfill.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore`
  Result: pass
  Evidence: ReviewGate passed with 0 errors, 0 warnings.
- Command: `dotnet run --project tools/VerifyAll/VerifyAll.csproj --no-restore`
  Result: pass
  Evidence: VerifyAll PASSED, 23/23 steps.

Manual/visual gates:
- Check: Visual/UI review
  Result: not applicable
  Evidence: This slice changes private building target storage only.

Reviewer result:
- Status: pass after integration review.
- Required fixes:
  - None.
- Reviewer notes:
  - This removes the last private ordered wrapper collection while keeping the
    remaining migration seed/index state for follow-up wrapper deletion.
  - The fallback ordering is deterministic by building id and only runs after
    EntityWorld ordered identity enumeration has had first chance to publish ids.
- Residual risks:
  - `UnitBattlefieldBuildingTarget` and `_buildingTargetsById` remain as temporary
    seed/index state until the final wrapper deletion phase.
  - ReviewGate is string/regex-based rather than semantic type analysis.

TODO update:
- Items marked done:
  - UnitBattlefieldBuildingTarget ordered wrapper list deletion
- Items left open:
  - Final `UnitBattlefieldBuildingTarget` / `_buildingTargetsById` wrapper deletion.
- Reason:
  - `UnitBattlefield` no longer keeps a second ordered wrapper list; id-index
    storage is the only remaining temporary building target state.
