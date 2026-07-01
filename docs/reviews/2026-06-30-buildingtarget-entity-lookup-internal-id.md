# Review Record - UnitBattlefieldBuildingTarget entity lookup internal id cleanup

Step:
- UnitBattlefieldBuildingTarget entity lookup internal id cleanup

Milestone:
- M1 EntityWorld authority

Owner AI:
- Codex

Reviewer AI:
- Russell the 2nd

Integrator AI:
- Codex

Scope:
- Files/folders:
  - scripts/core/units/runtime/UnitBattlefield.cs
  - tools/ReviewGate/Program.cs
  - TODO.md
  - docs/reviews/2026-06-30-buildingtarget-entity-lookup-internal-id.md
- Non-goals:
  - Do not change combat targeting, weapon cooldowns, damage, production matching,
    construction adoption semantics, or event payload types.
  - Do not delete wrapper storage or migrate snapshot/build-spec helper families.
  - Do not change UI, VFX, alerts, or production/combat balance.

Implementation summary:
- Replaced `BuildingTargetByEntityId(EntityId)` wrapper-returning lookup with
  `BuildingTargetIdByEntityId(EntityId entityId)`.
- Updated building damage events to resolve damaged building ids, sync health by id,
  snapshot by id, and track dead buildings by id.
- Updated turret damage events to resolve attacker building ids, snapshot by id, and
  derive weapon ammo from `BuildSpecCatalog.For(attackerSnapshot.Kind)`.
- Updated constructed-building adoption to detect existing targets by id before
  resolving the temporary wrapper.
- Added `ReviewGate buildingtargetentitylookupinternalid`.

Automated gates:
- Command: `dotnet build ProceduralRts.csproj --no-restore`
  Result: pass
  Evidence: 0 warnings, 0 errors.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore -- buildingtargetentitylookupinternalid`
  Result: pass
  Evidence: ReviewGate passed with 0 errors, 0 warnings.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore -- buildingtargeteventobjectdeleted`
  Result: pass
  Evidence: ReviewGate passed with 0 errors, 0 warnings.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore -- buildingtargetstaticprojectiondeleted`
  Result: pass
  Evidence: ReviewGate passed with 0 errors, 0 warnings.
- Command: `dotnet run --project tools/CombatBehavior/CombatBehavior.csproj --no-restore`
  Result: pass
  Evidence: Combat behavior passed.
- Command: `dotnet run --project tools/SimReplay/SimReplay.csproj --no-restore`
  Result: pass
  Evidence: SimReplay PASSED.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore -- review --require-record=buildingtarget-entity-lookup-internal-id`
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
  Evidence: This slice changes internal event lookup plumbing only.

Reviewer result:
- Status: pass on implementation shape; fail-for-completion until required gate
  hardening and evidence recording were applied.
- Required fixes:
  - Russell the 2nd found the first gate only banned the exact old helper name and
    exact call text. Fixed by adding regex guards that reject EntityId-based
    lookup helpers returning `UnitBattlefieldBuildingTarget?`, even if they use a
    different helper name.
  - Russell the 2nd noted the review record still had pending evidence and TODO was
    open before final gates were recorded. Fixed by recording reviewer and
    integrator gate evidence here.
- Residual risks:
  - The id helper still uses the migration `_buildingTargetEntityIds` reverse scan
    through `LegacyBuildingTargetId` during M1.
  - Snapshot/build-spec helpers still resolve the temporary wrapper and remain
    future cleanup slices.
  - ReviewGate is string/regex-based rather than semantic type analysis.

TODO update:
- Items marked done:
  - UnitBattlefieldBuildingTarget entity lookup internal id cleanup
- Items left open:
  - Snapshot/build-spec helper cleanup and final wrapper deletion migrations.
- Reason:
  - This slice removes wrapper-returning EntityWorld-to-building lookup flow while
    preserving event snapshot payload behavior.
