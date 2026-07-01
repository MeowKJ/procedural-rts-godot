# Review Record - UnitPresentationCatalog UnitKind entrypoint deletion

Step: UnitPresentationCatalog UnitKind entrypoint deletion
Milestone: M1 EntityWorld Becomes Authoritative / UnitSpec duplicate-data cleanup
Owner AI: Codex
Reviewer AI: ReviewGate unitpresentationforunitkinddeleted / Integrator
Integrator AI: Codex

Scope:
- Files/folders: `scripts/core/UnitPresentationCatalog.cs`, `tools/ReviewGate/Program.cs`, `TODO.md`, `docs/reviews/2026-06-30-unitpresentation-for-unitkind-deleted.md`.
- Non-goals: deleting `UnitPresentationCatalog.Units`, deleting `UnitCatalog`, deleting `UnitKind`, deleting production presentation compatibility, changing unit art recipes, changing UI layout, or changing unit balance/stats.

Implementation summary:
- Deleted the unused public `UnitPresentationCatalog.For(UnitKind kind)` compatibility entrypoint.
- Kept the UnitSpec-native `ForDesign(...)` and `ForSpec(...)` presentation entrypoints.
- Kept production compatibility APIs intact for later cleanup slices.
- Added `ReviewGate unitpresentationforunitkinddeleted` to prevent the UnitKind presentation entrypoint or source calls to `UnitPresentationCatalog.For(kind)` from returning.

Automated gates:
- Command: `dotnet build ProceduralRts.csproj --no-restore`
  Result: pass
  Evidence: build completed successfully with 0 warnings and 0 errors.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore -- unitpresentationforunitkinddeleted`
  Result: pass
  Evidence: narrow ReviewGate mode completed successfully with 0 errors and 0 warnings after excluding the ReviewGate source file from its own deleted-call scan.
- Command: `dotnet run --project tools/CombatBehavior/CombatBehavior.csproj --no-restore`
  Result: pass
  Evidence: CombatBehavior completed successfully.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore`
  Result: pass
  Evidence: full ReviewGate completed successfully with 0 errors and 0 warnings.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore -- review --require-record=unitpresentation-for-unitkind-deleted`
  Result: pass
  Evidence: review-record gate completed successfully with 0 errors and 0 warnings.

Manual/visual gates:
- Check: visual inspection not required.
  Result: not run.
  Evidence: API deletion only; active drawing paths already route through UnitSpec art.

Reviewer result:
- Status: pass.
- Required fixes: none.
- Residual risks: `UnitPresentationCatalog.Units` and production presentation compatibility still remain for later duplicate-data cleanup.

TODO update:
- Items marked done: `UnitPresentationCatalog UnitKind entrypoint deletion` under UnitSpec architecture phase 3 duplicate-data cleanup.
- Items left open: broad UnitSpec duplicate-data cleanup and final `UnitKind` / `UnitCatalog` deletion remain open.
- Reason: one unused legacy public method is removed, but broader compatibility state remains.
