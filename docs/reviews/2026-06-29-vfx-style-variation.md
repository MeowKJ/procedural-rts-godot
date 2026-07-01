# Review Record - VFX style variation

Step: Finish death/impact VFX variation by target weight class, movement domain, and ammo kind.
Milestone: Art and style - combat feedback.
Owner AI: Codex.
Reviewer AI: Codex self-review with CombatBehavior and ReviewGate coverage.
Integrator AI: Codex.

Scope:
- Files/folders: `scripts/core/ImpactVfxStyle.cs`, `scripts/core/ImpactVfxMath.cs`, `scripts/world/CombatEffectsLayer.cs`, `scripts/BattleRoot.cs`, `tools/CombatBehavior/Program.cs`, `tools/ReviewGate/Program.cs`, `TODO.md`, `docs/reviews/2026-06-29-vfx-style-variation.md`.
- Non-goals: no full art-direction pass, no screenshot tuning, no projectile gameplay rewrite, no new entity VFX type.

Implementation summary:
- Added `ImpactVfxStyle` and `ImpactVfxMath.StyleFor(weight, domain, ammo, damage)` so impact variation is testable core data instead of hidden private drawing constants.
- `CombatEffectsLayer` now draws impact flashes from the style math, including weight/domain/ammo-scaled expansion, line width, spark count/scale, ember hooks, and EMP/ion dissolve arcs.
- `BattleRoot` now passes live target `UnitWeightClass`, `MovementDomain`, damage, and ammo into pooled impact flashes for UnitSpec runtime hits; buildings use heavy land impact styling.
- Kept all impact flashes in the existing pooled/budgeted presentation layer.
- Extended `CombatBehavior` and `ReviewGate vfx` to prove death/impact variation hooks and prevent regression.

Automated gates:
- Command: `dotnet build ProceduralRts.csproj --no-restore`
  Result: pass
  Evidence: build completed with 0 warnings and 0 errors.
- Command: `dotnet run --project tools/CombatBehavior/CombatBehavior.csproj --no-restore`
  Result: pass
  Evidence: CombatBehavior passed, including impact VFX weight/ammo/domain assertions.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj vfx --no-restore`
  Result: pass
  Evidence: ReviewGate reported 0 errors and 0 warnings for VFX variation and pooling checks.

Manual/visual gates:
- Check: rendered VFX art tuning
  Result: not run
  Evidence: this slice adds data variation and regression gates; final look tuning remains part of later combat-juice polish.

Reviewer result:
- Status: pass
- Required fixes: none.
- Residual risks: screenshots/playtests may still tune exact colors, radii, and alpha values; future VFX families must reuse the same data-driven, pooled pattern.

TODO update:
- Items marked done: `Death/impact VFX vary by weight class + domain + ammo (flash ring, fragments, smoke, EMP dissolve); pooled, capped, fade-oldest under load`.
- Items left open: hit-feedback polish, heavy-impact feel, combat readability, and broader juice design items.
- Reason: both death and impact VFX now use explicit style math covering weight/domain/ammo and are verified through CombatBehavior plus ReviewGate.
