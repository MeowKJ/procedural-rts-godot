# Review Record - Ability Cost And Target Legality

Step: Implement EntityWorld ability cost and target legality core.
Milestone: Abilities, Repair & Support Powers
Owner AI: Codex
Reviewer AI: Codex reviewer pass
Integrator AI: Codex

Scope:
- Files/folders: `scripts/core/UnitSpec.cs`, `scripts/core/sim/systems/AbilitySystem.cs`, `tools/SimReplay/Program.cs`, `tools/ReviewGate/Program.cs`, `TODO.md`.
- Non-goals: ability UI buttons, AI planner casting, Build ability, charge/ammo systems, minimap pings, presentation/audio for failed casts.

Implementation summary:
- Added `AbilityTargetRule` and optional `AbilitySpec.Cost`/`TargetRule` data with defaults, preserving existing unit authoring calls.
- `AbilitySystem` now validates target legality and deterministic owner credits before applying an ability, then spends the cost only after a successful effect.
- Default target rules map Deploy to self, Scan to point, and RepairField/ShieldField to friendly point-or-entity support targeting.
- RepairField and ShieldField now use `OwnerRelationTable` friendly relations (`Self`/`Allied`) instead of same-owner checks.
- Added deterministic replay coverage for illegal hostile support targets, one successful paid repair, cooldown-gated repeat casts, insufficient-credit rejection, and stable replay hashes.

Automated gates:
- Command: `dotnet build ProceduralRts.csproj --no-restore`
  Result: pass
  Evidence: build completed with 0 warnings and 0 errors.
- Command: `dotnet run --project tools/SimReplay/SimReplay.csproj --no-restore`
  Result: pass
  Evidence: `OK [ability-legality]: ally hp 60, enemy hp 40, credits 5, cooldown 0.00s.` and `SimReplay PASSED.`
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj abilitylegality --no-restore`
  Result: pass
  Evidence: `Errors: 0`, `Warnings: 0`, `ReviewGate passed.`
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj repairfieldability --no-restore`
  Result: pass
  Evidence: `Errors: 0`, `Warnings: 0`, `ReviewGate passed.`
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj shieldfieldability --no-restore`
  Result: pass
  Evidence: `Errors: 0`, `Warnings: 0`, `ReviewGate passed.`
- Command: `dotnet run --project tools/VerifyAll/VerifyAll.csproj --no-restore`
  Result: pass
  Evidence: all steps passed after this slice.

Manual/visual gates:
- Check: Visual QA
  Result: not applicable
  Evidence: this slice is deterministic simulation behavior only; UI/presentation for ability failures remains open.

Reviewer result:
- Status: pass
- Required fixes: none
- Residual risks: `AbilitySpec.Radius`/`Value` are still overloaded per ability kind until the full ability schema is split. Build ability, AI casting, UI command affordances, and richer failed-cast feedback remain open.

TODO update:
- Items marked done: `Ability cost and target legality core`, `Deterministic ability tests in SimReplay`.
- Items left open: full active ability framework, Engineer/repair expansion, support field playable roster/UI wiring, Build ability, and future ability schema work.
- Reason: replay and ReviewGate prove the bounded cost/target legality behavior for current active abilities; adjacent UI, AI, Build, and schema expansion work remains separate.
