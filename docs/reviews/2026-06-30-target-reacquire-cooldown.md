# Review Record - Target Re-Acquire Cooldown

Step:
Close the M2 target stickiness/hysteresis cooldown slice for automatic combat
targets without blocking manual attack focus.

Milestone:
M2 Movement Algorithms & Unit Autonomy.

Owner AI:
Worker-M2 plus Codex integrator fix.

Reviewer AI:
Integrator gate review via `ReviewGate targetreacquirecooldown`.

Integrator AI:
Codex main thread.

Scope:
- Files/folders:
  - `scripts/core/sim/systems/CombatSystem.cs`
  - `scripts/core/sim/systems/CommandSystem.cs`
  - `scripts/core/entities/EntityComponentState.cs`
  - `scripts/core/entities/EntityStateHash.cs`
  - `scripts/core/sim/SimInvariants.cs`
  - `tools/SimReplay/Program.cs`
  - `tools/ReviewGate/Program.cs`
  - `TODO.md`
- Non-goals:
  - No last-known-position memory.
  - No shared ally-threat propagation.
  - No kiting/min-range micro.

Implementation summary:
- Added deterministic `AutoReacquireCooldownRemaining` state on weapon users.
- Hashed and invariant-checked the cooldown state.
- Starts the short cooldown after an automatic target is lost, while manual
  attacks clear and bypass the cooldown immediately.
- Integrator adjusted the worker patch so newly locked automatic targets do not
  self-impose a firing/selection delay; this preserved counter-readability combat
  pacing while keeping the lost-target cooldown behavior.

Automated gates:
- Command:
  `dotnet run --project tools/SimReplay/SimReplay.csproj --no-restore`
  Result:
  Pass.
  Evidence:
  `target-reacquire-cooldown`, `manual-attack-reacquire-bypass`, and the negative
  cooldown invariant pass deterministically.
- Command:
  `dotnet run --project tools/CounterReadabilityQa/CounterReadabilityQa.csproj --no-restore`
  Result:
  Pass.
  Evidence:
  Counter duels still resolve within the 60-second readability window after the
  cooldown integration.
- Command:
  `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore -- targetreacquirecooldown`
  Result:
  Pass.
  Evidence:
  Static gate locks state, hash, invariant, command, combat, and replay coverage.

Reviewer result:
Pass. The cooldown kills target-flicker after lost automatic targets without
making manual attack or normal target locking feel delayed.

Status:
Pass.

Residual risks:
- Shared ally-threat and last-known-position behavior remain open M2 work.
- Kiting/min-range micro remains open.

TODO update:
- Marked done: M2 target stickiness + hysteresis re-acquire cooldown.
