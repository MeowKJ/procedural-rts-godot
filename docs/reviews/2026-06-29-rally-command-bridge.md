# Review Record - Rally Command Bridge

Step: Route UnitBattlefield producer rally updates through EntityWorld `ProductionSystem` commands.
Milestone: M1 EntityWorld Becomes Authoritative
Owner AI: Codex
Reviewer AI: Codex reviewer pass
Integrator AI: Codex

Scope:
- Files/folders: `scripts/core/units/runtime/UnitBattlefield.cs`, `tools/CombatBehavior/Program.cs`, `tools/ReviewGate/Program.cs`, `TODO.md`.
- Non-goals: resource smart-right-click rally wiring, moving-unit rally targets, shift-queued rally commands, deleting legacy production UI wrappers, construction/build placement migration.

Implementation summary:
- `SetRallyPoint` now validates the producer, mirrors it into EntityWorld, and submits `SetRallyPointEntityCommand` to `ProductionSystem`.
- `SyncBuildingRallyFromEntity` copies `RallyPointComponentState` back to the legacy building target so current UI and production presentation keep working during migration.
- `CombatBehavior` proves rally command application, command count, entity rally component state, and legacy rally synchronization before production completion consumes the rally.
- `ReviewGate productionbridge` now locks rally routing alongside production enqueue, cancel, completion, queue sync, and credit sync.

Automated gates:
- Command: `dotnet build ProceduralRts.csproj --no-restore`
  Result: pass
  Evidence: build completed with 0 warnings and 0 errors.
- Command: `dotnet run --project tools/CombatBehavior/CombatBehavior.csproj --no-restore`
  Result: pass
  Evidence: Combat behavior passed with assertions proving rally and production enqueue route through EntityWorld and sync back.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj productionbridge --no-restore`
  Result: pass
  Evidence: `Errors: 0`, `Warnings: 0`, `ReviewGate passed.`
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj review --require-record=rally-command-bridge --no-restore`
  Result: pass
  Evidence: `Errors: 0`, `Warnings: 0`, `ReviewGate passed.`
- Command: `dotnet run --project tools/VerifyAll/VerifyAll.csproj --no-restore`
  Result: pass
  Evidence: all 14 steps passed after this slice.

Manual/visual gates:
- Check: Visual QA
  Result: not run
  Evidence: rally point UI still reads the legacy building target; this slice keeps that state synchronized from EntityWorld.

Reviewer result:
- Status: pass
- Required fixes: none
- Residual risks: Legacy building target runtime still exists as a UI compatibility mirror. Resource smart-rally UI and final building runtime deletion remain open.

TODO update:
- Items marked done: nested M1 slice `UnitBattlefield rally point EntityWorld command bridge`.
- Items left open: parent harvester/production/building migration item, building target cleanup, construction migration, legacy behavior deletion.
- Reason: tests and ReviewGate prove rally command routing through EntityWorld without claiming the entire M1 migration is complete.
