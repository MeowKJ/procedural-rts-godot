# Review Record - Power Consequences

Step: Implement owner-level power constraints with production and turret consequences.
Milestone: Power, Signal Network & Base Systems
Owner AI: Codex
Reviewer AI: Codex reviewer pass
Integrator AI: Codex

Scope:
- Files/folders: `scripts/core/sim/systems/PowerSystem.cs`, `scripts/core/sim/systems/CombatSystem.cs`, `scripts/BattleRoot.cs`, `tools/SimReplay/Program.cs`, `tools/ReviewGate/Program.cs`, `TODO.md`.
- Non-goals: signal-network gameplay, low-power/offline art states, alerts/audio, priority-based brownouts, sell/repair/base teardown.

Implementation summary:
- Added a pure deterministic `PowerSystem` that totals active provided/used power per owner and updates `PowerComponentState.Powered`.
- `CombatSystem` now skips target acquisition and firing for unpowered weapon users, so defense turret entities go offline under low power.
- Existing `ProductionSystem` unpowered pause behavior is now driven by aggregate owner power in the EntityWorld pipeline.
- Added deterministic replay coverage for sufficient-power and low-power worlds, including turret shots and producer pause.

Automated gates:
- Command: `dotnet build ProceduralRts.csproj --no-restore`
  Result: pass
  Evidence: build completed with 0 warnings and 0 errors.
- Command: `dotnet run --project tools/SimReplay/SimReplay.csproj --no-restore`
  Result: pass
  Evidence: `OK [power-consequences]: powered shots 4, offline shots 0, low-power production paused.` and `SimReplay PASSED.`
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj powerconsequences --no-restore`
  Result: pass
  Evidence: `Errors: 0`, `Warnings: 0`, `ReviewGate passed.`
- Command: `dotnet run --project tools/VerifyAll/VerifyAll.csproj --no-restore`
  Result: pass
  Evidence: all 14 steps passed, including build, SimReplay, CombatBehavior, PerfSmoke, BalanceReport, and Godot headless QA.

Manual/visual gates:
- Check: Visual QA
  Result: not applicable
  Evidence: this slice changes deterministic simulation behavior only; low-power/offline art remains open TODO work.

Reviewer result:
- Status: pass
- Required fixes: none
- Residual risks: brownout priority and low-rate production are still coarse; current rule is all-or-nothing per owner when total demand exceeds supply. Presentation still needs readable low-power/offline building states.

TODO update:
- Items marked done: `Power as a constraint with consequences`.
- Items left open: signal network, base teardown/sell/repair, low-power/offline/damaged building readability, alerts/audio low-power notification.
- Reason: replay and ReviewGate prove owner total supply/demand, production pause, and turret offline consequences; adjacent presentation and signal systems are separate unimplemented surfaces.
