# Review Record - M9 UnitBattlefield Selection Command Entity Buffer

Step:
M9 UnitBattlefield selection command entity buffer reuse (#88)

Milestone:
M9 - Elegance & Decoupling

Owner AI:
Remote Linux Codex

Reviewer AI:
Remote Linux Codex

Integrator AI:
Remote Linux Codex

Scope:
- Files/folders: `scripts/core/units/runtime/UnitBattlefield.cs`, `scripts/core/units/runtime/battlefield/UnitBattlefield.CommandBridge.cs`, `tools/ReviewGateDomains/UnitBattlefieldSelectionAllocationReviewGate.cs`, `TODO.md`.
- Non-goals: no selection UX, additive selection, box selection, same-unit selection, or `CommandSystem` behavior changes.

Implementation summary:
- Added `_selectionCommandEntityBuffer` to reuse the sorted selection command subject list.
- Replaced the `SubmitSelectionCommand(...)` LINQ `Where/Distinct/OrderBy/ToList` chain with explicit valid-id collection, duplicate scanning, and in-place sort.
- Extended `ReviewGate simhot` selection allocation evidence so the selection command path cannot return to LINQ materialization.

Automated gates:
- Command: `dotnet build ProceduralRts.csproj --no-restore`
  Result: pass
  Evidence: Debug build succeeded with 0 warnings and 0 errors.
- Command: `dotnet run --project tools/SelectionStress/SelectionStress.csproj --no-restore`
  Result: pass
  Evidence: Selection stress passed 100 cases.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore -- simhot --max-warnings=0`
  Result: pass
  Evidence: ReviewGate simhot passed with 0 errors and 0 warnings.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore`
  Result: pass
  Evidence: Full ReviewGate passed with 0 errors and 0 warnings after syncing validation-tool source budget evidence.
- Command: `dotnet run --project tools/VerifyAll/VerifyAll.csproj --no-restore`
  Result: pending
  Evidence: pending batch-level verification before issue closeout.

Manual/visual gates:
- Check: Visual QA
  Result: not applicable
  Evidence: Command subject allocation refactor only; no rendering or UI layout changed.

Reviewer result:
- Status: pass
- Required fixes: none
- Residual risks: the command stores a caller-owned list during immediate command-buffer drain, matching existing UnitBattlefield group-command buffer usage; no delayed command scheduling is introduced.

TODO update:
- Items marked done: none; M9 per-tick allocation paydown remains open for broader profiler-guided cleanup.
- Items left open: construction subject, selected-building rally, production option, and projection allocation paths.
- Reason: This closes only the selection command subject allocation child slice.
