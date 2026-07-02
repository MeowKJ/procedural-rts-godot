# Review Record - M9 AttackSlotMath buffer overload

Step: Route CommandSystem group attack through caller-owned AttackSlotMath buffers
Milestone: M9 Elegance, Decoupling, Performance
Owner AI: Remote Linux Codex
Reviewer AI: SimReplay / ReviewGate simhot
Integrator AI: Remote Linux Codex

Scope:
- Issue: #65.
- Files/folders: `scripts/core/sim/AttackSlotMath.cs`, `scripts/core/sim/systems/CommandSystem.cs`, `scripts/core/sim/systems/command/CommandSystem.CombatOrders.cs`, `tools/ReviewGateDomains/RegressionReviewGate.cs`, `TODO.md`, `docs/reviews/2026-07-02-m9-attack-slot-buffer-overload.md`.
- Non-goals: changing group attack slot semantics, anchor detection, standoff radius, target focus, movement state, `FormationMath`, damage, balance, AI strategy, or presentation behavior.

Implementation summary:
- Added `AttackSlotMath.AssignAttackSlotsInto(...)`, which writes assignments into caller-owned output and work buffers.
- Kept the existing `AssignAttackSlots(...)` API as a compatibility wrapper around the new buffer overload.
- Replaced `AttackSlotMath` LINQ ordering/average/result materialization inside the new hot path with deterministic list sorting and scan-based mover selection.
- Added reusable `CommandSystem` work buffers for AttackSlotMath ordered units, anchors, movers, free slots, and assignment results.
- Routed `CommandSystem.ApplyGroupAttack` through `AssignAttackSlotsInto(...)`, then reused the existing assignment lookup dictionary from #64.
- Extended broad `ReviewGate simhot` evidence so CommandSystem cannot regress to the allocating `AttackSlotMath.AssignAttackSlots(...)` API.

Automated gates:
- Command: `dotnet build ProceduralRts.csproj --no-restore`
  Result: pass
  Evidence: main project build completed with 0 warnings and 0 errors.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore -- simhot --max-warnings=0`
  Result: pass
  Evidence: ReviewGate completed with 0 errors and 0 warnings.
- Command: `dotnet run --project tools/SimReplay/SimReplay.csproj --no-restore`
  Result: pass
  Evidence: SimReplay passed, including anchored-group-attack-slotting, group-attack, firing-anchor, and attacking-anchor deterministic scenarios.
- Command: `dotnet run --project tools/CombatBehavior/CombatBehavior.csproj --no-restore`
  Result: pass
  Evidence: CombatBehavior passed weapon hit rules, turret states, terrain passability, shared threat propagation, rally production, economy, enemy AI, and outcomes.
- Command: `dotnet run --project tools/PerfSmoke/PerfSmoke.csproj --no-restore`
  Result: pass
  Evidence: 400-unit run averaged 11.087ms, p99 11.575ms, max 12.624ms, and 192620 bytes/tick, under the 16.667ms active budget.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore`
  Result: pass
  Evidence: full ReviewGate completed with 0 errors and 1 existing source-directory warning for `scripts/core/sim/`.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore -- review --require-record=m9-attack-slot-buffer-overload`
  Result: pass
  Evidence: review-record gate completed with 0 errors and 0 warnings.
- Command: `dotnet run --project tools/VerifyAll/VerifyAll.csproj --no-restore`
  Result: pass
  Evidence: VerifyAll completed all 23 steps successfully, including build, SimReplay, CombatBehavior, ReviewGate, PerfSmoke, balance/counter QA, and Godot headless QA.

Reviewer result:
- Status: pass.
- Required fixes: none.
- Residual risks: the compatibility `AssignAttackSlots(...)` API still allocates for non-hot callers; `FormationMath` group move internals still allocate; remaining M9 allocation debt includes Construction/placement lists, immutable queue/path arrays, and profiler-guided GC cleanup.

TODO update:
- Items marked done: none.
- Items left open: `Per-tick allocation paydown`.
- Reason: #65 removes one deeper group-attack assignment allocation family from CommandSystem's hot path, but the broad allocation-debt item remains open.
