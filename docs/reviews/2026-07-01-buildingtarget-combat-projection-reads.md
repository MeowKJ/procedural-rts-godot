# Review Record - UnitBattlefieldBuildingTarget combat projection read cleanup

Step:
- UnitBattlefieldBuildingTarget combat projection read cleanup

Milestone:
- M1 EntityWorld authority

Owner AI:
- Codex

Reviewer AI:
- Codex

Integrator AI:
- Codex

Scope:
- Files/folders:
  - scripts/core/units/runtime/UnitBattlefield.cs
  - tools/ReviewGate/Program.cs
  - TODO.md
  - docs/reviews/2026-07-01-buildingtarget-combat-projection-reads.md
- Non-goals:
  - Do not change turret target selection, weapon cooldowns, projectile events,
    damage, death cleanup, or combat balance.
  - Do not migrate unit-vs-building combat or turret combat systems in this slice.
  - Do not remove the private migration wrapper list yet.

Implementation summary:
- Changed `UpdateBuildingCombatFromEntityWorld(...)` to decide whether armed
  building combat should step by enumerating `BuildingTargetIds()`.
- Resolved building kind through `BuildingIdentity(int)` before reading
  `BuildSpecCatalog.For(identity.Kind).WeaponKind`.
- Preserved the existing early return and `_turretCombatSystem.Step(...)` behavior.
- Added `ReviewGate buildingtargetcombatprojectionreads`.
- Updated historical BuildSpec ReviewGate checks to require the new
  `BuildSpecCatalog.For(identity.Kind).WeaponKind` read path.

Automated gates:
- Command: `dotnet build ProceduralRts.csproj --no-restore`
  Result: pass
  Evidence: 0 warnings, 0 errors.
- Command: `dotnet build tools/ReviewGate/ReviewGate.csproj --no-restore`
  Result: pass
  Evidence: 0 warnings, 0 errors.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore -- buildingtargetcombatprojectionreads`
  Result: pass
  Evidence: ReviewGate passed with 0 errors, 0 warnings.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore -- buildingtargetstaticprojectiondeleted`
  Result: pass
  Evidence: ReviewGate passed with 0 errors, 0 warnings.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore -- buildingtargetbuildspechelperdeleted`
  Result: pass
  Evidence: ReviewGate passed with 0 errors, 0 warnings.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore -- turretcombatsystembridge`
  Result: pass
  Evidence: ReviewGate passed with 0 errors, 0 warnings.
- Command: `dotnet run --project tools/CombatBehavior/CombatBehavior.csproj --no-restore`
  Result: pass
  Evidence: Combat behavior passed.
- Command: `dotnet run --project tools/SimReplay/SimReplay.csproj --no-restore`
  Result: pass
  Evidence: SimReplay PASSED.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore -- review --require-record=buildingtarget-combat-projection-reads`
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
  Evidence: This slice changes the internal combat active-check read source only.

Reviewer result:
- Status: pass after integration review.
- Required fixes:
  - None.
- Reviewer notes:
  - The active armed-building check now follows the same id/identity projection
    path as other migrated building reads.
  - The actual turret combat system and event handling remain unchanged.
- Residual risks:
  - `BuildingTargetIds()` still has a wrapper fallback during the M1 migration
    window.
  - Direct private `Buildings` reads remain in unrelated construction,
    fog/visibility, placement, owner-relation, dock/refinery, and cleanup paths.
  - ReviewGate is string/regex-based rather than semantic type analysis.

TODO update:
- Items marked done:
  - UnitBattlefieldBuildingTarget combat projection read cleanup
- Items left open:
  - Remaining non-combat direct wrapper-list reads in construction,
    fog/visibility, placement, owner-relation, dock/refinery, and cleanup paths.
- Reason:
  - The building combat tick no longer scans the second building runtime list just
    to find armed building kinds.
