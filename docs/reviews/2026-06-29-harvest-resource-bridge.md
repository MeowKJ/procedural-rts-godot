# Review Record - Harvest Resource Bridge

Step: Route UnitBattlefield harvest commands through EntityWorld resource-node mirrors as a bounded M1 migration slice.
Milestone: M1 EntityWorld Becomes Authoritative
Owner AI: Codex
Reviewer AI: Codex reviewer pass
Integrator AI: Codex

Scope:
- Files/folders: `scripts/core/units/runtime/UnitBattlefield.cs`, `scripts/core/sim/systems/CommandSystem.cs`, `tools/CombatBehavior/Program.cs`, `tools/ReviewGate/Program.cs`, `TODO.md`.
- Non-goals: full ResourceSystem takeover of the live UnitBattlefield harvest loop, deleting legacy harvester update methods, production-completion spawn migration, building-target deletion.

Implementation summary:
- `UnitBattlefield` now mirrors `ResourceFieldModel` records into EntityWorld `EntityKind.Resource` entities with `ResourceNodeComponentState`.
- `CommandHarvestSelected` now submits `HarvestEntityCommand` through the same command-buffer bridge used by selected unit commands.
- Harvest command results are copied back to legacy `UnitInstance` harvester state, including legacy field id, movement intent, and cargo state.
- `CommandSystem.ApplyHarvest` now clears manual weapon targets so EntityWorld harvest command semantics match the old direct path.
- Existing legacy harvest loop remains in place after command assignment, preserving current playable behavior while moving command intent onto the EntityWorld path.

Automated gates:
- Command: `dotnet build ProceduralRts.csproj --no-restore`
  Result: pass
  Evidence: build completed with 0 warnings and 0 errors.
- Command: `dotnet run --project tools/CombatBehavior/CombatBehavior.csproj --no-restore`
  Result: pass
  Evidence: Combat behavior passed with assertions for mirrored ResourceNode harvest routing and the existing full harvest/unload loop.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj harvestbridge --no-restore`
  Result: pass
  Evidence: `Errors: 0`, `Warnings: 0`, `ReviewGate passed.`
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj review --require-record=harvest-resource-bridge --no-restore`
  Result: pass
  Evidence: `Errors: 0`, `Warnings: 0`, `ReviewGate passed.`
- Command: `dotnet run --project tools/VerifyAll/VerifyAll.csproj --no-restore`
  Result: pass
  Evidence: all steps passed after this slice.

Manual/visual gates:
- Check: Visual QA
  Result: not run
  Evidence: this slice preserves current harvest visuals and resource views; headless Godot gates cover boot stability.

Reviewer result:
- Status: pass
- Required fixes: none
- Residual risks: Live harvest simulation is still completed by legacy `UnitBattlefield` update methods after the EntityWorld command assignment. Full ResourceSystem takeover remains open.

TODO update:
- Items marked done: nested M1 slice `UnitBattlefield harvest ResourceNode EntityCommandBuffer bridge`.
- Items left open: parent harvester/production/building migration item, full ResourceSystem live takeover, production-completion spawn migration, building target migration cleanup, legacy behavior deletion.
- Reason: tests and ReviewGate prove harvest command/resource-node bridge routing without claiming the broader M1 migration is complete.
