# Review Record - Canonical Soft Old City palette

Step:
Unify the shared world and HUD palette source.

Milestone:
M7 UI and presentation polish.

Owner AI:
Codex main agent.

Reviewer AI:
Codex main-agent review with `ReviewGate palette`.

Integrator AI:
Codex main agent.

Scope:
- Files/folders:
  - `scripts/core/SoftOldCityPalette.cs`
  - `scripts/ui/SoftOldCityTheme.cs`
  - `scripts/core/WorldThemeMath.cs`
  - `tools/ReviewGate/Program.cs`
  - `TODO.md`
  - `docs/reviews/2026-06-29-canonical-soft-old-city-palette.md`
- Non-goals:
  - Do not redesign HUD layout or CommandPlate geometry.
  - Do not remove all decorative one-off colors from effects/debug layers.
  - Do not change ownership/relation color semantics.
  - Do not tune screenshots in this slice.

Implementation summary:
- `SoftOldCityPalette` now owns the shared Soft Old City color vocabulary:
  paper/ink/text/border, command/repair/route/cargo/danger, fog, dusk, and night
  radar tones.
- `SoftOldCityTheme` derives day/fog/dusk HUD palettes from `SoftOldCityPalette`
  rather than duplicating base hex values.
- `WorldThemeMath` derives day/fog/dusk/night world palettes from the same source.
- Added `ReviewGate palette` and included it in the default `all` gate.

Automated gates:
- Command:
  `dotnet build ProceduralRts.csproj --no-restore`
  Result:
  Pass.
  Evidence:
  Build completed with 0 warnings and 0 errors.
- Command:
  `dotnet run --project tools/ReviewGate/ReviewGate.csproj palette --no-restore`
  Result:
  Pass.
  Evidence:
  ReviewGate reported 0 errors and 0 warnings.
- Command:
  `dotnet run --project tools/ReviewGate/ReviewGate.csproj review --require-record=canonical-soft-old-city-palette --no-restore`
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
  Screenshot comparison of palette feel.
  Result:
  Not run.
  Evidence:
  This is a source-of-truth refactor; visual art tuning remains open.

Reviewer result:
- Status: pass.
- Required fixes:
  - None at record creation.
- Residual risks:
  - Individual VFX/debug colors are still local by design; later art passes may
    choose to centralize more of them.
  - The CommandPlate shape itself remains tile-based and is tracked by a separate
    TODO item.

TODO update:
- Items marked done:
  - `One canonical palette source shared by world + HUD`.
- Items left open:
  - CommandPlate rounded fog-like field.
  - Build/production UI.
  - Shared UI factory extraction.
- Reason:
  - The shared world/HUD theme vocabulary now flows through one palette class and
    has a regression gate.
