# Review Record - FogOfWarQa UnitSpec read-path cleanup

Step: UnitSpec architecture phase 3 duplicate-data cleanup FogOfWarQa read-path slice
Milestone: M1 EntityWorld Becomes Authoritative
Owner AI: Codex
Reviewer AI: ReviewGate fogofwarqaunitspecreadpath / Integrator
Integrator AI: Codex

Scope:
- Files/folders: `tools/FogOfWarQa/Program.cs`, `tools/ReviewGate/Program.cs`, `TODO.md`, `docs/reviews/2026-06-30-fogofwarqa-unitspec-readpath.md`.
- Non-goals: changing fog behavior, changing fog visual math, changing building fixture data, deleting legacy `GameState.UnitDefinitionFor(...)`, or changing balance data.

Implementation summary:
- Added a narrow `RuntimeDescriptorFor(UnitKind kind)` helper in FogOfWarQa.
- FogOfWarQa unit fixture HP now reads from `UnitKindDesignBridge.TryGetRuntimeDescriptor(...)` / `UnitSpecRuntimeDescriptor.MaxHp`.
- Removed the FogOfWarQa `GameState.UnitDefinitionFor(kind)` unit fixture read path.
- Updated the older `gamestatedefinitionspubliccleanup` gate so FogOfWarQa now proves the UnitSpec bridge instead of the legacy narrow accessor.
- Added `ReviewGate fogofwarqaunitspecreadpath` to prevent this tool read path from regressing.

Automated gates:
- Command: `dotnet build ProceduralRts.csproj --no-restore`
  Result: pass
  Evidence: build completed successfully with 0 warnings and 0 errors.
- Command: `dotnet run --project tools/FogOfWarQa/FogOfWarQa.csproj --no-restore`
  Result: pass
  Evidence: FogOfWarQa passed mask channels, feathered edges, explored memory, hidden mobile enemies, static memory, camera-scoped texture updates, and 100-source performance smoke.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore -- fogofwarqaunitspecreadpath`
  Result: pass
  Evidence: ReviewGate fogofwarqaunitspecreadpath completed with 0 errors and 0 warnings.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore -- gamestatedefinitionspubliccleanup`
  Result: pass
  Evidence: upgraded public-cleanup gate completed with 0 errors and 0 warnings.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore -- review --require-record=fogofwarqa-unitspec-readpath`
  Result: pass
  Evidence: required review record check completed with 0 errors and 0 warnings.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore`
  Result: pass
  Evidence: full ReviewGate completed with 0 errors and 0 warnings.
- Command: `dotnet run --project tools/VerifyAll/VerifyAll.csproj --no-restore`
  Result: pass
  Evidence: VerifyAll completed 23/23 checks after the slice.

Manual/visual gates:
- Check: visual inspection not required for this deterministic tool fixture read-path migration.
  Result: not run.
  Evidence: no runtime visuals changed; fog mask math and building fixture geometry are unchanged.

Reviewer result:
- Status: pass
- Required fixes: none after automated gates.
- Residual risks: legacy `GameState.UnitDefinitionFor(...)` remains for other compatibility paths and migration tools.

TODO update:
- Items marked done: none.
- Items left open: parent UnitSpec duplicate-data cleanup remains open.
- Reason: this is one scoped FogOfWarQa unit fixture read-path cleanup, not full legacy `UnitKind` deletion.
