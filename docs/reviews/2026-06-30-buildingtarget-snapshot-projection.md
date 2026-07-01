# Review Record - UnitBattlefieldBuildingTarget snapshot projection cleanup

Step:
- UnitBattlefieldBuildingTarget snapshot projection cleanup

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
  - docs/reviews/2026-06-30-buildingtarget-snapshot-projection.md
- Non-goals:
  - Do not remove the private migration wrapper yet.
  - Do not change building balance, costs, weapons, or footprint tuning.
  - Do not change public building snapshot/event API shapes.

Implementation summary:
- Changed `BuildingSnapshot(int id)` to resolve `BuildingIdentity(int)` first.
- Made immutable building snapshots prefer EntityWorld `EntityProjection`
  position, facing, and health.
- Kept a private `UnitBattlefieldBuildingTarget` fallback for migration-only seed
  reads while the wrapper still exists.
- Changed `LiveBuildingCount(...)` to count immutable snapshots instead of
  mutable wrapper health.
- Added `ReviewGate buildingtargetsnapshotprojection`.

Automated gates:
- Command: `dotnet build ProceduralRts.csproj --no-restore`
  Result: pass
  Evidence: 0 warnings, 0 errors.
- Command: `dotnet build tools/ReviewGate/ReviewGate.csproj --no-restore`
  Result: pass
  Evidence: 0 warnings, 0 errors.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore -- buildingtargetsnapshotprojection`
  Result: pass
  Evidence: ReviewGate passed with 0 errors, 0 warnings.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore -- buildingtargetsnapshotinternalid`
  Result: pass
  Evidence: ReviewGate passed with 0 errors, 0 warnings.
- Command: `dotnet run --project tools/CombatBehavior/CombatBehavior.csproj --no-restore`
  Result: pass
  Evidence: Combat behavior passed.
- Command: `dotnet run --project tools/SimReplay/SimReplay.csproj --no-restore`
  Result: pass
  Evidence: SimReplay passed.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore -- review --require-record=buildingtarget-snapshot-projection`
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
  Evidence: This slice changes snapshot authority plumbing only.

Reviewer result:
- Status: pass after integration review.
- Required fixes:
  - None.
- Reviewer notes:
  - `BuildingSnapshot(int id)` is now EntityWorld-first for runtime projection
    fields and wrapper-only for migration fallback.
  - Footprint remains BuildSpec-derived from id-resolved identity.
  - Public snapshot API remains immutable and id-based.
- Residual risks:
  - `UnitBattlefieldBuildingTarget` still exists as private migration storage.
  - `BuildingSnapshots()` still enumerates the ordered wrapper list until final
    wrapper removal.
  - ReviewGate is string/regex-based rather than semantic type analysis.

TODO update:
- Items marked done:
  - UnitBattlefieldBuildingTarget snapshot projection cleanup
- Items left open:
  - Private wrapper creation/storage removal and final legacy building runtime
    deletion.
- Reason:
  - Building snapshot readers now prefer EntityWorld projections without changing
    public runtime behavior.
