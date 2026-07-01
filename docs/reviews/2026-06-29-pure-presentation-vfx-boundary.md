# Review Record - Pure presentation VFX boundary

Step: Prove pure presentation effects stay pooled/budgeted in view layers instead of becoming EntityWorld entities.
Milestone: Architecture hard boundaries.
Owner AI: Codex.
Reviewer AI: Codex self-review with static ReviewGate coverage.
Integrator AI: Codex.

Scope:
- Files/folders: `scripts/world/CombatEffectsLayer.cs`, `scripts/world/CommandAcknowledgementLayer.cs`, `scripts/world/FootprintLayer.cs`, `scripts/BattleRoot.cs`, `scripts/core/entities/EntityKind.cs`, `tools/ReviewGate/Program.cs`, `TODO.md`, `docs/reviews/2026-06-29-pure-presentation-vfx-boundary.md`.
- Non-goals: no full rewrite of gameplay projectiles/beams, no claim that future impact/projectile VFX families are fully pooled, no change to EntityKind reserve values.

Implementation summary:
- Extended `ReviewGate vfx` to check the architecture boundary, not only budgets.
- The gate now verifies combat VFX, command acknowledgement rings, and footprints are installed as `Node2D` presentation layers from `BattleRoot`.
- The gate verifies death effects and command rings use explicit pools, and footprints use a bounded presentation mark list with soft/hard budgets and load fade.
- The gate forbids runtime code from instantiating pure presentation effects as `EntityKind.Effect`; `EntityKind.Effect` remains only a reserved classification for future gameplay-affecting entities.
- Projectile tracers and hit flashes are drawn by `CombatEffectsLayer` from existing gameplay state rather than spawning presentation entities.

Automated gates:
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj vfx --no-restore`
  Result: pass
  Evidence: ReviewGate reported 0 errors and 0 warnings for VFX pooling/boundary checks.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore`
  Result: pass
  Evidence: all static review gates passed after extending `vfx`.

Manual/visual gates:
- Check: in-game visual QA
  Result: not run
  Evidence: this slice adds architectural/static coverage only; rendered VFX behavior was not changed.

Reviewer result:
- Status: pass
- Required fixes: the all-gate run was re-run sequentially after a parallel `ReviewGate` invocation caused a temporary DLL write lock.
- Residual risks: the broader performance TODO for future projectile/impact VFX families remains open; current projectile models are gameplay state, while their tracer rendering is presentation-only.

TODO update:
- Items marked done: `Pure-presentation effects (tracers, dust, flashes) are pooled, not full entities`.
- Items left open: `Pool combat VFX/footprints; cap concurrent effects; fade oldest under load`, `Death/impact VFX vary by weight class + domain + ammo`, and `All juice is pooled + capped + fade-oldest under load`.
- Reason: existing VFX families are presentation layers with pools/budgets, and a durable gate now rejects pure presentation effects being authored as `EntityKind.Effect` entities.
