# Review Record - FactionCatalog default AI difficulty deletion

Step:
- FactionCatalog default AI difficulty deletion

Milestone:
- M1 UnitSpec duplicate-data cleanup

Owner AI:
- Codex

Reviewer AI:
- Codex; ReviewGate factioncatalogdefaultaidifficultydeleted

Integrator AI:
- Codex

Scope:
- Files/folders:
  - scripts/core/FactionDefinition.cs
  - scripts/core/FactionCatalog.cs
  - tools/ReviewGate/Program.cs
  - TODO.md
  - docs/reviews/2026-07-01-factioncatalog-default-ai-difficulty-deleted.md
- Non-goals:
  - Do not change AI difficulty behavior.
  - Do not change skirmish options, match config, faction select, balance, UI,
    or art.

Implementation summary:
- Removed the unused `DefaultAiDifficulty` field from `FactionDefinition`.
- Removed mirrored `EnemyDifficulty.Normal` constructor values from
  `FactionCatalog`; difficulty remains owned by skirmish/match configuration.

Automated gates:
- Command: `dotnet build ProceduralRts.csproj --no-restore`
  Result: pass.
  Evidence: build completed successfully with 0 warnings and 0 errors.
- Command: `dotnet build tools/ReviewGate/ReviewGate.csproj --no-restore`
  Result: pass.
  Evidence: build completed successfully with 0 warnings and 0 errors.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore -- factioncatalogdefaultaidifficultydeleted`
  Result: pass.
  Evidence: ReviewGate passed with 0 errors and 0 warnings.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore`
  Result: pass.
  Evidence: ReviewGate passed with 0 errors and 0 warnings.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore -- review --require-record=factioncatalog-default-ai-difficulty-deleted`
  Result: pass.
  Evidence: ReviewGate passed with 0 errors and 0 warnings.

Manual/visual gates:
- Check: Visual/UI review
  Result: not applicable.
  Evidence: This slice removes unused metadata only.

Reviewer result:
- Status: pass after integration review.
- Required fixes:
  - None currently known.
- Residual risks:
  - `FactionCatalog` still owns faction display and starting-building metadata
    until later cleanup.

TODO update:
- Items marked done:
  - FactionCatalog default AI difficulty deletion
- Items left open:
  - Broader UnitSpec duplicate-data cleanup and final legacy enum deletion remain
    open.
