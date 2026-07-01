# Review Record - EntityWorld shadow toggle

Step:
Allow profiling runs to disable the non-authoritative EntityWorld shadow path.

Milestone:
M6 Performance.

Owner AI:
Codex main agent.

Reviewer AI:
Codex main-agent self-review; independent reviewer was not spawned because the
current thread reached the subagent limit.

Integrator AI:
Codex main agent.

Scope:
- Files/folders:
  - `scripts/BattleRoot.cs`
  - `tools/ReviewGate/Program.cs`
  - `TODO.md`
  - `docs/reviews/2026-06-29-entity-shadow-toggle.md`
- Non-goals:
  - Do not make EntityWorld authoritative in this slice.
  - Do not remove the shadow path.
  - Do not change default gameplay behavior.

Implementation summary:
- Added `BattleRoot.RunEntityWorldShadow`.
- Default behavior continues running the non-authoritative EntityWorld shadow.
- Profiling can disable the shadow with `PROCEDURAL_RTS_DISABLE_ENTITY_SHADOW=1`
  or `PROCEDURAL_RTS_ENTITY_SHADOW=0`.
- `ConfigureEntityWorld()` and `StepEntityWorld()` are both guarded by the toggle.
- Added `ReviewGate shadow` to verify the profiling toggle hooks.

Automated gates:
- Command:
  `dotnet build ProceduralRts.csproj`
  Result:
  Pass.
  Evidence:
  Build completed with 0 warnings and 0 errors.
- Command:
  `dotnet run --project tools/ReviewGate/ReviewGate.csproj shadow`
  Result:
  Pass.
  Evidence:
  ReviewGate reported 0 errors and 0 warnings.
- Command:
  `dotnet run --project tools/ReviewGate/ReviewGate.csproj review --require-record=entity-shadow-toggle`
  Result:
  Pass.
  Evidence:
  ReviewGate reported 0 errors and 0 warnings.
- Command:
  `dotnet run --project tools/ReviewGate/ReviewGate.csproj`
  Result:
  Pass.
  Evidence:
  ReviewGate reported 0 errors and 0 warnings.

Manual/visual gates:
- Check:
  Runtime launch with and without the environment variable.
  Result:
  Not run.
  Evidence:
  This slice is guarded by source/build/static gates only.

Reviewer result:
- Status: pass-with-warnings
- Required fixes:
  - None for this bounded source-level slice.
- Residual risks:
  - Independent reviewer was not available due to subagent limit.
  - Runtime profiling with the environment variable should still be performed before
    relying on measurements from this switch.

TODO update:
- Items marked done:
  - None; runtime profiling verification remains open.
- Items left open:
  - Profiling run proving cost isolation.
  - EntityWorld authority migration.
- Reason:
  - Evidence proves the switch exists and is wired, but not its runtime profiling
    workflow.
