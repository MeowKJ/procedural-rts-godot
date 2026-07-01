# Review Record - Unit Entity Projection Sync

Step: Add a bounded M1 bridge so `UnitBattlefield` units have EntityWorld mirrors and views can read projections.
Milestone: M1 EntityWorld Becomes Authoritative
Owner AI: Codex
Reviewer AI: Codex reviewer pass
Integrator AI: Codex

Scope:
- Files/folders: `scripts/core/units/runtime/UnitInstance.cs`, `scripts/core/units/runtime/UnitBattlefield.cs`, `scripts/world/UnitInstanceView.cs`, `scripts/BattleRoot.cs`, `tools/CombatBehavior/Program.cs`, `tools/ReviewGate/Program.cs`, `TODO.md`.
- Non-goals: flipping the live game to full EntityWorld unit authority, routing player input through `EntityCommandBuffer`, deleting legacy `UnitBattlefield` behavior, migrating harvester/production/building behavior off the legacy runtime.

Implementation summary:
- `UnitInstance` now retains the `EntityId` of its EntityWorld mirror.
- `UnitBattlefield.Spawn` creates an EntityWorld unit mirror through `SpawnUnit`, then synchronizes health, selection, movement, command intent, weapon target, presentation pulses, and harvester cargo state.
- Dead `UnitInstance` records remove their EntityWorld mirrors.
- `UnitBattlefield.UnitProjection` and `UnitProjections` expose `EntityProjection` snapshots for presentation.
- `UnitInstanceView` now accepts a projection provider and prefers projected position, owner, facing, hp, and selection state while still using the existing `UnitSpec` art recipe during the transition.
- `BattleRoot` wires each `UnitInstanceView` to `UnitBattlefield.UnitProjection`.

Automated gates:
- Command: `dotnet build ProceduralRts.csproj --no-restore`
  Result: pass
  Evidence: build completed with 0 warnings and 0 errors.
- Command: `dotnet run --project tools/CombatBehavior/CombatBehavior.csproj --no-restore`
  Result: pass
  Evidence: Combat behavior passed after new spawn/projection/selection/movement/death mirror assertions.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj unitentitysync --no-restore`
  Result: pass
  Evidence: `Errors: 0`, `Warnings: 0`, `ReviewGate passed.`
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj review --require-record=unit-entity-projection-sync --no-restore`
  Result: pass
  Evidence: `Errors: 0`, `Warnings: 0`, `ReviewGate passed.`
- Command: `dotnet run --project tools/VerifyAll/VerifyAll.csproj --no-restore`
  Result: pass
  Evidence: all steps passed after this slice.

Manual/visual gates:
- Check: Visual QA
  Result: not run
  Evidence: this slice changes the view data source but keeps the existing art path and headless Godot gates cover boot stability.

Reviewer result:
- Status: pass
- Required fixes: none
- Residual risks: `UnitBattlefield` still owns live gameplay authority for unit behavior. The broader M1 item remains open until a `UseEntityWorldUnits` comparison/flip path exists and live input routes through `EntityCommandBuffer`.

TODO update:
- Items marked done: nested M1 slice `UnitBattlefield UnitInstance EntityWorld projection mirror`.
- Items left open: parent M1 sync/flag flip, live input command buffer routing, harvester/production/building target migration, cleanup/deletion of legacy kinds and runtime behavior.
- Reason: automated behavior tests and ReviewGate prove the bounded spawn/state/projection bridge, but not full EntityWorld authority.
