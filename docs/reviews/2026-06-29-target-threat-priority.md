# Review Record - Target threat priority

Step:
Add deterministic threat weighting to CombatSystem automatic target scoring.

Milestone:
M2 target priority scoring.

Owner AI:
Worker D / Codex.

Reviewer AI:
Pending.

Integrator AI:
Pending.

Scope:
- Files/folders: `scripts/core/sim/systems/CombatSystem.cs`, `tools/SimReplay/Program.cs`, `tools/ReviewGate/Program.cs`, `docs/reviews/2026-06-29-target-threat-priority.md`.
- Non-goals: no construction, placement, unit design catalog, HUD, fog memory, turret combat, or full squad threat-sharing changes.

Implementation summary:
- `CombatSystem` keeps base `TargetPriority` for WeaponTargetProfile legality and armor/domain/weight scoring.
- Automatic target scoring now applies a deterministic `ThreatTargetPriorityMultiplier` when a visible candidate's non-manual `WeaponUserComponentState.AttackTarget` points at the attacker.
- Integration tightened threat detection to ignore `AttackTargetIsManual` on the candidate, so explicit focus-fire orders are not reinterpreted as automatic threat-weight signals and canonical BalanceReport duels remain symmetric.
- Integration also bounded direct threat weighting to small local fights through
  `ThreatTargetMaxLocalCandidates`, preventing large symmetric duels from turning
  into deterministic self-defense focus cascades.
- Manual attack focus still returns before automatic scoring and is not reordered by threat weighting.
- Existing automatic candidate and sticky-current visibility gates remain in place.
- `SimReplay` adds `target-threat-priority` coverage for visible automatic threat-over-distance selection and manual non-threat focus preservation.
- `ReviewGate` adds the narrow `targetthreatpriority` gate.

Automated gates:
- Command: `dotnet build ProceduralRts.csproj --no-restore`
  Result: pass.
  Evidence: build completed with 0 warnings and 0 errors.
- Command: `dotnet run --project tools/SimReplay/SimReplay.csproj --no-restore`
  Result: pass.
  Evidence: `OK [target-threat-priority]: auto-acquire preferred the visible attacker threat, while manual focus stayed fixed.` Full SimReplay passed.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj targetthreatpriority --no-restore`
  Result: pass.
  Evidence: ReviewGate completed with 0 errors and 0 warnings.

Manual/visual gates:
- Check: Not applicable; this is a deterministic sim-core slice.
  Result: not run.
  Evidence: no presentation surface changed.

Reviewer result:
- Status: ready for review after automated gates pass.
- Required fixes: none known.

Residual risks:
- Threat weighting only considers direct candidate focus on this attacker, not ally-protection scoring or shared squad threat memory.
- Manual focus-fire by an opposing player is intentionally excluded from this automatic weight; richer tactical response to explicit enemy focus can be added later with separate balance coverage.
- Large army fights intentionally fall back to base weapon-profile target scoring;
  broader shared-threat squad logic remains a separate M2 item.
- The multiplier is a simple deterministic heuristic and may need future balance tuning against richer target profiles.

TODO update:
- Items marked done: none.
- Items left open: broader M2 target priority scoring remains open for future threat, focus-fire, and tactical context work.
- Reason: this worker slice intentionally adds one bounded, replay-proven threat weighting step.
