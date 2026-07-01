# Review Record - GameState UnitDefinition public read-surface deletion

Step: GameState UnitDefinition public read-surface deletion
Milestone: M1 EntityWorld Becomes Authoritative / UnitSpec duplicate-data cleanup
Owner AI: Codex
Reviewer AI: ReviewGate gamestateunitdefinitionpublicdeleted / Integrator
Integrator AI: Codex

Scope:
- Files/folders: `scripts/core/GameState.cs`, `tools/ReviewGate/Program.cs`, `TODO.md`, `docs/reviews/2026-06-30-gamestate-unitdefinition-public-deleted.md`.
- Non-goals: deleting `UnitKind`, deleting `UnitCatalog`, changing unit stats/balance, changing production/economy behavior, changing art/UI style, or migrating mobile unit-vs-unit combat.

Implementation summary:
- Removed `GameState.UnitDefinitionFor(...)`, `HasUnitDefinition(...)`, `UnitDefinitionValues`, `UnitDefinitionEntries`, and the private `LegacyUnitDefinitions` shim.
- `GameState` runtime compatibility reads now remain behind `UnitRuntimeDescriptorFor(...)`, which resolves legacy `UnitKind` through `UnitKindDesignBridge`.
- Updated ReviewGate so the old public-surface cleanup no longer requires replacement accessors and the new `gamestateunitdefinitionpublicdeleted` mode forbids them from returning.

Automated gates:
- Command: `dotnet build ProceduralRts.csproj --no-restore`
  Result: pass
  Evidence: build completed successfully with 0 warnings and 0 errors.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore -- gamestateunitdefinitionpublicdeleted`
  Result: pass
  Evidence: ReviewGate gamestateunitdefinitionpublicdeleted completed with 0 errors and 0 warnings.
- Command: `dotnet run --project tools/CombatBehavior/CombatBehavior.csproj --no-restore`
  Result: pass
  Evidence: CombatBehavior passed weapon hit rules, turret states, terrain passability, localization fallback, presentation descriptors, shared threat propagation, rally production, economy, enemy AI, and outcomes.
- Command: `dotnet run --project tools/SimulationSmoke/SimulationSmoke.csproj --no-restore`
  Result: pass
  Evidence: SimulationSmoke passed 300s with 10 orders, 10 completions, 2 waves, and outcome InProgress.
- Command: `dotnet run --project tools/FogOfWarQa/FogOfWarQa.csproj --no-restore`
  Result: pass
  Evidence: FogOfWarQa passed mask channels, feathered edges, explored memory, hidden mobile enemies, static memory, camera-scoped texture updates, 100-source smoke, and no runtime Snapshot rendering.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore`
  Result: pass
  Evidence: full ReviewGate completed with 0 errors and 0 warnings.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore -- review --require-record=gamestate-unitdefinition-public-deleted`
  Result: pass
  Evidence: ReviewGate review record check completed with 0 errors and 0 warnings.
- Command: `dotnet run --project tools/VerifyAll/VerifyAll.csproj --no-restore`
  Result: pass
  Evidence: VerifyAll passed all 23 steps, including build, SimReplay, CombatBehavior, SimulationSmoke, FogOfWarQa, SelectionStress, AI/player/sandbox/HUD QA, full ReviewGate, PerfSmoke, BalanceReport, CounterReadabilityQa, and Godot headless scene checks.

Manual/visual gates:
- Check: visual inspection not required.
  Result: not run.
  Evidence: no rendering, palette, layout, or camera behavior changed.

Reviewer result:
- Status: pass.
- Required fixes: none known.
- Residual risks: `UnitKind` and `UnitCatalog` still exist as compatibility data for old runtime paths. This slice removes only the `GameState` public UnitDefinition read surface.

TODO update:
- Items marked done: `GameState UnitDefinition public read-surface deletion` under UnitSpec architecture phase 3 duplicate-data cleanup.
- Items left open: the broad UnitSpec duplicate-data cleanup and final `UnitKind` / `UnitCatalog` deletion remain open.
- Reason: the duplicate public `GameState` read API is gone, but legacy compatibility enums/catalogs still back old-runtime surfaces.
