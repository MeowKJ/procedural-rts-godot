# Review Record - Faction Start Bridge

Step: Align legacy faction starting UnitKinds with UnitDesign runtime starting rosters.
Milestone: Playable 1v1 skirmish faction completeness.
Owner AI: Main thread.
Reviewer AI: ReviewGate factionstartbridge plus CombatBehavior assertions.
Integrator AI: Main thread.

Scope:
- Files/folders: `scripts/core/FactionCatalog.cs`, `tools/CombatBehavior/Program.cs`, `tools/ReviewGate/Program.cs`, `TODO.md`, `docs/reviews/2026-06-30-faction-start-bridge.md`.
- Non-goals: no new unit content, no balance changes, no removal of legacy `UnitKind`, no third-faction content.

Implementation summary:
- Expanded Dog legacy start units to mirror the UnitDesign runtime start: two guard tanks, one patrol vehicle, two infantry, one rocket, and one harvester.
- Expanded Cat legacy start units to mirror the UnitDesign runtime start: two tanks, one scout car, three basic cats, and one harvester.
- Added CombatBehavior proof that `FactionCatalog.StartingUnits` projected through `UnitKindDesignBridge` matches `UnitDesignRuntimeLoadouts` starting design ids for Dog and Cat.
- Added `ReviewGate factionstartbridge` so the compatibility bridge cannot drift silently.

Automated gates:
- Command: `dotnet build ProceduralRts.csproj --no-restore`
  Result: pass.
  Evidence: build completed with 0 warnings and 0 errors.
- Command: `dotnet run --project tools/CombatBehavior/CombatBehavior.csproj --no-restore`
  Result: pass.
  Evidence: `Combat behavior passed: weapon hit rules, turret states, terrain passability, localization fallback, presentation descriptors, shared threat propagation, rally production, economy, enemy AI, and outcomes`.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore -- factionstartbridge`
  Result: pass.
  Evidence: `ReviewGate passed` with 0 errors and 0 warnings.

Manual/visual gates:
- No visual change intended beyond matching the old and new starting rosters.

Reviewer result:
- Status: pass.
- Required fixes: none known.
- Residual risks: legacy `FactionCatalog` still exists as a compatibility layer until the later UnitSpec cleanup removes duplicate faction/unit data.

TODO update:
- Items marked done: none.
- Items left open: broad Dog/Cat playable faction item remains open until the full vertical-slice gates prove player loop, AI loop, counters, performance, and readability together.
