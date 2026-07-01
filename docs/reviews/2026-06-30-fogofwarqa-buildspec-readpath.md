# Review Record - FogOfWarQa BuildSpec read-path cleanup

Step: Migration cleanup FogOfWarQa BuildSpec read-path slice
Milestone: M1 EntityWorld Becomes Authoritative
Owner AI: Codex
Reviewer AI: ReviewGate fogofwarqabuildspecreadpath / Integrator
Integrator AI: Codex

Scope:
- Files/folders: `tools/FogOfWarQa/Program.cs`, `tools/ReviewGate/Program.cs`, `TODO.md`, `docs/reviews/2026-06-30-fogofwarqa-buildspec-readpath.md`.
- Non-goals: deleting `GameState.BuildingDefinitions`, migrating the FogOfWarQa unit fixture off `GameState.UnitDefinitionFor(...)`, changing fog behavior, or changing building data.

Implementation summary:
- FogOfWarQa building fixture HP now reads `BuildSpecCatalog.For(kind).MaxHp`.
- FogOfWarQa explored-memory building rectangles now read `BuildSpecCatalog.For(building.Kind).Footprint`.
- Removed FogOfWarQa reads of compatibility `GameState.BuildingDefinitions`.
- Added `ReviewGate fogofwarqabuildspecreadpath` to prevent this tool read path from regressing.

Automated gates:
- Command: `dotnet build ProceduralRts.csproj --no-restore`
  Result: pass
  Evidence: build completed successfully with 0 warnings and 0 errors before gate wiring.
- Command: `dotnet run --project tools/FogOfWarQa/FogOfWarQa.csproj --no-restore`
  Result: pass
  Evidence: FogOfWarQa passed after the BuildSpec read-path cleanup.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore -- fogofwarqabuildspecreadpath`
  Result: pass
  Evidence: ReviewGate fogofwarqabuildspecreadpath completed with 0 errors and 0 warnings.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore -- review --require-record=fogofwarqa-buildspec-readpath`
  Result: pass
  Evidence: required review record check completed with 0 errors and 0 warnings.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore`
  Result: pass
  Evidence: full ReviewGate completed with 0 errors and 0 warnings after the slice.
- Command: `dotnet run --project tools/VerifyAll/VerifyAll.csproj --no-restore`
  Result: pass
  Evidence: VerifyAll completed 23/23 checks after the slice.

Manual/visual gates:
- Check: visual inspection not required for this tool read-path migration.
  Result: not run.
  Evidence: no runtime visuals changed; fog geometry formulas are unchanged.

Reviewer result:
- Status: pass
- Required fixes: none known before gate execution.
- Residual risks: FogOfWarQa still uses `GameState.UnitDefinitionFor(kind)` for unit fixture HP because that remains the current narrow unit compatibility accessor until the remaining UnitSpec tool-read cleanup slices are done.

TODO update:
- Items marked done: `FogOfWarQa BuildSpec read-path cleanup`.
- Items left open: broader Migration cleanup remains open.
- Reason: this removes FogOfWarQa's building compatibility reads, not all building compatibility projections.
