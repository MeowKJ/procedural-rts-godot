# Review Record - UnitDesign Dog/Cat coverage

Step: UnitSpec duplicate-data cleanup Dog/Cat legacy coverage slice
Milestone: M1 EntityWorld Becomes Authoritative
Owner AI: Codex
Reviewer AI: ReviewGate unitdesigncoverage
Integrator AI: Main thread

Scope:
- Files/folders: `scripts/core/units/dog/*.cs`, `scripts/core/units/cat/*.cs`, `scripts/core/units/UnitKindDesignBridge.cs`, `scripts/core/UnitCatalog.cs`, `tools/CombatBehavior/Program.cs`, `tools/ReviewGate/Program.cs`, `TODO.md`.
- Non-goals: deleting generic legacy `UnitKind.Infantry` / `LightTank` / `Harvester`, deleting `UnitCatalog`, replacing legacy visual descriptors, adding new production UI tabs, or finalizing balance for T2/T3 units.

Implementation summary:
- Added UnitDesign authoring files for all Dog/Cat legacy units that were still missing UnitDesigns.
- Expanded `UnitKindDesignBridge` so every Dog/Cat legacy `UnitKind` maps to a UnitDesign id.
- Moved all Dog/Cat legacy `UnitCatalog` runtime definitions and presentation metadata through UnitDesign-backed compatibility projections.
- Added CombatBehavior coverage proving the bridge covers every Dog/Cat legacy unit and does not cover the generic compatibility test units.

Automated gates:
- Command: `dotnet build ProceduralRts.csproj --no-restore`
  Result: pass
  Evidence: build completed successfully with 0 warnings and 0 errors.
- Command: `dotnet run --project tools/CombatBehavior/CombatBehavior.csproj --no-restore`
  Result: pass
  Evidence: CombatBehavior completed successfully across combat, production, economy, enemy AI, and presentation descriptor checks after a sequential rerun; an earlier parallel run hit a build output file lock.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj unitdesigncoverage --no-restore`
  Result: pass
  Evidence: ReviewGate unitdesigncoverage completed with 0 errors and 0 warnings.

Manual/visual gates:
- Check: visual inspection not required.
  Result: not run.
  Evidence: this slice reuses existing procedural Dog/Cat art recipe families and keeps legacy visual descriptors for old UnitCatalog compatibility.

Reviewer result:
- Status: pass
- Required fixes: none.
- Residual risks: Newly covered T2/T3 UnitDesigns use existing weapon catalog behavior; later balance work should tune any role-specific weapon overrides or new weapon specs instead of moving data back into UnitCatalog.

TODO update:
- Items marked done: none.
- Items left open: parent UnitSpec duplicate-data cleanup and final legacy deletion.
- Reason: this slice completes Dog/Cat UnitDesign coverage but does not delete the compatibility layer.
