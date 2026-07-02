# Review Record - M9 CommandGateway Validation Scans

Step: #169 `[M9] Replace CommandGateway validation LINQ`
Milestone: M9 - Elegance & Decoupling
Owner AI: Remote Linux Codex
Reviewer AI: ReviewGate architecture/regression / SimReplay
Integrator AI: Remote Linux Codex

Scope:
- Replace invalid subject `Any(...)` in `CommandGateway.ValidatePayload(...)` with an indexed scan.
- Replace slot ownership `Any(...)` in `CommandGateway.ControlsSlot(...)` with an indexed scan.
- Cache `CommandGatewayResult` accepted count at construction time with an explicit scan, so rejected count does not rescan through predicate LINQ.
- Extend `CommandSystemAllocationReviewGate` so architecture/regression gates forbid these LINQ patterns from returning.
- Non-goals: changing command schema, validation errors, sequence handling, sandbox gating, sink behavior, or public `Commands` shape.

Automated gates:
- Command: `dotnet build ProceduralRts.csproj --no-restore`
  Result: pass.
- Command: `dotnet run --project tools/SimReplay/SimReplay.csproj --no-restore`
  Result: pass.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore -- architecture --max-warnings=0`
  Result: pass.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore -- regression --max-warnings=0`
  Result: pass.
- Command: `dotnet run --project tools/ReviewGate/ReviewGate.csproj --no-restore -- review --require-record=m9-commandgateway-validation-scans`
  Result: pass.

Reviewer result:
- Status: pass.
- Required fixes: none known.

Status:
- pass

Residual risks:
- Tools and QA assertions may still use LINQ; this slice only covers runtime command gateway code.
- Parent #10 remains open for broader allocation paydown.

TODO update:
- Added #169 follow-up evidence under the open M9 per-tick allocation paydown item.
- Items marked done: none.
- Items left open: parent #10 broader allocation paydown.
