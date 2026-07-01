Step: Close the M1 harvester, production, and building-target behavior migration parent.
Milestone: M1 EntityWorld Becomes Authoritative
Owner AI: Codex
Reviewer AI: Codex review pass
Integrator AI: Codex

Scope:
- Files/folders: `TODO.md`, `tools/ReviewGate/Program.cs`.
- Added `ReviewGate m1behaviorparentcomplete`, which proves the completed child slices for harvest, production, rally, building health/death, armed building combat, and unit-vs-building combat remain present.
- The gate also proves the legacy `UnitBattlefield` behavior methods for those domains stay deleted and the live path uses pure EntityWorld systems.
- Marked the parent TODO item complete.
- Non-goals: no deletion of all `UnitBattlefield`, no unit-vs-unit combat migration, no movement/autonomy migration.

Automated gates:
- Command: `dotnet build ProceduralRts.csproj --no-restore`
  Result: pass
  Evidence: build completed successfully with 0 warnings and 0 errors.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj m1behaviorparentcomplete --no-restore`
  Result: pass
  Evidence: dedicated M1 behavior parent completion gate completed with 0 errors and 0 warnings.
- Command: `dotnet run --project tools/CombatBehavior/CombatBehavior.csproj --no-restore`
  Result: pass
  Evidence: CombatBehavior completed with harvest, production, building target, turret, economy, enemy AI, and outcome scenarios intact.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore`
  Result: pass
  Evidence: full ReviewGate completed with 0 errors and 0 warnings after the record update.
- Command: `dotnet run --project tools/VerifyAll/VerifyAll.csproj --no-restore`
  Result: pass
  Evidence: VerifyAll completed all 16 steps successfully, including SimReplay, CombatBehavior, PerfSmoke, BalanceReport, and Godot headless QA.

Reviewer result:
- Status: pass pending gates.
- Design note: this parent is complete because the domains it names now route through `ResourceSystem`, `ProductionSystem`, `BuildingTargetCombatSystem`, and `TurretCombatSystem`; remaining mobile unit-vs-unit and movement work stays in later M1/M2 items.
- Required fixes: none.

Status:
- Pass.

Residual risks:
- `UnitBattlefieldBuildingTarget` still exists as a migration runtime and is tracked by the next M1 cleanup parent.
- Transitional combat systems duplicate subsets of generic `CombatSystem` and should be retired when mobile units fully run on EntityWorld.

TODO update:
- Marked done: parent M1 item `Move harvester, production-completion spawns, and building targets onto the EntityWorld path; then delete UnitBattlefield behavior methods.`
