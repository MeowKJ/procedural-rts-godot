# Review Record - Targeted Repair Over Time

Step: Implement EntityWorld targeted repair-over-time core.
Milestone: Abilities, Repair & Support Powers
Owner AI: Codex
Reviewer AI: Codex reviewer pass
Integrator AI: Codex

Scope:
- Files/folders: `scripts/core/entities/EntityCommand.cs`, `scripts/core/entities/EntityComponentState.cs`, `scripts/core/entities/EntityStateHash.cs`, `scripts/core/sim/SimInvariants.cs`, `scripts/core/sim/systems/CommandSystem.cs`, `scripts/core/sim/systems/RepairSystem.cs`, `scripts/BattleRoot.cs`, `tools/SimReplay/Program.cs`, `tools/ReviewGate/Program.cs`, `TODO.md`.
- Non-goals: smart-right-click UI, repair cursor/feedback, capture/restart objective structures, ConstructionSystem integration, per-unit repair economy schema, audio/VFX.

Implementation summary:
- Added `RepairEntityCommand` and `RepairOrderComponentState` as deterministic command/state for targeted repair work.
- `CommandSystem` now translates repair commands into repair orders only for owned units with authored `RepairField` ability data and friendly damaged targets.
- Added `RepairSystem`, which moves repairers into range, stops them while repairing, spends owner Credits, and restores HP over time in deterministic HP chunks.
- Repair orders are hashed and validated by `EntityStateHash` and `SimInvariants`.
- `BattleRoot` now runs `RepairSystem` in the EntityWorld pipeline after ability handling and before combat/movement.

Automated gates:
- Command: `dotnet build ProceduralRts.csproj --no-restore`
  Result: pass
  Evidence: build completed with 0 warnings and 0 errors.
- Command: `dotnet run --project tools/SimReplay/SimReplay.csproj --no-restore`
  Result: pass
  Evidence: `OK [targeted-repair]: ally hp 58, enemy hp 40, credits 0, moved True.` and `SimReplay PASSED.`
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj targetedrepair --no-restore`
  Result: pass
  Evidence: `Errors: 0`, `Warnings: 0`, `ReviewGate passed.`
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore`
  Result: pass
  Evidence: `Errors: 0`, `Warnings: 0`, `ReviewGate passed.`
- Command: `dotnet run --project tools/VerifyAll/VerifyAll.csproj --no-restore`
  Result: pass
  Evidence: all steps passed after this slice.

Manual/visual gates:
- Check: Visual QA
  Result: not applicable
  Evidence: this slice is deterministic simulation behavior only; repair command UI and feedback remain open.

Reviewer result:
- Status: pass
- Required fixes: none
- Residual risks: Repair cost is currently the default 1 credit per HP on `RepairOrderComponentState`; richer per-unit repair economy belongs in a later ability/repair tuning schema. Capture/restart objective structures and smart-right-click wiring remain open.

TODO update:
- Items marked done: `EntityWorld targeted repair-over-time core`.
- Items left open: Engineer/repair expansion remaining, including capture/restart objective structures, UI/smart-right-click wiring, feedback, and richer tuning.
- Reason: replay and ReviewGate prove the bounded targeted repair command/order/system path; adjacent construction, UI, and presentation work remains separate.
