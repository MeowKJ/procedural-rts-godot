# Review Record - BuildSpec Live Upsert Bridge

Step: Route live BattleRoot building mirroring through a BuildSpec-derived UnitBattlefield upsert overload.
Milestone: M1 EntityWorld Becomes Authoritative
Owner AI: Codex
Reviewer AI: Codex reviewer pass
Integrator AI: Codex

Scope:
- Files/folders: `scripts/BattleRoot.cs`, `scripts/core/units/runtime/UnitBattlefield.cs`, `tools/CombatBehavior/Program.cs`, `tools/ReviewGate/Program.cs`, `TODO.md`.
- Non-goals: deleting the old long-parameter `UpsertBuildingTarget` overload, deleting `BuildingModel`, deleting `UnitBattlefieldBuildingTarget`.

Implementation summary:
- Added a BuildSpec-derived `UnitBattlefield.UpsertBuildingTarget` overload that reads MaxHp, footprint, armor, and weapon binding from `BuildSpecCatalog`.
- Updated `BattleRoot.UpsertBuildingTarget(BuildingModel)` to call the BuildSpec-derived overload instead of manually forwarding `_state.Definition(building)` fields.
- `CombatBehavior` proves the overload derives runtime target shape plus EntityWorld construction/power components from `BuildSpecCatalog`.
- `ReviewGate buildspeclivebridge` locks the live bridge so BattleRoot does not regress to hand-merging building definition fields.

Automated gates:
- Command: `dotnet build ProceduralRts.csproj --no-restore`
  Result: pass
  Evidence: build completed with 0 warnings and 0 errors.
- Command: `dotnet run --project tools/CombatBehavior/CombatBehavior.csproj --no-restore`
  Result: pass
  Evidence: Combat behavior passed with BuildSpec upsert overload assertions.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj buildspeclivebridge --no-restore`
  Result: pass
  Evidence: `Errors: 0`, `Warnings: 0`, `ReviewGate passed.`
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj review --require-record=buildspec-live-upsert-bridge --no-restore`
  Result: pass
  Evidence: `Errors: 0`, `Warnings: 0`, `ReviewGate passed.`
- Command: `dotnet run --project tools/VerifyAll/VerifyAll.csproj --no-restore`
  Result: pass
  Evidence: all 14 steps passed after this slice.

Manual/visual gates:
- Check: Visual QA
  Result: not run
  Evidence: this slice changes building mirror plumbing only; existing views still consume the same runtime fields.

Reviewer result:
- Status: pass
- Required fixes: none
- Residual risks: The long compatibility overload still exists for tests and transitional callers. Final cleanup still requires removing the second building runtime.

TODO update:
- Items marked done: nested M1 slice `BattleRoot BuildSpec building upsert bridge`.
- Items left open: parent migration cleanup, `UnitBattlefieldBuildingTarget` removal, construction/build placement migration, legacy catalog deletion.
- Reason: tests and ReviewGate prove the live BattleRoot bridge now uses BuildSpec defaults without claiming the whole building runtime migration is complete.
