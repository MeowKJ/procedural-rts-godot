# Review Record - FactionCatalog UnitSpec availability cleanup

Step: FactionCatalog UnitSpec availability cleanup
Milestone: M1 EntityWorld Becomes Authoritative / UnitSpec duplicate-data cleanup
Owner AI: Codex
Reviewer AI: ReviewGate factioncatalogunitspecavailability / Integrator
Integrator AI: Codex

Scope:
- Files/folders: `scripts/core/FactionCatalog.cs`, `tools/ReviewGate/Program.cs`, `TODO.md`, `docs/reviews/2026-06-30-factioncatalog-unitspec-availability.md`.
- Non-goals: deleting `FactionCatalog`, changing starting loadouts, deleting `UnitKind`, deleting `UnitCatalog`, changing unit balance/stats, changing production/economy behavior, or changing art/UI style.

Implementation summary:
- `FactionCatalog.AvailableUnits` no longer calls `UnitCatalog.UnitsForFaction(...)`.
- Added a local `UnitKindsForFaction(...)` projection that derives legacy `UnitKind` availability from `UnitKindDesignBridge.DesignIds` and filters by `UnitDesignCatalog.Spec(...).Faction`.
- Kept `FactionDefinition.AvailableUnits` as `UnitKind` for old-runtime compatibility while removing one more direct `UnitCatalog` consumer.
- Added `ReviewGate factioncatalogunitspecavailability` to prevent the `UnitCatalog.UnitsForFaction(...)` dependency from returning.

Automated gates:
- Command: `dotnet build ProceduralRts.csproj --no-restore`
  Result: pass
  Evidence: build completed successfully.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore -- factioncatalogunitspecavailability`
  Result: pass
  Evidence: narrow ReviewGate mode completed successfully.
- Command: `dotnet run --project tools/CombatBehavior/CombatBehavior.csproj --no-restore`
  Result: pass
  Evidence: CombatBehavior completed successfully.
- Command: `dotnet run --project tools/RosterAuthoringQa/RosterAuthoringQa.csproj --no-restore`
  Result: pass
  Evidence: RosterAuthoringQa completed successfully.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore`
  Result: pass
  Evidence: full ReviewGate completed successfully.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore -- review --require-record=factioncatalog-unitspec-availability`
  Result: pass
  Evidence: review-record gate completed successfully.
- Command: `dotnet run --project tools/VerifyAll/VerifyAll.csproj --no-restore`
  Result: pass
  Evidence: VerifyAll completed successfully with all checks passing.

Manual/visual gates:
- Check: visual inspection not required.
  Result: not run.
  Evidence: no rendering, palette, layout, or camera behavior changed.

Reviewer result:
- Status: pass.
- Required fixes: none.
- Residual risks: `FactionCatalog` and `FactionDefinition.AvailableUnits` still expose legacy `UnitKind` values for old-runtime compatibility. This slice removes only the `UnitCatalog.UnitsForFaction(...)` dependency.

TODO update:
- Items marked done: `FactionCatalog UnitSpec availability cleanup` under UnitSpec architecture phase 3 duplicate-data cleanup.
- Items left open: broad UnitSpec duplicate-data cleanup and final `UnitKind` / `UnitCatalog` deletion remain open.
- Reason: faction availability now comes from UnitSpec metadata, but legacy compatibility types still exist.
