# Review Record - VFX budget and load fade

Step:
Add bounded VFX/footprint load control so visual effects cannot grow without
budget pressure.

Milestone:
M6 Performance.

Owner AI:
Codex main agent.

Reviewer AI:
Kant subagent read-only review, plus Codex main-agent integration.

Integrator AI:
Codex main agent.

Scope:
- Files/folders:
  - `scripts/world/CombatEffectsLayer.cs`
  - `scripts/world/FootprintLayer.cs`
  - `tools/ReviewGate/Program.cs`
  - `TODO.md`
  - `docs/reviews/2026-06-29-vfx-budget-fade.md`
- Non-goals:
  - Do not move projectiles/beams into a new pool in this slice.
  - Do not change combat simulation or authoritative projectile behavior.
  - Do not mark the broad VFX pooling TODO complete.
  - Do not add new visual styles or tune combat balance.

Implementation summary:
- `CombatEffectsLayer` now reuses `UnitDeathEffect` objects through a small pool.
- Unit-death effects have a soft budget and hard budget; old effects past the soft
  budget are shortened to a quick fade, and hard overflow returns to the pool.
- `FootprintLayer` now has soft and hard mark budgets; old marks past the soft budget
  are aged into a short fade instead of only being abruptly removed at the hard cap.
- `FootprintLayer` now reuses live-unit and expired-trail cleanup collections instead
  of allocating a `HashSet` and LINQ materialization every frame.
- Added `ReviewGate vfx` to verify that the budget/fade/pool hooks remain present.

Automated gates:
- Command:
  `dotnet build ProceduralRts.csproj`
  Result:
  Pass.
  Evidence:
  Build completed with 0 warnings and 0 errors.
- Command:
  `dotnet run --project tools/ReviewGate/ReviewGate.csproj vfx`
  Result:
  Pass.
  Evidence:
  ReviewGate reported 0 errors and 0 warnings.
- Command:
  `dotnet run --project tools/ReviewGate/ReviewGate.csproj review --require-record=vfx-budget-fade`
  Result:
  Pass.
  Evidence:
  ReviewGate reported 0 errors and 0 warnings.
- Command:
  `dotnet run --project tools/ReviewGate/ReviewGate.csproj`
  Result:
  Pass.
  Evidence:
  ReviewGate reported 0 errors and 0 warnings.

Manual/visual gates:
- Check:
  Independent Reviewer AI read-only audit.
  Result:
  Pass with warnings.
  Evidence:
  Reviewer verified the death-effect pool/budgets/fade, footprint soft/hard budgets,
  and that TODO remains open. Reviewer warned that age-shortening can make overloaded
  death effects visually jump and that dense-battle visual QA is still needed.
- Check:
  Godot runtime visual QA.
  Result:
  Not run.
  Evidence:
  The slice is behavior-preserving at API level, but no in-engine battle stress
  screenshot/video was captured.

Reviewer result:
- Status: pass-with-warnings
- Required fixes:
  - Reuse footprint cleanup collections instead of allocating a `HashSet`/LINQ array
    in every `_Process`; fixed in this slice and guarded by `ReviewGate vfx`.
- Residual risks:
  - Only unit-death effects are pooled in this slice; projectiles/beams remain
    gameplay models owned by `GameState`.
  - Footprints are value records in a capped list, not pooled heap objects.
  - Shortening effect age under load may visually jump young death bursts into their
    fade phase; dense-battle visual QA should tune this if it reads poorly.
  - Visual QA under a dense battle has not been performed.

TODO update:
- Items marked done:
  - None; the broad VFX pooling item remains open.
- Items left open:
  - Pool/cap/fade other future effect families and any dedicated impact layers.
  - Manual stress QA for dense combat.
- Reason:
  - Evidence covers death-effect pooling plus footprint/effect load fading, but not
    every VFX family that the broad TODO anticipates.
