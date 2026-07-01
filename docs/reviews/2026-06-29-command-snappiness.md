# Review Record - Command snappiness

Step: Prove immediate selection/command feedback for current live controls.
Milestone: Controls and command feel.
Owner AI: Codex.
Reviewer AI: Codex self-review with ReviewGate and CombatBehavior coverage.
Integrator AI: Codex.

Scope:
- Files/folders: `scripts/controllers/SelectionController.cs`, `scripts/world/CommandAcknowledgementLayer.cs`, `scripts/core/units/runtime/UnitBattlefield.cs`, `scripts/world/UnitInstanceView.cs`, `scripts/BattleRoot.cs`, `tools/CombatBehavior/Program.cs`, `tools/ReviewGate/Program.cs`, `TODO.md`, `docs/reviews/2026-06-29-command-snappiness.md`.
- Non-goals: no SimEvent-driven alert migration, no remappable bindings, no EntityWorld input-command routing, no new order vocabulary.

Implementation summary:
- Added `ReviewGate commandsnappiness` to lock the current command-feel feedback path.
- The gate verifies immediate command acknowledgement ring insertion/redraw.
- The gate verifies dashed command lines and pulsing intent markers consume `CommandVisualTarget` for both legacy and live UnitBattlefield selections.
- The gate verifies live commands store command visual targets and pulses.
- The gate verifies runtime weapon mounts aim through `AimWeaponMounts`, firing is gated by `WeaponCanFireAt`, and `UnitInstanceView` renders live mount facings.
- `CombatBehavior` already asserts command visual target assignment and turret state transitions; those checks now serve as durable evidence for this TODO.

Automated gates:
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj commandsnappiness --no-restore`
  Result: pass
  Evidence: ReviewGate reported 0 errors and 0 warnings for command snappiness hooks.
- Command: `dotnet run --project tools/CombatBehavior/CombatBehavior.csproj --no-restore`
  Result: pass
  Evidence: CombatBehavior passed, including command visual target and turret tracking state assertions.

Manual/visual gates:
- Check: interactive hands-on command feel
  Result: not run
  Evidence: this slice adds durable implementation coverage; future playtests may still tune alpha, dash length, and pulse timing.

Reviewer result:
- Status: pass
- Required fixes: none.
- Residual risks: the stricter `Command feedback ... all driven by SimEvents` TODO remains open because current presentation rings are still driven from input callbacks rather than sim events.

TODO update:
- Items marked done: `Selection/command snappiness: instant ring, dashed command line to intent point, crisp ack rings, responsive turret tracking - the feel of precise control`.
- Items left open: SimEvent-driven command feedback, EntityWorld input routing, remappable bindings, and additional order vocabulary.
- Reason: all listed feel elements are present on the current live path and now protected by automated gates.
