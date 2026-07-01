# Review Record - Selection Command Bridge

Step: Route UnitBattlefield selection input through `EntityCommandBuffer` as the next bounded M1 migration slice.
Milestone: M1 EntityWorld Becomes Authoritative
Owner AI: Codex
Reviewer AI: Codex reviewer pass
Integrator AI: Codex

Scope:
- Files/folders: `scripts/core/entities/EntityCommand.cs`, `scripts/core/sim/systems/CommandSystem.cs`, `scripts/core/units/runtime/UnitBattlefield.cs`, `tools/CombatBehavior/Program.cs`, `tools/ReviewGate/Program.cs`, `TODO.md`.
- Non-goals: harvest command routing, production/rally routing, deleting legacy `UnitBattlefield` selection APIs, full `UseEntityWorldUnits` flip.

Implementation summary:
- Added `SetSelectionEntityCommand` and `EntityCommandKind.Select`.
- `CommandSystem` now applies selection by mutating `SelectableComponentState` for all owned selectable entities.
- `UnitBattlefield` selection APIs now compute the desired selection set, enqueue `SetSelectionEntityCommand`, apply it through `CommandSystem` over the EntityWorld mirror, and copy selection state back to legacy `UnitInstance` records.
- Preserved existing selection behavior for single-click, additive toggle, same-design selection, box selection, explicit id recall, and clear selection.

Automated gates:
- Command: `dotnet build ProceduralRts.csproj --no-restore`
  Result: pass
  Evidence: build completed with 0 warnings and 0 errors.
- Command: `dotnet run --project tools/CombatBehavior/CombatBehavior.csproj --no-restore`
  Result: pass
  Evidence: Combat behavior passed with assertions for selection buffer routing and existing selection behavior.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj inputcommandbridge --no-restore`
  Result: pass
  Evidence: `Errors: 0`, `Warnings: 0`, `ReviewGate passed.`
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj review --require-record=selection-command-bridge --no-restore`
  Result: pass
  Evidence: `Errors: 0`, `Warnings: 0`, `ReviewGate passed.`
- Command: `dotnet run --project tools/VerifyAll/VerifyAll.csproj --no-restore`
  Result: pass
  Evidence: all steps passed after this slice.

Manual/visual gates:
- Check: Visual QA
  Result: not run
  Evidence: this slice preserves the existing selection controller/UI paths; headless Godot gates cover boot stability.

Reviewer result:
- Status: pass
- Required fixes: none
- Residual risks: This still leaves harvest, production/rally, and the full `UseEntityWorldUnits` flip open. Legacy `UnitBattlefield` APIs remain as compatibility wrappers.

TODO update:
- Items marked done: nested M1 slice `UnitBattlefield selection EntityCommandBuffer bridge`.
- Items left open: parent live-input routing item, harvest command routing, production/rally migration, full EntityWorld unit authority, legacy runtime deletion.
- Reason: tests and ReviewGate prove selection buffer routing without claiming the broader live-input migration is complete.
