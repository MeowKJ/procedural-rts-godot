# Review Record - Roster Authoring QA

Step: Add a deterministic roster authoring QA tool for Dog/Cat playable data.
Milestone: M7 roster and counter authoring support.
Owner AI: Worker B.
Reviewer AI: ReviewGate rosterauthoringqa.
Integrator AI: Main thread.

Scope:
- Files/folders: `tools/RosterAuthoringQa`, `tools/VerifyAll/Program.cs`, `tools/ReviewGate/Program.cs`, `TODO.md`, `docs/reviews/2026-06-30-roster-authoring-qa.md`.
- Non-goals: no live HUD proof, no production runtime proof, no Dog air content implementation, no TODO parent completion.

Implementation summary:
- Added `tools/RosterAuthoringQa` to validate Dog/Cat playable UnitDesign rosters across tiers, domains, production categories, starting units, playable air, counter hooks, locked third-faction placeholder, no playable naval, and i18n keys.
- Dog air is now a hard authoring requirement, so strict mode and current-slice baseline share the same playable-air expectation.
- Wired RosterAuthoringQa into `tools/VerifyAll`.
- Added `ReviewGate rosterauthoringqa` to lock the tool and TODO progress record.

Automated gates:
- Command: `dotnet run --project tools/RosterAuthoringQa/RosterAuthoringQa.csproj --no-restore`
  Result: pass.
  Evidence: `RosterAuthoringQa PASSED` with no playable-air warning.
- Command: `dotnet build ProceduralRts.csproj --no-restore`
  Result: pass.
  Evidence: build completed with 0 warnings and 0 errors.

Manual/visual gates:
- None. This is an authoring-data QA tool.

Reviewer result:
- Status: pass.
- Required fixes: none.
- Residual risks: This tool does not replace CombatBehavior/live production validation.

TODO update:
- Items marked done: none.
- Items left open: broad Dog/Cat T1-T3 roster remains open until final playable completeness and balance acceptance are closed.
- Reason: this slice adds deterministic authoring coverage only.
