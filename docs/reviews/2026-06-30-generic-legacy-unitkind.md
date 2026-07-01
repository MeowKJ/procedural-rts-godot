# Review Record - Generic legacy UnitKind bridge

Step: UnitSpec duplicate-data cleanup generic legacy UnitKind coverage.
Milestone: UnitSpec architecture phase 3.
Owner AI: Worker A.
Reviewer AI: Integrator sanity review.
Integrator AI: Main Codex thread.

Scope:
- Files/folders: `scripts/core/units/generic/*.cs`, `scripts/core/units/UnitKindDesignBridge.cs`, `scripts/core/UnitCatalog.cs`, `tools/CombatBehavior/Program.cs`, `tools/ReviewGate/Program.cs`, `docs/reviews/2026-06-30-generic-legacy-unitkind.md`.
- Non-goals: No deletion of `UnitKind` or `UnitCatalog`, no Dog/Cat stat changes, no T2/T3 production unlock changes, no unit art redesign, no legacy visual descriptor cleanup.

Implementation summary:
- Added `GenericInfantry`, `GenericLightTank`, and `GenericHarvester` as `UnitDesign` entries for the remaining generic legacy compatibility units.
- Mapped `UnitKind.Infantry`, `UnitKind.LightTank`, and `UnitKind.Harvester` through `UnitKindDesignBridge`.
- Routed their legacy `UnitCatalog` runtime definitions through `UnitDesignDefinitionCatalog.CompatibilityDefinition(...)`.
- Routed their legacy presentation metadata through `UnitPresentationCatalog.ForDesign(...)` while keeping explicit legacy `UnitVisualDescriptor` compatibility shapes.
- Added `genericlegacyunitkind` ReviewGate coverage and CombatBehavior assertions for runtime and presentation projection.

Automated gates:
- Command: `dotnet run --project tools/CombatBehavior/CombatBehavior.csproj --no-restore`
  Result: Pass.
  Evidence: `Combat behavior passed: weapon hit rules, turret states, terrain passability, localization fallback, presentation descriptors, shared threat propagation, rally production, economy, enemy AI, and outcomes`.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore -- genericlegacyunitkind`
  Result: Pass.
  Evidence: `ReviewGate passed` with 0 errors and 0 warnings.

Manual/visual gates:
- Check: None.
  Result: Not applicable.
  Evidence: Data/compatibility bridge only; legacy visual descriptors are intentionally preserved.

Reviewer result:
- Status: pass with residual risk.
- Required fixes: None for this slice.
- Residual risks: Generic compatibility designs currently use `UnitFactionId.Dog` because the authoring model has no neutral/generic `UnitFactionId`; their design-projected accent follows the current UnitSpec faction-color rule. Legacy `UnitKind`, `UnitCatalog`, and `UnitVisualDescriptor` remain compatibility layers for later deletion.

TODO update:
- Items marked done: None at parent level.
- Items left open: Broader UnitSpec duplicate-data cleanup and later legacy `UnitKind` / `UnitCatalog` / visual descriptor deletion.
- Reason: This closes generic coverage inside the bridge, but does not delete the legacy compatibility path.
