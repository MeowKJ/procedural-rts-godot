# Review Record - FactionCatalog building/production availability deletion

Step:
- FactionCatalog building/production availability deletion

Milestone:
- M1 UnitSpec / BuildSpec duplicate-data cleanup

Owner AI:
- Codex

Reviewer AI:
- Codex; ReviewGate factioncatalogbuildproductionavailabilitydeleted

Integrator AI:
- Codex

Scope:
- Files/folders:
  - scripts/core/FactionDefinition.cs
  - scripts/core/FactionCatalog.cs
  - tools/CombatBehavior/Program.cs
  - tools/ReviewGate/Program.cs
  - TODO.md
  - docs/reviews/2026-07-01-factioncatalog-build-production-availability-deleted.md
- Non-goals:
  - Do not delete `FactionCatalog` itself.
  - Do not delete `BuildingKind`, `ProductionKind`, or `UnitKind` globally.
  - Do not change start buildings, build specs, production rules, UI, art, or
    balance.

Implementation summary:
- Removed duplicate `AvailableBuildings` and `AvailableProduction` fields from
  `FactionDefinition`.
- Removed mirrored `Enum.GetValues<BuildingKind>()` and
  `Enum.GetValues<ProductionKind>()` availability data from `FactionCatalog`.
- CombatBehavior now proves turret availability through `BuildSpecCatalog`
  instead of faction-level availability mirrors.

Automated gates:
- Command: `dotnet build ProceduralRts.csproj --no-restore`
  Result: pass.
  Evidence: build completed successfully with 0 warnings and 0 errors.
- Command: `dotnet build tools/ReviewGate/ReviewGate.csproj --no-restore`
  Result: pass.
  Evidence: build completed successfully with 0 warnings and 0 errors.
- Command: `dotnet run --project tools/CombatBehavior/CombatBehavior.csproj --no-restore`
  Result: pass.
  Evidence: Combat behavior passed with BuildSpecCatalog turret availability assertions.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore -- factioncatalogbuildproductionavailabilitydeleted`
  Result: pass.
  Evidence: ReviewGate passed with 0 errors and 0 warnings.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore`
  Result: pass.
  Evidence: ReviewGate passed with 0 errors and 0 warnings.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore -- review --require-record=factioncatalog-build-production-availability-deleted`
  Result: pass.
  Evidence: ReviewGate passed with 0 errors and 0 warnings.
- Command: `dotnet run --project tools/VerifyAll/VerifyAll.csproj --no-restore`
  Result: pass.
  Evidence: VerifyAll PASSED, 23/23 steps.

Manual/visual gates:
- Check: Visual/UI review
  Result: not applicable.
  Evidence: This slice removes duplicate authoring data only.

Reviewer result:
- Status: pass after integration review.
- Required fixes:
  - None currently known.
- Residual risks:
  - `FactionCatalog` still owns faction display and starting-building metadata
    until later cleanup.
  - `BuildingKind` and `ProductionKind` remain compatibility enums.

TODO update:
- Items marked done:
  - FactionCatalog building/production availability deletion
- Items left open:
  - Broader UnitSpec/BuildSpec duplicate-data cleanup and final legacy enum
    deletion remain open.
