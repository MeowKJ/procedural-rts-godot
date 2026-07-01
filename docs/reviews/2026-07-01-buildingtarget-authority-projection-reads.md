# Review Record - UnitBattlefieldBuildingTarget authority projection read cleanup

Step:
- UnitBattlefieldBuildingTarget authority projection read cleanup

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
  - docs/reviews/2026-07-01-buildingtarget-authority-projection-reads.md
- Non-goals:
  - Do not change owner relation rules, alliance rules, resource inventory logic,
    construction command semantics, producer eligibility, or build progress values.
  - Do not remove the private migration wrapper list yet.
  - Do not touch unit roster, light/heavy balancing, UI, art, or movement feel in
    this slice.

Implementation summary:
- Changed `SyncOwnerRelations()` to discover building owner slots through
  `BuildingTargetIds()` and `BuildingIdentity(int)` instead of reading owner slots
  from the private wrapper list.
- Changed `ConstructionSubjectEntities(...)` to discover construction producers
  through `BuildingTargetIds()` and immutable `BuildingSnapshot(int)` state instead
  of enumerating `Buildings` directly.
- Preserved unit owner slots, resource inventory owner slots, baseline player slots,
  owner filtering, required producer filtering, alive/completed filtering,
  deterministic id ordering, and id-based producer entity sync.
- Added `ReviewGate buildingtargetauthorityprojectionreads`.

Automated gates:
- Command: `dotnet build ProceduralRts.csproj --no-restore`
  Result: pass
  Evidence: 0 warnings, 0 errors.
- Command: `dotnet build tools/ReviewGate/ReviewGate.csproj --no-restore`
  Result: pass
  Evidence: 0 warnings, 0 errors.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore -- buildingtargetauthorityprojectionreads`
  Result: pass
  Evidence: ReviewGate passed with 0 errors, 0 warnings.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore -- buildingtargetsyncinternalid`
  Result: pass
  Evidence: ReviewGate passed with 0 errors, 0 warnings.
- Command: `dotnet run --project tools/SimReplay/SimReplay.csproj --no-restore`
  Result: pass
  Evidence: SimReplay PASSED.
- Command: `dotnet run --project tools/CombatBehavior/CombatBehavior.csproj --no-restore`
  Result: pass
  Evidence: Combat behavior passed.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore -- review --require-record=buildingtarget-authority-projection-reads`
  Result: pass
  Evidence: ReviewGate passed with 0 errors, 0 warnings after evidence backfill.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore`
  Result: pass
  Evidence: ReviewGate passed with 0 errors, 0 warnings.
- Command: `dotnet run --project tools/VerifyAll/VerifyAll.csproj --no-restore`
  Result: pass
  Evidence: VerifyAll PASSED, 23/23 steps.

Manual/visual gates:
- Check: Visual/UI review
  Result: not applicable
  Evidence: This slice changes internal authority read plumbing only.

Reviewer result:
- Status: pass after integration review.
- Required fixes:
  - None.
- Reviewer notes:
  - The changed read paths follow the id/state boundary used by recent placement,
    visibility, combat, and production projection cleanup slices.
  - This slice deliberately leaves dock/refinery and cleanup wrapper-list reads for
    later bounded architecture slices.
- Residual risks:
  - `BuildingTargetIds()` still has a wrapper fallback during the M1 migration
    window.
  - ReviewGate is string/regex-based rather than semantic type analysis.
  - Direct private `Buildings` reads remain in construction/sync, dock/refinery,
    and cleanup paths.

TODO update:
- Items marked done:
  - UnitBattlefieldBuildingTarget authority projection read cleanup
- Items left open:
  - Remaining direct wrapper-list reads in construction/sync, dock/refinery, and
    cleanup paths.
- Reason:
  - Owner relation and construction-subject authority reads no longer scan the
    second building runtime list.
