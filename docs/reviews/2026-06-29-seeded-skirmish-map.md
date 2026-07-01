# Review Record - Seeded skirmish map

Step:
Implement deterministic seeded skirmish map generation with mirrored starts,
paired resources, and paired choke obstacles.

Milestone:
Design Reference - Match Lifecycle & Map Generation.

Owner AI:
Codex main agent.

Reviewer AI:
Codex main-agent review with `ReviewGate seededmap`, `ReviewGate matchconfig`,
`CombatBehavior`, and full `VerifyAll`.

Integrator AI:
Codex main agent.

Scope:
- Files/folders:
  - `scripts/core/SkirmishMapGenerator.cs`
  - `scripts/core/MatchStartLoadout.cs`
  - `scripts/core/GameState.cs`
  - `tools/CombatBehavior/Program.cs`
  - `tools/ReviewGate/Program.cs`
  - `TODO.md`
  - `docs/reviews/2026-06-29-seeded-skirmish-map.md`
- Non-goals:
  - Do not implement full match teardown/rematch lifecycle.
  - Do not migrate EntityWorld live authority.
  - Do not add a map editor.

Implementation summary:
- Added `SkirmishMapGenerator` and `SkirmishMapLayout`.
- The generator uses `MatchConfig.MapSeed` and `WorldSize` to produce non-default
  mirrored HQ starts, paired equal-value resource nodes, and paired choke
  obstacles.
- Added layout-aware `MatchStartLoadouts.For(...)` overload so starting
  buildings/units can be translated to generated start positions while preserving
  the existing data-driven faction rosters.
- Updated `GameState.Seed()` to consume generated map resources and loadouts.
- Added `GameState.MapObstacles` and included generated obstacles in land path
  blockers.
- Preserved the existing hand-authored default start positions for the default
  seed so current UI/rally smoke tests remain stable.

Automated gates:
- Command:
  `dotnet build ProceduralRts.csproj --no-restore`
  Result:
  Pass.
  Evidence:
  Build reported 0 errors and 0 warnings.
- Command:
  `dotnet run --project tools/CombatBehavior/CombatBehavior.csproj --no-restore`
  Result:
  Pass.
  Evidence:
  CombatBehavior passed, including new checks for paired resources/obstacles,
  mirrored generated starts, mirrored HQ positions, equal-value mirrored resource
  pairs, and path-obstacle integration.
- Command:
  `dotnet run --project tools/ReviewGate/ReviewGate.csproj seededmap --no-restore`
  Result:
  Pass.
  Evidence:
  ReviewGate reported 0 errors and 0 warnings.
- Command:
  `dotnet run --project tools/ReviewGate/ReviewGate.csproj matchconfig --no-restore`
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
  Visual QA.
  Result:
  Not required.
  Evidence:
  This is a deterministic map-generation/data slice. Godot headless Battle QA is
  covered by VerifyAll.

Reviewer result:
- Status: pass.
- Required fixes:
  - None at record creation.
- Residual risks:
  - Default seed intentionally preserves old hand-authored start positions for
    existing smoke-test compatibility; non-default seeded maps use mirrored starts.
  - Full match lifecycle teardown/rematch remains open.
  - Terrain visuals do not yet render generated obstacle affordances distinctly.

TODO update:
- Items marked done:
  - `Deterministic seeded map generation for skirmish: symmetric/fair layout - mirrored HQ start positions, balanced resource-node placement and counts, passable terrain with some chokes/obstacles, no side advantaged`.
- Items left open:
  - Match lifecycle setup/run/teardown/rematch.
  - Live EntityWorld authority migration.
  - UI/visual readout for generated map obstacles.
- Reason:
  - Current source and automated tests prove deterministic same-seed generation,
    different-seed variation, mirrored starts/resources/obstacles for generated
    skirmish maps, and pathing integration.
