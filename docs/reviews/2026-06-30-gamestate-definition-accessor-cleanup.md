# Review Record - GameState Definition accessor cleanup

Step: UnitSpec / BuildSpec duplicate-data cleanup GameState Definition accessor slice
Milestone: M1 EntityWorld Becomes Authoritative
Owner AI: Codex
Reviewer AI: ReviewGate gamestatedefinitionaccessorcleanup / Integrator
Integrator AI: Codex

Scope:
- Files/folders: `scripts/core/GameState.cs`, `tools/CombatBehavior/Program.cs`, `tools/ReviewGate/Program.cs`, `TODO.md`, `docs/reviews/2026-06-30-gamestate-definition-accessor-cleanup.md`.
- Non-goals: deleting `UnitDefinition`, deleting `BuildingDefinition`, deleting static compatibility projections, changing combat behavior, changing pathing behavior, or changing balance data.

Implementation summary:
- Removed `GameState.Definition(UnitModel)` after external unit reads moved to UnitSpec runtime descriptors.
- Removed `GameState.Definition(BuildingModel)` after external building reads moved to BuildSpec.
- Moved remaining internal GameState rally-label, shared-threat, and refinery-delivery reads to BuildSpec / UnitSpec descriptor data.
- Moved CombatBehavior building occupancy QA from `occupancyState.Definition(hqObstacle)` to `BuildSpecCatalog.For(hqObstacle.Kind)`.
- Added `ReviewGate gamestatedefinitionaccessorcleanup` to prevent the old public accessors from returning.

Automated gates:
- Command: `dotnet build ProceduralRts.csproj --no-restore`
  Result: pass
  Evidence: build completed successfully with 0 warnings and 0 errors.
- Command: `dotnet run --project tools/CombatBehavior/CombatBehavior.csproj --no-restore`
  Result: pass
  Evidence: CombatBehavior passed weapon hit rules, turret states, terrain passability, localization fallback, presentation descriptors, shared threat propagation, rally production, economy, enemy AI, and outcomes.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore -- gamestatedefinitionaccessorcleanup`
  Result: pass
  Evidence: ReviewGate gamestatedefinitionaccessorcleanup completed with 0 errors and 0 warnings.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore -- review --require-record=gamestate-definition-accessor-cleanup`
  Result: pass
  Evidence: required review record check completed with 0 errors and 0 warnings.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore`
  Result: pass
  Evidence: full ReviewGate completed with 0 errors and 0 warnings.
- Command: `dotnet run --project tools/VerifyAll/VerifyAll.csproj --no-restore`
  Result: pass
  Evidence: VerifyAll completed 23/23 checks after the slice.

Manual/visual gates:
- Check: visual inspection not required for this compatibility accessor cleanup.
  Result: not run.
  Evidence: no runtime visuals changed; occupancy QA still uses the same footprint dimensions from BuildSpec.

Reviewer result:
- Status: pass
- Required fixes: none after automated gates.
- Residual risks: static `UnitDefinition` / `BuildingDefinition` compatibility overloads and projections remain until the broader legacy deletion milestone.

TODO update:
- Items marked done: none.
- Items left open: parent UnitSpec duplicate-data cleanup and BuildSpec migration cleanup remain open.
- Reason: this removes two public GameState accessor methods, not all legacy compatibility types.
