# Review Record - M9 projectile projection buffer gate

Step: Lock projectile projection count and buffer reuse in ReviewGate simhot
Milestone: M9 Elegance, Decoupling, Performance
Owner AI: Remote Linux Codex
Reviewer AI: ReviewGate simhot
Integrator AI: Remote Linux Codex

Scope:
- Issue: #63.
- Files/folders: `tools/ReviewGateDomains/RegressionReviewGate.cs`, `TODO.md`, `docs/reviews/2026-07-02-m9-projectile-projection-buffer-gate.md`.
- Evidence-only source surfaces: `scripts/world/CombatEffectsLayer.cs`, `scripts/world/CombatEffectsLayer.CombatDraw.cs`, `scripts/core/sim/ProjectilePresentationProjection.cs`, `scripts/core/units/runtime/battlefield/UnitBattlefield.ProjectileProjection.cs`.
- Non-goals: changing projectile visuals, damage, speed, tracking, hit rules, batching, projection caching, balance values, or adding a one-off narrow gate.

Implementation summary:
- Extended the existing broad regression gate behind `ReviewGate simhot` with projectile projection allocation evidence.
- The gate now requires `CombatEffectsLayer.ActiveEffectCount` to use `ProjectileProjectionCount()` instead of constructing projection lists for counting.
- The gate now requires `CombatEffectsLayer.DrawProjectiles()` to fill the reusable `_projectileProjections` buffer and forbids calling the no-argument allocating `ProjectileProjections()` API from that draw path.
- The gate now requires `ProjectilePresentationProjector.ProjectInto(...)`, `ProjectilePresentationProjector.Count(...)`, and the `UnitBattlefield` buffer-fill facade to remain available.

Automated gates:
- Command: `dotnet build ProceduralRts.csproj --no-restore`
  Result: pass
  Evidence: main project build completed with 0 warnings and 0 errors.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore -- simhot --max-warnings=0`
  Result: pass
  Evidence: ReviewGate completed with 0 errors and 0 warnings.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore`
  Result: pass
  Evidence: full ReviewGate completed with 0 errors and 1 existing source-directory warning for `scripts/core/sim/`.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore -- review --require-record=m9-projectile-projection-buffer-gate`
  Result: pass
  Evidence: review-record gate completed with 0 errors and 0 warnings.
- Command: `dotnet run --project tools/VerifyAll/VerifyAll.csproj --no-restore`
  Result: pass
  Evidence: VerifyAll completed all 23 steps successfully, including build, SimReplay, CombatBehavior, ReviewGate, PerfSmoke, balance/counter QA, and Godot headless QA.

Reviewer result:
- Status: pass.
- Required fixes: none.
- Residual risks: this is a static regression guard for the presentation projection path; it does not reduce the remaining Construction/Command allocation debt or replace profiler-guided GC work.

TODO update:
- Items marked done: none.
- Items left open: `Per-tick allocation paydown`.
- Reason: #63 locks the projectile projection allocation fix from #61/#62, but the broader M9 allocation paydown still has remaining hot-path debt.
