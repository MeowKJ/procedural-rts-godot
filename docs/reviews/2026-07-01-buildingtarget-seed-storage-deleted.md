# Review Record - BuildingTarget seed storage deletion

Step:
- UnitBattlefieldBuildingTarget seed storage deletion

Milestone:
- M1 EntityWorld authority / Migration cleanup

Owner AI:
- Codex

Reviewer AI:
- Codex; ReviewGate buildingtargetseedstoragedeleted

Integrator AI:
- Codex

Scope:
- Files/folders:
  - scripts/core/units/runtime/UnitBattlefield.cs
  - tools/CombatBehavior/Program.cs
  - tools/ReviewGate/Program.cs
  - TODO.md
  - docs/reviews/2026-07-01-buildingtarget-seed-storage-deleted.md
- Non-goals:
  - Do not change building production, construction placement, combat balance,
    UI, art, or faction roster data.
  - Do not remove the EntityWorld building id mappings.
  - Do not change public building snapshot semantics beyond deleting the seed
    cache fallback.

Implementation summary:
- Deleted the final temporary `_buildingTargetSeedsById` lifecycle store.
- Deleted the `BuildingTargetById(...)` and `RemoveBuildingTargetState(...)`
  seed-cache helpers.
- Building sync now uses explicit seed data from upsert/adoption, or derives seed
  data from the existing EntityWorld building through
  `SeedForExistingBuildingEntity(...)`.
- Building id allocation, public removal, and dead-building cleanup now rely on
  EntityWorld identity/projection and the building EntityId mappings.

Automated gates:
- Command: `dotnet build ProceduralRts.csproj --no-restore`
  Result: pass.
  Evidence: build completed successfully with 0 warnings and 0 errors.
- Command: `dotnet build tools/ReviewGate/ReviewGate.csproj --no-restore`
  Result: pass.
  Evidence: build completed successfully with 0 warnings and 0 errors.
- Command: `dotnet run --project tools/CombatBehavior/CombatBehavior.csproj --no-restore`
  Result: pass.
  Evidence: Combat behavior passed with seed-storage absence assertions.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore -- buildingtargetseedstoragedeleted`
  Result: pass.
  Evidence: ReviewGate passed with 0 errors and 0 warnings.
- Command: `dotnet run --project tools/SimReplay/SimReplay.csproj --no-restore`
  Result: pass.
  Evidence: SimReplay PASSED.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore`
  Result: pass.
  Evidence: ReviewGate passed with 0 errors and 0 warnings.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore -- review --require-record=buildingtarget-seed-storage-deleted`
  Result: pass.
  Evidence: ReviewGate passed with 0 errors and 0 warnings.
- Command: `dotnet run --project tools/VerifyAll/VerifyAll.csproj --no-restore`
  Result: pass.
  Evidence: VerifyAll PASSED, 23/23 steps.

Manual/visual gates:
- Check: Visual/UI review
  Result: not applicable.
  Evidence: This slice changes simulation/runtime authority only.

Reviewer result:
- Status: pass after integration review.
- Required fixes:
  - None currently known.
- Residual risks:
  - Historical review records still describe earlier migration slices where seed
    storage intentionally remained; the current gate and this record are the new
    final-state authority.

TODO update:
- Items marked done:
  - UnitBattlefieldBuildingTarget seed storage deletion
- Items left open:
  - Broader M1 duplicate-data cleanup and legacy catalog deletion remain open.
