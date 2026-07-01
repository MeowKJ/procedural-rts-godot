# Review Record - Production Completion Bridge

Step: Route UnitBattlefield production completion through EntityWorld `ProductionSystem` as a bounded M1 migration slice.
Milestone: M1 EntityWorld Becomes Authoritative
Owner AI: Codex
Reviewer AI: Codex reviewer pass
Integrator AI: Codex

Scope:
- Files/folders: `scripts/core/units/runtime/UnitBattlefield.cs`, `tools/CombatBehavior/Program.cs`, `tools/ReviewGate/Program.cs`, `TODO.md`.
- Non-goals: production enqueue/cancel command routing, removing legacy production UI wrappers, full deletion of `UnitBattlefield` behavior methods, construction/build placement migration.

Implementation summary:
- `UnitBattlefield.UpdateProductionQueues` now syncs producer building mirrors and advances queues through EntityWorld `ProductionSystem`.
- Completed units are spawned by `ProductionSystem` as EntityWorld unit entities.
- `UnitBattlefield` adopts newly spawned EntityWorld units into legacy `UnitInstance` presentation/runtime records instead of creating a second EntityWorld mirror.
- Producer queues are synchronized back from EntityWorld after production advancement.
- Existing `ProductionCompleted` events remain available for HUD/audio/view code.

Automated gates:
- Command: `dotnet build ProceduralRts.csproj --no-restore`
  Result: pass
  Evidence: build completed with 0 warnings and 0 errors.
- Command: `dotnet run --project tools/CombatBehavior/CombatBehavior.csproj --no-restore`
  Result: pass
  Evidence: Combat behavior passed with assertions proving production completion adopts EntityWorld-spawned units and syncs producer queues.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj productionbridge --no-restore`
  Result: pass
  Evidence: `Errors: 0`, `Warnings: 0`, `ReviewGate passed.`
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj review --require-record=production-completion-bridge --no-restore`
  Result: pass
  Evidence: `Errors: 0`, `Warnings: 0`, `ReviewGate passed.`
- Command: `dotnet run --project tools/VerifyAll/VerifyAll.csproj --no-restore`
  Result: pass
  Evidence: all steps passed after this slice.

Manual/visual gates:
- Check: Visual QA
  Result: not run
  Evidence: existing production events and UnitInstanceView adoption path are preserved; headless Godot gates cover boot stability.

Reviewer result:
- Status: pass
- Required fixes: none
- Residual risks: Production enqueue/cancel still use legacy wrapper code and should be migrated separately. Legacy `UnitBattlefield` methods still own parts of runtime behavior.

TODO update:
- Items marked done: nested M1 slice `UnitBattlefield production completion EntityWorld spawn bridge`.
- Items left open: parent harvester/production/building migration item, production enqueue/cancel command routing, building target cleanup, legacy behavior deletion.
- Reason: tests and ReviewGate prove completion spawn ownership moved to EntityWorld without claiming the entire production/building migration is complete.
