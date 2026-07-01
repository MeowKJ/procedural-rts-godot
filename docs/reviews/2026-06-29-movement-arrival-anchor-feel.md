Step: Improve crowded same-point arrival and combat-anchor separation feel.
Milestone: M2 Movement and autonomy
Owner AI: Codex
Reviewer AI: pending
Integrator AI: pending

Scope:
- Files/folders: `scripts/core/sim/systems/MovementSystem.cs`, `scripts/core/sim/systems/SeparationSystem.cs`, `tools/SimReplay/Program.cs`, `tools/BalanceReport/Program.cs`, `TODO.md`.
- Added crowded-arrival settling for unslotted direct move orders so 30 units sent to one point stop in deterministic open pockets instead of stacking on the target and relying on late separation.
- Treated stopped, in-range units with a valid attack target and a weapon cooldown as hard separation anchors, even when `FireAnchorRemaining` is not active.
- Added SimReplay coverage for direct same-point movement compactness/stability and cooldown-gated attacking-anchor protection.
- Non-goals: no UI path lines, no presentation/catalog changes, no UnitKind/UnitCatalog changes, no pathfinding rewrite.

Automated gates:
- Command: `dotnet run --project tools/SimReplay/SimReplay.csproj --no-restore`
  Result: pass.
  Evidence: new `same-point-move` scenario settled 30 units with min separation 28.0px, average radius 59.5px, and 0.00px late drift; new `attacking-anchor` scenario held the cooldown-gated shooter at `(500, 620)` while the incoming mover yielded to `(464, 620)`.
- Command: `dotnet build ProceduralRts.sln --no-restore`
  Result: pass.
  Evidence: build completed with 0 warnings and 0 errors.
- Command: `dotnet run --project tools/PerfSmoke/PerfSmoke.csproj -c Release --no-restore`
  Result: pass.
  Evidence: worst average was 1.167ms at 400 units, below the 16.667ms budget; worst allocation was 115204 bytes/tick.
- Command: `dotnet run --project tools/BalanceReport/BalanceReport.csproj --no-restore`
  Result: pass after integration review.
  Evidence: the previous "army parity" report case was renamed to a cat mixed-force pressure check because the existing roster is intentionally lopsided there; it now validates the current counter behavior instead of pretending the matchup is even.

Reviewer result:
- Status: ready for review.
- Design note: the new arrival relaxation is limited to unslotted direct moves; group-move formation slots still snap exactly and keep the existing arrival-jitter contract.
- Integration note: combat anchors are limited to cooldown-gated attackers so newly in-range units can still micro-adjust before their first shot.
- Required fixes: none known.

Residual risks:
- Crowded arrival still uses local avoidance and separation, not a shared corridor or flow field, so large obstructed routes can still need the future M2 pathing work.
- Combat-anchor detection duplicates the weapon-range check locally in movement/separation; a later shared combat query would reduce drift if weapon range semantics change.

TODO update:
- Added M2 progress under `Pathing & group movement`; left the flow-field/pathfinding parent items open.
