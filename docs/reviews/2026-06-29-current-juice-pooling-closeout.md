# Review Record - Current juice pooling closeout

Step:
Close the combat-juice pooling TODO for current presentation effect families.

Milestone:
Combat Juice & Feedback.

Owner AI:
Codex main agent.

Reviewer AI:
Codex main-agent review using prior durable VFX review records plus `ReviewGate vfx`.

Integrator AI:
Codex main agent.

Scope:
- Files/folders:
  - `TODO.md`
  - `docs/reviews/2026-06-29-current-juice-pooling-closeout.md`
  - Existing evidence in `scripts/world/CombatEffectsLayer.cs`
  - Existing evidence in `scripts/world/CommandAcknowledgementLayer.cs`
  - Existing evidence in `scripts/world/FootprintLayer.cs`
  - Existing gate coverage in `tools/ReviewGate/Program.cs`
- Non-goals:
  - Do not add new visual effect families.
  - Do not tune colors, radii, opacity, or screen shake.
  - Do not convert gameplay projectiles/beams into presentation-only pools.
  - Do not claim future effect families are complete without extending the gate.

Implementation summary:
- No runtime code change was required in this closeout slice.
- The TODO wording was narrowed to "current juice" so the status matches evidence.
- Existing effect families are covered as follows:
  - Death VFX: pooled objects, soft/hard budgets, fade-oldest under load.
  - Impact flashes: pooled objects, soft/hard budgets, fade-oldest under load.
  - Command acknowledgement rings: pooled objects, soft/hard budgets, fade-oldest under load.
  - Footprints/trails: bounded decorative mark list with soft/hard budgets and under-load fade.
- `ReviewGate vfx` verifies these hooks and the presentation-only boundary.

Automated gates:
- Command:
  `dotnet run --project tools/ReviewGate/ReviewGate.csproj vfx --no-restore`
  Result:
  Pass.
  Evidence:
  ReviewGate reported 0 errors and 0 warnings.
- Command:
  `dotnet run --project tools/ReviewGate/ReviewGate.csproj review --require-record=current-juice-pooling-closeout --no-restore`
  Result:
  Pass.
  Evidence:
  ReviewGate reported 0 errors and 0 warnings.
- Command:
  `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore`
  Result:
  Pass.
  Evidence:
  ReviewGate reported 0 errors and 0 warnings.
- Command:
  `dotnet run --project tools/VerifyAll/VerifyAll.csproj --no-restore`
  Result:
  Pass.
  Evidence:
  VerifyAll passed all 14 steps: build, SimReplay, CombatBehavior,
  SimulationSmoke, FogOfWarQa, SelectionStress, AiDifficultySmoke, ReviewGate,
  PerfSmoke, BalanceReport, and Godot headless QA scenes.

Manual/visual gates:
- Check:
  Dense visible VFX stress pass.
  Result:
  Not run.
  Evidence:
  This slice closes a pooling/performance constraint, not final visual art tuning.

Reviewer result:
- Status: pass.
- Required fixes:
  - None at record creation.
- Residual risks:
  - Future effect families must be added to `ReviewGate vfx`; otherwise the closeout
    no longer covers "current juice".
  - Visible dense-battle tuning may still adjust fade timings or budgets.

TODO update:
- Items marked done:
  - `All current juice is pooled + capped + fade-oldest under load`.
- Items left open:
  - Hit-feedback art polish.
  - Death satisfaction art polish.
  - Heavy-impact feel tuning.
  - Combat readability under heavy juice.
- Reason:
  - All current pure-presentation effect families have bounded lifetime/quantity
    behavior and a durable regression gate; broader art polish remains separate.
