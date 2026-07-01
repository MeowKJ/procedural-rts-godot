# Review Record - Signal Network ECS Capabilities

Step: Implement signal network behavior as EntityWorld components and a pure system.
Milestone: Power, Signal Network & Base Systems
Owner AI: Codex
Reviewer AI: Codex reviewer pass
Integrator AI: Codex

Scope:
- Files/folders: `scripts/core/entities/EntityComponentState.cs`, `scripts/core/entities/EntityStateHash.cs`, `scripts/core/sim/SimInvariants.cs`, `scripts/core/sim/systems/SignalNetworkSystem.cs`, `scripts/BattleRoot.cs`, `tools/SimReplay/Program.cs`, `tools/ReviewGate/Program.cs`, `TODO.md`.
- Non-goals: campaign repair/restart commands, live EnvironmentTone-to-ResourceAtmosphere wiring, visual offline/safe-zone states, signal alerts/audio.

Implementation summary:
- Added `SignalNetworkComponentState` as the authored signal-node data carried by an entity.
- Added pure `SignalNetworkSystem` that emits build radius, night vision, and safety resource-regeneration aura components only from completed, alive, powered signal nodes.
- Signal outputs are ordinary ECS components, so `ResourceSystem` and `VisionSystem` consume them without signal-specific branches.
- Added deterministic replay scenarios for day/fog-style control and night vision behavior, including unpowered cleanup.

Automated gates:
- Command: `dotnet build ProceduralRts.csproj --no-restore`
  Result: pass
  Evidence: build completed with 0 warnings and 0 errors during the slice.
- Command: `dotnet run --project tools/SimReplay/SimReplay.csproj --no-restore`
  Result: pass
  Evidence: `OK [signal-network]: day build radius 160, resource 50>30; night vision reveals target.` and `SimReplay PASSED.`
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj signalnetwork --no-restore`
  Result: pass
  Evidence: `Errors: 0`, `Warnings: 0`, `ReviewGate passed.`
- Command: `dotnet run --project tools/VerifyAll/VerifyAll.csproj --no-restore`
  Result: pass
  Evidence: all 14 steps passed, including build, SimReplay, CombatBehavior, PerfSmoke, BalanceReport, and Godot headless QA.

Manual/visual gates:
- Check: Visual QA
  Result: not applicable
  Evidence: this slice is deterministic simulation only; readable signal/offline presentation remains open TODO work.

Reviewer result:
- Status: pass
- Required fixes: none
- Residual risks: live battle still needs a driver that maps visual day/night and mission repair state into EntityWorld signal entities and `ResourceAtmosphere`. Current signal behavior is intentionally coarse and component-based.

TODO update:
- Items marked done: `EntityWorld signal network capabilities`.
- Items left open: `Signal network live integration`, low-power/offline readability, alerts/audio.
- Reason: headless replay proves the core signal capabilities, while mission/runtime/presentation integration is a separate surface and remains open.
