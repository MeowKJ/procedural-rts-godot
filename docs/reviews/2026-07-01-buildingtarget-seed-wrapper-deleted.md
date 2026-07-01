# Review Record - UnitBattlefieldBuildingTarget seed wrapper deletion

Step:
- UnitBattlefieldBuildingTarget seed wrapper deletion

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
  - scripts/core/units/runtime/UnitBattlefieldBuildingTarget.cs
  - scripts/core/entities/BuildingEntitySeed.cs
  - tools/ReviewGate/Program.cs
  - TODO.md
  - docs/reviews/2026-07-01-buildingtarget-seed-wrapper-deleted.md
- Non-goals:
  - Do not change unit or building balance.
  - Do not change movement feel, formation behavior, fog, UI, art, roster data, or
    production rules.
  - Do not collapse `BuildingEntitySeed` into EntityWorld ownership yet.

Implementation summary:
- Deleted `UnitBattlefieldBuildingTarget.cs`.
- Replaced the final private wrapper dictionary with
  `Dictionary<int, BuildingEntitySeed> _buildingTargetSeedsById`.
- Kept `BuildingTargetIds()` deterministic by preferring EntityWorld ordered
  building identities and falling back to sorted seed ids.
- Changed building sync to pass `BuildingEntitySeed` directly into
  `BuildingTargetEntityBridge` instead of calling `ToEntitySeed()`.
- Changed EntityWorld health sync to update seed storage immutably with
  `building with { Hp = health.Hp }`.
- Added `ReviewGate buildingtargetseedwrapperdeleted` and updated historical
  wrapper-era gates to reject wrapper reintroduction.

Automated gates:
- Command: `dotnet build ProceduralRts.csproj --no-restore`
  Result: pass
  Evidence: 0 warnings, 0 errors.
- Command: `dotnet build tools/ReviewGate/ReviewGate.csproj --no-restore`
  Result: pass
  Evidence: 0 warnings, 0 errors.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore -- buildingtargetseedwrapperdeleted`
  Result: pass
  Evidence: ReviewGate passed with 0 errors, 0 warnings.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore -- buildingtargetorderedlistdeleted`
  Result: pass
  Evidence: ReviewGate passed with 0 errors, 0 warnings.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore -- buildingtargetseedbridge`
  Result: pass
  Evidence: ReviewGate passed with 0 errors, 0 warnings.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore -- buildingtargetsyncinternalid`
  Result: pass
  Evidence: ReviewGate passed with 0 errors, 0 warnings.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore -- buildingtargetidprojectionreads`
  Result: pass
  Evidence: ReviewGate passed with 0 errors, 0 warnings.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore -- buildingtargetlookupindexed`
  Result: pass
  Evidence: ReviewGate passed with 0 errors, 0 warnings.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore -- buildingtargetsnapshotinternalid`
  Result: pass
  Evidence: ReviewGate passed with 0 errors, 0 warnings.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore -- buildingtargetsnapshotprojection`
  Result: pass
  Evidence: ReviewGate passed with 0 errors, 0 warnings.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore -- buildingtargethealthinternalid`
  Result: pass
  Evidence: ReviewGate passed with 0 errors, 0 warnings.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore -- buildingtargetdeathinternalid`
  Result: pass
  Evidence: ReviewGate passed with 0 errors, 0 warnings.
- Command: `dotnet run --project tools/SimReplay/SimReplay.csproj --no-restore`
  Result: pass
  Evidence: SimReplay PASSED.
- Command: `dotnet run --project tools/CombatBehavior/CombatBehavior.csproj --no-restore`
  Result: pass
  Evidence: Combat behavior passed.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore -- review --require-record=buildingtarget-seed-wrapper-deleted`
  Result: pass
  Evidence: ReviewGate passed with 0 errors, 0 warnings after evidence backfill.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore`
  Result: pass
  Evidence: ReviewGate passed with 0 errors, 0 warnings.
- Command: `dotnet run --project tools/VerifyAll/VerifyAll.csproj --no-restore`
  Result: pass
  Evidence: VerifyAll PASSED, 23/23 steps.

Manual/visual gates:
- Check: Visual/UI review
  Result: not applicable
  Evidence: This slice changes private building target migration storage only.

Reviewer result:
- Status: pass after integration review.
- Required fixes:
  - None.
- Reviewer notes:
  - The deleted `UnitBattlefieldBuildingTarget` wrapper no longer duplicates
    `BuildingEntitySeed` fields.
  - The remaining seed dictionary is still migration storage, not a second
    gameplay-authoritative building runtime.
- Residual risks:
  - ReviewGate remains string/regex-based rather than semantic type analysis.
  - `BuildingEntitySeed` is still temporary migration storage until building
    creation can be fully EntityWorld-authored.

TODO update:
- Items marked done:
  - UnitBattlefieldBuildingTarget seed wrapper deletion
- Items left open:
  - Full EntityWorld-authored building creation remains future work.
- Reason:
  - The duplicated `UnitBattlefieldBuildingTarget` wrapper is deleted and the
    remaining temporary building migration state is direct `BuildingEntitySeed`
    storage behind a deterministic id index.
