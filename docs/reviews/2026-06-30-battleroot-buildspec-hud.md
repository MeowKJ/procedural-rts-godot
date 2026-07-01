# Review Record - BattleRoot BuildSpec HUD cleanup

Step: Migration cleanup BattleRoot BuildSpec HUD slice
Milestone: M1 EntityWorld Becomes Authoritative
Owner AI: Integrator / Codex
Reviewer AI: ReviewGate battlerootbuildspechud
Integrator AI: Integrator / Codex

Scope:
- Files/folders: `scripts/BattleRoot.cs`, `tools/ReviewGate/Program.cs`, `TODO.md`, `docs/reviews/2026-06-30-battleroot-buildspec-hud.md`.
- Non-goals: deleting `PresentationCatalog.Building`, changing BuildingView rendering, changing building gameplay authority, or changing HUD layout.

Implementation summary:
- Replaced single-building HUD title/glyph/accent reads from `PresentationCatalog.Building(...)` with direct `BuildSpecCatalog.For(building.Kind)` data plus `GameState.VisualAccent(...)`.
- Replaced multi-selection building icon summary reads from `PresentationCatalog.Building(...)` with `BuildSpec` icon/short-code/accent data.
- Added `ReviewGate battlerootbuildspechud` so BattleRoot building HUD read paths cannot regress to `PresentationCatalog.Building(...)`.

Automated gates:
- Command: `dotnet build ProceduralRts.csproj --no-restore`
  Result: pass
  Evidence: build completed successfully.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore -- battlerootbuildspechud`
  Result: pass
  Evidence: ReviewGate battlerootbuildspechud completed with 0 errors and 0 warnings.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore -- review --require-record=battleroot-buildspec-hud`
  Result: pass
  Evidence: required review record check completed with 0 errors and 0 warnings.

Manual/visual gates:
- Check: visual inspection not required for this narrow metadata-source migration.
  Result: not run.
  Evidence: HUD layout and strings remain unchanged; only building display metadata source changed.

Reviewer result:
- Status: pass
- Required fixes: none expected after automated gate.
- Residual risks: `PresentationCatalog.Building` remains for compatibility paths until building presentation catalog deletion is complete.

TODO update:
- Items marked done: `BattleRoot BuildSpec HUD cleanup`.
- Items left open: broader Migration cleanup remains open.
- Reason: this slice removes BattleRoot building HUD dependence on presentation catalog, not the full secondary building runtime.
