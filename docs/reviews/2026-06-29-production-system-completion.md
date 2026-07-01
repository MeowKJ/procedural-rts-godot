# Review Record - ProductionSystem completion

Step:
Complete the broad EntityWorld `ProductionSystem` TODO by adding cancel/refund,
pause reason, and prerequisite/power gates.

Milestone:
M1 EntityWorld authority / M4 Production & Economy System.

Owner AI:
Codex main agent.

Reviewer AI:
Codex main-agent review with `ReviewGate productionsystem`, `SimReplay`, and
full `VerifyAll`.

Integrator AI:
Codex main agent.

Scope:
- Files/folders:
  - `scripts/core/ProductionPauseReason.cs`
  - `scripts/core/entities/EntityCommand.cs`
  - `scripts/core/entities/EntityComponentState.cs`
  - `scripts/core/entities/EntityStateHash.cs`
  - `scripts/core/sim/systems/ProductionSystem.cs`
  - `tools/SimReplay/Program.cs`
  - `tools/ReviewGate/Program.cs`
  - `TODO.md`
  - `docs/reviews/2026-06-29-production-system-completion.md`
- Non-goals:
  - Do not migrate the live Godot UI or legacy `GameState` production path.
  - Do not implement tech-tree UI or upgrade systems.
  - Do not implement smart rally to resource/auto-harvest.

Implementation summary:
- Added `ProductionPauseReason` and stored pause reason on
  `ProductionQueueComponentState`.
- Added `CancelProductionEntityCommand`.
- `ProductionSystem` now handles cancel/refund of the first queued item.
- `ProductionSystem` records deterministic pause reasons for unpowered and
  under-construction producers.
- Producer gating now checks owner, health, construction state, required producer
  `BuildingKind`, and producer tech tier against unit tech tier.
- `EntityStateHash` now includes production pause reason.
- `SimReplay` `production-loop` now proves completion, independent producer lanes,
  unpowered pause reason, cancel/refund, and rally intent.
- `ReviewGate productionsystem` now locks these requirements.

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
  units, 1 paused queue, 0 cancelled queue items, and 200 remaining Credits after
  four queued infantry plus one half refund.
- Command:
  `dotnet run --project tools/ReviewGate/ReviewGate.csproj productionsystem --no-restore`
  Result:
  Pass.
  Evidence:
  ReviewGate reported 0 errors and 0 warnings.
- Command:
  `dotnet run --project tools/ReviewGate/ReviewGate.csproj review --require-record=production-system-completion --no-restore`
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
  This is a deterministic headless EntityWorld simulation feature; live UI
  migration remains open.

Reviewer result:
- Status: pass.
- Required fixes:
  - None at record creation.
- Residual risks:
  - Live gameplay still uses the legacy production path until EntityWorld becomes
    authoritative.
  - Tech-tree data is minimal; future T2/T3 prerequisite richness should extend
    the same producer/tech gates rather than adding UI-only checks.

TODO update:
- Items marked done:
  - `ProductionSystem (pure ISimSystem): per-producer ProductionQueue advances by buildTime...`.
  - `Add ResourceSystem and ProductionSystem as pure ISimSystems; prove in SimReplay`.
- Items left open:
  - EntityWorld live authority migration.
  - Economy metrics and regeneration.
  - Build/construction system.
- Reason:
  - Current code and gates prove the complete text of the ProductionSystem TODO in
    the headless EntityWorld path.
