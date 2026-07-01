# Review Record - Explicit scope guards

Step:
Close explicit non-goal guardrails for multiplayer, campaign, third-faction content,
in-progress save/load, and map editor scope.

Milestone:
Explicit non-goals.

Owner AI:
Codex main agent.

Reviewer AI:
Codex main-agent review with `ReviewGate scopeguards`, `mode1v1`,
`skirmishonly`, and `thirdplaceholder`.

Integrator AI:
Codex main agent.

Scope:
- Files/folders:
  - `tools/ReviewGate/Program.cs`
  - `TODO.md`
  - `docs/reviews/2026-06-29-explicit-scope-guards.md`
- Non-goals:
  - Do not implement multiplayer, campaign, third-faction content, save/load, or a
    map editor.
  - Do not remove deterministic command/replay-friendly core hooks.
  - Do not remove developer sandbox.
  - Do not forbid ordinary settings persistence.

Implementation summary:
- Added `ReviewGate scopeguards`.
- The gate rejects:
  - multiplayer launch modes, network/player-count config fields, UI entry points,
    and Godot networking APIs;
  - campaign/mission/chapter launch modes, scenes, and runtime script surfaces;
  - registered third-faction catalog content or third-faction `UnitDesign` content;
  - SaveGame/LoadGame/SavedMatch/MatchSave runtime or scene surfaces;
  - MapEditor/EditorMap runtime or scene surfaces.
- The gate intentionally allows:
  - deterministic command core and future-ready enum hooks;
  - developer sandbox;
  - settings persistence;
  - locked `Corruption` enum/menu placeholder.

Automated gates:
- Command:
  `dotnet run --project tools/ReviewGate/ReviewGate.csproj scopeguards --no-restore`
  Result:
  Pass.
  Evidence:
  ReviewGate reported 0 errors and 0 warnings.
- Command:
  `dotnet run --project tools/ReviewGate/ReviewGate.csproj mode1v1 --no-restore`
  Result:
  Pass.
  Evidence:
  ReviewGate reported 0 errors and 0 warnings.
- Command:
  `dotnet run --project tools/ReviewGate/ReviewGate.csproj skirmishonly --no-restore`
  Result:
  Pass.
  Evidence:
  ReviewGate reported 0 errors and 0 warnings.
- Command:
  `dotnet run --project tools/ReviewGate/ReviewGate.csproj thirdplaceholder --no-restore`
  Result:
  Pass.
  Evidence:
  ReviewGate reported 0 errors and 0 warnings.
- Command:
  `dotnet run --project tools/ReviewGate/ReviewGate.csproj review --require-record=explicit-scope-guards --no-restore`
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
  None required.
  Result:
  Not applicable.
  Evidence:
  This is a scope guardrail, not a visual change.

Reviewer result:
- Status: pass.
- Required fixes:
  - None at record creation.
- Residual risks:
  - These gates reject obvious runtime/scene/config surfaces. Future systems with
    unrelated names could still implement out-of-scope behavior and would need code
    review, but the current likely entry points are covered.

TODO update:
- Items marked done:
  - `No multiplayer networking`.
  - `No campaign / missions / scripted triggers yet`.
  - `No third faction content`.
  - `No save/load of in-progress matches`.
  - `No map editor`.
- Items left open:
  - The positive playable-slice requirements remain open.
- Reason:
  - Current source, scenes, configs, and gates prove these non-goals are absent from
    the vertical slice while preserving future-ready hooks where explicitly allowed.
