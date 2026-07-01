# Review Record - BuildingTarget rally command projection reads

Step:
- UnitBattlefieldBuildingTarget rally command projection read cleanup

Milestone:
- M1 EntityWorld authority / Migration cleanup

Owner AI:
- Codex

Reviewer AI:
- Codex; ReviewGate buildingtargetrallycommandprojectionreads

Integrator AI:
- Codex

Scope:
- Files/folders:
  - scripts/core/units/runtime/UnitBattlefield.cs
  - tools/CombatBehavior/Program.cs
  - tools/ReviewGate/Program.cs
  - TODO.md
  - docs/reviews/2026-07-01-buildingtarget-rally-command-projection-reads.md
- Non-goals:
  - Do not change selected-building rally public APIs.
  - Do not change rally UI, resource target behavior, production balance, or
    command timing.
  - Do not delete `_buildingTargetSeedsById`.

Implementation summary:
- Direct `SetRallyPoint(int, ...)` now resolves the producer entity through
  `BuildingEntityByTargetId(...)` and owner data through
  `BuildingIdentityComponentState`.
- `SetRallyPointEntityCommand` now receives the producer EntityWorld id directly
  instead of indexing `_buildingTargetEntityIds` through seed-shaped state.
- `CombatBehavior` removes a building seed entry and proves a direct rally
  command still updates EntityWorld rally state.

Automated gates:
- Command: `dotnet build ProceduralRts.csproj --no-restore`
  Result: pass.
  Evidence: build completed successfully with 0 warnings and 0 errors.
- Command: `dotnet build tools/ReviewGate/ReviewGate.csproj --no-restore`
  Result: pass.
  Evidence: build completed successfully with 0 warnings and 0 errors.
- Command: `dotnet run --project tools/CombatBehavior/CombatBehavior.csproj --no-restore`
  Result: pass.
  Evidence: Combat behavior passed after seedless direct-rally assertions were
    added.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore -- buildingtargetrallycommandprojectionreads`
  Result: pass.
  Evidence: ReviewGate accepted direct rally commands reading producer identity
    and entity ids from EntityWorld.
- Command: `dotnet run --project tools/SimReplay/SimReplay.csproj --no-restore`
  Result: pass.
  Evidence: deterministic replay suite passed after direct rally commands moved
    to EntityWorld projection reads.

Manual/visual gates:
- Check: Visual/UI review
  Result: not applicable.
  Evidence: This slice changes command read authority only.

Reviewer result:
- Status: pass after integration review.
- Required fixes:
  - None currently known.
- Residual risks:
  - `_buildingTargetSeedsById` remains lifecycle/write/sync compatibility storage.
  - Selected-building rally batching/performance remains a later hot-path slice.

TODO update:
- Items marked done:
  - UnitBattlefieldBuildingTarget rally command projection read cleanup
- Items left open:
  - Broader Migration cleanup and final seed-storage deletion remain open.
