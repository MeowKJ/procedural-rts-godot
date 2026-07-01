# Review Record - Attack slot anchoring cleanup

Step: M2 group attack slot anchoring and firing-anchor feel slice
Milestone: M2 Movement Algorithms & Unit Autonomy
Owner AI: Worker Ptolemy
Reviewer AI: ReviewGate attackslotanchoring / Integrator
Integrator AI: Integrator / Codex

Scope:
- Files/folders: `scripts/core/sim/AttackSlotMath.cs`, `scripts/core/sim/systems/CombatSystem.cs`, `scripts/core/units/dog/DogInfantry.cs`, `tools/SimReplay/Program.cs`, `tools/ReviewGate/Program.cs`, `TODO.md`, `docs/reviews/2026-06-30-attack-slot-anchoring.md`.
- Non-goals: implementing full flow fields, multi-ring reassignment, dynamic slot reservation for moving targets, or changing weapon balance.

Implementation summary:
- `AttackSlotMath` now treats in-range attackers as anchors and reserves their ring bearing before assigning rear movers.
- Slot assignment includes target collision radius in standoff positioning so units aim around the target shape instead of the target center.
- `CombatSystem` includes target radius in standoff math while keeping actual firing range based on weapon range for balance.
- `CombatSystem` preserves valid group-attack `FormationSlot`s for ground/building targets instead of overwriting them with per-tick direct standoff chase.
- Air targets bypass static slot preservation so anti-air units keep dynamic standoff pursuit.
- Firing/cooling attackers hold position instead of being pushed forward by rear movers.
- `SimReplay` now proves `attack-slot math` and deterministic `anchored-group-attack-slotting`.
- Integration balance fix: dog infantry HP moved from 50 to 52 so clearer attack slots keep light parity and mixed-force counter readability inside `BalanceReport` bands.

Automated gates:
- Command: `dotnet build ProceduralRts.csproj --no-restore`
  Result: pass
  Evidence: build completed successfully with 0 warnings and 0 errors.
- Command: `dotnet run --project tools/SimReplay/SimReplay.csproj --no-restore`
  Result: pass
  Evidence: SimReplay completed successfully, including `attack-slot math` and `anchored-group-attack-slotting`.
- Command: `dotnet run --project tools/CombatBehavior/CombatBehavior.csproj --no-restore`
  Result: pass
  Evidence: CombatBehavior completed successfully.
- Command: `dotnet run --project tools/BalanceReport/BalanceReport.csproj --no-restore`
  Result: pass
  Evidence: BalanceReport completed successfully after the dog infantry HP integration fix.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore -- attackslotanchoring`
  Result: pass
  Evidence: ReviewGate attackslotanchoring completed with 0 errors and 0 warnings.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore -- review --require-record=attack-slot-anchoring`
  Result: pass
  Evidence: required review record check completed with 0 errors and 0 warnings.

Manual/visual gates:
- Check: live visual playtest not run for this code slice.
  Result: not run.
  Evidence: deterministic replay covers anchor reservation, slot preservation, and rear-mover behavior.

Reviewer result:
- Status: pass
- Required fixes: removed non-ASCII comment text from `AttackSlotMath.cs`; kept target radius out of firing range math; bypassed static slot preservation for air targets after `CounterReadabilityQa` exposed anti-air drift; adjusted dog infantry HP from 50 to 52 after `BalanceReport` exposed light parity drift.
- Residual risks: this is not full M2 flow-field/corridor movement. Moving targets and long-duration multi-ring slot redistribution remain future work.

TODO update:
- Items marked done: `Attack slot anchoring cleanup`.
- Items left open: broader M2 flow-field/corridor and autonomy work remains open.
- Reason: this is one bounded group-attack feel improvement.
