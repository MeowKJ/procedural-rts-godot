Step: Delete BuildingPresentationCatalog after moving building presentation metadata to BuildSpec.
Milestone: M1 EntityWorld Becomes Authoritative
Owner AI: Codex
Reviewer AI: Codex review pass
Integrator AI: Codex

Scope:
- Files/folders: `scripts/core/PresentationCatalog.cs`, `scripts/core/BuildingPresentationCatalog.cs`, `scripts/core/BuildingPresentationDescriptor.cs`, `tools/CombatBehavior/Program.cs`, `tools/ReviewGate/Program.cs`, `TODO.md`.
- Deleted `BuildingPresentationCatalog.cs`.
- Deleted `BuildingPresentationDescriptor.cs`.
- `PresentationCatalog.Building(...)` now builds building presentation descriptors from `BuildSpecCatalog.For(kind)` and `BuildSpec` metadata.
- Structure and turret gates now validate building presentation metadata through `BuildSpecCatalog` instead of the deleted catalog.
- Non-goals: deleting `BuildingKind`, deleting `BuildCatalog` compatibility projections, deleting `GameState.BuildingDefinitions`, or migrating unit presentation catalogs.

Automated gates:
- Command: `dotnet build ProceduralRts.csproj --no-restore`
  Result: pass
  Evidence: build completed successfully with 0 warnings and 0 errors.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj buildingpresentationcatalogdeleted --no-restore`
  Result: pass
  Evidence: dedicated deletion gate completed with 0 errors and 0 warnings.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj structures --no-restore`
  Result: pass
  Evidence: structures gate completed with 0 errors and 0 warnings after moving presentation checks to BuildSpec.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj turrets --no-restore`
  Result: pass
  Evidence: turret gate completed with 0 errors and 0 warnings after moving presentation checks to BuildSpec.
- Command: `dotnet run --project tools/CombatBehavior/CombatBehavior.csproj --no-restore`
  Result: pass
  Evidence: CombatBehavior completed with building presentation metadata assertions reading BuildSpec.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore`
  Result: pass
  Evidence: full ReviewGate completed with 0 errors and 0 warnings.
- Command: `dotnet run --project tools/VerifyAll/VerifyAll.csproj --no-restore`
  Result: pass
  Evidence: VerifyAll completed all 16 steps successfully, including SimReplay, CombatBehavior, PerfSmoke, BalanceReport, and Godot headless QA.

Reviewer result:
- Status: pass.
- Design note: this removes one legacy catalog from the parent deletion milestone while preserving existing `PresentationCatalog.Building` callers through the new BuildSpec-backed implementation.
- Required fixes: none identified after gates.

Status:
- Pass.

Residual risks:
- The broader deletion milestone remains open for `UnitKind`, `BuildingKind`, `GameState.Definitions`, and `UnitCatalog`.
- `BuildCatalog` and `GameState.BuildingDefinitions` are still compatibility projections from `BuildSpecCatalog`.
- `PresentationCatalog.Unit` still depends on the legacy unit presentation path.

TODO update:
- Marked done: nested M1 slice `BuildingPresentationCatalog deletion`.
- Updated parent legacy-deletion line to remove `BuildingPresentationCatalog` from the remaining open list.
