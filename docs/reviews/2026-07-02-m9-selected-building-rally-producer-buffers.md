# Review Record - M9 Selected Building Rally Producer Buffers

Step:
M9 selected building rally producer buffer reuse (#90)

Milestone:
M9 - Elegance & Decoupling

Owner AI:
Remote Linux Codex

Reviewer AI:
Remote Linux Codex

Integrator AI:
Remote Linux Codex

Scope:
- Files/folders: `scripts/core/units/runtime/UnitBattlefield.cs`, `scripts/core/units/runtime/battlefield/UnitBattlefield.ProductionRally.cs`, `tools/ReviewGateDomains/UnitBattlefieldAllocationReviewGate.cs`, `TODO.md`.
- Non-goals: no rally target clamp, resource-field target entity, status text, HUD rally rendering, production option, or production queue summary changes.

Implementation summary:
- Added `_selectedBuildingRallyProducerIds` to reuse selected producer-id storage for building rally commands.
- Replaced both `SetSelectedBuildingRallyPoints(...)` overloads' selected/producers LINQ `ToList()` materialization with explicit selected-building scans and in-place producer-id sorting.
- Preserved the prior status distinction between no selected buildings and selected buildings without production support.
- Extended `ReviewGate simhot` UnitBattlefield allocation evidence so the selected-building rally path cannot return to selected/producers list materialization.

Automated gates:
- Command: `dotnet build ProceduralRts.csproj --no-restore`
  Result: pass
  Evidence: Debug build succeeded with 0 warnings and 0 errors after splitting the selected-rally helper from `UnitBattlefield.ProductionRally.cs`.
- Command: `dotnet run --project tools/CombatBehavior/CombatBehavior.csproj --no-restore`
  Result: pass
  Evidence: CombatBehavior passed, including rally production and presentation descriptor coverage.
- Command: `dotnet run --project tools/PlayerLoopQa/PlayerLoopQa.csproj --no-restore`
  Result: pass
  Evidence: PlayerLoopQa passed, including rally, production, selection, victory, and defeat coverage.
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
  Evidence: Command bridge allocation refactor only; no rendering or UI layout changed.

Reviewer result:
- Status: pass
- Required fixes: none
- Residual risks: production option states and queue summaries still contain separate LINQ materialization paths outside this selected-building rally slice.

TODO update:
- Items marked done: none; M9 per-tick allocation paydown remains open for broader profiler-guided cleanup.
- Items left open: production option, queue summary, and projection allocation paths.
- Reason: This closes only the selected-building rally producer allocation child slice.
