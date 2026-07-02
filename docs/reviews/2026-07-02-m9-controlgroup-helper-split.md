# Review Record - M9 ControlGroup Helper Split

Step: m9-controlgroup-helper-split (#142)
Milestone: M9 - Elegance & Decoupling
Owner AI: Remote Linux Codex
Reviewer AI: SelectionStress / ReviewGate filesize
Integrator AI: Remote Linux Codex

Scope:
- Moved control group save/recall/id-scan helpers from `ControlGroupController.cs` into `ControlGroupController.Groups.cs`.
- Kept `ControlGroupController.cs` as the stable Godot Node entry point for process/input/snapshot orchestration.
- Updated `ControlGroupAllocationReviewGate` to read partial-aware evidence and require the focused group partial.
- Synchronized validation-tool source-budget evidence after the ReviewGate line-count drift.

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
- Snapshot allocation cleanup is intentionally deferred to a separate child issue.
- `ControlGroupController.cs` and `ControlGroupController.Groups.cs` are both 175 lines; future snapshot work should use focused partials rather than regrowing the entry file.

TODO update:
- M9 per-tick allocation paydown remains open; #142 follow-up is recorded in TODO.md.
