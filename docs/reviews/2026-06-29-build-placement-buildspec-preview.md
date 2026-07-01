Step: Move build placement preview data to BuildSpecCatalog.
Milestone: M1 EntityWorld Becomes Authoritative
Owner AI: Codex
Reviewer AI: Codex review pass
Integrator AI: Codex

Scope:
- Files/folders: `scripts/controllers/BuildPlacementController.cs`, `tools/ReviewGate/Program.cs`, `TODO.md`.
- Replaced the local `BuildingDefinition` accessor with `CurrentSpec()` backed by `BuildSpecCatalog`.
- Build placement preview/status now derives label, accent, and footprint from `BuildSpec`.
- Non-goals: no placement legality rewrite, no construction system work, no removal of `GameState.BuildingDefinitions`.

Automated gates:
- Command: `dotnet build ProceduralRts.csproj --no-restore`
  Result: pass
  Evidence: build completed successfully with 0 warnings and 0 errors.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj buildplacementbuildspec --no-restore`
  Result: pass
  Evidence: dedicated build placement BuildSpec gate completed with 0 errors and 0 warnings.
- Command: `dotnet run --project tools/CombatBehavior/CombatBehavior.csproj --no-restore`
  Result: pass
  Evidence: CombatBehavior completed with placement-adjacent build/projection, economy, combat, AI, and outcome scenarios intact.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore`
  Result: pass
  Evidence: full ReviewGate completed with 0 errors and 0 warnings after the record update.
- Command: `dotnet run --project tools/VerifyAll/VerifyAll.csproj --no-restore`
  Result: pass
  Evidence: VerifyAll completed all 16 steps successfully, including SimReplay, CombatBehavior, PerfSmoke, BalanceReport, and Godot headless QA.

Reviewer result:
- Status: pass pending gates.
- Design note: this removes another live UI dependency on split building runtime definitions while keeping placement behavior unchanged.
- Required fixes: none.

Status:
- Pass.

Residual risks:
- `GameState.ValidateBuildingPlacement` and `PlaceBuilding` still own placement authority until the construction/build-radius TODO items move placement into pure systems.

TODO update:
- Marked done: nested M1 slice `BuildPlacementController BuildSpec preview cleanup`.
