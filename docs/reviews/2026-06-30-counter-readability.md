# Review Record - Counter Readability QA

Step:
Prove the "Counters feel real" TODO slice with direct data checks plus
deterministic one-minute combat cases.

Milestone:
Playable 1v1 skirmish vertical slice - counter readability.

Owner AI:
Worker A.

Reviewer AI:
Pending independent reviewer / ReviewGate counterreadability.

Integrator AI:
Pending integrator.

Scope:
- Files/folders:
  - `scripts/core/sim/systems/CombatSystem.cs`
  - `tools/CounterReadabilityQa/CounterReadabilityQa.csproj`
  - `tools/CounterReadabilityQa/Program.cs`
  - `tools/ReviewGate/Program.cs`
  - `tools/VerifyAll/Program.cs`
  - `docs/reviews/2026-06-30-counter-readability.md`
- Non-goals:
  - No TODO checkbox update in this worker slice.
  - No Godot graphical preview.
  - No broad balance retuning beyond target-legality correctness.

Implementation summary:
- Added `CounterReadabilityQa`, a deterministic headless gate that checks
  `WeaponTargetProfile`, `MovementDomain`, `ArmorTag`, cost, and speed data
  before running canonical counter scenarios.
- The QA proves light pressure, tank-vs-vehicle, tank-vs-structure,
  rocket-vs-vehicle, aircraft-vs-ground-tank, AA-unit-vs-aircraft, and
  AA-turret-vs-aircraft outcomes inside a 60-second window.
- Fixed `CombatSystem` so manual focus and individual weapon mounts respect
  target legality; a VectorCannon no longer fires at aircraft just because a
  player manually ordered it.
- Wired CounterReadabilityQa into `tools/VerifyAll` and added the
  `counterreadability` ReviewGate mode.

Automated gates:
- Command:
  `dotnet run --project tools/CounterReadabilityQa/CounterReadabilityQa.csproj --no-restore`
  Result:
  Pass.
  Evidence:
  CounterReadabilityQa passed. Canonical resolutions were: light pressure 196
  ticks, tank-vs-vehicle 135 ticks, tank-vs-structure 154 ticks,
  rocket-vs-vehicle 212 ticks, aircraft-vs-ground-tank 207 ticks,
  AA-unit-vs-aircraft 167 ticks, and AA-turret-vs-aircraft 177 ticks.
- Command:
  `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore -- counterreadability`
  Result:
  Pass.
  Evidence:
  ReviewGate passed with 0 errors and 0 warnings, validating the tool,
  VerifyAll wiring, CombatSystem target-legality guard, and this review record.

Manual/visual gates:
- Check:
  Visual readability in Godot.
  Result:
  Not run; this slice is headless/dotnet only.
  Evidence:
  The TODO is gameplay-data proof, not a UI/art pass.

Reviewer result:
- Status: pass
- Required fixes:
  - None from the automated gate.
- Residual risks:
  - The scenarios prove canonical counters, not full ladder balance.
  - `BalanceReport` remains the broader multi-trial balance sanity check.

TODO update:
- Items marked done:
  - None by Worker A.
- Items left open:
  - `Counters feel real...` remains for Integrator to mark after total
    verification and review.
- Reason:
  - Worker A was instructed not to update TODO.md.

Gate tag:
counterreadability
