# Review Record - Camera smoothing and view-change notifications

Step:
Make camera panning/zooming frame-rate-independent and notify dependent culling
only when the actual camera rect changes.

Milestone:
M6 Performance.

Owner AI:
Codex main agent.

Reviewer AI:
Codex main-agent self-review; independent reviewer was not spawned because the
current thread reached the subagent limit.

Integrator AI:
Codex main agent.

Scope:
- Files/folders:
  - `scripts/controllers/CameraController.cs`
  - `scripts/core/CameraInputMath.cs`
  - `tools/ReviewGate/Program.cs`
  - `TODO.md`
  - `docs/reviews/2026-06-29-camera-smoothing.md`
- Non-goals:
  - Do not add frame cap/vsync settings in this slice.
  - Do not rewrite minimap or fog rendering.
  - Do not tune unit movement/pathing.
  - Do not mark camera TODO items complete before runtime feel QA.

Implementation summary:
- Added `CameraInputMath.ExponentialSmoothingFactor()` using `1 - exp(-k*dt)`.
- `CameraController` now tracks `_targetPosition` and `_targetZoom` separately from
  actual `Position`/`Zoom`.
- WASD/edge-scroll move the target; actual camera position and zoom smooth toward it.
- `FocusOnWorldPoint()` now changes the target for damped minimap/camera jumps.
- `ViewChanged` fires when actual position/zoom changes, letting culling follow real
  camera-rect motion instead of only zoom events.
- Added `ReviewGate camera` to verify the smoothing and notification hooks.

Automated gates:
- Command:
  `dotnet build ProceduralRts.csproj`
  Result:
  Pass.
  Evidence:
  Build completed with 0 warnings and 0 errors.
- Command:
  `dotnet run --project tools/ReviewGate/ReviewGate.csproj camera`
  Result:
  Pass.
  Evidence:
  ReviewGate reported 0 errors and 0 warnings.
- Command:
  `dotnet run --project tools/ReviewGate/ReviewGate.csproj review --require-record=camera-smoothing`
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

Manual/visual gates:
- Check:
  Runtime camera feel at multiple frame rates.
  Result:
  Not run.
  Evidence:
  Required before marking the camera TODO complete.

Reviewer result:
- Status: pass-with-warnings
- Required fixes:
  - None for this bounded source-level slice.
- Residual risks:
  - Independent reviewer was not available due to subagent limit.
  - Runtime feel may need responsiveness tuning.
  - Damped minimap jumps are intentionally less instant; UX may need a snap option.
  - Fog/minimap refresh intervals are unchanged in this slice.

TODO update:
- Items marked done:
  - None; camera TODO items remain open pending runtime feel QA.
- Items left open:
  - Frame cap/vsync settings.
  - Runtime check at 30/60/144 FPS.
  - Further throttling of dependent redraws beyond culling notifications.
- Reason:
  - Evidence proves the smoothing hooks and camera change notifications, but not
    perceptual feel across frame rates.
