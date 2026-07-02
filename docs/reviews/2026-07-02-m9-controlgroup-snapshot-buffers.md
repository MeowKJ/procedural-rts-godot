# Review Record - M9 ControlGroup Snapshot Buffers

Step: m9-controlgroup-snapshot-buffers (#143)
Milestone: M9 - Elegance & Decoupling
Owner AI: Remote Linux Codex
Reviewer AI: SelectionStress / ReviewGate controlgrouphudallocations
Integrator AI: Remote Linux Codex

Scope:
- Moved control group HUD snapshot generation into `ControlGroupController.Snapshots.cs`.
- Reused controller-owned `List<ControlGroupSnapshot>` and selected-id `HashSet<int>` for each HUD refresh.
- Replaced legacy/runtime snapshot `ToHashSet()` / `ToList()` / LINQ projection/filter/count chains with explicit scans.
- Extended `ControlGroupAllocationReviewGate` to require the snapshot partial and forbid the old snapshot allocation patterns.
- Kept `ControlGroupController.cs`, `ControlGroupController.Groups.cs`, and `ControlGroupController.Snapshots.cs` under 200 lines.

Automated gates:
- Command: `dotnet build ProceduralRts.csproj --no-restore`
  Result: PASS, 0 warnings / 0 errors.
- Command: `dotnet run --project tools/SelectionStress/SelectionStress.csproj --no-restore`
  Result: PASS, 100 cases.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore -- controlgrouphudallocations --max-warnings=0`
  Result: PASS, Errors: 0, Warnings: 0.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore -- filesize --max-warnings=0`
  Result: PASS after source-budget evidence sync.
- Command: `dotnet run --project tools/VerifyAll/VerifyAll.csproj --no-restore`
  Result: PASS, 23/23 steps.

Reviewer result:
- pass

Status:
- pass

Residual risks:
- Active-state equivalence assumes group id lists remain unique, matching the existing save path.
- Broader `SelectionController` command-line allocation cleanup remains outside this slice.

TODO update:
- M9 per-tick allocation paydown remains open; #143 follow-up is recorded in TODO.md.
