# Review Record - Autonomy target stickiness

Step:
Add deterministic target stickiness and lightweight hysteresis to automatic combat
target selection.

Milestone:
M2 Unit autonomy redesign.

Owner AI:
Worker A / Codex.

Reviewer AI:
Pending.

Integrator AI:
Pending.

Scope:
- Files/folders: `scripts/core/sim/systems/CombatSystem.cs`, `tools/SimReplay/Program.cs`, `TODO.md`, `docs/reviews/2026-06-29-autonomy-target-stickiness.md`.
- Non-goals: no kiting, no last-known fog memory, no complete three-radius model, no unit balance/catalog changes, no UI changes.
- Boundaries: explicit manual attack remains first priority; direct/ignore move intent suppresses automatic target acquisition instead of letting combat replace the player move target.

Implementation summary:
- `CombatSystem` now persists non-manual auto targets in `WeaponUserComponentState`.
- Auto-acquire keeps the current valid hostile target inside an acceptable sticky range.
- Switching requires the current target to become invalid/out of sticky range, or a candidate to be clearly better by priority margin or large distance improvement.
- Auto target priority uses each mount's `WeaponTargetProfile` against target armor/domain/weight; ties remain deterministic by distance and then `EntityId`.
- Combat-owned chase movement clears stale formation slots so explicit formation/direct move orders can still be recognized separately.
- `SimReplay` adds `target-stickiness` / `no-target-flicker`: one hostile drifts behind another, and the attacker must keep target 2 with `TargetSwitchCount == 0`.

Automated gates:
- Command: `dotnet build ProceduralRts.csproj --no-restore`
  Result: pass.
  Evidence: build completed with 0 warnings and 0 errors.
- Command: `dotnet run --project tools/SimReplay/SimReplay.csproj --no-restore`
  Result: pass.
  Evidence: `OK [no-target-flicker]: auto target stayed on 2; switches=0.` Full SimReplay passed.
- Command: `dotnet run --project tools/PerfSmoke/PerfSmoke.csproj -c Release --no-restore`
  Result: pass.
  Evidence: worst average was 1.260ms at 400 units under the 16.667ms budget; worst allocation was 115204 bytes/tick.

Reviewer result:
- Status: ready for review.
- Required fixes: none known.

Residual risks:
- The sticky range is still a bounded heuristic, not the future explicit `LeashRange` data model.
- No reacquire cooldown was added in this slice; the priority/distance hysteresis covers the flicker case without introducing another timer state.
- `git status` could not be used because this workspace is not inside a Git repository.

TODO update:
- Added progress notes under M2 target stickiness and deterministic autonomy tests.
- Left parent checklist items open because leash, fog memory, retaliation, ignore-stance, min-range, and reacquire cooldown work remain.
