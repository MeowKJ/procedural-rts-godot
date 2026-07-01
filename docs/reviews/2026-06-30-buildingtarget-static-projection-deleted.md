# Review Record - UnitBattlefieldBuildingTarget static projection deletion

Step: UnitBattlefieldBuildingTarget static projection deletion
Milestone: M1 EntityWorld Becomes Authoritative / BuildSpec building-runtime cleanup
Owner AI: Codex
Reviewer AI: ReviewGate buildingtargetstaticprojectiondeleted / Integrator
Integrator AI: Codex

Scope:
- Files/folders: `scripts/core/units/runtime/UnitBattlefieldBuildingTarget.cs`, `scripts/core/units/runtime/UnitBattlefield.cs`, `scripts/BattleRoot.cs`, `tools/CombatBehavior/Program.cs`, `tools/ReviewGate/Program.cs`, `TODO.md`, `docs/reviews/2026-06-30-buildingtarget-static-projection-deleted.md`.
- Non-goals: deleting `UnitBattlefieldBuildingTarget`, changing `BuildingKind`, changing BuildSpec values, changing production behavior, changing building art/UI layout, or changing combat balance.

Implementation summary:
- Removed `MaxHp`, `Footprint`, `ArmorTag`, `WeaponKind`, and the private `BuildSpec` projection from `UnitBattlefieldBuildingTarget`.
- Replaced runtime reads of those convenience projections with direct `BuildSpecCatalog.For(kind)` lookups or a local `UnitBattlefield.BuildingSpec(...)` helper.
- Updated BattleRoot building-impact VFX and CombatBehavior building QA to read static building data from BuildSpec instead of the target wrapper.
- Updated ReviewGate historical checks and added `buildingtargetstaticprojectiondeleted` to prevent the projection properties from returning.

Automated gates:
- Command: `dotnet build ProceduralRts.csproj --no-restore`
  Result: pass
  Evidence: build completed successfully with 0 warnings and 0 errors.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore -- buildingtargetstaticprojectiondeleted`
  Result: pass
  Evidence: narrow ReviewGate mode completed successfully with 0 errors and 0 warnings.
- Command: `dotnet run --project tools/CombatBehavior/CombatBehavior.csproj --no-restore`
  Result: pass
  Evidence: CombatBehavior completed successfully.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore`
  Result: pass
  Evidence: full ReviewGate completed successfully with 0 errors and 0 warnings.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore -- review --require-record=buildingtarget-static-projection-deleted`
  Result: pass
  Evidence: review-record gate completed successfully with 0 errors and 0 warnings.
- Command: `dotnet run --project tools/VerifyAll/VerifyAll.csproj --no-restore`
  Result: pass
  Evidence: VerifyAll completed 23/23 steps successfully, including build, SimReplay, CombatBehavior, FogOfWarQa, PerfSmoke, ReviewGate, and Godot headless QA.

Manual/visual gates:
- Check: visual inspection not required.
  Result: not run.
  Evidence: architecture cleanup only; no rendering or layout behavior changed.

Reviewer result:
- Status: pass.
- Required fixes: none.
- Residual risks: `UnitBattlefieldBuildingTarget` remains as the second building runtime wrapper until id/projection APIs fully replace wrapper APIs.

TODO update:
- Items marked done: `UnitBattlefieldBuildingTarget static projection deletion`.
- Items left open: broader building-runtime migration cleanup and final `BuildingKind`/entity-spec legacy deletion remain open.
- Reason: the target wrapper is thinner, but not deleted.
