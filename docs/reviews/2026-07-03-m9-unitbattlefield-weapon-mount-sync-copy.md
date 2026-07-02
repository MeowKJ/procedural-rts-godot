# Review Record - M9 UnitBattlefield Weapon Mount Sync Copy

Step: #186 `[M9] Replace UnitBattlefield weapon mount sync LINQ`
Milestone: M9 - Elegance & Decoupling
Owner AI: Remote Linux Codex
Reviewer AI: ReviewGate regression / UnitBattlefieldRuntimeAllocationReviewGate
Integrator AI: Remote Linux Codex

Scope:
- Files/folders: `scripts/core/units/runtime/battlefield/UnitBattlefield.LegacyUtilities.cs`, `tools/ReviewGateRuntime/UnitBattlefieldRuntimeAllocationReviewGate.cs`.
- Non-goals: 不复用 returned component mount storage、不让 entity component alias `UnitInstance.WeaponMounts`、不改变 cooldown、targeting、attack cadence、mount facing、或 combat behavior。

Implementation summary:
- `WeaponMountsForEntity(...)` now allocates the required independent mount snapshot with an indexed array copy.
- The copy preserves `CooldownRemaining = unit.AttackCooldownRemaining` for every mount.
- `ReviewGate regression` locks the helper against returning to the LINQ `Select(...).ToArray()` projection.

Automated gates:
- Command: `dotnet build ProceduralRts.csproj --no-restore`
  Result: pass，0 warnings / 0 errors。
- Command: `dotnet run --project tools/SimReplay/SimReplay.csproj --no-restore`
  Result: pass，replay hashes remain deterministic。
- Command: `dotnet run --project tools/CombatBehavior/CombatBehavior.csproj --no-restore`
  Result: pass，weapon hit rules / turret states / economy / enemy AI / outcomes preserved。
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore -- regression --max-warnings=0`
  Result: pass，0 errors / 0 warnings。
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore -- filesize --max-warnings=0`
  Result: pass，0 errors / 0 warnings；ReviewGateRuntime suite remains at budget。
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore -- review --require-record=m9-unitbattlefield-weapon-mount-sync-copy`
  Result: pass，0 errors / 0 warnings。
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore`
  Result: pass，0 errors / 0 warnings。

Reviewer result:
- Status: pass.
- Required fixes: none currently known.

Residual risks:
- This slice intentionally keeps an independent returned mount snapshot; array allocation remains until a broader ownership-safe component storage design exists.
- Parent #10 remains open for broader allocation paydown.

TODO update:
- Items marked done: none.
- Items left open: broader M9 per-tick allocation paydown remains open under #10.
