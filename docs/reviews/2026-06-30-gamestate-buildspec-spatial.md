# Review Record - GameState BuildSpec spatial cleanup

Step: Migration cleanup GameState BuildSpec spatial slice
Milestone: M1 EntityWorld Becomes Authoritative
Owner AI: Worker Bohr
Reviewer AI: ReviewGate gamestatebuildspecspatial / Integrator
Integrator AI: Integrator / Codex

Scope:
- Files/folders: `scripts/core/GameState.cs`, `tools/ReviewGate/Program.cs`, `TODO.md`, `docs/reviews/2026-06-30-gamestate-buildspec-spatial.md`.
- Non-goals: changing building combat target profiles, building hit alert labels, production lane labels, or deleting `BuildingDefinition`.

Implementation summary:
- Building exploration rectangles now use `BuildSpec.Footprint`.
- Building fog-of-war vision sources now use `BuildSpec.SightRange`.
- Building placement obstacles now use `BuildSpec.Footprint`.
- Building combat radius now uses `BuildSpec.Footprint`.
- Added `ReviewGate gamestatebuildspecspatial` to keep these spatial paths off legacy `Definition(building)` reads.

Automated gates:
- Command: `dotnet build ProceduralRts.csproj --no-restore`
  Result: pass
  Evidence: build completed successfully with 0 warnings and 0 errors.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore -- gamestatebuildspecspatial`
  Result: pass
  Evidence: ReviewGate gamestatebuildspecspatial completed with 0 errors and 0 warnings.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore -- review --require-record=gamestate-buildspec-spatial`
  Result: pass
  Evidence: required review record check completed with 0 errors and 0 warnings.

Manual/visual gates:
- Check: visual inspection not required for this narrow runtime data-source migration.
  Result: not run.
  Evidence: geometry formulas remain equivalent; only the source moved to `BuildSpecCatalog`.

Reviewer result:
- Status: pass
- Required fixes: none after automated gate.
- Residual risks: `GameState` still contains separate legacy `Definition(building)` reads for weapon lookup, labels, target priority, and damage effectiveness. Those remain later cleanup slices.

TODO update:
- Items marked done: `GameState BuildSpec spatial cleanup`.
- Items left open: broader Migration cleanup remains open.
- Reason: this removes spatial/vision legacy reads, not all `GameState` building compatibility reads.
