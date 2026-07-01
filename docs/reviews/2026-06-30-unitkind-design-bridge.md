# Review Record - UnitKind design bridge

Step: UnitSpec architecture phase 3 duplicate-data cleanup UnitKind design bridge slice
Milestone: M1 EntityWorld Becomes Authoritative
Owner AI: Worker A / Codex
Reviewer AI: Codex self-review
Integrator AI: Main thread

Scope:
- Files/folders: `scripts/core/units/UnitKindDesignBridge.cs`, `scripts/core/UnitCatalog.cs`, `tools/ReviewGate/Program.cs`, `docs/reviews/2026-06-30-unitkind-design-bridge.md`.
- Non-goals: deleting `UnitKind`, deleting `UnitCatalog`, mapping UnitKinds that do not yet have UnitDesign files, changing presentation descriptors, changing production loadouts, changing live unit behavior outside legacy UnitDefinition compatibility.

Implementation summary:
- Added `UnitKindDesignBridge` as the explicit old `UnitKind` to `UnitDesign` id bridge for existing Dog/Cat UnitDesigns only.
- Routed bridged `UnitCatalog` entries through `UnitDesignDefinitionCatalog.CompatibilityDefinition(...)` via `UnitKindDesignBridge`, so `UnitCatalog.Definitions` receives UnitSpec-derived runtime values for those old UnitKinds.
- Added `ReviewGate unitkinddesignbridge` to lock the bridge mappings, reject premature mappings for old UnitKinds without UnitDesign files, and prevent hand-authored runtime definitions from returning for the bridged units.

Automated gates:
- Command: `dotnet build ProceduralRts.csproj --no-restore`
  Result: pass
  Evidence: build completed successfully with 0 warnings and 0 errors.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj todo --no-restore`
  Result: pass
  Evidence: ReviewGate todo completed with 0 errors and 0 warnings after a sequential rerun; an earlier parallel attempt hit the ReviewGate build output DLL lock.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj unitkinddesignbridge --no-restore`
  Result: pass
  Evidence: ReviewGate unitkinddesignbridge completed with 0 errors and 0 warnings.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj review --require-record unitkind-design-bridge --no-restore`
  Result: pass
  Evidence: ReviewGate review mode found the unitkind-design-bridge record and completed with 0 errors and 0 warnings.

Manual/visual gates:
- Check: visual inspection not required.
  Result: not run.
  Evidence: this slice only changes data authority for legacy runtime definitions and static review gates.

Reviewer result:
- Status: pass-with-warnings
- Required fixes: none identified in scoped self-review before independent review.
- Residual risks: legacy UnitKind/UnitCatalog compatibility remains; mapped runtime values now follow UnitDesign/UnitSpec and may differ from older hand-authored labels or numeric tuning where the duplicate sources had drifted.

TODO update:
- Items marked done: none.
- Items left open: parent UnitSpec duplicate-data cleanup and later deletion of old unit/faction catalogs.
- Reason: this is a narrow bridge slice; the main thread owns TODO integration after gates and independent review.
