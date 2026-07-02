# Review Record - M9 Construct Building Adoption Buffers

Step:
M9 construct-building adoption buffer reuse (#94)

Milestone:
M9 - Elegance & Decoupling

Owner AI:
Remote Linux Codex

Reviewer AI:
Remote Linux Codex

Integrator AI:
Remote Linux Codex

Scope:
- Files/folders: `scripts/core/units/runtime/battlefield/UnitBattlefield.BuildingLifecycle.cs`, `scripts/core/units/runtime/battlefield/UnitBattlefield.ConstructionTickets.cs`, `tools/ReviewGateRuntime/UnitBattlefieldRuntimeAllocationReviewGate.cs`, `TODO.md`.
- Non-goals: no construction placement, ready-ticket, credit/refund, build timing, power, or UI text changes.

Implementation summary:
- Reused `_constructionEntityIdsBefore` and `CollectEntityIds(...)` in direct `ConstructBuilding(...)`.
- Reused `DrainConstructionRejection(...)` and `LastNewConstructedEntity(...)` so direct construction and ready-ticket placement share the explicit adoption helpers.
- Added `ReviewGate simhot` evidence in the runtime allocation gate to forbid the old direct construction `ToHashSet()` and new-entity LINQ ordering chain.

Automated gates:
- Command: `dotnet build ProceduralRts.csproj --no-restore`
  Result: pass
  Evidence: Debug build succeeded with 0 warnings and 0 errors.
- Command: `dotnet run --project tools/PlayerLoopQa/PlayerLoopQa.csproj --no-restore`
  Result: pass
  Evidence: PlayerLoopQa passed, including build radius and cat ready-ticket construction placement.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore -- simhot --max-warnings=0`
  Result: pass
  Evidence: ReviewGate simhot passed with 0 errors and 0 warnings.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore`
  Result: pass
  Evidence: Full ReviewGate passed with 0 errors and 0 warnings after syncing validation-tool budget evidence.
- Command: `dotnet run --project tools/VerifyAll/VerifyAll.csproj --no-restore`
  Result: pass
  Evidence: VerifyAll passed 23/23.

Manual/visual gates:
- Check: Visual QA
  Result: not applicable
  Evidence: Construction adoption allocation refactor only; no rendering or UI changed.

Reviewer result:
- Status: pass
- Required fixes: kept new runtime allocation checks in `tools/ReviewGateRuntime` so `tools/ReviewGateDomains` remains under its 1000-line suite budget.
- Residual risks: broader `BuildingTargetIds()` snapshot allocation remains separate M9 debt.

TODO update:
- Items marked done: none; M9 per-tick allocation paydown remains open.
- Items left open: broader profiler-guided UnitBattlefield and projection allocation cleanup.
- Reason: This closes only the direct construction adoption allocation child slice.
