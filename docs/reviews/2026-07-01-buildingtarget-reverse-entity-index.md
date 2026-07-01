# Review Record - BuildingTarget reverse EntityId index

Step:
- UnitBattlefieldBuildingTarget reverse EntityId index cleanup

Milestone:
- M1 EntityWorld authority / Migration cleanup

Owner AI:
- Codex

Reviewer AI:
- Codex; ReviewGate buildingtargetreverseentityindex

Integrator AI:
- Codex

Scope:
- Files/folders:
  - scripts/core/units/runtime/UnitBattlefield.cs
  - tools/CombatBehavior/Program.cs
  - tools/ReviewGate/Program.cs
  - TODO.md
  - docs/reviews/2026-07-01-buildingtarget-reverse-entity-index.md
- Non-goals:
  - Do not change building id allocation.
  - Do not delete `_buildingTargetSeedsById`.
  - Do not change combat, harvest, dock, or construction behavior.
  - Do not run full `VerifyAll` for this single slice.

Implementation summary:
- Added `_buildingTargetIdsByEntityId` as the reverse `EntityId -> buildingId`
  index.
- Added shared forward/reverse mapping helpers for building entity mapping writes
  and removals.
- Replaced linear reverse scans in building target conversion and constructed
  building adoption with reverse-index lookups.
- `CombatBehavior` proves reverse lookup and cleanup after building removal.

Automated gates:
- Command: `dotnet build ProceduralRts.csproj --no-restore`
  Result: pass.
  Evidence: build completed successfully with 0 warnings and 0 errors.
- Command: `dotnet build tools/ReviewGate/ReviewGate.csproj --no-restore`
  Result: pass.
  Evidence: build completed successfully with 0 warnings and 0 errors.
- Command: `dotnet run --project tools/CombatBehavior/CombatBehavior.csproj --no-restore`
  Result: pass.
  Evidence: Combat behavior passed with reverse EntityId index lookup and
    removal assertions.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore -- buildingtargetreverseentityindex`
  Result: pass.
  Evidence: ReviewGate accepted reverse-index maintenance helpers and rejected
    linear forward-map reverse scans.
- Command: `dotnet run --project tools/SimReplay/SimReplay.csproj --no-restore`
  Result: pass.
  Evidence: deterministic replay suite passed after reverse-index migration.

Manual/visual gates:
- Check: Visual/UI review
  Result: not applicable.
  Evidence: This slice changes lookup structure only.

Reviewer result:
- Status: pass after integration review.
- Required fixes:
  - None currently known.
- Residual risks:
  - `_buildingTargetSeedsById` remains lifecycle/write/sync compatibility storage.
  - Resource-field reverse lookup is still linear and remains outside this slice.

TODO update:
- Items marked done:
  - UnitBattlefieldBuildingTarget reverse EntityId index cleanup
- Items left open:
  - Broader Migration cleanup and final seed-storage deletion remain open.
