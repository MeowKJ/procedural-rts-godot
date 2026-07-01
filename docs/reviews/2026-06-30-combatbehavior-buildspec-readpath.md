# Review Record - CombatBehavior BuildSpec read-path cleanup

Step: Migration cleanup CombatBehavior BuildSpec read-path slice
Milestone: M1 EntityWorld Becomes Authoritative
Owner AI: Codex
Reviewer AI: ReviewGate combatbehaviorbuildspecreadpath / Integrator
Integrator AI: Codex

Scope:
- Files/folders: `tools/CombatBehavior/Program.cs`, `tools/ReviewGate/Program.cs`, `TODO.md`, `docs/reviews/2026-06-30-combatbehavior-buildspec-readpath.md`.
- Non-goals: deleting `BuildCatalog`, deleting `GameState.BuildingDefinitions`, changing build data, changing balance, or migrating non-building unit compatibility reads.

Implementation summary:
- CombatBehavior building helper HP now reads `BuildSpecCatalog.For(kind).MaxHp`.
- Building coverage, structure presentation/build metadata, airfield semantics, turret semantics, HQ weapon checks, structure armor checks, and structure target-profile QA now read `BuildSpecCatalog` directly.
- Removed CombatBehavior reads of compatibility `GameState.BuildingDefinitions`, `BuildCatalog.For(...)`, and `BuildCatalog.Definitions`.
- Added `ReviewGate combatbehaviorbuildspecreadpath` to prevent this tool read path from regressing.

Automated gates:
- Command: `dotnet build ProceduralRts.csproj --no-restore`
  Result: pass
  Evidence: build completed successfully with 0 warnings and 0 errors before gate wiring.
- Command: `dotnet run --project tools/CombatBehavior/CombatBehavior.csproj --no-restore`
  Result: pass
  Evidence: CombatBehavior passed after the BuildSpec read-path cleanup.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore -- combatbehaviorbuildspecreadpath`
  Result: pass
  Evidence: ReviewGate combatbehaviorbuildspecreadpath completed with 0 errors and 0 warnings.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore -- review --require-record=combatbehavior-buildspec-readpath`
  Result: pass
  Evidence: required review record check completed with 0 errors and 0 warnings.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore`
  Result: pass
  Evidence: full ReviewGate completed with 0 errors and 0 warnings after updating the BuildSpec authority string gate.
- Command: `dotnet run --project tools/VerifyAll/VerifyAll.csproj --no-restore`
  Result: pass
  Evidence: VerifyAll passed 23/23 steps, including SimReplay, CombatBehavior, FogOfWarQa, PerfSmoke, BalanceReport, CounterReadabilityQa, and Godot headless QA.

Manual/visual gates:
- Check: visual inspection not required for this tool read-path migration.
  Result: not run.
  Evidence: no runtime visuals changed.

Reviewer result:
- Status: pass
- Required fixes: none known before gate execution.
- Residual risks: compatibility projections still exist for other tools and migration call sites, notably `FogOfWarQa` and source projection checks.

TODO update:
- Items marked done: `CombatBehavior BuildSpec read-path cleanup`.
- Items left open: broader Migration cleanup remains open.
- Reason: this removes a tool read path from building compatibility catalogs, not the compatibility catalogs themselves.
