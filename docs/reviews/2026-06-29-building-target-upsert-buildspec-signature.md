Step: Remove duplicated static-data arguments from building target upsert.
Milestone: M1 EntityWorld Becomes Authoritative
Owner AI: Codex
Reviewer AI: Codex review pass
Integrator AI: Codex

Scope:
- Files/folders: `scripts/core/units/runtime/UnitBattlefield.cs`, `tools/CombatBehavior/Program.cs`, `tools/AiDifficultySmoke/Program.cs`, `tools/ReviewGate/Program.cs`, `TODO.md`.
- Removed the legacy `UpsertBuildingTarget` overload that accepted `MaxHp`, `Footprint`, `ArmorTag`, and `WeaponKind`.
- Updated tool/test callers to use the single BuildSpec-backed entrypoint and read full-health seed values from `BuildSpecCatalog` where needed.
- Non-goals: no removal of `UnitBattlefieldBuildingTarget` itself, no event signature migration, no presentation projection rewrite.

Automated gates:
- Command: `dotnet build ProceduralRts.csproj --no-restore`
  Result: pass
  Evidence: build completed successfully with 0 warnings and 0 errors.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj buildingtargetupsertspec --no-restore`
  Result: pass
  Evidence: dedicated BuildSpec upsert signature gate completed with 0 errors and 0 warnings.
- Command: `dotnet run --project tools/CombatBehavior/CombatBehavior.csproj --no-restore`
  Result: pass
  Evidence: CombatBehavior completed after compiling all updated building-target call sites.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore`
  Result: pass
  Evidence: full ReviewGate completed with 0 errors and 0 warnings after updating older BuildSpec live bridge expectations.
- Command: `dotnet run --project tools/VerifyAll/VerifyAll.csproj --no-restore`
  Result: pass
  Evidence: VerifyAll completed all 16 steps successfully, including CombatBehavior, AiDifficultySmoke, FogOfWarQa, and Godot headless QA.

Reviewer result:
- Status: pass.
- Design note: this removes the last public API path that could reintroduce per-target static building truth.
- Required fixes: none identified before gates.

Status:
- Pass.

Residual risks:
- `UnitBattlefieldBuildingTarget` remains as a mutable migration wrapper around EntityWorld projections.
- Some tests still use partial current HP values for scenario setup; max HP now always comes from `BuildSpecCatalog`.

TODO update:
- Marked done: nested M1 slice `UnitBattlefield BuildSpec upsert signature cleanup`.
