# Review Record - BuildingTarget upsert projection reads

Step:
- UnitBattlefieldBuildingTarget upsert projection read cleanup

Milestone:
- M1 EntityWorld authority / Migration cleanup

Owner AI:
- Codex

Reviewer AI:
- Codex; ReviewGate buildingtargetupsertprojectionreads

Integrator AI:
- Codex

Scope:
- Files/folders:
  - scripts/core/units/runtime/UnitBattlefield.cs
  - tools/CombatBehavior/Program.cs
  - tools/ReviewGate/Program.cs
  - TODO.md
  - docs/reviews/2026-07-01-buildingtarget-upsert-projection-reads.md
- Non-goals:
  - Do not delete `_buildingTargetSeedsById`.
  - Do not change public upsert signature or event payloads.
  - Do not change construction placement, production, combat, UI, art, or balance.

Implementation summary:
- `UpsertBuildingTarget(...)` now reads existing identity from EntityWorld
  `BuildingIdentityComponentState` and preserves that identity on same-id
  upserts.
- Temporary seed storage is refreshed through an idempotent write helper instead
  of being read to decide existing building identity.
- `CombatBehavior` removes seed storage, then proves same-id upsert preserves
  EntityWorld identity while refreshing position, HP, and rally state.

Automated gates:
- Command: `dotnet build ProceduralRts.csproj --no-restore`
  Result: pass.
  Evidence: build completed successfully with 0 warnings and 0 errors.
- Command: `dotnet build tools/ReviewGate/ReviewGate.csproj --no-restore`
  Result: pass.
  Evidence: build completed successfully with 0 warnings and 0 errors.
- Command: `dotnet run --project tools/CombatBehavior/CombatBehavior.csproj --no-restore`
  Result: pass.
  Evidence: Combat behavior passed with seedless same-id upsert assertions.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore -- buildingtargetupsertprojectionreads`
  Result: pass.
  Evidence: ReviewGate accepted EntityWorld identity reads plus idempotent seed
    refresh for building upsert.
- Command: `dotnet run --project tools/SimReplay/SimReplay.csproj --no-restore`
  Result: pass.
  Evidence: deterministic replay suite passed after upsert projection-read
    migration.

Manual/visual gates:
- Check: Visual/UI review
  Result: not applicable.
  Evidence: This slice changes upsert read authority only.

Reviewer result:
- Status: pass after integration review.
- Required fixes:
  - None currently known.
- Residual risks:
  - `_buildingTargetSeedsById` remains lifecycle/write/sync compatibility storage.
  - `SyncBuildingTargetEntity(...)` still consumes seed data and remains a later
    M1 cleanup target.

TODO update:
- Items marked done:
  - UnitBattlefieldBuildingTarget upsert projection read cleanup
- Items left open:
  - Broader Migration cleanup and final seed-storage deletion remain open.
