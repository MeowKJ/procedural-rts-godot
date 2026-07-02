# Review Record - M9 Selection Command Line Buffers

Step: #168 `[M9] Reuse SelectionController command-line draw buffers`
Milestone: M9 - Elegance & Decoupling
Owner AI: Remote Linux Codex
Reviewer AI: ReviewGate presentation / SelectionStress / DesktopHudQa
Integrator AI: Remote Linux Codex

Scope:
- Reuse a legacy command-line unit buffer instead of materializing `State.SelectedUnits().Where(...).ToList()`.
- Reuse a runtime command-line unit buffer instead of materializing `UnitBattlefield.SelectedUnits(...).Where(...).ToList()`.
- Reuse the command-line target marker dictionary and keep the same rounded world-position grouping semantics with a value-tuple key.
- Extend `SelectionControllerAllocationReviewGate` so `ReviewGate presentation` forbids the old command-line list and marker dictionary allocations.
- Non-goals: changing command-line style, marker pulse/grouping behavior, building rally lines, selection semantics, public selected-unit enumerable APIs, or broad UI polish.

Automated gates:
- Command: `dotnet build ProceduralRts.csproj --no-restore`
  Result: pass.
- Command: `dotnet run --project tools/SelectionStress/SelectionStress.csproj --no-restore`
  Result: pass.
- Command: `dotnet run --project tools/DesktopHudQa/DesktopHudQa.csproj --no-restore`
  Result: pass.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore -- presentation --max-warnings=0`
  Result: pass.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore -- review --require-record=m9-selection-command-line-buffers`
  Result: pass.

Reviewer result:
- Status: pass.
- Required fixes: none known.

Status:
- pass

Residual risks:
- Building rally projection allocation remains outside this slice.
- Parent #10 remains open for broader allocation paydown.

TODO update:
- Added #168 follow-up evidence under the open M9 per-tick allocation paydown item.
- Items marked done: none.
- Items left open: parent #10 broader allocation paydown.
