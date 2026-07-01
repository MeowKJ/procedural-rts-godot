# Review Record - UnitBattlefieldBuildingTarget internal wrapper visibility cleanup

Step: UnitBattlefieldBuildingTarget internal wrapper visibility cleanup
Milestone: M1 EntityWorld Becomes Authoritative / BuildSpec building-runtime cleanup
Owner AI: Codex
Reviewer AI: ReviewGate buildingtargetinternalwrapper / Integrator
Integrator AI: Codex

Scope:
- Files/folders: `scripts/core/units/runtime/UnitBattlefieldBuildingTarget.cs`, `scripts/core/units/runtime/UnitBattlefield.cs`, `tools/CombatBehavior/Program.cs`, `tools/SimReplay/Program.cs`, `tools/ReviewGate/Program.cs`, `TODO.md`, `docs/reviews/2026-06-30-buildingtarget-internal-wrapper.md`.
- Non-goals: deleting the internal wrapper, replacing every private `UnitBattlefield` helper, changing building creation behavior, changing balance, or changing visuals.

Implementation summary:
- Changed `UnitBattlefieldBuildingTarget` from public to `internal sealed class UnitBattlefieldBuildingTarget` so it is no longer a public runtime type.
- Changed `ConstructBuilding(...)` to return `out UnitBattlefieldBuildingSnapshot?` instead of an out wrapper.
- Updated CombatBehavior bridge fixtures and SimReplay turret projection fixtures to use `BuildingEntitySeed` directly instead of constructing `UnitBattlefieldBuildingTarget`.
- Added `ReviewGate buildingtargetinternalwrapper` to lock the boundary.

Automated gates:
- Command: `dotnet build ProceduralRts.csproj --no-restore`
  Result: pass
  Evidence: build completed successfully with 0 warnings and 0 errors.
- Command: `dotnet run --project tools/CombatBehavior/CombatBehavior.csproj --no-restore`
  Result: pass
  Evidence: CombatBehavior completed successfully.
- Command: `dotnet run --project tools/SimReplay/SimReplay.csproj --no-restore`
  Result: pass
  Evidence: SimReplay completed successfully.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore -- buildingtargetinternalwrapper`
  Result: pass
  Evidence: narrow ReviewGate mode completed successfully with 0 errors and 0 warnings.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore`
  Result: pass
  Evidence: full ReviewGate completed successfully with 0 errors and 0 warnings.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore -- review --require-record=buildingtarget-internal-wrapper`
  Result: pass
  Evidence: review-record gate completed successfully with 0 errors and 0 warnings.
- Command: `dotnet run --project tools/VerifyAll/VerifyAll.csproj --no-restore`
  Result: pass
  Evidence: full VerifyAll completed successfully, 23/23 steps passed.

Manual/visual gates:
- Check: visual inspection not required.
  Result: not run.
  Evidence: type-visibility and fixture cleanup only; runtime behavior remains covered by automated gates.

Reviewer result:
- Status: pass for build, CombatBehavior, SimReplay, narrow gate, full ReviewGate, review-record gate, and VerifyAll.
- Required fixes: none.
- Residual risks: the wrapper still exists internally as migration state for private `UnitBattlefield` helpers until the building runtime can be fully collapsed into EntityWorld components/projections.

TODO update:
- Items marked done: `UnitBattlefieldBuildingTarget internal wrapper visibility cleanup`.
- Items left open: broader internal building-runtime migration cleanup and final `BuildingKind`/entity-spec legacy deletion remain open.
- Reason: the wrapper is no longer public, but private migration code still uses it.
