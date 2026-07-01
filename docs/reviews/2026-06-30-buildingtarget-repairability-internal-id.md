# Review Record - UnitBattlefieldBuildingTarget repairability internal id cleanup

Step:
- UnitBattlefieldBuildingTarget repairability internal id cleanup

Milestone:
- M1 EntityWorld authority

Owner AI:
- Codex

Reviewer AI:
- McClintock the 2nd

Integrator AI:
- Codex

Scope:
- Files/folders:
  - scripts/core/units/runtime/UnitBattlefield.cs
  - tools/ReviewGate/Program.cs
  - TODO.md
  - docs/reviews/2026-06-30-buildingtarget-repairability-internal-id.md
- Non-goals:
  - Do not change repair ability data, repair cost, repair range, target legality in
    `CommandSystem`, or `RepairSystem` behavior.
  - Do not change selection input, smart right-click behavior, or UI feedback.
  - Do not migrate repair command sync, producer candidates, combat targeting, or
    final building wrapper storage.

Implementation summary:
- Replaced internal building repairability checks that accepted
  `UnitBattlefieldBuildingTarget` with
  `IsRepairableBuildingTargetCore(PlayerSlotId playerSlotId, int buildingId)`.
- Kept public building repair preview/command APIs id-based.
- Preserved alive, damaged, and self/allied relation checks, and continued deriving
  building max HP from `BuildSpecCatalog`.
- Added `ReviewGate buildingtargetrepairabilityinternalid`.

Automated gates:
- Command: `dotnet build ProceduralRts.csproj --no-restore`
  Result: pass
  Evidence: 0 warnings, 0 errors.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore -- buildingtargetrepairabilityinternalid`
  Result: pass
  Evidence: ReviewGate passed with 0 errors, 0 warnings.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore -- buildingtargetstaticprojectiondeleted`
  Result: pass
  Evidence: ReviewGate passed with 0 errors, 0 warnings.
- Command: `dotnet run --project tools/CombatBehavior/CombatBehavior.csproj --no-restore`
  Result: pass
  Evidence: Combat behavior passed.
- Command: `dotnet run --project tools/PlayerLoopQa/PlayerLoopQa.csproj --no-restore`
  Result: pass
  Evidence: PlayerLoopQa PASSED.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore -- review --require-record=buildingtarget-repairability-internal-id`
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
  Evidence: This slice changes internal helper parameters only.

Reviewer result:
- Status: pass on static implementation shape; fail-for-completion until required
  gate hardening and evidence recording were applied.
- Required fixes:
  - McClintock the 2nd found the first repairability gate was too narrow because it
    only forbade `IsRepairableTarget(... UnitBattlefieldBuildingTarget)`. Fixed by
    adding regex checks that also reject `IsRepairableBuildingTargetCore(...)` and
    other building repairability helpers taking `UnitBattlefieldBuildingTarget`,
    while preserving the unit `IsRepairableTarget(PlayerSlotId, UnitInstance)` path.
  - McClintock the 2nd noted the review record still had pending evidence and TODO
    was open before final gates were recorded. Fixed by recording reviewer and
    integrator gate evidence here.
- Residual risks:
  - The helper still resolves the migration wrapper internally by id during M1.
  - Repair command sync still needs the wrapper until the live building command path
    fully moves to EntityWorld projections.
  - ReviewGate is string/regex-based rather than semantic type analysis.

TODO update:
- Items marked done:
  - UnitBattlefieldBuildingTarget repairability internal id cleanup
- Items left open:
  - Repair command sync, producer candidates, combat targeting, and final wrapper
    deletion migrations.
- Reason:
  - This slice only removes wrapper flow from internal building repairability helper
    parameters while preserving repair legality behavior.
