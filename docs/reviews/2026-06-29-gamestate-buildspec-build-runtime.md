Step: Move GameState build runtime reads to BuildSpecCatalog.
Milestone: M1 EntityWorld Becomes Authoritative
Owner AI: Codex
Reviewer AI: Codex review pass
Integrator AI: Codex

Scope:
- Files/folders: `scripts/core/GameState.cs`, `tools/CombatBehavior/Program.cs`, `tools/ReviewGate/Program.cs`, `TODO.md`.
- Build option snapshots now enumerate `BuildSpecCatalog` and expose category, icon, cost, build time, footprint, power, radius, and prerequisite state from the unified spec.
- Placement validation, placed building HP, production producer labels, and produced-unit spawn footprint now read `BuildSpecCatalog`.
- Non-goals: no construction-system rewrite, no build-radius command authority move, no removal of `BuildingDefinitions` or `BuildCatalog` yet.

Automated gates:
- Command: `dotnet build ProceduralRts.csproj --no-restore`
  Result: pass
  Evidence: build completed successfully with 0 warnings and 0 errors.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj gamestatebuildspecbuildruntime --no-restore`
  Result: pass
  Evidence: dedicated GameState BuildSpec build-runtime gate completed with 0 errors and 0 warnings.
- Command: `dotnet run --project tools/CombatBehavior/CombatBehavior.csproj --no-restore`
  Result: pass
  Evidence: CombatBehavior completed with the new BuildSpec-derived build option and placed-building HP assertions intact.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore`
  Result: pass
  Evidence: full ReviewGate completed with 0 errors and 0 warnings after the new gate and TODO update.
- Command: `dotnet run --project tools/VerifyAll/VerifyAll.csproj --no-restore`
  Result: pass
  Evidence: VerifyAll completed all 16 steps successfully, including SimReplay, CombatBehavior, PerfSmoke, BalanceReport, and Godot headless QA.

Reviewer result:
- Status: pass.
- Design note: this keeps the live `GameState` build surface compatible while shrinking direct dependence on the split build/runtime catalogs.
- Required fixes: none identified before gates.

Status:
- Pass.

Residual risks:
- `GameState` still has legacy combat, fog, and economy call sites that read `BuildingDefinition`; those remain outside this bounded build-runtime slice.
- `BuildSpecCatalog` still composes from `BuildingDefinitions` and `BuildCatalog`, so deleting those catalogs is still a later migration step.

TODO update:
- Marked done: nested M1 slice `GameState BuildSpec build runtime cleanup`.
