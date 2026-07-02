# Review Record - M9 Selection Legacy Rally Line Buffers

Step: #170 `[M9] Reuse SelectionController legacy rally-line buffers`
Milestone: M9 - Elegance & Decoupling
Owner AI: Remote Linux Codex
Reviewer AI: ReviewGate presentation / SelectionStress / DesktopHudQa
Integrator AI: Remote Linux Codex

Scope:
- Reuse a legacy selected-building rally buffer instead of enumerating `State.SelectedBuildings()` in `SelectionController.CommandLines`.
- Add an explicit `CollectLegacyCommandLineBuildings(...)` scan over `State.Buildings` for selected player buildings with rally points.
- Extend `SelectionControllerAllocationReviewGate` so `ReviewGate presentation` forbids the draw-time `State.SelectedBuildings()` rally-line enumerable.
- Non-goals: changing rally-line visuals, runtime building rally projections, selection semantics, or the public `GameState.SelectedBuildings()` API.

Automated gates:
- Command: `dotnet build ProceduralRts.csproj --no-restore`
  Result: pass.
- Command: `dotnet run --project tools/SelectionStress/SelectionStress.csproj --no-restore`
  Result: pass.
- Command: `dotnet run --project tools/DesktopHudQa/DesktopHudQa.csproj --no-restore`
  Result: pass.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore -- presentation --max-warnings=0`
  Result: pass.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore -- review --require-record=m9-selection-legacy-rally-line-buffers`
  Result: pass.

Reviewer result:
- Status: pass.
- Required fixes: none known.

Status:
- pass

Residual risks:
- Runtime building rally projection allocations are already covered by the buffered `SelectedBuildingRallyProjections(...)` path and are not changed here.
- Parent #10 remains open for broader allocation paydown.

TODO update:
- Added #170 follow-up evidence under the open M9 per-tick allocation paydown item.
- Items marked done: none.
- Items left open: parent #10 broader allocation paydown.
