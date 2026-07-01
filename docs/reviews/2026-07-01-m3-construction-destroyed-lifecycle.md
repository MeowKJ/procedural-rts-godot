# Review Record - M3 construction destroyed lifecycle

Step: M3 ConstructionSystem destroyed-state lifecycle
Milestone: M3 Build & Construction System
Owner AI: Codex
Reviewer AI: ReviewGate m3constructiondestroyedlifecycle / SimReplay
Integrator AI: Codex

Scope:
- Files/folders: `scripts/core/sim/SimEvent.cs`, `scripts/core/sim/systems/ConstructionSystem.cs`, `scripts/core/sim/systems/construction/ConstructionSystem.State.cs`, `tools/SimReplay/Economy/ConstructionDestroyedLifecycleScenarios.cs`, `tools/SimReplay/Core/ReplayPrelude.cs`, `TODO.md`.
- Non-goals: visual destruction effects, balance changes, or player-facing Dog construction HUD.

Implementation summary:
- Added `ConstructionDestroyedEvent` with entity, owner, build id, position, progress, and phase evidence.
- `ConstructionSystem` now receives the current tick when advancing construction.
- Construction lifecycle now terminates any entity that has `ConstructionComponentState` and `Health.Hp <= 0`: emits `ConstructionDestroyedEvent`, emits generic `EntityDestroyedEvent`, queues removal, and stops further construction work for that entity.
- Added deterministic replay coverage for dead under-construction buildings, dead completed construction buildings, and dead restart/capture objectives; the scenario also proves the removed footprint can be reused by replacement construction.

Automated gates:
- Command: `dotnet build ProceduralRts.csproj --no-restore`
  Result: pass
  Evidence: main project build completed with 0 warnings and 0 errors.
- Command: `dotnet build tools/SimReplay/SimReplay.csproj --no-restore`
  Result: pass
  Evidence: SimReplay project build completed with 0 warnings and 0 errors.
- Command: `dotnet run --project tools/SimReplay/SimReplay.csproj --no-restore`
  Result: pass
  Evidence: `construction-destroyed-lifecycle` completed deterministically; output showed destroyed 3, replacement entity 5, credits 600.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore -- m3constructiondestroyedlifecycle`
  Result: pass
  Evidence: historical narrow mode routed through the content gate and passed.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore -- review --require-record=m3-construction-destroyed-lifecycle`
  Result: pass
  Evidence: review record gate found this record and passed.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore -- filesize`
  Result: pass
  Evidence: file-size gate completed with 0 errors and 0 warnings.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore`
  Result: pass
  Evidence: full ReviewGate completed with 0 errors and 0 warnings.
- Command: `dotnet run --project tools/CombatBehavior/CombatBehavior.csproj --no-restore`
  Result: pass
  Evidence: combat behavior completed successfully after construction destruction events were added.
- Command: `dotnet run --project tools/PlayerLoopQa/PlayerLoopQa.csproj --no-restore`
  Result: pass
  Evidence: player-loop QA passed, including construction handoff coverage.

Manual/visual gates:
- Check: file-size governance
  Result: pass
  Evidence: new scenario file and touched system files are below the 200-line healthy target; no C# file crosses the 400-line normal ceiling.

Reviewer result:
- Status: pass
- Required fixes: none known.
- Residual risks: presentation-specific death VFX and player-facing construction UX remain separate TODOs.

TODO update:
- Items marked done: `ConstructionSystem` lifecycle backend, faction-distinct backend methods, and deterministic construction backend tests.
- Items left open: M7 player-facing Dog/Cat construction UI/HUD handoff.
- Reason: construction backend now covers direct builds, queues, ready placement, pause/offline, cancel/refund, Dog unit/deploy authority, shared restart/capture, and destroyed-state removal.
