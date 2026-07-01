# Review Record - Skirmish-only scope

Step: Lock campaign out of the current vertical slice and keep the playable surface skirmish-only.
Milestone: Playable 1v1 Skirmish - scope locks.
Owner AI: Codex.
Reviewer AI: Codex self-review (CombatBehavior and ReviewGate provide durable checks).
Integrator AI: Codex.

Scope:
- Files/folders: `scripts/core/LaunchMode.cs`, `scripts/core/SkirmishOptions.cs`, `scripts/core/MatchConfig.cs`, `scripts/MainMenuRoot.cs`, `tools/CombatBehavior/Program.cs`, `tools/ReviewGate/Program.cs`, `TODO.md`, `docs/reviews/2026-06-29-skirmish-only-scope.md`.
- Non-goals: no campaign deletion from design notes/TODO future sections, no story implementation, no trigger/objective graph, no UI visual redesign, no change to sandbox.

Implementation summary:
- Added CombatBehavior evidence that launch modes stay limited to skirmish and developer sandbox.
- Added a specific CombatBehavior guard that rejects campaign launch modes while the playable slice remains skirmish-only.
- Added `ReviewGate skirmishonly` to guard launch modes, menu entry points, campaign/chapter/mission scenes, and campaign runtime script names.
- Kept future campaign planning text in TODO untouched; this scope lock only covers implemented runtime surfaces.

Automated gates:
- Command: `dotnet build ProceduralRts.csproj --no-restore`
  Result: pass
  Evidence: build completed with 0 warnings and 0 errors.
- Command: `dotnet run --project tools/CombatBehavior/CombatBehavior.csproj --no-restore`
  Result: pass
  Evidence: CombatBehavior passed, including the skirmish-only launch-mode guard.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj skirmishonly --no-restore`
  Result: pass
  Evidence: ReviewGate reported 0 errors and 0 warnings for skirmish-only scope.

Manual/visual gates:
- Check: visual QA
  Result: not run
  Evidence: this is a runtime-surface scope guard; no visible UI layout changed.

Reviewer result:
- Status: pass
- Required fixes: none after scoping the scan to runtime entry points and file names instead of broad design text.
- Residual risks: story and objective-graph design remains in future TODO sections by design; this record does not claim campaign planning is complete or deleted.

TODO update:
- Items marked done: `Campaign: out of scope (TBD). Skirmish only.`
- Items left open: future M8 objective-graph campaign, no campaign/missions/scripted triggers yet, Dog/Cat full playability, player loop, AI command-buffer visibility, and counters.
- Reason: automated gates now prove the implemented launch surface remains skirmish-only, with sandbox limited to developer testing.
