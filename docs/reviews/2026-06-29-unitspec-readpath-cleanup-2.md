# Review Record - UnitSpec read path cleanup 2

Step: UnitSpec duplicate-data cleanup second slice
Milestone: M1 EntityWorld Becomes Authoritative
Owner AI: Worker-M1B / Codex
Reviewer AI: Codex review pass
Integrator AI: Main thread

Scope:
- Files/folders: `tools/CombatBehavior/Program.cs`, `tools/ReviewGate/Program.cs`, `docs/reviews/2026-06-29-unitspec-readpath-cleanup-2.md`.
- Non-goals: deleting `UnitKind`, `UnitCatalog`, or `FactionCatalog`; changing `GameState`; changing live combat, construction, movement, autonomy, replay, or command systems; updating `TODO.md`.

Implementation summary:
- Moved CombatBehavior read-only tier QA from `GameState.UnitDefinitionValues` to `UnitDesignDefinitionCatalog.RuntimeDescriptors.Values`.
- Moved CombatBehavior read-only armor/domain QA from `GameState.UnitDefinitionValues` to the same UnitSpec runtime descriptor read path.
- Updated ReviewGate so the UnitDesign definition catalog gate preserves the second cleanup slice and rejects new CombatBehavior reads of `GameState.UnitDefinitionValues`.

Automated gates:
- Command: `dotnet build ProceduralRts.csproj --no-restore`
  Result: pass
  Evidence: build completed successfully with 0 warnings and 0 errors.
- Command: `dotnet run --project tools/CombatBehavior/CombatBehavior.csproj --no-restore`
  Result: pass
  Evidence: CombatBehavior completed successfully.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj unitdesigndefinitioncatalog --no-restore`
  Result: pass
  Evidence: dedicated UnitDesign definition catalog gate completed with 0 errors and 0 warnings.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj review --require-record=unitspec-readpath-cleanup-2 --no-restore`
  Result: pass
  Evidence: review record gate completed with 0 errors and 0 warnings.

Manual/visual gates:
- Check: visual inspection not required.
  Result: not run.
  Evidence: this slice only changes QA/read-only data access and ReviewGate checks.

Reviewer result:
- Status: pass-with-warnings
- Required fixes: none identified in the scoped review.
- Residual risks: legacy `UnitKind`, `UnitCatalog`, `FactionCatalog`, `GameState.UnitDefinitionFor`, and keyed definition entry reads remain for later migration slices.

TODO update:
- Items marked done: none.
- Items left open: parent UnitSpec duplicate-data cleanup and later deletion of legacy unit/faction catalogs.
- Reason: this is a narrow read-path cleanup slice; the main thread owns TODO integration.
