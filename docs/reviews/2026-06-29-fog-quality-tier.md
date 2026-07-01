# Review Record - Fog quality tier

Step:
Add fog quality tiers and verify cached fog-mask rendering paths.

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
  - `scripts/core/FogQualityTier.cs`
  - `scripts/core/FogOfWarVisualPolicy.cs`
  - `scripts/core/FogOfWarMap.cs`
  - `scripts/core/GameState.cs`
  - `scripts/world/FogOfWarLayer.cs`
  - `scripts/BattleRoot.cs`
  - `tools/FogOfWarQa/Program.cs`
  - `tools/ReviewGate/Program.cs`
  - `TODO.md`
  - `docs/reviews/2026-06-29-fog-quality-tier.md`
- Non-goals:
  - Do not implement camera-rect-scoped fog recompute in this slice.
  - Do not add a visible settings-menu selector for fog quality.
  - Do not change default fog quality; Medium remains the default.

Implementation summary:
- Added `FogQualityTier` with Low, Medium, and High.
- `FogOfWarVisualPolicy` now maps fog quality to mask cell size and redraw
  interval, and exposes quality-specific `MaskSize`.
- `FogOfWarMap` can be constructed from a quality tier.
- `GameState` stores `FogQuality` and constructs its fog map with that tier.
- `FogOfWarLayer` uses `WorldRedrawIntervalFor(Quality)` instead of the fixed
  redraw interval.
- `BattleRoot` passes the state fog quality into the fog layer.
- `FogOfWarQa` now verifies Low/High mask resolution, redraw interval ordering,
  GameState fog cell size, and that normal runtime/minimap rendering does not
  call `FogOfWar.Snapshot()`.

Automated gates:
- Command:
  `dotnet build ProceduralRts.csproj --no-restore`
  Result:
  Pass.
  Evidence:
  Build completed with 0 warnings and 0 errors.
- Command:
  `dotnet run --project tools/FogOfWarQa/FogOfWarQa.csproj --no-restore`
  Result:
  Pass.
  Evidence:
  Fog-of-war QA passed mask channels, feathered edges, explored memory, hidden
  mobile enemies, static memory, 100-source smoke, and no runtime Snapshot
  rendering.
- Command:
  `dotnet run --project tools/ReviewGate/ReviewGate.csproj fog`
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
  Visible comparison of Low/Medium/High fog quality.
  Result:
  Not run.
  Evidence:
  This slice verifies data-path behavior and scene startup; visible quality
  comparison remains useful once a settings/sandbox selector is exposed.

Reviewer result:
- Status: pass-with-warnings
- Required fixes:
  - None for this bounded fog-quality slice.
- Residual risks:
  - Quality can be selected by constructors/data path but is not exposed in UI yet.
  - Camera-rect-scoped recompute remains open.

TODO update:
- Items marked done:
  - Reuse fog mask `ImageTexture` and upload only when visibility changed.
  - Fog quality tier.
  - Minimap consumes cached fog mask.
- Items left open:
  - Scope fog recompute to camera rect.
- Reason:
  - Existing dirty-upload/cache behavior is verified by FogOfWarQa/ReviewGate, and
    this slice adds and verifies quality-specific resolution/redraw policy.
