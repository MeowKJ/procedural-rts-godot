Step: Remove public GameState.Definitions access during UnitKind migration.
Milestone: M1 EntityWorld Becomes Authoritative
Owner AI: Codex
Reviewer AI: Codex review pass
Integrator AI: Codex

Scope:
- Files/folders: `scripts/core/GameState.cs`, `scripts/BattleRoot.cs`, `scripts/world/PathDebugLayer.cs`, `tools/FogOfWarQa/Program.cs`, `tools/CombatBehavior/Program.cs`, `tools/ReviewGate/Program.cs`, `TODO.md`.
- Replaced public `GameState.Definitions` with private `LegacyUnitDefinitions`.
- Added narrow compatibility accessors: `UnitDefinitionFor(...)`, `HasUnitDefinition(...)`, `UnitDefinitionValues`, and `UnitDefinitionEntries`.
- Updated BattleRoot, PathDebugLayer, FogOfWarQa, and CombatBehavior to stop reading the removed public dictionary.
- Non-goals: deleting `UnitKind`, deleting `UnitCatalog`, replacing old `GameState` combat simulation, or migrating legacy `UnitModel` data.

Automated gates:
- Command: `dotnet build ProceduralRts.csproj --no-restore`
  Result: pass
  Evidence: build completed successfully with 0 warnings and 0 errors.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj gamestatedefinitionspubliccleanup --no-restore`
  Result: pass
  Evidence: dedicated GameState definitions public-surface gate completed with 0 errors and 0 warnings.
- Command: `dotnet run --project tools/CombatBehavior/CombatBehavior.csproj --no-restore`
  Result: pass
  Evidence: CombatBehavior completed with unit definition checks using the narrow accessors.
- Command: `dotnet run --project tools/FogOfWarQa/FogOfWarQa.csproj --no-restore`
  Result: pass
  Evidence: FogOfWarQa completed with fixture unit HP seeded through `UnitDefinitionFor`.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore`
  Result: pass
  Evidence: full ReviewGate completed with 0 errors and 0 warnings.
- Command: `dotnet run --project tools/VerifyAll/VerifyAll.csproj --no-restore`
  Result: pass
  Evidence: VerifyAll completed all 16 steps successfully, including SimReplay, CombatBehavior, FogOfWarQa, PerfSmoke, BalanceReport, and Godot headless QA.

Reviewer result:
- Status: pass.
- Design note: this does not remove the legacy unit definition table yet; it removes the public dictionary dependency so the backing source can be swapped or deleted in later UnitCatalog/UnitKind migration slices.
- Required fixes: none identified after gates.

Status:
- Pass.

Residual risks:
- `UnitCatalog` still backs the private compatibility table.
- `UnitKind` and `UnitModel` remain in the legacy runtime and legacy tests.
- `UnitPresentationCatalog` still depends on legacy UnitKind presentation descriptors.

TODO update:
- Marked done: nested M1 slice `GameState.Definitions public surface cleanup`.
- Updated parent legacy-deletion line to remove `GameState.Definitions` from the remaining open list.
