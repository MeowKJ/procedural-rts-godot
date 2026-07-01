# Review Record - M2 shared ally threat

Step:
M2 shared ally threat scoring and autonomy-chain closure.

Milestone:
M2 Movement Algorithms & Unit Autonomy.

Owner AI:
Codex main agent.

Reviewer AI:
Kuhn read-only M2 audit, ReviewGate, and deterministic replay.

Integrator AI:
Codex main agent.

Scope:
- Files/folders: `scripts/core/sim/systems/CombatSystem.cs`, `scripts/core/sim/systems/combat/CombatTargetSearchSystem.cs`, `scripts/core/sim/systems/combat/CombatTargetResolutionSystem.cs`, `tools/SimReplay/Combat/SharedAllyThreatScenarios.cs`, `tools/SimReplay/Program.cs`, `TODO.md`, `docs/reviews/2026-07-01-m2-shared-ally-threat.md`.
- Non-goals: no kiting/min-range implementation, no UI work, no balance-table migration, no GameState legacy targeting rewrite, and no faction-specific targeting logic.

Implementation summary:
- Kept automatic target choice as one deterministic candidate-scoring path rather than adding a new runtime state machine.
- Added shared ally-threat weighting: a visible hostile candidate that is automatically attacking a nearby self/allied entity can outrank a closer non-threat.
- Preserved player control: manual attack focus returns before automatic scoring and Ignore/PassiveRetaliate stances do not answer shared ally-threat calls.
- Preserved stance boundaries: Hold only shares when the threat is near weapon range plus slack; ReturnGuard respects its leash; Aggressive can answer broader nearby ally pressure.
- Added `shared-ally-threat` SimReplay coverage for ally protection, manual-focus preservation, and Ignore stance.
- Marked the M2 autonomy decision chain and target-priority scoring items complete; left kiting/min-range and its deterministic assertion open.

Automated gates:
- Command: `dotnet build ProceduralRts.csproj --no-restore`
  Result: pass.
  Evidence: build completed with 0 warnings and 0 errors.
- Command: `dotnet run --project tools/SimReplay/SimReplay.csproj --no-restore`
  Result: pass.
  Evidence: `OK [shared-ally-threat]: shared ally threats are prioritized without stealing manual focus or Ignore stance.` Full SimReplay passed.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore -- sharedallythreat`
  Result: pass.
  Evidence: ReviewGate completed with 0 errors and 0 warnings.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore -- review --require-record=m2-shared-ally-threat`
  Result: pass.
  Evidence: ReviewGate found this durable review record.

Manual/visual gates:
- Check: Not applicable.
  Result: not run.
  Evidence: this slice only changes deterministic simulation target scoring.

Reviewer result:
- Status: pass.
- Required fixes: none.
- Residual risks: kiting/min-range remains open and should get its own weapon-data field plus replay; large local fights still intentionally cap threat weighting to avoid focus cascades.

TODO update:
- Items marked done: M2 autonomy decision chain; M2 target priority scoring.
- Items left open: M2 kiting/min-range and the ranged min-range autonomy assertion.
- Reason: shared ally threat and broader threat scoring are now replay-proven, while kiting is a separate movement/combat behavior.
