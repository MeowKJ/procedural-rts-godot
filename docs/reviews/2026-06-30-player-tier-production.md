# Review Record - Player Tier Production

Step: UnitDesign-driven T1-T3 player production.
Milestone: Playable 1v1 skirmish vertical slice.
Owner AI: Worker A.
Reviewer AI: ReviewGate playertierproduction.
Integrator AI: Main thread.

Scope:
- Files/folders: `scripts/core/units/*` UnitDesign production metadata, `scripts/core/units/runtime/UnitBattlefield.cs`, `scripts/ui/HudLayer.cs`, `scripts/BattleRoot.cs`, `tools/CombatBehavior/Program.cs`, `tools/ReviewGate/Program.cs`, `docs/reviews/2026-06-30-player-tier-production.md`.
- Non-goals: no TODO.md update, no production paging, no upgrade tech, no AI planner production expansion, no legacy ProductionKind deletion, no final balance tuning.

Implementation summary:
- Completed Dog/Cat playable UnitDesign production coverage for T1-T3, including Cat air production from `BuildingKind.Airfield`.
- Ensured UnitBattlefield gives every UnitDesign-backed producer building target a production queue component, covering Airfield without editing the global building bridge.
- Kept HUD production requests design-id driven and expanded the command card to 12 visible slots for the current Dog/Cat T1-T3 roster.
- Added CombatBehavior proof that UnitDesign production options expose every playable Dog/Cat T1-T3 design and complete concrete design-id production into runtime `UnitInstance` outputs.
- Added `ReviewGate playertierproduction` to lock the UnitDesign production, HUD, BattleRoot, CombatBehavior, and review-record contract.

Automated gates:
- Command: `dotnet build ProceduralRts.csproj --no-restore`
  Result: pass.
  Evidence: build completed with 0 warnings and 0 errors.
- Command: `dotnet run --project tools/CombatBehavior/CombatBehavior.csproj --no-restore`
  Result: pass.
  Evidence: `Combat behavior passed: weapon hit rules, turret states, terrain passability, localization fallback, presentation descriptors, shared threat propagation, rally production, economy, enemy AI, and outcomes`.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore -- playertierproduction`
  Result: pass.
  Evidence: `ReviewGate passed` with 0 errors and 0 warnings.

Manual/visual gates:
- Check: HUD screenshot QA.
  Result: not run.
  Evidence: production button change is covered by source and behavior gates; broader visual QA remains for the full production UI slice.

Reviewer result:
- Status: pass.
- Required fixes: none.
- Residual risks: the 12-slot command card is a vertical-slice bridge, not final production paging or per-faction construction UX.

TODO update:
- Items marked done: none.
- Items left open: the broad `Player can...` vertical slice remains open until build, economy, rally, command, and win/loss are proven together.
- Reason: this slice proves UnitDesign-backed T1-T3 training only and intentionally does not update `TODO.md`.
