# Review Record - Worker B Camera And Fog Perf

Step:
Reduce camera hitch feel and avoid unnecessary fog visual redraw/upload work.

Milestone:
Playable 1v1 skirmish vertical slice - performance and fog responsiveness.

Owner AI:
Worker B.

Reviewer AI:
Integrator code review plus automated gates.

Integrator AI:
Main Codex thread.

Scope:
- Files/folders:
  - `scripts/core/CameraInputMath.cs`
  - `scripts/controllers/CameraController.cs`
  - `scripts/core/FogOfWarMap.cs`
  - `scripts/world/FogOfWarLayer.cs`
  - `tools/FogOfWarQa/Program.cs`
  - `docs/reviews/2026-06-30-worker-b-camera-fog-perf.md`
- Non-goals:
  - No TODO checkbox update in this worker slice.
  - No VerifyAll or ReviewGate wiring changes.
  - No gameplay visibility authority changes.

Implementation summary:
- Camera pan/zoom now clamps visual delta through `CameraInputMath.StableVisualDelta`
  so slow frames do not create a large one-frame camera target jump.
- `FogOfWarMap` exposes `HasPendingMaskTextureUpload(Rect2?)`, allowing the visual
  layer to ask whether the current camera area actually needs a mask upload.
- `FogOfWarLayer` still queues redraws promptly for new visible fog revisions, but
  suppresses camera-only redraws when the current view has no dirty mask cells.

Automated gates:
- Command:
  `dotnet build ProceduralRts.csproj --no-restore`
  Result:
  Pass.
  Evidence:
  Build passed with 0 warnings and 0 errors.
- Command:
  `dotnet run --project tools/FogOfWarQa/FogOfWarQa.csproj --no-restore`
  Result:
  Pass.
  Evidence:
  Fog QA passed mask channels, feathered edges, explored memory, hidden enemies,
  static memory, camera-scoped texture updates, and 100-source unchanged-source
  performance smoke.
- Command:
  `dotnet run --project tools/PerfSmoke/PerfSmoke.csproj -c Release --no-restore`
  Result:
  Pass.
  Evidence:
  PerfSmoke passed; local integrator run reported worst average 1.493ms at 400
  units, under the 16.667ms budget.

Manual/visual gates:
- Check:
  Desktop pan/zoom feel and fog catch-up.
  Result:
  Pending manual playtest.
  Evidence:
  No interactive desktop playtest was run in this integration pass.

Reviewer result:
- Status: pass
- Required fixes:
  - Review record was expanded to the required protocol fields.
- Residual risks:
  - Very long frame hitches intentionally move the camera less in one frame, which
    should feel smoother but needs desktop feel testing.
  - The fog redraw gate optimizes visual uploads only; it does not by itself prove
    full 1080p render FPS under all scenes.

TODO update:
- Items marked done:
  - None.
- Items left open:
  - The broad 60 FPS / 1080p TODO remains open until a full active-base visual
    performance gate and manual camera check are recorded.

Gate tag:
camera-fog-perf
