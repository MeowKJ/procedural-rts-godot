# Review Record - construction system core

Step: M3-M4 build/construction/production/economy slice
Milestone: ConstructionSystem pure sim skeleton
Owner AI: Worker-M3
Reviewer AI: pending
Integrator AI: pending

Scope:
- Files/folders:
  - scripts/core/entities/EntityCommand.cs
  - scripts/core/entities/EntityComponentState.cs
  - scripts/core/entities/EntityStateHash.cs
  - scripts/core/sim/systems/ConstructionSystem.cs
  - scripts/BattleRoot.cs
  - tools/SimReplay/Program.cs
- Non-goals:
  - HUD/build menu and placement UI.
  - Movement/combat behavior changes.
  - Legacy GameState deletion or TODO.md updates.

Implementation summary:
- Added StartConstructionEntityCommand for pure sim build intents.
- Added ConstructionIdentityComponentState and deterministic hashing support.
- Added ConstructionSystem to validate prerequisites/build radius/resources, spend credits, spawn under-construction BuildSpec-backed entities, and advance construction to exact completion.
- Registered ConstructionSystem in the shadow EntityWorld pipeline before PowerSystem.
- Added SimReplay construction-loop coverage for rejected prerequisites, accepted resource spending, deterministic progress, completion, and activation through PowerSystem/ProductionSystem-visible state.

Automated gates:
- Command: `dotnet run --project tools/SimReplay/SimReplay.csproj --no-restore`
  Result: pass
  Evidence: SimReplay PASSED; construction-loop deterministic final hash EAC6A5FBA32D614E with 3 buildings and 80 credits.

Manual/visual gates:
- Check: UI/HUD review
  Result: not applicable
  Evidence: Slice is pure simulation and does not touch presentation UI.

Reviewer result:
- Status: pass-with-warnings
- Required fixes: none from this implementation pass.
- Residual risks: Placement collision/terrain legality, cancellation/refunds, builder worker assignment, and HUD command bridging remain future work.

TODO update:
- Items marked done: none by Worker-M3.
- Items left open: broader construction/build placement TODO remains open for integrator follow-up.
- Reason: User explicitly requested not to update TODO.md.
