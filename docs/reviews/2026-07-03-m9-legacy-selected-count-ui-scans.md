# Review Record - M9 Legacy Selected Count UI Scans

Step: #167 `[M9] Replace legacy selected-count UI LINQ`
Milestone: M9 - Elegance & Decoupling
Owner AI: Remote Linux Codex
Reviewer AI: ReviewGate presentation / SelectionStress / DesktopHudQa
Integrator AI: Remote Linux Codex

Scope:
- Expose `GameState.SelectedUnitCount()` as the explicit selected-unit scan helper.
- Route `BattleRoot.Events` stance fallback through `SelectedUnitCount()` instead of `SelectedUnits().Count()`.
- Route `SelectionController.Hotkeys` and `SelectionController.Hover` legacy selected-unit readouts through `SelectedUnitCount()`.
- Extend presentation allocation ReviewGate coverage so these UI call sites cannot return to LINQ `Count()`.
- Non-goals: changing selection semantics, stance command behavior, hover visuals, runtime `UnitBattlefield.SelectedCount(...)`, removing `SelectedUnits()`, or handling command-line preview materialization.

Automated gates:
- Command: `dotnet build ProceduralRts.csproj --no-restore`
  Result: pass.
- Command: `dotnet run --project tools/SelectionStress/SelectionStress.csproj --no-restore`
  Result: pass.
- Command: `dotnet run --project tools/DesktopHudQa/DesktopHudQa.csproj --no-restore`
  Result: pass.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore -- presentation --max-warnings=0`
  Result: pass.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore -- review --require-record=m9-legacy-selected-count-ui-scans`
  Result: pass.

Reviewer result:
- Status: pass.
- Required fixes: none known.

Status:
- pass

Residual risks:
- `SelectedUnits()` remains a public enumerable API for callers that need enumeration.
- Command-line preview list materialization remains a separate child slice.
- Parent #10 remains open for broader allocation paydown.

TODO update:
- Added #167 follow-up evidence under the open M9 per-tick allocation paydown item.
- Items marked done: none.
- Items left open: parent #10 broader allocation paydown.
