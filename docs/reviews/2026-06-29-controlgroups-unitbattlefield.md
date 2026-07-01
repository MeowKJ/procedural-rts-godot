# Review Record - Control groups UnitBattlefield route

Step:
Route control-group save/recall/HUD snapshots through the live UnitBattlefield
selection path and add double-tap recall camera focus.

Milestone:
Design Reference - Controls & Command Feel.

Owner AI:
Codex main agent.

Reviewer AI:
Codex main-agent self-review; independent reviewer was not spawned because the
current thread is at the subagent limit.

Integrator AI:
Codex main agent.

Scope:
- Files/folders:
  - `scripts/controllers/ControlGroupController.cs`
  - `scripts/BattleRoot.cs`
  - `tools/ReviewGate/Program.cs`
  - `TODO.md`
  - `docs/reviews/2026-06-29-controlgroups-unitbattlefield.md`
- Non-goals:
  - Do not claim final EntityWorld authoritative selection.
  - Do not add shift-queued orders or command-buffer-only input.

Implementation summary:
- `ControlGroupController` now accepts `UnitBattlefield` and `LocalPlayerSlotId`.
- Control-group save uses `UnitBattlefield.SelectedUnits(LocalPlayerSlotId)` when
  the live battlefield exists.
- Recall uses `UnitBattlefield.SelectUnitsByIds(LocalPlayerSlotId, ids)` and clears
  legacy `GameState` selection to avoid mixed building/unit selection state.
- Double-tap recall computes the live group center and routes `FocusRequested` to
  `CameraController.FocusOnWorldPoint` through `BattleRoot`.
- HUD snapshots are built from live UnitBattlefield units and authored role tags;
  economy units are counted separately from combat vehicles to avoid duplicate
  totals.
- `BattleRoot` wires `_unitBattlefield` and `PlayerSlotId.One` into the controller.
- `ReviewGate controlgroups` verifies the routing hooks.

Automated gates:
- Command:
  `dotnet run --project tools/ReviewGate/ReviewGate.csproj controlgroups`
  Result:
  Pass.
  Evidence:
  ReviewGate reported 0 errors and 0 warnings.
- Command:
  `dotnet build ProceduralRts.csproj --no-restore`
  Result:
  Pass.
  Evidence:
  Build completed with 0 warnings and 0 errors.

Manual/visual gates:
- Check:
  Runtime visual control-group HUD check.
  Result:
  Not run.
  Evidence:
  This slice changes routing and keeps the existing `ControlGroupSnapshot` HUD
  shape; visual QA remains useful when double-tap centering is added.

Reviewer result:
- Status: pass-with-warnings
- Required fixes:
  - None for this bounded routing slice.
- Residual risks:
  - This is still the UnitBattlefield bridge, not final EntityWorld authoritative
    command-buffer selection.

TODO update:
- Items marked done:
  - None.
- Items left open:
  - `Control groups: Ctrl+1-9 assign, 1-9 recall, double-tap recall+center`.
- Reason:
  - Save/recall/double-tap focus are implemented on the live UnitBattlefield path,
    but the full TODO acceptance still requires final EntityWorld selection.
