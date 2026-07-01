# Review Record - BuildingTarget adoption seedless

Step:
- UnitBattlefieldBuildingTarget adoption seedless cleanup

Milestone:
- M1 EntityWorld authority / Migration cleanup

Owner AI:
- Codex

Reviewer AI:
- Codex; ReviewGate buildingtargetadoptionseedless

Integrator AI:
- Codex

Scope:
- Files/folders:
  - scripts/core/units/runtime/UnitBattlefield.cs
  - tools/CombatBehavior/Program.cs
  - tools/ReviewGate/Program.cs
  - TODO.md
  - docs/reviews/2026-07-01-buildingtarget-adoption-seedless.md
- Non-goals:
  - Do not delete `_buildingTargetSeedsById`.
  - Do not change construction placement, production, combat, UI, art, or balance.
  - Do not change public construction or building snapshot semantics.

Implementation summary:
- `AdoptConstructedBuildingId(...)` no longer calls the temporary
  `AddBuildingTarget(...)` seed writer.
- Constructed buildings are mapped directly through `SetBuildingTargetEntityId(...)`
  and receive `BuildingIdentityComponentState` on the EntityWorld entity.
- The unused `AddBuildingTarget(...)` helper was deleted.

Automated gates:
- Command: `dotnet build ProceduralRts.csproj --no-restore`
  Result: pass.
  Evidence: build completed successfully with 0 warnings and 0 errors.
- Command: `dotnet build tools/ReviewGate/ReviewGate.csproj --no-restore`
  Result: pass.
  Evidence: build completed successfully with 0 warnings and 0 errors.
- Command: `dotnet run --project tools/CombatBehavior/CombatBehavior.csproj --no-restore`
  Result: pass.
  Evidence: Combat behavior passed with seedless adoption no-repopulation assertions.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore -- buildingtargetadoptionseedless`
  Result: pass.
  Evidence: ReviewGate passed with 0 errors and 0 warnings.
- Command: `dotnet run --project tools/SimReplay/SimReplay.csproj --no-restore`
  Result: pass.
  Evidence: SimReplay PASSED.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore`
  Result: pass.
  Evidence: ReviewGate passed with 0 errors and 0 warnings after updating legacy gate expectations for deleted AddBuildingTarget.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore -- review --require-record=buildingtarget-adoption-seedless`
  Result: pass.
  Evidence: ReviewGate passed with 0 errors and 0 warnings after evidence backfill.
- Command: `dotnet run --project tools/VerifyAll/VerifyAll.csproj --no-restore`
  Result: pass.
  Evidence: VerifyAll PASSED, 23/23 steps, after the multi-slice batch.

Manual/visual gates:
- Check: Visual/UI review
  Result: not applicable.
  Evidence: This slice changes adoption/write authority only.

Reviewer result:
- Status: pass after integration review.
- Required fixes:
  - None currently known.
- Residual risks:
  - `_buildingTargetSeedsById` remains compatibility storage but now has no normal
    runtime writer.
  - Final seed-storage deletion remains a later M1 cleanup target.

TODO update:
- Items marked done:
  - UnitBattlefieldBuildingTarget adoption seedless cleanup
- Items left open:
  - Broader Migration cleanup and final seed-storage deletion remain open.
