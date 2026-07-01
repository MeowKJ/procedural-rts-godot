# Review Record - CombatBehavior roster UnitSpec QA cleanup

Step: M1 tool-side unit roster duplicate-data cleanup
Milestone: M1 EntityWorld Becomes Authoritative
Owner AI: Codex
Reviewer AI: ReviewGate combatbehaviorrosterunitspecqa / Integrator
Integrator AI: Codex

Scope:
- Files/folders: `tools/CombatBehavior/Program.cs`, `tools/ReviewGate/Program.cs`, `TODO.md`, `docs/reviews/2026-07-01-combatbehavior-roster-unitspec-qa.md`.
- Non-goals: changing scripts/core runtime, changing authored Dog/Cat rosters, changing legacy bridge mappings, or deleting legacy `UnitKind`.

Implementation summary:
- Moved the early CombatBehavior Dog/Cat roster QA off handwritten `requiredDogUnits` / `requiredCatUnits` legacy `UnitKind` arrays.
- Reused `expectedDogPlayableDesignIds` and `expectedCatPlayableDesignIds` as the tool-side expected roster source.
- Compared those expected ids against `UnitDesignFactionRosterCatalog` playable design ids and validated runtime/presentation coverage by design id.
- Kept conversion to legacy `UnitKind` at the explicit `UnitKindDesignBridge` compatibility coverage edge and at old runtime sandbox checks.
- Added `ReviewGate combatbehaviorrosterunitspecqa` to prevent the handwritten Dog/Cat UnitKind roster from returning to this QA.

Automated gates:
- Command: `dotnet build tools/ReviewGate/ReviewGate.csproj --no-restore`
  Result: pass
  Evidence: local run completed with 0 warnings and 0 errors.
- Command: `dotnet run --project tools/CombatBehavior/CombatBehavior.csproj --no-restore`
  Result: pass
  Evidence: local run printed `Combat behavior passed: weapon hit rules, turret states, terrain passability, localization fallback, presentation descriptors, shared threat propagation, rally production, economy, enemy AI, and outcomes`.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore -- combatbehaviorrosterunitspecqa`
  Result: pass
  Evidence: local run completed with 0 warnings and 0 errors.

Manual/visual gates:
- Check: visual inspection not required for this deterministic tool QA cleanup.
  Result: not run.
  Evidence: no runtime visuals changed; this slice only changes tool assertions and static ReviewGate coverage.

Reviewer result:
- Status: pass
- Required fixes: none.
- Residual risks: legacy `UnitKind` remains intentionally available at compatibility edges until later runtime deletion slices.

TODO update:
- Items marked done: none.
- Items left open: parent M1 runtime migration remains open.
- Reason: this is a narrow tool-side roster QA cleanup, not a scripts/core runtime migration.
