# Review Record - Last-known-position memory

Step:
M2 Movement/autonomy first bounded Last-known-position memory slice.

Milestone:
M2 Movement Algorithms & Unit Autonomy.

Owner AI:
Parallel implementation worker B.

Reviewer AI:
Static reviewer gate via `ReviewGate lastknowntargetmemory`.

Integrator AI:
Parallel implementation worker B.

Scope:
- Files/folders:
  - `scripts/core/entities/EntityComponentState.cs`
  - `scripts/core/entities/EntityStateHash.cs`
  - `scripts/core/sim/SimInvariants.cs`
  - `scripts/core/sim/systems/CombatSystem.cs`
  - `tools/SimReplay/Program.cs`
  - `tools/ReviewGate/Program.cs`
  - `docs/reviews/2026-06-30-last-known-target-memory.md`
- Non-goals:
  - No UI work.
  - No Sandbox work.
  - No UnitSpec presentation cleanup.
  - No projectile/M5 implementation.
  - No changes to manual attack authority.

Implementation summary:
- Added deterministic `LastKnownTargetPosition` and `LastKnownTargetRemaining`
  state to weapon users.
- Hashed and invariant-checked last-known memory, including negative and expired
  memory probes.
- `CombatSystem` refreshes memory only while the current automatic target is
  visible, decays it every tick, clears active fire authority when the target
  enters fog, and starts the existing auto re-acquire cooldown.
- Short-range non-tracking units chase the remembered point; ranged units stop
  blind combat movement; tracking missile users are left under weapon projectile
  rule instead of being forced into fog.

Automated gates:
- Command:
  `dotnet build ProceduralRts.csproj --no-restore`
  Result:
  Pass.
  Evidence:
  Main project built successfully: 0 warnings, 0 errors.
- Command:
  `dotnet run --project tools/SimReplay/SimReplay.csproj --no-restore`
  Result:
  Pass.
  Evidence:
  `last-known-target-memory` passes deterministically; short-range chase,
  ranged hold, tracking-missile hold, memory decay/clear, and last-known
  invariant probes are covered. Existing `target-visibility`,
  `target-reacquire-cooldown`, and `manual-attack-reacquire-bypass` scenarios
  also pass.
- Command:
  `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore -- lastknowntargetmemory`
  Result:
  Pass.
  Evidence:
  Static gate locks component, hash, invariant, combat, replay, and review-record
  coverage for the slice.
- Command:
  `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore -- review --require-record=2026-06-30-last-known-target-memory`
  Result:
  Pass.
  Evidence:
  Durable review record format and required evidence fields are accepted.

Reviewer result:
Pass. No new path fires at invisible automatic targets: active auto fire
authority clears before last-known movement intent, the existing
`IsVisibleToOwner` gates remain in place, and manual attacks remain outside the
new memory path.

Status:
Pass.

Residual risks:
- Guard-order last-known behavior is not expanded in this first slice.
- M5 projectile entities are still future work; this slice only avoids forcing
  tracking-missile users into blind unit movement.
- Shared ally-threat scoring remains open.

TODO update:
- Marked done: M2 Last-known-position memory first combat-autonomy slice.
