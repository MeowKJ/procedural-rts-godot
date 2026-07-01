# Review Record - Production Command Bridge

Step: Route UnitBattlefield production enqueue/cancel through EntityWorld `ProductionSystem` commands.
Milestone: M1 EntityWorld Becomes Authoritative
Owner AI: Codex
Reviewer AI: Codex reviewer pass
Integrator AI: Codex

Scope:
- Files/folders: `scripts/core/units/runtime/UnitBattlefield.cs`, `tools/CombatBehavior/Program.cs`, `tools/ReviewGate/Program.cs`, `TODO.md`.
- Non-goals: deleting legacy production UI wrappers, repeat-production UI, construction/build placement migration, full `UnitBattlefield` removal.

Implementation summary:
- `EnqueueProduction` now syncs credits into EntityWorld and submits `ProduceEntityCommand` to `ProductionSystem`.
- `CancelFirstProduction` now submits `CancelProductionEntityCommand` to `ProductionSystem`.
- Credits and producer queues are synchronized back from EntityWorld to legacy UI/runtime state after production commands.
- Existing status text, `ProductionQueued`, `ProductionCompleted`, and production option UI surfaces remain intact.

Automated gates:
- Command: `dotnet build ProceduralRts.csproj --no-restore`
  Result: pass
  Evidence: build completed with 0 warnings and 0 errors.
- Command: `dotnet run --project tools/CombatBehavior/CombatBehavior.csproj --no-restore`
  Result: pass
  Evidence: Combat behavior passed with assertions proving enqueue/cancel route through EntityWorld and sync queue/credits back.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj productionbridge --no-restore`
  Result: pass
  Evidence: `Errors: 0`, `Warnings: 0`, `ReviewGate passed.`
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj review --require-record=production-command-bridge --no-restore`
  Result: pass
  Evidence: `Errors: 0`, `Warnings: 0`, `ReviewGate passed.`
- Command: `dotnet run --project tools/VerifyAll/VerifyAll.csproj --no-restore`
  Result: pass
  Evidence: all steps passed after this slice.

Manual/visual gates:
- Check: Visual QA
  Result: not run
  Evidence: existing HUD/status/event surfaces remain in use; headless Godot gates cover boot stability.

Reviewer result:
- Status: pass
- Required fixes: none
- Residual risks: Legacy wrappers still exist to provide UI/status compatibility. Build/construction and final `UnitBattlefield` behavior deletion remain open.

TODO update:
- Items marked done: nested M1 slice `UnitBattlefield production enqueue/cancel EntityWorld command bridge`.
- Items left open: parent harvester/production/building migration item, building target cleanup, construction migration, legacy behavior deletion.
- Reason: tests and ReviewGate prove production command routing through EntityWorld without claiming the entire M1 migration is complete.
