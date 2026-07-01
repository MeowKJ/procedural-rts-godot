# Review Record - UnitBattlefieldBuildingTarget refinery lookup internal id cleanup

Step:
- UnitBattlefieldBuildingTarget refinery lookup internal id cleanup

Milestone:
- M1 EntityWorld authority

Owner AI:
- Codex

Reviewer AI:
- Turing the 2nd

Integrator AI:
- Codex

Scope:
- Files/folders:
  - scripts/core/units/runtime/UnitBattlefield.cs
  - tools/ReviewGate/Program.cs
  - TODO.md
  - docs/reviews/2026-06-30-buildingtarget-refinery-lookup-internal-id.md
- Non-goals:
  - Do not change harvest economy, resource amounts, dock reservation behavior, or
    ResourceSystem authority.
  - Do not change refinery selection policy beyond preserving the current nearest
    ready refinery check.
  - Do not migrate combat targeting, producer candidates, death cleanup, snapshot,
    or final wrapper storage.

Implementation summary:
- Replaced the private `FindBestRefineryForHarvester(...)` wrapper-returning helper
  with `FindBestRefineryIdForHarvester(...)`.
- Updated selected-harvest and explicit-harvest validation to use the id-returning
  helper for existence checks.
- Preserved owner filtering, refinery-kind filtering, alive/completed filtering, and
  nearest-refinery ordering.
- Added `ReviewGate buildingtargetrefinerylookupinternalid`.

Automated gates:
- Command: `dotnet build ProceduralRts.csproj --no-restore`
  Result: pass
  Evidence: 0 warnings, 0 errors.
- Command: `dotnet run --project tools/SimReplay/SimReplay.csproj --no-restore`
  Result: pass
  Evidence: SimReplay PASSED, including resource-loop, auto-harvest, and resource-rally-production.
- Command: `dotnet run --project tools/CombatBehavior/CombatBehavior.csproj --no-restore`
  Result: pass
  Evidence: Combat behavior passed.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore -- buildingtargetrefinerylookupinternalid`
  Result: pass
  Evidence: ReviewGate passed with 0 errors, 0 warnings.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore -- review --require-record=buildingtarget-refinery-lookup-internal-id`
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
  Evidence: This slice changes internal harvest validation lookup plumbing only.

Reviewer result:
- Status: pass after gate hardening and explicit nullable-id validation.
- Required fixes:
  - Turing the 2nd noted the two harvest validation callers should read as
    explicit nullable-id checks rather than old wrapper-null checks. Fixed by
    using `FindBestRefineryIdForHarvester(...) is int`.
  - Turing the 2nd called out the empty-sequence `FirstOrDefault()` id-0 trap.
    Fixed by keeping `.Select(building => (int?)building.Id)` and adding a gate
    that rejects non-nullable id projection.
- Residual risks:
  - The id helper still scans the temporary `Buildings` migration list and checks
    EntityWorld build progress through `BuildingBuildProgress(building.Id)`.
  - Harvest command validation only needs existence today; future direct refinery
    assignment should continue passing ids or EntityIds rather than wrapper objects.
  - ReviewGate is string/regex-based rather than semantic type analysis.

TODO update:
- Items marked done:
  - UnitBattlefieldBuildingTarget refinery lookup internal id cleanup
- Items left open:
  - Combat targeting helpers, death cleanup, snapshot/build-spec helper cleanup, and
    final wrapper deletion migrations.
- Reason:
  - This slice removes wrapper flow from the private harvester-refinery lookup while
    preserving harvest eligibility behavior.
