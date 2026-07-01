Step: Move mobile unit attacks against building targets onto an EntityWorld bridge.
Milestone: M1 EntityWorld Becomes Authoritative
Owner AI: Codex
Reviewer AI: Codex review pass
Integrator AI: Codex

Scope:
- Files/folders: `scripts/core/sim/systems/BuildingTargetCombatSystem.cs`, `scripts/core/units/runtime/UnitBattlefield.cs`, `tools/ReviewGate/Program.cs`, `TODO.md`.
- Added a transitional pure `BuildingTargetCombatSystem` that processes only `EntityKind.Unit` attackers whose weapon target is a building/turret entity.
- Removed the legacy `UpdateBuildingTargetCombat` and unit-vs-building damage helper from `UnitBattlefield`.
- Synced EntityWorld movement, weapon mount cooldown, building health, hit pulse, attack events, and building death removal back into legacy presentation fields during migration.
- Non-goals: no unit-vs-unit combat migration, no generic `CombatSystem` replacement, no balance changes.

Automated gates:
- Command: `dotnet build ProceduralRts.csproj --no-restore`
  Result: pass
  Evidence: build completed successfully with 0 warnings and 0 errors.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj buildingtargetcombatsystembridge --no-restore`
  Result: pass
  Evidence: dedicated unit-vs-building combat bridge gate completed with 0 errors and 0 warnings.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj buildingtargetbridge --no-restore`
  Result: pass
  Evidence: existing building target health/death bridge gate completed with 0 errors and 0 warnings.
- Command: `dotnet run --project tools/CombatBehavior/CombatBehavior.csproj --no-restore`
  Result: pass
  Evidence: CombatBehavior completed with building target damage/death, turret state, economy, enemy AI, and outcome scenarios intact.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore`
  Result: pass
  Evidence: full ReviewGate completed with 0 errors and 0 warnings after the record update.
- Command: `dotnet run --project tools/VerifyAll/VerifyAll.csproj --no-restore`
  Result: pass
  Evidence: VerifyAll completed all 16 steps successfully, including SimReplay, CombatBehavior, PerfSmoke, BalanceReport, and Godot headless QA.

Reviewer result:
- Status: pass pending gates.
- Design note: the bridge is deliberately narrower than generic combat so mobile unit-vs-unit combat is not double-stepped while M1 remains incomplete.
- Required fixes: none.

Status:
- Pass.

Residual risks:
- `BuildingTargetCombatSystem` duplicates a subset of `CombatSystem`; it should be deleted once mobile units fully run on the generic EntityWorld combat path.
- Headless and behavior tests prove state transitions, not pixel-perfect combat presentation.

TODO update:
- Marked done: nested M1 slice `UnitBattlefield unit-vs-building CombatSystem bridge`.
- Left open: parent M1 behavior deletion until remaining mobile unit-vs-unit behavior is retired.
