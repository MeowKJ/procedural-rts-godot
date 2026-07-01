# Review Record - UnitBattlefieldBuildingTarget candidate producers internal id cleanup

Step:
- UnitBattlefieldBuildingTarget candidate producers internal id cleanup

Milestone:
- M1 EntityWorld authority

Owner AI:
- Codex

Reviewer AI:
- Leibniz the 2nd

Integrator AI:
- Codex

Scope:
- Files/folders:
  - scripts/core/units/runtime/UnitBattlefield.cs
  - tools/ReviewGate/Program.cs
  - TODO.md
  - docs/reviews/2026-06-30-buildingtarget-candidate-producers-internal-id.md
- Non-goals:
  - Do not change production costs, durations, queue ordering, tech rules, or UI
    labels.
  - Do not change production completion matching or cancel-production behavior.
  - Do not migrate production-completed event helper, snapshot helper, BuildSpec
    helper, or final wrapper storage.

Implementation summary:
- Replaced wrapper-returning `CandidateProducers(...)` helpers with id-returning
  `CandidateProducerIds(...)` helpers for both `ProductionKind` and `UnitSpec`.
- Updated production enqueue paths to choose nullable producer ids, sync producer
  entities by id, address `ProduceEntityCommand` with producer entity ids, read
  queues by id, and publish `ProductionQueued` with id-derived snapshots.
- Updated production option-state aggregation to sum queue/progress by producer id.
- Preserved owner, faction, alive, powered, completed, producer-kind,
  design-availability, and producer tech-tier filters.
- Added `ReviewGate buildingtargetcandidateproducersinternalid`.

Automated gates:
- Command: `dotnet build ProceduralRts.csproj --no-restore`
  Result: pass
  Evidence: 0 warnings, 0 errors.
- Command: `dotnet build tools/ReviewGate/ReviewGate.csproj --no-restore`
  Result: pass
  Evidence: 0 warnings, 0 errors.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore -- buildingtargetcandidateproducersinternalid`
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
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore -- review --require-record=buildingtarget-candidate-producers-internal-id`
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
  Evidence: This slice changes internal production-candidate plumbing only.

Reviewer result:
- Status: pass after review.
- Required fixes:
  - None.
- Reviewer notes:
  - Leibniz the 2nd confirmed enqueue paths avoid empty-sequence id 0 by nullable
    projection, candidate filtering still preserves owner/faction/alive/powered/
    completed/producer-kind/tech-tier rules, producer entity sync and queue reads
    use producer ids, queued events use id-derived snapshots, and UI option state
    still aggregates queue/progress by producer id.
- Residual risks:
  - Production completion matching still carries wrapper entries in `queuedBefore`
    and remains a future cleanup slice.
  - `BuildingSnapshot(int)` still resolves the temporary migration wrapper during M1.
  - ReviewGate is string/regex-based rather than semantic type analysis.

TODO update:
- Items marked done:
  - UnitBattlefieldBuildingTarget candidate producers internal id cleanup
- Items left open:
  - Production-completed matching, snapshot/build-spec helper cleanup, combat
    targeting helpers, and final wrapper deletion migrations.
- Reason:
  - This slice removes wrapper flow from internal production candidate selection while
    preserving production behavior and queued event snapshots.
