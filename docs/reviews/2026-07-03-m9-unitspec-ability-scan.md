# Review Record - M9 UnitSpec Ability Scan

Step: #175 `[M9] Replace UnitSpec ability LINQ checks`
Milestone: M9 - Elegance & Decoupling
Owner AI: Codex
Reviewer AI: ReviewGate regression / UnitSpecAbilityAllocationReviewGate
Integrator AI: Codex

Scope:
- Files/folders: `scripts/core/units/UnitSpec.cs`, `scripts/core/GameState.cs`, `scripts/BattleRoot.Selection.cs`, `scripts/controllers/SelectionController.Utilities.cs`, `scripts/core/units/runtime/battlefield/UnitBattlefield.CommandBridge.cs`, `scripts/core/units/runtime/battlefield/UnitBattlefield.SyncRuntime.cs`, `scripts/core/entities/UnitSpecEntityBridge.cs`, `tools/ReviewGateRuntime/UnitSpecAbilityAllocationReviewGate.cs`, `TODO.md`.
- Non-goals: 不改变 ability data model、cooldown、active ability execution、production/build rules、balance 或视觉表现。

Implementation summary:
- `UnitSpec` now exposes `HasAbility(...)` and `TryGetAbility(...)` as explicit indexed scans.
- Legacy `GameState`, `BattleRoot`, `SelectionController`, `UnitBattlefield`, and entity bridge ability-kind checks now use the helper instead of `Abilities.Any(...)`.
- `UnitSpecEntityBridge` now builds active ability runtime state with explicit loops, preserving the existing harvested/build/active ability split.
- `ReviewGate regression` locks the runtime/controller no-`Abilities.Any(...)` contract.

Automated gates:
- Command: `dotnet build ProceduralRts.csproj --no-restore`
  Result: pass，0 warnings / 0 errors。
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore -- regression --max-warnings=0`
  Result: pass，0 errors / 0 warnings。

Reviewer result:
- Status: pass
- Required fixes: none.
- Residual risks: Entity bridge active ability initialization still allocates the component-state array by design when a unit has active abilities; this slice only removes LINQ/query allocations and repeated ability-kind predicate paths.

TODO update:
- Items marked done: none，#10 parent remains open.
- Items left open: broader M9 allocation paydown remains profiler-guided and open.
