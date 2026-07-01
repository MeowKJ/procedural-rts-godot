# Review Record - SimulationSmoke UnitSpec read-path cleanup

Step: UnitSpec architecture phase 3 duplicate-data cleanup SimulationSmoke read-path slice
Milestone: M1 EntityWorld Becomes Authoritative
Owner AI: Codex
Reviewer AI: ReviewGate simulationsmokeunitspecreadpath / Integrator
Integrator AI: Codex

Scope:
- Files/folders: `tools/SimulationSmoke/Program.cs`, `tools/ReviewGate/Program.cs`, `TODO.md`, `docs/reviews/2026-06-30-simulationsmoke-unitspec-readpath.md`.
- Non-goals: deleting legacy `GameState.Definition(...)`, migrating harvester classification, changing smoke duration, changing simulation behavior, or changing balance data.

Implementation summary:
- Added a narrow `RuntimeDescriptorFor(UnitModel unit)` helper in SimulationSmoke.
- SimulationSmoke unit world-bound checks now read radius from `UnitKindDesignBridge.TryGetRuntimeDescriptor(...)` / `UnitSpecRuntimeDescriptor.Radius`.
- Removed the SimulationSmoke `state.Definition(unit)` read path for unit radius validation.
- Added `ReviewGate simulationsmokeunitspecreadpath` to prevent this tool read path from regressing.

Automated gates:
- Command: `dotnet build ProceduralRts.csproj --no-restore`
  Result: pass
  Evidence: build completed successfully with 0 warnings and 0 errors.
- Command: `dotnet run --project tools/SimulationSmoke/SimulationSmoke.csproj --no-restore`
  Result: pass
  Evidence: SimulationSmoke passed its 300 second smoke with 10 orders, 10 completions, 2 waves, and outcome InProgress.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore -- simulationsmokeunitspecreadpath`
  Result: pass
  Evidence: ReviewGate simulationsmokeunitspecreadpath completed with 0 errors and 0 warnings.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore -- review --require-record=simulationsmoke-unitspec-readpath`
  Result: pass
  Evidence: required review record check completed with 0 errors and 0 warnings.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore`
  Result: pass
  Evidence: full ReviewGate completed with 0 errors and 0 warnings.
- Command: `dotnet run --project tools/VerifyAll/VerifyAll.csproj --no-restore`
  Result: pass
  Evidence: VerifyAll completed 23/23 checks after the slice.

Manual/visual gates:
- Check: visual inspection not required for this deterministic tool read-path migration.
  Result: not run.
  Evidence: no runtime visuals changed; smoke assertions and simulation stepping are unchanged.

Reviewer result:
- Status: pass
- Required fixes: none after automated gates.
- Residual risks: SimulationSmoke still uses legacy `GameState.IsHarvesterUnit(...)` for harvester assignment because this slice only moves the radius validation read path.

TODO update:
- Items marked done: none.
- Items left open: parent UnitSpec duplicate-data cleanup remains open.
- Reason: this is one scoped tool read-path cleanup, not full legacy `UnitKind` deletion.
