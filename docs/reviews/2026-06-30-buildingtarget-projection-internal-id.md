# Review Record - UnitBattlefieldBuildingTarget internal projection id cleanup

Step:
- UnitBattlefieldBuildingTarget internal projection id cleanup

Milestone:
- M1 EntityWorld authority

Owner AI:
- Codex

Reviewer AI:
- Harvey

Integrator AI:
- Codex

Scope:
- Files/folders:
  - scripts/core/units/runtime/UnitBattlefield.cs
  - tools/ReviewGate/Program.cs
  - TODO.md
  - docs/reviews/2026-06-30-buildingtarget-projection-internal-id.md
- Non-goals:
  - Do not delete private building wrapper storage.
  - Do not migrate production, dock, power, weapon, or repair helpers in this slice.
  - Do not alter public projection API shapes.
  - Do not tune balance, movement, UI art, or fog.

Implementation summary:
- Changed `BuildingIdentity(...)` to resolve by building id and prefer
  `BuildingIdentityComponentState` from EntityWorld.
- Changed private selection, hit-pulse, and minimap projection helpers to accept
  building ids instead of `UnitBattlefieldBuildingTarget` wrappers.
- Updated view and hover projections to resolve identity through
  `BuildingIdentity(int buildingId)`.
- Added `ReviewGate buildingtargetprojectioninternalid` and updated historical
  identity/HUD gates to expect projected identity instead of wrapper parameters.

Automated gates:
- Command: `dotnet build ProceduralRts.csproj --no-restore`
  Result: pass
  Evidence: 0 warnings, 0 errors.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore -- buildingtargetidentitycomponent`
  Result: pass
  Evidence: ReviewGate passed with 0 errors, 0 warnings.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore -- buildingselectionhud`
  Result: pass
  Evidence: ReviewGate passed with 0 errors, 0 warnings.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore -- buildinghoverprojection`
  Result: pass
  Evidence: ReviewGate passed with 0 errors, 0 warnings.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore -- buildinghitpulseprojection`
  Result: pass
  Evidence: ReviewGate passed with 0 errors, 0 warnings.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore -- buildingminimapprojection`
  Result: pass
  Evidence: ReviewGate passed with 0 errors, 0 warnings.
- Command: `dotnet run --project tools/CombatBehavior/CombatBehavior.csproj --no-restore`
  Result: pass
  Evidence: Combat behavior passed.
- Command: `dotnet run --project tools/SimReplay/SimReplay.csproj --no-restore`
  Result: pass
  Evidence: SimReplay PASSED.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore -- buildingtargetprojectioninternalid`
  Result: pass
  Evidence: ReviewGate passed with 0 errors, 0 warnings.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore -- review --require-record=buildingtarget-projection-internal-id`
  Result: pass
  Evidence: ReviewGate passed with 0 errors, 0 warnings.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore`
  Result: pass
  Evidence: ReviewGate passed with 0 errors, 0 warnings.
- Command: `dotnet run --project tools/VerifyAll/VerifyAll.csproj --no-restore`
  Result: pass
  Evidence: VerifyAll PASSED, 23/23 steps.

Manual/visual gates:
- Check: Visual/UI review
  Result: not applicable
  Evidence: Projection helper parameters changed internally; public projection APIs
  and rendering surfaces are unchanged.

Reviewer result:
- Status: pass-with-warnings
- Required fixes:
  - None.
- Residual risks:
  - The new gate is intentionally string-based and may reject equivalent rewrites
    until the wrapper migration is complete.
  - `BuildingViewProjection(int id)` now trusts EntityWorld identity/presentation
    rather than explicitly requiring the migration wrapper first; this matches the
    M1 authority direction but is a slightly wider read path during migration.
  - Legacy private helpers for production, dock, power, weapon, and repair still
    accept the migration wrapper and remain future M1 slices.

TODO update:
- Items marked done:
  - UnitBattlefieldBuildingTarget internal projection id cleanup
- Items left open:
  - Broader deletion of `UnitBattlefieldBuildingTarget` storage and remaining
    private helper parameters.
- Reason:
  - This slice only removes wrapper flow from internal projection helpers.
