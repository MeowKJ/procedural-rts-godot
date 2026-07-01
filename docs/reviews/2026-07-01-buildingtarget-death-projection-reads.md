# Review Record - BuildingTarget death projection reads

Step:
- UnitBattlefieldBuildingTarget death projection read cleanup

Milestone:
- M1 EntityWorld authority / Migration cleanup

Owner AI:
- Codex

Reviewer AI:
- Codex; ReviewGate buildingtargetdeathprojectionreads

Integrator AI:
- Codex

Scope:
- Files/folders:
  - scripts/core/units/runtime/UnitBattlefield.cs
  - tools/CombatBehavior/Program.cs
  - tools/ReviewGate/Program.cs
  - TODO.md
  - docs/reviews/2026-07-01-buildingtarget-death-projection-reads.md
- Non-goals:
  - Do not delete `_buildingTargetSeedsById`.
  - Do not delete all `BuildingTargetById(int)` compatibility reads.
  - Do not change building combat balance, public events, UI, art, or production
    behavior.

Implementation summary:
- Deleted `SyncBuildingHealthFromEntityCore(int)` and stopped writing projected
  EntityWorld health back into `_buildingTargetSeedsById`.
- Updated existing-entity `SyncBuildingTargetEntity(...)` so ordinary sync
  preserves EntityWorld `HealthComponentState`, while explicit
  `UpsertBuildingTarget(...)` calls can still supply an HP override.
- Changed dead-building detection to read projected snapshot health through
  `BuildingSnapshot(...)`.
- Changed `BuildingDeathInfo(int)` to build immutable death payloads from
  projected snapshot data, including projected footprint.
- Changed building repair legality to read projected snapshot HP.

Automated gates:
- Command: `dotnet build ProceduralRts.csproj --no-restore`
  Result: pass
  Evidence: build completed successfully with 0 warnings and 0 errors.
- Command: `dotnet build tools/ReviewGate/ReviewGate.csproj --no-restore`
  Result: pass
  Evidence: build completed successfully with 0 warnings and 0 errors.
- Command: `dotnet run --project tools/CombatBehavior/CombatBehavior.csproj --no-restore`
  Result: pass
  Evidence: Combat behavior passed after EntityWorld health preservation and
    projected death reads.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore -- buildingtargetdeathprojectionreads`
  Result: pass
  Evidence: ReviewGate passed with 0 errors and 0 warnings.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore -- buildingtargetdeathinternalid`
  Result: pass
  Evidence: ReviewGate passed with 0 errors and 0 warnings.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore -- buildingtargethealthinternalid`
  Result: pass
  Evidence: ReviewGate passed with 0 errors and 0 warnings.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore -- buildingtargetentitylookupinternalid`
  Result: pass
  Evidence: ReviewGate passed with 0 errors and 0 warnings.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore -- buildingtargetbuildspechelperdeleted`
  Result: pass
  Evidence: ReviewGate passed with 0 errors and 0 warnings.
- Command: `dotnet run --project tools/SimReplay/SimReplay.csproj --no-restore`
  Result: pass
  Evidence: SimReplay PASSED.

Manual/visual gates:
- Check: Visual/UI review
  Result: not applicable
  Evidence: This slice changes building death/read-model authority only.

Reviewer result:
- Status: pass after integration review.
- Required fixes:
  - None.
- Reviewer notes:
  - Building death detection now uses projected snapshot health instead of seed
    health copied back from EntityWorld.
  - Explicit upsert HP remains supported through an override so tests and setup
    code can author damaged buildings deliberately.
- Residual risks:
  - `_buildingTargetSeedsById` and `BuildingTargetById(int)` remain for lifecycle
    write/sync compatibility.
  - More helper methods still read seed state for command legality and migration
    compatibility; those are later M1 slices.
  - Full `VerifyAll` passed 23/23 after the multi-slice batch.

TODO update:
- Items marked done:
  - UnitBattlefieldBuildingTarget death projection read cleanup
- Items left open:
  - Broader Migration cleanup and final `BuildingKind` / entity-path deletion
    remain open.
- Reason:
  - This slice removes seed health as the building death source, but does not
    delete all temporary seed lifecycle storage.
