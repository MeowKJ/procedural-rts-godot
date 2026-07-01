# Review Record - Construction offline pause

Step:
ConstructionSystem offline/low-power pause minimal core.

Milestone:
M3 ConstructionSystem lifecycle.

Owner AI:
Worker A.

Reviewer AI:
Codex owner self-review with deterministic replay and ReviewGate coverage; awaiting independent reviewer.

Integrator AI:
Pending integrator.

Scope:
- Files/folders: `scripts/core/ConstructionPauseReason.cs`, `scripts/core/entities/EntityComponentState.cs`, `scripts/core/entities/EntityStateHash.cs`, `scripts/core/sim/SimInvariants.cs`, `scripts/core/sim/systems/ConstructionSystem.cs`, `scripts/core/sim/systems/PowerSystem.cs`, `tools/SimReplay/Program.cs`, `tools/ReviewGate/Program.cs`, `docs/reviews/2026-06-29-construction-pause-offline.md`.
- Non-goals: UI construction pause indicators, TODO.md updates, new placement rules, resource refunds/cancel behavior, construction method UX, or broad PowerSystem balance changes beyond construction-period power budget participation.

Implementation summary:
- Added `ConstructionPauseReason` and stored deterministic pause state on `ConstructionComponentState`.
- `EntityStateHash` now includes construction pause reason, and `SimInvariants` rejects invalid/stale completed construction pauses.
- `ConstructionSystem` pauses progress only for started, power-consuming construction entities whose `PowerComponentState.Powered` is false, preserving progress and clearing pause when power returns.
- `PowerSystem` lets started under-construction consumers contribute demand without contributing supply, so construction can be judged powered/unpowered before completion while generators do not self-power early.
- SimReplay adds `construction-paused-offline` coverage for preserved progress, resume after restored power, zero-progress startup, and non-consuming/provider no-self-lock cases.
- ReviewGate adds the narrow `constructionpause` gate.

Automated gates:
- Command: `dotnet build ProceduralRts.csproj --no-restore`
  Result: pass.
  Evidence: Build succeeded with 0 warnings and 0 errors.
- Command: `dotnet run --project tools/SimReplay/SimReplay.csproj --no-restore`
  Result: pass.
  Evidence: `SimReplay PASSED`; `OK [construction-paused-offline]: paused at 0.250, resumed to 0.267, offline held 0.250.`
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj constructionpause --no-restore`
  Result: pass.
  Evidence: `Errors: 0`, `Warnings: 0`, `ReviewGate passed.`

Manual/visual gates:
- Check: Not applicable.
  Result: pass.
  Evidence: Pure simulation state/lifecycle slice with no presentation surface.

Reviewer result:
- Status: pass-with-warnings
- Required fixes: Independent reviewer still needed by the multi-agent protocol.
- Residual risks: ConstructionSystem runs before PowerSystem, so a just-spawned power-consuming building can advance for its first tick before the next power verdict. This is intentional in this minimal slice to avoid initial `Powered=false` self-lock and is covered by replay.

TODO update:
- Items marked done: None; TODO.md intentionally untouched for this worker slice.
- Items left open: Broader construction lifecycle polish, UI pause feedback, and integrator-owned TODO updates remain open.
- Reason: Worker A owns only the narrow deterministic pause/resume core and evidence record.
