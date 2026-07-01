# Review Record - unit projection opt-out removal

Step: Remove UnitInstanceView projection opt-out
Milestone: M1 EntityWorld Becomes Authoritative
Owner AI: Codex
Reviewer AI: ReviewGate and VerifyAll
Integrator AI: Codex

Scope:
- Files/folders: `scripts/world/UnitInstanceView.cs`, `scripts/BattleRoot.cs`,
  `scripts/BattleRoot.EntityWorld.cs`, `tools/VerifyAll/Program.cs`,
  `tools/ReviewGate/ArchitectureReviewGate.cs`, `TODO.md`,
  `docs/reviews/2026-07-01-unit-projection-optout-removal.md`.
- Non-goals: deleting the live UnitBattlefield adapter, changing balance,
  changing unit art, or redesigning movement/combat behavior.

Implementation summary:
- Removed `UnitInstanceView.ProjectionEnabledProvider`; unit views now read
  `EntityProjection` whenever the provider has one.
- Removed `BattleRoot.UseEntityWorldUnits`, `DebugUseEntityWorldUnits`, and the
  `PROCEDURAL_RTS_USE_ENTITY_WORLD_UNITS` environment switch.
- Removed the forced entity/legacy double Godot boot steps from `VerifyAll`.
- Updated the architecture gate so it no longer requires the deleted migration
  flag, and updated ReviewGate source-budget evidence to 26 C# files / 1509
  lines.

Automated gates:
- Command: `dotnet build ProceduralRts.csproj --no-restore`
  Result: pass
  Evidence: main Godot C# project compiled with 0 warnings and 0 errors.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore -- filesize`
  Result: pass
  Evidence: ReviewGate filesize completed with 0 errors and 0 warnings.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore`
  Result: pass
  Evidence: full ReviewGate completed with 0 errors and 0 warnings.
- Command: `dotnet run --project tools/VerifyAll/VerifyAll.csproj --no-restore`
  Result: pass
  Evidence: VerifyAll completed all 22 steps successfully after the opt-out
  removal.

Manual/visual gates:
- Check: source search for the removed projection opt-out
  Result: pass
  Evidence: runtime/tool sources no longer reference
  `PROCEDURAL_RTS_USE_ENTITY_WORLD_UNITS`, `UseEntityWorldUnits`, or
  `ProjectionEnabledProvider`.

Reviewer result:
- Status: pass
- Required fixes: none.
- Residual risks: historical review/archive records still mention the old flag
  as past migration context; runtime and active gate code no longer depend on it.

TODO update:
- Items marked done: `Remove unit projection opt-out`.
- Items left open: none under active M1 summary.
- Reason: live unit presentation no longer has a legacy projection disable path,
  and the broad verification suite passes without the old fallback boot step.
