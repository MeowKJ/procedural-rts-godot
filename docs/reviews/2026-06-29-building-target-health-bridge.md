# Review Record - Building Target Health Bridge

Step: Route UnitBattlefield building target damage and death through EntityWorld health mirrors.
Milestone: M1 EntityWorld Becomes Authoritative
Owner AI: Codex
Reviewer AI: Codex reviewer pass
Integrator AI: Codex

Scope:
- Files/folders: `scripts/core/units/runtime/UnitBattlefield.cs`, `tools/CombatBehavior/Program.cs`, `tools/ReviewGate/Program.cs`, `TODO.md`.
- Non-goals: replacing the full legacy combat loop with `CombatSystem`, moving building weapons to pure sim authority, deleting `UnitBattlefieldBuildingTarget`, construction/build placement migration.

Implementation summary:
- Unit-vs-building damage now calls `ApplyBuildingDamageToEntity`, which updates the building mirror's `HealthComponentState` and syncs HP back to the legacy target.
- Destroyed building targets now remove their EntityWorld mirror through `RemoveBuildingEntity` before legacy death events and outcome resolution continue.
- `CombatBehavior` proves a destroyed HQ is removed from both the legacy building list and EntityWorld mirror map while death and victory events still fire.
- `ReviewGate buildingtargetbridge` locks the bridge contract and is included in the global gate.

Automated gates:
- Command: `dotnet build ProceduralRts.csproj --no-restore`
  Result: pass
  Evidence: build completed with 0 warnings and 0 errors.
- Command: `dotnet run --project tools/CombatBehavior/CombatBehavior.csproj --no-restore`
  Result: pass
  Evidence: Combat behavior passed with assertions proving building target damage/death mirroring.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj buildingtargetbridge --no-restore`
  Result: pass
  Evidence: `Errors: 0`, `Warnings: 0`, `ReviewGate passed.`
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj review --require-record=building-target-health-bridge --no-restore`
  Result: pass
  Evidence: `Errors: 0`, `Warnings: 0`, `ReviewGate passed.`
- Command: `dotnet run --project tools/VerifyAll/VerifyAll.csproj --no-restore`
  Result: pass
  Evidence: all 14 steps passed after this slice.

Manual/visual gates:
- Check: Visual QA
  Result: not run
  Evidence: existing building hit/death events remain preserved; headless gates cover runtime stability.

Reviewer result:
- Status: pass
- Required fixes: none
- Residual risks: The legacy combat loop still decides when shots happen. This slice moves building HP/death mirroring only; full combat authority remains a later M1/M5 migration.

TODO update:
- Items marked done: nested M1 slice `UnitBattlefield building target health/death EntityWorld bridge`.
- Items left open: parent harvester/production/building migration item, building target cleanup, construction migration, legacy behavior deletion.
- Reason: tests and ReviewGate prove building target HP/death mirroring without claiming the entire building runtime migration is complete.
