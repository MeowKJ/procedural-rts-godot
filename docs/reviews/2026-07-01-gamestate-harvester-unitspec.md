# Review Record - GameState harvester UnitSpec role cleanup

Step:
- GameState harvester UnitSpec role cleanup

Milestone:
- M1 UnitSpec duplicate-data cleanup

Owner AI:
- Codex

Reviewer AI:
- Codex; ReviewGate gamestateharvesterunitspec

Integrator AI:
- Codex

Scope:
- Files/folders:
  - scripts/core/GameState.cs
  - tools/CombatBehavior/Program.cs
  - tools/ReviewGate/Program.cs
  - TODO.md
  - docs/reviews/2026-07-01-gamestate-harvester-unitspec.md
- Non-goals:
  - Do not delete `UnitKind` globally.
  - Do not change harvesting balance, dock behavior, selection rules, UI, or art.

Implementation summary:
- `GameState.IsHarvesterUnit(UnitKind)` now resolves the legacy kind through
  `UnitKindDesignBridge.TryGetSpec(...)`.
- Harvester semantics are centralized in `IsHarvesterSpec(UnitSpec)`, using the
  UnitSpec economy role and authored harvest ability metadata instead of a
  handwritten UnitKind list.
- CombatBehavior now routes its harvester helper through `GameState` and proves
  generic, Dog, and Cat harvesters still classify correctly.

Automated gates:
- Command: `dotnet build ProceduralRts.csproj --no-restore`
  Result: pass.
  Evidence: build completed successfully with 0 warnings and 0 errors.
- Command: `dotnet build tools/ReviewGate/ReviewGate.csproj --no-restore`
  Result: pass.
  Evidence: build completed successfully with 0 warnings and 0 errors.
- Command: `dotnet run --project tools/CombatBehavior/CombatBehavior.csproj --no-restore`
  Result: pass.
  Evidence: Combat behavior passed with UnitSpec-backed harvester helper
    assertions.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore -- gamestateharvesterunitspec`
  Result: pass.
  Evidence: ReviewGate passed with 0 errors and 0 warnings.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore`
  Result: pass.
  Evidence: ReviewGate passed with 0 errors and 0 warnings.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore -- review --require-record=gamestate-harvester-unitspec`
  Result: pass.
  Evidence: ReviewGate passed with 0 errors and 0 warnings.

Manual/visual gates:
- Check: Visual/UI review
  Result: not applicable.
  Evidence: This slice only changes legacy harvester classification semantics.

Reviewer result:
- Status: pass after integration review.
- Required fixes:
  - None currently known.
- Residual risks:
  - Old `GameState` still contains legacy harvesting behavior until the broader
    runtime deletion tail.
  - `UnitKind` remains as the compatibility key for this helper.

TODO update:
- Items marked done:
  - GameState harvester UnitSpec role cleanup.
- Items left open:
  - Broader duplicate-data cleanup and final legacy enum deletion remain open.
