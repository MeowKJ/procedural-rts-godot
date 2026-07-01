# Review Record - PassiveRetaliate core

Step:
Implement the minimal PassiveRetaliate semantics slice for EntityWorld combat.

Milestone:
M2 Unit autonomy decision/stance semantics.

Owner AI:
Worker-M2C / Codex.

Reviewer AI:
Pending.

Integrator AI:
Pending.

Scope:
- Files/folders: `scripts/core/entities/EntityComponentState.cs`, `scripts/core/entities/EntityStateHash.cs`, `scripts/core/sim/SimInvariants.cs`, `scripts/core/sim/systems/CombatSystem.cs`, `tools/SimReplay/Program.cs`, `tools/ReviewGate/Program.cs`, `docs/reviews/2026-06-29-passive-retaliate-core.md`.
- Non-goals: no fog last-known memory, no kiting, no construction cleanup, no UI changes, no target-grid rewrite, no full shared-threat priority model.

Implementation summary:
- Added `RetaliationComponentState` to store the most recent valid threat target and tick.
- `CombatSystem` now records attackers when a PassiveRetaliate unit is damaged and has no manual target.
- PassiveRetaliate still does not auto-acquire nearby hostiles; it only resolves the recorded attacker when explicit movement is not active, the target is hostile, inside weapon range, weapon-legal, and inside the autonomy leash.
- Manual attack focus remains higher priority than retaliatory threats.
- Deterministic state hash and invariant validation include retaliation state.
- `SimReplay` adds `passive-retaliate` coverage for idle-before-threat, attacker retaliation, and manual focus priority.

Automated gates:
- Command: `dotnet build ProceduralRts.csproj --no-restore`
  Result: pass.
  Evidence: build completed with 0 warnings and 0 errors.
- Command: `dotnet run --project tools/SimReplay/SimReplay.csproj --no-restore`
  Result: pass.
  Evidence: `OK [passive-retaliate]: passive stayed idle until hit, retaliated against its attacker, and preserved manual focus.` Full SimReplay passed.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj passiveretaliate --no-restore`
  Result: pass.
  Evidence: ReviewGate completed with 0 errors and 0 warnings.

Manual/visual gates:
- Check: Not applicable; this is a deterministic sim-core slice.
  Result: not run.
  Evidence: no presentation surface changed.

Reviewer result:
- Status: ready for review.
- Required fixes: none known.
- Residual risks: PassiveRetaliate currently responds only to direct damage events, not broader ally threat weighting or fog last-known memory.

TODO update:
- Items marked done: none.
- Items left open: broader M2 decision chain, target priority threat weighting, and full shared-threat semantics remain open.
- Reason: user explicitly requested not to update TODO.md.
