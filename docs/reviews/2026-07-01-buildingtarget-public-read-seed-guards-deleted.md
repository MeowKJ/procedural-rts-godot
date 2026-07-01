# Review Record - BuildingTarget public read seed guards deleted

Step:
- UnitBattlefieldBuildingTarget public read seed guard deletion

Milestone:
- M1 EntityWorld authority / Migration cleanup

Owner AI:
- Codex

Reviewer AI:
- Codex; ReviewGate buildingtargetpublicreadseedguardsdeleted

Integrator AI:
- Codex

Scope:
- Files/folders:
  - scripts/core/units/runtime/UnitBattlefield.cs
  - tools/CombatBehavior/Program.cs
  - tools/ReviewGate/Program.cs
  - TODO.md
  - docs/reviews/2026-07-01-buildingtarget-public-read-seed-guards-deleted.md
- Non-goals:
  - Do not delete `_buildingTargetSeedsById`.
  - Do not change building production, rally, combat, power, dock, or visibility
    behavior.
  - Do not run full `VerifyAll` for this single slice.

Implementation summary:
- Public building read APIs now delegate directly to their EntityWorld-backed
  core helpers instead of checking `BuildingTargetById(...)` as a seed-storage
  existence guard.
- Building visibility reads no longer synthesize missing building entities from
  seed state.
- Single-id radius fallback now uses `BuildingIdentity(...)` instead of seed
  state when a presentation projection is absent.
- `CombatBehavior` removes a building seed entry and proves the public read APIs
  still read EntityWorld components.

Automated gates:
- Command: `dotnet build ProceduralRts.csproj --no-restore`
  Result: pass.
  Evidence: build completed successfully with 0 warnings and 0 errors.
- Command: `dotnet build tools/ReviewGate/ReviewGate.csproj --no-restore`
  Result: pass.
  Evidence: build completed successfully with 0 warnings and 0 errors.
- Command: `dotnet run --project tools/CombatBehavior/CombatBehavior.csproj --no-restore`
  Result: pass.
  Evidence: Combat behavior passed after seedless public-read assertions were
    added.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore -- buildingtargetpublicreadseedguardsdeleted`
  Result: pass.
  Evidence: ReviewGate accepted public read APIs delegating to EntityWorld core
    helpers without temporary seed guards.
- Command: `dotnet run --project tools/SimReplay/SimReplay.csproj --no-restore`
  Result: pass.
  Evidence: deterministic replay suite passed after public read seed guards were
    removed.

Manual/visual gates:
- Check: Visual/UI review
  Result: not applicable.
  Evidence: This slice changes read authority only.

Reviewer result:
- Status: pass after integration review.
- Required fixes:
  - None currently known.
- Residual risks:
  - `_buildingTargetSeedsById` remains lifecycle/write/sync compatibility storage.
  - Hot-path batching remains a later performance slice.

TODO update:
- Items marked done:
  - UnitBattlefieldBuildingTarget public read seed guard deletion
- Items left open:
  - Broader Migration cleanup and final seed-storage deletion remain open.
