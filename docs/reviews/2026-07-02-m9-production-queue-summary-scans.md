# Review Record - M9 Production Queue Summary Scans

Step:
M9 production queue summary scan buffer reuse (#92)

Milestone:
M9 - Elegance & Decoupling

Owner AI:
Remote Linux Codex

Reviewer AI:
Remote Linux Codex

Integrator AI:
Remote Linux Codex

Scope:
- Files/folders: `scripts/core/units/runtime/UnitBattlefield.cs`, `scripts/core/units/runtime/battlefield/UnitBattlefield.ProductionRally.cs`, `scripts/core/units/runtime/battlefield/UnitBattlefield.ProductionQueueSummary.cs`, `tools/ReviewGateDomains/RegressionReviewGate.cs`, `tools/ReviewGateDomains/UnitBattlefieldAllocationReviewGate.cs`, `tools/ReviewGateDomains/UnitBattlefieldProductionAllocationReviewGate.cs`, `TODO.md`, `docs/reviews/2026-07-01-file-size-discipline-gate.md`.
- Non-goals: no production refund, production queue rule, HUD copy, HUD layout, or production option state changes.

Implementation summary:
- Split production queue summary/cancel logic out of the near-400-line `UnitBattlefield.ProductionRally.cs` into `UnitBattlefield.ProductionQueueSummary.cs`.
- Added `_productionQueueSummaryBuffer` and `_productionQueueSummarySeenIds` so queue summary and cancel paths reuse queue-entry and building de-duplication storage.
- Replaced `HasQueuedProduction(...)` LINQ filters with an explicit early-exit scan over `EntityWorld.OrderedEntities`.
- Replaced anonymous `SelectMany(... new { ... }).OrderBy(...).ToList()` queue summary materialization with `CollectQueuedProductionSummary(...)` plus in-place sort.
- Split production allocation checks into `UnitBattlefieldProductionAllocationReviewGate` so ReviewGate domain files remain under the 200-line validation-tool limit.
- Synced validation-tool source budget evidence after adding the new ReviewGate domain file.

Automated gates:
- Command: `dotnet build ProceduralRts.csproj --no-restore`
  Result: pass
  Evidence: Debug build succeeded with 0 warnings and 0 errors.
- Command: `dotnet run --project tools/PlayerLoopQa/PlayerLoopQa.csproj`
  Result: pass
  Evidence: PlayerLoopQa passed, including production, cancel-adjacent queue use, rally, selection, victory, and defeat coverage.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj simhot`
  Result: pass
  Evidence: ReviewGate simhot passed with 0 errors and 0 warnings.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj`
  Result: pass pending final rerun
  Evidence: Initial run only failed the expected validation-tool source budget lock before TODO/review evidence sync; final rerun is required before close.

Manual/visual gates:
- Check: Visual QA
  Result: not applicable
  Evidence: Queue summary allocation refactor only; no rendering or UI layout changed.

Reviewer result:
- Status: pass pending final full-gate rerun
- Required fixes: none identified in queue summary/cancel paths.
- Residual risks: production option state scans still contain producer-list materialization and remain tracked by #93.

TODO update:
- Items marked done: none; M9 per-tick allocation paydown remains open.
- Items left open: production option state scans and broader profiler-guided allocation cleanup.
- Reason: This closes only the production queue summary allocation child slice.
