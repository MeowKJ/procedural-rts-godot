# Review Record - Display frame-rate settings

Step:
Persist and apply desktop frame-rate / VSync settings through the existing settings
system.

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
  - `scripts/core/FrameRateMode.cs`
  - `scripts/core/DisplayAudioSettings.cs`
  - `scripts/ui/SettingsOverlayLayer.cs`
  - `scripts/core/GameText.cs`
  - `scripts/DisplaySettingsQaRoot.cs`
  - `scenes/DisplaySettingsQa.tscn`
  - `tools/ReviewGate/Program.cs`
  - `TODO.md`
  - `docs/reviews/2026-06-29-display-frame-rate-settings.md`
- Non-goals:
  - Do not change `SimClock` or simulation authority.
  - Do not add a full runtime performance options panel beyond the existing settings
    overlay.
  - Do not measure real monitor frame pacing for every display mode in this slice.

Implementation summary:
- Added `FrameRateMode` with Off, VSync, Fps60, and Fps144.
- `DisplayAudioSettings` now loads/saves `display.frame_rate` in `settings.cfg`.
- `ApplyFrameRateMode()` controls `DisplayServer.WindowSetVsyncMode`,
  `Engine.MaxFps`, and intentionally sets `Engine.PhysicsTicksPerSecond = 60`.
- `SettingsOverlayLayer` exposes the frame-rate mode with localized labels and
  applies changes immediately.
- `GameText` now includes English and zh-CN strings for frame-rate controls.
- Added `ReviewGate display` to verify persistence, apply hooks, and settings UI.
- Added a headless Godot QA scene that applies Off/VSync/60/144 and asserts the
  selected mode, `Engine.MaxFps`, and `Engine.PhysicsTicksPerSecond`.

Automated gates:
- Command:
  `dotnet build ProceduralRts.csproj --no-restore`
  Result:
  Pass.
  Evidence:
  Build completed with 0 warnings and 0 errors.
- Command:
  `dotnet run --project tools/ReviewGate/ReviewGate.csproj display`
  Result:
  Pass.
  Evidence:
  ReviewGate reported 0 errors and 0 warnings.
- Command:
  `Godot_v4.7-stable_mono_win64_console.exe --headless --path . --scene res://scenes/DisplaySettingsQa.tscn`
  Result:
  Pass.
  Evidence:
  Godot printed `Display settings QA passed: Off/VSync/60/144 apply MaxFps and
  physics ticks.`
- Command:
  `dotnet run --project tools/ReviewGate/ReviewGate.csproj review --require-record=display-frame-rate-settings`
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
  Settings runtime apply QA.
  Result:
  Pass.
  Evidence:
  The headless Godot QA scene applied all four modes and checked the runtime
  engine cap/tick values.

Reviewer result:
- Status: pass-with-warnings
- Required fixes:
  - None for this bounded source-level slice.
- Residual risks:
  - Independent reviewer was not available due to subagent limit.
  - Settings overlay layout has not been visually checked after adding the new row.
  - Real VSync frame pacing depends on platform/window manager behavior and was not
    measured in this slice.

TODO update:
- Items marked done:
  - Frame cap / vsync setting persisted in `DisplayAudioSettings`.
  - Intentional `Engine.MaxFps` / `PhysicsTicksPerSecond` settings while sim
    authority remains on `SimClock`.
- Items left open:
  - Optional future measurement that selected cap produces expected frame pacing.
- Reason:
  - Evidence now proves persisted/apply/UI source hooks plus runtime application of
    all four modes in Godot headless.
