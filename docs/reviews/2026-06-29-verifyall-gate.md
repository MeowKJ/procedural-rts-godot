# Review Record - VerifyAll gate

Step:
Add a single AI-friendly verification entrypoint for the current deterministic,
performance, review, and Godot headless gates.

Milestone:
Engineering Conventions.

Owner AI:
Codex main agent.

Reviewer AI:
Codex main-agent self-review; independent reviewer was not spawned because the
current thread is operating at the subagent limit.

Integrator AI:
Codex main agent.

Scope:
- Files/folders:
  - `tools/VerifyAll/VerifyAll.csproj`
  - `tools/VerifyAll/Program.cs`
  - `tools/BalanceReport/BalanceReport.csproj`
  - `tools/BalanceReport/Program.cs`
  - `tools/ReviewGate/Program.cs`
  - `TODO.md`
  - `docs/reviews/2026-06-29-verifyall-gate.md`
- Non-goals:
  - Do not replace the individual tools; this is an orchestrator over them.

Implementation summary:
- Added `tools/VerifyAll`, a sequential .NET 8 console tool that runs the existing
  gates without parallel `dotnet run` DLL lock contention.
- The default command list includes build, SimReplay, CombatBehavior,
  SimulationSmoke, FogOfWarQa, SelectionStress, ReviewGate, PerfSmoke,
  BalanceReport, Godot Battle headless, and DisplaySettingsQa headless.
- `tools/BalanceReport` now exists as an explicit gate, so the default VerifyAll
  path returns one pass/fail result without skip flags.
- Added `--skip-perf`, `--skip-godot`, and `--continue-on-failure` for local
  development workflows without changing the default full gate intent.
- Added `ReviewGate verifyall` coverage so the orchestrator cannot silently drop
  required commands.

Automated gates:
- Command:
  `dotnet build tools/VerifyAll/VerifyAll.csproj`
  Result:
  Pass.
  Evidence:
  Build completed with 0 warnings and 0 errors after initial compile fixes.
- Command:
  `dotnet run --project tools/ReviewGate/ReviewGate.csproj verifyall`
  Result:
  Pass.
  Evidence:
  ReviewGate reported 0 errors and 0 warnings.
- Command:
  `dotnet run --project tools/VerifyAll/VerifyAll.csproj --no-restore -- --skip-perf --skip-godot --allow-missing-balance-report`
  Result:
  Pass.
  Evidence:
  Build, SimReplay, CombatBehavior, SimulationSmoke, FogOfWarQa, SelectionStress,
  and ReviewGate passed; BalanceReport was explicitly skipped.
- Command:
  `dotnet run --project tools/VerifyAll/VerifyAll.csproj --no-restore`
  Result:
  Pass.
  Evidence:
  Build, SimReplay, CombatBehavior, SimulationSmoke, FogOfWarQa, SelectionStress,
  ReviewGate, PerfSmoke, BalanceReport, Godot Battle headless, and Godot
  DisplaySettingsQa passed.

Manual/visual gates:
- Check:
  Manual UI inspection.
  Result:
  Not applicable.
  Evidence:
  This slice adds a command-line verification orchestrator only.

Reviewer result:
- Status: pass-with-warnings
- Required fixes:
  - None for this bounded gate-orchestration slice.
- Residual risks:
  - VerifyAll currently streams command output but does not write a machine-readable
    artifact such as JSON.

TODO update:
- Items marked done:
  - `One verify gate: tools/VerifyAll ... + BalanceReport`.
- Items left open:
  - None for the VerifyAll gate itself.
- Reason:
  - VerifyAll now runs all current required gates, including BalanceReport, and
    returns a single pass/fail result.
