# Review Record - Grid terrain layout cache

Step:
Cache GridLayer terrain tile layout so theme redraws do not regenerate terrain
geometry/noise/kind data.

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
  - `scripts/core/TerrainFloorTileLayout.cs`
  - `scripts/core/TerrainFloorMath.cs`
  - `scripts/world/GridLayer.cs`
  - `tools/ReviewGate/Program.cs`
  - `TODO.md`
  - `docs/reviews/2026-06-29-grid-layout-cache.md`
- Non-goals:
  - Do not replace GridLayer with a rendered texture cache in this slice.
  - Do not implement MultiMesh or visible-rect drawing in this slice.
  - Do not change map terrain rules or visual output.
  - Do not mark the full GridLayer TODO complete.

Implementation summary:
- Added `TerrainFloorTileLayout` for terrain rect/kind/noise data.
- Added `TerrainFloorMath.CreateTileLayout()` and `TerrainFloorMath.PaletteFor()`.
- `GridLayer` now caches tile layout by world size and applies palette per redraw.
- `GridLayer.DrawFloorPanels()` no longer calls themed `CreateTiles(WorldSize,
  palette)` while drawing.
- Added `ReviewGate grid` to verify the layout-cache path.

Automated gates:
- Command:
  `dotnet build ProceduralRts.csproj`
  Result:
  Pass.
  Evidence:
  Build completed with 0 warnings and 0 errors.
- Command:
  `dotnet run --project tools/ReviewGate/ReviewGate.csproj grid`
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
  `dotnet run --project tools/ReviewGate/ReviewGate.csproj review --require-record=grid-layout-cache`
  Result:
  Pass.
  Evidence:
  ReviewGate reported 0 errors and 0 warnings.

Manual/visual gates:
- Check:
  Independent Reviewer AI read-only audit.
  Result:
  Pass with warnings.
  Evidence:
  Reviewer verified that `GridLayer` caches rect/kind/noise layout by world size,
  avoids themed `CreateTiles(WorldSize, palette)` in drawing, and correctly leaves
  the broad GridLayer TODO open. Reviewer noted that `ReviewGate grid` is still a
  static string gate and does not prove visual equivalence across theme changes.

Reviewer result:
- Status: pass-with-warnings
- Required fixes:
  - None for this bounded slice.
- Residual risks:
  - GridLayer still redraws the full map when the theme changes.
  - This is a layout/data cache, not a rendered texture cache.
  - Visual QA in the Godot window has not been performed.
  - `ReviewGate grid` is source-text based; a future pure cache-behavior test should
    prove same-size reuse, size invalidation, and palette-only theme changes.

TODO update:
- Items marked done:
  - None; the broad GridLayer cache/visible-rect TODO remains open.
- Items left open:
  - Rendered texture/MultiMesh cache or visible-rect drawing.
  - Further GridLayer visual QA.
- Reason:
  - Evidence proves terrain layout generation is cached, but not the full rendered
    static grid surface requested by the broad TODO.
