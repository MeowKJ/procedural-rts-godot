Step: Make UnitBattlefieldBuildingTarget static fields spec-backed.
Milestone: M1 EntityWorld Becomes Authoritative
Owner AI: Codex
Reviewer AI: Codex review pass
Integrator AI: Codex

Scope:
- Files/folders: `scripts/core/units/runtime/UnitBattlefieldBuildingTarget.cs`, `scripts/core/units/runtime/UnitBattlefield.cs`, `tools/CombatBehavior/Program.cs`, `tools/ReviewGate/Program.cs`, `TODO.md`.
- Removed per-target storage for static building data: `MaxHp`, `Footprint`, `ArmorTag`, and `WeaponKind`.
- Those fields are now computed from `BuildSpecCatalog.For(Kind)`, while the target object keeps mutable migration state such as HP, selection, production queue, rally, dock, and pulses.
- Non-goals: no removal of `UnitBattlefieldBuildingTarget`, no public upsert signature deletion, no projection/event rewrite in this slice.

Automated gates:
- Command: `dotnet build ProceduralRts.csproj --no-restore`
  Result: pass
  Evidence: build completed successfully with 0 warnings and 0 errors.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj buildingtargetspecbackedstate --no-restore`
  Result: pass
  Evidence: dedicated spec-backed building target gate completed with 0 errors and 0 warnings.
- Command: `dotnet run --project tools/CombatBehavior/CombatBehavior.csproj --no-restore`
  Result: pass
  Evidence: CombatBehavior completed with the BuildSpec-backed target field assertions intact.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore`
  Result: pass
  Evidence: full ReviewGate completed with 0 errors and 0 warnings after the new gate and TODO update.
- Command: `dotnet run --project tools/VerifyAll/VerifyAll.csproj --no-restore`
  Result: pass
  Evidence: VerifyAll completed all 16 steps successfully, including SimReplay, CombatBehavior, PerfSmoke, BalanceReport, and Godot headless QA.

Reviewer result:
- Status: pass.
- Design note: this thins the second building runtime into a mutable compatibility shell and prevents it from becoming a second source of static building truth again.
- Required fixes: none identified before gates.

Status:
- Pass.

Residual risks:
- The target class still owns mutable migration fields and public events still expose it.
- The legacy upsert overload still accepts static building parameters for compatibility, but now explicitly ignores them.

TODO update:
- Marked done: nested M1 slice `UnitBattlefieldBuildingTarget spec-backed state cleanup`.
