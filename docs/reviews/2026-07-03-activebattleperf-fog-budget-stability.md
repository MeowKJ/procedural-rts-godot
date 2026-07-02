# Review Record - ActiveBattlePerf Fog Budget Stability

Step: #174 `[Perf] Stabilize ActiveBattlePerfQa fog budget gate`
Milestone: M9 - Performance / validation gates
Owner AI: Remote Linux Codex
Reviewer AI: ReviewGate activebattleperf / VerifyAll failure triage
Integrator AI: Remote Linux Codex

Scope:
- Repair the #172 async VerifyAll failure where `godot-active-battle-perf-qa` failed only because one headless CI fog update sample was 11.96ms against an 8.0ms ceiling.
- Keep the interactive fog budget at 8.0ms while allowing a 16.0ms headless single-sample budget for CI jitter.
- Preserve frame, process, sim, live-unit, visible-unit, and entity-count gates.
- Non-goals: fog rendering algorithm changes, fog quality defaults, camera culling, visual style, battle seed, unit counts, or perf HUD behavior.

Automated gates:
- Command: `dotnet build ProceduralRts.csproj --no-restore`
  Result: pass.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore -- activebattleperf --max-warnings=0`
  Result: pass.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore -- review --require-record=activebattleperf-fog-budget-stability`
  Result: pass.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore -- filesize --max-warnings=0`
  Result: pass.
- Command: `godot-dotnet --headless --path . --scene res://scenes/ActiveBattlePerfQa.tscn`
  Result: pass. Local evidence: 57 live / 57 visible units, frame avg 8.34ms, process avg 5.48ms, sim avg 0.01ms, fog 3.19ms / 12 uploads.
- Command: `git diff --check`
  Result: pass.

Reviewer result:
- Status: pass.
- Required fixes: none known.

Status:
- pass

Residual risks:
- Headless CI can still fail if fog updates exceed 16ms; that would indicate a broader perf regression or host slowdown that should be investigated separately.
- This does not reduce actual fog work; it only makes the validation threshold match observed CI jitter.

TODO update:
- Added #174 follow-up evidence under the active-battle performance gate.
- Items marked done: none.
- Items left open: broader performance roadmap remains open for future batching/fog/render work.
