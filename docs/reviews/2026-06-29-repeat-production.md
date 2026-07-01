# Review Record - Repeat Production Core

Step: Implement EntityWorld repeat production core.
Milestone: Command Vocabulary Completeness
Owner AI: Codex
Reviewer AI: Codex reviewer pass
Integrator AI: Codex

Scope:
- Files/folders: `scripts/core/entities/EntityCommand.cs`, `scripts/core/entities/EntityComponentState.cs`, `scripts/core/entities/EntityStateHash.cs`, `scripts/core/sim/SimInvariants.cs`, `scripts/core/sim/systems/ProductionSystem.cs`, `tools/SimReplay/Program.cs`, `tools/ReviewGate/Program.cs`, `TODO.md`.
- Non-goals: UI repeat toggle, shift-queue command modifiers, AI planner use of repeat, rally onto moving units, legacy `GameState` production path migration.

Implementation summary:
- Added `SetRepeatProductionEntityCommand` so repeat production intent enters through the EntityWorld command buffer.
- Extended `ProductionQueueComponentState` with `RepeatOutputSpecId`; hash and invariants now include that state.
- `ProductionSystem` now validates repeat output specs, stores/clears repeat state, and refills an empty producer queue when repeat is enabled and credits are sufficient.
- Repeat enqueue spends credits only when the next loop item is actually queued; if credits are exhausted, the producer stays empty with repeat still armed.
- Existing per-producer queue, pause, cancel, and rally behavior is preserved.

Automated gates:
- Command: `dotnet build ProceduralRts.csproj --no-restore`
  Result: pass
  Evidence: build completed with 0 warnings and 0 errors.
- Command: `dotnet run --project tools/SimReplay/SimReplay.csproj --no-restore`
  Result: pass
  Evidence: `OK [repeat-production]: produced 2, repeat dog.infantry, queued 0, credits 0.` and `SimReplay PASSED.`
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj repeatproduction --no-restore`
  Result: pass
  Evidence: `Errors: 0`, `Warnings: 0`, `ReviewGate passed.`
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore`
  Result: pass
  Evidence: `Errors: 0`, `Warnings: 0`, `ReviewGate passed.`
- Command: `dotnet run --project tools/VerifyAll/VerifyAll.csproj --no-restore`
  Result: pass
  Evidence: all steps passed after this slice.

Manual/visual gates:
- Check: Visual QA
  Result: not applicable
  Evidence: this slice is deterministic simulation behavior only; UI repeat controls remain open.

Reviewer result:
- Status: pass
- Required fixes: none
- Residual risks: Repeat production is only exposed at the EntityWorld command/state layer. UI controls, AI planner usage, shift/queued command modifiers, and legacy runtime migration remain open.

TODO update:
- Items marked done: `EntityWorld repeat production core`.
- Items left open: queued command modifiers, UI repeat/loop controls, AI planner use of repeat, rally onto moving units, UI/smart-right-click rally wiring.
- Reason: replay and ReviewGate prove the bounded repeat command/state/refill behavior; adjacent UI, AI, and queued-command UX remains separate.
