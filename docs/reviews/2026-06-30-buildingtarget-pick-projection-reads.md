# Review Record - UnitBattlefieldBuildingTarget pick projection read cleanup

Step:
- UnitBattlefieldBuildingTarget pick projection read cleanup

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
  - docs/reviews/2026-06-30-buildingtarget-pick-projection-reads.md
- Non-goals:
  - Do not change public building pick API shapes.
  - Do not change selection-controller behavior or pick padding.
  - Do not migrate production, construction, placement, combat, or dock/refinery
    hot paths in this slice.
  - Do not remove the private migration wrapper list yet.

Implementation summary:
- Changed hostile, owned, and any-building pick helpers to enumerate
  `BuildingTargetIds()`.
- Changed pick candidates to immutable `BuildingSnapshot` values instead of
  direct `Buildings` wrapper records.
- Preserved pick distance checks, id-based radius padding, relation/owner
  filtering, hostile distance-only ordering, and owned/any id tie-breaks.
- Added `ReviewGate buildingtargetpickprojectionreads`.

Automated gates:
- Command: `dotnet build ProceduralRts.csproj --no-restore`
  Result: pass
  Evidence: 0 warnings, 0 errors.
- Command: `dotnet build tools/ReviewGate/ReviewGate.csproj --no-restore`
  Result: pass
  Evidence: 0 warnings, 0 errors.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore -- buildingtargetpickprojectionreads`
  Result: pass
  Evidence: ReviewGate passed with 0 errors, 0 warnings.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore -- buildingtargetpickinternalid`
  Result: pass
  Evidence: ReviewGate passed with 0 errors, 0 warnings.
- Command: `dotnet run --project tools/CombatBehavior/CombatBehavior.csproj --no-restore`
  Result: pass
  Evidence: Combat behavior passed.
- Command: `dotnet run --project tools/SimReplay/SimReplay.csproj --no-restore`
  Result: pass
  Evidence: SimReplay passed.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore -- review --require-record=buildingtarget-pick-projection-reads`
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
  Evidence: This slice changes input pick data plumbing only and preserves pick
    API behavior.

Reviewer result:
- Status: pass after integration review.
- Required fixes:
  - None.
- Reviewer notes:
  - Building pick helpers now use the same id/snapshot read path as migrated
    public building projections.
  - Public hover projection APIs stay unchanged.
  - Historical ordering expectations remain covered by ReviewGate.
- Residual risks:
  - Direct private `Buildings` reads remain in production, construction,
    placement, combat, fog/visibility source, dock/refinery, and cleanup paths.
  - The private migration wrapper list remains until final M1 deletion.
  - ReviewGate is string/regex-based rather than semantic type analysis.

TODO update:
- Items marked done:
  - UnitBattlefieldBuildingTarget pick projection read cleanup
- Items left open:
  - Remaining hot-path building list reads and final wrapper deletion.
- Reason:
  - Building click/hover candidates no longer directly read the second building
    runtime list.
