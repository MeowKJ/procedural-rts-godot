# Review Record - EntityWorld ProductionSystem

Step:
Add deterministic EntityWorld production queues and prove per-producer
production authority.

Milestone:
M4 Production & Economy System.

Owner AI:
Codex main agent.

Reviewer AI:
Codex main-agent review with `ReviewGate productionsystem`, `SimReplay`, and
full `VerifyAll`.

Integrator AI:
Codex main agent.

Scope:
- Files/folders:
  - `scripts/core/entities/EntityWorld.cs`
  - `scripts/core/sim/systems/ProductionSystem.cs`
  - `tools/SimReplay/Program.cs`
  - `tools/ReviewGate/Program.cs`
  - `TODO.md`
  - `docs/reviews/2026-06-29-entity-production-system.md`
- Non-goals:
  - Do not migrate live `GameState`/`UnitBattlefield` production UI.
  - Do not implement cancel/refund commands.
  - Do not implement full prerequisite/tech gating.
  - Do not mark the broad `ProductionSystem` TODO complete.

Implementation summary:
- Added pure `ProductionSystem : ISimSystem`.
- `ProductionSystem` consumes `ProduceEntityCommand`, validates producer ownership
  and producer building kind, spends owner Credits, and appends to that producer's
  `ProductionQueueComponentState`.
- Added deterministic production item id allocation to `EntityWorld` and folded
  the counter into the deterministic state hash.
- Queues advance with `ProductionMath.Advance` per producer, pause under
  unpowered or incomplete producers, and spawn authored `UnitSpec` units through
  `world.SpawnUnit`.
- Produced units receive the producer `RallyPointComponentState` as command intent.
- Added `SimReplay` `production-loop` scenario and `ReviewGate productionsystem`.

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
  SimReplay reported `OK [production-loop]`, deterministic final hash, 2 produced
  units from powered producers, 1 paused queue on an unpowered producer, and
  owner Credits reduced to 140 after three queued infantry.
- Command:
  `dotnet run --project tools/ReviewGate/ReviewGate.csproj productionsystem --no-restore`
  Result:
  Pass.
  Evidence:
  ReviewGate reported 0 errors and 0 warnings.
- Command:
  `dotnet run --project tools/ReviewGate/ReviewGate.csproj review --require-record=entity-production-system --no-restore`
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
  In-engine production UI visual check.
  Result:
  Not required for this slice.
  Evidence:
  This slice is a headless EntityWorld simulation path and does not alter the live
  HUD or legacy production UI.

Reviewer result:
- Status: pass.
- Required fixes:
  - None at record creation.
- Residual risks:
  - The broad `ProductionSystem` TODO remains open because cancel/refund,
    explicit pause reasons, and prerequisite/tech gates are not implemented.
  - Live gameplay still uses the legacy production path until EntityWorld becomes
    authoritative.
  - Produced unit spawn collision uses current entity obstacles, but building
    footprint/pathing integration will need more scenarios later.

TODO update:
- Items marked done:
  - `Per-producer authority: each barracks/factory owns its queue/progress/rally`.
- Items left open:
  - Broad `ProductionSystem` TODO.
  - Aggregate `Add ResourceSystem and ProductionSystem` milestone.
  - Deterministic economy/production tests covering cancel/refund and production
    prerequisites.
- Reason:
  - Current code and gates prove independent producer queues, progress, rally, and
    powered pause behavior, while broader production features remain open.
