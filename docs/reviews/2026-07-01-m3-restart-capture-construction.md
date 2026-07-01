# Review Record - M3 restart/capture construction

Step: M3 shared restart/capture backend
Milestone: M3 Build & Construction System
Owner AI: Codex
Reviewer AI: ReviewGate m3restartcaptureconstruction / SimReplay
Integrator AI: Codex

Scope:
- Files/folders: `scripts/core/entities/EntityInstance.cs`, `scripts/core/entities/EntityWorld.cs`, `scripts/core/entities/EntityComponentState.cs`, `scripts/core/sim/systems/RepairSystem.cs`, `scripts/core/sim/systems/command/CommandSystem.EconomyOrders.cs`, `scripts/core/sim/systems/construction/ConstructionSystem.State.cs`, `tools/SimReplay/Economy/ConstructionRestartCaptureScenarios.cs`, `tools/SimReplay/Core/ReplayPrelude.cs`, `TODO.md`.
- Non-goals: campaign objective graph UI, mission scripting, hostile building capture, or visual repair feedback.

Implementation summary:
- Adds `ConstructionPhase.RestartCapture` to represent pre-existing objective structures that should not auto-build.
- Keeps `ConstructionSystem` from advancing restart/capture objectives automatically.
- Adds `EntityWorld.ChangeOwner` so capture is an authoritative owner mutation, not a side marker.
- Extends `RepairEntityCommand` handling to accept neutral restartable objectives only when the subject has authored repair ability data.
- Extends `RepairSystem` so repair work can restore health and also advance restart/capture construction progress using repair-equivalent credit spend; completion switches the objective back to normal `Building` phase.
- Adds deterministic replay coverage proving a neutral signal objective stays inactive without repair, is captured by a repair command, completes construction, and then emits its normal signal build radius.

Automated gates:
- Command: `dotnet build ProceduralRts.csproj --no-restore`
  Result: pass
  Evidence: main project build completed with 0 warnings and 0 errors.
- Command: `dotnet build tools/SimReplay/SimReplay.csproj --no-restore`
  Result: pass
  Evidence: SimReplay project build completed with 0 warnings and 0 errors.
- Command: `dotnet run --project tools/SimReplay/SimReplay.csproj --no-restore`
  Result: pass
  Evidence: `restart-capture-construction` completed deterministically; output showed owner 1, radius 180, credits 200.
- Command: `dotnet run --project tools/CombatBehavior/CombatBehavior.csproj --no-restore`
  Result: pass
  Evidence: combat behavior completed successfully after RepairSystem learned restart/capture construction progress.
- Command: `dotnet run --project tools/PlayerLoopQa/PlayerLoopQa.csproj --no-restore`
  Result: pass
  Evidence: player-loop QA passed, including targeted repair and construction handoff coverage.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore -- m3restartcaptureconstruction`
  Result: pass
  Evidence: historical narrow mode routed through the content gate and passed.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore -- review --require-record=m3-restart-capture-construction`
  Result: pass
  Evidence: review record gate found this record and passed.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore -- filesize`
  Result: pass
  Evidence: file-size gate completed with 0 errors and 0 warnings.

Manual/visual gates:
- Check: file-size governance
  Result: pass
  Evidence: touched implementation files remain below 200 lines except existing normal-band files; no C# file crosses the 400-line normal ceiling.

Reviewer result:
- Status: pass
- Required fixes: none known.
- Residual risks: player-facing mission/HUD affordances and visual feedback remain separate TODOs.

TODO update:
- Items marked done: shared restart/capture backend under faction-distinct construction methods.
- Items left open: destroyed-state construction lifecycle and player-facing Dog construction UX/HUD handoff.
- Reason: restart/capture now uses the same Construction component plus existing repair commands/systems rather than a campaign-only fork.
