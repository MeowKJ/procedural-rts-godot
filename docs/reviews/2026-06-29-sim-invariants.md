# Review Record - SimInvariants

Step: Add debug invariant pass for EntityWorld state.
Milestone: Engineering Conventions / simulation safety.
Owner AI: Codex.
Reviewer AI: Codex self-review (subagents unavailable in this turn; review gate provides durable checks).
Integrator AI: Codex.

Scope:
- Files/folders: `scripts/core/sim/SimInvariants.cs`, `scripts/core/sim/SimInvariantViolation.cs`, `scripts/core/entities/EntityWorld.cs`, `scripts/core/entities/EntityComponentState.cs`, `scripts/core/entities/EntityStateHash.cs`, `tools/SimReplay/Program.cs`, `tools/ReviewGate/Program.cs`, `TODO.md`.
- Non-goals: no movement/combat behavior changes, no live EntityWorld authority migration, no production/economy system migration.

Implementation summary:
- Added an optional `EntityWorld.SimInvariantsEnabled` debug pass with `PROCEDURAL_RTS_SIM_INVARIANTS=1`.
- Added invariant checks for finite transforms and component values, health bounds, attack target existence/liveness, dock references and duplicate reservations, construction/power/cargo bounds, production queue length, and bounded command queues.
- Added `CommandQueueComponentState` as the future queued-order state slot and included it in deterministic hashing.
- Added SimReplay assertions that valid state passes and malformed transform, HP, target, dock reservation, and command queue state fail.
- Added a `ReviewGate siminvariants` mode to keep the invariant hook and tests from regressing.

Automated gates:
- Command: `dotnet build ProceduralRts.csproj --no-restore`
  Result: pass
  Evidence: build completed with 0 warnings and 0 errors.
- Command: `dotnet run --project tools/SimReplay/SimReplay.csproj --no-restore`
  Result: pass
  Evidence: `OK [sim-invariants]` plus full SimReplay PASSED.
- Command: `dotnet run --project tools/CombatBehavior/CombatBehavior.csproj --no-restore`
  Result: pass
  Evidence: Combat behavior passed all listed behavior checks.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj siminvariants --no-restore`
  Result: pass
  Evidence: ReviewGate reported 0 errors, 0 warnings.

Manual/visual gates:
- Check: visual QA
  Result: not applicable
  Evidence: simulation-only safety pass; no presentation output changed.

Reviewer result:
- Status: pass
- Required fixes: none.
- Residual risks: `CommandQueueComponentState` is only a bounded data slot today; future Shift-queued order behavior still needs a consuming system and replay scenarios.

TODO update:
- Items marked done: `SimInvariants` debug pass.
- Items left open: broader command vocabulary and EntityWorld authority migration.
- Reason: invariant hook, tests, review gate, and automated evidence now match the exact TODO scope.
