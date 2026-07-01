# Review Record - Pooled impact VFX

Step: Close the current pure combat VFX pooling gap by adding pooled, budgeted impact flashes.
Milestone: Performance optimization - VFX pooling.
Owner AI: Codex.
Reviewer AI: Codex self-review with ReviewGate coverage.
Integrator AI: Codex.

Scope:
- Files/folders: `scripts/world/CombatEffectsLayer.cs`, `scripts/BattleRoot.cs`, `tools/ReviewGate/Program.cs`, `TODO.md`, `docs/reviews/2026-06-29-pooled-impact-vfx.md`.
- Non-goals: no gameplay projectile rewrite, no EntityWorld event migration, no change to damage authority or combat balance, no new full-screen post-processing.

Implementation summary:
- Added pooled `ImpactFlashEffect` objects to `CombatEffectsLayer`, separate from death effects and gameplay projectile/beam state.
- Added soft/hard impact budgets, a reusable impact pool, and under-load fade-out so hit feedback cannot grow without bounds.
- Routed existing live runtime hit callbacks in `BattleRoot` into `AddImpactFlash` for unit hits, building hits, and legacy `GameState.EntityAttacked` hits.
- Kept projectiles and beams as gameplay models drawn directly by the presentation layer; no pure presentation impact effect is spawned as an `EntityKind.Effect`.
- Extended `ReviewGate vfx` so death effects, impact flashes, command rings, and footprints all stay pooled/budgeted presentation paths.

Automated gates:
- Command: `dotnet build ProceduralRts.csproj --no-restore`
  Result: pass
  Evidence: build completed with 0 warnings and 0 errors.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj vfx --no-restore`
  Result: pass
  Evidence: ReviewGate reported 0 errors and 0 warnings for VFX pooling/boundary checks.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj presentation --max-warnings=0 --no-restore`
  Result: pass
  Evidence: presentation gate remained at the 0-warning baseline after adding impact flashes.

Manual/visual gates:
- Check: rendered impact-flash look
  Result: not run
  Evidence: this slice is a pooling/performance architecture pass; Godot headless smoke is covered by VerifyAll.

Reviewer result:
- Status: pass
- Required fixes: none.
- Residual risks: future new VFX families still need to be added through the same pooled/budgeted pattern and covered by `ReviewGate vfx`; deeper art direction for impact shapes remains part of the combat-juice TODOs.

TODO update:
- Items marked done: `Pool combat VFX/footprints; cap concurrent effects; fade oldest under load`.
- Items left open: death/impact art variation, hit-feedback art polish, combat readability under heavy juice, and the broader all-juice design items.
- Reason: all current pure presentation VFX families now have explicit pools or bounded lists, soft/hard budgets, and under-load fade behavior, with a durable gate preventing regression.
