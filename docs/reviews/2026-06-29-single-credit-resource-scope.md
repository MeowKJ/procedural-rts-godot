# Review Record - Single credit resource scope

Step:
Lock the current playable slice to one banked economic resource: Credits.

Milestone:
Design Reference - Resource, Mining & Environment Regeneration.

Owner AI:
Codex main agent.

Reviewer AI:
Codex main-agent review with `ReviewGate resourcescope` and full `ReviewGate`.

Integrator AI:
Codex main agent.

Scope:
- Files/folders:
  - `scripts/core/ResourceInventory.cs`
  - `scripts/core/ResourceFieldModel.cs`
  - `scripts/core/ProductionDefinition.cs`
  - `scripts/core/BuildDefinition.cs`
  - `scripts/core/GameState.cs`
  - `scripts/core/units/runtime/UnitBattlefield.cs`
  - `scripts/core/entities/EntityComponentState.cs`
  - `tools/ReviewGate/Program.cs`
  - `TODO.md`
  - `docs/reviews/2026-06-29-single-credit-resource-scope.md`
- Non-goals:
  - Do not implement the future pure `ResourceSystem`.
  - Do not mark the full mining loop, regeneration, or economy metrics complete.
  - Do not remove extension room for a future rare resource after this slice.

Implementation summary:
- Added `ReviewGate resourcescope`.
- The gate verifies that `ResourceInventory` banks only `Credits`.
- The gate verifies production/build definitions use one `Cost` field.
- The gate verifies legacy harvesting and production paths spend/unload Credits.
- The gate verifies resource fields and entity cargo remain single-channel
  amount/cargo-capacity models.
- The gate rejects source hooks for typed or secondary resources before this slice
  intentionally expands scope.

Automated gates:
- Command:
  `dotnet run --project tools/ReviewGate/ReviewGate.csproj resourcescope --no-restore`
  Result:
  Pass.
  Evidence:
  ReviewGate reported 0 errors and 0 warnings.
- Command:
  `dotnet run --project tools/ReviewGate/ReviewGate.csproj review --require-record=single-credit-resource-scope --no-restore`
  Result:
  Pass.
  Evidence:
  ReviewGate reported 0 errors and 0 warnings.
- Command:
  `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore`
  Result:
  Pass.
  Evidence:
  ReviewGate reported 0 errors and 0 warnings.
- Command:
  `dotnet run --project tools/VerifyAll/VerifyAll.csproj --no-restore`
  Result:
  Pass.
  Evidence:
  VerifyAll passed all 14 steps: build, SimReplay, CombatBehavior,
  SimulationSmoke, FogOfWarQa, SelectionStress, AiDifficultySmoke, ReviewGate,
  PerfSmoke, BalanceReport, and Godot headless QA scenes.

Manual/visual gates:
- Check:
  Visual/resource UI inspection.
  Result:
  Not required.
  Evidence:
  This is a source-scope and data-model guard; it does not alter runtime visuals.

Reviewer result:
- Status: pass.
- Required fixes:
  - None at record creation.
- Residual risks:
  - The complete pure `ResourceSystem`, mining-loop congestion tests, regeneration,
    and economy metrics remain open TODO work.
  - Power is intentionally not treated as an economic resource by this gate; it
    remains a separate base constraint.

TODO update:
- Items marked done:
  - `Multiple resource types optional later; this slice ships ONE credit resource`.
- Items left open:
  - Mining loop in pure `ResourceSystem`.
  - ResourceNode data expansion.
  - Environment resource regeneration.
  - Economy metrics in `SimMetrics`.
  - Deterministic economy tests.
- Reason:
  - The current source and new `resourcescope` gate prove the playable slice has one
    banked economic resource and no typed/secondary-resource hooks.
