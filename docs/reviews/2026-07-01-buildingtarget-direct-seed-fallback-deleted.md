# Review Record - BuildingTarget direct seed fallback deletion

Step:
- UnitBattlefieldBuildingTarget direct seed fallback deletion

Milestone:
- M1 EntityWorld authority / Migration cleanup

Owner AI:
- Codex

Reviewer AI:
- Codex; ReviewGate buildingtargetdirectseedfallbackdeleted

Integrator AI:
- Codex

Scope:
- Files/folders:
  - scripts/core/units/runtime/UnitBattlefield.cs
  - tools/CombatBehavior/Program.cs
  - tools/ReviewGate/Program.cs
  - TODO.md
  - docs/reviews/2026-07-01-buildingtarget-direct-seed-fallback-deleted.md
- Non-goals:
  - Do not delete `_buildingTargetSeedsById`.
  - Do not delete `BuildingTargetById(int)` or seed lifecycle writes.
  - Do not change public building API signatures, building gameplay, UI, art, or
    production behavior.

Implementation summary:
- Changed `BuildingIdentity(int)` so it reads only
  `BuildingIdentityComponentState` from EntityWorld.
- Changed `BuildingSnapshot(int)` so it requires both EntityWorld identity and
  `EntityProjection`, and no longer assembles snapshots from seed fallback data.
- Extended CombatBehavior to prove `BuildingSnapshot should require EntityWorld building identity`
  after a building identity component is removed.
- Extended CombatBehavior to prove same-id upsert restores the EntityWorld
  identity after the missing-identity state.
- Added `ReviewGate buildingtargetdirectseedfallbackdeleted` and updated affected
  historical snapshot gates to forbid direct seed fallback.

Automated gates:
- Command: `dotnet build ProceduralRts.csproj --no-restore`
  Result: pass
  Evidence: build completed successfully with 0 warnings and 0 errors.
- Command: `dotnet build tools/ReviewGate/ReviewGate.csproj --no-restore`
  Result: pass
  Evidence: build completed successfully with 0 warnings and 0 errors.
- Command: `dotnet run --project tools/CombatBehavior/CombatBehavior.csproj --no-restore`
  Result: pass
  Evidence: Combat behavior passed, including the direct seed-fallback snapshot
    regression.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore -- buildingtargetsnapshotinternalid`
  Result: pass
  Evidence: ReviewGate passed with 0 errors and 0 warnings.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore -- buildingtargetsnapshotprojection`
  Result: pass
  Evidence: ReviewGate passed with 0 errors and 0 warnings.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore -- buildingtargetidsseedfallbackdeleted`
  Result: pass
  Evidence: ReviewGate passed with 0 errors and 0 warnings.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore -- buildingtargetdirectseedfallbackdeleted`
  Result: pass
  Evidence: ReviewGate passed with 0 errors and 0 warnings.
- Command: `dotnet run --project tools/SimReplay/SimReplay.csproj --no-restore`
  Result: pass
  Evidence: SimReplay PASSED.

Manual/visual gates:
- Check: Visual/UI review
  Result: not applicable
  Evidence: This slice changes building snapshot identity fallback only.

Reviewer result:
- Status: pass after integration review.
- Required fixes:
  - None.
- Reviewer notes:
  - Building read models now fail closed without EntityWorld identity/projection.
  - The compatibility upsert path still restores EntityWorld identity instead of
    relying on the deleted read fallback.
- Residual risks:
  - `_buildingTargetSeedsById` and `BuildingTargetById(int)` remain for lifecycle
    write/sync compatibility.
  - `BuildingKind` remains as the legacy building identity enum until the later
    entity-path deletion milestone.
  - Full `VerifyAll` passed 23/23 after the multi-slice batch.

TODO update:
- Items marked done:
  - UnitBattlefieldBuildingTarget direct seed fallback deletion
- Items left open:
  - Broader Migration cleanup and final `BuildingKind` / entity-path deletion
    remain open.
- Reason:
  - This slice removes seed fallback from direct read models, but does not delete
    the temporary seed lifecycle storage.
