# Review Record - UnitBattlefieldBuildingTarget combat helper internal id cleanup

Step:
- UnitBattlefieldBuildingTarget combat helper internal id cleanup

Milestone:
- M1 EntityWorld authority

Owner AI:
- Codex

Reviewer AI:
- Cicero the 2nd

Integrator AI:
- Codex

Scope:
- Files/folders:
  - scripts/core/units/runtime/UnitBattlefield.cs
  - tools/ReviewGate/Program.cs
  - TODO.md
  - docs/reviews/2026-06-30-buildingtarget-combat-helper-internal-id.md
- Non-goals:
  - Do not change weapon profiles, damage numbers, target priority, relations, or
    movement behavior.
  - Do not migrate final building wrapper storage, `BuildingSnapshot`, or
    `SyncBuildingTargetEntity`.

Implementation summary:
- Changed selected-building and explicit-building attack filtering to resolve
  `BuildSpec` from the id-resolved target once, then pass that spec into
  `CanUnitTarget(UnitInstance unit, BuildSpec targetSpec)`.
- Replaced wrapper-backed combat legality with
  `CanWeaponTarget(WeaponDefinition weapon, BuildSpec targetSpec)`, delegating to
  `WeaponTargetProfile.CanTarget(BuildSpec target)`.
- Replaced the remaining wrapper-shaped building damage helper with
  `EffectiveDamageAgainst(AmmoDefinition ammo, BuildSpec targetSpec)`.
- Added `ReviewGate buildingtargetcombathelperinternalid` and updated the older
  static-projection gate so it no longer depends on a wrapper-shaped combat helper.

Automated gates:
- Command: `dotnet build ProceduralRts.csproj --no-restore`
  Result: pass
  Evidence: 0 warnings, 0 errors.
- Command: `dotnet build tools/ReviewGate/ReviewGate.csproj --no-restore`
  Result: pass
  Evidence: 0 warnings, 0 errors.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore -- buildingtargetcombathelperinternalid`
  Result: pass
  Evidence: ReviewGate passed with 0 errors, 0 warnings.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore -- buildingtargetstaticprojectiondeleted`
  Result: pass
  Evidence: ReviewGate passed with 0 errors, 0 warnings.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore -- buildingtargetcombatsystembridge`
  Result: pass
  Evidence: ReviewGate passed with 0 errors, 0 warnings.
- Command: `dotnet run --project tools/CombatBehavior/CombatBehavior.csproj --no-restore`
  Result: pass
  Evidence: Combat behavior passed.
- Command: `dotnet run --project tools/SimReplay/SimReplay.csproj --no-restore`
  Result: pass
  Evidence: SimReplay passed.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore -- review --require-record=buildingtarget-combat-helper-internal-id`
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
  Evidence: This slice changes internal combat helper signatures only.

Reviewer result:
- Status: pass after review.
- Required fixes:
  - None.
- Reviewer notes:
  - Cicero the 2nd confirmed the concrete wrapper call sites are the selected and
    explicit building attack filters, and recommended BuildSpec-backed helper
    signatures plus the `buildingtargetcombathelperinternalid` gate.
- Residual risks:
  - `UnitBattlefield` still keeps the private migration wrapper list and snapshot
    conversion during M1.
  - `SyncBuildingTargetEntity(UnitBattlefieldBuildingTarget target)` still accepts
    the wrapper and remains a later migration surface.
  - ReviewGate is string/regex-based rather than semantic type analysis.

TODO update:
- Items marked done:
  - UnitBattlefieldBuildingTarget combat helper internal id cleanup
- Items left open:
  - Building snapshot helper cleanup, sync helper cleanup, private wrapper storage,
    and final legacy `BuildingKind` deletion.
- Reason:
  - Building target combat legality and damage helpers no longer accept the mutable
    `UnitBattlefieldBuildingTarget` wrapper.
