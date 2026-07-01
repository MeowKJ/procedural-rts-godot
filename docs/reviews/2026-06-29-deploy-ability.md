# Review Record - Deploy Ability Core

Step: Implement EntityWorld Deploy ability core.
Milestone: Abilities, Repair & Support Powers
Owner AI: Codex
Reviewer AI: Codex reviewer pass
Integrator AI: Codex

Scope:
- Files/folders: `scripts/core/entities/EntityComponentState.cs`, `scripts/core/entities/EntityStateHash.cs`, `scripts/core/sim/SimInvariants.cs`, `scripts/core/sim/systems/AbilitySystem.cs`, `scripts/core/sim/systems/CombatSystem.cs`, `scripts/core/sim/systems/MovementSystem.cs`, `tools/SimReplay/Program.cs`, `tools/ReviewGate/Program.cs`, `TODO.md`.
- Non-goals: deploy UI command buttons, playable roster authoring, deploy VFX/audio, per-unit balance tuning, target legality/cost framework, full WeaponSystem extraction.

Implementation summary:
- Added `DeployComponentState` with deterministic deployed/setup/range-multiplier state that is hashed and validated.
- `AbilitySystem` now handles `AbilityKind.Deploy`, ticks setup time, clears movement on deploy, applies deterministic cooldown, and lets toggle-off bypass cooldown for responsive undeploy.
- `CombatSystem` blocks firing during deploy setup, applies deploy range only after setup completes, and avoids writing chase targets to entities that cannot actually move.
- `MovementSystem` holds deployed entities still and clears their movement target while deployed.
- Added deterministic replay coverage for setup blocking fire, deployed range extension, undeploy removing the range firing window, and held movement.

Automated gates:
- Command: `dotnet build ProceduralRts.csproj --no-restore`
  Result: pass
  Evidence: build completed with 0 warnings and 0 errors.
- Command: `dotnet run --project tools/SimReplay/SimReplay.csproj --no-restore`
  Result: pass
  Evidence: `OK [deploy]: setup shots 0, deployed shots 2, undeployed shots 0, target hp 190.2.` and `SimReplay PASSED.`
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj deployability --no-restore`
  Result: pass
  Evidence: `Errors: 0`, `Warnings: 0`, `ReviewGate passed.`
- Command: `dotnet run --project tools/VerifyAll/VerifyAll.csproj --no-restore`
  Result: pass
  Evidence: all steps passed after this slice.

Manual/visual gates:
- Check: Visual QA
  Result: not applicable
  Evidence: this slice is deterministic simulation behavior only; deploy presentation and UI command surfaces remain open TODO work.

Reviewer result:
- Status: pass
- Required fixes: none
- Residual risks: Deploy is not yet exposed through playable unit rosters, HUD ability buttons, authored unit tuning, VFX/audio, AI planner behavior, or target/cost legality rules. Deploy currently uses `AbilitySpec.Radius` as setup seconds and `AbilitySpec.Value` as range multiplier until the full ability targeting/value schema is split.

TODO update:
- Items marked done: `EntityWorld Deploy ability core`.
- Items left open: full active ability framework, support fields expansion, playable roster/UI wiring, target legality, per-unit tuning, and richer support-power presentation.
- Reason: replay and ReviewGate prove the bounded Deploy command/setup/range/movement path; adjacent content, UI, presentation, and full framework work remains separate.
