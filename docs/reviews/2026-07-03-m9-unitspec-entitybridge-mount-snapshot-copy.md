# Review Record - M9 UnitSpec Entity Bridge Mount Snapshot Copy

Step: #189 `[M9] Replace UnitSpecEntityBridge mount snapshot LINQ`
Milestone: M9 - Elegance & Decoupling
Owner AI: Remote Linux Codex
Reviewer AI: ReviewGate regression / UnitSpecAbilityAllocationReviewGate
Integrator AI: Remote Linux Codex

Scope:
- Files/folders: `scripts/core/entities/UnitSpecEntityBridge.cs`, `tools/ReviewGateRuntime/UnitSpecAbilityAllocationReviewGate.cs`, `TODO.md`.
- Non-goals: 不改变 `EntitySpec` authoring metadata、tags、abilities、stance/autonomy、weapon definitions、combat behavior、或 component mount array ownership。

Implementation summary:
- `InitialUnitComponents(...)` now creates `WeaponUserComponentState` through `CreateWeaponMountStates(...)`.
- The helper allocates the required independent mount array and fills it with an indexed loop.
- `ReviewGate regression` locks the entity bridge against returning to the LINQ `Select(...).ToArray()` projection.

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
  Result: pass，0 errors / 0 warnings；ReviewGateRuntime suite remains under budget at 996 lines.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore -- review --require-record=m9-unitspec-entitybridge-mount-snapshot-copy`
  Result: pass，0 errors / 0 warnings。
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore`
  Result: pass，0 errors / 0 warnings。

Reviewer result:
- Status: pass.
- Required fixes: none currently known.

Residual risks:
- The component mount array allocation remains intentional as the immutable component snapshot ownership boundary.
- Parent #10 remains open for broader allocation paydown.

TODO update:
- Items marked done: none.
- Items left open: broader M9 per-tick allocation paydown remains open under #10.
