# Review Record - Command Vocabulary Patrol

Step: Implement Patrol command vocabulary minimal core.
Milestone: Command vocabulary and deterministic sim-state.
Owner AI: Worker B / Codex.
Reviewer AI: Integrator / Codex.
Integrator AI: Codex.

Scope:
- Files/folders: `scripts/core/entities/EntityCommand.cs`, `scripts/core/entities/EntityComponentState.cs`, `scripts/core/entities/EntityStateHash.cs`, `scripts/core/sim/SimInvariants.cs`, `scripts/core/sim/systems/CommandSystem.cs`, `scripts/core/sim/systems/MovementSystem.cs`, `tools/SimReplay/Program.cs`, `tools/ReviewGate/Program.cs`, `docs/reviews/2026-06-29-patrol-command-core.md`.
- Non-goals: no Guard command, no UI hotkey or button, no shift queue, no multi-point patrol queue, no ConstructionSystem, power, or art palette changes.

Implementation summary:
- Added `PatrolEntityCommand` and `PatrolOrderComponentState` for two-point patrol intent.
- `CommandSystem` translates Patrol commands into patrol state plus an attack-move-style current leg so existing CombatSystem stance and auto-acquire behavior applies along the route.
- Explicit Move, Attack, Stop/Hold, Harvest, and Repair command paths clear patrol state so Patrol cannot revive after explicit intent changes.
- `MovementSystem` flips between endpoints on arrival and restores the active patrol leg after non-manual combat target state clears.
- Patrol state is covered by deterministic state hashing and simulation invariants.

Automated gates:
- Command: `dotnet build ProceduralRts.csproj --no-restore`
  Result: pass.
  Evidence: build completed with 0 warnings and 0 errors.
- Command: `dotnet run --project tools/SimReplay/SimReplay.csproj --no-restore`
  Result: pass.
  Evidence: `OK [patrol]: unit looped A->B->A, auto-engaged route threat, and resumed patrol intent.` and `SimReplay PASSED.`
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj patrol --no-restore`
  Result: pass.
  Evidence: `Errors: 0`, `Warnings: 0`, `ReviewGate passed.`

Manual/visual gates:
- Check: Visual QA
  Result: not applicable.
  Evidence: this slice is deterministic sim-core only; no UI surface changed.

Reviewer result:
- Status: pass.
- Required fixes: none after focused gate and SimReplay verification.
- Residual risks: Patrol currently supports one two-point loop only. It intentionally does not implement Guard, shift-queued waypoints, or UI command issuance.

TODO update:
- Items marked done: `Patrol: loop between two+ points, engaging hostiles encountered, returning to route.`
- Items left open: UI command binding, queued command modifiers, Guard, and richer command vocabulary remain open.
- Reason: deterministic Patrol command-buffer core is implemented and proved; presentation/input conveniences are separate work.
