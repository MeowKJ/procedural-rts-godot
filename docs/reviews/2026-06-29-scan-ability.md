# Review Record - Scan Ability Core

Step: Implement EntityWorld Scan ability core.
Milestone: Abilities, Repair & Support Powers
Owner AI: Codex
Reviewer AI: Codex reviewer pass
Integrator AI: Codex

Scope:
- Files/folders: `scripts/core/entities/EntityComponentState.cs`, `scripts/core/entities/EntityStateHash.cs`, `scripts/core/sim/SimInvariants.cs`, `scripts/core/sim/systems/AbilitySystem.cs`, `scripts/core/sim/systems/VisionSystem.cs`, `tools/SimReplay/Program.cs`, `tools/ReviewGate/Program.cs`, `TODO.md`.
- Non-goals: scan UI/VFX/audio, playable scanner unit authoring, scan costs, minimap ping presentation, AI planner usage.

Implementation summary:
- Added `ScanRevealComponentState` with deterministic radius and duration state.
- `AbilitySystem` now handles `AbilityKind.Scan` by spawning short-lived gameplay reveal marker entities and removing them on expiry.
- `VisionSystem` consumes scan reveal components as temporary viewers, keeping gameplay visibility and visual fog integration on the same visibility source.
- Added deterministic replay coverage for scan reveal, radius gating, expiry, and effect cleanup.

Automated gates:
- Command: `dotnet build ProceduralRts.csproj --no-restore`
  Result: pass
  Evidence: build completed with 0 warnings and 0 errors during the slice.
- Command: `dotnet run --project tools/SimReplay/SimReplay.csproj --no-restore`
  Result: pass
  Evidence: `OK [scan]: hostile visible during scan, far hostile hidden, expired effects 0, cooldown 0.70s.` and `SimReplay PASSED.`
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj scanability --no-restore`
  Result: pass
  Evidence: `Errors: 0`, `Warnings: 0`, `ReviewGate passed.`
- Command: `dotnet run --project tools/VerifyAll/VerifyAll.csproj --no-restore`
  Result: pass
  Evidence: all 14 steps passed, including build, SimReplay, CombatBehavior, PerfSmoke, BalanceReport, and Godot headless QA.

Manual/visual gates:
- Check: Visual QA
  Result: not applicable
  Evidence: this slice is deterministic simulation/visibility only; scan presentation remains open TODO work.

Reviewer result:
- Status: pass
- Required fixes: none
- Residual risks: Scan is not yet exposed through a playable unit, HUD command, minimap ping, or AI planner. It has fixed system cooldown behavior and no resource cost.

TODO update:
- Items marked done: `EntityWorld Scan ability core`.
- Items left open: full active ability framework, support fields expansion, playable roster/UI wiring, richer support-power presentation.
- Reason: replay and ReviewGate prove the bounded Scan command/effect/visibility path; adjacent authoring and presentation work remains separate.
