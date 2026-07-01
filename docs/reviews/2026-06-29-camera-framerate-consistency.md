# Review Record - Camera framerate consistency

Step:
Close the camera smoothing TODO by proving frame-rate-independent pan/zoom
integration at 30/60/144Hz.

Milestone:
M6 Performance.

Owner AI:
Codex main agent.

Reviewer AI:
Codex main-agent self-review; independent reviewer was not spawned because the
current thread has been operating at the subagent limit.

Integrator AI:
Codex main agent.

Scope:
- Files/folders:
  - `scripts/core/CameraInputMath.cs`
  - `scripts/controllers/CameraController.cs`
  - `tools/SelectionStress/Program.cs`
  - `tools/ReviewGate/Program.cs`
  - `TODO.md`
  - `docs/reviews/2026-06-29-camera-framerate-consistency.md`
- Non-goals:
  - Do not retune camera speeds.
  - Do not add new camera controls.
  - Do not visually tune panning feel in a live window.

Implementation summary:
- Added `CameraInputMath.SmoothToward` helpers for scalar and 2D exponential
  smoothing using `1 - exp(-k * dt)`.
- `CameraController` now uses the shared smoothing helper for pan and zoom.
- `SelectionStress` simulates identical camera smoothing at 30, 60, and 144Hz and
  verifies the final pan/zoom values match within a tiny tolerance.
- `ReviewGate camera` now requires the shared helper and the 30/60/144Hz tests.

Automated gates:
- Command:
  `dotnet build ProceduralRts.csproj --no-restore`
  Result:
  Pass.
  Evidence:
  Build completed with 0 warnings and 0 errors.
- Command:
  `dotnet run --project tools/SelectionStress/SelectionStress.csproj --no-restore`
  Result:
  Pass.
  Evidence:
  Selection stress reported `Selection stress passed: 80 cases`.
- Command:
  `dotnet run --project tools/ReviewGate/ReviewGate.csproj camera`
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
  `Godot_v4.7-stable_mono_win64_console.exe --headless --path . --scene res://scenes/Battle.tscn --quit-after 2`
  Result:
  Pass.
  Evidence:
  Battle scene started and exited cleanly.

Manual/visual gates:
- Check:
  Visible camera feel at 30/60/144 FPS.
  Result:
  Not run.
  Evidence:
  Pure integration behavior is covered by deterministic math tests; live-window
  feel tuning remains optional if the camera subjectively feels too stiff/loose.

Reviewer result:
- Status: pass-with-warnings
- Required fixes:
  - None for this bounded camera consistency slice.
- Residual risks:
  - No visible-window feel pass was performed.

TODO update:
- Items marked done:
  - Frame-rate-independent camera smoothing.
  - Damped camera/minimap jumps and actual view-change notifications.
- Items left open:
  - Frame-rate settings runtime options-menu QA.
- Reason:
  - The TODO's required 30/60/144Hz consistency is now covered by automated math
    tests and the in-engine scene still starts cleanly.
