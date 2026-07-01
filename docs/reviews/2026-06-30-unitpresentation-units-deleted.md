# Review Record - UnitPresentationCatalog Units dictionary deletion

Step: UnitPresentationCatalog Units dictionary deletion
Milestone: M1 EntityWorld Becomes Authoritative / UnitSpec duplicate-data cleanup
Owner AI: Codex
Reviewer AI: ReviewGate unitpresentationunitsdeleted / Integrator
Integrator AI: Codex

Scope:
- Files/folders: `scripts/core/UnitPresentationCatalog.cs`, `tools/ReviewGate/Program.cs`, `TODO.md`, `docs/reviews/2026-06-30-unitpresentation-units-deleted.md`.
- Non-goals: deleting `UnitCatalog`, deleting `UnitKind`, deleting production presentation compatibility, changing production behavior, changing unit art recipes, or changing unit balance/stats.

Implementation summary:
- Removed the legacy `UnitPresentationCatalog.Units` facade over `UnitCatalog.Presentations`.
- Re-routed `UnitPresentationCatalog.ProductionDescriptor(...)` so legacy production button defaults derive short code, icon, accent, and role glyph through `UnitKindDesignBridge.DesignId(...)` plus `UnitPresentationCatalog.ForDesign(...)`.
- Kept `ForDesign(...)`, `ForSpec(...)`, `ForProductionSpec(...)`, and the legacy production dictionary intact for later cleanup slices.
- Added `ReviewGate unitpresentationunitsdeleted` to prevent `UnitPresentationCatalog.Units`, `UnitCatalog.Presentations`, or `Units[outputUnit]` reads from returning to active source.

Automated gates:
- Command: `dotnet build ProceduralRts.csproj --no-restore`
  Result: pass
  Evidence: build completed successfully with 0 warnings and 0 errors.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore -- unitpresentationunitsdeleted`
  Result: pass
  Evidence: narrow ReviewGate mode completed successfully with 0 errors and 0 warnings.
- Command: `dotnet run --project tools/CombatBehavior/CombatBehavior.csproj --no-restore`
  Result: pass
  Evidence: CombatBehavior completed successfully.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore`
  Result: pass
  Evidence: full ReviewGate completed successfully with 0 errors and 0 warnings.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore -- review --require-record=unitpresentation-units-deleted`
  Result: pass
  Evidence: review-record gate completed successfully with 0 errors and 0 warnings.

Manual/visual gates:
- Check: visual inspection not required.
  Result: not run.
  Evidence: architecture read-path/API cleanup only; active drawing still uses UnitSpec art.

Reviewer result:
- Status: pass.
- Required fixes: none.
- Residual risks: `UnitCatalog` and production presentation compatibility still remain for later duplicate-data cleanup.

TODO update:
- Items marked done: `UnitPresentationCatalog Units dictionary deletion` under UnitSpec architecture phase 3 duplicate-data cleanup.
- Items left open: broad UnitSpec duplicate-data cleanup and final `UnitKind` / `UnitCatalog` deletion remain open.
- Reason: the legacy unit-presentation dictionary surface is removed, but broader compatibility state remains.
