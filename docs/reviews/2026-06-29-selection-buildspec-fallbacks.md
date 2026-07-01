Step: Route SelectionController building fallback definition reads through BuildSpecCatalog as a bounded M1 migration cleanup slice.
Milestone: M1 EntityWorld Becomes Authoritative
Owner AI: Codex
Reviewer AI: Codex review pass
Integrator AI: Codex

Scope:
- Files/folders: `scripts/controllers/SelectionController.cs`, `tools/ReviewGate/Program.cs`, `TODO.md`.
- Updated old-runtime selected-building rally line accent selection to read `BuildSpecCatalog.For(building.Kind).Accent`.
- Removed the unused hover-affordance `State.Definition(building)` call.
- Non-goals: no deletion of old runtime branches, no command input rewrite, no `BuildingModel` or `BuildingKind` removal.

Automated gates:
- Command: `dotnet build ProceduralRts.csproj --no-restore`
  Result: pass
  Evidence: build completed successfully with 0 warnings and 0 errors.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj selectionbuildspecfallbacks --no-restore`
  Result: pass
  Evidence: SelectionController BuildSpec fallback gate completed successfully.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore`
  Result: pass
  Evidence: full ReviewGate completed successfully.
- Command: `dotnet run --project tools/VerifyAll/VerifyAll.csproj --no-restore`
  Result: pass
  Evidence: VerifyAll completed all 14 steps successfully.

Reviewer result:
- Status: pass.
- Design note: this keeps the old-runtime visual fallback alive while removing another direct building-definition dependency from presentation code.
- Required fixes: none.

Status:
- Pass.

Residual risks:
- `CombatEffectsLayer` still has an old-runtime building hit-pulse fallback that reads `GameState.Definition(building)`.
- Legacy building models and catalog bridges remain until the broader M1 migration is complete.

TODO update:
- Marked done: nested M1 slice `SelectionController BuildSpec fallback cleanup`.
- Left open: parent M1 legacy deletion and remaining fallback cleanup.
