# Review Record - UnitBattlefieldBuildingTarget clear attack target internal id cleanup

Step:
- UnitBattlefieldBuildingTarget clear attack target internal id cleanup

Milestone:
- M1 EntityWorld authority

Owner AI:
- Codex

Reviewer AI:
- Helmholtz the 2nd

Integrator AI:
- Codex

Scope:
- Files/folders:
  - scripts/core/units/runtime/UnitBattlefield.cs
  - tools/ReviewGate/Program.cs
  - TODO.md
  - docs/reviews/2026-06-30-buildingtarget-clear-attack-internal-id.md
- Non-goals:
  - Do not change target acquisition, weapon cooldowns, damage, target legality, or
    `WeaponUserComponentState` schema.
  - Do not migrate combat targeting helpers, snapshot helpers, or final building
    wrapper storage.
  - Do not change UI, VFX, or command feedback.

Implementation summary:
- Changed building attack-target clearing from a
  `UnitBattlefieldBuildingTarget` helper to
  `ClearBuildingAttackTargetCore(int buildingId)`.
- Updated dead-unit cleanup to clear building targets by `building.Id`.
- Preserved `WeaponUserComponentState` reset behavior:
  `AttackTarget = EntityId.None`, `AttackTargetKind = CombatTargetKind.Unit`, and
  `AttackTargetIsManual = false`.
- Added `ReviewGate buildingtargetclearattackinternalid`.

Automated gates:
- Command: `dotnet build ProceduralRts.csproj --no-restore`
  Result: pass
  Evidence: 0 warnings, 0 errors.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore -- buildingtargetclearattackinternalid`
  Result: pass
  Evidence: ReviewGate passed with 0 errors, 0 warnings.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore -- buildingtargetweaponreadinternalid`
  Result: pass
  Evidence: ReviewGate passed with 0 errors, 0 warnings.
- Command: `dotnet run --project tools/CombatBehavior/CombatBehavior.csproj --no-restore`
  Result: pass
  Evidence: Combat behavior passed.
- Command: `dotnet run --project tools/SimReplay/SimReplay.csproj --no-restore`
  Result: pass
  Evidence: SimReplay PASSED.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore -- review --require-record=buildingtarget-clear-attack-internal-id`
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
  Evidence: This slice changes internal combat cleanup helper parameters only.

Reviewer result:
- Status: pass on static implementation shape; fail-for-completion until evidence
  was recorded.
- Required fixes:
  - Helmholtz the 2nd noted the review record still had pending evidence and TODO
    was still open before final gates were recorded. Fixed by recording reviewer,
    integrator gate evidence, and the completed TODO update.
- Residual risks:
  - The helper still resolves EntityWorld mirror ids through the migration
    `_buildingTargetEntityIds` map during M1.
  - Broader combat targeting helpers still accept the migration wrapper and remain
    future M1 cleanup slices.
  - ReviewGate is string/regex-based rather than semantic type analysis.

TODO update:
- Items marked done:
  - UnitBattlefieldBuildingTarget clear attack target internal id cleanup
- Items left open:
  - Combat targeting helpers, snapshot helpers, and final wrapper deletion
    migrations.
- Reason:
  - This slice only removes wrapper flow from internal building attack-target
    clearing while preserving WeaponUserComponentState reset behavior.
