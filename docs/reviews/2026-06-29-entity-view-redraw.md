# Review Record - Entity view redraw throttling

Step:
Reduce battle entity view redraw warnings for buildings and unit views.

Milestone:
M6 Performance.

Owner AI:
Codex main agent.

Reviewer AI:
Popper subagent (`019f100e-4c8e-7d11-bc1a-fbc484e4609d`).

Integrator AI:
Codex main agent.

Scope:
- Files/folders:
  - `scripts/world/BuildingView.cs`
  - `scripts/world/UnitInstanceView.cs`
  - `scripts/world/UnitView.cs`
  - `TODO.md`
  - `docs/reviews/2026-06-29-entity-view-redraw.md`
- Non-goals:
  - Do not change gameplay authority or UnitBattlefield behavior.
  - Do not batch units into MultiMesh/atlas in this slice.
  - Do not mark the full redraw/culling TODO complete.
  - Do not tune menu/showcase roots in this battle-performance slice.

Implementation summary:
- `BuildingView` keeps position and rotation synchronized every frame, but redraws
  vector art at 20Hz.
- `UnitInstanceView` keeps position synchronized every frame, but redraws vector art
  at 30Hz.
- Legacy `UnitView` keeps position synchronized every frame, but redraws vector art
  at 30Hz.
- `ReviewGate presentation` dropped from 9 warnings to 6 warnings. Remaining warnings
  are menu/showcase roots, not battle entity views.

Automated gates:
- Command:
  `dotnet build ProceduralRts.csproj`
  Result:
  Pass.
  Evidence:
  Build completed with 0 warnings and 0 errors.
- Command:
  `dotnet run --project tools/ReviewGate/ReviewGate.csproj presentation`
  Result:
  Pass with warnings.
  Evidence:
  ReviewGate reported 0 errors and 6 remaining redraw warnings. BuildingView,
  UnitInstanceView, and UnitView are no longer listed.
- Command:
  `dotnet run --project tools/ReviewGate/ReviewGate.csproj presentation --max-warnings=6`
  Result:
  Pass.
  Evidence:
  ReviewGate reported 0 errors and 6 warnings, matching the new presentation
  baseline.
- Command:
  `dotnet run --project tools/ReviewGate/ReviewGate.csproj review --require-record=entity-view-redraw`
  Result:
  Pass.
  Evidence:
  ReviewGate reported 0 errors and 0 warnings with this durable record present.

Manual/visual gates:
- Check:
  Independent Reviewer AI read-only audit.
  Result:
  Pass.
  Evidence:
  Reviewer AI found no blocking issue. Position remains updated every frame in all
  three views; only vector redraw is throttled. Reviewer accepted 30Hz unit-facing
  and 20Hz building production feedback as low risk for the current RTS scale.

Reviewer result:
- Status: pass
- Required fixes:
  - Preserve TODO audit history from 9 warnings to 6 warnings. Fixed.
- Residual risks:
  - Visual QA in the Godot window has not been performed.
  - Unit facing and turret facing are drawn inside `_Draw`, so their visual rotation
    updates at 30Hz even though position updates every frame.
  - Building production bars and hit/selection pulses update at 20Hz.
  - Full batching/culling remains open TODO work.

TODO update:
- Items marked done:
  - None; the broad redraw/culling TODO remains open.
- Items left open:
  - Menu/showcase redraw warnings.
  - Off-screen culling.
  - Unit batching or dirty snapshot rendering.
- Reason:
  - Evidence proves battle entity view redraw warnings were removed, but the full
    M6 presentation performance item is not complete.
