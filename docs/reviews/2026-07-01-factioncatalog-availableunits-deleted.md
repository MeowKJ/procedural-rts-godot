# Review Record - FactionCatalog AvailableUnits deletion

Step:
- FactionCatalog AvailableUnits deletion

Milestone:
- M1 UnitSpec duplicate-data cleanup

Owner AI:
- Codex

Reviewer AI:
- Codex; ReviewGate factioncatalogunitspecavailability

Integrator AI:
- Codex

Scope:
- Files/folders:
  - scripts/core/FactionDefinition.cs
  - scripts/core/FactionCatalog.cs
  - tools/CombatBehavior/Program.cs
  - tools/ReviewGate/Program.cs
  - TODO.md
  - docs/reviews/2026-07-01-factioncatalog-availableunits-deleted.md
- Non-goals:
  - Do not delete `FactionCatalog` itself.
  - Do not delete `UnitKind` globally.
  - Do not change start loadouts, buildings, production, balance, UI, or art.

Implementation summary:
- Removed the duplicate `AvailableUnits` field from `FactionDefinition`.
- Removed `FactionCatalog.UnitKindsForFaction(...)` and the local
  `UnitKindDesignBridge.DesignIds` availability projection.
- CombatBehavior now proves Dog/Cat tier, coverage, disjointness, and sandbox
  roster coverage from `UnitDesignFactionRosterCatalog` playable design ids.

Automated gates:
- Command: `dotnet build ProceduralRts.csproj --no-restore`
  Result: pass.
  Evidence: build completed successfully with 0 warnings and 0 errors.
- Command: `dotnet build tools/ReviewGate/ReviewGate.csproj --no-restore`
  Result: pass.
  Evidence: build completed successfully with 0 warnings and 0 errors.
- Command: `dotnet run --project tools/CombatBehavior/CombatBehavior.csproj --no-restore`
  Result: pass.
  Evidence: Combat behavior passed with UnitDesign playable-roster availability assertions.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore -- factioncatalogunitspecavailability`
  Result: pass.
  Evidence: ReviewGate passed with 0 errors and 0 warnings.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore`
  Result: pass.
  Evidence: ReviewGate passed with 0 errors and 0 warnings.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore -- review --require-record=factioncatalog-availableunits-deleted`
  Result: pass.
  Evidence: ReviewGate passed with 0 errors and 0 warnings.
- Command: `dotnet run --project tools/VerifyAll/VerifyAll.csproj --no-restore`
  Result: pass.
  Evidence: VerifyAll PASSED, 23/23 steps, after the grouped availability cleanup batch.

Manual/visual gates:
- Check: Visual/UI review
  Result: not applicable.
  Evidence: This slice removes duplicate authoring data only.

Reviewer result:
- Status: pass after integration review.
- Required fixes:
  - None currently known.
- Residual risks:
  - `FactionCatalog` still owns faction display/building/production metadata
    until a later broader catalog cleanup.
  - `UnitKind` remains as a legacy runtime enum until the final deletion phase.

TODO update:
- Items marked done:
  - FactionCatalog AvailableUnits deletion
- Items left open:
  - Broader UnitSpec duplicate-data cleanup and final `UnitKind` /
    `BuildingKind` deletion remain open.
