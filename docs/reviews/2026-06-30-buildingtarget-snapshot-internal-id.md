# Review Record - UnitBattlefieldBuildingTarget snapshot internal id cleanup

Step:
- UnitBattlefieldBuildingTarget snapshot internal id cleanup

Milestone:
- M1 EntityWorld authority

Owner AI:
- External owner / Codex integration

Reviewer AI:
- Codex

Integrator AI:
- Codex

Scope:
- Files/folders:
  - scripts/core/units/runtime/UnitBattlefield.cs
  - tools/ReviewGate/Program.cs
  - TODO.md
  - docs/reviews/2026-06-30-buildingtarget-snapshot-internal-id.md
- Non-goals:
  - Do not change snapshot payload fields or event shapes.
  - Do not remove the private migration wrapper list.
  - Do not migrate `SyncBuildingTargetEntity`.

Implementation summary:
- Building snapshots now route through `BuildingSnapshot(int id)`.
- Required snapshot reads now route through
  `RequiredBuildingSnapshot(int id)` instead of a wrapper-shaped converter.
- Upsert, constructed-building adoption, combat events, turret events, and
  production events publish immutable id-derived building snapshots.
- Added `ReviewGate buildingtargetsnapshotinternalid` to lock the private
  snapshot helper boundary.

Automated gates:
- Command: `dotnet build ProceduralRts.csproj --no-restore`
  Result: pass
  Evidence: 0 warnings, 0 errors.
- Command: `dotnet build tools/ReviewGate/ReviewGate.csproj --no-restore`
  Result: pass
  Evidence: 0 warnings, 0 errors.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore -- buildingtargetsnapshotinternalid`
  Result: pass
  Evidence: ReviewGate passed with 0 errors, 0 warnings.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore -- review --require-record=buildingtarget-snapshot-internal-id`
  Result: pass
  Evidence: ReviewGate passed with 0 errors, 0 warnings.
- Command: `dotnet run --project tools/VerifyAll/VerifyAll.csproj --no-restore`
  Result: pass
  Evidence: VerifyAll PASSED, 23/23 steps.

Manual/visual gates:
- Check: Visual/UI review
  Result: not applicable
  Evidence: This slice changes internal snapshot plumbing only.

Reviewer result:
- Status: pass after integration review.
- Required fixes:
  - None.
- Reviewer notes:
  - `BuildingSnapshot(int id)` is the single nullable snapshot entrypoint.
  - `RequiredBuildingSnapshot(int id)` keeps upsert/adoption callers fail-fast
    without exposing the mutable wrapper.
  - Published building events still use `UnitBattlefieldBuildingSnapshot`.
- Residual risks:
  - `BuildingSnapshot(int id)` still resolves the private migration wrapper
    internally until final M1 wrapper deletion.
  - ReviewGate is string/regex-based rather than semantic type analysis.

TODO update:
- Items marked done:
  - UnitBattlefieldBuildingTarget snapshot internal id cleanup
- Items left open:
  - Sync helper cleanup, private wrapper storage, and final wrapper deletion.
- Reason:
  - Snapshot conversion no longer accepts `UnitBattlefieldBuildingTarget` as an
    internal helper parameter.
