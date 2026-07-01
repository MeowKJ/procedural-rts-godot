# Review Record - BuildingTarget selection seed guard deleted

Step:
- UnitBattlefieldBuildingTarget selection seed guard deletion

Milestone:
- M1 EntityWorld authority / Migration cleanup

Owner AI:
- Codex

Reviewer AI:
- Codex; ReviewGate buildingtargetselectionseedguarddeleted

Integrator AI:
- Codex

Scope:
- Files/folders:
  - scripts/core/units/runtime/UnitBattlefield.cs
  - tools/CombatBehavior/Program.cs
  - tools/ReviewGate/Program.cs
  - TODO.md
  - docs/reviews/2026-07-01-buildingtarget-selection-seed-guard-deleted.md
- Non-goals:
  - Do not change building selection UI behavior.
  - Do not change selected-building workflows or HUD projection shape.
  - Do not delete `_buildingTargetSeedsById`.

Implementation summary:
- `SetBuildingTargetSelected(int, bool)` now requires an existing EntityWorld
  building entity and writes `SelectableComponentState` directly.
- Removed the seed-storage existence guard and seed-sync fallback from building
  selection writes.
- `CombatBehavior` removes a building seed entry and proves selection writes
  still update EntityWorld projection state.

Automated gates:
- Command: `dotnet build ProceduralRts.csproj --no-restore`
  Result: pass.
  Evidence: build completed successfully with 0 warnings and 0 errors.
- Command: `dotnet build tools/ReviewGate/ReviewGate.csproj --no-restore`
  Result: pass.
  Evidence: build completed successfully with 0 warnings and 0 errors.
- Command: `dotnet run --project tools/CombatBehavior/CombatBehavior.csproj --no-restore`
  Result: pass.
  Evidence: Combat behavior passed with selection-write assertions after seed
    removal.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore -- buildingtargetselectionseedguarddeleted`
  Result: pass.
  Evidence: ReviewGate accepted selection writes that require existing
    EntityWorld building entities and do not sync from seed state.
- Command: `dotnet run --project tools/SimReplay/SimReplay.csproj --no-restore`
  Result: pass.
  Evidence: deterministic replay suite passed after selection seed guard
    deletion.

Manual/visual gates:
- Check: Visual/UI review
  Result: not applicable.
  Evidence: This slice changes selection write authority only.

Reviewer result:
- Status: pass after integration review.
- Required fixes:
  - None currently known.
- Residual risks:
  - Building selection still has legacy UI fallback mirroring outside this slice.
  - `_buildingTargetSeedsById` remains lifecycle/write/sync compatibility storage.

TODO update:
- Items marked done:
  - UnitBattlefieldBuildingTarget selection seed guard deletion
- Items left open:
  - Broader Migration cleanup and final seed-storage deletion remain open.
