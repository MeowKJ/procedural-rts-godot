# Review Record - Economy tuning config

Step:
Close the M4 economy tuning TODO by moving ResourceSystem harvest/dock/unload
knobs into EntityWorld-owned data and proving tuned values affect throughput.

Milestone:
M4 Production & Economy System.

Owner AI:
Codex main agent.

Reviewer AI:
Codex main-agent review with `ReviewGate economytuning`, `SimReplay`, and full
`VerifyAll`.

Integrator AI:
Codex main agent.

Scope:
- Files/folders:
  - `scripts/core/sim/EconomyTuningConfig.cs`
  - `scripts/core/entities/EntityWorld.cs`
  - `scripts/core/sim/systems/ResourceSystem.cs`
  - `tools/SimReplay/Program.cs`
  - `tools/ReviewGate/Program.cs`
  - `TODO.md`
  - `docs/reviews/2026-06-29-economy-tuning.md`
- Non-goals:
  - Do not implement environment-modulated resource regeneration.
  - Do not centralize all balance values into `BalanceConfig`.
  - Do not migrate live legacy economy/UI authority.

Implementation summary:
- Added `EconomyTuningConfig` as pure data for gather distance, dock distance,
  gather rate, and unload rate.
- Added `EntityWorld.EconomyTuning` and folded all tuning values into
  `DeterministicStateHash`.
- Updated `ResourceSystem` to read economy tuning instead of private hard-coded
  gather/unload constants.
- Added SimReplay coverage proving tuned rates change banked credits,
  credits-per-minute, and deterministic hash in a mid-run throughput window.
- Added `ReviewGate economytuning` to lock the architecture contract.

Automated gates:
- Command:
  `dotnet build ProceduralRts.csproj --no-restore`
  Result:
  Pass.
  Evidence:
  Build reported 0 errors and 0 warnings.
- Command:
  `dotnet run --project tools/SimReplay/SimReplay.csproj --no-restore`
  Result:
  Pass.
  Evidence:
  SimReplay passed all deterministic scenarios including `resource-loop`,
  `production-loop`, movement, combat, authored units, group move, group attack,
  firing anchor, and outcome.
- Command:
  `dotnet run --project tools/ReviewGate/ReviewGate.csproj economytuning --no-restore`
  Result:
  Pass.
  Evidence:
  ReviewGate reported 0 errors and 0 warnings for the economy tuning contract.
- Command:
  `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore`
  Result:
  Pass.
  Evidence:
  Global ReviewGate reported 0 errors and 0 warnings.
- Command:
  `dotnet run --project tools/VerifyAll/VerifyAll.csproj --no-restore`
  Result:
  Pass.
  Evidence:
  VerifyAll passed all 14 steps: build, SimReplay, CombatBehavior,
  SimulationSmoke, FogOfWarQa, SelectionStress, AiDifficultySmoke, ReviewGate,
  PerfSmoke, BalanceReport, and Godot headless QA scenes.

Manual/visual gates:
- Check:
  Visual QA.
  Result:
  Not required.
  Evidence:
  This is a headless deterministic simulation/data slice.

Reviewer result:
- Status: pass.
- Required fixes:
  - None at record creation.
- Residual risks:
  - Environment resource regeneration remains open and unimplemented.
  - `BalanceConfig` centralization remains open; this slice only covers the
    immediate economy loop knobs.
  - Live gameplay still has legacy authority paths until M1 migration.

TODO update:
- Items marked done:
  - `Economy is tunable from day one: gather rate, depletion behavior, refinery congestion, credits-per-minute metric in SimMetrics`.
- Items left open:
  - Environment resource regeneration and environment modifiers.
  - Deterministic economy tests that depend on future regeneration behavior.
  - Global `BalanceConfig` single-source tuning.
- Reason:
  - Current code exposes ResourceSystem economy knobs as pure EntityWorld data,
    keeps depletion behavior as resource-node data, uses existing metrics for
    refinery congestion and credits-per-minute, and proves the result through
    SimReplay plus `ReviewGate economytuning`.
