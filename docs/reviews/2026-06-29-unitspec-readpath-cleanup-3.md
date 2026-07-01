# Review Record - UnitSpec read path cleanup 3

Step: UnitSpec duplicate-data cleanup third read-path slice
Milestone: M1 EntityWorld Becomes Authoritative
Owner AI: Worker-M1C / Codex
Reviewer AI: Codex self-review
Integrator AI: Main thread

Scope:
- Files/folders: `scripts/core/units/UnitDesignDefinitionCatalog.cs`, `tools/CombatBehavior/Program.cs`, `tools/ReviewGate/Program.cs`, `docs/reviews/2026-06-29-unitspec-readpath-cleanup-3.md`.
- Non-goals: deleting `UnitKind`, `UnitCatalog`, or `FactionCatalog`; changing live `GameState`, `UnitBattlefield`, combat, construction, command, replay, or start-loadout behavior; updating `TODO.md`.

Implementation summary:
- Added `UnitDesignDefinitionCatalog.WithRole(UnitRoleTag roleTag)` as a UnitSpec runtime descriptor role-query helper.
- Moved CombatBehavior harvester/economy class QA from `GameState.UnitDefinitionEntries` to `UnitDesignDefinitionCatalog.WithRole(UnitRoleTag.Worker)`.
- Updated ReviewGate so `unitdesigndefinitioncatalog` and the older GameState definitions cleanup gate reject renewed CombatBehavior enumeration of legacy unit definition entries.

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
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj gamestatedefinitionspubliccleanup --no-restore`
  Result: pass
  Evidence: GameState definitions cleanup gate completed with 0 errors and 0 warnings after rejecting CombatBehavior legacy entry enumeration.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj review --require-record=unitspec-readpath-cleanup-3 --no-restore`
  Result: pass
  Evidence: review record gate completed with 0 errors and 0 warnings.

Manual/visual gates:
- Check: visual inspection not required.
  Result: not run.
  Evidence: this slice only changes read-only QA data access and static ReviewGate checks.

Reviewer result:
- Status: pass-with-warnings
- Required fixes: none identified in the scoped review.
- Residual risks: legacy `UnitKind`, `UnitCatalog`, `FactionCatalog`, `GameState.UnitDefinitionFor`, and legacy faction roster checks remain for later migration slices.

TODO update:
- Items marked done: none.
- Items left open: parent UnitSpec duplicate-data cleanup and later deletion of legacy unit/faction catalogs.
- Reason: this is a narrow read-path cleanup slice; the main thread owns TODO integration.
