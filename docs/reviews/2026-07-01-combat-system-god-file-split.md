# Review Record - CombatSystem god-file split

Step: Split CombatSystem into focused partial companion files
Milestone: Single responsibility - god-class breakup
Owner AI: Codex
Reviewer AI: ReviewGate / SimReplay / CombatBehavior
Integrator AI: Codex

Scope:
- Files/folders: `scripts/core/sim/systems/CombatSystem.cs`, `scripts/core/sim/systems/CombatSystem.*System.cs`, `tools/ReviewGate/ReviewGateEvidence.cs`, `tools/ReviewGate/ReviewGateChecks.Part57.cs`, `tools/ReviewGate/ReviewGateChecks.Part75.cs`, `tools/ReviewGate/FileSizeGate.cs`, `TODO.md`.
- Non-goals: converging `CombatSystem`, `TurretCombatSystem`, and `BuildingTargetCombatSystem` into one generic weapon engagement system; changing combat balance.

Implementation summary:
- Kept `CombatSystem.cs` as the only `ISimSystem` entry point with the deterministic `Step(SimContext)` loop.
- Moved combat concerns into focused partial companion files: memory cooldowns, target grid, target resolution, guard logic, target search/scoring, target state/last-known behavior, autonomy/leash return, engagement/mount updates, and damage/retaliation.
- Renamed companion files to `CombatSystem.*System.cs` and updated naming/sim convention gates so partial companions are allowed while main system files still must implement `ISimSystem`.
- Updated ReviewGate's combat source readers to include same-name partial files, preserving historical checks without forcing `CombatSystem` back into one file.
- Removed `scripts/core/sim/systems/CombatSystem.cs` from the known red-line file-size debt whitelist.

Automated gates:
- Command: `dotnet build ProceduralRts.csproj --no-restore`
  Result: pass
  Evidence: local run completed with 0 warnings and 0 errors.
- Command: `dotnet run --project tools/SimReplay/SimReplay.csproj --no-restore`
  Result: pass
  Evidence: deterministic combat, target stickiness, guard, last-known memory, group attack, and outcome replay scenarios passed.
- Command: `dotnet run --project tools/CombatBehavior/CombatBehavior.csproj --no-restore`
  Result: pass
  Evidence: combat behavior harness passed weapon hit rules, turret states, target propagation, enemy AI, and outcomes.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore`
  Result: pass
  Evidence: full ReviewGate completed with 0 errors and the expected file-size debt/watchlist warnings.

Reviewer result:
- Status: pass
- Required fixes: none.
- Residual risks: the three combat loops still need later convergence into one generic engagement path.

TODO update:
- Items marked done: `CombatSystem god-file split`.
- Items left open: combat system convergence across mobile units, turrets, and building-target combat.
- Reason: this slice removes the red-line God-file shape without changing combat behavior.
