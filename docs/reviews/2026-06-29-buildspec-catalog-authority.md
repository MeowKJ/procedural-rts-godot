Step: Invert building data authority into BuildSpecCatalog.
Milestone: M1 EntityWorld Becomes Authoritative
Owner AI: Codex
Reviewer AI: Codex review pass
Integrator AI: Codex

Scope:
- Files/folders: `scripts/core/BuildSpec.cs`, `scripts/core/BuildSpecCatalog.cs`, `scripts/core/BuildCatalog.cs`, `scripts/core/GameState.cs`, `tools/CombatBehavior/Program.cs`, `tools/ReviewGate/Program.cs`, `TODO.md`.
- `BuildSpecCatalog` now directly owns complete building/build specs instead of composing from `BuildCatalog` and `GameState.BuildingDefinitions`.
- `BuildCatalog.Definitions` and `GameState.BuildingDefinitions` remain as compatibility projections generated from `BuildSpec`.
- Non-goals: no deletion of the legacy public catalog types, no runtime behavior rewrite beyond preserving data equivalence.

Automated gates:
- Command: `dotnet build ProceduralRts.csproj --no-restore`
  Result: pass
  Evidence: build completed successfully with 0 warnings and 0 errors.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj buildspecauthority --no-restore`
  Result: pass
  Evidence: dedicated BuildSpec authority gate completed with 0 errors and 0 warnings.
- Command: `dotnet run --project tools/CombatBehavior/CombatBehavior.csproj --no-restore`
  Result: pass
  Evidence: CombatBehavior completed with the legacy-catalog projection assertion intact.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore`
  Result: pass
  Evidence: full ReviewGate completed with 0 errors and 0 warnings after updating structure/turret gates to the new authority direction.
- Command: `dotnet run --project tools/VerifyAll/VerifyAll.csproj --no-restore`
  Result: pass
  Evidence: VerifyAll completed all 16 steps successfully, including SimReplay, CombatBehavior, PerfSmoke, BalanceReport, and Godot headless QA.

Reviewer result:
- Status: pass.
- Design note: this is the real data-authority turn for the M1 building merge; old APIs survive, but they no longer own values.
- Required fixes: none identified before gates.

Status:
- Pass.

Residual risks:
- Legacy `BuildDefinition` and `BuildingDefinition` types still exist and many old call sites still read their projected dictionaries.
- `BuildingPresentationCatalog` remains separate and is not yet generated from `BuildSpec`.

TODO update:
- Marked done: nested M1 slice `BuildSpecCatalog authority inversion`.
