# Review Record - UnitBattlefieldBuildingTarget queue projection read cleanup

Step:
- UnitBattlefieldBuildingTarget queue projection read cleanup

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
  - docs/reviews/2026-07-01-buildingtarget-queue-projection-reads.md
- Non-goals:
  - Do not change production costs, timings, queue ordering semantics, or refund
    ratio.
  - Do not migrate production completion stepping or producer eligibility in this
    slice.
  - Do not remove the private migration wrapper list yet.

Implementation summary:
- Changed `CancelFirstProduction(...)` to select the cancel producer through
  `BuildingTargetIds()`, `BuildingIdentity(int)`, and
  `BuildingProductionQueue(int)`.
- Changed `HasQueuedProduction(...)` to query queue presence through id-based
  building enumeration.
- Changed `ProductionQueueSummary(...)` to carry producer ids and queue items
  instead of mutable building wrapper objects.
- Added `ReviewGate buildingtargetqueueprojectionreads`.

Automated gates:
- Command: `dotnet build ProceduralRts.csproj --no-restore`
  Result: pass
  Evidence: 0 warnings, 0 errors.
- Command: `dotnet build tools/ReviewGate/ReviewGate.csproj --no-restore`
  Result: pass
  Evidence: 0 warnings, 0 errors.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore -- buildingtargetqueueprojectionreads`
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
  Evidence: SimReplay passed.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore -- review --require-record=buildingtarget-queue-projection-reads`
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
  Evidence: This slice changes production queue read plumbing only.

Reviewer result:
- Status: pass after integration review.
- Required fixes:
  - None.
- Reviewer notes:
  - Queue presence, queue summary, and first cancel selection now share the
    EntityWorld-first building id read path.
  - Queue item ordering and 50% refund calculation are unchanged.
  - Production queue component state remains the queue authority.
- Residual risks:
  - Direct private `Buildings` reads remain in production completion, producer
    eligibility, construction, placement, combat, fog/visibility source,
    dock/refinery, and cleanup paths.
  - The private migration wrapper list remains until final M1 deletion.
  - ReviewGate is string/regex-based rather than semantic type analysis.

TODO update:
- Items marked done:
  - UnitBattlefieldBuildingTarget queue projection read cleanup
- Items left open:
  - Remaining producer eligibility/completion and other hot-path building list
    reads.
- Reason:
  - Production queue UI/cancel reads no longer directly scan the second building
    runtime list.
