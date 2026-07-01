# Review Record - UnitBattlefieldBuildingTarget BuildSpec helper deletion

Step:
- UnitBattlefieldBuildingTarget BuildSpec helper deletion

Milestone:
- M1 EntityWorld authority

Owner AI:
- Codex

Reviewer AI:
- Peirce the 2nd

Integrator AI:
- Codex

Scope:
- Files/folders:
  - scripts/core/units/runtime/UnitBattlefield.cs
  - tools/ReviewGate/Program.cs
  - TODO.md
  - docs/reviews/2026-06-30-buildingtarget-buildspec-helper-deleted.md
- Non-goals:
  - Do not change BuildSpecCatalog data, weapon target legality, damage, death
    event payload shape, or building snapshot payload shape.
  - Do not migrate combat targeting helpers, snapshot helpers, or final wrapper
    storage.

Implementation summary:
- Deleted `BuildingSpec(UnitBattlefieldBuildingTarget building)`.
- Updated building combat weapon checks to read
  `BuildSpecCatalog.For(building.Kind).WeaponKind` directly.
- Updated building death info footprint payloads to read
  `BuildSpecCatalog.For(building.Kind).Footprint` directly.
- Updated historical ReviewGate checks so the old helper cannot return.
- Added `ReviewGate buildingtargetbuildspechelperdeleted`.

Automated gates:
- Command: `dotnet build ProceduralRts.csproj --no-restore`
  Result: pass
  Evidence: 0 warnings, 0 errors.
- Command: `dotnet build tools/ReviewGate/ReviewGate.csproj --no-restore`
  Result: pass
  Evidence: 0 warnings, 0 errors.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore -- buildingtargetbuildspechelperdeleted`
  Result: pass
  Evidence: ReviewGate passed with 0 errors, 0 warnings.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore -- buildingtargetstaticprojectiondeleted`
  Result: pass
  Evidence: ReviewGate passed with 0 errors, 0 warnings.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore -- buildingtargetdeathinternalid`
  Result: pass
  Evidence: ReviewGate passed with 0 errors, 0 warnings.
- Command: `dotnet run --project tools/CombatBehavior/CombatBehavior.csproj --no-restore`
  Result: pass
  Evidence: Combat behavior passed.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore -- review --require-record=buildingtarget-buildspec-helper-deleted`
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
  Evidence: This slice changes internal BuildSpec read plumbing only.

Reviewer result:
- Status: pass after review.
- Required fixes:
  - None.
- Reviewer notes:
  - Peirce the 2nd confirmed the direct `BuildSpecCatalog.For(building.Kind)`
    weapon/footprint reads are equivalent to the deleted helper and did not change
    data source or branch behavior.
- Residual risks:
  - Building target combat and snapshot helpers still accept the temporary migration
    wrapper during M1.
  - ReviewGate is string/regex-based rather than semantic type analysis.

TODO update:
- Items marked done:
  - UnitBattlefieldBuildingTarget BuildSpec helper deletion
- Items left open:
  - Combat targeting helpers, snapshot helper cleanup, and final wrapper deletion
    migrations.
- Reason:
  - This slice removes the wrapper-shaped BuildSpec helper while preserving direct
    BuildSpecCatalog static data reads.
