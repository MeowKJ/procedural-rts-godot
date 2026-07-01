# Review Record - M11 map authoring pipeline

Step: M11 pure MapSpec and loader
Milestone: M11 Map Authoring Pipeline
Owner AI: Codex
Reviewer AI: ReviewGate mapauthoring / MapAuthoringQa
Integrator AI: Codex

Scope:
- Files/folders: `scripts/core/map/MapSpec.cs`, `scripts/core/map/MapLoader.cs`, `scripts/core/map/SkirmishMapSpecGenerator.cs`, `scripts/core/map/MapSpecGodotAdapter.cs`, `scripts/core/map/SkirmishMapGenerator.cs`, `tools/MapAuthoringQa/`, `tools/SimReplay/Content/MapAuthoringScenarios.cs`, `tools/VerifyAll/Program.cs`, `tools/ReviewGate/ContentAuthoringReviewGate.cs`, `TODO.md`.
- Non-goals: replacing the current live `GameState` and `UnitBattlefield` presentation seeding paths in this slice, or building the full campaign objective runtime.

Implementation summary:
- Added a pure C# `MapSpec` model with owner starts, terrain cells, resources, obstacles, building/unit seeds, trigger areas, objectives, and narrative nodes.
- Added `SkirmishMapSpecGenerator` so the existing seeded skirmish layout has a first-class `MapSpec` output.
- Added `MapLoader` to seed an `EntityWorld` from the same data path for units, buildings, resources, and objectives.
- Added a QA-only Godot scene baker that reads a hand-authored `.tscn` fixture and emits the same pure `MapSpec`; the sim and loader do not read scene files.
- Wired `MapAuthoringQa` into `VerifyAll`, added a SimReplay deterministic map-loader scenario, and added `ReviewGate mapauthoring` source guards.

Automated gates:
- Command: `dotnet build ProceduralRts.csproj --no-restore`
  Result: pass
  Evidence: main project build completed with 0 warnings and 0 errors.
- Command: `dotnet run --project tools/MapAuthoringQa/MapAuthoringQa.csproj --no-restore`
  Result: pass
  Evidence: generated skirmish and baked hand-designed maps both loaded through `MapLoader` and produced stable deterministic hashes.
- Command: `dotnet run --project tools/SimReplay/SimReplay.csproj --no-restore`
  Result: pass
  Evidence: SimReplay passed, including `map-spec-loader`.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore -- mapauthoring`
  Result: pass
  Evidence: ReviewGate completed with 0 errors and 0 warnings.
- Command: `dotnet run --project tools/VerifyAll/VerifyAll.csproj --no-restore`
  Result: pass
  Evidence: VerifyAll completed all 23 steps successfully, including `map-authoring-qa`, `map-spec-loader`, full ReviewGate, perf smoke, and Godot headless QA.

Reviewer result:
- Status: pass
- Required fixes: none.
- Residual risks: live campaign boot still needs a future integration slice to make `GameState` / `UnitBattlefield` consume baked story maps directly instead of only proving the authoritative `EntityWorld` load path.

TODO update:
- Items marked done: all M11 Map Authoring Pipeline checklist items.
- Items left open: future campaign boot integration and objective-graph runtime outside M11.
- Reason: the architecture now has one data format for seeded and hand-authored maps, plus loader, QA, SimReplay, and ReviewGate coverage.
