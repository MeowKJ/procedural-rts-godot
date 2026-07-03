# Review Record - M9 Active Battle Perf Attacker Buffers

Step: #198 `[M9] Reuse active battle perf attacker id buffers`
Milestone: M9 - Elegance & Decoupling
Owner AI: Remote Linux Codex
Reviewer AI: ReviewGate presentation / ActiveBattlePerfQa
Integrator AI: Remote Linux Codex

Scope:
- Added reusable player/enemy attacker id buffers for active battle performance scenario setup.
- Replaced attacker id `Where/Select/ToArray` chains with `CollectActiveBattlePerfAttackers(...)` explicit scans.
- Kept the original player slot, alive, and weapon-mount filtering contract before submitting `CommandAttackUnits(...)`.
- Extended `ReviewGate presentation` to lock active battle perf attacker id no-LINQ contracts.
- Non-goals: changing ActiveBattlePerfQa scale, targets, camera focus, combat semantics, balance, story, or UI visuals.

Automated gates:
- Command: `dotnet build ProceduralRts.csproj --no-restore`
  Result: pass.
- Command: `godot-dotnet --headless --path . --scene res://scenes/ActiveBattlePerfQa.tscn`
  Result: pass; 57 live / 57 visible units, commanded P/E 28/31, frame avg 7.18ms, process avg 3.94ms, sim avg 0.01ms, fog 2.70ms / 10 uploads.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore -- presentation --max-warnings=0`
  Result: pass.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore -- review --require-record=m9-active-battle-perf-attacker-buffers`
  Result: pass.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore -- filesize --max-warnings=0`
  Result: pass.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore`
  Result: pass.

Reviewer result:
- Status: pass.
- Required fixes: none known before validation.

Status:
- pass

Residual risks:
- This removes source-level setup allocations in a QA/perf scenario; it is not a standalone allocation profiler sample.
- Parent #10 remains open for broader allocation paydown.

TODO update:
- Synced validation tool suite budget evidence after extending `ReviewGatePresentation`.
- Evidence will be posted to #10 and #58 after verification.
- Items marked done: none.
- Items left open: parent #10 broader allocation paydown.
