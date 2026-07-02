# Review Record - M9 UnitBattlefield Unit Adoption Mount Buffers

Step: #187 `[M9] Reuse UnitBattlefield unit adoption mount buffers`
Milestone: M9 - Elegance & Decoupling
Owner AI: Remote Linux Codex
Reviewer AI: ReviewGate regression / UnitBattlefieldRuntimeAllocationReviewGate
Integrator AI: Remote Linux Codex

Scope:
- Files/folders: `scripts/core/units/runtime/battlefield/sync/UnitBattlefield.UnitEntityAdoption.cs`, `tools/ReviewGateRuntime/UnitBattlefieldRuntimeAllocationReviewGate.cs`, `TODO.md`.
- Non-goals: 不改变 weapon cooldown、mount facing、targeting、attack cadence、combat behavior、或 `UnitSpecEntityBridge` component construction。

Implementation summary:
- `AdoptUnitEntity(...)` now creates the `UnitInstance` first, then fills its owned `WeaponMounts` list.
- Existing `WeaponUserComponentState.Mounts` and default spec weapons are copied through explicit indexed loops.
- `ReviewGate regression` locks the adoption bridge against returning to `weapon.Mounts.ToList()` or `spec.Weapons.Select(...).ToList()`.

Automated gates:
- Command: `dotnet build ProceduralRts.csproj --no-restore`
  Result: pass，0 warnings / 0 errors。
- Command: `dotnet run --project tools/SimReplay/SimReplay.csproj --no-restore`
  Result: pass，SimReplay completed deterministic scenarios.
- Command: `dotnet run --project tools/CombatBehavior/CombatBehavior.csproj --no-restore`
  Result: pass，weapon hit rules / turret states / economy / enemy AI / outcomes preserved.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore -- regression --max-warnings=0`
  Result: pass，0 errors / 0 warnings。
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore -- filesize --max-warnings=0`
  Result: pass，0 errors / 0 warnings；ReviewGateRuntime suite remains under budget at 999 lines.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore -- review --require-record=m9-unitbattlefield-unit-adoption-mount-buffers`
  Result: pass，0 errors / 0 warnings。
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore`
  Result: pass，0 errors / 0 warnings。

Reviewer result:
- Status: pass.
- Required fixes: none currently known.

Residual risks:
- The adopted unit still owns a mutable `List<WeaponMountRuntimeState>`; this is required for legacy runtime mutation and intentionally does not alias entity component mount storage.
- Parent #10 remains open for broader allocation paydown.

TODO update:
- Items marked done: none.
- Items left open: broader M9 per-tick allocation paydown remains open under #10.
