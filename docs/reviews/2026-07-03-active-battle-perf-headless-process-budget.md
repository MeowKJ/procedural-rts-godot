# Review Record - Active Battle Perf Headless Process Budget

Step: #199 `[Perf] Stabilize ActiveBattlePerfQa headless process budget`
Milestone: M9 - Elegance & Decoupling
Owner AI: Remote Linux Codex
Reviewer AI: ActiveBattlePerfQa / ReviewGate
Integrator AI: Remote Linux Codex

Scope:
- Split `ActiveBattlePerfQaRoot` process budget into interactive and headless thresholds.
- Kept the interactive process budget at 10.0ms.
- Added a 12.0ms headless process budget for CI/headless renderer variance while keeping frame, sim, and fog gates unchanged.
- Non-goals: changing active battle scenario scale, unit roster, camera focus, combat behavior, balance, visuals, or `PerfSmoke`.

Automated gates:
- Command: `dotnet build ProceduralRts.csproj --no-restore`
  Result: pass.
- Command: `godot-dotnet --headless --path . --scene res://scenes/ActiveBattlePerfQa.tscn`
  Result: pass; 57 live / 57 visible units, commanded P/E 28/31, frame avg 7.34ms, process avg 4.70ms, sim avg 0.01ms, fog 2.80ms / 11 uploads.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore -- filesize --max-warnings=0`
  Result: pass.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore`
  Result: pass.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore -- review --require-record=active-battle-perf-headless-process-budget`
  Result: pass.

Reviewer result:
- Status: pass.
- Required fixes: none known before validation.

Status:
- pass

Residual risks:
- The CI failure was close to the previous threshold at 10.18ms; this stabilizes the headless gate but still relies on the frame budget, sim budget, fog budget, and visible-unit requirements for regression coverage.
- Parent #10 remains open for broader allocation paydown.

TODO update:
- Evidence will be posted to #10 and #58 after verification.
- Items marked done: none.
- Items left open: parent #10 broader allocation paydown.
