# Review Record - EntityWorld ResourceSystem

Step:
Add a deterministic pure `ResourceSystem` and resource-node component data to
the EntityWorld simulation.

Milestone:
M4 Production & Economy System / Design Reference - Resource, Mining &
Environment Regeneration.

Owner AI:
Codex main agent.

Reviewer AI:
Codex main-agent review with `ReviewGate resourcesystem`, `SimReplay`, and
full `VerifyAll`.

Integrator AI:
Codex main agent.

Scope:
- Files/folders:
  - `scripts/core/ResourceNodeState.cs`
  - `scripts/core/entities/EntityComponentState.cs`
  - `scripts/core/entities/EntityWorld.cs`
  - `scripts/core/entities/EntityStateHash.cs`
  - `scripts/core/sim/systems/CommandSystem.cs`
  - `scripts/core/sim/systems/ResourceSystem.cs`
  - `scripts/core/sim/SimInvariants.cs`
  - `tools/SimReplay/Program.cs`
  - `tools/ReviewGate/Program.cs`
  - `TODO.md`
  - `docs/reviews/2026-06-29-entity-resource-system.md`
- Non-goals:
  - Do not migrate the live `GameState`/`UnitBattlefield` economy path.
  - Do not implement `ProductionSystem`.
  - Do not implement resource regeneration or environment modifiers.
  - Do not mark the complete mining-loop/nearest-node UX item done.

Implementation summary:
- Added `ResourceNodeComponentState` with amount, maxAmount,
  gatherRateModifier, depletionBehavior, visibilityRule, and corruptionState.
- Added `ResourceNodeState` enums for depletion, visibility, and corruption
  authoring data.
- Added deterministic owner `ResourceInventories` to `EntityWorld` and folded
  them into the state hash.
- Routed `HarvestEntityCommand` through `CommandSystem` into harvester component
  intent and movement target.
- Added pure `ResourceSystem : ISimSystem` for moving to a resource node,
  gathering into cargo, reserving an owner dock, unloading Credits, releasing
  dock occupancy, and returning to the field or idling.
- Extended `EntityStateHash` and `SimInvariants` for resource-node state.
- Added `SimReplay` resource-loop scenario and `ReviewGate resourcesystem`.

Automated gates:
- Command:
  `dotnet build ProceduralRts.csproj --no-restore`
  Result:
  Pass.
  Evidence:
  Build completed with 0 warnings and 0 errors.
- Command:
  `dotnet run --project tools/SimReplay/SimReplay.csproj --no-restore`
  Result:
  Pass.
  Evidence:
  SimReplay reported `OK [resource-loop]`, deterministic final hash, Credits
  increasing from 25 to 145, resource node amount reaching 0, and cargo 0.
- Command:
  `dotnet run --project tools/ReviewGate/ReviewGate.csproj resourcesystem --no-restore`
  Result:
  Pass.
  Evidence:
  ReviewGate reported 0 errors and 0 warnings.
- Command:
  `dotnet run --project tools/ReviewGate/ReviewGate.csproj review --require-record=entity-resource-system --no-restore`
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
  In-engine economy visual check.
  Result:
  Not required for this slice.
  Evidence:
  This slice is a headless EntityWorld simulation system and does not change the
  live presentation path.

Reviewer result:
- Status: pass.
- Required fixes:
  - None at record creation.
- Residual risks:
  - Live gameplay still uses the legacy economy path until EntityWorld becomes
    authoritative.
  - ProductionSystem, regeneration, economy metrics, and UI aggregation remain
    open TODO work.
  - ResourceSystem currently consumes explicit harvest commands; automatic
    nearest-node smart harvest remains a later command-vocabulary item.

TODO update:
- Items marked done:
  - `ResourceSystem (pure ISimSystem): harvester gather -> dock reservation -> unload credits -> field depletion`.
  - `ResourceNode data: amount, maxAmount, gatherRateModifier, depletionBehavior, visibilityRule, corruptionState`.
- Items left open:
  - `Add ResourceSystem and ProductionSystem` aggregate item, because
    ProductionSystem is not done.
  - Mining loop nearest-node/smart command UX.
  - Environment resource regeneration and economy metrics.
  - Deterministic economy/production tests that include regeneration and
    ProductionSystem.
- Reason:
  - Current code and gates prove the pure EntityWorld resource loop and resource
    node data model, while broader economy and production work remains open.
