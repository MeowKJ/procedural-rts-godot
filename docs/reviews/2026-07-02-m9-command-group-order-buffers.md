# Review Record - M9 CommandSystem group order buffers

Step: Reuse CommandSystem group move and group attack scratch buffers
Milestone: M9 Elegance, Decoupling, Performance
Owner AI: Remote Linux Codex
Reviewer AI: SimReplay / ReviewGate simhot
Integrator AI: Remote Linux Codex

Scope:
- Issue: #64.
- Files/folders: `scripts/core/sim/systems/CommandSystem.cs`, `scripts/core/sim/systems/command/CommandSystem.MovementOrders.cs`, `scripts/core/sim/systems/command/CommandSystem.CombatOrders.cs`, `scripts/core/sim/systems/command/CommandSystem.SubjectsSelection.cs`, `tools/ReviewGateDomains/RegressionReviewGate.cs`, `TODO.md`, `docs/reviews/2026-07-02-m9-command-group-order-buffers.md`.
- Non-goals: changing `FormationMath` or `AttackSlotMath` internals, changing command semantics, changing slot assignment behavior, changing balance values, or closing the broad M9 allocation paydown item.

Implementation summary:
- Added reusable `CommandSystem` scratch buffers for group-order members, formation units, group-move destination lookup, attack slot units, and group-attack assignment lookup.
- Added `CollectOwnedSubjects(...)` to fill caller-owned member storage while preserving subject order and owner filtering.
- Replaced group move `OwnedSubjects(...).ToList()`, formation LINQ projection, and `ToDictionary(d => d.Id)` with reusable buffers.
- Replaced group attack `OwnedSubjects(...).ToList()`, attack slot LINQ projection, and `ToDictionary(a => a.Id)` with reusable buffers.
- Extended broad `ReviewGate simhot` evidence so these group-order paths cannot regress to the removed command-layer list/dictionary allocations.

Automated gates:
- Command: `dotnet build ProceduralRts.csproj --no-restore`
  Result: pass
  Evidence: main project build completed with 0 warnings and 0 errors.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore -- simhot --max-warnings=0`
  Result: pass
  Evidence: ReviewGate completed with 0 errors and 0 warnings.
- Command: `dotnet run --project tools/SimReplay/SimReplay.csproj --no-restore`
  Result: pass
  Evidence: SimReplay passed, including group-move, same-point-move, anchored-group-attack-slotting, group-attack, firing-anchor, and attacking-anchor deterministic scenarios.
- Command: `dotnet run --project tools/CombatBehavior/CombatBehavior.csproj --no-restore`
  Result: pass
  Evidence: CombatBehavior passed weapon hit rules, turret states, terrain passability, shared threat propagation, rally production, economy, enemy AI, and outcomes.
- Command: `dotnet run --project tools/PerfSmoke/PerfSmoke.csproj --no-restore`
  Result: pass
  Evidence: 400-unit run averaged 10.919ms, p99 11.599ms, max 13.272ms, and 192620 bytes/tick, under the 16.667ms active budget.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore`
  Result: pass
  Evidence: full ReviewGate completed with 0 errors and 1 existing source-directory warning for `scripts/core/sim/`.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore -- review --require-record=m9-command-group-order-buffers`
  Result: pass
  Evidence: review-record gate completed with 0 errors and 0 warnings.
- Command: `dotnet run --project tools/VerifyAll/VerifyAll.csproj --no-restore`
  Result: pass
  Evidence: VerifyAll completed all 23 steps successfully, including build, SimReplay, CombatBehavior, ReviewGate, PerfSmoke, balance/counter QA, and Godot headless QA.

Reviewer result:
- Status: pass.
- Required fixes: none.
- Residual risks: `FormationMath` and `AttackSlotMath` still allocate their own internal working lists; remaining M9 allocation debt still includes Construction/placement lists, immutable queue/path arrays, and profiler-guided GC cleanup.

TODO update:
- Items marked done: none.
- Items left open: `Per-tick allocation paydown`.
- Reason: #64 removes and locks one command-layer allocation family but does not complete the broad allocation-debt item.
