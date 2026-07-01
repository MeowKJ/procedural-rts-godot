# Review Record - Tier scope lock

Step: Make the T1-T3-only vertical-slice tier range explicit and guarded.
Milestone: Playable 1v1 Skirmish - scope locks.
Owner AI: Codex.
Reviewer AI: Codex self-review (CombatBehavior and ReviewGate provide durable checks).
Integrator AI: Codex.

Scope:
- Files/folders: `scripts/core/UnitDefinition.cs`, `scripts/core/UnitCatalog.cs`, `tools/CombatBehavior/Program.cs`, `tools/ReviewGate/Program.cs`, `TODO.md`, `docs/reviews/2026-06-29-tier-scope-lock.md`.
- Non-goals: no new units, no full tech-tree UI, no production unlock system, no claim that every T2/T3 unit is trainable in the live economy loop yet.

Implementation summary:
- Added `TechTier` metadata to legacy `UnitDefinition`, defaulting to T1.
- Marked existing advanced legacy Dog/Cat units as T2 or T3.
- Added CombatBehavior checks that all legacy unit definitions stay in T1-T3.
- Added CombatBehavior checks that Dog and Cat rosters each expose T1, T2, and T3 units with no T4/T5 content.
- Added CombatBehavior checks that data-driven `UnitDesign` specs also stay within T1-T3.
- Added `ReviewGate tiers`.

Automated gates:
- Command: `dotnet build ProceduralRts.csproj --no-restore`
  Result: pass
  Evidence: build completed with 0 warnings and 0 errors after adding tier metadata.
- Command: `dotnet run --project tools/CombatBehavior/CombatBehavior.csproj --no-restore`
  Result: pass
  Evidence: CombatBehavior passed, including legacy and UnitDesign T1-T3 range checks.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj tiers --no-restore`
  Result: pass
  Evidence: ReviewGate reported 0 errors and 0 warnings for tier coverage.

Manual/visual gates:
- Check: visual QA
  Result: not run
  Evidence: this slice is data scope and automated validation; tech-tree visuals remain separate UI work.

Reviewer result:
- Status: pass
- Required fixes: `UnitCatalog.Definition` helper initially did not accept `techTier`; updated it to pass metadata into `UnitDefinition`.
- Residual risks: training/unlock progression for T2/T3 remains an open gameplay systems task; this slice only proves the content range is bounded to T1-T3 and no higher.

TODO update:
- Items marked done: `Tiers: T1, T2, T3 only. No super-units, no experimental tier.`
- Items left open: player can train T1-T3 from producers; T1/T2/T3 roster per faction; tech progression UI and production unlocks.
- Reason: tier metadata and gates now prove the vertical slice is bounded to T1-T3, with Dog/Cat rosters spanning those tiers and no T4/T5 definitions.
