Step: Route explicit UnitBattlefield CommandAttackUnits APIs through EntityCommandBuffer.
Milestone: M1 EntityWorld Becomes Authoritative
Owner AI: Codex
Reviewer AI: Codex review pass
Integrator AI: Codex

Scope:
- Files/folders: `scripts/core/units/runtime/UnitBattlefield.cs`, `tools/CombatBehavior/Program.cs`, `tools/ReviewGate/Program.cs`, `TODO.md`.
- Changed both public `CommandAttackUnits(...)` overloads to submit `GroupAttackEntityCommand` against EntityWorld unit/building mirrors.
- Removed the private direct legacy `CommandAttackUnit(...)` mutation helpers.
- Non-goals: no combat-system authority flip, no AI planner rewrite, no behavior tuning.

Automated gates:
- Command: `dotnet build ProceduralRts.csproj --no-restore`
  Result: pass
  Evidence: build completed successfully with 0 warnings and 0 errors.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj attackunitscommandbridge --no-restore`
  Result: pass
  Evidence: explicit attack-units command bridge gate completed successfully.
- Command: `dotnet run --project tools/CombatBehavior/CombatBehavior.csproj --no-restore`
  Result: pass
  Evidence: CombatBehavior completed including the explicit attack-units command-buffer check.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore`
  Result: pass
  Evidence: full ReviewGate completed successfully.
- Command: `dotnet run --project tools/VerifyAll/VerifyAll.csproj --no-restore`
  Result: pass
  Evidence: VerifyAll completed all 15 steps successfully.

Reviewer result:
- Status: pass.
- Design note: AI wave attack commands now share the same command-buffer path as selected player attacks, reducing authority drift.
- Required fixes: none.

Status:
- Pass.

Residual risks:
- Unit movement and combat ticking still execute through legacy `UnitBattlefield.Update` behavior during migration.
- Building attack behavior remains a later authority cleanup item.

TODO update:
- Marked done: nested M1 slice `UnitBattlefield explicit attack-units EntityCommandBuffer bridge`.
- Left open: parent M1 command authority cleanup until remaining gameplay behavior is moved or deleted.
