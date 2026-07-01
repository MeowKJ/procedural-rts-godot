# Review Record - GameState BuildSpec combat metadata cleanup

Step: Migration cleanup GameState BuildSpec combat metadata slice
Milestone: M1 EntityWorld Becomes Authoritative
Owner AI: Codex
Reviewer AI: ReviewGate gamestatebuildspeccombatmetadata / Integrator
Integrator AI: Codex

Scope:
- Files/folders: `scripts/core/GameState.cs`, `scripts/core/WeaponTargetProfile.cs`, `tools/ReviewGate/Program.cs`, `TODO.md`, `docs/reviews/2026-06-30-gamestate-buildspec-combat-metadata.md`.
- Non-goals: changing balance values, deleting `BuildingDefinition`, migrating tool compatibility reads, or rewriting building combat.

Implementation summary:
- `WeaponTargetProfile` now accepts `BuildSpec` for building target legality and priority.
- `GameState` building weapon lookup, production-lane labels, target legality, target priority, damage multiplier, combat-source accent, and under-attack labels now use `BuildSpecCatalog.For(building.Kind)`.
- Legacy `BuildingDefinition` overloads remain for compatibility tools while live `GameState` building combat metadata reads no longer depend on them.
- Added `ReviewGate gamestatebuildspeccombatmetadata` to prevent these paths from regressing to direct legacy `BuildingDefinition` reads.

Automated gates:
- Command: `dotnet build ProceduralRts.csproj --no-restore`
  Result: pass
  Evidence: build completed successfully with 0 warnings and 0 errors before gate wiring.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore -- gamestatebuildspeccombatmetadata`
  Result: pass
  Evidence: ReviewGate gamestatebuildspeccombatmetadata completed with 0 errors and 0 warnings.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore -- review --require-record=gamestate-buildspec-combat-metadata`
  Result: pass
  Evidence: required review record check completed with 0 errors and 0 warnings.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore`
  Result: pass
  Evidence: full ReviewGate completed with 0 errors and 0 warnings.
- Command: `dotnet run --project tools/VerifyAll/VerifyAll.csproj --no-restore`
  Result: pass
  Evidence: VerifyAll passed 23/23 steps, including SimReplay, CombatBehavior, FogOfWarQa, PerfSmoke, BalanceReport, CounterReadabilityQa, and Godot headless QA.

Manual/visual gates:
- Check: visual inspection not required for this narrow runtime data-source migration.
  Result: not run.
  Evidence: combat formulas and target-profile rules remain unchanged; only the source of building metadata moved to BuildSpec.

Reviewer result:
- Status: pass
- Required fixes: none known before gate execution.
- Residual risks: `GameState.BuildingDefinitions`, `BuildCatalog`, and legacy `BuildingDefinition` / `BuildDefinition` compatibility types still exist for tools and migration call sites.

TODO update:
- Items marked done: `GameState BuildSpec combat metadata cleanup`.
- Items left open: broader Migration cleanup remains open.
- Reason: this removes live `GameState` combat/label reads of `BuildingDefinition`, not all building compatibility surfaces.
