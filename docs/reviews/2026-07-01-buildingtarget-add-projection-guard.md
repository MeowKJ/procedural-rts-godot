# Review Record - BuildingTarget add projection guard

Step:
- UnitBattlefieldBuildingTarget add projection guard cleanup

Milestone:
- M1 EntityWorld authority / Migration cleanup

Owner AI:
- Codex

Reviewer AI:
- Codex; ReviewGate buildingtargetaddprojectionguard

Integrator AI:
- Codex

Scope:
- Files/folders:
  - scripts/core/units/runtime/UnitBattlefield.cs
  - tools/CombatBehavior/Program.cs
  - tools/ReviewGate/Program.cs
  - TODO.md
  - docs/reviews/2026-07-01-buildingtarget-add-projection-guard.md
- Non-goals:
  - Do not delete `_buildingTargetSeedsById`.
  - Do not change public building id semantics.
  - Do not change construction placement, production, combat, UI, art, or balance.

Implementation summary:
- `AddBuildingTarget(...)` now routes duplicate-id checks through
  `BuildingTargetIdInUse(target.Id)`.
- The shared helper rejects ids already present as EntityWorld
  `BuildingIdentityComponentState` entries and keeps seed storage as a migration
  compatibility guard.
- `CombatBehavior` removes seed storage, invokes the private add helper through
  reflection with an existing EntityWorld building id, and proves the duplicate
  is rejected.

Automated gates:
- Command: `dotnet build ProceduralRts.csproj --no-restore`
  Result: pass.
  Evidence: build completed successfully with 0 warnings and 0 errors.
- Command: `dotnet build tools/ReviewGate/ReviewGate.csproj --no-restore`
  Result: pass.
  Evidence: build completed successfully with 0 warnings and 0 errors.
- Command: `dotnet run --project tools/CombatBehavior/CombatBehavior.csproj --no-restore`
  Result: pass.
  Evidence: Combat behavior passed with seedless add-guard assertions.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore -- buildingtargetaddprojectionguard`
  Result: pass.
  Evidence: ReviewGate passed with 0 errors and 0 warnings.
- Command: `dotnet run --project tools/SimReplay/SimReplay.csproj --no-restore`
  Result: pass.
  Evidence: SimReplay PASSED.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore -- review --require-record=buildingtarget-add-projection-guard`
  Result: pass.
  Evidence: ReviewGate passed with 0 errors and 0 warnings after evidence backfill.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore`
  Result: pass.
  Evidence: ReviewGate passed with 0 errors and 0 warnings.
- Command: `dotnet run --project tools/VerifyAll/VerifyAll.csproj --no-restore`
  Result: pass.
  Evidence: VerifyAll PASSED, 23/23 steps, after the multi-slice batch.

Manual/visual gates:
- Check: Visual/UI review
  Result: not applicable.
  Evidence: This slice changes duplicate-id authority only.

Reviewer result:
- Status: pass after integration review.
- Required fixes:
  - None currently known.
- Residual risks:
  - `_buildingTargetSeedsById` remains lifecycle/write/sync compatibility storage.
  - Final seed-storage deletion remains a later M1 cleanup target.

TODO update:
- Items marked done:
  - UnitBattlefieldBuildingTarget add projection guard cleanup
- Items left open:
  - Broader Migration cleanup and final seed-storage deletion remain open.
