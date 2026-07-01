# Review Record - UnitBattlefieldBuildingTarget production completion internal id cleanup

Step:
- UnitBattlefieldBuildingTarget production completion internal id cleanup

Milestone:
- M1 EntityWorld authority

Owner AI:
- Codex

Reviewer AI:
- Kierkegaard the 2nd

Integrator AI:
- Codex

Scope:
- Files/folders:
  - scripts/core/units/runtime/UnitBattlefield.cs
  - tools/ReviewGate/Program.cs
  - TODO.md
  - docs/reviews/2026-06-30-buildingtarget-production-complete-internal-id.md
- Non-goals:
  - Do not change production queue advancement, spawn positions, costs, durations,
    or completion event payload types.
  - Do not change production enqueue, cancel, option-state aggregation, or AI
    production planning.
  - Do not migrate snapshot helper, BuildSpec helper, combat targeting helpers, or
    final wrapper storage.

Implementation summary:
- Changed `UpdateProductionQueues` to collect `activeProducerIds` instead of
  active producer wrappers.
- Changed `queuedBefore` entries to carry producer id, immutable
  `UnitBattlefieldBuildingSnapshot`, and queue head item.
- Kept completion matching by owner, completed unit design id, and nearest producer
  snapshot position.
- Published `ProductionCompleted` with the stored id-derived snapshot.
- Added `ReviewGate buildingtargetproductioncompleteinternalid`.

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
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore -- buildingtargeteventobjectdeleted`
  Result: pass
  Evidence: ReviewGate passed with 0 errors, 0 warnings.
- Command: `dotnet run --project tools/PlayerLoopQa/PlayerLoopQa.csproj --no-restore`
  Result: pass
  Evidence: PlayerLoopQa PASSED.
- Command: `dotnet run --project tools/AiOpponentLoopQa/AiOpponentLoopQa.csproj --no-restore`
  Result: pass
  Evidence: AiOpponentLoopQa PASSED.
- Command: `dotnet run --project tools/CombatBehavior/CombatBehavior.csproj --no-restore`
  Result: pass
  Evidence: Combat behavior passed.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore -- review --require-record=buildingtarget-production-complete-internal-id`
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
  Evidence: This slice changes internal production-completion matching plumbing only.

Reviewer result:
- Status: pass after review.
- Required fixes:
  - None.
- Reviewer notes:
  - Kierkegaard the 2nd confirmed completion matching still uses owner, design id,
    and nearest producer snapshot position; `ProductionCompleted` still publishes
    the same snapshot/item/unit payload shape; UI and AI production paths continue
    through the same enqueue and option-state surfaces.
- Residual risks:
  - `BuildingSnapshot(int)` still resolves the temporary migration wrapper during M1.
  - Production completion matching intentionally keeps the previous nearest-position
    heuristic, now using a snapshot captured before the production system step.
  - Multiple same-owner same-design producers completing at the same tick still use
    the prior nearest-position attribution heuristic and do not consume matched
    queued-before entries.
  - `UnitProductionQueueItem` remains a mutable class reference in the completion
    event payload; this slice only removes producer wrapper flow.
  - ReviewGate is string/regex-based rather than semantic type analysis.

TODO update:
- Items marked done:
  - UnitBattlefieldBuildingTarget production completion internal id cleanup
- Items left open:
  - Snapshot/build-spec helper cleanup, combat targeting helpers, and final wrapper
    deletion migrations.
- Reason:
  - This slice removes wrapper flow from production completion matching while
    preserving completed-unit event behavior.
