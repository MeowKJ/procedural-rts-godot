# Review Record - M9 Production Enqueue Producer Scans

Step:
M9 production enqueue producer scan buffer reuse (#91)

Milestone:
M9 - Elegance & Decoupling

Owner AI:
Remote Linux Codex

Reviewer AI:
Remote Linux Codex

Integrator AI:
Remote Linux Codex

Scope:
- Files/folders: `scripts/core/units/runtime/UnitBattlefield.cs`, `scripts/core/units/runtime/battlefield/UnitBattlefield.ProductionRally.cs`, `scripts/core/units/runtime/battlefield/UnitBattlefield.EntityWorldSystems.cs`, `tools/ReviewGateDomains/UnitBattlefieldAllocationReviewGate.cs`, `TODO.md`, `docs/reviews/2026-07-01-file-size-discipline-gate.md`.
- Non-goals: no production balance, HUD layout, queue refund, or `BuildingTargetIds()` lifecycle changes.

Implementation summary:
- Added `_productionCandidateProducerIds` as reusable producer candidate storage on `UnitBattlefield`.
- Added explicit `CollectCandidateProducerIds(...)` overloads for legacy `ProductionKind` and UnitDesign `UnitSpec` production paths.
- Replaced production enqueue candidate `OrderBy(...).ThenBy(...).Select(...).FirstOrDefault()` chains with `LeastQueuedProducerId(...)`, preserving queue length priority and building-id tie-break behavior.
- Extended `ReviewGate simhot` UnitBattlefield allocation evidence so enqueue producer selection cannot return to ordered LINQ candidate chains.
- Synced validation-tool source budget evidence after adding ReviewGate domain checks.

Automated gates:
- Command: `dotnet build ProceduralRts.csproj --no-restore`
  Result: pass
  Evidence: Debug build succeeded with 0 errors; one Godot dll copy retry warning resolved during the build.
- Command: `dotnet run --project tools/PlayerLoopQa/PlayerLoopQa.csproj`
  Result: pass
  Evidence: PlayerLoopQa passed, including T1-T3 production, rally, selection, victory, and defeat coverage.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj simhot`
  Result: pass
  Evidence: ReviewGate simhot passed with 0 errors and 0 warnings.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj`
  Result: pass
  Evidence: Full ReviewGate passed with 0 errors and 0 warnings after syncing validation-tool source budget evidence.

Manual/visual gates:
- Check: Visual QA
  Result: not applicable
  Evidence: Production enqueue allocation refactor only; no rendering or UI layout changed.

Reviewer result:
- Status: pass
- Required fixes: none identified in the enqueue path.
- Residual risks: production option states and queue summary scans are separate follow-up slices (#92, #93).

TODO update:
- Items marked done: none; M9 per-tick allocation paydown remains open.
- Items left open: production queue summary, production option state scans, and broader profiler-guided allocation cleanup.
- Reason: This closes only the producer selection allocation child slice.
