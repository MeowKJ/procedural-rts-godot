# Review Record - Command Vocabulary Guard

Step: Implement Guard command vocabulary minimal core.
Milestone: Command vocabulary and deterministic sim-state.
Owner AI: Worker D / Codex.
Reviewer AI: Integrator / Codex.
Integrator AI: Codex.

Scope:
- Files/folders: `scripts/core/entities/EntityCommand.cs`, `scripts/core/entities/EntityComponentState.cs`, `scripts/core/entities/EntityStateHash.cs`, `scripts/core/sim/SimInvariants.cs`, `scripts/core/sim/systems/CommandSystem.cs`, `scripts/core/sim/systems/MovementSystem.cs`, `scripts/core/sim/systems/CombatSystem.cs`, `tools/SimReplay/Program.cs`, `tools/ReviewGate/Program.cs`, `docs/reviews/2026-06-29-guard-command-core.md`.
- Non-goals: no UI hotkey, no right-click smart command, no command queue modifiers, no building projection/view changes, no art tone changes, no TODO.md update.

Implementation summary:
- Added `GuardEntityCommand` and `GuardOrderComponentState` with optional protected entity, fixed guard point, and radius.
- `CommandSystem` applies Guard only for positive-radius orders and friendly protected entities, clears manual attack focus, and keeps Guard mutually exclusive with Patrol.
- Explicit Move, Attack, Stop, Hold, Harvest, Repair, and Patrol command paths clear Guard so old protection intent cannot revive.
- `MovementSystem` keeps guard units inside the current guard radius, following a protected entity when present and falling back to the fixed point when the target is gone.
- `CombatSystem` resolves bounded guard threats before normal auto-acquire, prioritizing enemies threatening the protected entity or friendlies inside the guarded area without falling through to unbounded aggressive scans.
- Guard state is covered by deterministic state hashing and simulation invariants.

Automated gates:
- Command: `dotnet build ProceduralRts.csproj --no-restore`
  Result: pass.
  Evidence: build completed with 0 warnings and 0 errors.
- Command: `dotnet run --project tools/SimReplay/SimReplay.csproj --no-restore`
  Result: pass.
  Evidence: `OK [guard]: entity guard protected/followed an ally, area guard held a point, threats were cleared, and explicit orders cleared Guard.` and `SimReplay PASSED.`
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj guard --no-restore`
  Result: pass.
  Evidence: `Errors: 0`, `Warnings: 0`, `ReviewGate passed.`

Manual/visual gates:
- Check: Visual QA
  Result: not applicable.
  Evidence: this slice is deterministic sim-core only; no UI or presentation surface changed.

Reviewer result:
- Status: pass.
- Required fixes: none after focused gate and SimReplay verification.
- Residual risks: Guard is intentionally minimal: one protected entity or one fixed area, no UI issuance, no queued modifiers, and no richer escort formation offsets.

TODO update:
- Items marked done: none in this worker slice; TODO.md is intentionally left for the main thread.
- Items left open: UI command binding, right-click smart command, queued command modifiers, and richer command vocabulary remain open.
- Reason: deterministic Guard command-buffer core is implemented and proved; presentation/input conveniences are separate work.
