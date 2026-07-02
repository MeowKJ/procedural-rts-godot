# Review Record - M9 Runtime Harvester Selection Scan

Step: #185 `[M9] Replace runtime harvester selection iterator`
Milestone: M9 - Elegance & Decoupling
Owner AI: Remote Linux Codex
Reviewer AI: ReviewGate presentation / SelectionControllerAllocationReviewGate
Integrator AI: Remote Linux Codex

Scope:
- Files/folders: `scripts/controllers/SelectionController.Utilities.cs`, `tools/ReviewGateRuntime/SelectionControllerAllocationReviewGate.cs`.
- Non-goals: 不改变 command preview、right-click command、harvest command、selection rules、UI visual behavior、或 `UnitBattlefield.SelectedUnits(...)` public compatibility API。

Implementation summary:
- `HasSelectedRuntimeHarvester()` now scans `UnitBattlefield.Units` directly.
- The scan preserves the existing `LocalPlayerSlotId`, selected-state, and harvester spec checks.
- `ReviewGate presentation` locks the helper against returning to `UnitBattlefield.SelectedUnits(...)` enumeration.

Automated gates:
- Command: `dotnet build ProceduralRts.csproj --no-restore`
  Result: pass，0 warnings / 0 errors。
- Command: `dotnet run --project tools/SelectionStress/SelectionStress.csproj --no-restore`
  Result: pass，100 cases。
- Command: `dotnet run --project tools/DesktopHudQa/DesktopHudQa.csproj --no-restore`
  Result: pass，1280x720 / 1600x900 / 1920x1080 / high-DPI HUD constraints。
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore -- presentation --max-warnings=0`
  Result: pass，0 errors / 0 warnings。
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore -- filesize --max-warnings=0`
  Result: pass，0 errors / 0 warnings；ReviewGateRuntime suite remains at budget。
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore -- review --require-record=m9-runtime-harvester-selection-scan`
  Result: pass，0 errors / 0 warnings。
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore`
  Result: pass，0 errors / 0 warnings。

Reviewer result:
- Status: pass.
- Required fixes: none currently known.

Residual risks:
- `UnitBattlefield.SelectedUnits(...)` remains a LINQ-backed compatibility API for non-hot callers; this slice only removes the runtime harvester preview caller.
- Parent #10 remains open for broader allocation paydown.

TODO update:
- Items marked done: none.
- Items left open: broader M9 per-tick allocation paydown remains open under #10.
