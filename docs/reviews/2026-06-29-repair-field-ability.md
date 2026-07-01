# Review Record - RepairField Ability Core

Step: Implement EntityWorld RepairField ability core.
Milestone: Abilities, Repair & Support Powers
Owner AI: Codex
Reviewer AI: Codex reviewer pass
Integrator AI: Codex

Scope:
- Files/folders: `scripts/core/entities/EntityCommand.cs`, `scripts/core/entities/EntityComponentState.cs`, `scripts/core/entities/UnitSpecEntityBridge.cs`, `scripts/core/entities/EntityStateHash.cs`, `scripts/core/sim/SimInvariants.cs`, `scripts/core/sim/systems/AbilitySystem.cs`, `scripts/BattleRoot.cs`, `tools/SimReplay/Program.cs`, `tools/ReviewGate/Program.cs`, `TODO.md`.
- Non-goals: shield/scan/deploy/build abilities, ability UI, audio/VFX, over-time credit repair, capture/restart objective structures.

Implementation summary:
- Added `AbilityEntityCommand` so active ability intent can flow through the command buffer.
- Added `AbilityRuntimeComponentState` and cooldown hashing/invariant validation.
- `UnitSpecEntityBridge` now attaches ability runtime state from authored `UnitSpec.Abilities`.
- Added pure `AbilitySystem` with deterministic cooldown ticking and `RepairField` effect application.
- `RepairField` heals damaged friendly entities within the target radius, ignores hostiles and out-of-range allies, and sets a cooldown after a successful cast.

Automated gates:
- Command: `dotnet build ProceduralRts.csproj --no-restore`
  Result: pass
  Evidence: build completed with 0 warnings and 0 errors during the slice.
- Command: `dotnet run --project tools/SimReplay/SimReplay.csproj --no-restore`
  Result: pass
  Evidence: `OK [repair-field]: ally hp 82, enemy hp 40, far ally hp 50, cooldown 0.67s.` and `SimReplay PASSED.`
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj repairfieldability --no-restore`
  Result: pass
  Evidence: `Errors: 0`, `Warnings: 0`, `ReviewGate passed.`
- Command: `dotnet run --project tools/VerifyAll/VerifyAll.csproj --no-restore`
  Result: pass
  Evidence: all 14 steps passed, including build, SimReplay, CombatBehavior, PerfSmoke, BalanceReport, and Godot headless QA.

Manual/visual gates:
- Check: Visual QA
  Result: not applicable
  Evidence: this slice is deterministic simulation only; ability VFX/UI remain open TODO work.

Reviewer result:
- Status: pass
- Required fixes: none
- Residual risks: the broader ability framework is still incomplete. Repair currently applies as an instant field heal with no credit cost or over-time channeling.

TODO update:
- Items marked done: `EntityWorld RepairField ability core`.
- Items left open: full active ability framework, repair expansion, support fields, ability UI/VFX/audio.
- Reason: replay and ReviewGate prove the bounded RepairField command/cooldown/effect path; adjacent abilities and richer repair semantics remain separate work.
