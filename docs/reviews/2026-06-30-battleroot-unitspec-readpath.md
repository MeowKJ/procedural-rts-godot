# Review Record - BattleRoot UnitSpec read path

Step: UnitSpec architecture phase 3 duplicate-data cleanup BattleRoot read-path slice
Milestone: M1 EntityWorld Becomes Authoritative
Owner AI: BattleRoot UnitSpec read-path worker / Codex
Reviewer AI: ReviewGate battlerootunitspecreadpath
Integrator AI: Integrator / Codex

Scope:
- Files/folders: `scripts/BattleRoot.cs`, `tools/ReviewGate/Program.cs`, `TODO.md`, `docs/reviews/2026-06-30-battleroot-unitspec-readpath.md`.
- Non-goals: editing `FootprintLayer`, editing `SelectionController`, changing gameplay authority, deleting legacy `UnitKind` compatibility, or changing building presentation paths.

Implementation summary:
- Moved BattleRoot legacy unit death VFX accent/radius styling through `UnitKindDesignBridge.TryGetSpec(...)`, `TryGetRuntimeDescriptor(...)`, `UnitPresentationCatalog.ForSpec(...)`, and the existing owner-color/environment-tone presentation palette.
- Moved old-runtime selected unit summary and selection icon samples off `PresentationCatalog.Unit(...)` and onto UnitSpec presentation descriptors.
- Replaced selected-summary harvester kind checks with UnitSpec economy role-tag checks while leaving non-target BattleRoot harvester logic out of scope.
- Added `ReviewGate battlerootunitspecreadpath` to lock the scoped BattleRoot read-path cleanup.
- Advanced the older `gamestatedefinitionspubliccleanup` gate so it now requires the UnitSpec death VFX read path and forbids regressing to `GameState.UnitDefinitionFor(death.Kind)`.

Automated gates:
- Command: `dotnet build ProceduralRts.csproj --no-restore`
  Result: pass
  Evidence: build completed successfully after the BattleRoot read-path migration.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore -- battlerootunitspecreadpath`
  Result: pass
  Evidence: narrow ReviewGate mode completed with 0 errors and 0 warnings.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore -- review --require-record=battleroot-unitspec-readpath`
  Result: pass
  Evidence: required review record check completed with 0 errors and 0 warnings.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore -- gamestatedefinitionspubliccleanup`
  Result: pass
  Evidence: stale BattleRoot death VFX expectation was advanced to the UnitSpec read path.
- Command: `dotnet run --project tools/VerifyAll/VerifyAll.csproj --no-restore`
  Result: pass
  Evidence: full 23-step verification suite passed after integration.

Manual/visual gates:
- Check: visual smoke not run.
  Result: not run.
  Evidence: this slice only changes metadata read paths for existing BattleRoot HUD/VFX calls.

Reviewer result:
- Status: pass-with-warnings
- Required fixes: none in the scoped BattleRoot read-path slice.
- Residual risks: legacy `UnitModel`, `UnitKind`, and compatibility catalogs remain for later M1 cleanup slices.

TODO update:
- Items marked done: none.
- Items left open: parent UnitSpec duplicate-data cleanup remains open.
- Reason: this is a narrow BattleRoot presentation/read-path cleanup, not full deletion of legacy unit compatibility data.
