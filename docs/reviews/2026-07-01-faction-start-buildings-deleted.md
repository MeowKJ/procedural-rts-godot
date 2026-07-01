# Review Record - Faction start building loadout cleanup

Step:
- Faction start building loadout cleanup

Milestone:
- M1 BuildSpec / faction duplicate-data cleanup

Owner AI:
- Codex

Reviewer AI:
- Codex; ReviewGate factionstartbuildingsdeleted

Integrator AI:
- Codex

Scope:
- Files/folders:
  - scripts/core/FactionDefinition.cs
  - scripts/core/FactionCatalog.cs
  - scripts/core/MatchStartLoadout.cs
  - tools/CombatBehavior/Program.cs
  - tools/ReviewGate/Program.cs
  - TODO.md
  - docs/reviews/2026-07-01-faction-start-buildings-deleted.md
- Non-goals:
  - Do not delete `FactionCatalog` itself.
  - Do not delete `BuildingKind` globally.
  - Do not change start positions, build specs, production, balance, UI, or art.

Implementation summary:
- Removed duplicate `StartingBuildings` from `FactionDefinition` and
  `FactionCatalog`.
- `MatchStartLoadouts` now owns faction starting building lists through
  `StartingBuildingsByFaction` and `StartingBuildings(FactionId)`.
- CombatBehavior now proves MatchConfig start routing through
  `MatchStartLoadouts` for buildings and `UnitDesignRuntimeLoadouts` for units.

Automated gates:
- Command: `dotnet build ProceduralRts.csproj --no-restore`
  Result: pass.
  Evidence: build completed successfully with 0 warnings and 0 errors.
- Command: `dotnet build tools/ReviewGate/ReviewGate.csproj --no-restore`
  Result: pass.
  Evidence: build completed successfully with 0 warnings and 0 errors.
- Command: `dotnet run --project tools/CombatBehavior/CombatBehavior.csproj --no-restore`
  Result: pass.
  Evidence: Combat behavior passed with MatchStartLoadouts-owned start-building assertions.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore -- startloadout`
  Result: pass.
  Evidence: ReviewGate passed with 0 errors and 0 warnings.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore -- factionstartbuildingsdeleted`
  Result: pass.
  Evidence: ReviewGate passed with 0 errors and 0 warnings.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore`
  Result: pass.
  Evidence: ReviewGate passed with 0 errors and 0 warnings.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore -- review --require-record=faction-start-buildings-deleted`
  Result: pass.
  Evidence: ReviewGate passed with 0 errors and 0 warnings.
- Command: `dotnet run --project tools/VerifyAll/VerifyAll.csproj --no-restore`
  Result: pass.
  Evidence: Grouped post-slice VerifyAll passed 23/23 after the
    default-owner and sandbox-roster cleanup batch.

Manual/visual gates:
- Check: Visual/UI review
  Result: not applicable.
  Evidence: This slice only moves start-loadout authoring data.

Reviewer result:
- Status: pass after integration review.
- Required fixes:
  - None currently known.
- Residual risks:
  - `FactionCatalog` still owns faction display metadata and the default
    owner-to-faction compatibility helper.
  - `BuildingKind` remains as a legacy/build identity enum.

TODO update:
- Items marked done:
  - Faction start building loadout cleanup
- Items left open:
  - Broader duplicate-data cleanup and final legacy enum deletion remain open.
