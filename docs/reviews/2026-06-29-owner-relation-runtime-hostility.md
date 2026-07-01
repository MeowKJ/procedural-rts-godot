# Review Record - Owner relation runtime hostility

Step: Move runtime targetability and hostility checks away from faction helpers and onto owner relation tables.
Milestone: Architecture hard boundaries.
Owner AI: Codex.
Reviewer AI: Codex self-review with CombatBehavior and ReviewGate evidence.
Integrator AI: Codex.

Scope:
- Files/folders: `scripts/core/GameState.cs`, `scripts/core/FactionRelations.cs`, `scripts/core/EnemyAttackWaveAi.cs`, `scripts/controllers/SelectionController.cs`, `tools/CombatBehavior/Program.cs`, `tools/ReviewGate/Program.cs`, `TODO.md`, `docs/reviews/2026-06-29-owner-relation-runtime-hostility.md`.
- Non-goals: no live migration from legacy `GameState` to `EntityWorld`, no UI color redesign, no relation editor, no multiplayer owner model.

Implementation summary:
- Added `GameState.OwnerRelations` as the legacy runtime bridge to `OwnerRelationTable`.
- Added owner-only `GameState.CanOwnerAttack`, `OwnerRelation`, and `IsTargetableHostile` APIs.
- Removed faction parameters from legacy runtime target picking, auto-acquire, building combat, enemy wave targeting, and shared threat propagation.
- Removed `FactionRelations.IsTargetableHostile` so targetability no longer has a faction-named helper.
- Kept `FactionRelations.Relation` for presentation relation semantics such as overlay colors, where faction identity remains useful for display.
- Updated selection input so attack target picking no longer derives a faction from the selected unit.
- Added `ReviewGate ownerrelations` to reject faction-based targetability helpers and old faction parameters in runtime target paths.

Automated gates:
- Command: `dotnet build ProceduralRts.csproj --no-restore`
  Result: pass
  Evidence: build completed with 0 warnings and 0 errors.
- Command: `dotnet run --project tools/CombatBehavior/CombatBehavior.csproj --no-restore`
  Result: pass
  Evidence: CombatBehavior passed, including owner-relation authority and same-faction mirror hostility checks.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj ownerrelations --no-restore`
  Result: pass
  Evidence: ReviewGate reported 0 errors and 0 warnings for owner-only runtime hostility.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore`
  Result: pass
  Evidence: all static review gates passed after adding the owner-relations gate.

Manual/visual gates:
- Check: in-game visual QA
  Result: not run
  Evidence: this slice changes runtime targetability and architecture boundaries, not visual rendering.

Reviewer result:
- Status: pass
- Required fixes: none after the initial owner-only API migration and gate addition.
- Residual risks: `GameState` is still a legacy authority path until the EntityWorld migration is complete; this slice only ensures both legacy and EntityWorld targetability use owner relation tables instead of faction identity.

TODO update:
- Items marked done: `Faction identity never decides runtime hostility - only OwnerRelationTable`.
- Items left open: `Views never mutate authoritative health/movement/target/queue/economy/outcome`, pure-presentation pooled effects, and the broader EntityWorld authority migration.
- Reason: runtime attack/target/acquire/wave paths now resolve hostility through owner relation tables; faction helpers no longer expose a targetability API.
