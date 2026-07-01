# Review Record - UnitBattlefield building public-surface projection cleanup

Step: Migration cleanup building public-surface projection slice
Milestone: M1 EntityWorld Becomes Authoritative
Owner AI: Codex
Reviewer AI: ReviewGate buildingtargetpublicsurface / Integrator
Integrator AI: Codex

Scope:
- Files/folders: `scripts/core/units/runtime/UnitBattlefield.cs`, `scripts/controllers/SelectionController.cs`, `tools/CombatBehavior/Program.cs`, `tools/ReviewGate/Program.cs`, `TODO.md`, `docs/reviews/2026-06-30-building-target-public-surface.md`.
- Non-goals: deleting `UnitBattlefieldBuildingTarget`, changing building balance, changing unit movement/combat algorithms, changing production/economy rules, or changing visual style.

Implementation summary:
- Added id/projection public APIs for live building picking, hover, selected attack, explicit attack, and selected repair.
- Kept existing `UnitBattlefieldBuildingTarget` overloads as compatibility wrappers while moving new input callers to id-based commands.
- `SelectionController` now stores hovered live buildings as `BuildingHoverProjection` snapshots and submits building ids for attack/repair commands.
- `CombatBehavior` proves hover, hostile building attack, and damaged building repair use projection plus id APIs.

Automated gates:
- Command: `dotnet build ProceduralRts.csproj --no-restore`
  Result: pass
  Evidence: build completed successfully with 0 warnings and 0 errors.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore -- buildingtargetpublicsurface`
  Result: pass
  Evidence: ReviewGate buildingtargetpublicsurface completed with 0 errors and 0 warnings.
- Command: `dotnet run --project tools/CombatBehavior/CombatBehavior.csproj --no-restore`
  Result: pass
  Evidence: CombatBehavior passed weapon hit rules, turret states, terrain passability, localization fallback, presentation descriptors, shared threat propagation, rally production, economy, enemy AI, and outcomes.
- Command: `dotnet run --project tools/SimReplay/SimReplay.csproj --no-restore`
  Result: pass
  Evidence: SimReplay passed deterministic resource, production, construction, ability, movement, combat, group-move, group-attack, and outcome scenarios.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore -- review --require-record=building-target-public-surface`
  Result: pass
  Evidence: ReviewGate review record check completed with 0 errors and 0 warnings.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore`
  Result: pass
  Evidence: full ReviewGate completed with 0 errors and 0 warnings.
- Command: `dotnet run --project tools/VerifyAll/VerifyAll.csproj --no-restore`
  Result: pass
  Evidence: VerifyAll passed all 23 steps, including build, SimReplay, CombatBehavior, SimulationSmoke, FogOfWarQa, SelectionStress, AI/player/sandbox/HUD QA, full ReviewGate, PerfSmoke, BalanceReport, CounterReadabilityQa, and Godot headless scene checks.

Manual/visual gates:
- Check: visual inspection not required for this input/public-surface cleanup.
  Result: not run.
  Evidence: no drawing style, palette, or layout behavior changed; hover drawing now consumes the same projection data earlier in the call chain.

Reviewer result:
- Status: pass.
- Required fixes: none known.
- Residual risks: `UnitBattlefieldBuildingTarget` remains public for compatibility events, AI paths, and older tools; this slice only quarantines live input/hover callers behind ids and projections.

TODO update:
- Items marked done: `UnitBattlefield building public-surface projection cleanup` subitem under Migration cleanup.
- Items left open: parent Migration cleanup remains open.
- Reason: input callers no longer need mutable building target handles for hover/attack/repair, but the compatibility wrapper and several runtime/event surfaces remain.
