# Review Record - M9 CommandSystem selection subject set

Step:
M9 CommandSystem selection subject set (#74)

Milestone:
M9 - Elegance & Decoupling

Owner AI:
Remote Linux Codex

Reviewer AI:
Remote Linux Codex

Integrator AI:
Remote Linux Codex

Scope:
- Files/folders: `scripts/core/sim/systems/CommandSystem.cs`, `scripts/core/sim/systems/command/CommandSystem.SubjectsSelection.cs`, `tools/ReviewGateDomains/CommandSystemAllocationReviewGate.cs`, `TODO.md`.
- Non-goals: no changes to scalar movement/combat semantics, harvest/repair orders, building selection UI, command buffer ordering, balance, visuals, or the full #10 closeout.

Implementation summary:
- Added `_selectionSubjectIds` to `CommandSystem`.
- Replaced `command.Subjects.Select(...).ToHashSet()` in `ApplySelection` with fill/contains/clear on the reusable set.
- Extended `CommandSystemAllocationReviewGate` so `ReviewGate simhot` rejects selection command `ToHashSet()` allocation returning.

Automated gates:
- Command: `dotnet build ProceduralRts.csproj --no-restore`
  Result: pass
  Evidence: Debug build succeeded with 0 warnings and 0 errors.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore -- simhot --max-warnings=0`
  Result: pass
  Evidence: ReviewGate passed with 0 errors and 0 warnings.
- Command: `dotnet run --project tools/SimReplay/SimReplay.csproj --no-restore`
  Result: pass
  Evidence: SimReplay PASSED; selection-adjacent command scenarios and full deterministic replay suite stayed green.
- Command: `dotnet run --project tools/CombatBehavior/CombatBehavior.csproj --no-restore`
  Result: pass
  Evidence: Combat behavior passed.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore`
  Result: pass
  Evidence: Full ReviewGate passed after this record was added.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore -- review --require-record=m9-command-selection-subject-set`
  Result: pass
  Evidence: ReviewGate found this durable review record.
- Command: `dotnet run --project tools/VerifyAll/VerifyAll.csproj --no-restore`
  Result: pass
  Evidence: Concentrated VerifyAll passed for #73/#74/#75 before final closeout.

Manual/visual gates:
- Check: Visual QA
  Result: not applicable
  Evidence: Simulation command allocation refactor only; no rendering or UI layout changed.

Reviewer result:
- Status: pass
- Required fixes: none
- Residual risks: Follow-up #75 economy order cleanup is complete; broad #10 remains open for profiler-guided allocation cleanup beyond CommandSystem.

TODO update:
- Items marked done: none; #10 remains open.
- Items left open: M9 per-tick allocation paydown.
- Reason: This only closes the selection-set allocation child slice.
