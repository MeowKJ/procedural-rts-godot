# Review Record - Combat target broadphase

Step:
Replace per-attacker full-entity hostile target scans with a reusable combat target
broadphase.

Milestone:
M6 Performance.

Owner AI:
Codex main agent.

Reviewer AI:
Codex main-agent self-review; independent reviewer was not spawned because the
current thread reached the subagent limit.

Integrator AI:
Codex main agent.

Scope:
- Files/folders:
  - `scripts/core/sim/systems/CombatSystem.cs`
  - `tools/ReviewGate/Program.cs`
  - `TODO.md`
  - `docs/reviews/2026-06-29-combat-target-broadphase.md`
- Non-goals:
  - Do not change combat balance, damage, cooldowns, or command semantics.
  - Do not rewrite weapon mount runtime storage in this slice.
  - Do not mark the full sim-step allocation TODO complete.

Implementation summary:
- `CombatSystem` now builds a reusable per-tick `_targetGrid` containing living
  health-bearing target candidates.
- Auto-acquire `NearestHostile` searches nearby grid cells instead of iterating all
  entities for every attacker.
- Manual target focus remains direct lookup via `EntityWorld.TryGet`.
- Equal-distance target ties remain deterministic by selecting the lower `EntityId`.
- `ReviewGate simhot` now verifies combat broadphase hooks and guards against
  `NearestHostile` returning to a full `world.OrderedEntities` scan.

Automated gates:
- Command:
  `dotnet build ProceduralRts.csproj`
  Result:
  Pass.
  Evidence:
  Build completed with 0 warnings and 0 errors.
- Command:
  `dotnet run --project tools/SimReplay/SimReplay.csproj`
  Result:
  Pass.
  Evidence:
  SimReplay reported deterministic movement, combat, authored, group-move,
  group-attack, and outcome scenarios.
- Command:
  `dotnet run --project tools/PerfSmoke/PerfSmoke.csproj -c Release`
  Result:
  Pass.
  Evidence:
  Worst average was 1.169ms at 400 units under the 16.667ms budget; 400u p99 was
  1.821ms and allocation was about 188125 bytes/tick.
- Command:
  `dotnet run --project tools/ReviewGate/ReviewGate.csproj simhot`
  Result:
  Pass.
  Evidence:
  ReviewGate reported 0 errors and 0 warnings.
- Command:
  `dotnet run --project tools/ReviewGate/ReviewGate.csproj review --require-record=combat-target-broadphase`
  Result:
  Pass.
  Evidence:
  ReviewGate reported 0 errors and 0 warnings.
- Command:
  `dotnet run --project tools/ReviewGate/ReviewGate.csproj`
  Result:
  Pass.
  Evidence:
  ReviewGate reported 0 errors and 0 warnings.

Manual/visual gates:
- Check:
  Runtime battle stress QA.
  Result:
  Not run.
  Evidence:
  Headless deterministic and performance gates passed; no in-engine battle capture
  was taken.

Reviewer result:
- Status: pass-with-warnings
- Required fixes:
  - None for this bounded source-level slice.
- Residual risks:
  - Independent reviewer was not available due to subagent limit.
  - Combat mount list updates and event drain allocations remain visible in
    `alloc/tick`.
  - The target grid retains reusable bucket storage; long matches with units moving
    across many cells should be profiled for memory growth if maps become much larger.

TODO update:
- Items marked done:
  - None; the broad sim-step cost item remains open.
- Items left open:
  - Combat mount list allocation cleanup.
  - Event drain allocation cleanup.
  - Further proof for `StableEntities`/`StableSpecs` usage.
- Reason:
  - Evidence proves the combat target broadphase and performance improvement, but not
    all hot-path allocation work.
