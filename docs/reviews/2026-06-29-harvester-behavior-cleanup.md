# Review Record - Harvester Behavior Cleanup

Step: Delete the now-dead UnitBattlefield per-unit harvester behavior loop after live harvesting moved to `ResourceSystem`.
Milestone: M1 EntityWorld Becomes Authoritative
Owner AI: Codex
Reviewer AI: Codex reviewer pass
Integrator AI: Codex

Scope:
- Files/folders: `scripts/core/units/runtime/UnitBattlefield.cs`, `tools/ReviewGate/Program.cs`, `TODO.md`.
- Non-goals: deleting command-time refinery availability checks, deleting legacy dock feedback fields still read by UI, rewriting old `GameState` economy.

Implementation summary:
- Removed `UpdateHarvester`, `UpdateHarvesterMovingToField`, `UpdateHarvesterGathering`, `SendHarvesterToRefinery`, `UpdateHarvesterReturning`, and `UpdateHarvesterUnloading` from `UnitBattlefield`.
- Removed live `UnitBattlefield` hard-coded `HarvestRate` and `UnloadRate` constants.
- Kept command-time refinery validation and dock feedback helpers because current selection/status UI still uses those legacy fields during migration.
- Added `ReviewGate harvestcleanup` to keep the old per-unit harvest loop and local rates from returning.

Automated gates:
- Command: `dotnet build ProceduralRts.csproj --no-restore`
  Result: pass
  Evidence: build completed with 0 warnings and 0 errors.
- Command: `dotnet run --project tools/CombatBehavior/CombatBehavior.csproj --no-restore`
  Result: pass
  Evidence: Combat behavior passed after deleting the legacy harvest loop.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj harvestcleanup --no-restore`
  Result: pass
  Evidence: `Errors: 0`, `Warnings: 0`, `ReviewGate passed.`
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj review --require-record=harvester-behavior-cleanup --no-restore`
  Result: pass
  Evidence: `Errors: 0`, `Warnings: 0`, `ReviewGate passed.`
- Command: `dotnet run --project tools/VerifyAll/VerifyAll.csproj --no-restore`
  Result: pass
  Evidence: all 14 steps passed after this slice.

Manual/visual gates:
- Check: Visual QA
  Result: not run
  Evidence: cleanup only removes dead behavior methods; current UI-facing fields remain synchronized by the ResourceSystem bridge.

Reviewer result:
- Status: pass
- Required fixes: none
- Residual risks: Some command-time legacy refinery selection/dock helpers remain until the UI/status layer reads EntityWorld directly.

TODO update:
- Items marked done: nested M1 slice `UnitBattlefield legacy harvester behavior cleanup`.
- Items left open: parent harvester/production/building migration item, building target cleanup, construction migration, legacy behavior deletion.
- Reason: build and gates prove the old harvester behavior loop was removed without claiming all `UnitBattlefield` behavior has been deleted.
