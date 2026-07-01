# Review Record - ProductionKindDesignBridge cleanup

Step: UnitSpec architecture phase 3 duplicate-data cleanup ProductionKind design bridge slice
Milestone: M1 EntityWorld Becomes Authoritative
Owner AI: Codex
Reviewer AI: ReviewGate productionkinddesignbridge / Integrator
Integrator AI: Codex

Scope:
- Files/folders: `scripts/core/units/ProductionKindDesignBridge.cs`, `scripts/core/sim/systems/ProductionSystem.cs`, `scripts/core/units/runtime/UnitBattlefield.cs`, `tools/CombatBehavior/Program.cs`, `tools/SimulationSmoke/Program.cs`, `tools/ReviewGate/Program.cs`, `TODO.md`, `docs/reviews/2026-06-30-production-kind-design-bridge.md`.
- Non-goals: deleting `GameState.ProductionDefinitions`, changing production balance, changing production durations/costs, changing AI production choices, or expanding the legacy `ProductionKind` enum.

Implementation summary:
- Added `ProductionKindDesignBridge` as the shared compatibility bridge between authored `UnitSpec` production data and the old three-value `ProductionKind` surface.
- Centralized legacy faction mapping, UnitSpec-to-ProductionKind mapping, faction/kind-to-UnitSpec resolution, playable production spec enumeration, and duration bounds.
- Added legacy INF/TNK/HAR compatibility UnitSpecs through `LegacySpecFor(...)` / `LegacyProductionSpecs()` so old-runtime `ProductionKind` queues can use UnitSpec data without inheriting faction-specific balance values.
- Refactored `ProductionSystem`, `UnitBattlefield`, `CombatBehavior`, and `SimulationSmoke` to use the bridge instead of local duplicated production-kind helpers.
- Preserved existing compatibility semantics, including scout aircraft resolving to the old light-vehicle production lane.
- Added `ReviewGate productionkinddesignbridge` to lock the shared bridge and prevent local helper mappings from returning.

Automated gates:
- Command: `dotnet build ProceduralRts.csproj --no-restore`
  Result: pass
  Evidence: build completed successfully with 0 warnings and 0 errors.
- Command: `dotnet run --project tools/CombatBehavior/CombatBehavior.csproj --no-restore`
  Result: pass
  Evidence: CombatBehavior passed weapon hit rules, turret states, terrain passability, localization fallback, presentation descriptors, shared threat propagation, rally production, economy, enemy AI, and outcomes.
- Command: `dotnet run --project tools/SimulationSmoke/SimulationSmoke.csproj --no-restore`
  Result: pass
  Evidence: SimulationSmoke passed its 300 second smoke with 10 orders, 10 completions, 2 waves, and outcome InProgress.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore -- productionkinddesignbridge`
  Result: pass
  Evidence: ReviewGate productionkinddesignbridge completed with 0 errors and 0 warnings.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore -- combatbehaviorproductionspecreadpath`
  Result: pass
  Evidence: ReviewGate combatbehaviorproductionspecreadpath completed with 0 errors and 0 warnings.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore -- simulationsmokeproductionspecreadpath`
  Result: pass
  Evidence: ReviewGate simulationsmokeproductionspecreadpath completed with 0 errors and 0 warnings.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore -- review --require-record=production-kind-design-bridge`
  Result: pass
  Evidence: required review record check completed with 0 errors and 0 warnings.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore`
  Result: pass
  Evidence: full ReviewGate completed with 0 errors and 0 warnings.
- Command: `dotnet run --project tools/VerifyAll/VerifyAll.csproj --no-restore`
  Result: pass
  Evidence: VerifyAll completed 23/23 checks after the slice.

Manual/visual gates:
- Check: visual inspection not required for this deterministic architecture bridge.
  Result: not run.
  Evidence: no runtime visuals or balance values changed.

Reviewer result:
- Status: pass
- Required fixes: none after automated gates.
- Residual risks: `GameState.ProductionDefinitions` still exists and remains used by legacy runtime compatibility paths until later production-runtime migration slices.

TODO update:
- Items marked done: none.
- Items left open: parent UnitSpec duplicate-data cleanup remains open.
- Reason: this centralizes legacy production-kind compatibility mapping, but does not delete the old runtime production definitions.
