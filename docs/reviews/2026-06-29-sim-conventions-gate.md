# Review Record - Sim conventions gate

Step: Add automated gates for authoritative simulation purity and ISimSystem conventions.
Milestone: Engineering Conventions.
Owner AI: Codex.
Reviewer AI: Codex self-review (subagents unavailable in this continuation turn; ReviewGate provides durable source checks).
Integrator AI: Codex.

Scope:
- Files/folders: `tools/ReviewGate/Program.cs`, `scripts/core/entities/EntityWorld.cs`, `TODO.md`, `docs/reviews/2026-06-29-sim-conventions-gate.md`.
- Non-goals: no EntityWorld authority migration, no gameplay behavior changes, no claim that content authoring or view-authority TODOs are complete.

Implementation summary:
- Added `ReviewGate simconventions`.
- The gate scans `scripts/core/entities` and `scripts/core/sim` for forbidden authoritative runtime hooks: Node/SceneTree, `_Process`/`_PhysicsProcess`, scene access, Godot random, `new Random`, wall-clock time, and real-time APIs.
- Stopwatch is allowed only in `EntityWorld`'s debug timing path, guarded by `SystemTimingEnabled`.
- The gate verifies every `scripts/core/sim/systems/*System.cs` implements `ISimSystem`, exposes `Step(SimContext)`, and either iterates `EntityWorld.OrderedEntities` or consumes the ordered command-buffer output in `CommandSystem`.
- The gate verifies SimReplay coverage for movement, combat, group move, group attack, and vision/outcome determinism.

Automated gates:
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj simconventions --no-restore`
  Result: pass
  Evidence: ReviewGate reported 0 errors and 0 warnings.
- Command: `dotnet build ProceduralRts.csproj --no-restore`
  Result: pass
  Evidence: build completed with 0 warnings and 0 errors.
- Command: `dotnet run --project tools/SimReplay/SimReplay.csproj --no-restore`
  Result: pass
  Evidence: full deterministic SimReplay suite passed after rerunning sequentially to avoid dotnet temp DLL locks.

Manual/visual gates:
- Check: visual QA
  Result: not applicable
  Evidence: source-level simulation convention gate only; no presentation output changed.

Reviewer result:
- Status: pass
- Required fixes: none.
- Residual risks: the gate proves current authoritative sim purity and system structure, but it does not prove the broader live-game authority migration or that legacy `GameState`/`UnitBattlefield` paths have been retired.

TODO update:
- Items marked done: `Simulation purity`; `Every system is a pure ISimSystem`.
- Items left open: new-content data-only rule, component/view authority boundaries, EntityWorld live authority migration.
- Reason: the exact TODO scope now has automated source checks plus deterministic replay evidence.
