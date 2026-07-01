# Review Record - BuildingTarget upsert seedless

Step:
- UnitBattlefieldBuildingTarget upsert seedless cleanup

Milestone:
- M1 EntityWorld authority / Migration cleanup

Owner AI:
- Codex

Reviewer AI:
- Codex; ReviewGate buildingtargetupsertseedless

Integrator AI:
- Codex

Scope:
- Files/folders:
  - scripts/core/units/runtime/UnitBattlefield.cs
  - tools/CombatBehavior/Program.cs
  - tools/ReviewGate/Program.cs
  - TODO.md
  - docs/reviews/2026-07-01-buildingtarget-upsert-seedless.md
- Non-goals:
  - Do not delete `_buildingTargetSeedsById`.
  - Do not change public building upsert semantics.
  - Do not change construction placement, production, combat, UI, art, or balance.

Implementation summary:
- `UpsertBuildingTarget(...)` now passes explicit `BuildingEntitySeed` data into
  `SyncBuildingTargetEntity(...)`.
- The upsert seed-cache helper was removed, so ordinary upserts no longer
  repopulate `_buildingTargetSeedsById`.
- The sync bridge prefers explicit seed data, then optional seed cache state, and
  finally seedless EntityWorld identity/transform/health.

Automated gates:
- Command: `dotnet build ProceduralRts.csproj --no-restore`
  Result: pass.
  Evidence: build completed successfully with 0 warnings and 0 errors.
- Command: `dotnet build tools/ReviewGate/ReviewGate.csproj --no-restore`
  Result: pass.
  Evidence: build completed successfully with 0 warnings and 0 errors.
- Command: `dotnet run --project tools/CombatBehavior/CombatBehavior.csproj --no-restore`
  Result: pass.
  Evidence: Combat behavior passed with seedless upsert no-repopulation assertions.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore -- buildingtargetupsertseedless`
  Result: pass.
  Evidence: ReviewGate passed with 0 errors and 0 warnings.
- Command: `dotnet run --project tools/SimReplay/SimReplay.csproj --no-restore`
  Result: pass.
  Evidence: SimReplay PASSED.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore`
  Result: pass.
  Evidence: ReviewGate passed with 0 errors and 0 warnings after updating legacy gate expectations for explicit seed sync.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore -- review --require-record=buildingtarget-upsert-seedless`
  Result: pass.
  Evidence: ReviewGate passed with 0 errors and 0 warnings after evidence backfill.
- Command: `dotnet run --project tools/VerifyAll/VerifyAll.csproj --no-restore`
  Result: pass.
  Evidence: VerifyAll PASSED, 23/23 steps, after the multi-slice batch.

Manual/visual gates:
- Check: Visual/UI review
  Result: not applicable.
  Evidence: This slice changes upsert/sync authority only.

Reviewer result:
- Status: pass after integration review.
- Required fixes:
  - None currently known.
- Residual risks:
  - `_buildingTargetSeedsById` remains lifecycle compatibility storage for
    constructed-building adoption.
  - Final seed-storage deletion remains a later M1 cleanup target.

TODO update:
- Items marked done:
  - UnitBattlefieldBuildingTarget upsert seedless cleanup
- Items left open:
  - Broader Migration cleanup and final seed-storage deletion remain open.
