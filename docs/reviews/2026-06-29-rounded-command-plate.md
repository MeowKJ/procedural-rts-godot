# Review Record - Rounded CommandPlate field

Step:
Replace tile-based CommandPlate visuals with a continuous rounded field.

Milestone:
M7 UI and presentation polish.

Owner AI:
Codex main agent.

Reviewer AI:
Codex main-agent review with `ReviewGate grid`.

Integrator AI:
Codex main agent.

Scope:
- Files/folders:
  - `scripts/core/TerrainFloorMath.cs`
  - `scripts/world/GridLayer.cs`
  - `tools/CombatBehavior/Program.cs`
  - `tools/ReviewGate/Program.cs`
  - `TODO.md`
  - `docs/reviews/2026-06-29-rounded-command-plate.md`
- Non-goals:
  - Do not redesign the full terrain style.
  - Do not change build-radius gameplay rules.
  - Do not change fog-of-war visibility.
  - Do not tune screenshots or final palette values in this slice.

Implementation summary:
- `TerrainFloorMath.KindAt` no longer marks tiles as `CommandPlate`; command areas
  are no longer part of cached terrain tile classification.
- `GridLayer.DrawCommandZone` now delegates to `DrawSoftCommandField`, which draws
  layered circular fields, soft lobes, and low-contrast curved edge hints.
- Removed the tile-local `CommandPlate` rectangle/cross motif from `DrawTileMotif`.
- Extended `ReviewGate grid` to require the soft field path and reject tile-local
  rectangular CommandPlate motifs or `IsCommandPlate` tile classification.
- Updated `CombatBehavior` terrain-floor assertions so cached terrain tiles must
  not contain `CommandPlate`; this keeps the deterministic behavior test aligned
  with the presentation-only rounded field.

Automated gates:
- Command:
  `dotnet build ProceduralRts.csproj --no-restore`
  Result:
  Pass.
  Evidence:
  Build completed with 0 warnings and 0 errors.
- Command:
  `dotnet run --project tools/ReviewGate/ReviewGate.csproj grid --no-restore`
  Result:
  Pass.
  Evidence:
  ReviewGate reported 0 errors and 0 warnings.
- Command:
  `dotnet run --project tools/CombatBehavior/CombatBehavior.csproj --no-restore`
  Result:
  Pass.
  Evidence:
  CombatBehavior passed after asserting CommandPlate stays out of cached terrain
  tiles.
- Command:
  `dotnet run --project tools/ReviewGate/ReviewGate.csproj review --require-record=rounded-command-plate --no-restore`
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
  Screenshot comparison of CommandPlate readability.
  Result:
  Not run.
  Evidence:
  This slice removes the grid implementation and adds automated structural
  protection; final visual tuning remains open to screenshot review.

Reviewer result:
- Status: pass.
- Required fixes:
  - None at record creation.
- Residual risks:
  - The soft field is still a drawn tactical wash, not a real fog mask texture.
  - Final alpha/shape tuning should be done with desktop screenshots.

TODO update:
- Items marked done:
  - `CommandPlate: replace tile-based plate with a continuous rounded fog-like field`.
- Items left open:
  - Broader UI factory extraction.
  - Build/production UI.
  - Visual screenshot tuning for Soft Old City.
- Reason:
  - CommandPlate is no longer tile classified or drawn with rectangular tile motifs,
    and the regression gate enforces the new rounded-field path.
