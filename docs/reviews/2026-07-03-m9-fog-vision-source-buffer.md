# Review Record - M9 Fog Vision Source Buffer

Step: #195 `[M9] Reuse FogOfWarMap vision source buffer`
Milestone: M9 - Elegance & Decoupling
Owner AI: Remote Linux Codex
Reviewer AI: ReviewGate regression / GameStateAllocationReviewGate
Integrator AI: Remote Linux Codex

Scope:
- Files/folders: `scripts/core/fog/FogOfWarMap.cs`, `scripts/core/game-state/GameState.RelationsPickingFog.cs`, `tools/ReviewGateRuntime/GameStateAllocationReviewGate.cs`.
- Non-goals: 不改变 fog reveal math、mask texture upload、dirty viewport gate、visibility/explored semantics、unit sight range、balance 或 visual style。

Implementation summary:
- `FogOfWarMap.Update(...)` now takes `IReadOnlyList<(Vector2 Position, float SightRange)>` on the main runtime path.
- The update loop uses the caller-owned list directly for signature, skip checks, reveal iteration, and source count tracking.
- `GameStateAllocationReviewGate` locks the fog path against deferred enumerable input and `visionSources.ToArray()` materialization.

Automated gates:
- Command: `dotnet build ProceduralRts.csproj --no-restore`
  Result: pass，0 warnings / 0 errors。
- Command: `dotnet run --project tools/FogOfWarQa/FogOfWarQa.csproj --no-restore`
  Result: pass，mask channels / explored memory / camera-scoped texture updates / 100-source smoke preserved。
- Command: `dotnet run --project tools/PlayerLoopQa/PlayerLoopQa.csproj --no-restore`
  Result: pass，runtime player loop preserved with fog updates active。
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore -- regression --max-warnings=0`
  Result: pass，0 errors / 0 warnings。
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore -- review --require-record=m9-fog-vision-source-buffer`
  Result: pass，record is present with concrete automated gate evidence。

Reviewer result:
- Status: pass.
- Required fixes: none currently known.

Residual risks:
- Callers must now provide an indexable source list; existing runtime callers already collect into reusable buffers.
- Parent #10 remains open for broader allocation paydown.

TODO update:
- Items marked done: none.
- Items left open: broader M9 per-tick allocation paydown remains open under #10.
