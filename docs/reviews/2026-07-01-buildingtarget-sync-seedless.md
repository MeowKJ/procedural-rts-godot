# Review Record - BuildingTarget seedless sync

Step:
- UnitBattlefieldBuildingTarget seedless sync cleanup

Milestone:
- M1 EntityWorld authority / Migration cleanup

Owner AI:
- Codex

Reviewer AI:
- Codex; ReviewGate buildingtargetsyncseedless

Integrator AI:
- Codex

Scope:
- Files/folders:
  - scripts/core/units/runtime/UnitBattlefield.cs
  - tools/CombatBehavior/Program.cs
  - tools/ReviewGate/Program.cs
  - TODO.md
  - docs/reviews/2026-07-01-buildingtarget-sync-seedless.md
- Non-goals:
  - Do not delete `_buildingTargetSeedsById`.
  - Do not change public building sync signatures.
  - Do not change construction placement, production, combat, UI, art, or balance.

Implementation summary:
- `SyncBuildingTargetEntity(...)` now treats seed state as optional for existing
  EntityWorld buildings.
- If temporary seed storage is missing but a mapped building entity has
  `BuildingIdentityComponentState`, sync derives a seed from EntityWorld
  transform, identity, and health.
- The seedless path refreshes components without repopulating seed storage.

Automated gates:
- Command: `dotnet build ProceduralRts.csproj --no-restore`
  Result: pass.
  Evidence: build completed successfully with 0 warnings and 0 errors.
- Command: `dotnet build tools/ReviewGate/ReviewGate.csproj --no-restore`
  Result: pass.
  Evidence: build completed successfully with 0 warnings and 0 errors.
- Command: `dotnet run --project tools/CombatBehavior/CombatBehavior.csproj --no-restore`
  Result: pass.
  Evidence: Combat behavior passed with seedless existing-entity sync assertions.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore -- buildingtargetsyncseedless`
  Result: pass.
  Evidence: ReviewGate passed with 0 errors and 0 warnings.
- Command: `dotnet run --project tools/SimReplay/SimReplay.csproj --no-restore`
  Result: pass.
  Evidence: SimReplay PASSED.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore -- review --require-record=buildingtarget-sync-seedless`
  Result: pass.
  Evidence: ReviewGate passed with 0 errors and 0 warnings after evidence backfill.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore`
  Result: pass.
  Evidence: ReviewGate passed with 0 errors and 0 warnings after updating legacy gate expectations for seedless sync.
- Command: `dotnet run --project tools/VerifyAll/VerifyAll.csproj --no-restore`
  Result: pass.
  Evidence: VerifyAll PASSED, 23/23 steps, after the multi-slice batch.

Manual/visual gates:
- Check: Visual/UI review
  Result: not applicable.
  Evidence: This slice changes entity sync authority only.

Reviewer result:
- Status: pass after integration review.
- Required fixes:
  - None currently known.
- Residual risks:
  - `_buildingTargetSeedsById` remains lifecycle/write/sync compatibility storage.
  - Final seed-storage deletion remains a later M1 cleanup target.

TODO update:
- Items marked done:
  - UnitBattlefieldBuildingTarget seedless sync cleanup
- Items left open:
  - Broader Migration cleanup and final seed-storage deletion remain open.
