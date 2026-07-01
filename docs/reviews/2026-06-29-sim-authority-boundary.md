# Review Record - Simulation authority boundary

Step: Prove the authoritative EntityWorld simulation path does not depend on Godot nodes, scene tree processing, frame callbacks, real time, or nondeterministic RNG as authority.
Milestone: Architecture hard boundaries.
Owner AI: Codex.
Reviewer AI: Codex self-review (ReviewGate simconventions and SimReplay provide durable checks).
Integrator AI: Codex.

Scope:
- Files/folders: `scripts/core/entities/*.cs`, `scripts/core/sim/**/*.cs`, `tools/SimReplay/Program.cs`, `tools/ReviewGate/Program.cs`, `TODO.md`, `docs/reviews/2026-06-29-sim-authority-boundary.md`.
- Non-goals: no migration of live `GameState`/`UnitBattlefield` authority, no removal of Godot math structs such as `Vector2`, no claim that presentation no longer reads legacy models, no gameplay behavior change.

Implementation summary:
- Verified existing `ReviewGate simconventions` scans the authoritative entity/sim folders for forbidden authority tokens such as Godot `Node`, `SceneTree`, `_Process`, `_PhysicsProcess`, `GetTree`, Godot RNG, `new Random`, `DateTime`, and real-time APIs.
- Verified `EntityWorld` keeps `Stopwatch` use behind debug `SystemTimingEnabled` metrics rather than simulation authority.
- Verified each `*System.cs` implements `ISimSystem`, steps via `Step(SimContext)`, and iterates stable entity order or ordered command-buffer data.
- Verified `SimReplay` covers deterministic movement, combat, group move, group attack, and outcome scenarios with stable hashes.

Automated gates:
- Command: `dotnet build ProceduralRts.csproj --no-restore`
  Result: pass
  Evidence: build completed with 0 warnings and 0 errors.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj simconventions --no-restore`
  Result: pass
  Evidence: ReviewGate reported 0 errors and 0 warnings for simulation conventions.
- Command: `dotnet run --project tools/SimReplay/SimReplay.csproj --no-restore`
  Result: pass
  Evidence: SimReplay passed deterministic movement, combat, authored, group-move, group-attack, and outcome checks.

Manual/visual gates:
- Check: visual QA
  Result: not run
  Evidence: this is an architecture/simulation boundary; no rendering changed.

Reviewer result:
- Status: pass
- Required fixes: none.
- Residual risks: the live game still has legacy `GameState`/`UnitBattlefield` paths and `EntityWorld` is still a shadow authority in BattleRoot; those migration TODOs remain open. This record only closes the hard boundary for the new authoritative EntityWorld simulation path.

TODO update:
- Items marked done: `Simulation never references Godot Node/SceneTree/_Process/real time as authority.`
- Items left open: view mutation boundary, owner-relation-only hostility, relation overlay color boundary, VFX pooling, EntityWorld live-authority migration.
- Reason: existing gates now provide durable proof that the authoritative entity simulation path is deterministic, fixed-step, and free of Godot node/scene-tree/frame-time authority.
