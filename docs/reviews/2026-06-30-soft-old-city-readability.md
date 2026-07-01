# Review Record - Soft Old City Readability

Step:
Close the broad Soft Old City readability TODO: clear Dog/Cat silhouettes, owner
color separate from faction identity, edge-docked low-obstruction HUD, and readable
day/fog/dusk battle views.

Milestone:
Playable 1v1 skirmish vertical slice - UI and presentation polish.

Owner AI:
Worker D plus integrator implementation.

Reviewer AI:
Integrator visual review plus `ReviewGate softoldcity`.

Integrator AI:
Codex main thread.

Scope:
- Files/folders:
  - `scripts/core/HudLayoutMath.cs`
  - `tools/DesktopHudQa/Program.cs`
  - `scripts/VisualQaCaptureRoot.cs`
  - `scripts/core/SoftOldCityPalette.cs`
  - `scripts/core/WorldThemeMath.cs`
  - `scripts/ui/SoftOldCityTheme.cs`
  - `scripts/world/UnitInstanceView.cs`
  - `scripts/world/BuildingView.cs`
  - `scripts/core/units/dog/DogUnitArt.cs`
  - `scripts/core/units/cat/CatUnitArt.cs`
  - `tools/VerifyAll/Program.cs`
  - `tools/ReviewGate/Program.cs`
  - `artifacts/visual-qa/*.png`
  - `docs/reviews/2026-06-30-soft-old-city-readability.md`
- Non-goals:
  - No new unit roster or gameplay balance work.
  - No bitmap or SVG unit assets.
  - No 200+ unit batching target.
  - No full build/production UI redesign.

Implementation summary:
- Added `desktop-hud-qa` to `VerifyAll` so HUD layout coverage is part of the
  durable gate.
- Added `ReviewGate softoldcity`, which locks the combined contract for HUD
  low-obstruction math, visual QA captures, canonical palette usage, environment
  tone usage, owner-color art layers, and Dog/Cat shape-language hints.
- Reused the existing Dog/Cat procedural art direction: Dog bodies are heavier
  with tracked/repair/shield/air-patrol hints; Cat bodies are slimmer with
  quiet-step/glide-track/air-hover hints. Both use `ColorRole.Owner` stripes and
  badges as normal art layers rather than a separate decal system.
- Regenerated Visual QA screenshots after the low-fatigue background/performance
  pass so the current day/fog/dusk and desktop-size artifacts reflect the active
  implementation.

Automated gates:
- Command:
  `dotnet build ProceduralRts.csproj --no-restore`
  Result:
  Pass.
  Evidence:
  Build passed with 0 warnings and 0 errors.
- Command:
  `dotnet run --project tools/DesktopHudQa/DesktopHudQa.csproj --no-restore`
  Result:
  Pass.
  Evidence:
  Desktop HUD QA passed for 1280x720, 1600x900, 1920x1080, and high-DPI layout
  constraints.
- Command:
  `powershell -NoProfile -ExecutionPolicy Bypass -File .\tools\VisualQaCapture.ps1`
  Result:
  Pass.
  Evidence:
  Regenerated `main_menu.png`, `main_menu_settings.png`, `battle_hud.png`,
  `battle_hud_1280x720.png`, `battle_hud_1600x900.png`,
  `battle_hud_1920x1080.png`, `battle_hud_style1b_fog.png`,
  `battle_hud_style1c_dusk.png`, `pause_menu.png`, and `outcome_victory.png`.
- Command:
  `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore -- softoldcity`
  Result:
  Pass.
  Evidence:
  ReviewGate softoldcity passed with 0 errors and 0 warnings.
- Command:
  `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore -- review --require-record=soft-old-city-readability`
  Result:
  Pass.
  Evidence:
  ReviewGate review passed with 0 errors and 0 warnings for this durable record.
- Command:
  `dotnet run --project tools/VerifyAll/VerifyAll.csproj --no-restore`
  Result:
  Pass.
  Evidence:
  VerifyAll passed 23/23 steps, including the new `desktop-hud-qa` step and the
  updated `review-gate` all-mode softoldcity checks.

Manual/visual gates:
- Check:
  `artifacts/visual-qa/battle_hud_1920x1080.png`
  Result:
  Pass.
  Evidence:
  HUD is edge-docked, minimap/top resource strip/command ribbon do not cover the
  central fight space, owner-color bars remain visible, and unit/building silhouettes
  read against the warm paper field and fog boundary.
- Check:
  `artifacts/visual-qa/battle_hud_1280x720.png`
  Result:
  Pass.
  Evidence:
  The same layout remains usable at the minimum desktop size; persistent HUD stays
  near edges and the lower-middle playfield is still open.
- Check:
  `artifacts/visual-qa/battle_hud_style1b_fog.png`
  Result:
  Pass.
  Evidence:
  Fog tone reduces fatigue without erasing ink outlines, owner-color marks, resource
  rings, or command buttons.
- Check:
  `artifacts/visual-qa/battle_hud_style1c_dusk.png`
  Result:
  Pass.
  Evidence:
  Dusk/night tone preserves unit outlines, owner color, minimap pips, and HUD
  affordances while darkening the world.

Reviewer result:
- Status: pass
- Required fixes:
  - None after the softoldcity gate and visual capture evidence are present.
- Residual risks:
  - The background is intentionally minimal after the performance pass; a later art
    slice can add a low-cost cached/shader texture treatment without returning to a
    dense grid.
  - The screenshots prove current Dog/Cat readability in battle framing; a future
    sandbox lineup can provide more controlled side-by-side faction comparison.

Status:
Pass.

TODO update:
- Items marked done:
  - `Reads as Soft Old City: clear silhouettes, owner color separate from faction,
    HUD edge-docked and low-obstruction, fog/day/night legible.`

Gate tag:
soft-old-city-readability
