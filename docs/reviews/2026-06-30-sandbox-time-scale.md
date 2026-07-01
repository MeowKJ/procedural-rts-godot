# Review Record - Sandbox Time Scale

Step: Add developer sandbox time-scale controls.
Milestone: M8 Sandbox developer controls.
Owner AI: Main thread.
Reviewer AI: ReviewGate sandboxtimescale plus CombatBehavior assertions.
Integrator AI: Main thread.

Scope:
- Files/folders: `scripts/core/SandboxTimeScaleMath.cs`, `scripts/BattleRoot.cs`, `tools/CombatBehavior/Program.cs`, `tools/ReviewGate/Program.cs`, `TODO.md`, `docs/reviews/2026-06-30-sandbox-time-scale.md`.
- Non-goals: no global `Engine.TimeScale`, no Skirmish speed change, no UI slider, no spawn-any-spec controls, no debug overlay rendering.

Implementation summary:
- Added `SandboxTimeScaleMath` as a pure core helper with bounded presets: 0.25x, 0.5x, 1x, 2x, and 4x.
- Routed BattleRoot gameplay delta through `SandboxTimeScaleMath.ScaledGameplayDelta` while keeping UI timers, alert fading, minimap refresh, and view-culling timers on real frame delta.
- Added sandbox-only F2/F3/F4 controls for slow, reset, and fast playback, with status and alert feedback.
- Added CombatBehavior assertions proving preset stepping, min/max clamp behavior, sandbox delta scaling, Skirmish isolation, and stable label formatting.

Automated gates:
- Command: `dotnet build ProceduralRts.csproj --no-restore`
  Result: pass.
  Evidence: build completed with 0 warnings and 0 errors.
- Command: `dotnet run --project tools/CombatBehavior/CombatBehavior.csproj --no-restore`
  Result: pass.
  Evidence: `Combat behavior passed: weapon hit rules, turret states, terrain passability, localization fallback, presentation descriptors, shared threat propagation, rally production, economy, enemy AI, and outcomes`.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore -- sandboxtimescale`
  Result: pass.
  Evidence: `ReviewGate passed` with 0 errors and 0 warnings.

Manual/visual gates:
- In-game sandbox feel check remains useful after UI spawn/debug controls exist, especially for 4x pathing and fog readability under load.

Reviewer result:
- Status: pass.
- Required fixes: none known.
- Residual risks: `GameState` still has its internal delta clamp, so legacy state paths may not fully express 4x acceleration in every subsystem; the UnitDesign runtime and EntityWorld paths consume the scaled delta directly.

TODO update:
- Items marked done: none.
- Items left open: the broad sandbox parent remains open for spawn-any-spec, owner/faction/team switching, debug overlays, and one-click stress tests.
