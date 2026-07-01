# Review Record - building damaged readability

Step:
M1/M7 focused presentation slice
Milestone:
Low-power / offline / damaged building readability
Owner AI:
Codex worker implementation
Reviewer AI:
Pending integrator review
Integrator AI:
Pending main-thread integration

Scope:
- Files/folders:
  - scripts/core/sim/BuildingPresentationProjection.cs
  - scripts/world/BuildingView.cs
  - tools/CombatBehavior/Program.cs
  - tools/ReviewGate/Program.cs
- Non-goals:
  - No gameplay authority changes.
  - No TODO.md edits.
  - No broad building destruction animation pass.
  - No screenshot QA claim for final art polish.

Implementation summary:
- Building presentation projections expose deterministic damaged-state helpers from projected EntityWorld health.
- BuildingView prefers projected damage severity and falls back to the shared helper for legacy runtime paths.
- BuildingView draws low-obstruction edge cracks, small local gaps, and compact severe-damage sparks without using owner color as damage semantics.
- ReviewGate has a focused `buildingdamagedreadability` gate covering projection helpers, view drawing hooks, CombatBehavior proof, and this review record.

Automated gates:
- Command: dotnet build ProceduralRts.csproj --no-restore
  Result: pass
  Evidence: Project generated ProceduralRts.dll with 0 warnings / 0 errors after a transient parallel-worker UnitBattlefield edit finished.
- Command: dotnet run --project tools/CombatBehavior/CombatBehavior.csproj --no-restore
  Result: pass
  Evidence: CombatBehavior passed with damaged building readability assertions.
- Command: dotnet run --project tools/ReviewGate/ReviewGate.csproj buildingdamagedreadability --no-restore
  Result: pass
  Evidence: Focused gate passed with 0 errors / 0 warnings.

Manual/visual gates:
- Check: Source visual audit
  Result: pass-with-warnings
  Evidence: Damage marks are edge-only and non-owner-colored; live Godot screenshot QA is still recommended before closing the broad readability TODO.

Reviewer result:
- Status: pass-with-warnings
- Required fixes:
  - None for the narrow damaged-state projection and source-rendering slice.
- Residual risks:
  - Exact crack/spark pixel balance still needs an in-engine visual pass.
  - The broader TODO remains open until low-power/offline/damaged states are integrated with alert UX and visually accepted together.

TODO update:
- Items marked done:
  - None by this worker.
- Items left open:
  - Low-power / offline / damaged building states are readable (art + alert), not silent.
- Reason:
  - This slice proves damaged-state readability only; TODO.md updates are reserved for the main integrator.
