# Review Record - Target visibility autonomy

Step:
Make non-manual CombatSystem target selection respect current gameplay visibility.

Milestone:
M2 Movement/autonomy decision semantics.

Owner AI:
Worker A / Codex.

Reviewer AI:
Pending.

Integrator AI:
Pending.

Scope:
- Files/folders: `scripts/core/sim/systems/CombatSystem.cs`, `scripts/BattleRoot.cs`, `tools/SimReplay/Program.cs`, `tools/BalanceReport/Program.cs`, `tools/ReviewGate/Program.cs`, `docs/reviews/2026-06-29-target-visibility-autonomy.md`.
- Non-goals: no fog last-known position memory, no UI fog changes, no target-priority redesign, no construction/placement/unit-catalog changes.

Implementation summary:
- Auto-acquire candidates now require `VisibilityIndex` visibility for the attacker's owner.
- Existing non-manual sticky targets are retained only while still visible.
- PassiveRetaliate resolves and records retaliatory targets only when the victim owner can currently see the attacker.
- Manual attack targets keep their existing live/hostile legality path and are not visibility-gated.
- The EntityWorld runtime pipeline now rebuilds gameplay visibility before CombatSystem consumes it.
- BalanceReport now runs `VisionSystem` before `CombatSystem`, so canonical duel
  checks use the same gameplay visibility contract as live EntityWorld combat.
- `SimReplay` adds `target-visibility` coverage for hidden auto targets, visible target preference, hidden manual focus, and hidden PassiveRetaliate rejection.

Automated gates:
- Command: `dotnet build ProceduralRts.csproj`
  Result: pass.
  Evidence: build completed with 0 warnings and 0 errors.
- Command: `dotnet run --project tools/SimReplay/SimReplay.csproj`
  Result: pass.
  Evidence: `OK [target-visibility]: auto acquire and PassiveRetaliate require current visibility; manual focus still works.` Full SimReplay passed.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj targetvisibility`
  Result: pass.
  Evidence: ReviewGate completed with 0 errors and 0 warnings.

Manual/visual gates:
- Check: Not applicable; this is a deterministic sim-core target selection slice.
  Result: not run.
  Evidence: no presentation surface changed.

Reviewer result:
- Status: ready for review.
- Required fixes: none known.
- Residual risks: Last-known enemy memory remains open; hidden enemies are simply unavailable for automatic selection until currently visible.

TODO update:
- Items marked done: none.
- Items left open: broader AI planner use of `VisibilityIndex`, last-known targeting memory, and full M2 tactical threat weighting.
- Reason: this slice only proves visible-only automatic target selection and preserves manual attack behavior.
