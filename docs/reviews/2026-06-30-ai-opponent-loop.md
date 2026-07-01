# Review Record - AI opponent loop

Step:
Complete the playable 1v1 AI opponent loop: harvest, build, mixed production,
defense, and attack waves through command-buffer-backed UnitBattlefield bridges.

Milestone:
Playable 1v1 skirmish vertical slice - AI opponent.

Owner AI:
Main integrator plus parallel behavior worker.

Reviewer AI:
Parallel QA/ReviewGate workers plus automated gates.

Integrator AI:
Main thread.

Scope:
- Files/folders:
  - `scripts/core/units/runtime/UnitBattlefield.cs`
  - `scripts/core/units/runtime/UnitBattlefieldEnemyProductionAi.cs`
  - `scripts/core/units/runtime/UnitBattlefieldEnemyAttackWaveAi.cs`
  - `tools/AiOpponentLoopQa/`
  - `tools/ReviewGate/Program.cs`
  - `tools/AiDifficultySmoke/Program.cs`
  - `tools/VerifyAll/Program.cs`
  - `docs/reviews/2026-06-30-ai-opponent-loop.md`
- Non-goals:
  - Do not add multiplayer or campaign AI.
  - Do not migrate the whole runtime to pure EntityWorld authority in this slice.
  - Do not let AI directly assign attack/move state in wave orders.

Implementation summary:
- Added UnitBattlefield command bridges for explicit AI construction, explicit-unit
  harvest, explicit-unit movement, and VisibilityIndex rebuild/query.
- Enemy production AI now maintains harvesters, starts buildings through
  `ConstructBuilding`, queues concrete UnitDesign outputs for mixed armies, and
  exposes construction/order counters.
- Enemy attack-wave AI now rebuilds VisibilityIndex before target selection,
  filters targets through `IsVisibleTo`, defends visible base threats, scouts with
  attack-move when no target is visible, and attacks through `CommandAttackUnits`.
- Added `tools/AiOpponentLoopQa` and wired it into `tools/VerifyAll`.
- Upgraded `ReviewGate aiopponentloop` to enforce the AI loop static contract.

Automated gates:
- Command:
  `dotnet build ProceduralRts.csproj --no-restore`
  Result:
  Pass.
  Evidence:
  Build completed with 0 warnings and 0 errors after the AI loop changes.
- Command:
  `dotnet run --project tools/AiDifficultySmoke/AiDifficultySmoke.csproj --no-restore`
  Result:
  Pass.
  Evidence:
  Easy/Normal/Hard production and wave pacing scale, and Hard beats Easy in
  the wave-pressure smoke without relying on hidden HQ targeting.
- Command:
  `dotnet run --project tools/AiOpponentLoopQa/AiOpponentLoopQa.csproj --no-restore`
  Result:
  Pass.
  Evidence:
  96-second UnitBattlefield loop proved harvester assignment, 5100 resource
  depletion, full AI base structure set, 20 production orders / 16 completions,
  mixed infantry and vehicle output, defense hits, 6 attack waves, 1200 player-HQ
  damage, and command bridge deltas for harvest/production/waves. Construction
  probe completed PowerPlant and GroundTurret via `StartConstructionEntityCommand`.
- Command:
  `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore -- aiopponentloop`
  Result:
  Pass.
  Evidence:
  0 errors, 0 warnings.
- Command:
  `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore -- review --require-record=ai-opponent-loop`
  Result:
  Pass after this record update.
  Evidence:
  Required durable review record is present and complete.

Manual/visual gates:
- Check:
  Live skirmish AI observation.
  Result:
  Covered by deterministic headless loop, not manually observed in Godot this slice.
  Evidence:
  `AiOpponentLoopQa` exercises the runtime UnitBattlefield path used by skirmish AI.

Reviewer result:
- Status: pass
- Required fixes:
  - None for this slice.
- Residual risks:
  - UnitBattlefield is still a bridge runtime; future M1 cleanup should continue
    moving construction/AI authority deeper into EntityWorld.
  - AI difficulty tuning is functional but still coarse; future balance work should
    tune Easy/Normal/Hard feel, counters, and scouting personality.

TODO update:
- Items marked done:
  - AI opponent harvests, builds, produces a mixed army, defends, and attacks in
    waves - all via the command buffer.
- Items left open:
  - Counter readability, 60 FPS full-battle performance, and Soft Old City
    readability remain separate vertical-slice TODO items.
- Reason:
  - Runtime QA and ReviewGate now prove the AI opponent loop acceptance criteria.
