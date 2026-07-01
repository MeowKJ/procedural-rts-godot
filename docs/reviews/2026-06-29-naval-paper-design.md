# Review Record - Naval paper design

Step:
Document naval units as paper-only and gate against playable naval implementation.

Milestone:
Design Reference - Art & Style / Explicit Non-Goals.

Owner AI:
Codex main agent.

Reviewer AI:
Codex main-agent review with `ReviewGate unitclasses`.

Integrator AI:
Codex main agent.

Scope:
- Files/folders:
  - `docs/unit-data/naval-paper-design.md`
  - `tools/ReviewGate/Program.cs`
  - `TODO.md`
  - `docs/reviews/2026-06-29-naval-paper-design.md`
- Non-goals:
  - Do not add playable naval units.
  - Do not add shipyards, naval production, naval balance scenarios, or map-editor
    water gameplay.
  - Do not remove future enum hooks such as `MovementDomain.Naval` or `ArmorTag.Ship`.

Implementation summary:
- Added a Chinese paper-design document for future naval units.
- The document defines the current `PAPER-ONLY-NAVAL` status, future movement-domain
  meaning, readability rules, possible unit directions, faction style differences,
  and explicit current-slice bans.
- Extended `ReviewGate unitclasses` to require the paper document and to reject
  playable ship/naval `UnitDesign` file names in the current slice.

Automated gates:
- Command:
  `dotnet run --project tools/ReviewGate/ReviewGate.csproj unitclasses --no-restore`
  Result:
  Pass.
  Evidence:
  ReviewGate reported 0 errors and 0 warnings.
- Command:
  `dotnet run --project tools/ReviewGate/ReviewGate.csproj review --require-record=naval-paper-design --no-restore`
  Result:
  Pass.
  Evidence:
  ReviewGate reported 0 errors and 0 warnings.
- Command:
  `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore`
  Result:
  Pass.
  Evidence:
  ReviewGate reported 0 errors and 0 warnings.
- Command:
  `dotnet run --project tools/VerifyAll/VerifyAll.csproj --no-restore`
  Result:
  Pass.
  Evidence:
  VerifyAll passed all 14 steps: build, SimReplay, CombatBehavior,
  SimulationSmoke, FogOfWarQa, SelectionStress, AiDifficultySmoke, ReviewGate,
  PerfSmoke, BalanceReport, and Godot headless QA scenes.

Manual/visual gates:
- Check:
  Visual naval concept review.
  Result:
  Not run.
  Evidence:
  This is paper design only; no renderable naval assets were created.

Reviewer result:
- Status: pass.
- Required fixes:
  - None at record creation.
- Residual risks:
  - Naval balance, production, pathing, and UI remain future work by design.
  - The disabled naval HUD tab still exists as a future affordance.

TODO update:
- Items marked done:
  - `Ship/Naval (PAPER DESIGN ONLY this slice)`.
  - `No naval units built (paper design only)`.
- Items left open:
  - Any future real naval implementation.
- Reason:
  - The design is documented under `docs/unit-data`, and gates prove no playable
    ship/naval content has entered the current vertical slice.
