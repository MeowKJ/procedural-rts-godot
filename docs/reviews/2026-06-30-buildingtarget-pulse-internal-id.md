# Review Record - UnitBattlefieldBuildingTarget presentation pulse internal id cleanup

Step:
- UnitBattlefieldBuildingTarget presentation pulse internal id cleanup

Milestone:
- M1 EntityWorld authority

Owner AI:
- Codex

Reviewer AI:
- Kant the 2nd

Integrator AI:
- Codex

Scope:
- Files/folders:
  - scripts/core/units/runtime/UnitBattlefield.cs
  - tools/ReviewGate/Program.cs
  - TODO.md
  - docs/reviews/2026-06-30-buildingtarget-pulse-internal-id.md
- Non-goals:
  - Do not change pulse decay rates or visual timing.
  - Do not change selection, combat, refinery delivery, or rally semantics.
  - Do not migrate repair, producer-candidate, refinery, snapshot, radius, or spec
    helpers.
  - Do not delete private building wrapper storage.

Implementation summary:
- Replaced private building presentation-pulse helpers that accepted
  `UnitBattlefieldBuildingTarget` with `BuildingHitPulseCore(int buildingId)`,
  `BuildingPresentationPulseCore(int buildingId)`,
  `SetBuildingHitPulseCore(int buildingId, float value)`,
  `SetBuildingDeliveryPulseCore(int buildingId, float value)`,
  `SetBuildingRallyPulseCore(int buildingId, float value)`,
  `SetBuildingPresentationPulseCore(int buildingId, ...)`, and
  `DecayBuildingPresentationPulses(int buildingId, float dt)`.
- Kept public `SetBuildingHitPulse(int buildingId, float value)` stable, including
  its missing-building migration no-op.
- Updated selection fallback, pulse decay, rally pulse writes, and refinery delivery
  pulse writes to pass `building.Id`.
- Updated pulse ReviewGate checks and added `ReviewGate buildingtargetpulseinternalid`.

Automated gates:
- Command: `dotnet build ProceduralRts.csproj --no-restore`
  Result: pass
  Evidence: 0 warnings, 0 errors.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore -- buildingtargetpulseinternalid`
  Result: pass
  Evidence: ReviewGate passed with 0 errors, 0 warnings.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore -- buildingtargetpresentationpulseentitystate`
  Result: pass
  Evidence: ReviewGate passed with 0 errors, 0 warnings.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore -- buildingtargetpulseobjectdeleted`
  Result: pass
  Evidence: ReviewGate passed with 0 errors, 0 warnings.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore -- buildingtargetpulsereadobjectdeleted`
  Result: pass
  Evidence: ReviewGate passed with 0 errors, 0 warnings.
- Command: `dotnet run --project tools/CombatBehavior/CombatBehavior.csproj --no-restore`
  Result: pass
  Evidence: Combat behavior passed.
- Command: `dotnet run --project tools/SimReplay/SimReplay.csproj --no-restore`
  Result: pass
  Evidence: SimReplay PASSED.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore -- review --require-record=buildingtarget-pulse-internal-id`
  Result: pass
  Evidence: ReviewGate passed with 0 errors, 0 warnings.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore`
  Result: pass
  Evidence: ReviewGate passed with 0 errors, 0 warnings after updating the historical rally pulse gate.
- Command: `dotnet run --project tools/VerifyAll/VerifyAll.csproj --no-restore`
  Result: pass
  Evidence: VerifyAll PASSED, 23/23 steps.

Manual/visual gates:
- Check: Visual/UI review
  Result: not applicable
  Evidence: This slice changes internal presentation-pulse helper parameters only.

Reviewer result:
- Status: pass-with-warnings
- Required fixes:
  - None blocking. Kant the 2nd noted the shared pulse writer gate was weaker than
    the code shape; fixed by requiring the exact `SetBuildingPresentationPulseCore`
    multiline signature with `int buildingId`.
- Residual risks:
  - Public hit-pulse writes still preserve migration wrapper existence checks.
  - Other internal helper families still accept the migration wrapper and remain
    future M1 slices.
  - The new gate is string-based and may reject equivalent rewrites during migration.

TODO update:
- Items marked done:
  - UnitBattlefieldBuildingTarget presentation pulse internal id cleanup
- Items left open:
  - Repair, producer-candidate, refinery, radius, snapshot, spec, and final wrapper
    deletion migrations.
- Reason:
  - This slice only removes wrapper flow from internal building presentation-pulse
    helpers.
