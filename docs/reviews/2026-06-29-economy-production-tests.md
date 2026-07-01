# Review Record - Economy and production deterministic tests

Step:
Lock deterministic SimReplay coverage for economy and production behavior.

Milestone:
M4 Production & Economy System.

Owner AI:
Codex main agent.

Reviewer AI:
Codex main-agent review with `ReviewGate economyproductiontests`, `SimReplay`,
and full `VerifyAll`.

Integrator AI:
Codex main agent.

Scope:
- Files/folders:
  - `tools/SimReplay/Program.cs`
  - `tools/ReviewGate/Program.cs`
  - `TODO.md`
  - `docs/reviews/2026-06-29-economy-production-tests.md`
- Non-goals:
  - Do not add new economy behavior.
  - Do not mark resource regeneration tests complete.
  - Do not migrate live UI or legacy runtime authority.

Implementation summary:
- Added `ReviewGate economyproductiontests`.
- The gate locks SimReplay coverage for deterministic `resource-loop` and
  `production-loop` scenarios.
- The gate requires resource depletion, credit banking, dock congestion metrics,
  producer-owned queues, rally behavior, cancel/refund, and multi-producer
  independence assertions.
- Updated TODO with durable evidence for the M4 deterministic economy/production
  tests item.

Automated gates:
- Command:
  `dotnet run --project tools/ReviewGate/ReviewGate.csproj economyproductiontests --no-restore`
  Result:
  Pass.
  Evidence:
  ReviewGate reported 0 errors and 0 warnings.
- Command:
  `dotnet run --project tools/SimReplay/SimReplay.csproj --no-restore`
  Result:
  Pass.
  Evidence:
  SimReplay reported deterministic `resource-loop` and `production-loop` checks,
  including dock congestion, resource depletion, credit banking, production
  cancel/refund, rally, and multi-producer behavior.
- Command:
  `dotnet run --project tools/ReviewGate/ReviewGate.csproj review --require-record=economy-production-tests --no-restore`
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
  Visual QA.
  Result:
  Not required.
  Evidence:
  This is a headless deterministic test coverage slice.

Reviewer result:
- Status: pass.
- Required fixes:
  - None at record creation.
- Residual risks:
  - Environment regeneration tests remain open because regeneration is not
    implemented yet.
  - Live gameplay migration remains separate from this headless test gate.

TODO update:
- Items marked done:
  - `Deterministic economy/production tests: dock reservation under congestion, queue/rally/cancel/refund, multi-producer independence, in SimReplay`.
- Items left open:
  - Deterministic tests that require future resource regeneration.
  - Live EntityWorld authority migration.
- Reason:
  - Current SimReplay source and the new ReviewGate mode prove all named
    deterministic M4 economy/production test cases are present and guarded.
