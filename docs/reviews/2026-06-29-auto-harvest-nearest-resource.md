# Review Record - Auto-harvest nearest resource

Step:
Implement deterministic auto-harvest selection of the nearest available resource
node and fallback to the next resource after depletion.

Milestone:
M4 Production & Economy System / Resource, Mining & Environment Regeneration.

Owner AI:
Codex main agent.

Reviewer AI:
Codex main-agent review with `ReviewGate autoharvest`,
`ReviewGate economyproductiontests`, `SimReplay`, and full `VerifyAll`.

Integrator AI:
Codex main agent.

Scope:
- Files/folders:
  - `scripts/core/entities/EntityCommand.cs`
  - `scripts/core/sim/ResourceMiningMath.cs`
  - `scripts/core/sim/systems/CommandSystem.cs`
  - `scripts/core/sim/systems/ResourceSystem.cs`
  - `tools/SimReplay/Program.cs`
  - `tools/ReviewGate/Program.cs`
  - `TODO.md`
  - `docs/reviews/2026-06-29-auto-harvest-nearest-resource.md`
- Non-goals:
  - Do not implement smart right-click UI.
  - Do not migrate live legacy economy authority.
  - Do not add resource visibility or reservation balancing beyond existing
    deterministic dock behavior.

Implementation summary:
- Added `AutoHarvestEntityCommand` as a command-buffer intent for harvesters to
  pick a resource automatically.
- Added `ResourceMiningMath.TryFindNearestAvailableResourceNode` to choose the
  nearest non-depleted resource by stable `EntityWorld.OrderedEntities` iteration.
- Updated `CommandSystem` to route auto-harvest through the same harvest intent
  path as explicit `HarvestEntityCommand`.
- Updated `ResourceSystem` so harvesters retarget to the next nearest available
  resource when their previous field is depleted or missing.
- Added deterministic `auto-harvest` SimReplay coverage and `ReviewGate
  autoharvest`.

Automated gates:
- Command:
  `dotnet build ProceduralRts.csproj --no-restore`
  Result:
  Pass.
  Evidence:
  Build reported 0 errors and 0 warnings.
- Command:
  `dotnet run --project tools/SimReplay/SimReplay.csproj --no-restore`
  Result:
  Pass.
  Evidence:
  SimReplay reported deterministic `auto-harvest`: nearest resource was chosen
  first, the near resource depleted to 0, fallback gathered from the far resource,
  and credits/cargo exceeded the first resource amount.
- Command:
  `dotnet run --project tools/ReviewGate/ReviewGate.csproj autoharvest --no-restore`
  Result:
  Pass.
  Evidence:
  ReviewGate reported 0 errors and 0 warnings.
- Command:
  `dotnet run --project tools/ReviewGate/ReviewGate.csproj economyproductiontests --no-restore`
  Result:
  Pass.
  Evidence:
  ReviewGate reported 0 errors and 0 warnings, including the new auto-harvest
  requirements.
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
  This is a headless deterministic simulation/command slice.

Reviewer result:
- Status: pass.
- Required fixes:
  - None at record creation.
- Residual risks:
  - Smart right-click and UI command preview remain separate TODO work.
  - Live gameplay still uses legacy economy paths until EntityWorld authority
    migration.
  - Resource visibility constraints are not yet applied to auto-harvest selection.

TODO update:
- Items marked done:
  - `Mining loop: harvester picks nearest available ResourceNode -> travels -> gathers to ResourceCargo capacity -> reserves a refinery Dock -> unloads -> credits bank -> returns`.
- Items left open:
  - Smart right-click harvest command.
  - EntityWorld live authority migration.
  - AI planner usage of the command-buffer harvest path.
- Reason:
  - Current code and `auto-harvest` replay prove deterministic nearest-resource
    selection, depletion fallback, dock/unload/banking behavior, and persistent
    review-gated coverage.
