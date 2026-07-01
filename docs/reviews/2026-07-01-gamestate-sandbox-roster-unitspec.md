# Review Record - GameState sandbox UnitDesign roster cleanup

Step:
- GameState sandbox UnitDesign roster cleanup

Milestone:
- M1 UnitSpec duplicate-data cleanup

Owner AI:
- Codex

Reviewer AI:
- Codex; ReviewGate gamestatesandboxrosterunitspec

Integrator AI:
- Codex

Scope:
- Files/folders:
  - scripts/core/GameState.cs
  - tools/ReviewGate/Program.cs
  - TODO.md
  - docs/reviews/2026-07-01-gamestate-sandbox-roster-unitspec.md
- Non-goals:
  - Do not change unit balance, production rosters, faction availability, UI, or
    art.
  - Do not delete `UnitKind` globally in this slice.

Implementation summary:
- Replaced handwritten Dog/Cat `UnitKind` arrays in
  `GameState.AddSandboxFactionTestUnits()` with shared sandbox roster rows.
- `AddSandboxFactionLine(...)` now reads playable design ids from
  `UnitDesignFactionRosterCatalog` and converts to legacy `UnitKind` only at the
  old `AddUnit(...)` spawn edge.
- Not-yet-legacy-bridged UnitDesigns are skipped in the old sandbox path instead
  of forcing new UnitDesign content back into the legacy enum.

Automated gates:
- Command: `dotnet build ProceduralRts.csproj --no-restore`
  Result: pass.
  Evidence: build completed successfully with 0 warnings and 0 errors.
- Command: `dotnet build tools/ReviewGate/ReviewGate.csproj --no-restore`
  Result: pass.
  Evidence: build completed successfully with 0 warnings and 0 errors.
- Command: `dotnet run --project tools/CombatBehavior/CombatBehavior.csproj --no-restore`
  Result: pass.
  Evidence: Combat behavior passed after sandbox roster rows moved to UnitDesign
    playable design ids with legacy-bridge filtering.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore -- gamestatesandboxrosterunitspec`
  Result: pass.
  Evidence: ReviewGate passed with 0 errors and 0 warnings.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore`
  Result: pass.
  Evidence: ReviewGate passed with 0 errors and 0 warnings.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore -- review --require-record=gamestate-sandbox-roster-unitspec`
  Result: pass.
  Evidence: ReviewGate passed with 0 errors and 0 warnings.
- Command: `dotnet run --project tools/VerifyAll/VerifyAll.csproj --no-restore`
  Result: pass.
  Evidence: Grouped post-slice VerifyAll passed 23/23.

Manual/visual gates:
- Check: Visual/UI review
  Result: not applicable.
  Evidence: This slice changes sandbox roster source data only.

Reviewer result:
- Status: pass after integration review.
- Required fixes:
  - None currently known.
- Residual risks:
  - Sandbox still spawns through legacy `AddUnit(UnitKind, ...)` until the old
    runtime spawn edge is deleted.
  - Legacy `UnitKind` remains globally until the later duplicate-data cleanup
    tail.

TODO update:
- Items marked done:
  - GameState sandbox UnitDesign roster cleanup.
- Items left open:
  - Broader duplicate-data cleanup and final legacy enum deletion remain open.
