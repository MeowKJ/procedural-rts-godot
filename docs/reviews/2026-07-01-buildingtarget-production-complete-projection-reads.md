# Review Record - UnitBattlefieldBuildingTarget production completion projection read cleanup

Step:
- UnitBattlefieldBuildingTarget production completion projection read cleanup

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
  - docs/reviews/2026-07-01-buildingtarget-production-complete-projection-reads.md
- Non-goals:
  - Do not change production queue advancement, timing, costs, spawn positions, or
    completion event payload types.
  - Do not change producer eligibility, enqueue, cancel, AI production planning, or
    production option aggregation.
  - Do not remove the private migration wrapper list yet.

Implementation summary:
- Changed `UpdateProductionQueues(...)` to collect active producer ids through
  `BuildingTargetIds()` instead of scanning the private `Buildings` wrapper list.
- Kept active producer filtering on id-based `BuildingProductionQueue(buildingId)`
  reads.
- Preserved pre-step immutable producer snapshots, completed-unit owner/design
  matching, nearest-producer attribution, and `ProductionCompleted` payload shape.
- Added `ReviewGate buildingtargetproductioncompleteprojectionreads` and updated
  the historical production-complete internal-id gate so it no longer requires a
  direct wrapper-list scan.
- Updated the historical production-queue internal-id gate to require
  `BuildingProductionQueue(buildingId)` instead of a wrapper-shaped
  `BuildingProductionQueue(building.Id)` marker.

Automated gates:
- Command: `dotnet build ProceduralRts.csproj --no-restore`
  Result: pass
  Evidence: 0 warnings, 0 errors.
- Command: `dotnet build tools/ReviewGate/ReviewGate.csproj --no-restore`
  Result: pass
  Evidence: 0 warnings, 0 errors.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore -- buildingtargetproductioncompleteinternalid`
  Result: pass
  Evidence: ReviewGate passed with 0 errors, 0 warnings.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore -- buildingtargetproductioncompleteprojectionreads`
  Result: pass
  Evidence: ReviewGate passed with 0 errors, 0 warnings.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore -- buildingtargetproductionqueueinternalid`
  Result: pass
  Evidence: ReviewGate passed with 0 errors, 0 warnings.
- Command: `dotnet run --project tools/CombatBehavior/CombatBehavior.csproj --no-restore`
  Result: pass
  Evidence: Combat behavior passed.
- Command: `dotnet run --project tools/SimReplay/SimReplay.csproj --no-restore`
  Result: pass
  Evidence: SimReplay PASSED.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore -- review --require-record=buildingtarget-production-complete-projection-reads`
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
  Evidence: This slice changes production-completion read plumbing only.

Reviewer result:
- Status: pass after integration review.
- Required fixes:
  - None.
- Reviewer notes:
  - Production completion now uses the same id/projection enumeration direction as
    queue and producer lookup reads.
  - The behavior-sensitive matching logic stays unchanged after the active producer
    list is collected.
- Residual risks:
  - `BuildingTargetIds()` still has a wrapper fallback during the M1 migration
    window.
  - Production completion keeps the prior nearest-position attribution heuristic
    for simultaneous same-owner same-design completions.
  - Direct private `Buildings` reads remain in unrelated construction,
    fog/visibility, placement, combat, dock/refinery, pulse, and cleanup paths.
  - ReviewGate is string/regex-based rather than semantic type analysis.

TODO update:
- Items marked done:
  - UnitBattlefieldBuildingTarget production completion projection read cleanup
- Items left open:
  - Remaining non-production direct wrapper-list reads in construction,
    fog/visibility, combat, placement, dock/refinery, and cleanup paths.
- Reason:
  - Production completion no longer scans the second building runtime list to find
    active producers.
