# Review Record - Owner color accessibility

Step:
Add a colorblind-safe owner-color palette option.

Milestone:
Engineering conventions / Accessibility.

Owner AI:
Codex main agent.

Reviewer AI:
Codex main-agent review with `ReviewGate accessibility`, `CombatBehavior`, and
Godot display settings QA.

Integrator AI:
Codex main agent.

Scope:
- Files/folders:
  - `scripts/core/OwnerColorPaletteMode.cs`
  - `scripts/core/SoftOldCityPalette.cs`
  - `scripts/core/DisplayAudioSettings.cs`
  - `scripts/ui/SettingsOverlayLayer.cs`
  - `scripts/core/GameText.cs`
  - `scripts/DisplaySettingsQaRoot.cs`
  - `tools/CombatBehavior/Program.cs`
  - `tools/ReviewGate/Program.cs`
  - `TODO.md`
  - `docs/reviews/2026-06-29-owner-color-accessibility.md`
- Non-goals:
  - Do not redesign faction colors or relation overlays.
  - Do not add a full accessibility menu.
  - Do not tune every decorative debug/VFX color.
  - Do not change the rule that owner color is the body ownership signal.

Implementation summary:
- Added `OwnerColorPaletteMode` with `Standard` and `ColorblindSafe`.
- Added colorblind-safe owner colors to `SoftOldCityPalette` and routed
  `PlayerColor(PlayerSlotId)` through `DisplayAudioSettings.OwnerColors`.
- Added persisted `ui.owner_colors` to `DisplayAudioSettings`.
- Added an owner-color palette selector to `SettingsOverlayLayer`.
- Added English `GameText` keys for owner color settings.
- Extended `DisplaySettingsQaRoot` and `CombatBehavior` to verify the selectable
  colorblind-safe palette and minimum separation between safe owner colors.
- Added `ReviewGate accessibility`.

Automated gates:
- Command:
  `dotnet build ProceduralRts.csproj --no-restore`
  Result:
  Pass.
  Evidence:
  Build completed with 0 warnings and 0 errors.
- Command:
  `dotnet run --project tools/ReviewGate/ReviewGate.csproj accessibility --no-restore`
  Result:
  Pass.
  Evidence:
  ReviewGate reported 0 errors and 0 warnings.
- Command:
  `dotnet run --project tools/CombatBehavior/CombatBehavior.csproj --no-restore`
  Result:
  Pass.
  Evidence:
  CombatBehavior passed after checking colorblind-safe owner color separation.
- Command:
  `Godot_v4.7-stable_mono_win64_console.exe --headless --path . --scene res://scenes/DisplaySettingsQa.tscn`
  Result:
  Pass.
  Evidence:
  Display settings QA passed frame-rate and owner color palette mode checks.
- Command:
  `dotnet run --project tools/ReviewGate/ReviewGate.csproj review --require-record=owner-color-accessibility --no-restore`
  Result:
  Pass.
  Evidence:
  ReviewGate reported 0 errors and 0 warnings.
- Command:
  `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore`
  Result:
  Pass.
  Evidence:
  ReviewGate reported 0 errors and 0 warnings.
- Command:
  `dotnet run --project tools/VerifyAll/VerifyAll.csproj --no-restore`
  Result:
  Pass.
  Evidence:
  VerifyAll passed all 14 steps: build, SimReplay, CombatBehavior,
  SimulationSmoke, FogOfWarQa, SelectionStress, AiDifficultySmoke, ReviewGate,
  PerfSmoke, BalanceReport, and Godot headless QA scenes.

Manual/visual gates:
- Check:
  In-engine screenshot comparison of standard vs colorblind-safe owner colors.
  Result:
  Not run.
  Evidence:
  This slice adds the functional option and automated separation checks; final visual
  tuning may still adjust exact colors.

Reviewer result:
- Status: pass.
- Required fixes:
  - None at record creation.
- Residual risks:
  - The settings panel layout should still be visually reviewed after adding a row.
  - Exact safe palette values may be tuned later if screenshots show fatigue or poor
    contrast in night/fog themes.

TODO update:
- Items marked done:
  - `Accessibility: owner colors must be colorblind-distinguishable`.
- Items left open:
  - Broader UI factory extraction.
  - Final screenshot tuning of Soft Old City surfaces.
- Reason:
  - Owner color can now be switched to a colorblind-safe palette through persisted
    settings, and automated gates cover the setting, render source, and color
    separation.
