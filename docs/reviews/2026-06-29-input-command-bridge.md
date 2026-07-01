# Review Record - Input Command Bridge

Step: Route selected move/attack live input through `EntityCommandBuffer` as a bounded M1 migration slice.
Milestone: M1 EntityWorld Becomes Authoritative
Owner AI: Codex
Reviewer AI: Codex reviewer pass
Integrator AI: Codex

Scope:
- Files/folders: `scripts/core/units/runtime/UnitBattlefield.cs`, `tools/CombatBehavior/Program.cs`, `tools/ReviewGate/Program.cs`, `TODO.md`.
- Non-goals: select/stop/stance command routing, deleting legacy `UnitBattlefield` command fields, fully flipping unit movement/combat authority to `EntityWorld`, UI command refactor outside existing `UnitBattlefield` calls.

Implementation summary:
- Added a migration bridge inside `UnitBattlefield` with an `EntityCommandBuffer` and `CommandSystem`.
- `CommandMoveSelected` now creates a `GroupMoveEntityCommand`, drains it through the buffer, applies it through `CommandSystem` over the EntityWorld mirror, then copies command state back to legacy units.
- `CommandAttackSelected` for units and building targets now creates a `GroupAttackEntityCommand` through the same bridge.
- Legacy feel fields such as command pulse, harvester stop behavior, command visual target, formation slot, and attack target are preserved by the bridge.
- Added `AppliedInputCommandCount` as test evidence that selected commands passed through the bridge.

Automated gates:
- Command: `dotnet build ProceduralRts.csproj --no-restore`
  Result: pass
  Evidence: build completed with 0 warnings and 0 errors.
- Command: `dotnet run --project tools/CombatBehavior/CombatBehavior.csproj --no-restore`
  Result: pass
  Evidence: Combat behavior passed with assertions for selected move/attack buffer routing.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj inputcommandbridge --no-restore`
  Result: pass
  Evidence: `Errors: 0`, `Warnings: 0`, `ReviewGate passed.`
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj review --require-record=input-command-bridge --no-restore`
  Result: pass
  Evidence: `Errors: 0`, `Warnings: 0`, `ReviewGate passed.`
- Command: `dotnet run --project tools/VerifyAll/VerifyAll.csproj --no-restore`
  Result: pass
  Evidence: all steps passed after this slice.

Manual/visual gates:
- Check: Visual QA
  Result: not run
  Evidence: this slice preserves existing command presentation fields and headless Godot gates cover boot stability.

Reviewer result:
- Status: pass
- Required fixes: none
- Residual risks: This is still a bridge: selected move/attack enter the command buffer, but selection, stop, stance, and full live EntityWorld authority remain open.

TODO update:
- Items marked done: nested M1 slice `UnitBattlefield selected move/attack EntityCommandBuffer bridge`.
- Items left open: parent live input routing item, select/stop/stance command routing, full EntityWorld unit authority, legacy runtime deletion.
- Reason: tests and ReviewGate prove selected move/attack buffer routing without claiming the broader live-input migration is complete.
