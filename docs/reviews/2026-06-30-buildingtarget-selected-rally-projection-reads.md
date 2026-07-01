# Review Record - UnitBattlefieldBuildingTarget selected rally projection read cleanup

Step:
- UnitBattlefieldBuildingTarget selected rally projection read cleanup

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
  - docs/reviews/2026-06-30-buildingtarget-selected-rally-projection-reads.md
- Non-goals:
  - Do not change public `SetSelectedBuildingRallyPoints` signatures.
  - Do not change rally command semantics, target clamping, or resource-rally
    entity syncing.
  - Do not migrate the single-building `SetRallyPoint(int, ...)` write helper in
    this slice.
  - Do not remove the private migration wrapper list yet.

Implementation summary:
- Changed selected point-rally and resource-rally commands to enumerate selected
  buildings through `BuildingTargetIds()`.
- Changed owner filtering to `BuildingIdentity(int)` and selection filtering to
  `BuildingProjection(int)`.
- Changed producer filtering and rally submission to carry producer ids instead
  of `UnitBattlefieldBuildingTarget` wrapper objects.
- Changed single-producer status labels to derive kind from building identity.
- Added `ReviewGate buildingtargetselectedrallyprojectionreads`.

Automated gates:
- Command: `dotnet build ProceduralRts.csproj --no-restore`
  Result: pass
  Evidence: 0 warnings, 0 errors.
- Command: `dotnet build tools/ReviewGate/ReviewGate.csproj --no-restore`
  Result: pass
  Evidence: 0 warnings, 0 errors.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore -- buildingtargetselectedrallyprojectionreads`
  Result: pass
  Evidence: ReviewGate passed with 0 errors, 0 warnings.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore -- buildingtargetrallyinternalid`
  Result: pass
  Evidence: ReviewGate passed with 0 errors, 0 warnings.
- Command: `dotnet run --project tools/CombatBehavior/CombatBehavior.csproj --no-restore`
  Result: pass
  Evidence: Combat behavior passed.
- Command: `dotnet run --project tools/SimReplay/SimReplay.csproj --no-restore`
  Result: pass
  Evidence: SimReplay passed.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore -- review --require-record=buildingtarget-selected-rally-projection-reads`
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
  Evidence: This slice changes selected-rally command data plumbing only.

Reviewer result:
- Status: pass after integration review.
- Required fixes:
  - None.
- Reviewer notes:
  - Selected producer discovery now shares the EntityWorld-first building id read
    path.
  - Existing public rally command surfaces and resource-rally target syncing are
    preserved.
  - The single-building rally write helper still resolves the wrapper internally
    and is left for a later slice.
- Residual risks:
  - Direct private `Buildings` reads remain in production, construction,
    placement, combat, fog/visibility source, dock/refinery, and cleanup paths.
  - The private migration wrapper list remains until final M1 deletion.
  - ReviewGate is string/regex-based rather than semantic type analysis.

TODO update:
- Items marked done:
  - UnitBattlefieldBuildingTarget selected rally projection read cleanup
- Items left open:
  - Single-building rally write helper migration and remaining hot-path building
    list reads.
- Reason:
  - Selected-building rally commands no longer carry mutable building wrappers
    while choosing selected producers.
