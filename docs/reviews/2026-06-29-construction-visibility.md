# Review Record - Construction visibility gate

Step:
M3 construction build visibility minimal slice.
Milestone:
EntityWorld construction authority and deterministic replay coverage.
Owner AI:
Worker B.
Reviewer AI:
Codex self-review with SimReplay and ReviewGate coverage.
Integrator AI:
Pending human/integrator review.

Scope:
- Files/folders: `scripts/core/PlacementMath.cs`, `scripts/core/sim/systems/ConstructionSystem.cs`, `tools/SimReplay/Program.cs`, `tools/ReviewGate/Program.cs`, `docs/reviews/2026-06-29-construction-visibility.md`.
- Non-goals: UI placement preview, explored-memory persistence, CombatSystem, UnitDesignDefinitionCatalog, and TODO.md updates.

Implementation summary:
- Added `PlacementBuildVisibility` and `PlacementMath.HasBuildVisibility` for pure footprint sampling against deterministic build visibility sources.
- ConstructionSystem now requires build visibility for StartConstructionEntityCommand placement and emits `placement.notVisible` through the existing ConstructionRejectedEvent path.
- Build visibility sources come from live completed self/allied VisionComponentState entities, preserving power/build-radius/terrain/overlap/cancel/refund behavior.
- SimReplay adds `construction-visibility` coverage for an inside-build-radius but unseen placement rejection plus a visible control build.
- ReviewGate adds the narrow `constructionvisibility` mode.

Automated gates:
- Command: `dotnet build ProceduralRts.csproj`
  Result: pass.
  Evidence: Build succeeded with 0 warnings and 0 errors.
- Command: `dotnet run --project tools/SimReplay/SimReplay.csproj`
  Result: pass.
  Evidence: `SimReplay PASSED`; `OK [construction-visibility]: rejected 1, buildings 2, credits 600.`
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj constructionvisibility`
  Result: pass.
  Evidence: ReviewGate passed with 0 errors and 0 warnings.

Manual/visual gates:
- Check: UI/HUD review
  Result: not applicable.
  Evidence: Pure simulation placement authority only.

Reviewer result:
- Status: pass
- Required fixes: None after deterministic replay coverage.
- Residual risks: EntityWorld still has only live current visibility for construction authority; explored-memory placement policy remains future work if the design wants historical exploration separate from current vision.

TODO update:
- Items marked done: None.
- Items left open: Broader construction placement UX and fog/explored-memory policy remain open for integrator follow-up.
- Reason: This is a minimal pure-sim slice with narrow replay and ReviewGate coverage.
