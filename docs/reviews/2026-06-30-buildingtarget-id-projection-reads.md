# Review Record - UnitBattlefieldBuildingTarget id projection read cleanup

Step:
- UnitBattlefieldBuildingTarget id projection read cleanup

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
  - docs/reviews/2026-06-30-buildingtarget-id-projection-reads.md
- Non-goals:
  - Do not migrate production, construction, pathing, or combat hot paths in this
    slice.
  - Do not remove `UnitBattlefieldBuildingTarget` storage yet.
  - Do not change building balance, placement rules, or visual style.

Implementation summary:
- Added `BuildingTargetIds()` as the shared private building id enumeration helper.
- Made `BuildingTargetIds()` prefer EntityWorld `BuildingIdentityComponentState`
  from ordered entities, with a private wrapper fallback during migration.
- Moved public building snapshots, selected-building projections, rally
  projections, selected building entity ids, hit-pulse projections, and minimap
  projections through `BuildingTargetIds()`.
- Moved owner filters in those read paths to `BuildingIdentity(int)` and liveness
  filters to `EntityProjection`.
- Added `ReviewGate buildingtargetidprojectionreads`.

Automated gates:
- Command: `dotnet build ProceduralRts.csproj --no-restore`
  Result: pass
  Evidence: 0 warnings, 0 errors.
- Command: `dotnet build tools/ReviewGate/ReviewGate.csproj --no-restore`
  Result: pass
  Evidence: 0 warnings, 0 errors.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore -- buildingtargetidprojectionreads`
  Result: pass
  Evidence: ReviewGate passed with 0 errors, 0 warnings.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore -- buildingtargetsnapshotprojection`
  Result: pass
  Evidence: ReviewGate passed with 0 errors, 0 warnings.
- Command: `dotnet run --project tools/CombatBehavior/CombatBehavior.csproj --no-restore`
  Result: pass
  Evidence: Combat behavior passed.
- Command: `dotnet run --project tools/SimReplay/SimReplay.csproj --no-restore`
  Result: pass
  Evidence: SimReplay passed.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore -- review --require-record=buildingtarget-id-projection-reads`
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
  Evidence: This slice changes building read-path plumbing only.

Reviewer result:
- Status: pass after integration review.
- Required fixes:
  - None.
- Reviewer notes:
  - Public building projection read paths now share one id enumeration boundary.
  - EntityWorld identity/projection owns order, owner filtering, and liveness for
    the migrated reads.
  - The fallback wrapper loop is explicit and local to `BuildingTargetIds()`.
- Residual risks:
  - Production, construction, combat, and placement helpers still use the private
    wrapper list directly.
  - The fallback wrapper list remains until final M1 deletion.
  - ReviewGate is string/regex-based rather than semantic type analysis.

TODO update:
- Items marked done:
  - UnitBattlefieldBuildingTarget id projection read cleanup
- Items left open:
  - Hot-path building list reads in production, construction, placement, combat,
    dock/refinery, and final wrapper deletion.
- Reason:
  - Public projection readers no longer directly enumerate the second building
    runtime list.
