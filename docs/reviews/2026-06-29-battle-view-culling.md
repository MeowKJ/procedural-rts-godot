# Review Record - Battle entity view culling

Step:
Add camera-rect culling for battle entity/resource presentation views.

Milestone:
M6 Performance.

Owner AI:
Codex main agent.

Reviewer AI:
Pending reviewer subagent.

Integrator AI:
Codex main agent.

Scope:
- Files/folders:
  - `scripts/BattleRoot.cs`
  - `tools/ReviewGate/Program.cs`
  - `TODO.md`
  - `docs/reviews/2026-06-29-battle-view-culling.md`
- Non-goals:
  - Do not cull gameplay simulation.
  - Do not cull HUD, fog, selection, command lines, or combat effect layers in this
    slice.
  - Do not implement unit batching or VFX pooling.
  - Do not mark the full off-screen culling TODO complete.

Implementation summary:
- Added `BattleRoot.RefreshViewCulling()` as a centralized presentation culling pass.
- The pass uses `_camera.VisibleWorldRect().Grow(ViewCullingMargin)`.
- Building, legacy unit, UnitInstance, and resource field views are hidden and have
  `ProcessMode` set to `Disabled` when outside the padded camera rect.
- Visible views use `ProcessMode.Inherit` again, so their own throttled redraw logic
  resumes.
- Added `_resourceViews` tracking so resource field views participate in culling.
- Added `ReviewGate culling` to verify the centralized culling pass and covered view
  families.
- After reviewer feedback, active views sync their latest position/rotation before
  being re-enabled and request a redraw, preventing stale one-frame pop-in.
- Camera zoom changes now raise `CameraController.ViewChanged`, causing immediate
  culling refresh after zoom.

Automated gates:
- Command:
  `dotnet build ProceduralRts.csproj`
  Result:
  Pass.
  Evidence:
  Build completed with 0 warnings and 0 errors.
- Command:
  `dotnet run --project tools/ReviewGate/ReviewGate.csproj culling`
  Result:
  Pass.
  Evidence:
  ReviewGate reported 0 errors and 0 warnings.
- Command:
  `dotnet run --project tools/ReviewGate/ReviewGate.csproj`
  Result:
  Pass.
  Evidence:
  ReviewGate reported 0 errors and 0 warnings.
- Command:
  `dotnet run --project tools/ReviewGate/ReviewGate.csproj review --require-record=battle-view-culling`
  Result:
  Pass.
  Evidence:
  ReviewGate reported 0 errors and 0 warnings with this durable record present.

Manual/visual gates:
- Check:
  Independent Reviewer AI read-only audit.
  Result:
  Pass with warnings.
  Evidence:
  Reviewer AI found no permanent-return bug and confirmed dictionaries are maintained,
  but warned about stale transform on re-enable and zoom refresh delay. Both were
  fixed by syncing transforms before activation and refreshing culling on camera
  zoom changes.

Reviewer result:
- Status: pass-with-warnings
- Required fixes:
  - Sync view transforms before re-enabling culled views. Fixed.
  - Refresh culling immediately on camera zoom changes. Fixed.
- Residual risks:
  - Combat effects, command acknowledgements, selection overlays, fog, and footprints
    are not culled in this slice.
  - The pass is interval-based at 20Hz for ordinary camera movement; zoom and minimap
    jumps refresh immediately.
  - Visual QA in the Godot window has not been performed.

TODO update:
- Items marked done:
  - None; off-screen culling remains open because this slice covers entity/resource
    views only.
- Items left open:
  - Effect/overlay culling or pooling.
  - Unit batching / pooled VFX.
  - GridLayer cached texture/MultiMesh or visible rect.
- Reason:
  - Evidence proves a centralized culling pass exists for key battle views, but not
    the full culling and pooling TODO surface.
