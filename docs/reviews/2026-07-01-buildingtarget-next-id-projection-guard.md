# Review Record - BuildingTarget next id projection guard

Step:
- UnitBattlefieldBuildingTarget next id projection guard cleanup

Milestone:
- M1 EntityWorld authority / Migration cleanup

Owner AI:
- Codex

Reviewer AI:
- Codex; ReviewGate buildingtargetnextidprojectionguard

Integrator AI:
- Codex

Scope:
- Files/folders:
  - scripts/core/units/runtime/UnitBattlefield.cs
  - tools/CombatBehavior/Program.cs
  - tools/ReviewGate/Program.cs
  - TODO.md
  - docs/reviews/2026-07-01-buildingtarget-next-id-projection-guard.md
- Non-goals:
  - Do not change public building id semantics.
  - Do not delete `_buildingTargetSeedsById`.
  - Do not change construction placement, production, combat, UI, art, or balance.

Implementation summary:
- `NextBuildingTargetId()` now routes id occupancy through
  `BuildingTargetIdInUse(...)`.
- The occupancy helper checks EntityWorld building identity first and keeps seed
  storage as a compatibility duplicate guard.
- `CombatBehavior` removes seed storage, forces the next id to an existing
  EntityWorld building id, and proves allocation skips that id.

Automated gates:
- Command: `dotnet build ProceduralRts.csproj --no-restore`
  Result: pass.
  Evidence: build completed successfully with 0 warnings and 0 errors.
- Command: `dotnet build tools/ReviewGate/ReviewGate.csproj --no-restore`
  Result: pass.
  Evidence: build completed successfully with 0 warnings and 0 errors.
- Command: `dotnet run --project tools/CombatBehavior/CombatBehavior.csproj --no-restore`
  Result: pass.
  Evidence: Combat behavior passed with seedless id-allocation assertions.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore -- buildingtargetnextidprojectionguard`
  Result: pass.
  Evidence: ReviewGate accepted `BuildingTargetIdInUse(...)` checking
    EntityWorld identities before seed compatibility storage.
- Command: `dotnet run --project tools/SimReplay/SimReplay.csproj --no-restore`
  Result: pass.
  Evidence: deterministic replay suite passed after next-id projection guard
    migration.

Manual/visual gates:
- Check: Visual/UI review
  Result: not applicable.
  Evidence: This slice changes id allocation authority only.

Reviewer result:
- Status: pass after integration review.
- Required fixes:
  - None currently known.
- Residual risks:
  - `_buildingTargetSeedsById` remains lifecycle/write/sync compatibility storage.
  - Final seed-storage deletion remains a later M1 cleanup target.

TODO update:
- Items marked done:
  - UnitBattlefieldBuildingTarget next id projection guard cleanup
- Items left open:
  - Broader Migration cleanup and final seed-storage deletion remain open.
