# Review Record - M9 UnitBattlefield Construction Subject Buffers

Step:
M9 UnitBattlefield construction subject buffer reuse (#89)

Milestone:
M9 - Elegance & Decoupling

Owner AI:
Remote Linux Codex

Reviewer AI:
Remote Linux Codex

Integrator AI:
Remote Linux Codex

Scope:
- Files/folders: `scripts/core/units/runtime/UnitBattlefield.cs`, `scripts/core/units/runtime/battlefield/UnitBattlefield.CommandBridge.cs`, `tools/ReviewGateDomains/UnitBattlefieldAllocationReviewGate.cs`, `TODO.md`.
- Non-goals: no ConstructionSystem placement validation, ticket lifecycle, credits/refund, BuildSpec data, or faction construction UX changes.

Implementation summary:
- Added `_constructionSubjectBuildingIds` and `_constructionSubjectEntityBuffer` to reuse UnitBattlefield construction command subject storage.
- Replaced `ConstructionSubjectEntities(...)` snapshot LINQ and `ToList()` materialization with explicit candidate collection, in-place building-id sorting, and entity-id buffer fill.
- Extended `ReviewGate simhot` UnitBattlefield allocation evidence so the construction subject bridge cannot return to snapshot/order LINQ materialization.

Automated gates:
- Command: `dotnet build ProceduralRts.csproj --no-restore`
  Result: pass
  Evidence: Debug build succeeded with 0 warnings and 0 errors.
- Command: `dotnet run --project tools/PlayerLoopQa/PlayerLoopQa.csproj --no-restore`
  Result: pass
  Evidence: PlayerLoopQa passed, including cat ready-ticket placement and player construction loop coverage.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore -- simhot --max-warnings=0`
  Result: pass
  Evidence: ReviewGate simhot passed with 0 errors and 0 warnings.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore`
  Result: pass
  Evidence: Full ReviewGate passed with 0 errors and 0 warnings after syncing validation-tool source budget evidence.
- Command: `dotnet run --project tools/VerifyAll/VerifyAll.csproj --no-restore`
  Result: pass
  Evidence: VerifyAll passed 23/23, including build, SimReplay, CombatBehavior, PlayerLoopQa, ReviewGate, PerfSmoke, and Godot headless QA.

Manual/visual gates:
- Check: Visual QA
  Result: not applicable
  Evidence: Command subject allocation refactor only; no rendering or UI layout changed.

Reviewer result:
- Status: pass
- Required fixes: none
- Residual risks: the reusable subject entity list follows existing immediate UnitBattlefield command bridge usage; delayed scheduling of these commands would require a snapshot contract.

TODO update:
- Items marked done: none; M9 per-tick allocation paydown remains open for broader profiler-guided cleanup.
- Items left open: selected-building rally, production option, queue summary, and projection allocation paths.
- Reason: This closes only the UnitBattlefield construction subject allocation child slice.
