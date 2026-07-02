# Review Record - M9 UnitBattlefield Selected Count Scan

Step: #163 `[M9] Replace UnitBattlefield selected count LINQ`
Milestone: M9 - Elegance & Decoupling
Owner AI: Remote Linux Codex
Reviewer AI: ReviewGate simhot / SelectionStress
Integrator AI: Remote Linux Codex

Scope:
- Replace `UnitBattlefield.SelectedCount(PlayerSlotId)` `SelectedUnits(playerSlotId).Count()` with an explicit scan over `Units`.
- Preserve the public `SelectedUnits(PlayerSlotId)` enumerable API for existing callers.
- Extend `UnitBattlefieldSelectionAllocationReviewGate` so `ReviewGate simhot` forbids the old selected-count LINQ path.
- Non-goals: changing selection command semantics, selection buffers, HUD visuals, control groups, legacy `GameState` selection, or closing parent #10.

Automated gates:
- Command: `dotnet build ProceduralRts.csproj --no-restore`
  Result: pass.
- Command: `dotnet run --project tools/SelectionStress/SelectionStress.csproj --no-restore`
  Result: pass.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore -- simhot --max-warnings=0`
  Result: pass.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore -- review --require-record=m9-unitbattlefield-selected-count-scan`
  Result: pass.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore`
  Result: pass.

Reviewer result:
- Status: pass.
- Required fixes: none known.

Status:
- pass

Residual risks:
- `SelectedUnits(...)` remains an enumerable API for current callers; this slice only removes the hot selected-count LINQ path.
- Parent #10 remains open for broader allocation paydown.

TODO update:
- Added #163 follow-up evidence under the open M9 per-tick allocation paydown item.
- Items marked done: none.
- Items left open: parent #10 broader allocation paydown.
