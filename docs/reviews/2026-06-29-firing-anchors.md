# Review Record - Firing anchors

Step: Make units that recently fired become temporary non-displaceable anchors.
Milestone: M2 movement, combat feel, and autonomy.
Owner AI: Codex.
Reviewer AI: Codex self-review with deterministic simulation and static gates.
Integrator AI: Codex.

Scope:
- Files/folders: `scripts/core/entities/EntityComponentState.cs`, `scripts/core/entities/EntityStateHash.cs`, `scripts/core/sim/SimInvariants.cs`, `scripts/core/sim/systems/CombatSystem.cs`, `scripts/core/sim/systems/MovementSystem.cs`, `scripts/core/sim/systems/SeparationSystem.cs`, `tools/SimReplay/Program.cs`, `tools/ReviewGate/Program.cs`, `TODO.md`, `docs/reviews/2026-06-29-firing-anchors.md`.
- Non-goals: no new stance model, no kiting behavior, no flow-field corridor work, no target memory, no visual command-line polish.

Implementation summary:
- Added `MovementComponentState.FireAnchorRemaining` as deterministic movement state.
- Combat sets a short 0.26 second fire-anchor window when a shot is fired, clears movement intent, and zeros velocity so active shooters hold their firing spot.
- Movement decays the anchor timer deterministically and treats recently fired units as higher-priority avoidance anchors.
- Separation treats recent-fire units as hard anchors so moving units yield around them instead of shoving them forward.
- State hashing and invariants now include the anchor timer.
- SimReplay now has a `firing-anchor` scenario proving a rear mover yields while the shooter stays stable.
- ReviewGate now has a `firinganchors` mode to keep component, combat, movement, separation, hashing, invariant, and replay coverage in place.

Automated gates:
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj firinganchors --no-restore`
  Result: pass
  Evidence: ReviewGate reported 0 errors and 0 warnings.
- Command: `dotnet run --project tools/SimReplay/SimReplay.csproj --no-restore`
  Result: pass
  Evidence: `firing-anchor` was deterministic for 80 ticks; shooter held at `(500, 500)` and mover yielded to `(464, 500)`.
- Command: `dotnet run --project tools/CombatBehavior/CombatBehavior.csproj --no-restore`
  Result: pass
  Evidence: CombatBehavior passed weapon, turret, terrain, localization, production, economy, enemy AI, and outcome checks.
- Command: `dotnet run --project tools/BalanceReport/BalanceReport.csproj --no-restore`
  Result: pass
  Evidence: canonical balance scenarios stayed inside their expected bands after tuning the anchor window from 0.65s to 0.26s.

Manual/visual gates:
- Check: in-game visual QA
  Result: not run
  Evidence: this slice changes deterministic sim/feel rules only; broader visual command feedback and sandbox playtest remain separate TODO items.

Reviewer result:
- Status: pass
- Required fixes: initial `ReviewGate firinganchors` text check looked for an over-specific `m.FireAnchorRemaining > 0` substring while the implementation used the null-safe `m?.FireAnchorRemaining > 0`; the gate was narrowed to the semantic token `FireAnchorRemaining > 0`. Full verification also showed 0.65s made firing units nearly permanent anchors and pushed army parity out of band, so the window was tuned to 0.26s.
- Residual risks: the anchor duration is currently a fixed `CombatSystem.FireAnchorSeconds = 0.26f`; future unit specs may want weapon- or role-specific anchor windows.

TODO update:
- Items marked done: `Firing anchors (already in Separation): a unit that has fired recently is a non-displaceable anchor for a short window so micro does not shove shooters.`
- Items left open: stance semantics, kiting, last-known-position memory, deterministic autonomy tests beyond this scenario, and broader movement/formation polish.
- Reason: fired units now become deterministic short-lived hard anchors, separation respects them, and replay coverage proves nearby movers route/yield instead of pushing the shooter.
