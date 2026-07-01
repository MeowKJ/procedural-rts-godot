# Review Record - BuildingTarget adoption seed guard deleted

Step:
- UnitBattlefieldBuildingTarget adoption seed guard deletion

Milestone:
- M1 EntityWorld authority / Migration cleanup

Owner AI:
- Codex

Reviewer AI:
- Codex; ReviewGate buildingtargetadoptseedguarddeleted

Integrator AI:
- Codex

Scope:
- Files/folders:
  - scripts/core/units/runtime/UnitBattlefield.cs
  - tools/CombatBehavior/Program.cs
  - tools/ReviewGate/Program.cs
  - TODO.md
  - docs/reviews/2026-07-01-buildingtarget-adopt-seed-guard-deleted.md
- Non-goals:
  - Do not change construction placement behavior.
  - Do not change building id allocation.
  - Do not delete `_buildingTargetSeedsById`.

Implementation summary:
- Existing constructed-building adoption now reuses the reverse EntityWorld
  entity index directly instead of validating the id through temporary seed
  storage.
- Adoption restores missing `BuildingIdentityComponentState` and ensures producer
  queue components on existing mapped entities.
- `CombatBehavior` proves seedless adoption reuses the existing id and restores
  EntityWorld identity.

Automated gates:
- Command: `dotnet build ProceduralRts.csproj --no-restore`
  Result: pass.
  Evidence: build completed successfully with 0 warnings and 0 errors.
- Command: `dotnet build tools/ReviewGate/ReviewGate.csproj --no-restore`
  Result: pass.
  Evidence: build completed successfully with 0 warnings and 0 errors.
- Command: `dotnet run --project tools/CombatBehavior/CombatBehavior.csproj --no-restore`
  Result: pass.
  Evidence: Combat behavior passed with seedless adoption identity restoration.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore -- buildingtargetadoptseedguarddeleted`
  Result: pass.
  Evidence: ReviewGate accepted reverse-index adoption without seed validation.
- Command: `dotnet run --project tools/SimReplay/SimReplay.csproj --no-restore`
  Result: pass.
  Evidence: deterministic replay suite passed after adoption seed guard deletion.
- Command: `dotnet run --project tools/VerifyAll/VerifyAll.csproj --no-restore`
  Result: pass.
  Evidence: VerifyAll PASSED, 23/23 steps.

Manual/visual gates:
- Check: Visual/UI review
  Result: not applicable.
  Evidence: This slice changes adoption read authority only.

Reviewer result:
- Status: pass after integration review.
- Required fixes:
  - None currently known.
- Residual risks:
  - `_buildingTargetSeedsById` remains lifecycle/write/sync compatibility storage.
  - Full construction placement behavior is covered by replay gates, not changed
    directly here.

TODO update:
- Items marked done:
  - UnitBattlefieldBuildingTarget adoption seed guard deletion
- Items left open:
  - Broader Migration cleanup and final seed-storage deletion remain open.
