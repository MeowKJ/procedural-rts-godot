# Review Record - UnitBattlefieldBuildingTarget adopt internal id cleanup

Step:
- UnitBattlefieldBuildingTarget adopt internal id cleanup

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
  - docs/reviews/2026-06-30-buildingtarget-adopt-internal-id.md
- Non-goals:
  - Do not change construction command semantics, identity components, queue
    component setup, or building snapshot payloads.
  - Do not remove the private migration wrapper list or wrapper creation.

Implementation summary:
- Renamed the private adoption helper to `AdoptConstructedBuildingId(...)`.
- Changed constructed-building adoption to return an `int` building id instead of
  a mutable `UnitBattlefieldBuildingTarget`.
- Updated `ConstructBuilding(...)` to publish `RequiredBuildingSnapshot(adoptedId)`.
- Kept unmapped constructed building adoption and identity/queue component setup
  unchanged aside from the id-returning helper.
- Added `ReviewGate buildingtargetadoptinternalid`.

Automated gates:
- Command: `dotnet build ProceduralRts.csproj --no-restore`
  Result: pass
  Evidence: 0 warnings, 0 errors.
- Command: `dotnet build tools/ReviewGate/ReviewGate.csproj --no-restore`
  Result: pass
  Evidence: 0 warnings, 0 errors.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore -- buildingtargetadoptinternalid`
  Result: pass
  Evidence: ReviewGate passed with 0 errors, 0 warnings.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore -- buildingtargetentitylookupinternalid`
  Result: pass
  Evidence: ReviewGate passed with 0 errors, 0 warnings.
- Command: `dotnet run --project tools/PlayerLoopQa/PlayerLoopQa.csproj --no-restore`
  Result: pass
  Evidence: PlayerLoopQa passed.
- Command: `dotnet run --project tools/SimReplay/SimReplay.csproj --no-restore`
  Result: pass
  Evidence: SimReplay passed.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore -- review --require-record=buildingtarget-adopt-internal-id`
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
  Evidence: This slice changes private adoption return plumbing only.

Reviewer result:
- Status: pass after integration review.
- Required fixes:
  - None.
- Reviewer notes:
  - `AdoptConstructedBuildingId(...)` still validates existing EntityWorld building
    mirrors by id and still writes `BuildingIdentityComponentState` plus production
    queue components for newly adopted buildings.
  - `ConstructBuilding(...)` now only receives an id and re-reads immutable
    snapshot data through `RequiredBuildingSnapshot(adoptedId)`.
- Residual risks:
  - The private wrapper list and `new UnitBattlefieldBuildingTarget` creation
    remain until final M1 wrapper deletion.
  - ReviewGate is string/regex-based rather than semantic type analysis.

TODO update:
- Items marked done:
  - UnitBattlefieldBuildingTarget adopt internal id cleanup
- Items left open:
  - Private wrapper storage, wrapper creation, `BuildingTargetById`, and final
    legacy `BuildingKind` deletion.
- Reason:
  - Constructed-building adoption no longer returns mutable target-wrapper handles.
