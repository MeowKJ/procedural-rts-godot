# Review Record - FactionCatalog default owner mapping deletion

Step:
- FactionCatalog default owner mapping deletion

Milestone:
- M1 UnitSpec duplicate-data cleanup

Owner AI:
- Codex

Reviewer AI:
- Codex; ReviewGate factioncatalogdefaultownerdeleted

Integrator AI:
- Codex

Scope:
- Files/folders:
  - scripts/core/FactionCatalog.cs
  - tools/CombatBehavior/Program.cs
  - tools/ReviewGate/Program.cs
  - TODO.md
  - docs/reviews/2026-07-01-factioncatalog-default-owner-deleted.md
- Non-goals:
  - Do not change match faction selection.
  - Do not change faction display metadata, balance, UI, art, or AI behavior.

Implementation summary:
- Removed the remaining `DefaultFactionForOwner(...)` helper from
  `FactionCatalog`.
- `CombatBehavior` now keeps its fixture default owner-to-faction mapping in a
  local helper, so the compatibility assumption does not live in faction display
  metadata.

Automated gates:
- Command: `dotnet build ProceduralRts.csproj --no-restore`
  Result: pass.
  Evidence: build completed successfully with 0 warnings and 0 errors.
- Command: `dotnet build tools/ReviewGate/ReviewGate.csproj --no-restore`
  Result: pass.
  Evidence: build completed successfully with 0 warnings and 0 errors.
- Command: `dotnet run --project tools/CombatBehavior/CombatBehavior.csproj --no-restore`
  Result: pass.
  Evidence: Combat behavior passed with fixture default faction mapping covered
    outside FactionCatalog metadata.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore -- factioncatalogdefaultownerdeleted`
  Result: pass.
  Evidence: ReviewGate passed with 0 errors and 0 warnings.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore`
  Result: pass.
  Evidence: ReviewGate passed with 0 errors and 0 warnings.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore -- review --require-record=factioncatalog-default-owner-deleted`
  Result: pass.
  Evidence: ReviewGate passed with 0 errors and 0 warnings.
- Command: `dotnet run --project tools/VerifyAll/VerifyAll.csproj --no-restore`
  Result: pass.
  Evidence: Grouped post-slice VerifyAll passed 23/23 after the
    sandbox-roster cleanup batch.

Manual/visual gates:
- Check: Visual/UI review
  Result: not applicable.
  Evidence: This slice only deletes faction metadata coupling from a tool fixture edge.

Reviewer result:
- Status: pass after integration review.
- Required fixes:
  - None currently known.
- Residual risks:
  - `FactionCatalog` still owns faction display metadata.
  - Legacy `UnitKind` / `BuildingKind` compatibility enums remain until the
    later duplicate-data cleanup tail.

TODO update:
- Items marked done:
  - FactionCatalog default owner mapping deletion.
- Items left open:
  - Broader duplicate-data cleanup and final legacy enum deletion remain open.
