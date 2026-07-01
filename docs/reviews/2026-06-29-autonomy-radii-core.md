# Review Record - Autonomy three-radii core

Step:
Implement the minimal Autonomy three-radii slice for EntityWorld combat.

Milestone:
M2 Unit autonomy redesign.

Owner AI:
Worker-M2B / Codex.

Reviewer AI:
Pending.

Integrator AI:
Pending.

Scope:
- Files/folders: `scripts/core/entities/EntityComponentState.cs`, `scripts/core/entities/UnitSpecEntityBridge.cs`, `scripts/core/entities/EntityStateHash.cs`, `scripts/core/sim/SimInvariants.cs`, `scripts/core/sim/systems/CommandSystem.cs`, `scripts/core/sim/systems/CombatSystem.cs`, `tools/SimReplay/Program.cs`, `tools/ReviewGate/Program.cs`, `docs/reviews/2026-06-29-autonomy-radii-core.md`.
- Non-goals: no fog last-known memory, no kiting, no construction cleanup, no UI changes, no PathfindingSystem rewrite.

Implementation summary:
- Added `AutonomyComponentState` with explicit `AcquireRange`, `LeashRange`, and optional anchor.
- Armed units spawned through `UnitSpecEntityBridge` now receive default autonomy data from authored sight/weapon data.
- `CombatSystem` resolves a stance-tuned autonomy model: Hold acquires only inside weapon range and does not chase, Aggressive auto-acquires and pursues, ReturnGuard respects leash and returns to anchor, Ignore does not auto-acquire.
- `CommandSystem` refreshes autonomy anchor when Hold or SetStance commands update the stance anchor.
- Deterministic state hash and invariant validation include autonomy radii.
- `SimReplay` adds `autonomy-radii` coverage for Hold, Aggressive, ReturnGuard, and Ignore.

Automated gates:
- Command: `dotnet build ProceduralRts.csproj --no-restore`
  Result: pass.
  Evidence: build completed with 0 warnings and 0 errors.
- Command: `dotnet run --project tools/SimReplay/SimReplay.csproj --no-restore`
  Result: pass.
  Evidence: `OK [autonomy-radii]: Hold anchored, Aggressive chased, ReturnGuard leashed home, Ignore stayed passive.` Full SimReplay passed.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj autonomyradii --no-restore`
  Result: pass.
  Evidence: ReviewGate completed with 0 errors and 0 warnings.

Manual/visual gates:
- Check: Not applicable; this is a deterministic sim-core slice.
  Result: not run.
  Evidence: no presentation surface changed.

Reviewer result:
- Status: ready for review.
- Required fixes: none known.
- Residual risks: ReturnGuard leash currently applies to auto-acquired targets; manual attack focus remains explicitly prioritized. PassiveRetaliate remains outside this minimum slice.

TODO update:
- Items marked done: none.
- Items left open: parent M2 autonomy items remain open for broader retaliation, fog memory, and follow-up tuning.
- Reason: user explicitly requested not to update TODO.md.
