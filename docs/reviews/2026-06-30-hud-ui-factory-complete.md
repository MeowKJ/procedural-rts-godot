# Review Record - HUD UiFactory Completion

Step:
Close the shared UiFactory extraction for MainMenu, Settings, Pause, Outcome, and
Hud surfaces.

Milestone:
M7 UI & Presentation Polish.

Owner AI:
Worker-M7.

Reviewer AI:
Integrator gate review via `ReviewGate huduifactory`.

Integrator AI:
Codex main thread.

Scope:
- Files/folders:
  - `scripts/ui/UiFactory.cs`
  - `scripts/ui/HudLayer.cs`
  - `tools/DesktopHudQa/Program.cs`
  - `tools/ReviewGate/Program.cs`
  - `TODO.md`
- Non-goals:
  - No build/production UX redesign.
  - No layout redesign.
  - No new visual assets.

Implementation summary:
- Moved remaining HUD panel, label, action button, command button, move/stance,
  control-group, and command-overlay style resolution into `UiFactory`.
- Kept HUD layout and gameplay state wiring in `HudLayer`.
- Extended `DesktopHudQa` with static source checks proving HUD style calls stay
  routed through `UiFactory`.

Automated gates:
- Command:
  `dotnet run --project tools/DesktopHudQa/DesktopHudQa.csproj --no-restore`
  Result:
  Pass.
  Evidence:
  Desktop layout constraints pass and HUD UiFactory extraction is statically
  checked.
- Command:
  `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore -- huduifactory`
  Result:
  Pass.
  Evidence:
  Gate verifies UiFactory owns HUD styling helpers and HudLayer no longer writes
  direct panel styleboxes.

Reviewer result:
Pass. Shared styling is centralized without changing the HUD's behavior surface.

Status:
Pass.

Residual risks:
- Build/production UI redesign remains open as a separate M7 item.

TODO update:
- Marked done: shared `UiTheme`/`UiFactory` extraction from MainMenu, Settings,
  Pause, Outcome, and Hud.
