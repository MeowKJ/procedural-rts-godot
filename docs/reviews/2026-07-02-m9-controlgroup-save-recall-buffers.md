# Review Record - M9 ControlGroup Save/Recall Buffers

Step: m9-controlgroup-save-recall-buffers (#141)
Milestone: M9 - Elegance & Decoupling
Owner AI: Remote Linux Codex
Reviewer AI: SelectionStress / ReviewGate presentation
Integrator AI: Remote Linux Codex

Scope:
- Reused stored control-group `List<int>` instances in `SaveGroup(...)` instead of materializing selected ids through LINQ.
- Added explicit selected-id collection from `UnitBattlefield.Units` or legacy `GameState.Units`.
- Routed recall selection through read-only id storage and a legacy explicit scan, avoiding `ToHashSet()` allocation.
- Replaced double-tap recall center `HashSet` + position-list materialization with a single running-sum scan.
- Added `ControlGroupAllocationReviewGate` under the presentation gate to lock the save/recall allocation contract.

Automated gates:
- Command: `dotnet build ProceduralRts.csproj --no-restore`
  Result: PASS, 0 warnings / 0 errors.
- Command: `dotnet run --project tools/SelectionStress/SelectionStress.csproj --no-restore`
  Result: PASS, 100 cases.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore -- presentation --max-warnings=0`
  Result: PASS, Errors: 0, Warnings: 0.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore`
  Result: PASS after source-budget evidence sync.
- Command: `dotnet run --project tools/VerifyAll/VerifyAll.csproj --no-restore`
  Result: PASS, 23/23 steps.

Reviewer result:
- pass

Status:
- pass

Residual risks:
- Snapshot generation still has allocation debt; it is intentionally left for separate child issues.
- `GameState.SelectUnitsByIds(...)` remains allocating for other callers; this slice only bypasses it from control-group recall.

TODO update:
- M9 per-tick allocation paydown remains open; #141 follow-up is recorded in TODO.md.
