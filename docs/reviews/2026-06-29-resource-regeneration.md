# Review Record - Resource regeneration

Step:
Implement deterministic resource regeneration with cap, corruption, atmosphere,
and powered aura modifiers.

Milestone:
M4 Production & Economy System / Resource, Mining & Environment Regeneration.

Owner AI:
Codex main agent.

Reviewer AI:
Codex main-agent review with `ReviewGate resourceregen`,
`ReviewGate economyproductiontests`, `SimReplay`, and full `VerifyAll`.

Integrator AI:
Codex main agent.

Scope:
- Files/folders:
  - `scripts/core/ResourceAtmosphere.cs`
  - `scripts/core/sim/EconomyTuningConfig.cs`
  - `scripts/core/entities/EntityComponentState.cs`
  - `scripts/core/entities/EntityStateHash.cs`
  - `scripts/core/entities/EntityWorld.cs`
  - `scripts/core/sim/SimInvariants.cs`
  - `scripts/core/sim/systems/ResourceSystem.cs`
  - `tools/SimReplay/Program.cs`
  - `tools/ReviewGate/Program.cs`
  - `TODO.md`
  - `docs/reviews/2026-06-29-resource-regeneration.md`
- Non-goals:
  - Do not migrate live legacy economy authority.
  - Do not make harvest commands auto-pick nearest resource fields.
  - Do not add UI or visual resource effects.

Implementation summary:
- Added `ResourceAtmosphere` as hashed EntityWorld state for day/night/corruption
  economy hooks.
- Extended `EconomyTuningConfig` with regeneration rate, cap, corruption,
  atmosphere, and aura multipliers.
- Added `RegenerationProgress` to `ResourceNodeComponentState` so fractional
  regrowth is deterministic without per-tick rounding spikes.
- Added `ResourceRegenerationAuraComponentState` for powered safe-zone/light/signal
  style regrowth boosts.
- Updated `ResourceSystem` to regenerate only `DepleteThenRegrow` nodes, respect
  cap ratio, suppress hostile corruption, slow tainted/night states, and apply the
  strongest powered aura.
- Added hash and invariant coverage for the new state.
- Added deterministic `resource-regen` SimReplay coverage and ReviewGate modes.

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
  SimReplay reported deterministic `resource-regen` with aura 75, night 55,
  tainted 32, hostile 10, non-regrow 10, plus existing resource, production,
  movement, combat, group, and outcome scenarios.
- Command:
  `dotnet run --project tools/ReviewGate/ReviewGate.csproj resourceregen --no-restore`
  Result:
  Pass.
  Evidence:
  ReviewGate reported 0 errors and 0 warnings for resource regeneration.
- Command:
  `dotnet run --project tools/ReviewGate/ReviewGate.csproj economyproductiontests --no-restore`
  Result:
  Pass.
  Evidence:
  ReviewGate reported 0 errors and 0 warnings and now requires
  `AssertResourceRegeneration`.
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
  - Live gameplay still uses legacy economy paths until EntityWorld authority
    migration.
  - Actual signal towers/road lights are not yet spawned into EntityWorld; the
    generic powered aura component is ready for that migration.
  - Harvest commands still target a specified resource node rather than
    auto-picking the nearest available node.

TODO update:
- Items marked done:
  - `Environment resource regeneration (the "alive map" hook)`.
  - Regen environment modulation, cap/pacing tunables, and day/night atmosphere
    hook subitems.
  - `Deterministic economy tests: gather/dock/unload/deplete, regen up-to-cap with environment modifiers, congestion fairness, in SimReplay`.
- Items left open:
  - Mining loop auto-picks nearest available `ResourceNode`.
  - EntityWorld live authority migration.
  - Global `BalanceConfig` single-source tuning.
- Reason:
  - `resource-loop`, `AssertDockCongestionMetrics`, and `resource-regen` now cover
    the named deterministic economy behaviors, and `ReviewGate resourceregen` plus
    `ReviewGate economyproductiontests` lock that coverage.
