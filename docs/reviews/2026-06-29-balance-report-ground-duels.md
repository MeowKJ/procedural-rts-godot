# Review Record - BalanceReport ground duels

Step:
Add a headless BalanceReport tool for early canonical ground duels and use it to
catch a real dog/cat tank parity issue.

Milestone:
Design Reference - Balance & Tuning Data.

Owner AI:
Codex main agent.

Reviewer AI:
Codex main-agent self-review; independent reviewer was not spawned because the
current thread is operating at the subagent limit.

Integrator AI:
Codex main agent.

Scope:
- Files/folders:
  - `tools/BalanceReport/BalanceReport.csproj`
  - `tools/BalanceReport/Program.cs`
  - `scripts/core/units/cat/CatTank.cs`
  - `tools/VerifyAll/Program.cs`
  - `tools/ReviewGate/Program.cs`
  - `TODO.md`
  - `docs/reviews/2026-06-29-balance-report-ground-duels.md`
- Non-goals:
  - This historical slice did not add aircraft units, anti-air duel coverage, or
    army-vs-army reports; those were completed by the later air/army BalanceReport
    slice.

Implementation summary:
- Added `tools/BalanceReport`, a .NET 8 tool that runs multi-seed EntityWorld
  canonical duels and reports win rates, draw rates, average ticks, survivors, and
  remaining HP.
- The report uses real `UnitDesignCatalog` specs, `EntityWorld`, `GroupAttackEntityCommand`,
  `CommandSystem`, `CombatSystem`, `MovementSystem`, and `SeparationSystem`.
- Initial scenarios cover dog/cat light parity, dog/cat vehicle parity,
  rocket-vs-tank should-win, and patrol-vehicle-vs-light should-win.
- The first run found dog guard tanks beat cat tanks 100% in 4v4 vehicle parity.
  `CatTank` was tuned from 128 to 140 HP, sight/cost adjusted slightly, and its
  turret arc/turn rate widened so the matchup now reports 42%/58%.
- `VerifyAll` now runs BalanceReport by default.
- `ReviewGate balance` verifies the tool is real, uses the deterministic sim, has
  multi-trial scenarios, reports win rates, and fails unacceptable matchups.

Automated gates:
- Command:
  `dotnet run --project tools/BalanceReport/BalanceReport.csproj --no-restore`
  Result:
  Pass.
  Evidence:
  BalanceReport reported 24 trials per scenario; vehicle parity was 42%/58%;
  rocket-vs-tank and anti-light checks both passed.
- Command:
  `dotnet run --project tools/ReviewGate/ReviewGate.csproj balance`
  Result:
  Pass.
  Evidence:
  ReviewGate reported 0 errors and 0 warnings.
- Command:
  `dotnet run --project tools/VerifyAll/VerifyAll.csproj --no-restore`
  Result:
  Pass.
  Evidence:
  VerifyAll passed build, SimReplay, CombatBehavior, SimulationSmoke, FogOfWarQa,
  SelectionStress, ReviewGate, PerfSmoke, BalanceReport, Godot Battle headless,
  and Godot DisplaySettingsQa.

Manual/visual gates:
- Check:
  Manual battle-feel tuning.
  Result:
  Not run.
  Evidence:
  Numeric ground duel parity is now guarded; visual/feel review should follow when
  unit art and live combat readability are revisited.

Reviewer result:
- Status: pass-with-warnings
- Required fixes:
  - None for this bounded ground-duel slice.
- Residual risks:
  - A 24-trial sample is enough for early regression detection, not final esports
    balance.

TODO update:
- Items marked done:
  - None in this historical ground-duel slice.
- Items left open:
  - BalanceReport remained open after this slice until Air/AA and army-vs-army
    scenarios were added by `2026-06-29-balance-report-air-army.md`.
- Reason:
  - This record describes the first ground-duel stage; the later record closes the
    broader BalanceReport TODO.
