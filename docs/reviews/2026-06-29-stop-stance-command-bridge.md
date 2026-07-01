# Review Record - Stop Stance Command Bridge

Step: Route selected stop and stance live input through `EntityCommandBuffer` as the next bounded M1 migration slice.
Milestone: M1 EntityWorld Becomes Authoritative
Owner AI: Codex
Reviewer AI: Codex reviewer pass
Integrator AI: Codex

Scope:
- Files/folders: `scripts/core/units/runtime/UnitInstance.cs`, `scripts/core/units/runtime/UnitBattlefield.cs`, `scripts/BattleRoot.cs`, `tools/CombatBehavior/Program.cs`, `tools/ReviewGate/Program.cs`, `TODO.md`.
- Non-goals: selection command routing, harvest command routing, deleting legacy `UnitBattlefield` command fields, fully flipping unit movement/combat authority to `EntityWorld`.

Implementation summary:
- Added `UnitInstance.Stance` as the legacy runtime copy of `StanceComponentState`.
- `UnitBattlefield.CommandStopSelected` now enqueues `StopEntityCommand`, applies it through `CommandSystem`, and copies cleared movement/attack state back to legacy units.
- `UnitBattlefield.CommandSetSelectedStance` now enqueues `SetStanceEntityCommand`, applies it through `CommandSystem`, and copies stance state back to legacy units.
- `SyncUnitEntity` now mirrors stance into `EntityWorld` for armed units.
- `BattleRoot.OnUnitStanceRequested` now calls the UnitBattlefield stance command path for the live UnitSpec runtime and updates HUD status/stance state.

Automated gates:
- Command: `dotnet build ProceduralRts.csproj --no-restore`
  Result: pass
  Evidence: build completed with 0 warnings and 0 errors.
- Command: `dotnet run --project tools/CombatBehavior/CombatBehavior.csproj --no-restore`
  Result: pass
  Evidence: Combat behavior passed with assertions for stop and stance buffer routing.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj inputcommandbridge --no-restore`
  Result: pass
  Evidence: `Errors: 0`, `Warnings: 0`, `ReviewGate passed.`
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj review --require-record=stop-stance-command-bridge --no-restore`
  Result: pass
  Evidence: `Errors: 0`, `Warnings: 0`, `ReviewGate passed.`
- Command: `dotnet run --project tools/VerifyAll/VerifyAll.csproj --no-restore`
  Result: pass
  Evidence: all steps passed after this slice.

Manual/visual gates:
- Check: Visual QA
  Result: not run
  Evidence: existing HUD stance display path is reused; headless Godot gates cover boot stability.

Reviewer result:
- Status: pass
- Required fixes: none
- Residual risks: This still does not complete the parent live-input TODO. Selection, harvest, production/rally, and full EntityWorld authority remain open.

TODO update:
- Items marked done: nested M1 slice `UnitBattlefield selected stop/stance EntityCommandBuffer bridge`.
- Items left open: parent live input routing item, selection/harvest command routing, full EntityWorld unit authority, legacy runtime deletion.
- Reason: tests and ReviewGate prove selected stop/stance buffer routing without claiming the broader live-input migration is complete.
