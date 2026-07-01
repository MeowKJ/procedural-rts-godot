# Review Record - BuildingTarget attack projection reads

Step:
- UnitBattlefieldBuildingTarget attack projection read cleanup

Milestone:
- M1 EntityWorld authority / Migration cleanup

Owner AI:
- Codex

Reviewer AI:
- Codex; ReviewGate buildingtargetattackprojectionreads

Integrator AI:
- Codex

Scope:
- Files/folders:
  - scripts/core/units/runtime/UnitBattlefield.cs
  - tools/CombatBehavior/Program.cs
  - tools/ReviewGate/Program.cs
  - TODO.md
  - docs/reviews/2026-07-01-buildingtarget-attack-projection-reads.md
- Non-goals:
  - Do not delete `_buildingTargetSeedsById`.
  - Do not delete all `BuildingTargetById(int)` compatibility reads.
  - Do not change attack balance, target profiles, UI, art, or production
    behavior.

Implementation summary:
- Changed `CommandAttackSelected(PlayerSlotId, int buildingId)` to resolve
  `BuildingSnapshot(...)` and `BuildingEntityByTargetId(...)` instead of seed
  target state.
- Changed explicit group building attack commands to do the same.
- Removed attack-command dependence on `_buildingTargetEntityIds[target.Id]` and
  submitted the resolved EntityWorld `targetEntity.Id` directly.
- Added `ReviewGate buildingtargetattackprojectionreads` and updated affected
  historical gates to expect projected building command reads.

Automated gates:
- Command: `dotnet build ProceduralRts.csproj --no-restore`
  Result: pass
  Evidence: build completed successfully with 0 warnings and 0 errors.
- Command: `dotnet build tools/ReviewGate/ReviewGate.csproj --no-restore`
  Result: pass
  Evidence: build completed successfully with 0 warnings and 0 errors.
- Command: `dotnet run --project tools/CombatBehavior/CombatBehavior.csproj --no-restore`
  Result: pass
  Evidence: Combat behavior passed after attack commands moved to projected
    building reads.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore -- buildingtargetattackprojectionreads`
  Result: pass
  Evidence: ReviewGate accepted attack commands reading building snapshots and
    target EntityWorld ids instead of seed target objects.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore -- attackunitscommandbridge`
  Result: pass
  Evidence: Existing attack-units command bridge gate stayed green after the
    projection-read migration.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore -- buildingtargetcommandobjectdeleted`
  Result: pass
  Evidence: Building attack command paths stayed free of target-wrapper command
    object dependence.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore -- buildingtargetsyncinternalid`
  Result: pass
  Evidence: Entity sync still routes through internal ids after the attack read
    cleanup.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore -- buildingtargetcombathelperinternalid`
  Result: pass
  Evidence: Combat helper gates stayed on id-based building target helpers.
- Command: `dotnet run --project tools/SimReplay/SimReplay.csproj --no-restore`
  Result: pass
  Evidence: Deterministic replay completed after attack commands moved to
    projected building reads.

Manual/visual gates:
- Check: Visual/UI review
  Result: not applicable
  Evidence: This slice changes command read authority only.

Reviewer result:
- Status: pass after integration review.
- Required fixes:
  - None.
- Reviewer notes:
  - Building attack commands now use projected owner/kind plus EntityWorld entity
    id, matching the M1 authority direction.
- Residual risks:
  - `_buildingTargetSeedsById` and `BuildingTargetById(int)` remain for lifecycle
    write/sync compatibility.
  - Other command/state helpers still use seed state and remain later M1 slices.
  - Full `VerifyAll` passed 23/23 after the multi-slice batch.

TODO update:
- Items marked done:
  - UnitBattlefieldBuildingTarget attack projection read cleanup
- Items left open:
  - Broader Migration cleanup and final `BuildingKind` / entity-path deletion
    remain open.
- Reason:
  - This slice removes seed reads from building attack commands, but not from all
    temporary compatibility helpers.
