# Review Record - Live Harvest ResourceSystem Bridge

Step: Route UnitBattlefield live harvester gather/return/unload ticks through EntityWorld `ResourceSystem`.
Milestone: M1 EntityWorld Becomes Authoritative
Owner AI: Codex
Reviewer AI: Codex reviewer pass
Integrator AI: Codex

Scope:
- Files/folders: `scripts/core/units/runtime/UnitBattlefield.cs`, `tools/CombatBehavior/Program.cs`, `tools/ReviewGate/Program.cs`, `TODO.md`.
- Non-goals: deleting legacy `UpdateHarvester*` helper methods, rewriting movement to EntityWorld `MovementSystem`, replacing the old `GameState` campaign/skirmish economy path.

Implementation summary:
- Added a live `_resourceSystem` bridge inside `UnitBattlefield.Update`.
- The per-unit legacy harvest loop is no longer called from live update; harvesters are mirrored into EntityWorld, `ResourceSystem` advances gather/return/unload, then state syncs back.
- Legacy resource field ids, refinery ids, and dock harvester ids are translated to EntityWorld ids before ResourceSystem runs; EntityWorld results are translated back for current UI/runtime state.
- Resource node amounts, cargo, harvester mode, dock claims, and banked credits sync back after each live resource tick.
- `CombatBehavior` proves a selected harvester can gather a field to zero, unload at a refinery, credit the owner, and release dock claims.

Automated gates:
- Command: `dotnet build ProceduralRts.csproj --no-restore`
  Result: pass
  Evidence: build completed with 0 warnings and 0 errors.
- Command: `dotnet run --project tools/CombatBehavior/CombatBehavior.csproj --no-restore`
  Result: pass
  Evidence: Combat behavior passed with assertions proving live harvest completion through the bridge.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj liveharvestbridge --no-restore`
  Result: pass
  Evidence: `Errors: 0`, `Warnings: 0`, `ReviewGate passed.`
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj review --require-record=live-harvest-resource-system-bridge --no-restore`
  Result: pass
  Evidence: `Errors: 0`, `Warnings: 0`, `ReviewGate passed.`
- Command: `dotnet run --project tools/VerifyAll/VerifyAll.csproj --no-restore`
  Result: pass
  Evidence: all 14 steps passed after this slice.

Manual/visual gates:
- Check: Visual QA
  Result: not run
  Evidence: current views still read legacy unit/resource/building fields; this bridge keeps those fields synchronized from EntityWorld.

Reviewer result:
- Status: pass
- Required fixes: none
- Residual risks: Legacy harvest helper methods still exist until final `UnitBattlefield` cleanup. Movement remains legacy-driven, so ResourceSystem currently sets movement intent rather than directly moving entities.

TODO update:
- Items marked done: nested M1 slice `UnitBattlefield live harvester ResourceSystem bridge`.
- Items left open: parent harvester/production/building migration item, building target cleanup, construction migration, legacy behavior deletion.
- Reason: tests and ReviewGate prove live harvester economy progression moved to EntityWorld without claiming the entire runtime migration is complete.
